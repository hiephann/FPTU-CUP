using fptu_cup.Controllers;
using fptu_cup.Data;

namespace fptu_cup.Common
{
    /// <summary>
    /// Chạy nền mỗi phút: xử thua / hủy các trận quá hạn nộp đội hình (BR23).
    /// Logic nằm ở MatchesController.ProcessOverdueLineups; Admin cũng có thể gọi tay qua POST /api/matches/process-forfeits.
    /// </summary>
    public class ForfeitWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ForfeitWorker> _logger;

        public ForfeitWorker(IServiceScopeFactory scopes, ILogger<ForfeitWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // chờ migrate + seed xong
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<FptuCupDbContext>();
                        var done = await MatchesController.ProcessOverdueLineups(db, VietnamTime.Now);
                        if (done.Count > 0) _logger.LogInformation("Đã xử lý {Count} trận quá hạn nộp đội hình", done.Count);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Lỗi khi xử lý trận quá hạn nộp đội hình");
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // app đang tắt
            }
        }
    }
}
