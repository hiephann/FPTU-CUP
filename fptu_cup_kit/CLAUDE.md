# CLAUDE.md – FPTU Cup (Hệ thống Quản lý Giải bóng đá Sinh viên)

Bài tập lớn cuối kỳ của nhóm 3 người. Đây là **backend RESTful Web API**, không phải đồ án lớn.
Không thêm Docker, Kafka/RabbitMQ, microservices, SignalR hay cloud. Nếu không được yêu cầu,
cũng không dựng frontend; mẫu giao diện trong `design/` chỉ để tham khảo.

Tài liệu gốc (khi có mâu thuẫn, ưu tiên theo thứ tự này):
1. `docs/requirements.md` – đặc tả đầy đủ (FR, BR, API, CSDL, design spec 15 chức năng)
2. `docs/database/schema.sql` – script SQL Server tham chiếu
3. `docs/Requirement_Design_Specification_SFTMS*.docx` – bản Word để nộp (EN + VI)

## Công nghệ

| Thành phần | Lựa chọn |
| --- | --- |
| Framework | ASP.NET Core Web API (.NET 8), Controllers (không dùng Minimal API) |
| ORM | Entity Framework Core 8, Code First + Migrations |
| CSDL | Microsoft SQL Server |
| Xác thực | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), băm mật khẩu bằng BCrypt (`BCrypt.Net-Next`) |
| Validation | Data Annotations (hoặc FluentValidation nếu nhóm thống nhất) |
| Tài liệu API | Swagger/OpenAPI (Swashbuckle) có nút Authorize nhập JWT |

## Cấu trúc solution đề xuất

```
FptuCup.sln
src/FptuCup.Api/
  Controllers/        // mỏng: nhận DTO, gọi Service, trả ApiResponse
  Services/           // toàn bộ nghiệp vụ + kiểm tra BR01–BR22
    Interfaces/
  Repositories/       // truy cập dữ liệu qua DbContext
    Interfaces/
  Data/
    FptuCupDbContext.cs
    Configurations/   // IEntityTypeConfiguration<T> cho từng bảng
    Seed/             // dữ liệu mẫu
  Entities/           // 11 entity khớp docs/database/schema.sql
  DTOs/               // Request/Response, KHÔNG trả Entity ra ngoài
  Middlewares/        // ExceptionMiddleware trả JSON thống nhất
  Common/             // ApiResponse<T>, PagedResult<T>, AppException, enums
  Program.cs
  appsettings.json
```

## Quy ước code

- Kiến trúc: Controller → Service → Repository → DbContext. Đăng ký mọi thứ bằng DI có sẵn.
- Đặt tên PascalCase cho class/property, camelCase cho JSON. Mỗi file một class.
- Mọi response dùng chung dạng `{ "success": bool, "message": string, "data": T }`.
- API danh sách luôn phân trang `?page=&pageSize=&search=&sortBy=` (mặc định 10, tối đa 50).
- Mã lỗi: 400 dữ liệu sai · 401 chưa đăng nhập · 403 sai quyền/không sở hữu · 404 không thấy · 409 vi phạm nghiệp vụ hoặc trùng.
- Vi phạm quy tắc nghiệp vụ thì `throw` exception riêng (ví dụ `BusinessRuleException` → 409), middleware sẽ map sang mã HTTP. Không trả stack trace.
- Thời gian lưu UTC (`DATETIME2`). Enum lưu dạng chuỗi (`HasConversion<string>()`).
- Không hard-code secret: JWT key và connection string đặt trong `appsettings.json` / user-secrets.
- Phân quyền: `[Authorize(Roles = "Admin")]`. Kiểm tra quyền sở hữu (đội của mình, trận được phân công) ở Service.
- Thao tác nhiều bảng (kết thúc trận, sinh lịch) phải chạy trong transaction.
- Bảng xếp hạng **không lưu bảng riêng**, mà tính từ các trận `Finished` (view `vw_Standings` hoặc LINQ).

## Thể thức giải (quan trọng)

- Giải đá **sân 7**, vòng tròn **một bảng duy nhất** (giống Ngoại hạng Anh/La Liga), **không chia bảng**, không có bảng `Groups`.
- Đá 1 hoặc 2 lượt (`Tournaments.NumberOfLegs`). Với n đội, 2 lượt có 2×(n−1) vòng; lượt về đảo sân nhà/khách.
- Mỗi trận 2 hiệp × 30 phút, nghỉ 10 phút. Phút sự kiện hợp lệ: 1–60. Hòa không đá luân lưu.
- Mỗi đội có 7–14 cầu thủ. Đội hình xuất phát đúng 7 người, có đúng 1 GK, còn lại là dự bị.
- Đội hình **bị khóa khi trận bắt đầu**. Thay người lưu riêng ở bảng `Substitutions`, không sửa `MatchLineups`.
- Tính điểm: thắng 3, hòa 1, thua 0. Xếp hạng theo: Điểm → Hiệu số → Bàn thắng → Đối đầu.
- Thẻ phạt: 2 vàng trong 1 trận = đỏ; đỏ = treo 1 trận; tích lũy 3 vàng = treo 1 trận.

Danh sách đầy đủ 22 quy tắc nghiệp vụ (BR01–BR22) nằm ở `docs/requirements.md`, mục 2.5.
Khi viết code cho quy tắc nào, ghi chú mã BR tương ứng trong comment.

## Vai trò

`Admin` (ban tổ chức) · `Referee` (trọng tài) · `TeamManager` (quản lý đội) · `Guest` (không đăng nhập, chỉ đọc).

## Lệnh thường dùng

```bash
dotnet restore
dotnet build
dotnet ef migrations add <Ten> -p src/FptuCup.Api
dotnet ef database update -p src/FptuCup.Api
dotnet run --project src/FptuCup.Api      # Swagger: https://localhost:<port>/swagger
```

## Phân công nhóm

| Thành viên | Module | FR |
| --- | --- | --- |
| HE151219 – Võ Phan Hoàng Tùng | Xác thực, Giải đấu, Sân & trọng tài; khởi tạo solution, DbContext, JWT, middleware | FR01–FR07, FR10, FR15–FR16 |
| HE186191 – Phan Quang Hiệp | Đội bóng, Cầu thủ, Đội hình, Thống kê cầu thủ; seed data, Postman | FR08–FR09, FR11–FR14, FR26–FR29 |
| HE172394 – Hoàng Ngọc Tỉnh | Lịch thi đấu, Kết quả, Bảng xếp hạng; sinh lịch vòng tròn, treo giò | FR17–FR25, FR30–FR31 |

## Khi làm việc trong repo này

- Làm theo đúng endpoint, DTO và mã lỗi ghi trong `docs/requirements.md`. Thêm endpoint mới thì cập nhật luôn tài liệu.
- Không đổi tên bảng/cột so với `schema.sql` nếu chưa hỏi lại.
- Sau khi sửa, chạy `dotnet build` và đảm bảo không có lỗi trước khi báo xong.
