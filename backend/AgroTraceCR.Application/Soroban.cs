namespace AgroTraceCR.Application;

public record SorobanEntry(string Hash, string Author, int Version, int Ledger, long Timestamp);

public record SorobanReceipt(string TransactionId, string ContractId, SorobanEntry Entry);

public record SorobanTransaction(string Status, SorobanEntry? Entry);

public interface ISorobanService
{
    string ContractId { get; }
    Task<SorobanReceipt> RegisterAsync(string kind, int id, string hash);
    Task<SorobanEntry?> ReadAsync(
        string kind,
        int id,
        string? contractId = null,
        int? version = null
    );
    Task<SorobanTransaction> TransactionAsync(string hash);
    object Info();
}

public class SorobanSubmissionException(string message, string? transactionId, bool pending)
    : InvalidOperationException(message)
{
    public string? TransactionId { get; } = transactionId;
    public bool Pending { get; } = pending;
}
