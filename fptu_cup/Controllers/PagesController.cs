using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fptu_cup.Controllers
{
    /// <summary>
    /// Các trang giao diện (MVC). Không có logic: mỗi view là khung HTML, dữ liệu lấy từ /api/... bằng fetch.
    /// Trang quản lý được chặn bằng ASP.NET Core Identity (cookie, xem AccountController) theo vai trò;
    /// dữ liệu và kiểm tra nghiệp vụ vẫn do API (JWT) xử lý.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PagesController : Controller
    {
        // Công khai
        [HttpGet("/")] public IActionResult Home() => View();
        [HttpGet("/schedule")] public IActionResult Schedule() => View();
        [HttpGet("/standings")] public IActionResult Standings() => View();
        [HttpGet("/matches/{id:int}")] public IActionResult MatchDetail(int id) => View();
        [HttpGet("/tournaments")] public IActionResult TournamentList() => View();
        [HttpGet("/teams")] public IActionResult Teams() => View();
        [HttpGet("/teams/{id:int}")] public IActionResult TeamDetail(int id) => View();
        [HttpGet("/players")] public IActionResult Players() => View();
        [HttpGet("/players/{id:int}")] public IActionResult PlayerDetail(int id) => View();

        // Mọi người dùng đã đăng nhập: hồ sơ + đổi mật khẩu
        [Authorize][HttpGet("/account")] public IActionResult Account() => View();

        // Admin
        [Authorize(Roles = "Admin")][HttpGet("/admin")] public IActionResult AdminDashboard() => View();
        [Authorize(Roles = "Admin")][HttpGet("/admin/tournaments")] public IActionResult AdminTournaments() => View();
        [Authorize(Roles = "Admin")][HttpGet("/admin/teams")] public IActionResult AdminTeams() => View();
        [Authorize(Roles = "Admin")][HttpGet("/admin/matches")] public IActionResult AdminMatches() => View();
        [Authorize(Roles = "Admin")][HttpGet("/admin/venues")] public IActionResult AdminVenues() => View();
        [Authorize(Roles = "Admin")][HttpGet("/admin/users")] public IActionResult AdminUsers() => View();

        // Quản lý đội
        [Authorize(Roles = "TeamManager,Admin")][HttpGet("/manager/team")] public IActionResult ManagerTeam() => View();
        [Authorize(Roles = "TeamManager,Admin")][HttpGet("/manager/lineup/{matchId:int}")] public IActionResult ManagerLineup(int matchId) => View();

        // Trọng tài
        [Authorize(Roles = "Referee,Admin")][HttpGet("/referee/matches")] public IActionResult RefereeMatches() => View();
        [Authorize(Roles = "Referee,Admin")][HttpGet("/referee/matches/{id:int}")] public IActionResult RefereeConsole(int id) => View();
    }
}
