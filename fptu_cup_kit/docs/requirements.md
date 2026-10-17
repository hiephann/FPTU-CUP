# Đặc tả Yêu cầu & Thiết kế – Hệ thống Quản lý Giải bóng đá Sinh viên (SFTMS)

Nhóm: Group X · HE151219 – Võ Phan Hoàng Tùng (34%) · HE186191 – Phan Quang Hiệp (33%) · HE172394 – Hoàng Ngọc Tỉnh (33%)

## I. Tổng quan

Hệ thống Quản lý Giải bóng đá Sinh viên (SFTMS) là một RESTful Web API hỗ trợ ban tổ chức cấp trường/khoa vận hành một giải bóng đá sân 7 dành cho sinh viên. Hệ thống quản lý tài khoản, giải đấu, đội bóng, cầu thủ, sân thi đấu, lịch thi đấu, đội hình xuất phát, diễn biến trận đấu (bàn thắng, thẻ phạt, thay người), bảng xếp hạng, vua phá lưới và cầu thủ bị treo giò.

Giải đấu thi đấu theo thể thức vòng tròn tính điểm với một bảng duy nhất (tương tự Ngoại hạng Anh hoặc La Liga): không chia bảng, mỗi đội gặp tất cả các đội còn lại một hoặc hai lượt (sân nhà – sân khách), đội đứng đầu bảng xếp hạng khi kết thúc mùa giải là nhà vô địch. Mỗi trận đấu kéo dài 60 phút (2 hiệp, mỗi hiệp 30 phút, nghỉ giữa hiệp 10 phút).

Dự án được phát triển bằng ASP.NET Core Web API (.NET 8), Entity Framework Core (Code First và Migrations) và Microsoft SQL Server. Xác thực sử dụng JWT Bearer Token, toàn bộ endpoint được mô tả và kiểm thử bằng Swagger/OpenAPI. Do là bài tập lớn cuối kỳ, phạm vi chỉ gồm backend API và cơ sở dữ liệu; giao diện người dùng, Docker, message queue và triển khai cloud nằm ngoài phạm vi.

## 1. Tác nhân

| # | Tác nhân | Mô tả |
|---|---|---|
| 1 | Admin | Thành viên ban tổ chức. Có toàn quyền quản lý hệ thống: tài khoản người dùng (tạo tài khoản trọng tài, khóa/mở tài khoản), giải đấu, duyệt đội, sân thi đấu, lịch thi đấu, phân công trọng tài và sửa kết quả trận đấu. |
| 2 | Team Manager | Đội trưởng hoặc quản lý đội. Đăng ký tài khoản, đăng ký đội vào giải, quản lý danh sách cầu thủ và nộp đội hình xuất phát trước mỗi trận. |
| 3 | Referee | Trọng tài được phân công bắt trận. Bắt đầu và kết thúc trận, ghi nhận diễn biến trận đấu: bàn thắng, thẻ vàng/thẻ đỏ và thay người. |
| 4 | Guest | Người dùng không cần đăng nhập. Xem giải đấu, đội bóng, cầu thủ, lịch thi đấu, chi tiết trận đấu, bảng xếp hạng, vua phá lưới và cầu thủ bị treo giò. |

## 2. Ca sử dụng

| ID | Tác nhân | Ca sử dụng | Mô tả |
|---|---|---|---|
| 01 | Admin | Đăng nhập / Đăng xuất | Đăng nhập bằng tên đăng nhập và mật khẩu để nhận JWT token. |
| 02 |  | Quản lý tài khoản | Tạo tài khoản trọng tài, xem danh sách tài khoản, khóa hoặc mở khóa tài khoản. |
| 03 |  | Quản lý giải đấu | Thêm, sửa, xóa, xem giải đấu; chọn số lượt (1 hoặc 2); chuyển trạng thái Draft → Registration → Ongoing → Finished. |
| 04 |  | Duyệt đội | Duyệt hoặc từ chối các đội đăng ký tham gia giải. |
| 05 |  | Quản lý sân | Thêm, sửa, xóa, xem sân bóng đá 7 người. |
| 06 |  | Sinh lịch thi đấu | Tự động sinh lịch vòng tròn cho toàn giải, chia theo vòng đấu, đảo sân nhà/khách ở lượt về. |
| 07 |  | Quản lý trận đấu | Tạo trận thủ công, đổi lịch, đổi sân, phân công trọng tài, hoãn hoặc hủy trận. |
| 08 |  | Sửa kết quả trận | Sửa kết quả trận đã kết thúc khi có khiếu nại; mọi thay đổi đều được ghi lại. |
| 09 |  | Xem tổng quan giải | Xem số trận, số bàn thắng và số bàn trung bình mỗi trận. |
| 10 | Team Manager | Đăng ký tài khoản | Tạo tài khoản Team Manager. |
| 11 |  | Đăng ký đội | Đăng ký đội (tên, khoa, logo, màu áo) vào giải đấu. |
| 12 |  | Quản lý cầu thủ | Thêm, sửa, xóa cầu thủ của đội mình (MSSV, họ tên, số áo, vị trí). |
| 13 |  | Nộp đội hình | Nộp đội hình xuất phát (7 cầu thủ, trong đó có 1 thủ môn) và danh sách dự bị trước giờ bóng lăn. |
| 14 | Referee | Bắt đầu / kết thúc trận | Chuyển trạng thái trận Scheduled → InProgress → Finished. |
| 15 |  | Ghi diễn biến trận | Ghi bàn thắng (cầu thủ, phút, phản lưới), thẻ vàng/thẻ đỏ và thay người. |
| 16 | Guest | Xem giải đấu & đội bóng | Xem giải đấu, đội bóng, cầu thủ và thống kê cầu thủ. |
| 17 |  | Xem lịch & chi tiết trận | Xem lịch theo vòng, đội hoặc ngày; xem trận đã kết thúc gồm đội hình, bàn thắng, thẻ phạt và thay người. |
| 18 |  | Xem bảng xếp hạng & thống kê | Xem bảng xếp hạng, vua phá lưới và cầu thủ bị treo giò. |

Sơ đồ: `docs/diagrams/uc_admin_vi.png`, `docs/diagrams/uc_others_vi.png` (nguồn `.puml` cùng thư mục).

## 3. Yêu cầu chức năng (FR)

| Mã | Module | Chức năng | Vai trò | Ưu tiên |
|---|---|---|---|---|
| FR01 | Xác thực | Đăng ký tài khoản TeamManager | Guest | Must |
| FR02 | Xác thực | Đăng nhập, nhận JWT | Tất cả | Must |
| FR03 | Xác thực | Đổi mật khẩu, xem thông tin cá nhân | Đã đăng nhập | Should |
| FR04 | Xác thực | Admin tạo tài khoản Referee, khóa/mở tài khoản | Admin | Must |
| FR05 | Giải đấu | CRUD giải đấu (tên, mùa, ngày bắt đầu/kết thúc, số đội tối đa, trạng thái) | Admin | Must |
| FR06 | Giải đấu | Chuyển trạng thái giải: Draft → Registration → Ongoing → Finished | Admin | Must |
| FR07 | Giải đấu | Xem danh sách, chi tiết giải (lọc, phân trang) | Guest | Must |
| FR08 | Đội bóng | Đăng ký đội vào giải (tên, khoa, logo URL, màu áo) | TeamManager | Must |
| FR09 | Đội bóng | Duyệt / từ chối đội đăng ký | Admin | Must |
| FR10 | Giải đấu | Cấu hình thể thức vòng tròn: 1 lượt hoặc 2 lượt (đi – về) | Admin | Must |
| FR11 | Đội bóng | Xem danh sách đội, chi tiết đội | Guest | Must |
| FR12 | Cầu thủ | Thêm / sửa / xóa cầu thủ của đội mình (MSSV, họ tên, số áo, vị trí) | TeamManager | Must |
| FR13 | Cầu thủ | Tìm kiếm cầu thủ theo tên, MSSV, đội | Guest | Should |
| FR14 | Cầu thủ | Xem thống kê cá nhân (bàn thắng, thẻ, số trận) | Guest | Should |
| FR15 | Sân & trọng tài | CRUD sân thi đấu | Admin | Must |
| FR16 | Sân & trọng tài | Xem danh sách trọng tài | Admin | Must |
| FR17 | Lịch thi đấu | Tạo trận thủ công (đội nhà, đội khách, sân, giờ, vòng) | Admin | Must |
| FR18 | Lịch thi đấu | Tự sinh lịch vòng tròn cho toàn giải, đảo sân ở lượt về | Admin | Should |
| FR19 | Lịch thi đấu | Phân công trọng tài cho trận | Admin | Must |
| FR20 | Lịch thi đấu | Đổi lịch / hoãn / hủy trận | Admin | Must |
| FR21 | Lịch thi đấu | Xem lịch theo giải, vòng, đội, ngày | Guest | Must |
| FR22 | Kết quả | Bắt đầu / kết thúc trận, cập nhật tỉ số | Referee | Must |
| FR23 | Kết quả | Ghi bàn thắng (cầu thủ, phút, phản lưới), thẻ vàng, thẻ đỏ | Referee | Must |
| FR24 | Kết quả | Admin sửa kết quả khi có khiếu nại | Admin | Should |
| FR25 | Thống kê | Bảng xếp hạng chung của giải | Guest | Must |
| FR26 | Thống kê | Vua phá lưới (top N) | Guest | Must |
| FR27 | Thống kê | Danh sách cầu thủ bị treo giò | Guest | Should |
| FR28 | Thống kê | Tổng quan giải: số trận, số bàn, trung bình bàn/trận | Admin | Should |
| FR29 | Đội hình | Nộp / sửa đội hình trước trận: 7 xuất phát (1 GK) + dự bị | TeamManager | Must |
| FR30 | Kết quả | Ghi thay người: cầu thủ ra, cầu thủ vào, phút | Referee | Must |
| FR31 | Kết quả | Xem chi tiết trận đã kết thúc: tỉ số, đội hình xuất phát, bàn thắng, thẻ, thay người | Guest | Must |

## 4. Luồng API chính

Do hệ thống là Web API không có giao diện người dùng, luồng màn hình được thể hiện bằng luồng gọi API chính trong vòng đời của một giải đấu, phân theo từng tác nhân. Xem `docs/diagrams/flow_vi.png`.

## 5. Danh sách API

Mọi endpoint dùng tiền tố /api, trả về JSON theo định dạng thống nhất { success, message, data } và hỗ trợ phân trang cho API danh sách bằng ?page=&pageSize=&search=&sortBy= (mặc định 10, tối đa 50).

| # | Chức năng | Method | Endpoint | Mô tả |
|---|---|---|---|---|
| 1 | Xác thực | POST | `/api/auth/register` | Đăng ký tài khoản Team Manager |
| 2 |  | POST | `/api/auth/login` | Đăng nhập, nhận JWT token |
| 3 |  | GET | `/api/auth/me` | Xem thông tin người dùng hiện tại |
| 4 |  | PUT | `/api/auth/change-password` | Đổi mật khẩu |
| 5 | Quản lý tài khoản | GET, POST | `/api/users` | Danh sách tài khoản / tạo tài khoản trọng tài |
| 6 |  | PATCH | `/api/users/{id}/status` | Khóa hoặc mở khóa tài khoản |
| 7 | Quản lý giải đấu | GET | `/api/tournaments` | Danh sách giải đấu (lọc, phân trang) |
| 8 |  | GET | `/api/tournaments/{id}` | Chi tiết giải đấu |
| 9 |  | POST, PUT, DELETE | `/api/tournaments/{id?}` | Tạo / sửa / xóa giải đấu (kèm số lượt) |
| 10 |  | PATCH | `/api/tournaments/{id}/status` | Chuyển trạng thái giải đấu |
| 11 |  | GET | `/api/tournaments/{id}/summary` | Thống kê tổng quan giải |
| 12 | Quản lý đội bóng | GET | `/api/tournaments/{id}/teams` | Danh sách đội của giải |
| 13 |  | POST | `/api/tournaments/{id}/teams` | Đăng ký đội |
| 14 |  | GET, PUT | `/api/teams/{id}` | Chi tiết đội / sửa thông tin đội |
| 15 |  | PATCH | `/api/teams/{id}/approval` | Duyệt hoặc từ chối đội |
| 16 | Quản lý cầu thủ | GET, POST | `/api/teams/{id}/players` | Danh sách cầu thủ / thêm cầu thủ |
| 17 |  | PUT, DELETE | `/api/players/{id}` | Sửa / xóa cầu thủ |
| 18 |  | GET | `/api/players?search=` | Tìm cầu thủ theo tên, MSSV, đội |
| 19 |  | GET | `/api/players/{id}/stats` | Thống kê cầu thủ (bàn thắng, thẻ, số trận) |
| 20 | Sân & trọng tài | GET, POST, PUT, DELETE | `/api/venues/{id?}` | Quản lý sân thi đấu |
| 21 |  | GET | `/api/referees` | Danh sách trọng tài |
| 22 | Quản lý lịch thi đấu | GET | `/api/matches?tournamentId=&round=&teamId=&date=` | Lịch thi đấu có bộ lọc |
| 23 |  | POST | `/api/matches` | Tạo trận đấu thủ công |
| 24 |  | POST | `/api/tournaments/{id}/matches/generate` | Sinh lịch thi đấu vòng tròn |
| 25 |  | PUT | `/api/matches/{id}` | Đổi lịch, đổi sân, phân công trọng tài |
| 26 | Đội hình | GET | `/api/matches/{id}/lineups` | Xem đội hình hai đội |
| 27 |  | PUT | `/api/matches/{id}/lineups/{teamId}` | Nộp / sửa đội hình của một đội |
| 28 | Kết quả trận đấu | PATCH | `/api/matches/{id}/status` | Bắt đầu, kết thúc, hoãn hoặc hủy trận |
| 29 |  | POST, DELETE | `/api/matches/{id}/goals` | Ghi / xóa bàn thắng |
| 30 |  | POST, DELETE | `/api/matches/{id}/cards` | Ghi / xóa thẻ phạt |
| 31 |  | POST, DELETE | `/api/matches/{id}/substitutions` | Ghi / xóa thay người |
| 32 |  | PUT | `/api/matches/{id}/result` | Sửa kết quả trận đã kết thúc |
| 33 |  | GET | `/api/matches/{id}` | Chi tiết trận: tỉ số, đội hình xuất phát, bàn thắng, thẻ phạt, thay người |
| 34 | Thống kê | GET | `/api/tournaments/{id}/standings` | Bảng xếp hạng |
| 35 |  | GET | `/api/tournaments/{id}/top-scorers?top=10` | Vua phá lưới |
| 36 |  | GET | `/api/tournaments/{id}/suspensions` | Cầu thủ bị treo giò |

## 6. Phân quyền API

Phân quyền được cài đặt bằng claim role trong JWT và thuộc tính [Authorize(Roles = "...")]. Các quy tắc sở hữu (đội của mình, trận được phân công) được kiểm tra ở tầng Service.

| # | Nhóm API | Admin | Referee | Team Manager | Guest |
|---|---|---|---|---|---|
| 1 | Đăng nhập / đăng ký / đổi mật khẩu | X | X | X | Đăng ký, đăng nhập |
| 2 | Quản lý tài khoản | X |  |  |  |
| 3 | Xem giải đấu, đội bóng, cầu thủ | X | X | X | X |
| 4 | Tạo / sửa / xóa giải đấu, chuyển trạng thái | X |  |  |  |
| 5 | Đăng ký đội, sửa thông tin đội mình |  |  | X |  |
| 6 | Duyệt / từ chối đội | X |  |  |  |
| 7 | Thêm / sửa / xóa cầu thủ của đội mình |  |  | X |  |
| 8 | Quản lý sân, xem danh sách trọng tài | X |  |  |  |
| 9 | Xem lịch thi đấu, chi tiết trận, đội hình | X | X | X | X |
| 10 | Tạo trận, sinh lịch, đổi lịch | X |  |  |  |
| 11 | Nộp đội hình của đội mình |  |  | X |  |
| 12 | Bắt đầu / kết thúc trận, ghi bàn thắng, thẻ, thay người |  | Trận được phân công |  |  |
| 13 | Hoãn / hủy trận, sửa kết quả | X |  |  |  |
| 14 | Xem bảng xếp hạng, vua phá lưới, treo giò | X | X | X | X |
| 15 | Tổng quan giải | X |  |  |  |

## 7. Cơ sở dữ liệu

ERD: `docs/diagrams/erd.png`. Script: `docs/database/schema.sql`.

| STT | Bảng | Mô tả | Khóa & ràng buộc |
|---|---|---|---|
| 01 | Users | Lưu tài khoản người dùng gồm Id, Username, PasswordHash, FullName, Email, Role, IsActive, CreatedAt. Role ∈ {Admin, Referee, TeamManager}. | Khóa chính: Id. Duy nhất: Username, Email. |
| 02 | Tournaments | Lưu giải đấu gồm Name, Season, StartDate, EndDate, MaxTeams, NumberOfLegs (1 hoặc 2), Status (Draft, Registration, Ongoing, Finished), CreatedBy. | Khóa chính: Id. Khóa ngoại: CreatedBy → Users. Kiểm tra: EndDate ≥ StartDate. |
| 03 | Teams | Lưu đội đăng ký tham gia giải gồm Name, Faculty, LogoUrl, KitColor, Status (Pending, Approved, Rejected). Không có bảng Groups vì giải chỉ có một bảng xếp hạng. | Khóa chính: Id. Khóa ngoại: TournamentId → Tournaments, ManagerId → Users. Duy nhất: (TournamentId, Name). |
| 04 | Players | Lưu cầu thủ của đội gồm StudentCode, FullName, ShirtNumber, Position (GK, DF, MF, FW), DateOfBirth. Mỗi đội có 7–14 cầu thủ. | Khóa chính: Id. Khóa ngoại: TeamId → Teams. Duy nhất: (TeamId, ShirtNumber). |
| 05 | Venues | Lưu sân bóng đá 7 người gồm Name, Address, Capacity. | Khóa chính: Id. |
| 06 | Matches | Lưu trận đấu gồm Round, KickoffTime, HomeScore, AwayScore, Status (Scheduled, InProgress, Finished, Postponed, Cancelled), UpdatedBy, UpdatedAt. | Khóa chính: Id. Khóa ngoại: TournamentId, HomeTeamId, AwayTeamId, VenueId, RefereeId, UpdatedBy. Kiểm tra: HomeTeamId ≠ AwayTeamId. |
| 07 | MatchLineups | Lưu đội hình mỗi đội nộp cho một trận; IsStarter = 1 với 7 cầu thủ xuất phát, 0 với cầu thủ dự bị. Đội hình bị khóa khi trận bắt đầu. | Khóa chính: Id. Khóa ngoại: MatchId, TeamId, PlayerId. Duy nhất: (MatchId, PlayerId). |
| 08 | Substitutions | Lưu các lần thay người gồm PlayerOutId, PlayerInId, Minute (1–60). | Khóa chính: Id. Khóa ngoại: MatchId, TeamId, PlayerOutId, PlayerInId → Players. |
| 09 | Goals | Lưu bàn thắng gồm PlayerId, TeamId (đội được tính bàn thắng), Minute (1–60), IsOwnGoal. | Khóa chính: Id. Khóa ngoại: MatchId, PlayerId, TeamId. |
| 10 | Cards | Lưu thẻ phạt gồm PlayerId, CardType (Yellow, Red), Minute (1–60). | Khóa chính: Id. Khóa ngoại: MatchId, PlayerId. |
| 11 | Suspensions | Lưu án treo giò được sinh tự động khi trận kết thúc, gồm Reason và MatchesRemaining. | Khóa chính: Id. Khóa ngoại: PlayerId, TournamentId. |

Quy ước: khóa chính kiểu INT IDENTITY; thời gian lưu kiểu DATETIME2 theo UTC; bảng xếp hạng không lưu mà tính từ các trận đã kết thúc qua view vw_Standings. Tạo index cho mọi khóa ngoại và cho Players.StudentCode, Matches.KickoffTime.

## 8. Quy tắc nghiệp vụ (BR)

| Mã | Quy tắc |
|---|---|
| BR01 | Mỗi đội có từ 7 đến 14 cầu thủ; đội chưa đủ 7 người không được duyệt. |
| BR02 | Một sinh viên (MSSV) chỉ thuộc một đội trong cùng một giải. |
| BR03 | Số áo là duy nhất trong một đội. |
| BR04 | Chỉ đăng ký đội và sửa danh sách cầu thủ khi giải ở trạng thái Registration. |
| BR05 | Số đội được duyệt không vượt quá số đội tối đa (MaxTeams) của giải. |
| BR06 | Hai đội trong một trận phải khác nhau và cùng thuộc giải đó. |
| BR07 | Một sân không có hai trận trùng khung giờ (mỗi trận 60 phút + 10 phút nghỉ). |
| BR08 | Một đội không đá hai trận trong cùng một ngày. |
| BR09 | Trọng tài chỉ cập nhật trận mình được phân công và khi trận ở trạng thái InProgress. |
| BR10 | Cầu thủ ghi bàn / nhận thẻ phải thuộc một trong hai đội của trận. Phút từ 1 đến 60. |
| BR11 | Tỉ số trận phải khớp với số bàn thắng đã ghi (kể cả phản lưới nhà). |
| BR12 | Tính điểm: thắng 3, hòa 1, thua 0. Trận hòa giữ nguyên kết quả, không đá hiệp phụ hay luân lưu. |
| BR13 | Xếp hạng theo thứ tự: Điểm → Hiệu số → Bàn thắng → Đối đầu trực tiếp. |
| BR14 | 2 thẻ vàng trong một trận = 1 thẻ đỏ. Thẻ đỏ: treo giò 1 trận kế tiếp. Tích lũy 3 thẻ vàng: treo giò 1 trận. |
| BR15 | Trận đã Finished chỉ Admin được sửa; mọi thay đổi ghi lại người sửa và thời điểm. |
| BR16 | Trạng thái trận: Scheduled → InProgress → Finished, hoặc Scheduled → Postponed / Cancelled. |
| BR17 | Vòng tròn một bảng duy nhất: mỗi đội gặp tất cả đội còn lại. Đá 2 lượt thì mỗi cặp gặp nhau 2 lần, đảo sân nhà/khách; với n đội có 2×(n−1) vòng đấu. Đội đứng đầu bảng khi kết thúc giải là vô địch. |
| BR18 | Mỗi trận gồm 2 hiệp, mỗi hiệp 30 phút, nghỉ giữa hiệp 10 phút (tối đa 60 phút thi đấu). |
| BR19 | Đội hình xuất phát đúng 7 cầu thủ, trong đó đúng 1 thủ môn; các cầu thủ còn lại (tối đa 7) là dự bị. Cầu thủ đang bị treo giò không được đăng ký vào đội hình. |
| BR20 | Đội hình phải nộp trước giờ bóng lăn; trận chỉ được bắt đầu khi cả 2 đội đã có đội hình. Khi trận chuyển InProgress, đội hình bị khóa và không sửa, kể cả khi có thay người. |
| BR21 | Thay người: cầu thủ ra phải đang trên sân, cầu thủ vào phải là dự bị chưa vào sân; cầu thủ đã bị thay ra không được vào lại; cầu thủ nhận thẻ đỏ không được thay thế. |
| BR22 | Bàn thắng chỉ được ghi cho cầu thủ đang có mặt trên sân tại phút đó (tính từ đội hình xuất phát và các lần thay người). |

## 9. Đặc tả thiết kế API

### 9.1. Người dùng đăng nhập bằng tài khoản được cấp

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/auth/login` |
| Method | POST |
| Phân quyền | Không cần đăng nhập |
| Body | JSON: username, password |

Request body:

```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

| Mã trạng thái | Nội dung |
|---|---|
| OK - 200 | JSON gồm accessToken, expiresIn và thông tin người dùng (id, fullName, role) |
| BadRequest - 400 | Username hoặc password để trống |
| Unauthorized - 401 | Sai tên đăng nhập hoặc mật khẩu |
| Forbidden - 403 | Tài khoản đang bị khóa (IsActive = false) |

Response (OK - 200):

```json
{
  "success": true,
  "message": "Đăng nhập thành công",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "user": {
      "id": 1,
      "fullName": "Tournament Admin",
      "role": "Admin"
    }
  }
}
```

Xử lý & kiểm tra:

- Tìm người dùng theo Username; kiểm tra mật khẩu với PasswordHash bằng BCrypt.
- Từ chối tài khoản đang bị khóa (IsActive = false).
- Sinh JWT chứa các claim sub (id người dùng), name và role; token hết hạn sau 60 phút.
- Client gửi token trong header Authorization: Bearer <token> khi gọi các endpoint cần xác thực.

### 9.2. Team Manager đăng ký tài khoản

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/auth/register` |
| Method | POST |
| Phân quyền | Không cần đăng nhập |
| Body | JSON: username, password, fullName, email |

Request body:

```json
{
  "username": "manager_cntt",
  "password": "Manager@123",
  "fullName": "Nguyen Van A",
  "email": "anv@fpt.edu.vn"
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON tài khoản vừa tạo (không có mật khẩu) |
| BadRequest - 400 | Dữ liệu không hợp lệ (thiếu trường, email sai định dạng, mật khẩu yếu) |
| Conflict - 409 | Username hoặc email đã tồn tại |

Response (Created - 201):

```json
{
  "success": true,
  "message": "Tạo tài khoản thành công",
  "data": {
    "id": 12,
    "username": "manager_cntt",
    "fullName": "Nguyen Van A",
    "email": "anv@fpt.edu.vn",
    "role": "TeamManager"
  }
}
```

Xử lý & kiểm tra:

- Vai trò luôn là TeamManager; tài khoản Admin và Referee không thể tự đăng ký.
- Mật khẩu tối thiểu 8 ký tự và được băm bằng BCrypt trước khi lưu.
- Username và Email là duy nhất.

### 9.3. Admin tạo giải đấu

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/tournaments` |
| Method | POST |
| Phân quyền | Bearer token – vai trò Admin |
| Body | JSON: name, season, startDate, endDate, maxTeams, numberOfLegs |

Request body:

```json
{
  "name": "FPTU Student Football Cup 2026",
  "season": "Fall 2026",
  "startDate": "2026-11-01",
  "endDate": "2026-12-20",
  "maxTeams": 8,
  "numberOfLegs": 2
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON giải đấu vừa tạo với trạng thái Draft |
| BadRequest - 400 | Dữ liệu không hợp lệ (EndDate < StartDate, numberOfLegs khác 1 hoặc 2, maxTeams < 2) |
| Unauthorized - 401 | Thiếu hoặc sai token |
| Forbidden - 403 | Người dùng không phải Admin |

Response (Created - 201):

```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "FPTU Student Football Cup 2026",
    "season": "Fall 2026",
    "startDate": "2026-11-01",
    "endDate": "2026-12-20",
    "maxTeams": 8,
    "numberOfLegs": 2,
    "status": "Draft"
  }
}
```

Xử lý & kiểm tra:

- Giải đấu mới luôn ở trạng thái Draft.
- Trạng thái được chuyển qua PATCH /api/tournaments/{id}/status theo thứ tự Draft → Registration → Ongoing → Finished (BR04).
- Chỉ được xóa giải khi đang ở trạng thái Draft.

### 9.4. Team Manager đăng ký đội vào giải đấu

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/tournaments/{id}/teams` |
| Method | POST |
| Phân quyền | Bearer token – vai trò TeamManager |
| Body | JSON: name, faculty, logoUrl, kitColor |

Request body:

```json
{
  "name": "CNTT FC",
  "faculty": "Information Technology",
  "logoUrl": "https://example.com/logo/cntt.png",
  "kitColor": "Orange"
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON đội với trạng thái Pending |
| BadRequest - 400 | Dữ liệu không hợp lệ |
| NotFound - 404 | Giải đấu không tồn tại |
| Conflict - 409 | Giải không ở trạng thái Registration hoặc tên đội đã tồn tại |

Response (Created - 201):

```json
{
  "success": true,
  "data": {
    "id": 3,
    "tournamentId": 1,
    "name": "CNTT FC",
    "faculty": "Information Technology",
    "managerId": 12,
    "status": "Pending"
  }
}
```

Xử lý & kiểm tra:

- ManagerId lấy từ JWT của người dùng hiện tại.
- Chỉ được đăng ký khi giải ở trạng thái Registration (BR04).
- Tên đội là duy nhất trong một giải.

### 9.5. Team Manager thêm cầu thủ vào đội

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/teams/{id}/players` |
| Method | POST |
| Phân quyền | Bearer token – vai trò TeamManager (đội của mình) |
| Body | JSON: studentCode, fullName, shirtNumber, position, dateOfBirth |

Request body:

```json
{
  "studentCode": "HE190123",
  "fullName": "Tran Van B",
  "shirtNumber": 10,
  "position": "FW",
  "dateOfBirth": "2004-05-20"
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON cầu thủ vừa tạo |
| BadRequest - 400 | Dữ liệu không hợp lệ (vị trí không thuộc GK, DF, MF, FW) |
| Forbidden - 403 | Đội không do người dùng hiện tại quản lý |
| Conflict - 409 | Đội đã đủ 14 cầu thủ, trùng số áo, hoặc sinh viên đã thuộc đội khác trong giải |

Response (Created - 201):

```json
{
  "success": true,
  "data": {
    "id": 25,
    "teamId": 3,
    "studentCode": "HE190123",
    "fullName": "Tran Van B",
    "shirtNumber": 10,
    "position": "FW"
  }
}
```

Xử lý & kiểm tra:

- Tối đa 14 cầu thủ mỗi đội (BR01).
- Số áo duy nhất trong đội (BR03); MSSV duy nhất trong giải (BR02).
- Chỉ được thêm khi giải ở trạng thái Registration (BR04).

### 9.6. Admin duyệt hoặc từ chối đội

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/teams/{id}/approval` |
| Method | PATCH |
| Phân quyền | Bearer token – vai trò Admin |
| Body | JSON: approved (bool), reason |

Request body:

```json
{
  "approved": true,
  "reason": null
}
```

| Mã trạng thái | Nội dung |
|---|---|
| OK - 200 | JSON đội với trạng thái mới |
| NotFound - 404 | Đội không tồn tại |
| Conflict - 409 | Đội chưa đủ 7 cầu thủ, hoặc giải đã đủ số đội tối đa được duyệt |

Response (OK - 200):

```json
{
  "success": true,
  "data": {
    "id": 3,
    "name": "CNTT FC",
    "status": "Approved"
  }
}
```

Xử lý & kiểm tra:

- Chỉ duyệt đội có từ 7 đến 14 cầu thủ (BR01).
- Số đội được duyệt không vượt quá MaxTeams (BR05).
- Đội bị từ chối vẫn giữ dữ liệu nhưng không được xếp lịch.

### 9.7. Admin sinh lịch thi đấu vòng tròn

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/tournaments/{id}/matches/generate` |
| Method | POST |
| Phân quyền | Bearer token – vai trò Admin |
| Body | JSON: firstKickoff, daysBetweenRounds, kickoffTimes[], venueIds[] |

Request body:

```json
{
  "firstKickoff": "2026-11-07",
  "daysBetweenRounds": 7,
  "kickoffTimes": [
    "08:00",
    "09:10",
    "10:20",
    "11:30"
  ],
  "venueIds": [
    1,
    2
  ]
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON gồm số vòng đấu và danh sách trận đã sinh |
| BadRequest - 400 | Tham số không hợp lệ |
| Conflict - 409 | Giải không ở trạng thái Ongoing, có ít hơn 2 đội được duyệt, hoặc lịch đã tồn tại |

Response (Created - 201):

```json
{
  "success": true,
  "data": {
    "totalRounds": 14,
    "totalMatches": 56,
    "matches": [
      {
        "id": 1,
        "round": 1,
        "homeTeam": "CNTT FC",
        "awayTeam": "Kinh te FC",
        "venue": "Pitch A",
        "kickoffTime": "2026-11-07T08:00:00"
      }
    ]
  }
}
```

Xử lý & kiểm tra:

- Dùng thuật toán xoay vòng (circle method) cho các đội đã duyệt; nếu số đội lẻ thì thêm một đội 'nghỉ' (bye).
- 1 lượt: n−1 vòng; 2 lượt: 2×(n−1) vòng, lượt về lặp lại lượt đi và đảo sân nhà/khách (BR17).
- Các trận trong một vòng được xếp vào các khung giờ và sân đã chọn, mỗi trận chiếm 70 phút (BR07) và mỗi đội tối đa một trận mỗi ngày (BR08).
- Toàn bộ trận được lưu trong một transaction với trạng thái Scheduled.

### 9.8. Team Manager nộp đội hình xuất phát

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/matches/{id}/lineups/{teamId}` |
| Method | PUT |
| Phân quyền | Bearer token – vai trò TeamManager (đội của mình) |
| Body | JSON: starters[] (7 id cầu thủ), substitutes[] (id cầu thủ) |

Request body:

```json
{
  "starters": [
    21,
    22,
    23,
    24,
    25,
    26,
    27
  ],
  "substitutes": [
    28,
    29,
    30
  ]
}
```

| Mã trạng thái | Nội dung |
|---|---|
| OK - 200 | JSON đội hình đã lưu |
| BadRequest - 400 | Không đúng 7 cầu thủ xuất phát, không đúng 1 thủ môn, hoặc có cầu thủ không thuộc đội |
| Forbidden - 403 | Đội không do người dùng hiện tại quản lý |
| Conflict - 409 | Trận đã bắt đầu, hoặc có cầu thủ đang bị treo giò |

Response (OK - 200):

```json
{
  "success": true,
  "data": {
    "matchId": 12,
    "teamId": 3,
    "starters": [
      {
        "playerId": 21,
        "shirtNumber": 1,
        "fullName": "Le Van C",
        "position": "GK"
      }
    ],
    "substitutes": [
      {
        "playerId": 28,
        "shirtNumber": 12,
        "fullName": "Pham Van D",
        "position": "MF"
      }
    ]
  }
}
```

Xử lý & kiểm tra:

- Đúng 7 cầu thủ xuất phát, trong đó đúng 1 thủ môn; còn lại là dự bị (BR19).
- Cầu thủ phải thuộc đội và không bị treo giò.
- PUT thay thế đội hình cũ trong MatchLineups; chỉ được nộp khi trận ở trạng thái Scheduled. Khi trận chuyển InProgress, đội hình bị khóa (BR20).

### 9.9. Trọng tài bắt đầu hoặc kết thúc trận đấu

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/matches/{id}/status` |
| Method | PATCH |
| Phân quyền | Bearer token – vai trò Referee (được phân công) / Admin |
| Body | JSON: status (InProgress, Finished, Postponed, Cancelled) |

Request body:

```json
{
  "status": "Finished"
}
```

| Mã trạng thái | Nội dung |
|---|---|
| OK - 200 | JSON trận đấu với trạng thái mới và tỉ số cuối cùng |
| Forbidden - 403 | Trọng tài không được phân công trận này |
| Conflict - 409 | Chuyển trạng thái không hợp lệ, hoặc thiếu đội hình khi bắt đầu trận |

Response (OK - 200):

```json
{
  "success": true,
  "data": {
    "id": 12,
    "status": "Finished",
    "homeScore": 2,
    "awayScore": 1
  }
}
```

Xử lý & kiểm tra:

- Các chuyển trạng thái hợp lệ: Scheduled → InProgress → Finished; Scheduled → Postponed / Cancelled (chỉ Admin) (BR16).
- Chỉ bắt đầu được khi cả hai đội đã nộp đội hình (BR20).
- Kết thúc trận chạy trong một transaction: tính lại tỉ số từ bàn thắng (BR11), sinh án treo giò cho thẻ đỏ và mỗi 3 thẻ vàng tích lũy, giảm MatchesRemaining của các án treo giò hiện có của hai đội (BR14).

### 9.10. Trọng tài ghi bàn thắng

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/matches/{id}/goals` |
| Method | POST |
| Phân quyền | Bearer token – vai trò Referee (được phân công) |
| Body | JSON: playerId, minute, isOwnGoal |

Request body:

```json
{
  "playerId": 25,
  "minute": 12,
  "isOwnGoal": false
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON bàn thắng và tỉ số đã cập nhật |
| BadRequest - 400 | Phút không nằm trong khoảng 1–60 |
| Conflict - 409 | Trận không ở trạng thái InProgress, hoặc cầu thủ không có mặt trên sân tại phút đó |

Response (Created - 201):

```json
{
  "success": true,
  "data": {
    "goalId": 40,
    "matchId": 12,
    "playerId": 25,
    "teamId": 3,
    "minute": 12,
    "isOwnGoal": false,
    "homeScore": 1,
    "awayScore": 0
  }
}
```

Xử lý & kiểm tra:

- Cầu thủ phải đang trên sân tại phút đó, tính từ đội hình xuất phát và các lần thay người (BR22).
- TeamId là đội được tính bàn thắng: đội của cầu thủ, hoặc đội đối phương nếu là phản lưới nhà.
- HomeScore / AwayScore được cập nhật ngay để tỉ số luôn bằng số bàn thắng (BR11).
- DELETE /api/matches/{id}/goals/{goalId} dùng để xóa bàn thắng ghi nhầm.

### 9.11. Trọng tài ghi thẻ phạt

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/matches/{id}/cards` |
| Method | POST |
| Phân quyền | Bearer token – vai trò Referee (được phân công) |
| Body | JSON: playerId, cardType (Yellow, Red), minute |

Request body:

```json
{
  "playerId": 31,
  "cardType": "Yellow",
  "minute": 25
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON thẻ phạt; nếu là thẻ vàng thứ hai của cầu thủ thì tự động thêm thẻ đỏ |
| BadRequest - 400 | Loại thẻ hoặc phút không hợp lệ |
| Conflict - 409 | Trận không ở trạng thái InProgress, hoặc cầu thủ đã bị truất quyền thi đấu |

Response (Created - 201):

```json
{
  "success": true,
  "data": {
    "cardId": 7,
    "matchId": 12,
    "playerId": 31,
    "cardType": "Yellow",
    "minute": 25,
    "sentOff": false
  }
}
```

Xử lý & kiểm tra:

- Cầu thủ phải thuộc một trong hai đội của trận (BR10).
- Thẻ vàng thứ hai trong cùng trận tự động sinh một thẻ đỏ (BR14).
- Án treo giò được sinh khi kết thúc trận, không phải khi ghi thẻ.

### 9.12. Trọng tài ghi thay người

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/matches/{id}/substitutions` |
| Method | POST |
| Phân quyền | Bearer token – vai trò Referee (được phân công) |
| Body | JSON: teamId, playerOutId, playerInId, minute |

Request body:

```json
{
  "teamId": 3,
  "playerOutId": 26,
  "playerInId": 28,
  "minute": 40
}
```

| Mã trạng thái | Nội dung |
|---|---|
| Created - 201 | JSON lần thay người |
| BadRequest - 400 | Phút không nằm trong khoảng 1–60, hoặc playerOutId = playerInId |
| Conflict - 409 | Cầu thủ ra không ở trên sân, cầu thủ vào không phải dự bị chưa vào sân, hoặc cầu thủ ra đã bị truất quyền thi đấu |

Response (Created - 201):

```json
{
  "success": true,
  "data": {
    "id": 9,
    "matchId": 12,
    "teamId": 3,
    "playerOut": "Nguyen Van E",
    "playerIn": "Pham Van D",
    "minute": 40
  }
}
```

Xử lý & kiểm tra:

- Thay người chỉ lưu vào bảng Substitutions; đội hình xuất phát trong MatchLineups không bao giờ bị sửa (BR20).
- Cầu thủ đã bị thay ra không được vào lại; cầu thủ nhận thẻ đỏ không được thay thế (BR21).

### 9.13. Khách xem chi tiết trận đấu đã kết thúc

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/matches/{id}` |
| Method | GET |
| Phân quyền | Không cần đăng nhập |
| Body | Không có |

| Mã trạng thái | Nội dung |
|---|---|
| OK - 200 | JSON trận đấu: tỉ số, sân, trọng tài, đội hình xuất phát hai đội và diễn biến bàn thắng, thẻ phạt, thay người theo thời gian |
| NotFound - 404 | Trận đấu không tồn tại |

Response (OK - 200):

```json
{
  "success": true,
  "data": {
    "id": 12,
    "round": 3,
    "status": "Finished",
    "kickoffTime": "2026-11-07T08:00:00",
    "venue": "Pitch A",
    "referee": "Hoang Van R",
    "homeTeam": {
      "id": 3,
      "name": "CNTT FC",
      "score": 2,
      "startingLineup": [
        {
          "playerId": 21,
          "shirtNumber": 1,
          "name": "Le Van C",
          "position": "GK"
        }
      ],
      "substitutes": [
        {
          "playerId": 28,
          "shirtNumber": 12,
          "name": "Pham Van D"
        }
      ]
    },
    "awayTeam": {
      "id": 4,
      "name": "Kinh te FC",
      "score": 1,
      "startingLineup": [],
      "substitutes": []
    },
    "events": [
      {
        "minute": 12,
        "type": "Goal",
        "teamId": 3,
        "player": "Tran Van B",
        "isOwnGoal": false
      },
      {
        "minute": 25,
        "type": "YellowCard",
        "teamId": 4,
        "player": "Vu Van F"
      },
      {
        "minute": 40,
        "type": "Substitution",
        "teamId": 3,
        "playerOut": "Nguyen Van E",
        "playerIn": "Pham Van D"
      }
    ]
  }
}
```

Xử lý & kiểm tra:

- startingLineup lấy từ MatchLineups với IsStarter = 1 và không thay đổi sau khi thay người.
- events gộp Goals, Cards và Substitutions, sắp xếp theo phút.
- Dữ liệu được truy vấn bằng EF Core (Include/Select projection) vào DTO trong một câu truy vấn.

### 9.14. Khách xem bảng xếp hạng

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/tournaments/{id}/standings` |
| Method | GET |
| Phân quyền | Không cần đăng nhập |
| Body | Không có |

| Mã trạng thái | Nội dung |
|---|---|
| OK - 200 | Mảng JSON các dòng bảng xếp hạng theo thứ hạng |
| NotFound - 404 | Giải đấu không tồn tại |

Response (OK - 200):

```json
{
  "success": true,
  "data": [
    {
      "rank": 1,
      "teamId": 3,
      "team": "CNTT FC",
      "played": 5,
      "won": 4,
      "drawn": 1,
      "lost": 0,
      "goalsFor": 14,
      "goalsAgainst": 4,
      "goalDifference": 10,
      "points": 13
    },
    {
      "rank": 2,
      "teamId": 4,
      "team": "Kinh te FC",
      "played": 5,
      "won": 3,
      "drawn": 1,
      "lost": 1,
      "goalsFor": 9,
      "goalsAgainst": 6,
      "goalDifference": 3,
      "points": 10
    }
  ]
}
```

Xử lý & kiểm tra:

- Bảng xếp hạng được tính từ các trận Finished (view vw_Standings), không lưu thành bảng riêng.
- Tính điểm: thắng 3, hòa 1, thua 0 (BR12).
- Xếp hạng: điểm → hiệu số → bàn thắng → đối đầu trực tiếp (BR13).
- Hiển thị tất cả đội đã duyệt, kể cả đội chưa đá trận nào.

### 9.15. Khách xem danh sách vua phá lưới

| Thuộc tính | Giá trị |
|---|---|
| URL | `/api/tournaments/{id}/top-scorers?top=10` |
| Method | GET |
| Phân quyền | Không cần đăng nhập |
| Body | Không có |

| Mã trạng thái | Nội dung |
|---|---|
| OK - 200 | Mảng JSON cầu thủ sắp xếp theo số bàn thắng |
| NotFound - 404 | Giải đấu không tồn tại |

Response (OK - 200):

```json
{
  "success": true,
  "data": [
    {
      "rank": 1,
      "playerId": 25,
      "player": "Tran Van B",
      "team": "CNTT FC",
      "goals": 7
    },
    {
      "rank": 2,
      "playerId": 44,
      "player": "Do Van G",
      "team": "Kinh te FC",
      "goals": 5
    }
  ]
}
```

Xử lý & kiểm tra:

- Đếm các bàn thắng có IsOwnGoal = false trong các trận Finished của giải.
- Cầu thủ bằng số bàn được xếp theo số trận ít hơn, sau đó theo tên.
- Tham số top mặc định 10, tối đa 50.
