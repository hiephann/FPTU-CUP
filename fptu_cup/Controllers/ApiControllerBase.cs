using fptu_cup.Common;
using Microsoft.AspNetCore.Mvc;

namespace fptu_cup.Controllers
{
    /// <summary>Lớp cha của các API controller: lấy user hiện tại và trả JSON theo dạng { success, message, data }.</summary>
    // Chỉ nhận/trả JSON thường (để formatter của OData không chen media type odata.* vào Swagger của /api).
    [Consumes("application/json")]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int CurrentUserId => int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : 0;

        protected bool IsAdmin => User.IsInRole("Admin");

        protected IActionResult Success(object? data = null, string message = "Thành công") =>
            Ok(ApiResponse<object?>.Ok(data, message));

        protected IActionResult SuccessPaged<T>(List<T> items, int total, PagedQuery query) =>
            Success(PagedResult<T>.Create(items, total, query));

        protected IActionResult Created201(object? data, string message = "Tạo thành công") =>
            StatusCode(201, ApiResponse<object?>.Ok(data, message));

        protected IActionResult Error(int status, string message) =>
            StatusCode(status, ApiResponse<object>.Fail(message));

        protected IActionResult BadRequest400(string message) => Error(400, message);
        protected IActionResult Forbidden403(string message = "Bạn không có quyền thực hiện thao tác này") => Error(403, message);
        protected IActionResult NotFound404(string message = "Không tìm thấy dữ liệu") => Error(404, message);
        protected IActionResult Conflict409(string message) => Error(409, message);
    }
}
