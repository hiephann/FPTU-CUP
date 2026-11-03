using Microsoft.AspNetCore.Mvc;

namespace fptu_cup.Controllers
{
    /// <summary>
    /// Các trang giao diện (MVC). Không có logic: mỗi view là khung HTML, dữ liệu lấy từ /api/... bằng fetch.
    /// Đăng nhập, phân quyền theo vai trò và dữ liệu đều do API xử lý.
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
        [HttpGet("/login")] public IActionResult Login() => View();
        [HttpGet("/register")] public IActionResult Register() => View();

        // Mọi người dùng đã đăng nhập: hồ sơ + đổi mật khẩu
        [HttpGet("/account")] public IActionResult Account() => View();

        // Admin
        [HttpGet("/admin")] public IActionResult AdminDashboard() => View();
        [HttpGet("/admin/tournaments")] public IActionResult AdminTournaments() => View();
        [HttpGet("/admin/teams")] public IActionResult AdminTeams() => View();
        [HttpGet("/admin/matches")] public IActionResult AdminMatches() => View();
        [HttpGet("/admin/venues")] public IActionResult AdminVenues() => View();
        [HttpGet("/admin/users")] public IActionResult AdminUsers() => View();

        // Quản lý đội
        [HttpGet("/manager/team")] public IActionResult ManagerTeam() => View();
        [HttpGet("/manager/lineup/{matchId:int}")] public IActionResult ManagerLineup(int matchId) => View();

        // Trọng tài
        [HttpGet("/referee/matches")] public IActionResult RefereeMatches() => View();
        [HttpGet("/referee/matches/{id:int}")] public IActionResult RefereeConsole(int id) => View();
    }
}
