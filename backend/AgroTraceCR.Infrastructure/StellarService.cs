using AgroTraceCR.Application;
using Microsoft.Extensions.Configuration;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Transactions;
using StellarDotnetSdk.Operations;
using StellarDotnetSdk.Memos;
using System.Net.Http.Json;
using System.Text.Json;
namespace AgroTraceCR.Infrastructure;
public class StellarService(IConfiguration config) : IStellarService {
 public const string Horizon = "https://horizon-testnet.stellar.org";
 public async Task<StellarReceipt> RegistrarAsync(string hash) {
 var secret = config["Stellar:SecretSeed"];
 if(string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Configure Stellar__SecretSeed o dotnet user-secrets set Stellar:SecretSeed. Solo Testnet.");
 var key = KeyPair.FromSecretSeed(secret);
 using var server = new Server(Horizon);
 var account = await server.Accounts.Account(key.AccountId);
 var tx = new TransactionBuilder(account).AddOperation(new ManageDataOperation("agrotrace:v1",Convert.FromHexString(hash))).AddMemo(Memo.Hash(Convert.FromHexString(hash))).AddTimeBounds(new TimeBounds(TimeSpan.FromSeconds(120))).Build();
 tx.Sign(key,Network.Test());
 var result = await server.SubmitTransaction(tx);
 if(result == null || !result.IsSuccess || string.IsNullOrEmpty(result.Hash)) throw new InvalidOperationException("Stellar rechazó la transacción Testnet.");
 return new(result.Hash,DateTimeOffset.UtcNow);
 }
 public async Task<bool> VerificarAsync(string transactionId,string hash) {
 using var client = new HttpClient { Timeout=TimeSpan.FromSeconds(20) };
 var response=await client.GetAsync($"{Horizon}/transactions/{Uri.EscapeDataString(transactionId)}");
 if(response.StatusCode==System.Net.HttpStatusCode.NotFound) return false;
 response.EnsureSuccessStatusCode();
 var json=await response.Content.ReadFromJsonAsync<JsonElement>();
 return json.GetProperty("successful").GetBoolean() && json.GetProperty("memo_type").GetString()=="hash" && json.GetProperty("memo").GetString()==Convert.ToBase64String(Convert.FromHexString(hash));
 }
}


