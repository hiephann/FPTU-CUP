using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using fptu_cup.Common;
using fptu_cup.Data;
using fptu_cup.Data.Seed;
using fptu_cup.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace fptu_cup
{
    public class Program
    {
        private static readonly JsonSerializerOptions ErrorJson = new(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // API controllers + MVC views (giao diện chỉ là khung HTML gọi API bằng fetch)
            builder.Services.AddControllersWithViews()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
                .ConfigureApiBehaviorOptions(o =>
                {
                    // Lỗi validation (Data Annotations) -> 400 dạng { success, message, data }
                    o.InvalidModelStateResponseFactory = ctx =>
                    {
                        var errors = ctx.ModelState
                            .Where(e => e.Value?.Errors.Count > 0)
                            .ToDictionary(
                                e => JsonNamingPolicy.CamelCase.ConvertName(e.Key),
                                e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Giá trị không hợp lệ" : x.ErrorMessage).ToArray());
                        return new BadRequestObjectResult(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", errors));
                    };
                });

            // Giữ nguyên ký tự tiếng Việt trong HTML
            builder.Services.AddWebEncoders(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

            builder.Services.AddDbContext<FptuCupDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Xử thua các trận quá hạn nộp đội hình (BR23)
            builder.Services.AddHostedService<ForfeitWorker>();

            // JWT: đọc Jwt:Issuer, Jwt:Audience, Jwt:Key từ appsettings / user-secrets
            var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
                throw new InvalidOperationException("Jwt:Key phải có tối thiểu 32 ký tự (đặt trong appsettings / user-secrets).");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.MapInboundClaims = false; // giữ nguyên tên claim: sub, name, role
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = "name",
                        RoleClaimType = "role",
                    };
                    o.Events = new JwtBearerEvents
                    {
                        OnChallenge = ctx =>
                        {
                            ctx.HandleResponse();
                            return WriteError(ctx.HttpContext, HttpStatusCode.Unauthorized, "Thiếu hoặc sai token đăng nhập");
                        },
                        OnForbidden = ctx => WriteError(ctx.HttpContext, HttpStatusCode.Forbidden, "Bạn không có quyền thực hiện thao tác này"),
                    };
                });
            builder.Services.AddAuthorization();

            // Swagger có nút Authorize để nhập JWT
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo { Title = "FPTU Cup API", Version = "v1" });
                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập JWT nhận từ /api/auth/login (không cần gõ chữ 'Bearer').",
                });
                o.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        Array.Empty<string>()
                    },
                });
                o.CustomSchemaIds(t => t.FullName!.Replace("fptu_cup.Controllers.", "").Replace("+", "."));
            });

            var app = builder.Build();

            app.UseMiddleware<ExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                // Môi trường dev: tự áp migration và nạp dữ liệu mẫu để chạy được ngay.
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FptuCupDbContext>();
                await db.Database.MigrateAsync();
                await DbSeeder.SeedAsync(db, app.Configuration);
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }

        private static Task WriteError(HttpContext context, HttpStatusCode status, string message)
        {
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json; charset=utf-8";
            return context.Response.WriteAsync(JsonSerializer.Serialize(ApiResponse<object>.Fail(message), ErrorJson));
        }
    }
}
