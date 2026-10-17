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

## Stellar Testnet y contrato Soroban

AgroTrace incluye un contrato inteligente Rust desplegado en Stellar Testnet. Las nuevas evidencias de lotes, pedidos y certificados se guardan como versiones de una huella SHA-256 dentro del contrato. Los registros clásicos anteriores siguen siendo verificables.

- Contrato: `CBONGQ25QQKHULFHWCB4F225PH75ORD237QUWF6D4GR2KN24XBVI2EK5`.
- Despliegue y datos públicos: `blockchain/testnet.json`.
- La administradora firma los registros con `Soroban:SecretSeed` en user-secrets o `Soroban__SecretSeed` en el entorno del backend. La consulta es pública y no necesita ese secreto.
- Ejecuta `npm ci` también en la raíz para instalar el SDK que usa el puente de la API.
- En **Verificar trazabilidad**, selecciona el registro y pulsa **Registrar en contrato Soroban**. La interfaz muestra la versión, el contrato y la evidencia.
- Los pagos del carrito conservan las operaciones XLM de Testnet. Las claves del comprador permanecen en su billetera.

Consulta [la guía de Soroban](docs/soroban.md) para compilar, desplegar otra instancia, configurar el backend, revisar pruebas y mantener el TTL. La clave administradora no está incluida en el repositorio.

## Arquitectura y API

```text
React + TypeScript + Vite → controladores ASP.NET Core
Domain ← Application ← Infrastructure ← Api
                         SQL Server / Stellar RPC y Horizon Testnet
```

- `Domain`: entidades del negocio, compras, evidencia blockchain y roles.
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
- Etapa futura: autorización completa, outbox y conciliación, concurrencia/sequence numbers, validación veterinaria, pruebas de integración y otros contratos, fuera del alcance actual.

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

El usuario añade productos, conecta Freighter o inicia sesión con Cavos en Testnet, revisa el monto y aprueba en su billetera. La cotización dura 15 minutos; el servidor calcula cantidades, precios y monto definitivo. Nunca se solicita ni transmite la clave secreta del usuario. Cavos es opcional: configurar el identificador público de aplicación siguiendo [docs/cavos.md](docs/cavos.md). Su sesión corresponde a la billetera de la tienda y no protege los endpoints administrativos.

El cliente conserva el hash y el XDR firmado antes de enviar. Si la red falla, puede verificar o reenviar la misma transacción, sin construir un segundo pago. El servidor consulta Horizon Testnet y valida éxito, emisor, destinatario, XLM nativo, monto, memo y fecha antes de marcar la compra como pagada. Un hash no puede aplicarse a dos compras. La compra pendiente se conserva en el navegador; no borres sus datos antes de resolverla. Testnet puede reiniciarse.

Endpoints: `GET/PUT /api/tienda/configuracion`, `POST /api/tienda/compras`, `GET /api/tienda/compras/{id}`, `POST /api/tienda/compras/{id}/confirmar`. La migración `CheckoutTestnet` agrega compras y configuración, conservando las tablas existentes.

La cuenta receptora creada para esta demo local tiene su clave protegida con Windows DPAPI en `C:\Stellar Agro\.tools\payments\receiver-secret.dpapi`, fuera del repositorio. Solo recibe fondos de Testnet. El panel de compras muestra las 100 compras más recientes y el enlace a sus pagos confirmados.
