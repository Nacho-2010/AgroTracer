# Cavos en AgroTraceCR

La tienda admite Freighter y una billetera Stellar integrada con `@cavos/kit` 0.4.2. Cavos usa su inicio de sesión alojado (Google o correo), recuperación social y bóveda de claves. Solo se configura Stellar Testnet.

## Activación

1. Crear una aplicación en la consola de Cavos y habilitar el acceso de Google/correo según la consola. Registrar el origen local `http://127.0.0.1:5173` y las URL de retorno que indique Cavos; registrar también el dominio final al desplegar.
2. Copiar `frontend/agrotracecr-web/.env.example` a `.env.local` y completar `VITE_CAVOS_APP_ID` con el identificador público de la aplicación. `VITE_CAVOS_ENVIRONMENT=development` selecciona el entorno de consola; usar `production` cuando corresponda. Esto no cambia la red Stellar: sigue siendo Testnet. Habilitar recuperación social en ese entorno de consola. No poner secretos en variables `VITE_`.
3. Mantener `VITE_CAVOS_APP_SALT=agrotracecr-stellar-v1` estable en todas las instalaciones de esta aplicación. Cambiarlo deriva otras billeteras.
4. Reiniciar Vite. En la tienda, elegir Cavos y pulsar **Entrar con Cavos**.
5. Obtener XLM de prueba para la dirección mostrada mediante Friendbot. El pago actual firma un XDR clásico y lo envía a Horizon; requiere una cuenta activa y saldo para el importe, reserva y comisión. No usa el envío patrocinado `execute`, porque el checkout necesita preservar su memo, tiempo de vencimiento y hash exactos.
6. Añadir productos, preparar el pago, revisar el resumen y aprobarlo. La API verifica el pago antes de marcar la compra como pagada. Si hay incertidumbre de red, usar **Verificar pago**; el reenvío conserva el mismo XDR.

Las compras pendientes guardan el proveedor y la dirección: reconectar la misma billetera para pagarlas. Las compras anteriores sin proveedor continúan usando Freighter. Cavos no habilitado deja Freighter operativo.

## Alcance y verificación

El inicio de sesión de Cavos identifica la billetera de la tienda; no protege las rutas administrativas ni sustituye la autorización de la API. El contrato Soroban de evidencias mantiene su cuenta administradora y su flujo existente.

La activación real depende de una aplicación Cavos válida y de su configuración externa. Comprobar con ella: acceso Google/correo, recuperación en otro dispositivo, fondeo Testnet, pago confirmado, cancelación, reconexión y recuperación de pago tras recargar. No se deben declarar esas pruebas superadas sin realizarlas.

Documentación oficial consultada: https://docs.cavos.xyz/llms-full.txt, https://docs.cavos.xyz/docs/react y https://docs.cavos.xyz/docs/stellar.

Validación local: compilación de producción y formato aprobados; 15 pruebas del backend aprobadas; pruebas Chromium del catálogo, carrito y alternativa Freighter sin configurar Cavos, y del modal Cavos con configuración de prueba. No se realizó un inicio de sesión real con Cavos ni un pago firmado por él.

Dependencias: se fuerza Axios `^1.20.0` para corregir las alertas altas del árbol que incorpora el SDK. `npm audit` conserva siete alertas moderadas transitivas en dependencias de Solana/Expo del paquete multired; no se aplicó el downgrade incompatible sugerido por `audit --force`. Revisar las actualizaciones de Cavos antes de un despliegue público.
