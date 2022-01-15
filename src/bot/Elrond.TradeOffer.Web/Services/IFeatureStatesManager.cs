namespace Elrond.TradeOffer.Web.Services;

public interface IFeatureStatesManager
{
    Task<bool> GetMainNetEnabledAsync(CancellationToken ct);
    Task<bool> GetTestNetEnabledAsync(CancellationToken ct);
    Task<bool> GetDevNetEnabledAsync(CancellationToken ct);

    Task SetDevNetStateAsync(bool isEnabled, long userId, CancellationToken ct);
    Task SetTestNetStateAsync(bool isEnabled, long userId, CancellationToken ct);
    Task SetMainNetStateAsync(bool isEnabled, long userId, CancellationToken ct);
}