namespace Elrond.TradeOffer.Web.Network;

public class DevnetNetworkStrategy : NetworkStrategy
{
    private readonly string _smartContractAddress;

    public DevnetNetworkStrategy(IConfiguration configuration) :
        base("https://devnet-wallet.elrond.com", Erdcsharp.Configuration.Network.DevNet)
    {
        _smartContractAddress = configuration.GetValue<string>("SmartContractAddressDev");
    }

    public override string GetSmartContractAddress()
    {
        return _smartContractAddress;
    }
}