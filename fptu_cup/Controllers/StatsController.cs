using fptu_cup.Common.Enums;
using fptu_cup.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Controllers
{
    /// <summary>FR25–FR27: bảng xếp hạng, vua phá lưới, cầu thủ bị treo giò (đều công khai).</summary>
    [ApiController]
    [Route("api/tournaments/{id:int}")]
    public class StatsController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        public StatsController(FptuCupDbContext db) => _db = db;

        private class Row
        {
            public int TeamId;
            public string Team = "";
            public string? LogoUrl;
            public string? KitColor;
            public int Played, Won, Drawn, Lost, GoalsFor, GoalsAgainst, HeadToHead;
            public string Form = "";
            public int Points => Won * 3 + Drawn;
            public int GoalDifference => GoalsFor - GoalsAgainst;
        }

        /// <summary>Tính từ các trận Finished (không lưu bảng riêng): Điểm → Hiệu số → Bàn thắng → Đối đầu (BR12, BR13).</summary>
        [HttpGet("standings")]
        public async Task<IActionResult> Standings(int id)
        {
            if (!await _db.Tournaments.AnyAsync(t => t.Id == id)) return NotFound404("Giải đấu không tồn tại");

            var teams = await _db.Teams.Where(t => t.TournamentId == id && t.Status == TeamStatus.Approved).ToListAsync();
            var matches = await _db.Matches
                .Where(m => m.TournamentId == id && m.Status == MatchStatus.Finished)
                .OrderBy(m => m.KickoffTime).ThenBy(m => m.Id)
                .Select(m => new { m.HomeTeamId, m.AwayTeamId, m.HomeScore, m.AwayScore })
                .ToListAsync();

            var rows = teams.Select(t =>
            {
                var r = new Row { TeamId = t.Id, Team = t.Name, LogoUrl = t.LogoUrl, KitColor = t.KitColor };
                foreach (var m in matches.Where(m => m.HomeTeamId == t.Id || m.AwayTeamId == t.Id))
                {
                    var home = m.HomeTeamId == t.Id;
                    var gf = home ? m.HomeScore : m.AwayScore;
                    var ga = home ? m.AwayScore : m.HomeScore;
                    r.Played++;
                    r.GoalsFor += gf;
                    r.GoalsAgainst += ga;
                    if (gf > ga) { r.Won++; r.Form += "T"; }
                    else if (gf == ga) { r.Drawn++; r.Form += "H"; }
                    else { r.Lost++; r.Form += "B"; }
                }
                r.Form = r.Form.Length > 3 ? r.Form[^3..] : r.Form;
                return r;
            }).ToList();

            // Cùng Điểm, Hiệu số, Bàn thắng thì xét đối đầu trực tiếp giữa các đội bằng điểm.
            var ordered = new List<Row>();
            var groups = rows.GroupBy(r => new { r.Points, r.GoalDifference, r.GoalsFor })
                .OrderByDescending(g => g.Key.Points).ThenByDescending(g => g.Key.GoalDifference).ThenByDescending(g => g.Key.GoalsFor);
            foreach (var g in groups)
            {
                var list = g.ToList();
                if (list.Count > 1)
                {
                    var ids = list.Select(r => r.TeamId).ToHashSet();
                    foreach (var r in list)
                    {
                        foreach (var m in matches.Where(m => ids.Contains(m.HomeTeamId) && ids.Contains(m.AwayTeamId)
                                                          && (m.HomeTeamId == r.TeamId || m.AwayTeamId == r.TeamId)))
                        {
                            var gf = m.HomeTeamId == r.TeamId ? m.HomeScore : m.AwayScore;
                            var ga = m.HomeTeamId == r.TeamId ? m.AwayScore : m.HomeScore;
                            r.HeadToHead += gf > ga ? 3 : gf == ga ? 1 : 0;
                        }
                    }
                    list = list.OrderByDescending(r => r.HeadToHead).ThenBy(r => r.Team).ToList();
                }
                ordered.AddRange(list);
            }

            var result = ordered.Select((r, i) => new
            {
                rank = i + 1,
                r.TeamId,
                team = r.Team,
                r.LogoUrl,
                r.KitColor,
                played = r.Played,
                won = r.Won,
                drawn = r.Drawn,
                lost = r.Lost,
                goalsFor = r.GoalsFor,
                goalsAgainst = r.GoalsAgainst,
                goalDifference = r.GoalDifference,
                points = r.Points,
                form = r.Form,
            }).ToList();
            return Success(result);
        }

        /// <summary>Chỉ đếm bàn thắng không phải phản lưới trong các trận Finished; bằng bàn thì ít trận hơn xếp trên.</summary>
        [HttpGet("top-scorers")]
        public async Task<IActionResult> TopScorers(int id, int top = 10)
        {
            if (!await _db.Tournaments.AnyAsync(t => t.Id == id)) return NotFound404("Giải đấu không tồn tại");
            top = Math.Clamp(top, 1, 50);

            var goals = await _db.Goals
                .Where(g => !g.IsOwnGoal && g.Match!.TournamentId == id && g.Match.Status == MatchStatus.Finished)
                .GroupBy(g => g.PlayerId)
                .Select(g => new { PlayerId = g.Key, Goals = g.Count() })
                .ToListAsync();
            var playerIds = goals.Select(g => g.PlayerId).ToList();

            var appearances = await _db.MatchLineups
                .Where(l => playerIds.Contains(l.PlayerId) && l.Match!.Status == MatchStatus.Finished)
                .GroupBy(l => l.PlayerId)
                .Select(g => new { PlayerId = g.Key, Matches = g.Count() })
                .ToDictionaryAsync(x => x.PlayerId, x => x.Matches);
            var players = await _db.Players.Where(p => playerIds.Contains(p.Id))
                .Select(p => new { p.Id, p.FullName, p.ShirtNumber, p.TeamId, Team = p.Team!.Name })
                .ToDictionaryAsync(p => p.Id);

            var result = goals
                .Select(g => new
                {
                    playerId = g.PlayerId,
                    player = players[g.PlayerId].FullName,
                    teamId = players[g.PlayerId].TeamId,
                    team = players[g.PlayerId].Team,
                    goals = g.Goals,
                    matches = appearances.GetValueOrDefault(g.PlayerId),
                })
                .OrderByDescending(x => x.goals).ThenBy(x => x.matches).ThenBy(x => x.player)
                .Take(top)
                .Select((x, i) => new { rank = i + 1, x.playerId, x.player, x.teamId, x.team, x.goals, x.matches })
                .ToList();
            return Success(result);
        }

        [HttpGet("suspensions")]
        public async Task<IActionResult> Suspensions(int id)
        {
            if (!await _db.Tournaments.AnyAsync(t => t.Id == id)) return NotFound404("Giải đấu không tồn tại");

            var items = await _db.Suspensions
                .Where(s => s.TournamentId == id && s.MatchesRemaining > 0)
                .OrderBy(s => s.Player!.FullName)
                .Select(s => new
                {
                    s.Id,
                    s.PlayerId,
                    player = s.Player!.FullName,
                    teamId = s.Player.TeamId,
                    team = s.Player.Team!.Name,
                    s.Reason,
                    s.MatchesRemaining,
                })
                .ToListAsync();
            return Success(items);
        }
    }
}
