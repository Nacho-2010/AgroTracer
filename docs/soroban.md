# Contrato Soroban de AgroTraceCR

AgroTrace usa un contrato inteligente propio en Stellar Testnet para registrar las huellas SHA-256 de lotes, pedidos y certificados. Los pagos del carrito siguen usando operaciones de pago XLM de Stellar Classic.

## Despliegue público de la demo

- Contrato: `CBONGQ25QQKHULFHWCB4F225PH75ORD237QUWF6D4GR2KN24XBVI2EK5`
- Cuenta administradora: `GDEC3YPLJ2LVXF6DKAH34WZ6MWDQLES6USP6Z2YJ4DQSJ46UMH56ES7U`
- RPC: `https://soroban-testnet.stellar.org`
- [Transacción de despliegue](https://stellar.expert/explorer/testnet/tx/37448378680bd9b2c681bb35222e1bfa5cd0c75254842f382e93dd22d623e3f0)
- [Primer registro del lote 1](https://stellar.expert/explorer/testnet/tx/43967782d48bdb988c3daaa6d0ec9df6e42866bfe0bf19e3f1aeba3615bfceeb)

`blockchain/testnet.json` contiene el identificador, la cuenta pública, el hash del WASM y las transacciones de despliegue. No contiene claves secretas. Testnet puede reiniciarse, por lo que puede ser necesario volver a desplegar.

## Funciones y reglas

| Función                    | Acceso                     | Resultado                                                |
| -------------------------- | -------------------------- | -------------------------------------------------------- |
| `__constructor(admin)`     | Despliegue                 | Fija la cuenta administradora                            |
| `admin()`                  | Público                    | Devuelve la cuenta autorizada                            |
| `register(kind, id, hash)` | Firma de la administradora | Registra una versión y publica un evento                 |
| `latest(kind, id)`         | Público                    | Devuelve la versión más reciente o ausencia de evidencia |
| `get(kind, id, version)`   | Público                    | Consulta una versión histórica                           |
| `renew(kind, id, version)` | Firma de la administradora | Renueva el TTL sin modificar la evidencia                |

Solo se aceptan los tipos `lote`, `pedido` y `certificado`, identificadores positivos y huellas de 32 bytes. Cada evidencia contiene huella, autor, versión, ledger y fecha. El contrato conserva las versiones previas. Si se repite la última huella, devuelve la misma versión. Volver a una huella anterior después de otro cambio crea una versión nueva.

El contrato exige `require_auth()` a su administradora. No admite reemplazar esa cuenta ni actualizar su código en esta versión. La aplicación no escribe nombres, fincas ni información personal dentro del contrato.

## Usar el contrato existente desde otra computadora

Desde la raíz del repositorio:

```powershell
npm.cmd ci
dotnet restore
dotnet build
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__SqlServer = 'Server=localhost;Database=AgroTraceCR;Integrated Security=True;TrustServerCertificate=True'
dotnet run --project backend/AgroTraceCR.Api -- --migrate
dotnet run --project backend/AgroTraceCR.Api --urls http://localhost:5080
```

Para SQL Express, usa `Server=localhost\SQLEXPRESS`. En otra terminal instala e inicia el frontend con `npm.cmd ci` y `npm.cmd run dev` dentro de `frontend/agrotracecr-web`.

**El `npm ci` de la raíz también es necesario:** la API ejecuta el SDK JavaScript oficial de Stellar mediante `blockchain/soroban-bridge.mjs`. La consulta del contrato público no requiere la clave administradora. Puedes definir `Soroban__NodePath` si `node` no está en el PATH del proceso de la API.

Para registrar hay que configurar `Soroban:SecretSeed` mediante user-secrets, o `Soroban__SecretSeed` en el entorno de la API. Tiene que corresponder a la administradora del contrato. No se comparte ni se guarda esa clave en Git, en React o en el chat. Las compañeras que necesiten registrar con sus propias cuentas pueden desplegar una instancia independiente con el procedimiento siguiente.

## Compilar y desplegar otra instancia

Herramientas verificadas: Rust `1.99.1`, SDK Soroban `29.0.1`, Stellar CLI `28.1.0`, Node.js 24 y .NET 10. El archivo `contracts/rust-toolchain.toml` fija Rust y el target `wasm32v1-none`. Windows necesita las herramientas C++ de Visual Studio para compilar las dependencias nativas y ejecutar las pruebas.

1. Instala Rust y Stellar CLI desde sus fuentes oficiales.
2. Ejecuta `npm.cmd ci` en la raíz.
3. Prepara una cuenta administradora con fondos de Testnet. Su clave debe permanecer privada.
4. Desde PowerShell ejecuta:

```powershell
.\scripts\deploy-soroban.ps1 -ConfigureBackend
```

El script solicita la clave sin mostrarla, compila mediante `stellar contract build`, despliega en **Testnet** y actualiza únicamente los datos públicos de `blockchain/testnet.json`. Con `-ConfigureBackend` guarda la clave en user-secrets fuera del repositorio. También admite la variable de entorno `Soroban__SecretSeed` si ya está configurada.

Los user-secrets de .NET son una herramienta local de desarrollo y no cifran el archivo. La cuenta de esta instalación tiene además un respaldo protegido con Windows DPAPI fuera del repositorio, en `C:\Stellar Agro\.tools\soroban\admin-secret.dpapi`.

Para recompilar y probar sin desplegar:

```powershell
cd contracts
cargo test
stellar contract build
```

El WASM queda en `contracts/target/wasm32v1-none/release/agrotrace_registry.wasm`. Solo se usan fondos de prueba. El cliente rechaza comisiones superiores a 5 XLM por operación.

## Integración y verificación

En **Administrador / Verificar trazabilidad**, elige un registro y pulsa **Registrar en contrato Soroban**. La API calcula el hash de los datos SQL, invoca el contrato y guarda su identificador, versión y transacción. **Consultar** compara los datos actuales con la versión almacenada en el contrato. La pantalla muestra contrato, versión, ledger, autor y enlaces de evidencia.

- `GET /api/contratos/configuracion`: identificador y cuenta pública del despliegue.
- `POST /api/trazabilidad/{tipo}/{id}`: nuevo registro Soroban, con idempotencia de la última huella.
- `GET /api/trazabilidad/{tipo}/{id}`: comparación de SQL con la versión registrada.

La migración `SorobanEvidence` agrega dos columnas opcionales a `Registros`; conserva los registros previos. Si una evidencia antigua no tiene `ContractId`, la API mantiene su verificación clásica mediante Horizon.

Si se pierde la respuesta después de enviar, el backend conserva el hash de la transacción y el estado pendiente cuando están disponibles. Repetir el registro reconcilia primero ese intento. El contrato impide que la repetición de la última huella cree versiones duplicadas. El bloqueo de firma evita carreras de secuencia dentro de un proceso de API; un despliegue con varias instancias necesita coordinación distribuida.

## Alcance y mantenimiento

Las entradas usan almacenamiento persistente y TTL, no una garantía de disponibilidad ilimitada. El registro y `renew` extienden su vigencia hasta 518.400 ledgers cuando corresponde; las versiones históricas deben renovarse o restaurarse si se archivan. La restauración puede requerir una transacción adicional con herramientas Stellar. Los reinicios de Testnet pueden borrar la evidencia.

La firma administradora protege las escrituras del contrato. El selector de vistas de la aplicación sigue siendo una demo sin login ni autorización de API. No debe publicarse el backend tal como está. Un hash permite comprobar integridad, pero no demuestra por sí solo el origen físico ni la veracidad de los datos ingresados.

## Evidencia de pruebas

Las pruebas del contrato cubren autorización, registros duplicados, historial inmutable, separación de tipos e identificadores, validaciones y renovación. La integración en Testnet comprobó:

- Registro y verificación del lote 1 desde la API.
- Repetición del mismo registro sin una versión o fila duplicada.
- Detección de una modificación del lote 2.
- Nuevas versiones 2 y 3 después de modificar y restaurar sus datos originales.

El lote 2 terminó con sus datos originales. [Transacción de su versión 3](https://stellar.expert/explorer/testnet/tx/d879917c845cd33d94e32ef329a267f9125f9b397d42e66b200afc58ecd7cc84).

## Referencias

- [Configuración oficial de Soroban](https://developers.stellar.org/docs/build/smart-contracts/getting-started/setup)
- [Autorización](https://developers.stellar.org/docs/learn/fundamentals/contract-development/authorization)
- [Almacenamiento persistente](https://developers.stellar.org/docs/learn/fundamentals/contract-development/storage/persisting-data)
- [Despliegue con el SDK JavaScript](https://developers.stellar.org/docs/build/guides/transactions/install-deploy-contract-with-code)
