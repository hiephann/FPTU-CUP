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
    /// <summary>FR29: nộp / xem đội hình xuất phát của từng đội trong trận.</summary>
    [ApiController]
    [Route("api/matches/{id:int}/lineups")]
    public class LineupsController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        public LineupsController(FptuCupDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var match = await _db.Matches.Where(m => m.Id == id)
                .Select(m => new { m.HomeTeamId, home = m.HomeTeam!.Name, m.AwayTeamId, away = m.AwayTeam!.Name, m.KickoffTime })
                .FirstOrDefaultAsync();
            if (match == null) return NotFound404("Trận đấu không tồn tại");
            var deadline = VietnamTime.LineupDeadline(match.KickoffTime);

            var rows = await _db.MatchLineups.Where(l => l.MatchId == id)
                .OrderBy(l => l.Player!.ShirtNumber)
                .Select(l => new { l.TeamId, l.IsStarter, playerId = l.PlayerId, shirtNumber = l.Player!.ShirtNumber, name = l.Player.FullName, position = l.Player.Position })
                .ToListAsync();

            object Team(int teamId, string name) => new
            {
                teamId, name,
                submitted = rows.Any(r => r.TeamId == teamId),
                starters = rows.Where(r => r.TeamId == teamId && r.IsStarter).Select(r => new { r.playerId, r.shirtNumber, r.name, r.position }).ToList(),
                substitutes = rows.Where(r => r.TeamId == teamId && !r.IsStarter).Select(r => new { r.playerId, r.shirtNumber, r.name, r.position }).ToList(),
            };
            return Success(new
            {
                matchId = id, deadline, overdue = VietnamTime.Now > deadline,
                home = Team(match.HomeTeamId, match.home), away = Team(match.AwayTeamId, match.away),
            });
        }

        /// <summary>
        /// Đúng 7 cầu thủ xuất phát, đúng 1 thủ môn, dự bị tối đa 7, không ai bị treo giò (BR19);
        /// chỉ nộp được khi trận còn Scheduled – sau đó đội hình bị khóa (BR20).
        /// </summary>
        [Authorize(Roles = "TeamManager")]
        [HttpPut("{teamId:int}")]
        public async Task<IActionResult> Submit(int id, int teamId, LineupRequest req)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound404("Trận đấu không tồn tại");
            if (teamId != match.HomeTeamId && teamId != match.AwayTeamId) return BadRequest400("Đội không tham gia trận này");

            var team = await _db.Teams.FindAsync(teamId);
            if (team == null || team.ManagerId != CurrentUserId) return Forbidden403("Đội không do bạn quản lý");
            if (match.Status != MatchStatus.Scheduled) return Conflict409("Trận đã bắt đầu hoặc đã đóng, đội hình bị khóa (BR20)");

            var deadline = VietnamTime.LineupDeadline(match.KickoffTime);
            if (VietnamTime.Now > deadline)
                return Conflict409($"Đã quá hạn nộp đội hình ({deadline:HH:mm dd/MM/yyyy}); đội không nộp sẽ bị xử thua (BR23)");

            var starters = req.Starters.Distinct().ToList();
            var subs = req.Substitutes.Distinct().ToList();
            if (starters.Count != 7 || starters.Count != req.Starters.Count) return BadRequest400("Đội hình xuất phát phải có đúng 7 cầu thủ khác nhau (BR19)");
            if (subs.Count != req.Substitutes.Count || subs.Count > 7) return BadRequest400("Dự bị tối đa 7 cầu thủ, không trùng nhau (BR19)");
            if (subs.Intersect(starters).Any()) return BadRequest400("Một cầu thủ không thể vừa xuất phát vừa dự bị");

            var ids = starters.Concat(subs).ToList();
            var players = await _db.Players.Where(p => ids.Contains(p.Id)).ToListAsync();
            if (players.Count != ids.Count || players.Any(p => p.TeamId != teamId)) return BadRequest400("Có cầu thủ không thuộc đội");
            if (players.Count(p => starters.Contains(p.Id) && p.Position == PlayerPosition.GK) != 1)
                return BadRequest400("Đội hình xuất phát phải có đúng 1 thủ môn (BR19)");

            var suspended = await _db.Suspensions
                .Where(s => s.TournamentId == match.TournamentId && s.MatchesRemaining > 0 && ids.Contains(s.PlayerId))
                .Select(s => s.Player!.FullName).ToListAsync();
            if (suspended.Count > 0) return Conflict409("Cầu thủ đang bị treo giò không được đăng ký: " + string.Join(", ", suspended));

            _db.MatchLineups.RemoveRange(_db.MatchLineups.Where(l => l.MatchId == id && l.TeamId == teamId));
            foreach (var p in starters) _db.MatchLineups.Add(new MatchLineup { MatchId = id, TeamId = teamId, PlayerId = p, IsStarter = true });
            foreach (var p in subs) _db.MatchLineups.Add(new MatchLineup { MatchId = id, TeamId = teamId, PlayerId = p, IsStarter = false });
            await _db.SaveChangesAsync();

            var byId = players.ToDictionary(p => p.Id);
            object Item(int pid) => new { playerId = pid, shirtNumber = byId[pid].ShirtNumber, fullName = byId[pid].FullName, position = byId[pid].Position };
            return Success(new
            {
                matchId = id, teamId,
                starters = starters.Select(Item).ToList(),
                substitutes = subs.Select(Item).ToList(),
            }, "Đã nộp đội hình");
        }

        public class LineupRequest
        {
            [Required] public List<int> Starters { get; set; } = new();
            public List<int> Substitutes { get; set; } = new();
        }
    }
}
