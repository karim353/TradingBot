using System;
using System.IO;
using System.Text.RegularExpressions;
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

                string ticker = string.Empty;
                string direction = string.Empty;
                decimal pnlValue = 0;

                if (!string.IsNullOrEmpty(text))
                {
                    string upperText = text.ToUpper();
                    if (upperText.Contains("LONG"))
                        direction = "Long";
                    else if (upperText.Contains("SHORT"))
                        direction = "Short";

                    if (!string.IsNullOrEmpty(direction))
                    {
                        int idx = upperText.IndexOf(direction.ToUpper(), StringComparison.Ordinal);
                        if (idx > 0)
                        {
                            string before = text.Substring(0, idx);
                            var parts = before.Split(new char[] { ' ', '\n', '\r', '\t', ':', '-' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                                ticker = parts[^1];
                        }
                    }

                    var match = Regex.Match(text, @"-?\d+(\.\d+)?");
                    if (match.Success)
                        decimal.TryParse(match.Value, out pnlValue);
                }

                return new PnLData
                {
                    Ticker = ticker,
                    Direction = direction,
                    PnL = pnlValue
                };
            }
        }
    }
}