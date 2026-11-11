# CLAUDE.md – FPTU Cup (Hệ thống Quản lý Giải bóng đá Sinh viên)

Bài tập lớn cuối kỳ của nhóm 3 người. Đây là **backend RESTful Web API**, không phải đồ án lớn.
Không thêm Docker, Kafka/RabbitMQ, microservices, SignalR hay cloud. Nếu không được yêu cầu,
cũng không dựng frontend riêng (React...); giao diện hiện có là MVC Razor mỏng gọi API, mẫu trong `design/` chỉ để tham khảo.

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
| Truy vấn | OData (`Microsoft.AspNetCore.OData` 8) chỉ đọc ở `/odata` trên các DTO |
| Giao diện | MVC Razor + Bootstrap 5 (CDN), TagHelper, PartialView; đăng nhập MVC bằng ASP.NET Core Identity (cookie) |

## Cấu trúc project (đơn giản, dự án là API)

Project nằm ở `fptu_cup/` (namespace `fptu_cup`). Không có Service/Repository/interface: controller inject thẳng `FptuCupDbContext` và viết logic trong controller.

```
Controllers/          // API controller: [ApiController], logic + kiểm tra BR01–BR23 viết thẳng ở đây
  ApiControllerBase   // CurrentUserId, Success()/Created201()/Conflict409()... trả { success, message, data }
  PagesController     // chỉ trả View(); trang quản lý chặn bằng [Authorize(Roles = ...)] (cookie Identity)
  AccountController   // MVC: /login, /register, /logout, /access-denied bằng UserManager/SignInManager
  OData/              // ODataController chỉ đọc: /odata/Tournaments, Teams, Players, Matches, Venues
Data/                 // FptuCupDbContext, Configurations/ (mỗi bảng một file), Seed/DbSeeder, Migrations/
Entities/             // 11 entity khớp docs/database/schema.sql
Common/               // ApiResponse<T>, PagedQuery, PagedResult<T>, JwtTokenFactory, VietnamTime, Enums/
Dtos/                 // DTO chỉ-đọc + phép chiếu Entity -> DTO (DtoProjections), mô hình EDM cho OData
Identity/             // AppUserStore (Identity trên bảng Users có sẵn), BcryptPasswordHasher, claims factory
Models/               // ViewModel cho form MVC (Data Annotations -> kiểm tra client + server)
TagHelpers/           // NavLinkTagHelper: <a nav-key="..."> tự thêm class is-active
Middlewares/          // ExceptionMiddleware (bắt lỗi bất ngờ -> 500 JSON)
Views/                // khung HTML (MVC), KHÔNG chứa dữ liệu; Shared/ gồm các partial (_Navbar, _Sidebar, _LoginStatus...)
wwwroot/js, css       // JS gọi /api/... bằng fetch để dựng giao diện
Program.cs, appsettings.json
```

## Quy ước code

- Đặt tên PascalCase cho class/property, camelCase cho JSON. Request class (Data Annotations) đặt lồng trong controller; response dùng DTO trong `Dtos/` (OData, sân) hoặc object ẩn danh (`Select(x => new { ... })`), KHÔNG trả Entity ra ngoài.
- Mọi response dùng chung dạng `{ "success": bool, "message": string, "data": T }` (qua `ApiControllerBase`).
- API danh sách luôn phân trang `?page=&pageSize=&search=&sortBy=` (mặc định 10, tối đa 50) bằng `PagedQuery`.
- Mã lỗi: 400 dữ liệu sai · 401 chưa đăng nhập · 403 sai quyền/không sở hữu · 404 không thấy · 409 vi phạm nghiệp vụ hoặc trùng. Trả thẳng từ controller (`Conflict409("...")`...), không trả stack trace.
- Thời gian lưu `DATETIME2` (giờ địa phương theo ví dụ trong đặc tả, vd `2026-11-07T08:00:00`). Enum lưu dạng chuỗi (`HasConversion<string>()`).
- Không hard-code secret: JWT key và connection string đặt trong `appsettings.json` / user-secrets (key dev trong repo chỉ để chạy thử).
- Phân quyền: `[Authorize(Roles = "Admin")]`. Kiểm tra quyền sở hữu (đội của mình, trận được phân công) ngay trong action.
- Thao tác nhiều bảng (kết thúc trận, sinh lịch) phải nằm trong một lần `SaveChanges`/transaction.
- Giao diện là khung HTML + JS gọi API. Đăng nhập qua form MVC (`AccountController`, Identity): cấp cookie để chặn trang theo vai trò và cấp JWT lưu `localStorage` để JS gọi `/api`. Scheme mặc định chọn theo đường dẫn: `/api`, `/odata` dùng JWT; trang MVC dùng cookie. Sửa `.cshtml` cần build lại, sửa css/js thì không.
- OData chỉ để đọc/tra cứu; mọi thao tác ghi vẫn đi qua `/api/...` để kiểm tra BR.
- Email (xác nhận email, quên mật khẩu): `AccountEmailService` sinh token bằng `UserManager`, `SmtpEmailSender` gửi qua SMTP mục `Email`.
  KHÔNG commit mật khẩu SMTP: đặt bằng `dotnet user-secrets set "Email:Password" "..." --project fptu_cup` (cùng `Email:Username`).
  Chưa có mật khẩu thì email ghi ra `fptu_cup/App_Data/mails/*.eml` (đã gitignore) – mở file để lấy link khi chạy thử.
- Logo đội upload qua `POST /api/teams/{id}/logo`, lưu file ở `wwwroot/uploads/logos/` (đã gitignore, không commit), DB chỉ lưu đường dẫn trong `Teams.LogoUrl`.
  Giao diện nạp bản đồ teamId → logo một lần mỗi trang (`App.loadLogos`, qua OData) rồi `App.avatar` / `App.teamLabel` tự hiện ảnh.
  Đội chưa có logo dùng logo mặc định `wwwroot/img/default-team-logo.png` (`App.DEFAULT_LOGO`); tên tắt (`App.teamCode`, vd CNTT, KT) luôn hiện cạnh logo để phân biệt các đội.
- Chạy ở Development tự áp migration và nạp dữ liệu mẫu (`Data/Seed/DbSeeder.cs`; tài khoản mẫu ở `docs/requirements.md`).
- Bảng xếp hạng **không lưu bảng riêng**, mà tính từ các trận `Finished` (view `vw_Standings` hoặc LINQ).

## Thể thức giải (quan trọng)

- Giải đá **sân 7**, vòng tròn **một bảng duy nhất** (giống Ngoại hạng Anh/La Liga), **không chia bảng**, không có bảng `Groups`.
- Đá 1 hoặc 2 lượt (`Tournaments.NumberOfLegs`). Với n đội, 2 lượt có 2×(n−1) vòng; lượt về đảo sân nhà/khách.
- Mỗi trận 2 hiệp × 30 phút, nghỉ 10 phút. Phút sự kiện hợp lệ: 1–60. Hòa không đá luân lưu.
- Mỗi đội có 7–14 cầu thủ. Đội hình xuất phát đúng 7 người, có đúng 1 GK, còn lại là dự bị.
- Đội hình **bị khóa khi trận bắt đầu**. Thay người lưu riêng ở bảng `Substitutions`, không sửa `MatchLineups`.
- Tính điểm: thắng 3, hòa 1, thua 0. Xếp hạng theo: Điểm → Hiệu số → Bàn thắng → Đối đầu.
- Thẻ phạt: 2 vàng trong 1 trận = đỏ; đỏ = treo 1 trận; tích lũy 3 vàng (ở 3 trận khác nhau, không tính 2 vàng đã thành đỏ) = treo 1 trận.

- Giờ đá (`KickoffTime`) lưu theo giờ địa phương Việt Nam (UTC+7); so sánh với "bây giờ" dùng `VietnamTime.Now`.
- Hạn nộp đội hình = trước giờ đá 1 ngày (BR23). Quá hạn: đội thiếu đội hình bị xử thua 0–3 (ghi `ForfeitTeamId` + `ForfeitReason`), cả hai thiếu thì hủy trận; `ForfeitWorker` tự chạy mỗi phút.

Danh sách đầy đủ 23 quy tắc nghiệp vụ (BR01–BR23) nằm ở `docs/requirements.md`, mục 8.
Khi viết code cho quy tắc nào, ghi chú mã BR tương ứng trong comment.

## Vai trò

`Admin` (ban tổ chức) · `Referee` (trọng tài) · `TeamManager` (quản lý đội) · `Guest` (không đăng nhập, chỉ đọc).

Admin là **quyền tối cao**: mọi endpoint của TeamManager/Referee đều cho cả `Admin` (`[Authorize(Roles = "TeamManager,Admin")]`),
kiểm tra sở hữu viết dạng `if (!IsAdmin && ...)`, Admin bỏ qua BR04 và hạn nộp đội hình BR23. Quy tắc toàn vẹn dữ liệu (BR01–BR03, BR06–BR22) vẫn giữ.

## Lệnh thường dùng

```bash
dotnet restore
dotnet build
dotnet ef migrations add <Ten> -p fptu_cup
dotnet ef database update -p fptu_cup
dotnet run --project fptu_cup              # Swagger: http://localhost:5100/swagger · OData: /odata/$metadata
```

## Phân công nhóm

| Thành viên | Module | FR |
| --- | --- | --- |
| HE151219 – Võ Phan Hoàng Tùng | Xác thực, Giải đấu, Sân & trọng tài; khởi tạo solution, DbContext, JWT, middleware | FR01–FR07, FR10, FR15–FR16 |
| HE186191 – Phan Quang Hiệp | Đội bóng, Cầu thủ, Đội hình, Thống kê cầu thủ; seed data, Postman | FR08–FR09, FR11–FR14, FR26–FR29 |
| HE172394 – Hoàng Ngọc Tỉnh | Lịch thi đấu, Kết quả, Bảng xếp hạng; sinh lịch vòng tròn, treo giò | FR17–FR25, FR30–FR31 |

## Khi làm việc trong repo này

- Làm theo đúng endpoint, dữ liệu vào/ra và mã lỗi ghi trong `docs/requirements.md`. Thêm endpoint mới thì cập nhật luôn tài liệu.
- Không đổi tên bảng/cột so với `schema.sql` nếu chưa hỏi lại.
- Sau khi sửa, chạy `dotnet build` và đảm bảo không có lỗi trước khi báo xong.
