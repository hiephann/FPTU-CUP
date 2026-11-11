using System.ComponentModel.DataAnnotations;
using fptu_cup.Common;
using fptu_cup.Data;
using fptu_cup.Dtos;
using fptu_cup.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Controllers
{
    /// <summary>FR15: CRUD sân thi đấu (xem công khai, sửa chỉ Admin).</summary>
    [ApiController]
    [Route("api/venues")]
    public class VenuesController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        public VenuesController(FptuCupDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PagedQuery query)
        {
            var q = _db.Venues.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(v => v.Name.Contains(s) || (v.Address != null && v.Address.Contains(s)));
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(v => v.Name).Skip(query.Skip).Take(query.PageSize)
                .Select(DtoProjections.Venue)
                .ToListAsync();
            return SuccessPaged(items, total, query);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var v = await _db.Venues.Where(x => x.Id == id).Select(DtoProjections.Venue).FirstOrDefaultAsync();
            return v == null ? NotFound404("Sân không tồn tại") : Success(v);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(VenueRequest req)
        {
            var v = new Venue { Name = req.Name.Trim(), Address = req.Address?.Trim(), Capacity = req.Capacity };
            _db.Venues.Add(v);
            await _db.SaveChangesAsync();
            return Created201(v.ToDto(), "Thêm sân thành công");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, VenueRequest req)
        {
            var v = await _db.Venues.FindAsync(id);
            if (v == null) return NotFound404("Sân không tồn tại");

            v.Name = req.Name.Trim();
            v.Address = req.Address?.Trim();
            v.Capacity = req.Capacity;
            await _db.SaveChangesAsync();
            return Success(v.ToDto(), "Cập nhật sân thành công");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _db.Venues.FindAsync(id);
            if (v == null) return NotFound404("Sân không tồn tại");
            if (await _db.Matches.AnyAsync(m => m.VenueId == id)) return Conflict409("Sân đã được dùng trong lịch thi đấu");

            _db.Venues.Remove(v);
            await _db.SaveChangesAsync();
            return Success(null, "Đã xóa sân");
        }

        public class VenueRequest
        {
            [Required, StringLength(100)] public string Name { get; set; } = "";
            [StringLength(255)] public string? Address { get; set; }
            [Range(1, 100000)] public int? Capacity { get; set; }
        }
    }
}
