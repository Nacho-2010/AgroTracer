param([switch]$ConfigureBackend)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Push-Location $repo
try {
    $secret = $env:Soroban__SecretSeed
    if ([string]::IsNullOrWhiteSpace($secret)) {
        $secure = Read-Host 'Clave secreta de la cuenta administradora Testnet (no se mostrará)' -AsSecureString
        $credential = New-Object System.Management.Automation.PSCredential('soroban-admin', $secure)
        $secret = $credential.GetNetworkCredential().Password
    }
    if ($secret -notmatch '^S[A-Z2-7]{55}$') { throw 'La clave Testnet no tiene el formato esperado.' }
    Push-Location (Join-Path $repo 'contracts')
    try {
        & stellar contract build
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el contrato.' }
    } finally { Pop-Location }
    @{ secret = $secret } | ConvertTo-Json -Compress | & node blockchain/deploy.mjs
    if ($LASTEXITCODE -ne 0) { throw 'Falló el despliegue. Revisa el saldo de Testnet y npm ci en la raíz.' }
    if ($ConfigureBackend) {
        @{ 'Soroban:SecretSeed' = $secret } | ConvertTo-Json -Compress | & dotnet user-secrets set --project backend/AgroTraceCR.Api
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar el secreto local del backend.' }
    }
    Write-Host 'Contrato desplegado. Reinicia la API en Development.'
} finally {
    Remove-Variable secret,secure,credential -ErrorAction SilentlyContinue
    Pop-Location
}
