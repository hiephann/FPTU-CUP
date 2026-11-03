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

        public TeamsController(FptuCupDbContext db) => _db = db;

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

        /// <summary>BR04: chỉ đăng ký khi giải đang ở trạng thái Registration.</summary>
        [Authorize(Roles = "TeamManager")]
        [HttpPost("api/tournaments/{id:int}/teams")]
        public async Task<IActionResult> Register(int id, TeamRequest req)
        {
            var tournament = await _db.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound404("Giải đấu không tồn tại");
            if (tournament.Status != TournamentStatus.Registration) return Conflict409("Giải không ở trạng thái Registration (BR04)");

            var name = req.Name.Trim();
            if (await _db.Teams.AnyAsync(t => t.TournamentId == id && t.Name == name))
                return Conflict409("Tên đội đã tồn tại trong giải");

            var team = new Team
            {
                TournamentId = id,
                ManagerId = CurrentUserId,
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

        /// <summary>Các đội do user đang đăng nhập quản lý.</summary>
        [Authorize(Roles = "TeamManager")]
        [HttpGet("api/teams/mine")]
        public async Task<IActionResult> Mine()
        {
            var me = CurrentUserId;
            var items = await _db.Teams.Where(t => t.ManagerId == me)
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

        [Authorize(Roles = "TeamManager")]
        [HttpPut("api/teams/{id:int}")]
        public async Task<IActionResult> Update(int id, TeamRequest req)
        {
            var team = await _db.Teams.FindAsync(id);
            if (team == null) return NotFound404("Đội không tồn tại");
            if (team.ManagerId != CurrentUserId) return Forbidden403("Đội không do bạn quản lý");

            var name = req.Name.Trim();
            if (await _db.Teams.AnyAsync(t => t.TournamentId == team.TournamentId && t.Name == name && t.Id != id))
                return Conflict409("Tên đội đã tồn tại trong giải");

            team.Name = name;
            team.Faculty = req.Faculty?.Trim();
            team.LogoUrl = req.LogoUrl?.Trim();
            team.KitColor = req.KitColor?.Trim();
            await _db.SaveChangesAsync();
            return Success(new { team.Id, team.Name, team.Faculty, team.LogoUrl, team.KitColor, team.Status }, "Cập nhật đội thành công");
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
        }

        public class ApprovalRequest
        {
            public bool Approved { get; set; }
            public string? Reason { get; set; }
        }
    }
}
