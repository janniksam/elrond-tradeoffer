namespace Elrond.TradeOffer.Web.Network;

public class TestnetNetworkStrategy : NetworkStrategy
{
    private readonly string _smartContractAddress;

    public TestnetNetworkStrategy(IConfiguration configuration) : 
        base("https://testnet-wallet.elrond.com", Erdcsharp.Configuration.Network.TestNet)
    {
        _smartContractAddress = configuration.GetValue<string>("SmartContractAddressTest");
    }

    public override string GetSmartContractAddress()
    {
        return _smartContractAddress;
    }
}