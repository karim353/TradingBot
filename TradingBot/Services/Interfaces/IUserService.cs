using TradingBot.Models;

namespace TradingBot.Services.Interfaces;

public interface IUserService
{
    Task<UserSettings> GetUserSettingsAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SaveUserSettingsAsync(long userId, UserSettings settings, CancellationToken cancellationToken = default);
    Task<UserState> GetUserStateAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> SaveUserStateAsync(long userId, UserState state, CancellationToken cancellationToken = default);
    Task<bool> ClearUserStateAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentTickersAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentDirectionsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentAccountsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentSessionsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentPositionsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentResultsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentSetupsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentContextsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentEmotionsAsync(long userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetRecentCommentsAsync(long userId, CancellationToken cancellationToken = default);
}
