# Verificación de la entrega

Consultar el resumen final y las notas de ejecución anexadas aquí. No se configura ni publica una clave Stellar en esta entrega.

## Resultado local (13 de octubre de 2026, Costa Rica)

- SDK .NET 10.0.402 instalado en `../.tools/dotnet`, fuera del árbol versionado.
- `dotnet build`: correcto, cero errores y cero advertencias.
- `dotnet test`: 3 pruebas correctas (hash, ausencia de secreto y construcción/firma/XDR de transacción Testnet con clave efímera).
- `npm run build`: TypeScript y Vite correctos. `npm install`: cero vulnerabilidades reportadas.
- Migración inicial aplicada en SQL Server local; creada base `AgroTraceCR` con semillas.
- HTTP 200: health, productos, productores, lotes, ganado, verificación y Swagger JSON.
- Creado pedido de prueba ID 1 y certificado para BOV-CR-0001 ID 1. Segunda generación devuelve el mismo certificado.
- Verificación de certificado sin registro devuelve `verificado: false` y SHA-256 de 64 caracteres.
- Registro sin secreto devuelve HTTP 503. No se envió ninguna transacción a Stellar ni se fondeó ninguna cuenta.
- Código bovino inválido y comprador con rol incorrecto: HTTP 400. Código bovino duplicado: HTTP 409.
- No fue posible inspeccionar visualmente React: no había navegador conectado disponible en las herramientas. La compilación del frontend pasó; revisión visual pendiente.

La migración se llama `20261014042157_InitialCreate` porque EF generó su identificador en UTC, correspondiente al 13 de octubre en Costa Rica.

Prueba manual: catálogo → lote → verificación sin registro; ganado → BOV-CR-0001 → generar certificado; pedidos → crear con comprador 3; registrar sin secreto debe responder 503, nunca Confirmado. Con cuenta Testnet fondeada, registrar y verificar memo en Horizon.
