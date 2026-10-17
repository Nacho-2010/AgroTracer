using AgroTraceCR.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

public class SorobanTests
{
    [Fact]
    public async Task MissingSigningSecretFailsBeforeLaunchingNode()
    {
        var service = new SorobanService(new ConfigurationBuilder().Build(), Path.GetTempPath());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync("lote", 1, new string('a', 64))
        );
        Assert.Contains("Soroban__SecretSeed", error.Message);
    }

    [Fact]
    public void MissingManifestDoesNotPretendAContractIsConfigured()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "backend", "api");
        var service = new SorobanService(new ConfigurationBuilder().Build(), root);
        var error = Assert.Throws<InvalidOperationException>(() => service.Info());
        Assert.Contains("testnet.json", error.Message);
    }
}
