using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;

namespace TradingBot.Services;

public class HealthCheckService : IHealthCheck
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly string _connectionString;

    public HealthCheckService(ILogger<HealthCheckService> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверка подключения к базе данных
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Проверка доступности таблиц
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
            var tableCount = await command.ExecuteScalarAsync(cancellationToken);
            
            if (tableCount == null || Convert.ToInt32(tableCount) < 3)
            {
                _logger.LogWarning("Недостаточно таблиц в базе данных: {TableCount}", tableCount);
                return HealthCheckResult.Degraded("Недостаточно таблиц в базе данных");
            }

            // Проверка размера базы данных
            var fileInfo = new FileInfo(_connectionString.Replace("Data Source=", ""));
            if (fileInfo.Exists && fileInfo.Length > 100 * 1024 * 1024) // 100 MB
            {
                _logger.LogWarning("База данных превышает рекомендуемый размер: {Size} MB", fileInfo.Length / (1024 * 1024));
                return HealthCheckResult.Degraded("База данных превышает рекомендуемый размер");
            }

            _logger.LogInformation("Health check пройден успешно");
            return HealthCheckResult.Healthy("Система работает нормально");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check не пройден");
            return HealthCheckResult.Unhealthy("Ошибка подключения к базе данных", ex);
        }
    }
}
