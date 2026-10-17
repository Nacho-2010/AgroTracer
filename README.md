# AgroTraceCR

Demo B2B agropecuaria de Costa Rica: catálogo, productores, lotes, pedidos de prueba, tienda con pagos XLM en Testnet, ganado y certificados de trazabilidad con SHA-256 y Stellar Testnet.

## Requisitos

- .NET SDK 10 (el SDK local de esta entrega está en `../.tools/dotnet`).
- Node.js 22.12+ o 24 y npm, solamente para React/Vite.
- SQL Server 2022+ local o remoto. Un usuario autorizado a crear la base y aplicar migraciones.
- Acceso a NuGet/npm y, para blockchain, Horizon Testnet.

## Ejecutar en PowerShell

Desde `C:\Stellar Agro\AgroTraceCR`, si no tienes un SDK global:

```powershell
$env:PATH = (Resolve-Path ../.tools/dotnet).Path + ';' + $env:PATH
$env:DOTNET_ROOT = (Resolve-Path ../.tools/dotnet).Path
$env:DOTNET_CLI_HOME = (Resolve-Path ../.tools/cli).Path
$env:NUGET_PACKAGES = (Resolve-Path ../.tools/nuget).Path
```

Configura SQL Server mediante entorno (no se lee `.env` automáticamente):

```powershell
$env:ConnectionStrings__SqlServer = 'Server=localhost;Database=AgroTraceCR;Integrated Security=True;TrustServerCertificate=True'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet restore AgroTraceCR.slnx
dotnet build AgroTraceCR.slnx --no-restore
dotnet run --no-build --project backend/AgroTraceCR.Api -- --migrate
dotnet run --no-build --project backend/AgroTraceCR.Api --urls http://localhost:5080
```

La opción `--migrate` aplica migraciones y sale. Crea solamente la base indicada; no elimina bases ni tablas. El fallback local usa la misma conexión integrada anterior. Para SQL Authentication, configura usuario/contraseña exclusivamente en entorno o user-secrets; usa TLS con certificado válido en servidores remotos. `TrustServerCertificate=True` es una comodidad local.

En otra terminal:

```powershell
cd frontend/agrotracecr-web
Copy-Item .env.example .env
npm ci
npm run dev
```

Frontend: http://localhost:5173 · API: http://localhost:5080 · Swagger (Development): http://localhost:5080/swagger

## Stellar Testnet

SDK comunitario verificado antes de instalar: [`stellar-dotnet-sdk` 15.1.0](https://www.nuget.org/packages/stellar-dotnet-sdk/15.1.0), mantenido en [Beans-BV/dotnet-stellar-sdk](https://github.com/Beans-BV/dotnet-stellar-sdk). Su ensamblado net8.0 es compatible con la aplicación net10.0 y se compiló en esta entrega. No es un SDK oficial de Microsoft ni de SDF.

1. Abre [Stellar Lab](https://lab.stellar.org), selecciona **Testnet** y genera una cuenta de prueba. Guarda su secreto privadamente; no lo pegues en este chat, React ni archivos del proyecto.
2. Fondea la dirección pública `G…` con [Friendbot](https://friendbot.stellar.org) o la herramienta de funding de Lab. Friendbot solo necesita la dirección pública; nunca envíes el secreto. Consulta las [redes oficiales](https://developers.stellar.org/docs/networks).
3. Configura el secreto solamente en backend mediante `Stellar__SecretSeed` o user-secrets. Ejemplo interactivo que evita escribir el secreto literal en el historial:

```powershell
$seed = Read-Host 'Secreto de la cuenta Testnet' -MaskInput
dotnet user-secrets set 'Stellar:SecretSeed' $seed --project backend/AgroTraceCR.Api
Remove-Variable seed
```

`-MaskInput` requiere PowerShell 7. En Windows PowerShell 5.1 usa `Read-Host -AsSecureString` y configura el secreto con tu gestor local. User-secrets reside fuera del repositorio y no cifra su contenido: es una herramienta para desarrollo local. Reinicia la API en `Development` después de configurarlo.

4. En **Verificar trazabilidad**, selecciona `lote`, ID `1` y pulsa **Registrar en Testnet**. Para ganado, primero genera el certificado desde el detalle de una vaca y usa el ID del certificado.
5. El backend crea una transacción clásica firmada con operación `ManageData`, clave `agrotrace:v1` (última huella) y un **memo hash de 32 bytes**. La evidencia histórica está en el memo de cada transacción; no depende de que el dato mutable de la cuenta siga siendo el último. No emite un activo negociable ni usa Soroban.
6. SQL almacena SHA-256, ID de transacción real, estado, fecha UTC y red. La verificación vuelve a calcular la huella y consulta `https://horizon-testnet.stellar.org/transactions/{id}` para comprobar éxito y memo. El enlace al explorador usa `https://stellar.expert/explorer/testnet/tx/{id}`.

Horizon y la passphrase de Testnet están fijados en el servidor; no se acepta una URL de Mainnet por configuración. Sin secreto, la API responde 503 con el nombre de la variable y persiste el intento como `Error`, sin inventar transaction IDs. Testnet se reinicia periódicamente: una transacción eliminada deja de verificarse.

## Arquitectura y API

```text
React + TypeScript + Vite → controladores ASP.NET Core
Domain ← Application ← Infrastructure ← Api
                         SQL Server / Stellar Horizon Testnet
```

- `Domain`: nueve entidades y roles.
- `Application`: DTOs validados y contratos de hashing, Stellar y trazabilidad.
- `Infrastructure`: EF Core, relaciones y migración inicial con semillas; implementaciones de hashing, casos de trazabilidad y Stellar. DbContext implementa unidad de trabajo y sus DbSets los repositorios EF; no se agrega un repositorio genérico redundante.
- `Api`: controladores MVC sin Razor, CORS, Swagger, errores y middleware JWT preparado.
- `Tests`: determinismo y detección de cambios del hash, configuración ausente y construcción/firma/serialización XDR de una transacción Testnet.

| Método | Ruta bajo `/api`                                              | Uso                               |
| ------ | ------------------------------------------------------------- | --------------------------------- |
| GET    | `/productos`, `/productores`, `/lotes`, `/pedidos`, `/ganado` | Listar                            |
| GET    | `/productores/{id}`, `/lotes/{id}`, `/ganado/{id}`            | Detalle                           |
| POST   | `/lotes`, `/pedidos`, `/ganado`                               | Crear con DTO validado            |
| POST   | `/ganado/{id}/certificado`                                    | Generar un certificado por vaca   |
| POST   | `/trazabilidad/{tipo}/{id}`                                   | Registrar lote/pedido/certificado |
| GET    | `/trazabilidad/{tipo}/{id}`                                   | Comparar SQL y evidencia Horizon  |

Las semillas incluyen dos productores, tres productos, dos lotes, una PYME, administrador y dos vacas (`BOV-CR-0001`, `BOV-CR-0002`). Los pedidos y certificados se crean desde la interfaz. La tienda permite seleccionar productos y pagar con XLM de Testnet mediante Freighter. Los precios están en colones y la conversión de demostración es configurable.

## Comandos de construcción utilizados

```powershell
dotnet new sln -n AgroTraceCR
dotnet sln add backend/AgroTraceCR.Api backend/AgroTraceCR.Application backend/AgroTraceCR.Domain backend/AgroTraceCR.Infrastructure backend/AgroTraceCR.Tests
dotnet tool install dotnet-ef --version 10.0.0 --tool-path ../.tools/bin
dotnet build AgroTraceCR.slnx --no-restore
../.tools/bin/dotnet-ef migrations add InitialCreate --no-build --project backend/AgroTraceCR.Infrastructure --startup-project backend/AgroTraceCR.Api
dotnet test AgroTraceCR.slnx --no-restore
npm --prefix frontend/agrotracecr-web install
npm --prefix frontend/agrotracecr-web run build
```

Los `.csproj` contienen las referencias exactas a EF Core SQL Server/Design 10.0.0, JwtBearer 10.0.0, Swashbuckle 10.0.1, OpenAPI 2.7.5, Stellar 15.1.0 y xUnit. Se incluyen lockfiles NuGet y npm. OpenAPI 2.7.5 se fijó para corregir la advertencia del paquete transitivo 2.3.0.

## Límites de la demo

- Roles modelados y middleware JWT preparado, **sin login ni autorización por rol aplicada**. Ejecutar en localhost; antes de publicar, completar identidad y políticas. El comprador de la demo usa ID 3.
- El certificado representa identificación y trazabilidad; no propiedad legal, inversión, rendimiento, fracciones ni pagos. No es un NFT financiero.
- No se exige una cuenta Stellar para consultar catálogo. Se necesitan secreto y fondos de prueba para registrar.
- Hash canónico versión 1: JSON UTF-8 de un DTO con orden fijo, propiedades explícitas y fechas/decimales serializados por System.Text.Json. Cambiar este contrato requiere nueva versión. No se incluye un hash recursivo de productores/productos vinculados.
- No hay cola durable/outbox, conciliación automática ni garantía de exactamente una vez entre SQL y Stellar. Si la respuesta de red se pierde tras enviar, consultar Horizon antes de reintentar. Evitar registros concurrentes con la misma cuenta en esta demo.
- Sin reserva de inventario, auditoría de usuarios, documentos veterinarios ni firma de autoridad certificadora.
- Los errores de infraestructura no se convierten en éxitos simulados. Una caída de Horizon puede impedir verificar.
- Etapa futura: autorización completa, outbox y conciliación, concurrencia/sequence numbers, validación veterinaria, pruebas de integración y eventualmente Soroban, fuera del alcance actual.

## Formato del proyecto

Visual Studio Code tiene formato al guardar con Prettier para React, CSS, JSON y Markdown, y CSharpier para C# y los proyectos .NET.

Desde la raíz del repositorio, con el SDK .NET configurado como se indica arriba:

```powershell
npm ci
dotnet tool restore
npm run format
npm run format:check
```

Se excluyen dependencias, compilados y configuración privada del entorno.

## Vistas y tienda Stellar Testnet

El selector de la esquina superior derecha cambia entre Administrador y Usuario. El usuario ve únicamente catálogo y carrito; el administrador conserva los CRUD y la configuración de pagos. Es un selector de demostración sin login, no un control de autorización: los endpoints administrativos siguen sin autenticación. Ejecutar solo en localhost.

En **Administrador / Configurar pagos**, registra una cuenta pública G… activa en Testnet y la conversión fija de colones por XLM. No es una tasa de mercado. La clave privada receptora no se necesita para recibir pagos. La configuración se guarda en SQL y las compras conservan una copia del destino y de la tasa al crearse.

El usuario añade productos, conecta Freighter en Testnet, revisa el monto y aprueba en su billetera. La cotización dura 15 minutos; el servidor calcula cantidades, precios y monto definitivo. Nunca se solicita ni transmite la clave secreta del usuario. La firma se pide únicamente en Freighter.

El cliente conserva el hash y el XDR firmado antes de enviar. Si la red falla, puede verificar o reenviar la misma transacción, sin construir un segundo pago. El servidor consulta Horizon Testnet y valida éxito, emisor, destinatario, XLM nativo, monto, memo y fecha antes de marcar la compra como pagada. Un hash no puede aplicarse a dos compras. La compra pendiente se conserva en el navegador; no borres sus datos antes de resolverla. Testnet puede reiniciarse.

Endpoints: `GET/PUT /api/tienda/configuracion`, `POST /api/tienda/compras`, `GET /api/tienda/compras/{id}`, `POST /api/tienda/compras/{id}/confirmar`. La migración `CheckoutTestnet` agrega compras y configuración, conservando las tablas existentes.

La cuenta receptora creada para esta demo local tiene su clave protegida con Windows DPAPI en `C:\Stellar Agro\.tools\payments\receiver-secret.dpapi`, fuera del repositorio. Solo recibe fondos de Testnet. El panel de compras muestra las 100 compras más recientes y el enlace a sus pagos confirmados.
