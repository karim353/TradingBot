using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TradingBot.Models;
using System.Linq;

namespace TradingBot.Services
{
	/// <summary>
	/// Фоновый сервис, отвечающий за запуск Telegram-бота и получение сообщений
	/// </summary>
	public class BotService : BackgroundService
	{
		private readonly ITelegramBotClient _botClient;
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<BotService> _logger;

		public BotService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<BotService> logger)
		{
			_botClient = botClient;
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// Регистрация команд
			await _botClient.SetMyCommands(new[]
			{
				new BotCommand { Command = "start", Description = "🚀 Запуск бота и обучение" },
				new BotCommand { Command = "menu", Description = "📋 Главное меню" },
				new BotCommand { Command = "help", Description = "🆘 Помощь" }
			}, cancellationToken: stoppingToken);

			_botClient.StartReceiving(
				// Обработчик входящего обновления
				async (bot, update, ct) =>
				{
					try
					{
						using var scope = _serviceProvider.CreateScope();
						var handler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
						await handler.HandleUpdateAsync(bot, update, ct);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Ошибка при обработке обновления");
					}
				},
				// Обработчик ошибок при получении обновлений
				(bot, exception, ct) =>
				{
					_logger.LogError(exception, "Ошибка при получении обновления от Telegram.");
					return Task.CompletedTask;
				},
				cancellationToken: stoppingToken
			);

			_logger.LogInformation("Telegram bot started receiving updates.");

			// Держим сервис активным, пока не будет отмены.
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}
	}

	/// <summary>
	/// Сервис для генерации еженедельных отчетов
	/// </summary>
	public class ReportService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<ReportService> _logger;

		public ReportService(IServiceProvider serviceProvider, ILogger<ReportService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Weekly report service is running.");
			while (!stoppingToken.IsCancellationRequested)
			{
				var now = DateTime.Now;
				int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
				if (daysUntilMonday == 0) daysUntilMonday = 7;
				var nextMonday = now.Date.AddDays(daysUntilMonday).AddHours(9);
				await Task.Delay(nextMonday - now, stoppingToken);

				try
				{
					using var scope = _serviceProvider.CreateScope();
					var repo = scope.ServiceProvider.GetRequiredService<TradeRepository>();
					var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
					var userIds = await repo.GetAllUserIdsAsync();

					foreach (var uid in userIds)
					{
						var trades = await repo.GetTradesInDateRangeAsync(uid, DateTime.Now.AddDays(-7), DateTime.Now);
						if (!trades.Any()) continue;

						decimal totalPnL = trades.Sum(t => t.PnL);
						int totalTrades = trades.Count;
						int profitable = trades.Count(t => t.PnL > 0);
						decimal winRate = totalTrades > 0 ? (decimal)profitable / totalTrades * 100 : 0;
						string report = $"📅 Еженедельный отчёт:\nСделок: {totalTrades}\nPnL: {totalPnL:F2}%\nWinrate: {winRate:F2}%";

						var plt = new ScottPlot.Plot();
						double cumulative = 0;
						var xs = new List<double>();
						var ys = new List<double>();
						trades.Sort((a, b) => a.Date.CompareTo(b.Date));
						for (int i = 0; i < trades.Count; i++)
						{
							cumulative += (double)trades[i].PnL;
							xs.Add(i);
							ys.Add(cumulative);
						}
						plt.Add.Scatter(xs, ys);
						plt.Title("📊 Кривая эквити за неделю");
						string tmpPng = Path.Combine(Path.GetTempPath(), $"weekly_{uid}.png");
						plt.SavePng(tmpPng, 600, 400);
						using var fs = new FileStream(tmpPng, FileMode.Open, FileAccess.Read);
						await bot.SendPhoto(uid, InputFile.FromStream(fs), caption: report, cancellationToken: stoppingToken);
						File.Delete(tmpPng);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in weekly report generation");
				}
			}
		}
	}

	/// <summary>
	/// Ежедневная сводка пользователям (время и включение управляются UserSettings)
	/// </summary>
	public class DailySummaryService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<DailySummaryService> _logger;

		public DailySummaryService(IServiceProvider serviceProvider, ILogger<DailySummaryService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("DailySummaryService running");
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var now = DateTime.Now;
					var next = now.Date.AddDays(1).AddHours(21); // 21:00 локального времени
					if (now < next)
						await Task.Delay(next - now, stoppingToken);
					else
						await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

					using var scope = _serviceProvider.CreateScope();
					var repo = scope.ServiceProvider.GetRequiredService<TradeRepository>();
					var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
					var settingsSvc = scope.ServiceProvider.GetRequiredService<UserSettingsService>();

					var userIds = await repo.GetAllUserIdsAsync();
					foreach (var uid in userIds)
					{
						var settings = await settingsSvc.GetUserSettingsAsync(uid);
						if (settings?.DailySummaryEnabled != true) continue;

						var trades = await repo.GetTradesInDateRangeAsync(uid, DateTime.Now.Date.AddDays(-1), DateTime.Now.Date);
						int total = trades.Count;
						decimal totalPnL = trades.Sum(t => t.PnL);
						int wins = trades.Count(t => t.PnL > 0);
						decimal winRate = total > 0 ? (decimal)wins / total * 100 : 0;
						string summary = $"📅 Итог дня:\nСделок: {total}\nPnL: {totalPnL:F2}%\nWinRate: {winRate:F2}%";
						await bot.SendMessage(uid, summary, cancellationToken: stoppingToken);
					}
				}
				catch (TaskCanceledException) { }
				catch (Exception ex)
				{
					_logger.LogError(ex, "DailySummaryService error");
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
			}
		}
	}
}
