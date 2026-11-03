using System.Text.Json;
using fptu_cup.Common;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace fptu_cup.Middlewares
{
    /// <summary>Bắt lỗi không lường trước, trả 500 dạng JSON { success, message, data } và không lộ stack trace.</summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly JsonSerializerOptions _json;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IOptions<JsonOptions> json)
        {
            _next = next;
            _logger = logger;
            _json = json.Value.SerializerOptions;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                _logger.LogError(ex, "Lỗi khi xử lý {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.Clear();
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(ApiResponse<object>.Fail("Lỗi hệ thống, vui lòng thử lại sau"), _json));
            }
        }
    }
}
