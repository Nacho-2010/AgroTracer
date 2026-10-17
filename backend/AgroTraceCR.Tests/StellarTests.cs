using AgroTraceCR.Infrastructure;
using Microsoft.Extensions.Configuration;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Memos;
using StellarDotnetSdk.Operations;
using StellarDotnetSdk.Transactions;
using Xunit;

public class StellarTests
{
    [Fact]
    public async Task MissingSecretFailsBeforeNetworkSubmission()
    {
        var service = new StellarService(new ConfigurationBuilder().Build());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegistrarAsync(new string('a', 64))
        );
        Assert.Contains("Stellar__SecretSeed", error.Message);
    }

    [Fact]
    public void TestnetTransactionContainsFullSha256MemoAndCanBeEncoded()
    {
        // Ephemeral test-only keypair: never persisted or funded.
        var key = KeyPair.Random();
        var hash = Convert.FromHexString(new HashService().Hash(new { codigo = "BOV-CR-0001" }));
        var account = new Account(key.AccountId, 1);
        var transaction = new TransactionBuilder(account)
            .AddOperation(new ManageDataOperation("agrotrace:v1", hash))
            .AddMemo(Memo.Hash(hash))
            .AddTimeBounds(new TimeBounds(TimeSpan.FromSeconds(120)))
            .Build();
        transaction.Sign(key, Network.Test());
        var xdr = transaction.ToEnvelopeXdrBase64();
        Assert.NotEmpty(xdr);
        Assert.NotNull(TransactionBuilder.FromEnvelopeXdr(xdr));
    }
}
