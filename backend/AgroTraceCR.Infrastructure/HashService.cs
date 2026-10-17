using System.Security.Cryptography;
using System.Text.Json;
using AgroTraceCR.Application;

namespace AgroTraceCR.Infrastructure;

// Fixed DTO property order and explicit schema version form the canonical payload contract.
public class HashService : IHashService
{
    public string Hash(object value) =>
        Convert
            .ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)))
            .ToLowerInvariant();
}
