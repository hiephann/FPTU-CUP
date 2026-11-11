using System.Linq.Expressions;
using fptu_cup.Common.Enums;
using fptu_cup.Entities;

namespace fptu_cup.Dtos
{
    /// <summary>
    /// Phép chiếu Entity → DTO viết bằng Expression để EF Core dịch thẳng sang SQL
    /// (OData áp $filter/$orderby/$top/$skip lên IQueryable&lt;Dto&gt; trước khi truy vấn).
    /// </summary>
    public static class DtoProjections
    {
        public static readonly Expression<Func<Tournament, TournamentDto>> Tournament = t => new TournamentDto
        {
            Id = t.Id,
            Name = t.Name,
            Season = t.Season,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            MaxTeams = t.MaxTeams,
            NumberOfLegs = t.NumberOfLegs,
            Status = t.Status,
            ApprovedTeams = t.Teams.Count(x => x.Status == TeamStatus.Approved),
        };

        public static readonly Expression<Func<Player, PlayerDto>> Player = p => new PlayerDto
        {
            Id = p.Id,
            TeamId = p.TeamId,
            TeamName = p.Team!.Name,
            TournamentId = p.Team.TournamentId,
            StudentCode = p.StudentCode,
            FullName = p.FullName,
            ShirtNumber = p.ShirtNumber,
            Position = p.Position,
            DateOfBirth = p.DateOfBirth,
        };

        public static readonly Expression<Func<Team, TeamDto>> Team = t => new TeamDto
        {
            Id = t.Id,
            TournamentId = t.TournamentId,
            TournamentName = t.Tournament!.Name,
            Name = t.Name,
            Faculty = t.Faculty,
            LogoUrl = t.LogoUrl,
            KitColor = t.KitColor,
            Status = t.Status,
            ManagerName = t.Manager!.FullName,
            PlayerCount = t.Players.Count,
            Players = t.Players.OrderBy(p => p.ShirtNumber).Select(p => new PlayerDto
            {
                Id = p.Id,
                TeamId = p.TeamId,
                TeamName = t.Name,
                TournamentId = t.TournamentId,
                StudentCode = p.StudentCode,
                FullName = p.FullName,
                ShirtNumber = p.ShirtNumber,
                Position = p.Position,
                DateOfBirth = p.DateOfBirth,
            }).ToList(),
        };

        public static readonly Expression<Func<Match, MatchDto>> Match = m => new MatchDto
        {
            Id = m.Id,
            TournamentId = m.TournamentId,
            Round = m.Round,
            KickoffTime = m.KickoffTime,
            Status = m.Status,
            HomeTeamId = m.HomeTeamId,
            HomeTeamName = m.HomeTeam!.Name,
            AwayTeamId = m.AwayTeamId,
            AwayTeamName = m.AwayTeam!.Name,
            HomeScore = m.HomeScore,
            AwayScore = m.AwayScore,
            VenueId = m.VenueId,
            VenueName = m.Venue != null ? m.Venue.Name : null,
            RefereeId = m.RefereeId,
            RefereeName = m.Referee != null ? m.Referee.FullName : null,
            ForfeitTeamId = m.ForfeitTeamId,
            ForfeitReason = m.ForfeitReason,
        };

        public static readonly Expression<Func<Venue, VenueDto>> Venue = v => new VenueDto
        {
            Id = v.Id,
            Name = v.Name,
            Address = v.Address,
            Capacity = v.Capacity,
        };
    
        private static readonly Func<Venue, VenueDto> VenueCompiled = Venue.Compile();

        /// <summary>Chuyển Entity đã nạp trong bộ nhớ sang DTO (dùng sau khi thêm/sửa).</summary>
        public static VenueDto ToDto(this Venue v) => VenueCompiled(v);
    }
}
