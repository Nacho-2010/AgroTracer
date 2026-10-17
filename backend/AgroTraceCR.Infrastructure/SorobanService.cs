using System.Diagnostics;
using System.Text.Json;
using AgroTraceCR.Application;
using Microsoft.Extensions.Configuration;

namespace AgroTraceCR.Infrastructure;

public class SorobanService(IConfiguration config, string contentRootPath) : ISorobanService
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    string Folder => Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", "blockchain"));

    JsonElement Manifest()
    {
        try
        {
            using var doc = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(Folder, "testnet.json"))
            );
            return doc.RootElement.Clone();
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            throw new InvalidOperationException(
                "Falta blockchain/testnet.json. Despliega el contrato en Testnet siguiendo docs/soroban.md."
            );
        }
    }

    public string ContractId => Manifest().GetProperty("contractId").GetString()!;

    public object Info()
    {
        var m = Manifest();
        return new
        {
            contractId = m.GetProperty("contractId").GetString(),
            admin = m.GetProperty("admin").GetString(),
            network = "Testnet",
            rpc = "https://soroban-testnet.stellar.org",
            explorador = $"https://stellar.expert/explorer/testnet/contract/{ContractId}",
        };
    }

    async Task<JsonElement> RunAsync(object request)
    {
        var start = new ProcessStartInfo(config["Soroban:NodePath"] ?? "node")
        {
            WorkingDirectory = Folder,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(Path.Combine(Folder, "soroban-bridge.mjs"));
        start.Environment.Remove("Stellar__SecretSeed");
        start.Environment.Remove("Soroban__SecretSeed");
        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException(
                "Instala Node.js y ejecuta npm ci desde la raíz del repositorio para usar Soroban."
            );
        }
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request, Json));
        process.StandardInput.Close();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException(
                "Soroban tardó en responder. Consulta la trazabilidad antes de volver a registrar."
            );
        }
        var output = await outputTask;
        await errorTask;
        JsonElement result;
        try
        {
            using var parsed = JsonDocument.Parse(output);
            result = parsed.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(
                "No se pudo iniciar la integración Soroban. Ejecuta npm ci en la raíz del proyecto y comprueba blockchain/testnet.json."
            );
        }
        if (!result.GetProperty("ok").GetBoolean())
            throw new SorobanSubmissionException(
                result.GetProperty("error").GetString()!,
                result.TryGetProperty("transactionId", out var tx) ? tx.GetString() : null,
                result.TryGetProperty("pending", out var pending) && pending.GetBoolean()
            );
        return result;
    }

    public async Task<SorobanReceipt> RegisterAsync(string kind, int id, string hash)
    {
        var secret = config["Soroban:SecretSeed"] ?? config["Stellar:SecretSeed"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Configure Soroban__SecretSeed o Soroban:SecretSeed en user-secrets con la cuenta administradora del contrato. Solo Testnet."
            );
        var r = await RunAsync(
            new
            {
                command = "register",
                kind,
                id,
                hash,
                secret,
            }
        );
        return new(
            r.GetProperty("transactionId").GetString()!,
            r.GetProperty("contractId").GetString()!,
            r.GetProperty("entry").Deserialize<SorobanEntry>(Json)!
        );
    }

    public async Task<SorobanEntry?> ReadAsync(
        string kind,
        int id,
        string? contractId = null,
        int? version = null
    )
    {
        var r = await RunAsync(
            new
            {
                command = "read",
                kind,
                id,
                contractId,
                version,
            }
        );
        return r.GetProperty("entry").ValueKind == JsonValueKind.Null
            ? null
            : r.GetProperty("entry").Deserialize<SorobanEntry>(Json);
    }

    public async Task<SorobanTransaction> TransactionAsync(string hash)
    {
        var r = await RunAsync(new { command = "transaction", transactionId = hash });
        return new(
            r.GetProperty("status").GetString()!,
            r.GetProperty("entry").ValueKind == JsonValueKind.Null
                ? null
                : r.GetProperty("entry").Deserialize<SorobanEntry>(Json)
        );
    }
}
