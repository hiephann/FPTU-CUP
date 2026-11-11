using System.ComponentModel.DataAnnotations;
using fptu_cup.Common;
using fptu_cup.Common.Enums;
using fptu_cup.Data;
using fptu_cup.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Controllers
{
    /// <summary>FR08, FR09, FR11: đăng ký đội, duyệt đội, xem đội.</summary>
    [ApiController]
    public class TeamsController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        private readonly IWebHostEnvironment _env;

        public TeamsController(FptuCupDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /// <summary>Admin thấy mọi đội; người khác chỉ thấy đội đã duyệt (và đội của chính mình).</summary>
        [HttpGet("api/tournaments/{id:int}/teams")]
        public async Task<IActionResult> GetByTournament(int id, [FromQuery] PagedQuery query, TeamStatus? status)
        {
            if (!await _db.Tournaments.AnyAsync(t => t.Id == id)) return NotFound404("Giải đấu không tồn tại");

            var q = _db.Teams.Where(t => t.TournamentId == id);
            if (!IsAdmin)
            {
                var me = CurrentUserId;
                q = q.Where(t => t.Status == TeamStatus.Approved || t.ManagerId == me);
            }
            if (status != null) q = q.Where(t => t.Status == status);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(t => t.Name.Contains(s) || (t.Faculty != null && t.Faculty.Contains(s)));
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(t => t.Name).Skip(query.Skip).Take(query.PageSize)
                .Select(t => new
                {
                    t.Id, t.TournamentId, t.Name, t.Faculty, t.LogoUrl, t.KitColor, t.Status,
                    t.ManagerId,
                    managerName = t.Manager!.FullName,
                    playerCount = t.Players.Count,
                })
                .ToListAsync();
            return SuccessPaged(items, total, query);
        }

        /// <summary>
        /// BR04: TeamManager chỉ đăng ký khi giải đang ở trạng thái Registration.
        /// Admin (quyền tối cao) đăng ký được ở mọi trạng thái và có thể chỉ định managerId.
        /// </summary>
        [Authorize(Roles = "TeamManager,Admin")]
        [HttpPost("api/tournaments/{id:int}/teams")]
        public async Task<IActionResult> Register(int id, TeamRequest req)
        {
            var tournament = await _db.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound404("Giải đấu không tồn tại");
            if (!IsAdmin && tournament.Status != TournamentStatus.Registration) return Conflict409("Giải không ở trạng thái Registration (BR04)");

            var (managerId, managerError) = await ResolveManager(req.ManagerId, CurrentUserId);
            if (managerError != null) return managerError;
            if (!IsValidLogoUrl(req.LogoUrl)) return BadRequest400(LogoUrlMessage);

            var name = req.Name.Trim();
            if (await _db.Teams.AnyAsync(t => t.TournamentId == id && t.Name == name))
                return Conflict409("Tên đội đã tồn tại trong giải");

            var team = new Team
            {
                TournamentId = id,
                ManagerId = managerId,
                Name = name,
                Faculty = req.Faculty?.Trim(),
                LogoUrl = req.LogoUrl?.Trim(),
                KitColor = req.KitColor?.Trim(),
                Status = TeamStatus.Pending,
            };
            _db.Teams.Add(team);
            await _db.SaveChangesAsync();
            return Created201(new { team.Id, team.TournamentId, team.Name, team.Faculty, team.ManagerId, team.Status }, "Đăng ký đội thành công, chờ ban tổ chức duyệt");
        }

        /// <summary>Các đội do user đang đăng nhập quản lý; Admin nhận về mọi đội (để quản lý thay).</summary>
        [Authorize(Roles = "TeamManager,Admin")]
        [HttpGet("api/teams/mine")]
        public async Task<IActionResult> Mine()
        {
            var me = CurrentUserId;
            var all = IsAdmin;
            var items = await _db.Teams.Where(t => all || t.ManagerId == me)
                .OrderByDescending(t => t.Tournament!.StartDate)
                .Select(t => new
                {
                    t.Id, t.TournamentId, tournament = t.Tournament!.Name, tournamentStatus = t.Tournament.Status,
                    t.Name, t.Faculty, t.LogoUrl, t.KitColor, t.Status,
                    playerCount = t.Players.Count,
                })
                .ToListAsync();
            return Success(items);
        }

        [HttpGet("api/teams/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var team = await _db.Teams.Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id, t.TournamentId, tournament = t.Tournament!.Name, tournamentStatus = t.Tournament.Status,
                    t.Name, t.Faculty, t.LogoUrl, t.KitColor, t.Status, t.ManagerId,
                    managerName = t.Manager!.FullName,
                    playerCount = t.Players.Count,
                })
                .FirstOrDefaultAsync();
            return team == null ? NotFound404("Đội không tồn tại") : Success(team);
        }

        /// <summary>TeamManager sửa đội của mình; Admin sửa được mọi đội và đổi được người quản lý (managerId).</summary>
        [Authorize(Roles = "TeamManager,Admin")]
        [HttpPut("api/teams/{id:int}")]
        public async Task<IActionResult> Update(int id, TeamRequest req)
        {
            var team = await _db.Teams.FindAsync(id);
            if (team == null) return NotFound404("Đội không tồn tại");
            if (!IsAdmin && team.ManagerId != CurrentUserId) return Forbidden403("Đội không do bạn quản lý");

            var (managerId, managerError) = await ResolveManager(req.ManagerId, team.ManagerId);
            if (managerError != null) return managerError;
            if (!IsValidLogoUrl(req.LogoUrl)) return BadRequest400(LogoUrlMessage);
            team.ManagerId = managerId;

            var name = req.Name.Trim();
            if (await _db.Teams.AnyAsync(t => t.TournamentId == team.TournamentId && t.Name == name && t.Id != id))
                return Conflict409("Tên đội đã tồn tại trong giải");

            team.Name = name;
            team.Faculty = req.Faculty?.Trim();
            var newLogo = string.IsNullOrWhiteSpace(req.LogoUrl) ? null : req.LogoUrl.Trim();
            if (newLogo != team.LogoUrl) DeleteLocalLogo(team.LogoUrl); // bỏ file cũ đã upload nếu đổi sang logo khác
            team.LogoUrl = newLogo;
            team.KitColor = req.KitColor?.Trim();
            await _db.SaveChangesAsync();
            return Success(new { team.Id, team.Name, team.Faculty, team.LogoUrl, team.KitColor, team.Status, team.ManagerId }, "Cập nhật đội thành công");
        }

        // ------------------------------------------------------------------ Logo đội

        private const long MaxLogoBytes = 1024 * 1024; // 1 MB
        private const string LogoFolder = "uploads/logos";
        private const string LogoUrlMessage = "Logo phải là đường dẫn http(s) hoặc ảnh đã upload (/uploads/logos/...)";

        /// <summary>
        /// Upload logo (multipart/form-data, trường "file"): PNG, JPG, WEBP hoặc GIF, tối đa 1 MB.
        /// File lưu ở wwwroot/uploads/logos/team-{id}-{guid}.{ext}; LogoUrl = "/uploads/logos/...". Logo cũ đã upload bị xóa.
        /// </summary>
        [Authorize(Roles = "TeamManager,Admin")]
        [HttpPost("api/teams/{id:int}/logo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> UploadLogo(int id, IFormFile? file)
        {
            var team = await _db.Teams.FindAsync(id);
            if (team == null) return NotFound404("Đội không tồn tại");
            if (!IsAdmin && team.ManagerId != CurrentUserId) return Forbidden403("Đội không do bạn quản lý");

            if (file == null || file.Length == 0) return BadRequest400("Chưa chọn file ảnh");
            if (file.Length > MaxLogoBytes) return BadRequest400("Logo tối đa 1 MB");

            // Xác định loại ảnh bằng chữ ký đầu file (không tin phần mở rộng hay Content-Type do client gửi).
            var header = new byte[12];
            await using (var s = file.OpenReadStream()) _ = await s.ReadAsync(header);
            var ext = DetectImage(header);
            if (ext == null) return BadRequest400("Chỉ nhận ảnh PNG, JPG, WEBP hoặc GIF");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "logos");
            Directory.CreateDirectory(dir);
            var fileName = $"team-{team.Id}-{Guid.NewGuid():N}{ext}";
            await using (var output = System.IO.File.Create(Path.Combine(dir, fileName)))
                await file.CopyToAsync(output);

            DeleteLocalLogo(team.LogoUrl);
            team.LogoUrl = $"/{LogoFolder}/{fileName}";
            await _db.SaveChangesAsync();
            return Success(new { team.Id, team.LogoUrl }, "Đã cập nhật logo đội");
        }

        /// <summary>Xóa logo (file đã upload cũng bị xóa); đội quay về hiển thị chữ viết tắt.</summary>
        [Authorize(Roles = "TeamManager,Admin")]
        [HttpDelete("api/teams/{id:int}/logo")]
        public async Task<IActionResult> DeleteLogo(int id)
        {
            var team = await _db.Teams.FindAsync(id);
            if (team == null) return NotFound404("Đội không tồn tại");
            if (!IsAdmin && team.ManagerId != CurrentUserId) return Forbidden403("Đội không do bạn quản lý");

            DeleteLocalLogo(team.LogoUrl);
            team.LogoUrl = null;
            await _db.SaveChangesAsync();
            return Success(new { team.Id, team.LogoUrl }, "Đã xóa logo đội");
        }

        private static string? DetectImage(byte[] h)
        {
            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return ".png";
            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
            if (h[0] == 'G' && h[1] == 'I' && h[2] == 'F' && h[3] == '8') return ".gif";
            if (h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return ".webp";
            return null; // SVG bị từ chối vì có thể chứa script
        }

        private static bool IsValidLogoUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;
            url = url.Trim();
            if (url.StartsWith($"/{LogoFolder}/", StringComparison.OrdinalIgnoreCase) && !url.Contains("..")) return true;
            return Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>Chỉ xóa file nằm trong wwwroot/uploads/logos (link ngoài thì bỏ qua).</summary>
        private void DeleteLocalLogo(string? logoUrl)
        {
            if (string.IsNullOrEmpty(logoUrl) || !logoUrl.StartsWith($"/{LogoFolder}/", StringComparison.OrdinalIgnoreCase)) return;
            var path = Path.Combine(_env.WebRootPath, "uploads", "logos", Path.GetFileName(logoUrl));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        /// <summary>managerId chỉ Admin được chỉ định (phải là tài khoản TeamManager); người khác luôn dùng giá trị mặc định.</summary>
        private async Task<(int ManagerId, IActionResult? Error)> ResolveManager(int? requested, int fallback)
        {
            if (requested == null || !IsAdmin) return (fallback, null);
            var ok = await _db.Users.AnyAsync(u => u.Id == requested && u.Role == UserRole.TeamManager);
            return ok ? (requested.Value, null) : (fallback, BadRequest400("managerId phải là một tài khoản quản lý đội (TeamManager)"));
        }

        /// <summary>Duyệt cần 7–14 cầu thủ (BR01) và không vượt MaxTeams (BR05).</summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("api/teams/{id:int}/approval")]
        public async Task<IActionResult> Approval(int id, ApprovalRequest req)
        {
            var team = await _db.Teams.Include(t => t.Tournament).FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound404("Đội không tồn tại");
            if (team.Tournament!.Status != TournamentStatus.Registration)
                return Conflict409("Chỉ duyệt đội khi giải ở trạng thái Registration");

            if (req.Approved)
            {
                var players = await _db.Players.CountAsync(p => p.TeamId == id);
                if (players < 7 || players > 14) return Conflict409($"Đội có {players} cầu thủ, cần từ 7 đến 14 (BR01)");

                if (team.Status != TeamStatus.Approved)
                {
                    var approved = await _db.Teams.CountAsync(t => t.TournamentId == team.TournamentId && t.Status == TeamStatus.Approved);
                    if (approved >= team.Tournament.MaxTeams) return Conflict409("Giải đã đủ số đội tối đa được duyệt (BR05)");
                }
                team.Status = TeamStatus.Approved;
            }
            else
            {
                team.Status = TeamStatus.Rejected;
            }

            await _db.SaveChangesAsync();
            return Success(new { team.Id, team.Name, team.Status, reason = req.Reason },
                req.Approved ? "Đã duyệt đội" : "Đã từ chối đội");
        }

        public class TeamRequest
        {
            [Required, StringLength(100)] public string Name { get; set; } = "";
            [StringLength(100)] public string? Faculty { get; set; }
            [StringLength(255)] public string? LogoUrl { get; set; }
            [StringLength(30)] public string? KitColor { get; set; }
            /// <summary>Chỉ Admin dùng: gán đội cho một tài khoản TeamManager.</summary>
            public int? ManagerId { get; set; }
        }

        public class ApprovalRequest
        {
            public bool Approved { get; set; }
            public string? Reason { get; set; }
        }
    }
}
