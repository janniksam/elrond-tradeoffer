namespace Elrond.TradeOffer.Web.Network;

public class MainnetNetworkStrategy : NetworkStrategy
{
    private readonly string _smartContractAddress;

    public MainnetNetworkStrategy(IConfiguration configuration) :
        base("https://wallet.elrond.com", Erdcsharp.Configuration.Network.MainNet)
    {
        _smartContractAddress = configuration.GetValue<string>("SmartContractAddressMain");
    }

    public override string GetSmartContractAddress()
    {
        return _smartContractAddress;
    }
}