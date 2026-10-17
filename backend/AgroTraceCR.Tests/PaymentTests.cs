using System.Text.Json;
using AgroTraceCR.Domain;
using AgroTraceCR.Infrastructure;
using Xunit;

public class PaymentTests
{
    static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    static Compra Order() =>
        new()
        {
            Cuenta = "payer",
            Destino = "receiver",
            Memo = "AGRO-demo",
            MontoXlm = 12.5m,
            Fecha = Now,
            Vence = Now.AddMinutes(15),
        };

    static JsonElement Tx(
        bool successful = true,
        string memo = "AGRO-demo",
        string source = "payer",
        int minutes = 1
    ) =>
        JsonSerializer.SerializeToElement(
            new
            {
                successful,
                memo_type = "text",
                memo,
                source_account = source,
                created_at = Now.AddMinutes(minutes),
            }
        );

    static JsonElement Ops(
        string destination = "receiver",
        string amount = "12.5000000",
        string asset = "native",
        string from = "payer"
    ) =>
        JsonSerializer.SerializeToElement(
            new
            {
                _embedded = new
                {
                    records = new[]
                    {
                        new
                        {
                            type = "payment",
                            asset_type = asset,
                            from,
                            to = destination,
                            amount,
                        },
                    },
                },
            }
        );

    [Fact]
    public void AcceptsExactPayment() => Assert.True(PaymentEvidence.Matches(Order(), Tx(), Ops()));

    [Theory]
    [InlineData("wrong", "12.5000000", "native", "payer")]
    [InlineData("receiver", "12.4", "native", "payer")]
    [InlineData("receiver", "12.5000000", "credit_alphanum4", "payer")]
    [InlineData("receiver", "12.5000000", "native", "someone")]
    public void RejectsWrongPayment(string destination, string amount, string asset, string from) =>
        Assert.False(PaymentEvidence.Matches(Order(), Tx(), Ops(destination, amount, asset, from)));

    [Fact]
    public void RejectsFailedTransaction() =>
        Assert.False(PaymentEvidence.Matches(Order(), Tx(successful: false), Ops()));

    [Fact]
    public void RejectsWrongMemo() =>
        Assert.False(PaymentEvidence.Matches(Order(), Tx(memo: "other"), Ops()));

    [Fact]
    public void RejectsWrongSource() =>
        Assert.False(PaymentEvidence.Matches(Order(), Tx(source: "someone"), Ops()));

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void RejectsOutsideQuote(int minutes) =>
        Assert.False(PaymentEvidence.Matches(Order(), Tx(minutes: minutes), Ops()));
}
