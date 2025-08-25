// PnLService.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Tesseract;
using TradingBot.Models;
using Microsoft.Extensions.Configuration;

namespace TradingBot.Services
{
    public class PnLService
    {
        private readonly TesseractEngine _engine;
        private readonly object _lockObj = new object();

        public PnLService(IConfiguration config)
        {
            string tessDataPath = config["Tesseract:DataPath"] ?? "./tessdata";
            _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
        }

        public PnLData ExtractFromImage(Stream imageStream)
        {
            lock (_lockObj)
            {
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

        /// <summary>
        /// Извлекает торговые данные из произвольного текста пользователя (быстрое добавление)
        /// Поддерживаемые примеры:
        ///  - "BTC/USDT long +2.3%"
        ///  - "ETHUSDT short -10%"
        ///  - "AAPL buy 1.2%"
        ///  - "BTC/USDT +25$"
        /// </summary>
        public PnLData ExtractFromText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
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

            string text = rawText.Trim();

            // Поиск тикера (разрешаем форматы TICKER/USDT, TICKERUSDT, TICKER-USD и просто TICKER)
            string ticker = string.Empty;
            var tickerMatch = Regex.Match(text, @"([A-Z]{2,12}(?:\s*/\s*|\s*-\s*|)USDT|[A-Z]{2,12}(?:\s*/\s*|\s*-\s*|)USD|[A-Z]{2,12}/BTC|BTC/USDT|ETH/USDT|[A-Z]{2,12})", RegexOptions.IgnoreCase);
            if (tickerMatch.Success)
            {
                ticker = tickerMatch.Value.ToUpper()
                    .Replace(" ", "")
                    .Replace("-", "/");

                if (!ticker.Contains('/') && (ticker.EndsWith("USDT") || ticker.EndsWith("USD") || ticker.EndsWith("BTC")))
                {
                    if (ticker.EndsWith("USDT")) ticker = ticker.Replace("USDT", "/USDT");
                    else if (ticker.EndsWith("USD")) ticker = ticker.Replace("USD", "/USD");
                    else if (ticker.EndsWith("BTC")) ticker = ticker.Replace("BTC", "/BTC");
                }
            }

            // Поиск направления
            string direction = string.Empty;
            var upper = text.ToUpperInvariant();
            if (upper.Contains("LONG") || upper.Contains("BUY")) direction = "Long";
            else if (upper.Contains("SHORT") || upper.Contains("SELL")) direction = "Short";

            // Поиск PnL (в процентах или в валюте)
            decimal? pnlPercent = null;
            var pnlPatterns = new[]
            {
                @"([+\-]?\d{1,6}(?:[\.,]\d{1,4})?)\s*%",
                @"PnL[\s:]*([+\-]?\d{1,6}(?:[\.,]\d{1,4})?)\s*%?",
                @"P&L[\s:]*([+\-]?\d{1,6}(?:[\.,]\d{1,4})?)\s*%?",
                @"([+\-]?\d{1,6}(?:[\.,]\d{1,4})?)\s*(?:USDT|USD|\$)"
            };

            foreach (var pattern in pnlPatterns)
            {
                var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var raw = m.Groups[1].Value.Replace(',', '.');
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    {
                        pnlPercent = val;
                        break;
                    }
                }
            }

            return new PnLData
            {
                Ticker = ticker,
                PnLPercent = pnlPercent,
                Direction = direction,
                Open = null,
                Close = null,
                TradeDate = DateTime.Now,
                UserName = "unknown",
                ReferralCode = "none"
            };
        }
    }
}