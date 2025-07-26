using System;
using System.IO;
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
        File.WriteAllText("last_ocr.txt", text);

        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

        string ticker = string.Empty;
        string direction = string.Empty;
        decimal? pnlPercent = null;
        decimal? closePrice = null;
        decimal? openPrice = null;

        // Тикер
        var tickerMatch = Regex.Match(text, @"([A-Z]{3,6}USDT|[A-Z]{3,6}USD|[A-Z]{3,6}BTC)", RegexOptions.IgnoreCase);
        if (tickerMatch.Success)
            ticker = tickerMatch.Value.ToUpper();

        // Направление
        if (text.ToUpper().Contains("LONG")) direction = "Long";
        else if (text.ToUpper().Contains("SHORT")) direction = "Short";

        // PnL Percent (ищем любую строку с плюсом/минусом и числом)
        var pnlLine = lines.FirstOrDefault(l => l.StartsWith("+") || l.StartsWith("-"));
        if (pnlLine != null)
        {
            // берем только число, даже если нет %
            var match = Regex.Match(pnlLine, @"([+\-]?\d{1,6}([.,]\d{1,4})?)");
            if (match.Success)
            {
                var numStr = match.Groups[1].Value.Replace(",", ".").Replace(" ", "");
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                {
                    pnlPercent = val;
                }
            }
        }
        if (!pnlPercent.HasValue)
            pnlPercent = 0;

        // Close Price
        var closeMatch = Regex.Match(text, @"Close Price[\s:]+([0-9\.,]+)", RegexOptions.IgnoreCase);
        if (closeMatch.Success)
        {
            var priceStr = closeMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var close))
                closePrice = close;
        }

        // Open Price
        var openMatch = Regex.Match(text, @"Avg\.?\s*Open Price[\s:]+([0-9\.,]+)", RegexOptions.IgnoreCase);
        if (!openMatch.Success)
            openMatch = Regex.Match(text, @"Open Price[\s:]+([0-9\.,]+)", RegexOptions.IgnoreCase);

        if (openMatch.Success)
        {
            var openStr = openMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(openStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var open))
                openPrice = open;
        }

        return new PnLData
        {
            Ticker = ticker,
            Direction = direction,
            PnLPercent = pnlPercent ?? 0,
            Close = closePrice,
            Open = openPrice
        };
    }
}







    }
}
