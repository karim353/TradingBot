using FluentValidation;
using TradingBot.Models;

namespace TradingBot.Validators;

public class TradeValidator : AbstractValidator<Trade>
{
    public TradeValidator()
    {
        RuleFor(x => x.Ticker)
            .NotEmpty().WithMessage("Тикер обязателен")
            .MaximumLength(20).WithMessage("Тикер не может быть длиннее 20 символов")
            .Matches(@"^[A-Z0-9/]+$").WithMessage("Тикер должен содержать только буквы, цифры и символ /");

        RuleFor(x => x.PnL)
            .InclusiveBetween(-1000000, 1000000).WithMessage("PnL должен быть в диапазоне от -1,000,000 до 1,000,000");

        RuleFor(x => x.Risk)
            .InclusiveBetween(0, 100).WithMessage("Риск должен быть в диапазоне от 0 до 100%");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Дата обязательна")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Дата не может быть в будущем");

        RuleFor(x => x.Direction)
            .MaximumLength(50).WithMessage("Направление не может быть длиннее 50 символов");

        RuleFor(x => x.Account)
            .MaximumLength(100).WithMessage("Аккаунт не может быть длиннее 100 символов");

        RuleFor(x => x.Session)
            .MaximumLength(100).WithMessage("Сессия не может быть длиннее 100 символов");

        RuleFor(x => x.Position)
            .MaximumLength(100).WithMessage("Позиция не может быть длиннее 100 символов");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Заметка не может быть длиннее 1000 символов");

        RuleFor(x => x.Setup)
            .Must(x => x == null || x.Count <= 10).WithMessage("Максимум 10 настроек");

        RuleFor(x => x.Context)
            .Must(x => x == null || x.Count <= 10).WithMessage("Максимум 10 контекстов");

        RuleFor(x => x.Emotions)
            .Must(x => x == null || x.Count <= 10).WithMessage("Максимум 10 эмоций");

        // Дополнительные правила валидации
        RuleFor(x => x.RR)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.RR))
            .WithMessage("R:R не может быть длиннее 20 символов")
            .Matches(@"^[0-9]+:[0-9]+$").When(x => !string.IsNullOrEmpty(x.RR))
            .WithMessage("R:R должен быть в формате X:Y (например, 1:2)");

        RuleFor(x => x.EntryDetails)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.EntryDetails))
            .WithMessage("Детали входа не могут быть длиннее 200 символов");

        RuleFor(x => x.NotionPageId)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.NotionPageId))
            .WithMessage("ID страницы Notion не может быть длиннее 100 символов");

        // Валидация коллекций
        RuleForEach(x => x.Context)
            .MaximumLength(100).When(x => x.Context != null)
            .WithMessage("Каждый элемент контекста не может быть длиннее 100 символов");

        RuleForEach(x => x.Setup)
            .MaximumLength(100).When(x => x.Setup != null)
            .WithMessage("Каждый элемент настройки не может быть длиннее 100 символов");

        RuleForEach(x => x.Emotions)
            .MaximumLength(50).When(x => x.Emotions != null)
            .WithMessage("Каждая эмоция не может быть длиннее 50 символов");
    }
}
