using fptu_cup.Common.Enums;
using fptu_cup.Data;
using fptu_cup.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Controllers.OData
{
    // OData (chỉ đọc) cho các nghiệp vụ tra cứu: lọc, sắp xếp, chọn cột, phân trang, đếm, mở rộng quan hệ.
    // Ví dụ:
    //   /odata/Players?$filter=TournamentId eq 1 and (contains(FullName,'an') or contains(StudentCode,'HE19'))&$orderby=FullName&$top=12&$count=true
    //   /odata/Matches?$filter=TournamentId eq 1 and Status eq 'Finished'&$orderby=KickoffTime desc&$select=Round,HomeTeamName,HomeScore,AwayScore,AwayTeamName
    //   /odata/Teams?$filter=TournamentId eq 1&$expand=Players($filter=Position eq 'GK')
    // Dữ liệu trả về là DTO (Dtos/ReadDtos.cs), không trả Entity. Ghi dữ liệu vẫn đi qua /api/... để kiểm tra BR.
    // Tên controller có hậu tố "OData" để không trùng với API controller cùng tên; route khai báo bằng attribute.

    internal static class ODataLimits
    {
        public const int MaxTop = 50;
        public const int PageSize = 50;
        public const AllowedQueryOptions Allowed = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy | AllowedQueryOptions.Select
            | AllowedQueryOptions.Top | AllowedQueryOptions.Skip | AllowedQueryOptions.Count | AllowedQueryOptions.Expand;
    }

    public class TournamentsODataController : ODataController
    {
        private readonly FptuCupDbContext _db;
        public TournamentsODataController(FptuCupDbContext db) => _db = db;

        [HttpGet("odata/Tournaments")]
        [EnableQuery(MaxTop = ODataLimits.MaxTop, PageSize = ODataLimits.PageSize, AllowedQueryOptions = ODataLimits.Allowed)]
        public IQueryable<TournamentDto> Get() => _db.Tournaments.AsNoTracking().Select(DtoProjections.Tournament);

        [HttpGet("odata/Tournaments({key})")]
        [EnableQuery(AllowedQueryOptions = ODataLimits.Allowed)]
        public SingleResult<TournamentDto> Get(int key) =>
            SingleResult.Create(_db.Tournaments.AsNoTracking().Where(t => t.Id == key).Select(DtoProjections.Tournament));
    }

    /// <summary>Khách chỉ thấy đội đã duyệt; Admin thấy mọi đội (giống GET /api/tournaments/{id}/teams).</summary>
    public class TeamsODataController : ODataController
    {
        private readonly FptuCupDbContext _db;
        public TeamsODataController(FptuCupDbContext db) => _db = db;

        private IQueryable<TeamDto> Query()
        {
            var q = _db.Teams.AsNoTracking();
            if (!User.IsInRole("Admin")) q = q.Where(t => t.Status == TeamStatus.Approved);
            return q.Select(DtoProjections.Team);
        }

        [HttpGet("odata/Teams")]
        [EnableQuery(MaxTop = ODataLimits.MaxTop, PageSize = ODataLimits.PageSize, AllowedQueryOptions = ODataLimits.Allowed, MaxExpansionDepth = 1)]
        public IQueryable<TeamDto> Get() => Query();

        [HttpGet("odata/Teams({key})")]
        [EnableQuery(AllowedQueryOptions = ODataLimits.Allowed, MaxExpansionDepth = 1)]
        public SingleResult<TeamDto> Get(int key) => SingleResult.Create(Query().Where(t => t.Id == key));
    }

    /// <summary>Tra cứu cầu thủ của các đội đã duyệt (FR13).</summary>
    public class PlayersODataController : ODataController
    {
        private readonly FptuCupDbContext _db;
        public PlayersODataController(FptuCupDbContext db) => _db = db;

        private IQueryable<PlayerDto> Query() =>
            _db.Players.AsNoTracking().Where(p => p.Team!.Status == TeamStatus.Approved).Select(DtoProjections.Player);

        [HttpGet("odata/Players")]
        [EnableQuery(MaxTop = ODataLimits.MaxTop, PageSize = ODataLimits.PageSize, AllowedQueryOptions = ODataLimits.Allowed)]
        public IQueryable<PlayerDto> Get() => Query();

        [HttpGet("odata/Players({key})")]
        [EnableQuery(AllowedQueryOptions = ODataLimits.Allowed)]
        public SingleResult<PlayerDto> Get(int key) => SingleResult.Create(Query().Where(p => p.Id == key));
    }

    /// <summary>Lịch thi đấu và kết quả (FR21, FR31).</summary>
    public class MatchesODataController : ODataController
    {
        private readonly FptuCupDbContext _db;
        public MatchesODataController(FptuCupDbContext db) => _db = db;

        [HttpGet("odata/Matches")]
        [EnableQuery(MaxTop = ODataLimits.MaxTop, PageSize = ODataLimits.PageSize, AllowedQueryOptions = ODataLimits.Allowed)]
        public IQueryable<MatchDto> Get() => _db.Matches.AsNoTracking().Select(DtoProjections.Match);

        [HttpGet("odata/Matches({key})")]
        [EnableQuery(AllowedQueryOptions = ODataLimits.Allowed)]
        public SingleResult<MatchDto> Get(int key) =>
            SingleResult.Create(_db.Matches.AsNoTracking().Where(m => m.Id == key).Select(DtoProjections.Match));
    }

    public class VenuesODataController : ODataController
    {
        private readonly FptuCupDbContext _db;
        public VenuesODataController(FptuCupDbContext db) => _db = db;

        [HttpGet("odata/Venues")]
        [EnableQuery(MaxTop = ODataLimits.MaxTop, PageSize = ODataLimits.PageSize, AllowedQueryOptions = ODataLimits.Allowed)]
        public IQueryable<VenueDto> Get() => _db.Venues.AsNoTracking().Select(DtoProjections.Venue);

        [HttpGet("odata/Venues({key})")]
        [EnableQuery(AllowedQueryOptions = ODataLimits.Allowed)]
        public SingleResult<VenueDto> Get(int key) =>
            SingleResult.Create(_db.Venues.AsNoTracking().Where(v => v.Id == key).Select(DtoProjections.Venue));
    }
}
