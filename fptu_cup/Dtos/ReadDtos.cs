using fptu_cup.Common.Enums;

namespace fptu_cup.Dtos
{
    // DTO chỉ-đọc dùng cho OData (/odata/...) và các API danh sách: KHÔNG trả Entity ra ngoài.
    // Mỗi DTO có Id làm khóa (OData convention) và cột tên đã "làm phẳng" (TeamName, VenueName...) để lọc/sắp xếp.

    public class TournamentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Season { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int MaxTeams { get; set; }
        public int NumberOfLegs { get; set; }
        public TournamentStatus Status { get; set; }
        public int ApprovedTeams { get; set; }
    }

    public class TeamDto
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Faculty { get; set; }
        public string? LogoUrl { get; set; }
        public string? KitColor { get; set; }
        public TeamStatus Status { get; set; }
        public string ManagerName { get; set; } = "";
        public int PlayerCount { get; set; }
        public List<PlayerDto> Players { get; set; } = new();
    }

    public class PlayerDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public int TournamentId { get; set; }
        public string StudentCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public int ShirtNumber { get; set; }
        public PlayerPosition Position { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }

    public class MatchDto
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int Round { get; set; }
        public DateTime KickoffTime { get; set; }
        public MatchStatus Status { get; set; }
        public int HomeTeamId { get; set; }
        public string HomeTeamName { get; set; } = "";
        public int AwayTeamId { get; set; }
        public string AwayTeamName { get; set; } = "";
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int? VenueId { get; set; }
        public string? VenueName { get; set; }
        public int? RefereeId { get; set; }
        public string? RefereeName { get; set; }
        public int? ForfeitTeamId { get; set; }
        public string? ForfeitReason { get; set; }
    }

    public class VenueDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public int? Capacity { get; set; }
    }
}
