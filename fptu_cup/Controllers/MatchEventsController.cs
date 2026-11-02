using System.ComponentModel.DataAnnotations;
using fptu_cup.Common.Enums;
using fptu_cup.Data;
using fptu_cup.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Controllers
{
    /// <summary>FR23, FR30: trọng tài ghi / xóa bàn thắng, thẻ phạt, thay người khi trận đang InProgress.</summary>
    [ApiController]
    [Authorize(Roles = "Admin,Referee")]
    [Route("api/matches/{id:int}")]
    public class MatchEventsController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        public MatchEventsController(FptuCupDbContext db) => _db = db;

        // ------------------------------------------------------------------ Bàn thắng

        /// <summary>Cầu thủ phải thuộc một trong hai đội (BR10) và đang trên sân tại phút đó (BR22); tỉ số cập nhật ngay (BR11).</summary>
        [HttpPost("goals")]
        public async Task<IActionResult> AddGoal(int id, GoalRequest req)
        {
            var (match, error) = await LoadMatch(id);
            if (error != null) return error;

            var player = await _db.Players.FindAsync(req.PlayerId);
            if (player == null || (player.TeamId != match!.HomeTeamId && player.TeamId != match.AwayTeamId))
                return BadRequest400("Cầu thủ phải thuộc một trong hai đội của trận (BR10)");
            if (!await IsOnField(id, player.Id, req.Minute))
                return Conflict409("Cầu thủ không có mặt trên sân tại phút này (BR22)");

            // Bàn phản lưới nhà được tính cho đội đối phương.
            var opponent = player.TeamId == match.HomeTeamId ? match.AwayTeamId : match.HomeTeamId;
            var credited = req.IsOwnGoal ? opponent : player.TeamId;

            var goal = new Goal { MatchId = id, PlayerId = player.Id, TeamId = credited, Minute = req.Minute, IsOwnGoal = req.IsOwnGoal };
            _db.Goals.Add(goal);
            if (credited == match.HomeTeamId) match.HomeScore++; else match.AwayScore++;
            await _db.SaveChangesAsync();

            return Created201(new
            {
                goalId = goal.Id, matchId = id, playerId = player.Id, teamId = credited,
                goal.Minute, goal.IsOwnGoal, match.HomeScore, match.AwayScore,
            }, "Đã ghi bàn thắng");
        }

        [HttpDelete("goals/{goalId:int}")]
        public async Task<IActionResult> DeleteGoal(int id, int goalId)
        {
            var (match, error) = await LoadMatch(id);
            if (error != null) return error;

            var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == goalId && g.MatchId == id);
            if (goal == null) return NotFound404("Bàn thắng không tồn tại");

            if (goal.TeamId == match!.HomeTeamId) match.HomeScore = Math.Max(0, match.HomeScore - 1);
            else match.AwayScore = Math.Max(0, match.AwayScore - 1);
            _db.Goals.Remove(goal);
            await _db.SaveChangesAsync();
            return Success(new { match.HomeScore, match.AwayScore }, "Đã xóa bàn thắng");
        }

        // ------------------------------------------------------------------ Thẻ phạt

        /// <summary>Thẻ vàng thứ hai của một cầu thủ trong trận tự động sinh thẻ đỏ (BR14).</summary>
        [HttpPost("cards")]
        public async Task<IActionResult> AddCard(int id, CardRequest req)
        {
            var (match, error) = await LoadMatch(id);
            if (error != null) return error;

            var player = await _db.Players.FindAsync(req.PlayerId);
            if (player == null || (player.TeamId != match!.HomeTeamId && player.TeamId != match.AwayTeamId))
                return BadRequest400("Cầu thủ phải thuộc một trong hai đội của trận (BR10)");

            var existing = await _db.Cards.Where(c => c.MatchId == id && c.PlayerId == player.Id).ToListAsync();
            var yellows = existing.Count(c => c.CardType == CardType.Yellow);
            if (existing.Any(c => c.CardType == CardType.Red) || yellows >= 2)
                return Conflict409("Cầu thủ đã bị truất quyền thi đấu");

            var card = new Card { MatchId = id, PlayerId = player.Id, CardType = req.CardType, Minute = req.Minute };
            _db.Cards.Add(card);
            var sentOff = req.CardType == CardType.Red;
            if (req.CardType == CardType.Yellow && yellows == 1)
            {
                _db.Cards.Add(new Card { MatchId = id, PlayerId = player.Id, CardType = CardType.Red, Minute = req.Minute });
                sentOff = true;
            }
            await _db.SaveChangesAsync();

            return Created201(new { cardId = card.Id, matchId = id, playerId = player.Id, card.CardType, card.Minute, sentOff },
                sentOff && req.CardType == CardType.Yellow ? "Thẻ vàng thứ hai, tự động thêm thẻ đỏ" : "Đã ghi thẻ phạt");
        }

        [HttpDelete("cards/{cardId:int}")]
        public async Task<IActionResult> DeleteCard(int id, int cardId)
        {
            var (_, error) = await LoadMatch(id);
            if (error != null) return error;

            var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId && c.MatchId == id);
            if (card == null) return NotFound404("Thẻ phạt không tồn tại");

            _db.Cards.Remove(card);
            // Xóa thẻ vàng thứ hai thì thẻ đỏ tự sinh cùng phút cũng bị xóa.
            if (card.CardType == CardType.Yellow)
            {
                var autoRed = await _db.Cards.FirstOrDefaultAsync(c => c.MatchId == id && c.PlayerId == card.PlayerId
                    && c.CardType == CardType.Red && c.Minute == card.Minute);
                var yellowsLeft = await _db.Cards.CountAsync(c => c.MatchId == id && c.PlayerId == card.PlayerId
                    && c.CardType == CardType.Yellow && c.Id != cardId);
                if (autoRed != null && yellowsLeft < 2) _db.Cards.Remove(autoRed);
            }
            await _db.SaveChangesAsync();
            return Success(null, "Đã xóa thẻ phạt");
        }

        // ------------------------------------------------------------------ Thay người

        /// <summary>
        /// Cầu thủ ra đang trên sân, cầu thủ vào là dự bị chưa vào sân (BR21).
        /// Chỉ lưu vào Substitutions, không bao giờ sửa MatchLineups (BR20).
        /// </summary>
        [HttpPost("substitutions")]
        public async Task<IActionResult> AddSubstitution(int id, SubstitutionRequest req)
        {
            var (match, error) = await LoadMatch(id);
            if (error != null) return error;

            if (req.PlayerOutId == req.PlayerInId) return BadRequest400("Cầu thủ ra và vào phải khác nhau");
            if (req.TeamId != match!.HomeTeamId && req.TeamId != match.AwayTeamId) return BadRequest400("Đội không tham gia trận này");

            var players = await _db.Players.Where(p => p.Id == req.PlayerOutId || p.Id == req.PlayerInId).ToListAsync();
            if (players.Count != 2 || players.Any(p => p.TeamId != req.TeamId)) return BadRequest400("Hai cầu thủ phải thuộc đội thay người");

            if (!await IsOnField(id, req.PlayerOutId, req.Minute))
                return Conflict409("Cầu thủ ra không ở trên sân hoặc đã bị truất quyền (BR21)");

            var benchRow = await _db.MatchLineups.FirstOrDefaultAsync(l => l.MatchId == id && l.PlayerId == req.PlayerInId);
            if (benchRow == null || benchRow.IsStarter) return Conflict409("Cầu thủ vào phải là dự bị trong đội hình đã nộp (BR21)");
            if (await _db.Substitutions.AnyAsync(s => s.MatchId == id && s.PlayerInId == req.PlayerInId))
                return Conflict409("Cầu thủ vào đã vào sân rồi (BR21)");

            var sub = new Substitution { MatchId = id, TeamId = req.TeamId, PlayerOutId = req.PlayerOutId, PlayerInId = req.PlayerInId, Minute = req.Minute };
            _db.Substitutions.Add(sub);
            await _db.SaveChangesAsync();

            return Created201(new
            {
                sub.Id, matchId = id, sub.TeamId,
                playerOut = players.First(p => p.Id == req.PlayerOutId).FullName,
                playerIn = players.First(p => p.Id == req.PlayerInId).FullName,
                sub.Minute,
            }, "Đã ghi thay người");
        }

        [HttpDelete("substitutions/{subId:int}")]
        public async Task<IActionResult> DeleteSubstitution(int id, int subId)
        {
            var (_, error) = await LoadMatch(id);
            if (error != null) return error;

            var sub = await _db.Substitutions.FirstOrDefaultAsync(s => s.Id == subId && s.MatchId == id);
            if (sub == null) return NotFound404("Lần thay người không tồn tại");

            _db.Substitutions.Remove(sub);
            await _db.SaveChangesAsync();
            return Success(null, "Đã xóa lần thay người");
        }

        // ------------------------------------------------------------------ Hàm dùng chung

        /// <summary>Trọng tài chỉ cập nhật trận mình được phân công và khi trận đang InProgress (BR09).</summary>
        private async Task<(Match? Match, IActionResult? Error)> LoadMatch(int id)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return (null, NotFound404("Trận đấu không tồn tại"));
            if (!IsAdmin && match.RefereeId != CurrentUserId) return (null, Forbidden403("Bạn không được phân công trận này (BR09)"));
            if (match.Status != MatchStatus.InProgress) return (null, Conflict409("Trận không ở trạng thái InProgress (BR09)"));
            return (match, null);
        }

        /// <summary>
        /// Cầu thủ đang trên sân tại một phút: có trong đội hình xuất phát hoặc đã được vào thay trước đó,
        /// chưa bị thay ra và chưa nhận thẻ đỏ.
        /// </summary>
        private async Task<bool> IsOnField(int matchId, int playerId, int minute)
        {
            var lineup = await _db.MatchLineups.FirstOrDefaultAsync(l => l.MatchId == matchId && l.PlayerId == playerId);
            if (lineup == null) return false;

            var subIn = await _db.Substitutions.Where(s => s.MatchId == matchId && s.PlayerInId == playerId)
                .Select(s => (int?)s.Minute).FirstOrDefaultAsync();
            var subOut = await _db.Substitutions.Where(s => s.MatchId == matchId && s.PlayerOutId == playerId)
                .Select(s => (int?)s.Minute).FirstOrDefaultAsync();
            var red = await _db.Cards.Where(c => c.MatchId == matchId && c.PlayerId == playerId && c.CardType == CardType.Red)
                .Select(c => (int?)c.Minute).FirstOrDefaultAsync();

            var entered = lineup.IsStarter || (subIn != null && subIn <= minute);
            if (!entered) return false;
            if (subOut != null && subOut < minute) return false;
            if (red != null && red < minute) return false;
            return true;
        }

        public class GoalRequest
        {
            public int PlayerId { get; set; }
            [Range(1, 60, ErrorMessage = "Phút phải từ 1 đến 60")] public int Minute { get; set; }
            public bool IsOwnGoal { get; set; }
        }

        public class CardRequest
        {
            public int PlayerId { get; set; }
            public CardType CardType { get; set; }
            [Range(1, 60, ErrorMessage = "Phút phải từ 1 đến 60")] public int Minute { get; set; }
        }

        public class SubstitutionRequest
        {
            public int TeamId { get; set; }
            public int PlayerOutId { get; set; }
            public int PlayerInId { get; set; }
            [Range(1, 60, ErrorMessage = "Phút phải từ 1 đến 60")] public int Minute { get; set; }
        }
    }
}
