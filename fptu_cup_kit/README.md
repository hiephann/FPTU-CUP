# FPTU Cup – Hệ thống Quản lý Giải bóng đá Sinh viên

Bài tập lớn cuối kỳ: ASP.NET Core Web API (.NET 8) + EF Core + SQL Server.

## Nội dung thư mục

| Đường dẫn | Dùng để |
| --- | --- |
| `CLAUDE.md` | Hướng dẫn dự án cho Claude Code (tự đọc mỗi phiên): công nghệ, cấu trúc, quy ước, thể thức giải, phân công |
| `docs/requirements.md` | Đặc tả đầy đủ: tác nhân, use case, 31 FR, 36 API, phân quyền, 11 bảng, 22 BR, design spec 15 chức năng |
| `docs/database/schema.sql` | Script SQL Server tham chiếu (11 bảng + view `vw_Standings`) |
| `docs/diagrams/` | Sơ đồ use case, luồng API, ERD: file `.puml` (sửa được) và `.png` |
| `docs/Requirement_Design_Specification_SFTMS.docx` | Báo cáo theo mẫu, tiếng Anh |
| `docs/Dac_ta_Yeu_cau_Thiet_ke_SFTMS_VI.docx` | Báo cáo theo mẫu, tiếng Việt |
| `design/*.html` | 4 mẫu giao diện tĩnh (trang chủ, chi tiết trận, dashboard, đăng nhập) – mở bằng trình duyệt |
| `design/tokens.css` | Màu, font, bo góc dùng trong mẫu giao diện |

## Bắt đầu với Claude Code trong VS Code

1. Giải nén vào thư mục `fptu_cup` (giữ `CLAUDE.md` ở thư mục gốc).
2. Mở thư mục trong VS Code, vào tab **Claude Code**.
3. Gợi ý prompt đầu tiên:
   > Đọc CLAUDE.md và docs/requirements.md, tạo solution FptuCup theo cấu trúc đề xuất: Entities, DbContext,
   > cấu hình JWT, ExceptionMiddleware, ApiResponse, Swagger có Authorize. Chưa làm controller nghiệp vụ.
4. Sau đó làm từng module theo bảng phân công, ví dụ:
   > Làm module Đội bóng & Cầu thủ (FR08–FR14) theo docs/requirements.md, kiểm tra BR01–BR04.

## Xem sơ đồ PlantUML trong VS Code

Cài extension **PlantUML** (jebbs.plantuml) cùng Java, mở file `.puml` rồi nhấn `Alt + D` để xem trước.
