// PnLService.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Tesseract;
using Microsoft.Extensions.Logging;
using TradingBot.Models;
using Microsoft.Extensions.Configuration;

namespace TradingBot.Services
{
    public class PnLService : IDisposable
    {
        private TesseractEngine? _engine;
        private readonly object _lockObj = new object();
        private bool _ocrEnabled;
        private readonly string _tessDataPath;
        private readonly ILogger<PnLService> _logger;

        public PnLService(IConfiguration config, ILogger<PnLService> logger)
        {
            _logger = logger;
            _ocrEnabled = config.GetValue<bool>("Tesseract:Enabled", false);
            _tessDataPath = config["Tesseract:DataPath"] ?? "./tessdata";
        }

        public PnLData ExtractFromImage(Stream imageStream)
        {
            lock (_lockObj)
            {
                string text = string.Empty;

                if (_ocrEnabled)
                {
                    try
                    {
                        _engine ??= new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);

                        byte[] imageData;
                        using (var ms = new MemoryStream())
                        {
                            imageStream.CopyTo(ms);
                            imageData = ms.ToArray();
                        }
                        using var pix = Pix.LoadFromMemory(imageData);
                        using var page = _engine.Process(pix);
                        text = page.GetText();
                    }
                    catch (DllNotFoundException ex)
                    {
                        _logger.LogWarning(ex, "Tesseract OCR библиотеки не найдены. OCR будет отключен.");
                        _ocrEnabled = false;
                        _engine?.Dispose();
                        _engine = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось выполнить OCR через Tesseract. OCR будет отключен.");
                        _ocrEnabled = false;
                        _engine?.Dispose();
                        _engine = null;
                    }
                }
                else
                {
                    _logger.LogDebug("OCR отключен — возврат пустых данных PnL");
                }
                try { File.WriteAllText("last_ocr.txt", text); } catch { }

                var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

                string ticker = string.Empty;
                string direction = string.Empty;
                decimal? pnlPercent = null;
                decimal? closePrice = null;
                decimal? openPrice = null;
                DateTime? tradeDate = DateTime.Now; // Устанавливаем текущую дату

                // Ищем тикер с улучшенным паттерном
                var tickerMatch = Regex.Match(text, @"([A-Z]{2,6}[/-]?USDT|[A-Z]{2,6}[/-]?USD|[A-Z]{2,6}[/-]?BTC|BTC[/-]?USDT|ETH[/-]?USDT)", RegexOptions.IgnoreCase);
                if (tickerMatch.Success)
                {
                    ticker = tickerMatch.Value.ToUpper().Replace("-", "/").Replace("USDT", "/USDT").Replace("USD", "/USD").Replace("BTC", "/BTC");
                    // Убираем дублирование слешей
                    ticker = Regex.Replace(ticker, @"/+", "/");
                }

                // Ищем направление с улучшенным поиском
                if (text.ToUpper().Contains("LONG") || text.ToUpper().Contains("BUY")) direction = "Long";
                else if (text.ToUpper().Contains("SHORT") || text.ToUpper().Contains("SELL")) direction = "Short";

                // Ищем PnL с улучшенным паттерном
                var pnlPatterns = new[]
                {
                    @"PnL[\s:]*([+\-]?\d{1,6}(?:[.,]\d{1,4})?)\s*%?",
                    @"P&L[\s:]*([+\-]?\d{1,6}(?:[.,]\d{1,4})?)\s*%?",
                    @"Profit[\s:]*([+\-]?\d{1,6}(?:[.,]\d{1,4})?)\s*%?",
                    @"([+\-]\d{1,6}(?:[.,]\d{1,4})?)\s*%",
                    @"([+\-]?\d{1,6}(?:[.,]\d{1,4})?)\s*USDT"
                };

                foreach (var pattern in pnlPatterns)
                {
                    var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var numStr = match.Groups[1].Value.Replace(",", ".").Replace(" ", "");
                        if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                        {
                            pnlPercent = val;
                            break;
                        }
                    }
                }

                // Если не нашли в именованных полях, ищем строки с + или -
                if (!pnlPercent.HasValue)
                {
                    var pnlLine = lines.FirstOrDefault(l => l.StartsWith("+") || l.StartsWith("-"));
                    if (pnlLine != null)
                    {
                        var match = Regex.Match(pnlLine, @"([+\-]?\d{1,6}(?:[.,]\d{1,4})?)");
                        if (match.Success)
                        {
                            var numStr = match.Groups[1].Value.Replace(",", ".").Replace(" ", "");
                            if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                                pnlPercent = val;
                        }
                    }
                }

                // Ищем цены закрытия и открытия
                var closeMatch = Regex.Match(text, @"Close\s*Price[\s:]*([0-9\.,]+)", RegexOptions.IgnoreCase);
                if (closeMatch.Success)
                {
                    var priceStr = closeMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
                    if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var close))
                        closePrice = close;
                }

                var openMatch = Regex.Match(text, @"(?:Avg\.?\s*)?Open\s*Price[\s:]*([0-9\.,]+)", RegexOptions.IgnoreCase);
                if (openMatch.Success)
                {
                    var openStr = openMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
                    if (decimal.TryParse(openStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var open))
                        openPrice = open;
                }

                return new PnLData
                {
                    Ticker = ticker,
                    PnLPercent = pnlPercent,
                    Close = closePrice,
                    Open = openPrice,
                    Direction = direction,
                    TradeDate = tradeDate, // Теперь устанавливаем дату
                    UserName = "unknown",
                    ReferralCode = "none"
                };
            }
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                _engine?.Dispose();
                _engine = null;
            }
        }
    }
}