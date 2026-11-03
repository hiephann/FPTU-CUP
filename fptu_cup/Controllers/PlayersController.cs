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
    /// <summary>FR12–FR14: quản lý cầu thủ của đội mình, tìm kiếm, thống kê cá nhân.</summary>
    [ApiController]
    public class PlayersController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        public PlayersController(FptuCupDbContext db) => _db = db;

        [HttpGet("api/teams/{teamId:int}/players")]
        public async Task<IActionResult> GetByTeam(int teamId)
        {
            if (!await _db.Teams.AnyAsync(t => t.Id == teamId)) return NotFound404("Đội không tồn tại");

            var items = await _db.Players.Where(p => p.TeamId == teamId)
                .OrderBy(p => p.ShirtNumber)
                .Select(p => new { p.Id, p.TeamId, p.StudentCode, p.FullName, p.ShirtNumber, p.Position, p.DateOfBirth })
                .ToListAsync();
            return Success(items);
        }

        [Authorize(Roles = "TeamManager")]
        [HttpPost("api/teams/{teamId:int}/players")]
        public async Task<IActionResult> Add(int teamId, PlayerRequest req)
        {
            var team = await _db.Teams.Include(t => t.Tournament).FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return NotFound404("Đội không tồn tại");
            if (team.ManagerId != CurrentUserId) return Forbidden403("Đội không do bạn quản lý");

            var error = await CheckPlayerRules(team, req, null);
            if (error != null) return error;
            if (await _db.Players.CountAsync(p => p.TeamId == teamId) >= 14)
                return Conflict409("Đội đã đủ 14 cầu thủ (BR01)");

            var player = new Player
            {
                TeamId = teamId,
                StudentCode = req.StudentCode.Trim().ToUpperInvariant(),
                FullName = req.FullName.Trim(),
                ShirtNumber = req.ShirtNumber,
                Position = req.Position,
                DateOfBirth = req.DateOfBirth,
            };
            _db.Players.Add(player);
            await _db.SaveChangesAsync();
            return Created201(new { player.Id, player.TeamId, player.StudentCode, player.FullName, player.ShirtNumber, player.Position }, "Thêm cầu thủ thành công");
        }

        [Authorize(Roles = "TeamManager")]
        [HttpPut("api/players/{id:int}")]
        public async Task<IActionResult> Update(int id, PlayerRequest req)
        {
            var player = await _db.Players.Include(p => p.Team!.Tournament).FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound404("Cầu thủ không tồn tại");
            if (player.Team!.ManagerId != CurrentUserId) return Forbidden403("Cầu thủ không thuộc đội bạn quản lý");

            var error = await CheckPlayerRules(player.Team, req, id);
            if (error != null) return error;

            player.StudentCode = req.StudentCode.Trim().ToUpperInvariant();
            player.FullName = req.FullName.Trim();
            player.ShirtNumber = req.ShirtNumber;
            player.Position = req.Position;
            player.DateOfBirth = req.DateOfBirth;
            await _db.SaveChangesAsync();
            return Success(new { player.Id, player.TeamId, player.StudentCode, player.FullName, player.ShirtNumber, player.Position }, "Cập nhật cầu thủ thành công");
        }

        [Authorize(Roles = "TeamManager")]
        [HttpDelete("api/players/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var player = await _db.Players.Include(p => p.Team!.Tournament).FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound404("Cầu thủ không tồn tại");
            if (player.Team!.ManagerId != CurrentUserId) return Forbidden403("Cầu thủ không thuộc đội bạn quản lý");
            if (player.Team.Tournament!.Status != TournamentStatus.Registration)
                return Conflict409("Chỉ sửa danh sách cầu thủ khi giải ở trạng thái Registration (BR04)");

            var count = await _db.Players.CountAsync(p => p.TeamId == player.TeamId);
            if (player.Team.Status == TeamStatus.Approved && count <= 7)
                return Conflict409("Đội đã được duyệt phải còn tối thiểu 7 cầu thủ (BR01)");
            if (await _db.MatchLineups.AnyAsync(l => l.PlayerId == id) || await _db.Goals.AnyAsync(g => g.PlayerId == id)
                || await _db.Cards.AnyAsync(c => c.PlayerId == id))
                return Conflict409("Cầu thủ đã có dữ liệu thi đấu, không thể xóa");

            _db.Players.Remove(player);
            await _db.SaveChangesAsync();
            return Success(null, "Đã xóa cầu thủ");
        }

        /// <summary>Tìm cầu thủ theo tên, MSSV, đội (FR13).</summary>
        [HttpGet("api/players")]
        public async Task<IActionResult> Search([FromQuery] PagedQuery query, int? teamId, int? tournamentId)
        {
            var q = _db.Players.Where(p => p.Team!.Status == TeamStatus.Approved);
            if (teamId != null) q = q.Where(p => p.TeamId == teamId);
            if (tournamentId != null) q = q.Where(p => p.Team!.TournamentId == tournamentId);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(p => p.FullName.Contains(s) || p.StudentCode.Contains(s) || p.Team!.Name.Contains(s));
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(p => p.FullName).Skip(query.Skip).Take(query.PageSize)
                .Select(p => new { p.Id, p.StudentCode, p.FullName, p.ShirtNumber, p.Position, teamId = p.TeamId, team = p.Team!.Name })
                .ToListAsync();
            return SuccessPaged(items, total, query);
        }

        /// <summary>FR14: bàn thắng (không tính phản lưới), thẻ, số trận đã ra sân.</summary>
        [HttpGet("api/players/{id:int}/stats")]
        public async Task<IActionResult> Stats(int id)
        {
            var player = await _db.Players.Where(p => p.Id == id)
                .Select(p => new { p.Id, p.FullName, p.ShirtNumber, p.Position, teamId = p.TeamId, team = p.Team!.Name, p.Team.TournamentId })
                .FirstOrDefaultAsync();
            if (player == null) return NotFound404("Cầu thủ không tồn tại");

            var finished = MatchStatus.Finished;
            return Success(new
            {
                player,
                goals = await _db.Goals.CountAsync(g => g.PlayerId == id && !g.IsOwnGoal && g.Match!.Status == finished),
                ownGoals = await _db.Goals.CountAsync(g => g.PlayerId == id && g.IsOwnGoal && g.Match!.Status == finished),
                yellowCards = await _db.Cards.CountAsync(c => c.PlayerId == id && c.CardType == CardType.Yellow && c.Match!.Status == finished),
                redCards = await _db.Cards.CountAsync(c => c.PlayerId == id && c.CardType == CardType.Red && c.Match!.Status == finished),
                matches = await _db.MatchLineups.CountAsync(l => l.PlayerId == id && l.Match!.Status == finished),
                suspendedMatches = await _db.Suspensions.Where(s => s.PlayerId == id).SumAsync(s => (int?)s.MatchesRemaining) ?? 0,
            });
        }

        /// <summary>BR02 (MSSV duy nhất trong giải), BR03 (số áo duy nhất trong đội), BR04 (chỉ khi Registration).</summary>
        private async Task<IActionResult?> CheckPlayerRules(Team team, PlayerRequest req, int? playerId)
        {
            if (team.Tournament!.Status != TournamentStatus.Registration)
                return Conflict409("Chỉ sửa danh sách cầu thủ khi giải ở trạng thái Registration (BR04)");

            var code = req.StudentCode.Trim().ToUpperInvariant();
            if (await _db.Players.AnyAsync(p => p.StudentCode == code && p.Team!.TournamentId == team.TournamentId && p.Id != playerId))
                return Conflict409("Sinh viên này đã thuộc một đội trong giải (BR02)");
            if (await _db.Players.AnyAsync(p => p.TeamId == team.Id && p.ShirtNumber == req.ShirtNumber && p.Id != playerId))
                return Conflict409("Số áo đã được dùng trong đội (BR03)");
            return null;
        }

        public class PlayerRequest
        {
            [Required, StringLength(20)] public string StudentCode { get; set; } = "";
            [Required, StringLength(100)] public string FullName { get; set; } = "";
            [Range(1, 99, ErrorMessage = "Số áo từ 1 đến 99")] public int ShirtNumber { get; set; }
            public PlayerPosition Position { get; set; }
            public DateOnly? DateOfBirth { get; set; }
        }
    }
}
