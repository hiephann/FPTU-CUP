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
    /// <summary>FR17–FR24, FR31: lịch thi đấu, tạo/sinh lịch, đổi lịch, trạng thái trận, kết quả, chi tiết trận.</summary>
    [ApiController]
    public class MatchesController : ApiControllerBase
    {
        private const int MatchBlockMinutes = 70; // 60 phút thi đấu + 10 phút nghỉ (BR07)

        private readonly FptuCupDbContext _db;

        public MatchesController(FptuCupDbContext db) => _db = db;

        // ------------------------------------------------------------------ Xem lịch

        /// <summary>Lịch thi đấu có bộ lọc theo giải, vòng, đội, ngày; mine=true: trận được phân công cho trọng tài đang đăng nhập.</summary>
        [HttpGet("api/matches")]
        public async Task<IActionResult> GetAll([FromQuery] PagedQuery query, int? tournamentId, int? round, int? teamId,
            DateOnly? date, MatchStatus? status, bool mine = false)
        {
            var q = _db.Matches.AsQueryable();
            if (mine)
            {
                if (User.Identity?.IsAuthenticated != true) return Error(401, "Cần đăng nhập");
                var me = CurrentUserId;
                q = q.Where(m => m.RefereeId == me);
            }
            if (tournamentId != null) q = q.Where(m => m.TournamentId == tournamentId);
            if (round != null) q = q.Where(m => m.Round == round);
            if (teamId != null) q = q.Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId);
            if (status != null) q = q.Where(m => m.Status == status);
            if (date != null)
            {
                var from = date.Value.ToDateTime(TimeOnly.MinValue);
                var to = from.AddDays(1);
                q = q.Where(m => m.KickoffTime >= from && m.KickoffTime < to);
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(m => m.KickoffTime).ThenBy(m => m.Id)
                .Skip(query.Skip).Take(query.PageSize)
                .Select(m => new
                {
                    m.Id, m.TournamentId, m.Round, m.KickoffTime, m.Status, m.HomeScore, m.AwayScore,
                    homeTeamId = m.HomeTeamId, homeTeam = m.HomeTeam!.Name,
                    awayTeamId = m.AwayTeamId, awayTeam = m.AwayTeam!.Name,
                    m.VenueId, venue = m.Venue!.Name,
                    m.RefereeId, referee = m.Referee!.FullName,
                    lineupsSubmitted = m.Lineups.Select(l => l.TeamId).Distinct().Count(),
                    lineupDeadline = m.KickoffTime.AddHours(-VietnamTime.LineupDeadlineHours),
                    m.ForfeitTeamId, m.ForfeitReason,
                })
                .ToListAsync();
            return SuccessPaged(items, total, query);
        }

        /// <summary>FR31: tỉ số, đội hình xuất phát, bàn thắng, thẻ, thay người – gộp theo phút.</summary>
        [HttpGet("api/matches/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var m = await _db.Matches.Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id, x.TournamentId, x.Round, x.Status, x.KickoffTime, x.HomeScore, x.AwayScore,
                    x.HomeTeamId, homeName = x.HomeTeam!.Name, homeLogo = x.HomeTeam.LogoUrl, homeKit = x.HomeTeam.KitColor,
                    x.AwayTeamId, awayName = x.AwayTeam!.Name, awayLogo = x.AwayTeam.LogoUrl, awayKit = x.AwayTeam.KitColor,
                    x.VenueId, venue = x.Venue!.Name,
                    x.RefereeId, referee = x.Referee!.FullName,
                    x.ForfeitTeamId, x.ForfeitReason,
                })
                .FirstOrDefaultAsync();
            if (m == null) return NotFound404("Trận đấu không tồn tại");

            var lineups = await _db.MatchLineups.Where(l => l.MatchId == id)
                .OrderBy(l => l.Player!.ShirtNumber)
                .Select(l => new
                {
                    l.TeamId, l.IsStarter,
                    playerId = l.PlayerId, shirtNumber = l.Player!.ShirtNumber, name = l.Player.FullName, position = l.Player.Position,
                })
                .ToListAsync();

            var goals = await _db.Goals.Where(g => g.MatchId == id)
                .Select(g => new { g.Id, g.Minute, g.TeamId, g.PlayerId, player = g.Player!.FullName, g.IsOwnGoal }).ToListAsync();
            var cards = await _db.Cards.Where(c => c.MatchId == id)
                .Select(c => new { c.Id, c.Minute, c.CardType, c.PlayerId, player = c.Player!.FullName, teamId = c.Player.TeamId }).ToListAsync();
            var subs = await _db.Substitutions.Where(s => s.MatchId == id)
                .Select(s => new
                {
                    s.Id, s.Minute, s.TeamId,
                    playerOutId = s.PlayerOutId, playerOut = s.PlayerOut!.FullName,
                    playerInId = s.PlayerInId, playerIn = s.PlayerIn!.FullName,
                }).ToListAsync();

            var events = new List<(int Minute, object Data)>();
            events.AddRange(goals.Select(g => (g.Minute, (object)new { eventId = g.Id, g.Minute, type = "Goal", g.TeamId, g.PlayerId, g.player, g.IsOwnGoal })));
            events.AddRange(cards.Select(c => (c.Minute, (object)new { eventId = c.Id, c.Minute, type = c.CardType == CardType.Yellow ? "YellowCard" : "RedCard", TeamId = c.teamId, c.PlayerId, c.player })));
            events.AddRange(subs.Select(s => (s.Minute, (object)new { eventId = s.Id, s.Minute, type = "Substitution", s.TeamId, s.playerOutId, s.playerOut, s.playerInId, s.playerIn })));
            var sortedEvents = events.OrderBy(e => e.Minute).Select(e => e.Data).ToList();

            object Team(int teamId, string name, string? logo, string? kit, int score) => new
            {
                id = teamId, name, logoUrl = logo, kitColor = kit, score,
                lineupSubmitted = lineups.Any(l => l.TeamId == teamId),
                startingLineup = lineups.Where(l => l.TeamId == teamId && l.IsStarter).Select(l => new { l.playerId, l.shirtNumber, l.name, l.position }).ToList(),
                substitutes = lineups.Where(l => l.TeamId == teamId && !l.IsStarter).Select(l => new { l.playerId, l.shirtNumber, l.name, l.position }).ToList(),
            };

            return Success(new
            {
                m.Id, m.TournamentId, m.Round, m.Status, m.KickoffTime,
                m.VenueId, m.venue, m.RefereeId, m.referee,
                lineupDeadline = VietnamTime.LineupDeadline(m.KickoffTime),
                m.ForfeitTeamId, m.ForfeitReason,
                homeTeam = Team(m.HomeTeamId, m.homeName, m.homeLogo, m.homeKit, m.HomeScore),
                awayTeam = Team(m.AwayTeamId, m.awayName, m.awayLogo, m.awayKit, m.AwayScore),
                events = sortedEvents,
            });
        }

        // ------------------------------------------------------------------ Tạo / sinh / đổi lịch

        [Authorize(Roles = "Admin")]
        [HttpPost("api/matches")]
        public async Task<IActionResult> Create(CreateMatchRequest req)
        {
            var tournament = await _db.Tournaments.FindAsync(req.TournamentId);
            if (tournament == null) return NotFound404("Giải đấu không tồn tại");
            if (tournament.Status != TournamentStatus.Ongoing) return Conflict409("Chỉ tạo trận khi giải ở trạng thái Ongoing");

            var error = await CheckSchedule(req.TournamentId, req.HomeTeamId, req.AwayTeamId, req.VenueId, req.RefereeId, req.KickoffTime, null);
            if (error != null) return error;

            var match = new Match
            {
                TournamentId = req.TournamentId,
                Round = req.Round,
                HomeTeamId = req.HomeTeamId,
                AwayTeamId = req.AwayTeamId,
                VenueId = req.VenueId,
                RefereeId = req.RefereeId,
                KickoffTime = req.KickoffTime,
                Status = MatchStatus.Scheduled,
            };
            _db.Matches.Add(match);
            await _db.SaveChangesAsync();
            return Created201(new { match.Id, match.Round, match.HomeTeamId, match.AwayTeamId, match.VenueId, match.RefereeId, match.KickoffTime, match.Status }, "Tạo trận thành công");
        }

        /// <summary>Đổi giờ, đổi sân, phân công trọng tài (FR19, FR20). Trận Postponed được xếp lại sẽ về Scheduled.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("api/matches/{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateMatchRequest req)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound404("Trận đấu không tồn tại");
            if (match.Status != MatchStatus.Scheduled && match.Status != MatchStatus.Postponed)
                return Conflict409("Chỉ đổi lịch trận đang ở trạng thái Scheduled hoặc Postponed");

            var error = await CheckSchedule(match.TournamentId, match.HomeTeamId, match.AwayTeamId, req.VenueId, req.RefereeId, req.KickoffTime, id);
            if (error != null) return error;

            match.KickoffTime = req.KickoffTime;
            match.VenueId = req.VenueId;
            match.RefereeId = req.RefereeId;
            match.Status = MatchStatus.Scheduled;
            match.UpdatedBy = CurrentUserId;
            match.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Success(new { match.Id, match.KickoffTime, match.VenueId, match.RefereeId, match.Status }, "Cập nhật trận thành công");
        }

        /// <summary>Sinh lịch vòng tròn bằng circle method; 2 lượt thì lượt về đảo sân (BR17). Lưu trong một transaction.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("api/tournaments/{id:int}/matches/generate")]
        public async Task<IActionResult> Generate(int id, GenerateRequest req)
        {
            var tournament = await _db.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound404("Giải đấu không tồn tại");
            if (tournament.Status != TournamentStatus.Ongoing) return Conflict409("Chỉ sinh lịch khi giải ở trạng thái Ongoing");

            var teams = await _db.Teams.Where(t => t.TournamentId == id && t.Status == TeamStatus.Approved).OrderBy(t => t.Id).ToListAsync();
            if (teams.Count < 2) return Conflict409("Cần ít nhất 2 đội được duyệt");
            if (await _db.Matches.AnyAsync(m => m.TournamentId == id)) return Conflict409("Lịch thi đấu đã tồn tại");

            var times = new List<TimeOnly>();
            foreach (var text in req.KickoffTimes)
            {
                if (!TimeOnly.TryParse(text, out var t)) return BadRequest400($"Khung giờ '{text}' không hợp lệ (dạng HH:mm)");
                times.Add(t);
            }
            times.Sort();
            for (var i = 1; i < times.Count; i++)
                if ((times[i] - times[i - 1]).TotalMinutes < MatchBlockMinutes)
                    return BadRequest400("Các khung giờ phải cách nhau ít nhất 70 phút (60 phút thi đấu + 10 phút nghỉ) – BR07");

            var venueIds = req.VenueIds.Distinct().ToList();
            var venueCount = await _db.Venues.CountAsync(v => venueIds.Contains(v.Id));
            if (venueCount != venueIds.Count) return BadRequest400("Có sân không tồn tại");

            // Circle method: nếu số đội lẻ thì thêm một đội "nghỉ" (null).
            var arr = teams.Select(t => (int?)t.Id).ToList();
            if (arr.Count % 2 == 1) arr.Add(null);
            var size = arr.Count;
            var firstLeg = new List<List<(int Home, int Away)>>();
            for (var r = 0; r < size - 1; r++)
            {
                var pairs = new List<(int Home, int Away)>();
                for (var i = 0; i < size / 2; i++)
                {
                    var a = arr[i];
                    var b = arr[size - 1 - i];
                    if (a == null || b == null) continue;
                    pairs.Add((r + i) % 2 == 0 ? (a.Value, b.Value) : (b.Value, a.Value));
                }
                firstLeg.Add(pairs);
                var last = arr[^1];
                arr.RemoveAt(size - 1);
                arr.Insert(1, last);
            }

            var rounds = new List<List<(int Home, int Away)>>(firstLeg);
            if (tournament.NumberOfLegs == 2)
                rounds.AddRange(firstLeg.Select(p => p.Select(x => (Home: x.Away, Away: x.Home)).ToList()));

            var slots = times.SelectMany(t => venueIds.Select(v => (Time: t, VenueId: v))).ToList();
            var perRound = rounds.Max(r => r.Count);
            if (slots.Count < perRound)
                return BadRequest400($"Mỗi vòng có {perRound} trận, cần ít nhất {perRound} khung giờ × sân (đang có {slots.Count})");

            var matches = new List<Match>();
            for (var r = 0; r < rounds.Count; r++)
            {
                var day = req.FirstKickoff.AddDays(r * req.DaysBetweenRounds);
                for (var i = 0; i < rounds[r].Count; i++)
                {
                    matches.Add(new Match
                    {
                        TournamentId = id,
                        Round = r + 1,
                        HomeTeamId = rounds[r][i].Home,
                        AwayTeamId = rounds[r][i].Away,
                        VenueId = slots[i].VenueId,
                        KickoffTime = day.ToDateTime(slots[i].Time),
                        Status = MatchStatus.Scheduled,
                    });
                }
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.Matches.AddRange(matches);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var names = teams.ToDictionary(t => t.Id, t => t.Name);
            var venues = await _db.Venues.Where(v => venueIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Name);
            return Created201(new
            {
                totalRounds = rounds.Count,
                totalMatches = matches.Count,
                matches = matches.Select(m => new
                {
                    m.Id, m.Round,
                    homeTeam = names[m.HomeTeamId], awayTeam = names[m.AwayTeamId],
                    venue = venues[m.VenueId!.Value], m.KickoffTime,
                }).ToList(),
            }, "Đã sinh lịch thi đấu");
        }

        // ------------------------------------------------------------------ Trạng thái trận

        /// <summary>
        /// Scheduled → InProgress → Finished; Scheduled → Postponed/Cancelled (chỉ Admin) – BR16.
        /// Bắt đầu cần đủ đội hình hai đội (BR20); trọng tài chỉ cập nhật trận được phân công (BR09).
        /// </summary>
        [Authorize(Roles = "Admin,Referee")]
        [HttpPatch("api/matches/{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, ChangeMatchStatusRequest req)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound404("Trận đấu không tồn tại");
            if (!IsAdmin && match.RefereeId != CurrentUserId) return Forbidden403("Bạn không được phân công trận này");

            var allowed = (match.Status, req.Status) switch
            {
                (MatchStatus.Scheduled, MatchStatus.InProgress) => true,
                (MatchStatus.InProgress, MatchStatus.Finished) => true,
                (MatchStatus.Scheduled, MatchStatus.Postponed) => IsAdmin,
                (MatchStatus.Scheduled, MatchStatus.Cancelled) => IsAdmin,
                _ => false,
            };
            if (!allowed)
            {
                if ((req.Status == MatchStatus.Postponed || req.Status == MatchStatus.Cancelled) && !IsAdmin)
                    return Forbidden403("Chỉ Admin được hoãn hoặc hủy trận");
                return Conflict409($"Không thể chuyển trận từ {match.Status} sang {req.Status} (BR16)");
            }

            if (req.Status == MatchStatus.InProgress)
            {
                var submitted = await _db.MatchLineups.Where(l => l.MatchId == id).Select(l => l.TeamId).Distinct().CountAsync();
                if (submitted < 2) return Conflict409("Trận chỉ được bắt đầu khi cả hai đội đã nộp đội hình (BR20)");
            }

            if (req.Status == MatchStatus.Finished)
                await FinishMatch(match);

            match.Status = req.Status;
            match.UpdatedBy = CurrentUserId;
            match.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Success(new { match.Id, match.Status, match.HomeScore, match.AwayScore }, "Đã cập nhật trạng thái trận");
        }

        /// <summary>
        /// Kết thúc trận: tính lại tỉ số từ bàn thắng (BR11), giảm án treo giò hiện có của hai đội,
        /// sinh án mới cho thẻ đỏ và mỗi 3 thẻ vàng tích lũy (BR14). Gọi trước SaveChanges của action.
        /// </summary>
        private async Task FinishMatch(Match match)
        {
            var goals = await _db.Goals.Where(g => g.MatchId == match.Id).ToListAsync();
            match.HomeScore = goals.Count(g => g.TeamId == match.HomeTeamId);
            match.AwayScore = goals.Count(g => g.TeamId == match.AwayTeamId);

            var teamIds = new[] { match.HomeTeamId, match.AwayTeamId };
            var existing = await _db.Suspensions
                .Where(s => s.TournamentId == match.TournamentId && s.MatchesRemaining > 0 && teamIds.Contains(s.Player!.TeamId))
                .ToListAsync();
            foreach (var s in existing) s.MatchesRemaining--;

            var cards = await _db.Cards.Where(c => c.MatchId == match.Id).ToListAsync();
            foreach (var playerId in cards.Where(c => c.CardType == CardType.Red).Select(c => c.PlayerId).Distinct())
            {
                _db.Suspensions.Add(new Suspension
                {
                    PlayerId = playerId, TournamentId = match.TournamentId,
                    Reason = $"Thẻ đỏ vòng {match.Round}", MatchesRemaining = 1,
                });
            }

            foreach (var g in cards.Where(c => c.CardType == CardType.Yellow).GroupBy(c => c.PlayerId))
            {
                var before = await _db.Cards.CountAsync(c => c.PlayerId == g.Key && c.CardType == CardType.Yellow
                    && c.MatchId != match.Id && c.Match!.TournamentId == match.TournamentId && c.Match.Status == MatchStatus.Finished);
                if ((before + g.Count()) / 3 > before / 3)
                {
                    _db.Suspensions.Add(new Suspension
                    {
                        PlayerId = g.Key, TournamentId = match.TournamentId,
                        Reason = "Tích lũy 3 thẻ vàng", MatchesRemaining = 1,
                    });
                }
            }
        }

        /// <summary>BR15: chỉ Admin sửa trận đã Finished; ghi lại người sửa và thời điểm.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("api/matches/{id:int}/result")]
        public async Task<IActionResult> EditResult(int id, EditResultRequest req)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound404("Trận đấu không tồn tại");
            if (match.Status != MatchStatus.Finished) return Conflict409("Chỉ sửa kết quả trận đã kết thúc");

            match.HomeScore = req.HomeScore;
            match.AwayScore = req.AwayScore;
            match.UpdatedBy = CurrentUserId;
            match.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Tỉ số phải khớp với số bàn thắng đã ghi (BR11); bảng xếp hạng dùng tỉ số mới, nên cảnh báo nếu lệch.
            var homeGoals = await _db.Goals.CountAsync(g => g.MatchId == id && g.TeamId == match.HomeTeamId);
            var awayGoals = await _db.Goals.CountAsync(g => g.MatchId == id && g.TeamId == match.AwayTeamId);
            var consistent = homeGoals == req.HomeScore && awayGoals == req.AwayScore;
            return Success(
                new { match.Id, match.HomeScore, match.AwayScore, match.UpdatedBy, match.UpdatedAt, req.Reason, goalsRecorded = new { home = homeGoals, away = awayGoals }, consistent },
                consistent ? "Đã sửa kết quả trận" : $"Đã sửa kết quả. Lưu ý: danh sách bàn thắng đang ghi {homeGoals}–{awayGoals}, khác tỉ số mới (BR11)");
        }

        // ------------------------------------------------------------------ Quá hạn nộp đội hình (BR23)

        /// <summary>
        /// Xử lý các trận chưa đá đã quá hạn nộp đội hình (trước giờ đá 1 ngày, giờ Việt Nam).
        /// Thiếu đội hình một đội: đội đó bị xử thua 0–3 (trận Finished, ghi lý do và đội bị xử thua).
        /// Cả hai đội đều thiếu: trận bị hủy (Cancelled), ghi lý do. Chạy tự động mỗi phút bởi ForfeitWorker.
        /// </summary>
        public static async Task<List<object>> ProcessOverdueLineups(FptuCupDbContext db, DateTime nowVn)
        {
            var limit = nowVn.AddHours(VietnamTime.LineupDeadlineHours);
            var overdue = await db.Matches
                .Include(m => m.HomeTeam).Include(m => m.AwayTeam)
                .Where(m => m.Status == MatchStatus.Scheduled && m.Tournament!.Status == TournamentStatus.Ongoing && m.KickoffTime <= limit)
                .ToListAsync();

            var results = new List<object>();
            foreach (var m in overdue)
            {
                var submitted = await db.MatchLineups.Where(l => l.MatchId == m.Id).Select(l => l.TeamId).Distinct().ToListAsync();
                var homeMissing = !submitted.Contains(m.HomeTeamId);
                var awayMissing = !submitted.Contains(m.AwayTeamId);
                if (!homeMissing && !awayMissing) continue; // đủ đội hình, chờ trọng tài bắt đầu trận

                var deadline = VietnamTime.LineupDeadline(m.KickoffTime).ToString("HH:mm dd/MM/yyyy");
                m.UpdatedAt = DateTime.UtcNow;
                m.UpdatedBy = null;

                if (homeMissing && awayMissing)
                {
                    m.Status = MatchStatus.Cancelled;
                    m.ForfeitTeamId = null;
                    m.ForfeitReason = $"Cả hai đội không nộp đội hình trước hạn {deadline}, trận bị hủy (BR23)";
                }
                else
                {
                    var loser = homeMissing ? m.HomeTeam! : m.AwayTeam!;
                    m.Status = MatchStatus.Finished;
                    m.HomeScore = homeMissing ? 0 : 3;
                    m.AwayScore = homeMissing ? 3 : 0;
                    m.ForfeitTeamId = loser.Id;
                    m.ForfeitReason = $"{loser.Name} không nộp đội hình trước hạn {deadline}, bị xử thua 0–3 (BR23)";

                    // Trận xử thua vẫn tính là một trận: giảm án treo giò hiện có của hai đội (BR14).
                    var teamIds = new[] { m.HomeTeamId, m.AwayTeamId };
                    var suspensions = await db.Suspensions
                        .Where(s => s.TournamentId == m.TournamentId && s.MatchesRemaining > 0 && teamIds.Contains(s.Player!.TeamId))
                        .ToListAsync();
                    foreach (var s in suspensions) s.MatchesRemaining--;
                }

                results.Add(new { m.Id, m.Round, m.Status, m.ForfeitTeamId, reason = m.ForfeitReason });
            }

            if (results.Count > 0) await db.SaveChangesAsync();
            return results;
        }

        /// <summary>Admin chạy xử lý quá hạn ngay; asOf (giờ VN) dùng để giả lập thời điểm khi demo/test.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("api/matches/process-forfeits")]
        public async Task<IActionResult> ProcessForfeits(ProcessForfeitsRequest? req)
        {
            var now = req?.AsOf ?? VietnamTime.Now;
            var processed = await ProcessOverdueLineups(_db, now);
            return Success(new { asOf = now, processed },
                processed.Count == 0 ? "Không có trận nào quá hạn nộp đội hình" : $"Đã xử lý {processed.Count} trận quá hạn nộp đội hình");
        }

        // ------------------------------------------------------------------ Kiểm tra lịch (BR06–BR08)

        private async Task<IActionResult?> CheckSchedule(int tournamentId, int homeId, int awayId, int? venueId, int? refereeId,
            DateTime kickoff, int? excludeMatchId)
        {
            if (homeId == awayId) return BadRequest400("Hai đội trong một trận phải khác nhau (BR06)");

            var teamCount = await _db.Teams.CountAsync(t => (t.Id == homeId || t.Id == awayId)
                && t.TournamentId == tournamentId && t.Status == TeamStatus.Approved);
            if (teamCount != 2) return BadRequest400("Hai đội phải được duyệt và cùng thuộc giải này (BR06)");

            if (venueId != null && !await _db.Venues.AnyAsync(v => v.Id == venueId)) return BadRequest400("Sân không tồn tại");
            if (refereeId != null && !await _db.Users.AnyAsync(u => u.Id == refereeId && u.Role == UserRole.Referee && u.IsActive))
                return BadRequest400("Trọng tài không hợp lệ");

            var activeMatches = _db.Matches.Where(m => m.Id != excludeMatchId
                && m.Status != MatchStatus.Cancelled && m.Status != MatchStatus.Postponed);

            if (venueId != null)
            {
                var from = kickoff.AddMinutes(-MatchBlockMinutes);
                var to = kickoff.AddMinutes(MatchBlockMinutes);
                if (await activeMatches.AnyAsync(m => m.VenueId == venueId && m.KickoffTime > from && m.KickoffTime < to))
                    return Conflict409("Sân đã có trận khác trong khung giờ này (BR07)");
            }

            var day = kickoff.Date;
            var nextDay = day.AddDays(1);
            if (await activeMatches.AnyAsync(m => m.KickoffTime >= day && m.KickoffTime < nextDay
                && (m.HomeTeamId == homeId || m.AwayTeamId == homeId || m.HomeTeamId == awayId || m.AwayTeamId == awayId)))
                return Conflict409("Một trong hai đội đã có trận khác trong ngày này (BR08)");

            return null;
        }

        public class CreateMatchRequest
        {
            public int TournamentId { get; set; }
            [Range(1, 100)] public int Round { get; set; }
            public int HomeTeamId { get; set; }
            public int AwayTeamId { get; set; }
            public int? VenueId { get; set; }
            public int? RefereeId { get; set; }
            public DateTime KickoffTime { get; set; }
        }

        public class UpdateMatchRequest
        {
            public DateTime KickoffTime { get; set; }
            public int? VenueId { get; set; }
            public int? RefereeId { get; set; }
        }

        public class ProcessForfeitsRequest
        {
            /// <summary>Thời điểm (giờ Việt Nam) dùng để xét quá hạn; bỏ trống = bây giờ.</summary>
            public DateTime? AsOf { get; set; }
        }

        public class GenerateRequest
        {
            public DateOnly FirstKickoff { get; set; }
            [Range(1, 60)] public int DaysBetweenRounds { get; set; } = 7;
            [Required, MinLength(1)] public List<string> KickoffTimes { get; set; } = new();
            [Required, MinLength(1)] public List<int> VenueIds { get; set; } = new();
        }

        public class ChangeMatchStatusRequest
        {
            public MatchStatus Status { get; set; }
        }

        public class EditResultRequest
        {
            [Range(0, 99)] public int HomeScore { get; set; }
            [Range(0, 99)] public int AwayScore { get; set; }
            public string? Reason { get; set; }
        }
    }
}
