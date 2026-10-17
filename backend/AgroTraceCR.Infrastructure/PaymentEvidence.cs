using System.Globalization;
using System.Text.Json;
using AgroTraceCR.Domain;

namespace AgroTraceCR.Infrastructure;

public static class PaymentEvidence
{
    public static bool Matches(Compra order, JsonElement tx, JsonElement operations)
    {
        if (
            !tx.GetProperty("successful").GetBoolean()
            || tx.GetProperty("memo_type").GetString() != "text"
            || tx.GetProperty("memo").GetString() != order.Memo
            || tx.GetProperty("source_account").GetString() != order.Cuenta
        )
            return false;
        var date = tx.GetProperty("created_at").GetDateTimeOffset();
        if (date < order.Fecha.AddSeconds(-5) || date > order.Vence)
            return false;
        var records = operations.GetProperty("_embedded").GetProperty("records");
        return records
            .EnumerateArray()
            .Any(op =>
                op.GetProperty("type").GetString() == "payment"
                && op.GetProperty("asset_type").GetString() == "native"
                && op.GetProperty("from").GetString() == order.Cuenta
                && op.GetProperty("to").GetString() == order.Destino
                && decimal.TryParse(
                    op.GetProperty("amount").GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amount
                )
                && amount == order.MontoXlm
            );
    }
}
