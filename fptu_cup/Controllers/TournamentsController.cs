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
    /// <summary>FR05–FR07, FR10, FR28: CRUD giải đấu, chuyển trạng thái, thống kê tổng quan.</summary>
    [ApiController]
    [Route("api/tournaments")]
    public class TournamentsController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        public TournamentsController(FptuCupDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PagedQuery query, TournamentStatus? status)
        {
            var q = _db.Tournaments.AsQueryable();
            if (status != null) q = q.Where(t => t.Status == status);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(t => t.Name.Contains(s) || t.Season.Contains(s));
            }

            q = query.SortBy?.ToLowerInvariant() == "name"
                ? q.OrderBy(t => t.Name)
                : q.OrderByDescending(t => t.StartDate).ThenByDescending(t => t.Id);

            var total = await q.CountAsync();
            var items = await q.Skip(query.Skip).Take(query.PageSize)
                .Select(t => new
                {
                    t.Id, t.Name, t.Season, t.StartDate, t.EndDate, t.MaxTeams, t.NumberOfLegs, t.Status,
                    approvedTeams = t.Teams.Count(x => x.Status == TeamStatus.Approved),
                    totalTeams = t.Teams.Count,
                })
                .ToListAsync();
            return SuccessPaged(items, total, query);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var t = await _db.Tournaments.Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id, x.Name, x.Season, x.StartDate, x.EndDate, x.MaxTeams, x.NumberOfLegs, x.Status,
                    approvedTeams = x.Teams.Count(y => y.Status == TeamStatus.Approved),
                    totalTeams = x.Teams.Count,
                })
                .FirstOrDefaultAsync();
            return t == null ? NotFound404("Giải đấu không tồn tại") : Success(t);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(TournamentRequest req)
        {
            if (req.EndDate < req.StartDate) return BadRequest400("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu");

            var t = new Tournament
            {
                Name = req.Name.Trim(),
                Season = req.Season.Trim(),
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                MaxTeams = req.MaxTeams,
                NumberOfLegs = req.NumberOfLegs,
                Status = TournamentStatus.Draft,
                CreatedBy = CurrentUserId,
            };
            _db.Tournaments.Add(t);
            await _db.SaveChangesAsync();
            return Created201(new { t.Id, t.Name, t.Season, t.StartDate, t.EndDate, t.MaxTeams, t.NumberOfLegs, t.Status }, "Tạo giải đấu thành công");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, TournamentRequest req)
        {
            var t = await _db.Tournaments.FindAsync(id);
            if (t == null) return NotFound404("Giải đấu không tồn tại");
            if (t.Status == TournamentStatus.Finished) return Conflict409("Giải đã kết thúc, không thể sửa");
            if (req.EndDate < req.StartDate) return BadRequest400("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu");
            if (t.Status == TournamentStatus.Ongoing && req.NumberOfLegs != t.NumberOfLegs)
                return Conflict409("Giải đang diễn ra, không đổi được số lượt");

            var approved = await _db.Teams.CountAsync(x => x.TournamentId == id && x.Status == TeamStatus.Approved);
            if (req.MaxTeams < approved) return Conflict409($"Số đội tối đa không được nhỏ hơn số đội đã duyệt ({approved})");

            t.Name = req.Name.Trim();
            t.Season = req.Season.Trim();
            t.StartDate = req.StartDate;
            t.EndDate = req.EndDate;
            t.MaxTeams = req.MaxTeams;
            t.NumberOfLegs = req.NumberOfLegs;
            await _db.SaveChangesAsync();
            return Success(new { t.Id, t.Name, t.Season, t.StartDate, t.EndDate, t.MaxTeams, t.NumberOfLegs, t.Status }, "Cập nhật giải đấu thành công");
        }

        /// <summary>Chỉ xóa được giải ở trạng thái Draft.</summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.Tournaments.FindAsync(id);
            if (t == null) return NotFound404("Giải đấu không tồn tại");
            if (t.Status != TournamentStatus.Draft) return Conflict409("Chỉ xóa được giải ở trạng thái Draft");
            if (await _db.Teams.AnyAsync(x => x.TournamentId == id)) return Conflict409("Giải đã có đội đăng ký");

            _db.Tournaments.Remove(t);
            await _db.SaveChangesAsync();
            return Success(null, "Đã xóa giải đấu");
        }

        /// <summary>Draft → Registration → Ongoing → Finished (BR04), mỗi lần chỉ đi một bước.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, ChangeStatusRequest req)
        {
            var t = await _db.Tournaments.FindAsync(id);
            if (t == null) return NotFound404("Giải đấu không tồn tại");
            if (req.Status != t.Status + 1)
                return Conflict409($"Không thể chuyển từ {t.Status} sang {req.Status}; thứ tự là Draft → Registration → Ongoing → Finished");

            if (req.Status == TournamentStatus.Ongoing)
            {
                var approved = await _db.Teams.CountAsync(x => x.TournamentId == id && x.Status == TeamStatus.Approved);
                if (approved < 2) return Conflict409("Cần ít nhất 2 đội được duyệt trước khi bắt đầu giải");
            }

            t.Status = req.Status;
            await _db.SaveChangesAsync();
            return Success(new { t.Id, t.Name, t.Status }, $"Đã chuyển giải sang {t.Status}");
        }

        /// <summary>FR28: số trận, số bàn, trung bình bàn/trận, thẻ phạt, đội và cầu thủ.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("{id:int}/summary")]
        public async Task<IActionResult> Summary(int id)
        {
            var t = await _db.Tournaments.FindAsync(id);
            if (t == null) return NotFound404("Giải đấu không tồn tại");

            var matches = _db.Matches.Where(m => m.TournamentId == id);
            var finished = matches.Where(m => m.Status == MatchStatus.Finished);
            var totalMatches = await matches.CountAsync(m => m.Status != MatchStatus.Cancelled);
            var playedMatches = await finished.CountAsync();
            var goals = await finished.SumAsync(m => (int?)(m.HomeScore + m.AwayScore)) ?? 0;
            var cards = _db.Cards.Where(c => c.Match!.TournamentId == id && c.Match.Status == MatchStatus.Finished);
            var approvedTeams = await _db.Teams.CountAsync(x => x.TournamentId == id && x.Status == TeamStatus.Approved);
            var players = await _db.Players.CountAsync(p => p.Team!.TournamentId == id && p.Team.Status == TeamStatus.Approved);
            var currentRound = await matches.Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.InProgress)
                .MinAsync(m => (int?)m.Round);
            var totalRounds = await matches.MaxAsync(m => (int?)m.Round) ?? 0;

            return Success(new
            {
                tournamentId = id,
                totalMatches,
                playedMatches,
                goals,
                averageGoalsPerMatch = playedMatches == 0 ? 0 : Math.Round(goals / (double)playedMatches, 2),
                yellowCards = await cards.CountAsync(c => c.CardType == CardType.Yellow),
                redCards = await cards.CountAsync(c => c.CardType == CardType.Red),
                approvedTeams,
                maxTeams = t.MaxTeams,
                players,
                suspendedPlayers = await _db.Suspensions.CountAsync(s => s.TournamentId == id && s.MatchesRemaining > 0),
                currentRound,
                totalRounds,
            });
        }

        public class TournamentRequest
        {
            [Required, StringLength(150)] public string Name { get; set; } = "";
            [Required, StringLength(20)] public string Season { get; set; } = "";
            public DateOnly StartDate { get; set; }
            public DateOnly EndDate { get; set; }
            [Range(2, 64, ErrorMessage = "Số đội tối đa phải từ 2 trở lên")] public int MaxTeams { get; set; }
            [Range(1, 2, ErrorMessage = "Số lượt phải là 1 hoặc 2")] public int NumberOfLegs { get; set; } = 2;
        }

        public class ChangeStatusRequest
        {
            public TournamentStatus Status { get; set; }
        }
    }
}
