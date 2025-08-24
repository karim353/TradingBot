// PnLService.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Tesseract;
using TradingBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace TradingBot.Services
{
    public class PnLService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PnLService> _logger;
        private TesseractEngine? _engine;
        private readonly object _lockObj = new object();
        private readonly bool _ocrEnabled;

        public PnLService(IConfiguration config, ILogger<PnLService> logger)
        {
            _configuration = config;
            _logger = logger;

            // Allow disabling OCR via configuration; default to true on Windows, false on non-Windows to avoid missing native libs by default
            bool defaultEnabled = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _ocrEnabled = config.GetValue("Tesseract:Enabled", defaultEnabled);
            if (!_ocrEnabled)
            {
                _logger.LogInformation("OCR (Tesseract) is disabled via configuration. PnL extraction from images will be skipped.");
            }
        }

        public PnLData ExtractFromImage(Stream imageStream)
        {
            lock (_lockObj)
            {
                if (!_ocrEnabled)
                {
                    _logger.LogWarning("OCR is disabled. Returning empty PnL data.");
                    return new PnLData
                    {
                        Ticker = string.Empty,
                        PnLPercent = null,
                        Close = null,
                        Open = null,
                        Direction = string.Empty,
                        TradeDate = DateTime.Now,
                        UserName = "unknown",
                        ReferralCode = "none"
                    };
                }

                EnsureEngineInitialized();
                if (_engine == null)
                {
                    _logger.LogError("Tesseract engine is not available. Returning empty PnL data.");
                    return new PnLData
                    {
                        Ticker = string.Empty,
                        PnLPercent = null,
                        Close = null,
                        Open = null,
                        Direction = string.Empty,
                        TradeDate = DateTime.Now,
                        UserName = "unknown",
                        ReferralCode = "none"
                    };
                }

                byte[] imageData;
                using (var ms = new MemoryStream())
                {
                    imageStream.CopyTo(ms);
                    imageData = ms.ToArray();
                }
                using var pix = Pix.LoadFromMemory(imageData);
                using var page = _engine.Process(pix);
                string text = page.GetText();
                File.WriteAllText("last_ocr.txt", text); // Для отладки

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

        private void EnsureEngineInitialized()
        {
            if (_engine != null)
                return;

            try
            {
                string? configuredPath = _configuration["Tesseract:DataPath"];
                string dataPath = ResolveTessdataPath(configuredPath);
                _engine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
                _logger.LogInformation("Tesseract engine initialized with tessdata at {DataPath}", dataPath);
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogError(ex, "Native libraries for Tesseract/Leptonica not found. Install system packages or disable OCR. On Fedora: 'sudo dnf install -y tesseract tesseract-langpack-eng'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Tesseract engine. OCR will be unavailable.");
            }
        }

        private string ResolveTessdataPath(string? configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath) && Directory.Exists(configuredPath))
                return configuredPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var windowsCandidates = new[]
                {
                    @"C:\\Program Files\\Tesseract-OCR\\tessdata",
                    @"C:\\Program Files (x86)\\Tesseract-OCR\\tessdata"
                };
                var foundWin = windowsCandidates.FirstOrDefault(Directory.Exists);
                if (!string.IsNullOrEmpty(foundWin)) return foundWin;
            }
            else
            {
                var linuxCandidates = new[]
                {
                    "/usr/share/tessdata",
                    "/usr/share/tesseract-ocr/4.00/tessdata",
                    "/usr/share/tesseract-ocr/tessdata",
                    "/usr/local/share/tessdata"
                };
                var foundLinux = linuxCandidates.FirstOrDefault(Directory.Exists);
                if (!string.IsNullOrEmpty(foundLinux)) return foundLinux;
            }

            // Fallback to local folder
            return "./tessdata";
        }
    }
}