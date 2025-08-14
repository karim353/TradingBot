using FluentValidation;
using Microsoft.Extensions.Logging;
using TradingBot.Models;
using TradingBot.Validators;
using FluentValidation.Results;

namespace TradingBot.Services;

public class ValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly TradeValidator _tradeValidator;

    public ValidationService(ILogger<ValidationService> logger)
    {
        _logger = logger;
        _tradeValidator = new TradeValidator();
    }

    public async Task<ValidationResult> ValidateTradeAsync(Trade trade)
    {
        try
        {
            var result = await _tradeValidator.ValidateAsync(trade);
            
            if (!result.IsValid)
            {
                _logger.LogWarning("Валидация сделки не прошла: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при валидации сделки");
            throw;
        }
    }

    public bool IsValidTicker(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            return false;
            
        return ticker.Length <= 10 && 
               ticker.All(c => char.IsLetterOrDigit(c) && char.IsUpper(c));
    }

    public bool IsValidPnL(decimal pnl)
    {
        return pnl >= -1000000 && pnl <= 1000000;
    }

    public bool IsValidRisk(decimal risk)
    {
        return risk >= 0 && risk <= 100;
    }

    public bool IsValidDate(DateTime date)
    {
        return date <= DateTime.UtcNow.AddDays(1);
    }
}
