import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useEmbeddedWallet } from './cavos';
type Product = {
  id: number;
  nombre: string;
  unidad: string;
  precioReferencia: number;
  productorId: number;
};
type CartItem = { productoId: number; cantidad: number };
type Config = { destino: string; colonesPorXlm: number; configurado: boolean };
type Order = {
  id: string;
  cuenta: string;
  destino: string;
  memo: string;
  totalColones: number;
  colonesPorXlm: number;
  montoXlm: string;
  vence: string;
  estado: string;
  transactionId: string | null;
  explorador: string | null;
  items: {
    nombre: string;
    cantidad: number;
    unidad: string;
    subtotal: number;
  }[];
};
type Pending = {
  order: Order;
  hash?: string;
  xdr?: string;
  provider?: 'freighter' | 'cavos';
};
const base =
  (import.meta as unknown as { env: Record<string, string> }).env
    .VITE_API_URL || 'http://localhost:5080/api';
const hex = (bytes: Uint8Array) =>
  Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
const money = (n: number) =>
  new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC' }).format(
    n,
  );
function read<T>(key: string, fallback: T): T {
  try {
    return JSON.parse(localStorage.getItem(key) || 'null') ?? fallback;
  } catch {
    return fallback;
  }
}
async function api(path: string, method = 'GET', body?: unknown) {
  let response: Response;
  try {
    response = await fetch(`${base}/${path}`, {
      method,
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new Error('No se pudo conectar con la API. Intenta nuevamente.');
  }
  const data = await response.json();
  if (!response.ok)
    throw new Error(
      data.errors
        ? Object.values(data.errors).flat().join(' ')
        : data.title || 'No se pudo completar la operación',
    );
  return data;
}
export function Shop() {
  const embedded = useEmbeddedWallet();
  const [provider, setProvider] = useState<'freighter' | 'cavos'>('freighter');
  const location = useLocation(),
    cartPage = location.pathname === '/carrito';
  const [products, setProducts] = useState<Product[]>([]),
    [producers, setProducers] = useState<
      { id: number; nombre: string; finca: string }[]
    >([]),
    [cart, setCart] = useState<CartItem[]>(() => read('agro-cart', [])),
    [config, setConfig] = useState<Config | null>(null),
    [wallet, setWallet] = useState(''),
    [pending, setPending] = useState<Pending | null>(() =>
      read('agro-payment', null),
    ),
    [error, err] = useState(''),
    [notice, msg] = useState(''),
    [busy, b] = useState(false),
    [loading, load] = useState(true),
    [now, tick] = useState(Date.now());
  useEffect(() => {
    let active = true;
    Promise.all([
      api('productos'),
      api('productores'),
      api('tienda/configuracion'),
    ])
      .then(([p, pr, c]) => {
        if (active) {
          setProducts(p);
          setProducers(pr);
          setConfig(c);
        }
      })
      .catch((e) => {
        if (active) err(e.message);
      })
      .finally(() => {
        if (active) load(false);
      });
    return () => {
      active = false;
    };
  }, []);
  useEffect(() => {
    localStorage.setItem('agro-cart', JSON.stringify(cart));
  }, [cart]);
  useEffect(() => {
    if (pending) localStorage.setItem('agro-payment', JSON.stringify(pending));
    else localStorage.removeItem('agro-payment');
  }, [pending]);
  useEffect(() => {
    const timer = setInterval(() => tick(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  const total = cart.reduce(
      (sum, item) =>
        sum +
        (products.find((p) => p.id === item.productoId)?.precioReferencia ||
          0) *
          item.cantidad,
      0,
    ),
    count = cart.length;
  const locked = !!pending && pending.order.estado !== 'Pagado';
  function quantity(id: number, value: number) {
    if (locked) return;
    setCart((items) =>
      value <= 0
        ? items.filter((x) => x.productoId !== id)
        : items.map((x) =>
            x.productoId === id ? { ...x, cantidad: value } : x,
          ),
    );
  }
  function add(p: Product) {
    if (locked) return;
    setCart((items) =>
      items.some((x) => x.productoId === p.id)
        ? items.map((x) =>
            x.productoId === p.id ? { ...x, cantidad: x.cantidad + 1 } : x,
          )
        : [...items, { productoId: p.id, cantidad: 1 }],
    );
    msg(`${p.nombre} añadido al carrito.`);
  }
  async function connect(selected = provider) {
    if (selected === 'cavos') {
      if (!embedded.address)
        throw new Error('Inicia sesión con Cavos antes de preparar el pago.');
      if (embedded.needsApproval)
        throw new Error(
          'Vuelve a iniciar sesión con Cavos para autorizar este dispositivo.',
        );
      setWallet(embedded.address);
      return embedded.address;
    }
    const f = await import('@stellar/freighter-api');
    const connected = await f.isConnected();
    if (!connected.isConnected)
      throw new Error(
        'Instala o desbloquea Freighter y selecciona Testnet para continuar.',
      );
    const access = await f.requestAccess();
    if (access.error || !access.address)
      throw new Error(
        access.error?.message || 'No se autorizó la conexión con Freighter.',
      );
    const network = await f.getNetworkDetails();
    const { Networks } = await import('@stellar/stellar-sdk');
    if (network.error || network.networkPassphrase !== Networks.TESTNET)
      throw new Error('Selecciona la red Testnet en Freighter.');
    setWallet(access.address);
    return access.address;
  }
  async function action(fn: () => Promise<void>) {
    b(true);
    err('');
    msg('');
    try {
      await fn();
    } catch (e) {
      err((e as Error).message || 'No se pudo completar la operación.');
    } finally {
      b(false);
    }
  }
  async function quote() {
    const account = await connect();
    const order = await api('tienda/compras', 'POST', {
      cuenta: account,
      items: cart,
    });
    setPending({ order, provider });
  }
  async function confirm(payment: Pending) {
    if (!payment.hash) return;
    const order = await api(
      `tienda/compras/${payment.order.id}/confirmar`,
      'POST',
      { transactionId: payment.hash },
    );
    setPending({ order, hash: payment.hash });
    if (order.estado === 'Pagado') {
      setCart([]);
      msg('Pago confirmado en Stellar Testnet. Tu compra quedó registrada.');
    }
  }
  async function pay() {
    if (!pending || pending.hash) return;
    const selected = pending.provider || 'freighter';
    const account = await connect(selected);
    if (account !== pending.order.cuenta)
      throw new Error(
        'Conecta la misma billetera con la que preparaste la compra.',
      );
    if (new Date(pending.order.vence).getTime() <= Date.now())
      throw new Error(
        'La cotización venció. Cancélala y prepara una nueva compra.',
      );
    const { Horizon, TransactionBuilder, Asset, Operation, Memo, Networks } =
      await import('@stellar/stellar-sdk');
    const server = new Horizon.Server('https://horizon-testnet.stellar.org');
    let source;
    try {
      source = await server.loadAccount(account);
    } catch {
      throw new Error(
        'La billetera no está activa en Testnet. Fóndéala con Friendbot y vuelve a intentar.',
      );
    }
    try {
      await server.loadAccount(pending.order.destino);
    } catch {
      throw new Error(
        'La cuenta receptora no está activa en Testnet. El administrador debe fondearla.',
      );
    }
    const transaction = new TransactionBuilder(source, {
      fee: String(await server.fetchBaseFee()),
      networkPassphrase: Networks.TESTNET,
    })
      .addOperation(
        Operation.payment({
          destination: pending.order.destino,
          asset: Asset.native(),
          amount: pending.order.montoXlm,
        }),
      )
      .addMemo(Memo.text(pending.order.memo))
      .setTimebounds(
        0,
        Math.floor(new Date(pending.order.vence).getTime() / 1000),
      )
      .build();
    const signed =
      selected === 'cavos'
        ? {
            signedTxXdr: await embedded.sign(transaction.toXDR()),
            error: undefined,
          }
        : await (
            await import('@stellar/freighter-api')
          ).signTransaction(transaction.toXDR(), {
            networkPassphrase: Networks.TESTNET,
            address: account,
          });
    if (signed.error || !signed.signedTxXdr)
      throw new Error(
        signed.error?.message || 'El pago fue cancelado en la billetera.',
      );
    const parsed = TransactionBuilder.fromXDR(
      signed.signedTxXdr,
      Networks.TESTNET,
    );
    if (hex(parsed.hash()) !== hex(transaction.hash()))
      throw new Error(
        'La billetera devolvió una transacción diferente. No se envió el pago.',
      );
    const payment = {
      ...pending,
      hash: hex(transaction.hash()),
      xdr: signed.signedTxXdr,
    };
    localStorage.setItem('agro-payment', JSON.stringify(payment));
    setPending(payment);
    try {
      await server.submitTransaction(parsed);
    } catch {
      throw new Error(
        'No se pudo confirmar el envío. Usa «Verificar pago» antes de intentar de nuevo. Puedes reenviar exactamente la misma transacción.',
      );
    }
    await confirm(payment);
  }
  async function resend() {
    if (!pending?.xdr) return;
    const { Horizon, TransactionBuilder, Networks } =
      await import('@stellar/stellar-sdk');
    const server = new Horizon.Server('https://horizon-testnet.stellar.org');
    try {
      await server.submitTransaction(
        TransactionBuilder.fromXDR(pending.xdr, Networks.TESTNET),
      );
    } catch {
      throw new Error(
        'No se pudo reenviar. Verifica el hash en Stellar antes de crear otro pago.',
      );
    }
    await confirm(pending);
  }
  const expired = pending && new Date(pending.order.vence).getTime() <= now;
  return (
    <>
      <div className="eyebrow">TIENDA · STELLAR TESTNET</div>
      <div className="shop-heading">
        <div>
          <h1>{cartPage ? 'Tu carrito' : 'Del campo a tu mesa'}</h1>
          <p className="lead">
            Elige tus productos y paga con XLM de prueba desde tu billetera.
          </p>
        </div>
        <Link className="button" to={cartPage ? '/catalogo' : '/carrito'}>
          {cartPage ? 'Seguir comprando' : `Ver carrito (${count})`}
        </Link>
      </div>
      <section className="wallet-panel" aria-label="Acceso y billetera">
        <h2>Tu billetera</h2>
        <label>
          Forma de acceso
          <select
            value={pending?.provider || provider}
            disabled={busy || locked}
            onChange={(e) => {
              setProvider(e.target.value as 'freighter' | 'cavos');
              setWallet('');
            }}
          >
            <option value="freighter">Freighter</option>
            <option value="cavos" disabled={!embedded.configured}>
              Cavos · Google o correo
            </option>
          </select>
        </label>
        {!embedded.configured && (
          <p>Cavos estará disponible cuando se configure la aplicación.</p>
        )}
        {(pending?.provider || provider) === 'cavos' && (
          <>
            <p>
              Accede con Google o correo para usar tu billetera integrada en
              Stellar Testnet.
            </p>
            {embedded.error && (
              <p className="error" role="alert">
                {embedded.error}
              </p>
            )}
            {embedded.address ? (
              <>
                <p className="account">{embedded.address}</p>
                <div className="actions">
                  {embedded.needsApproval && (
                    <button disabled={busy} onClick={embedded.login}>
                      Autorizar dispositivo
                    </button>
                  )}
                  <button
                    className="secondary"
                    disabled={busy}
                    onClick={embedded.logout}
                  >
                    Cerrar sesión de Cavos
                  </button>
                </div>
              </>
            ) : (
              <button
                disabled={embedded.loading || busy}
                onClick={embedded.login}
              >
                {embedded.loading ? 'Conectando…' : 'Entrar con Cavos'}
              </button>
            )}
          </>
        )}
      </section>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {notice && (
        <p className="success" role="status">
          {notice}
        </p>
      )}
      {!cartPage &&
        (loading ? (
          <p>Cargando catálogo…</p>
        ) : (
          <div className="grid">
            {products.map((p) => {
              const producer = producers.find((x) => x.id === p.productorId);
              return (
                <article key={p.id}>
                  <i>{producer?.finca || 'COSTA RICA'}</i>
                  <h2>{p.nombre}</h2>
                  <p>{producer?.nombre}</p>
                  <p className="price">
                    {money(p.precioReferencia)} <small>por {p.unidad}</small>
                  </p>
                  <button
                    disabled={locked || p.precioReferencia <= 0}
                    onClick={() => add(p)}
                  >
                    {p.precioReferencia > 0
                      ? 'Añadir al carrito'
                      : 'Sin precio disponible'}
                  </button>
                </article>
              );
            })}
          </div>
        ))}
      {locked && !cartPage && (
        <p>
          Hay una compra pendiente.{' '}
          <Link to="/carrito">Continúa el pago en tu carrito.</Link>
        </p>
      )}
      {cartPage && (
        <>
          {!cart.length && !pending && (
            <section>
              <h2>Tu carrito está vacío</h2>
              <p>Añade productos del catálogo para comenzar.</p>
              <Link className="button" to="/catalogo">
                Explorar catálogo
              </Link>
            </section>
          )}
          {cart.length > 0 && (
            <section>
              <h2>Productos seleccionados</h2>
              {cart.map((item) => {
                const product = products.find((p) => p.id === item.productoId);
                return (
                  <div className="cart-row" key={item.productoId}>
                    <div>
                      <strong>
                        {product?.nombre || `Producto #${item.productoId}`}
                      </strong>
                      <p>
                        {product
                          ? `${money(product.precioReferencia)} / ${product.unidad}`
                          : 'Este producto ya no está disponible'}
                      </p>
                    </div>
                    <label>
                      Cantidad
                      <input
                        aria-label={`Cantidad de ${product?.nombre || item.productoId}`}
                        type="number"
                        min="0.01"
                        max="1000000"
                        step="0.01"
                        disabled={locked}
                        value={item.cantidad}
                        onChange={(e) =>
                          quantity(item.productoId, Number(e.target.value))
                        }
                      />
                    </label>
                    <strong>
                      {money((product?.precioReferencia || 0) * item.cantidad)}
                    </strong>
                    <button
                      className="secondary"
                      disabled={locked}
                      onClick={() => quantity(item.productoId, 0)}
                    >
                      Quitar
                    </button>
                  </div>
                );
              })}
              <p className="price">Total estimado: {money(total)}</p>
              <p>El total definitivo se calcula al preparar el pago.</p>
            </section>
          )}
          {!pending && cart.length > 0 && (
            <section>
              <h2>Pagar con Stellar</h2>
              <p>
                Red: <strong>Testnet</strong> · Solo fondos de prueba.
              </p>
              {config?.configurado ? (
                <p>
                  Conversión de demostración: 1 XLM ={' '}
                  {money(config.colonesPorXlm)}. No es una cotización de
                  mercado.
                </p>
              ) : (
                <p>
                  El administrador debe configurar la cuenta receptora antes de
                  habilitar los pagos.
                </p>
              )}
              <p>
                {provider === 'cavos' ? (
                  'Usa tu sesión de Cavos'
                ) : (
                  <>
                    Necesitas{' '}
                    <a
                      href="https://www.freighter.app/"
                      target="_blank"
                      rel="noreferrer"
                    >
                      Freighter
                    </a>
                  </>
                )}{' '}
                en Testnet. Revisa el total antes de aprobar en tu billetera.
              </p>
              {wallet && <p className="account">Billetera: {wallet}</p>}
              <button
                disabled={
                  busy ||
                  (provider === 'cavos' &&
                    (!embedded.address ||
                      embedded.loading ||
                      embedded.needsApproval)) ||
                  loading ||
                  !config?.configurado ||
                  cart.some((x) => !products.some((p) => p.id === x.productoId))
                }
                onClick={() => action(quote)}
              >
                {busy ? 'Conectando…' : 'Conectar billetera y preparar pago'}
              </button>
            </section>
          )}
          {pending && (
            <section className="checkout">
              <div className="eyebrow">
                {pending.order.estado === 'Pagado'
                  ? 'PAGO CONFIRMADO'
                  : 'RESUMEN DEL PAGO'}
              </div>
              <h2>
                {pending.order.estado === 'Pagado'
                  ? 'Gracias por tu compra de prueba'
                  : `${pending.order.montoXlm} XLM`}
              </h2>
              <p>{money(pending.order.totalColones)} · Stellar Testnet</p>
              {pending.order.items.map((item, index) => (
                <p key={index}>
                  {item.nombre} · {item.cantidad} {item.unidad} ·{' '}
                  {money(item.subtotal)}
                </p>
              ))}
              <p>
                Conversión fijada: 1 XLM = {money(pending.order.colonesPorXlm)}.
                La comisión de red se muestra en la billetera.
              </p>
              <p>Billetera del comprador</p>
              <code>{pending.order.cuenta}</code>
              {pending.order.estado !== 'Pagado' && (
                <a
                  href={`https://friendbot.stellar.org/?addr=${pending.order.cuenta}`}
                  target="_blank"
                  rel="noreferrer"
                >
                  Obtener fondos de prueba con Friendbot
                </a>
              )}
              <p>Cuenta receptora</p>
              <code>{pending.order.destino}</code>
              <p>Referencia: {pending.order.memo}</p>
              {pending.order.estado !== 'Pagado' && (
                <p>
                  {expired
                    ? 'La cotización venció.'
                    : `Válida hasta ${new Date(pending.order.vence).toLocaleTimeString('es-CR')}.`}
                </p>
              )}
              {pending.hash && (
                <>
                  <p>Hash de la transacción</p>
                  <code>{pending.hash}</code>
                  <a
                    href={`https://stellar.expert/explorer/testnet/tx/${pending.hash}`}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Ver en Stellar Expert ↗
                  </a>
                </>
              )}
              <div className="actions">
                {pending.order.estado === 'Pagado' ? (
                  <button
                    onClick={() => {
                      setPending(null);
                      msg('');
                    }}
                  >
                    Nueva compra
                  </button>
                ) : pending.hash ? (
                  <>
                    <button
                      disabled={busy}
                      onClick={() => action(() => confirm(pending))}
                    >
                      Verificar pago
                    </button>
                    {pending.xdr && !expired && (
                      <button
                        className="secondary"
                        disabled={busy}
                        onClick={() => action(resend)}
                      >
                        Reenviar misma transacción
                      </button>
                    )}
                  </>
                ) : (
                  <>
                    <button
                      disabled={busy || !!expired}
                      onClick={() => action(pay)}
                    >
                      {busy
                        ? 'Procesando…'
                        : `Aprobar pago en ${pending.provider === 'cavos' ? 'Cavos' : 'Freighter'}`}
                    </button>
                    <button
                      className="secondary"
                      disabled={busy}
                      onClick={() => setPending(null)}
                    >
                      Cancelar compra
                    </button>
                  </>
                )}
              </div>
              {pending.hash && pending.order.estado !== 'Pagado' && (
                <p>
                  La compra seguirá pendiente hasta verificar el pago en la red.
                  No envíes un segundo pago.
                </p>
              )}
            </section>
          )}
        </>
      )}
    </>
  );
}
export function PaymentSettings() {
  const [config, set] = useState<Config>({
      destino: '',
      colonesPorXlm: 100,
      configurado: false,
    }),
    [message, msg] = useState(''),
    [error, err] = useState(''),
    [busy, b] = useState(false);
  useEffect(() => {
    api('tienda/configuracion')
      .then(set)
      .catch((e) => err(e.message));
  }, []);
  return (
    <>
      <div className="eyebrow">ADMINISTRACIÓN · TESTNET</div>
      <h1>Pagos con Stellar</h1>
      <section>
        <h2>Cuenta receptora</h2>
        <p>
          Usa una dirección pública de Testnet activa y fondeada. La conversión
          es una referencia fija para la demo.
        </p>
        <form
          onSubmit={async (e) => {
            e.preventDefault();
            b(true);
            err('');
            msg('');
            try {
              await api('tienda/configuracion', 'PUT', {
                destino: config.destino,
                colonesPorXlm: config.colonesPorXlm,
              });
              msg(
                'Configuración guardada. Las compras nuevas usarán estos datos.',
              );
            } catch (e) {
              err((e as Error).message);
            } finally {
              b(false);
            }
          }}
        >
          <label>
            Dirección pública (G…)
            <input
              required
              minLength={56}
              maxLength={56}
              value={config.destino}
              onChange={(e) =>
                set({ ...config, destino: e.target.value.trim() })
              }
            />
          </label>
          <label>
            Colones por XLM de prueba
            <input
              required
              type="number"
              min="0.01"
              max="1000000"
              step="0.01"
              value={config.colonesPorXlm}
              onChange={(e) =>
                set({ ...config, colonesPorXlm: Number(e.target.value) })
              }
            />
          </label>
          <button disabled={busy}>Guardar configuración</button>
        </form>
        {message && <p role="status">{message}</p>}
        {error && (
          <p role="alert" className="error">
            {error}
          </p>
        )}
        <p className="account">{config.destino}</p>
        <a
          href="https://laboratory.stellar.org/#account-creator?network=test"
          target="_blank"
          rel="noreferrer"
        >
          Herramientas de cuentas Testnet ↗
        </a>
      </section>
      <p>
        El selector Administrador / Usuario es una demostración de vistas. El
        acceso todavía no usa autenticación.
      </p>
    </>
  );
}

export function Purchases() {
  const [rows, set] = useState<Order[]>([]),
    [error, err] = useState(''),
    [busy, b] = useState(false);
  async function reload() {
    b(true);
    err('');
    try {
      set(await api('tienda/compras'));
    } catch (e) {
      err((e as Error).message);
    } finally {
      b(false);
    }
  }
  useEffect(() => {
    reload();
  }, []);
  return (
    <>
      <div className="eyebrow">ADMINISTRACI?N ? TIENDA</div>
      <h1>Compras de la tienda</h1>
      <button className="secondary" disabled={busy} onClick={reload}>
        {busy ? 'Actualizando?' : 'Actualizar'}
      </button>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      <div className="grid">
        {rows.map((order) => (
          <article key={order.id}>
            <i>{order.estado} ? TESTNET</i>
            <h2>{money(order.totalColones)}</h2>
            <p>{order.montoXlm} XLM</p>
            {order.items.map((item, index) => (
              <p key={index}>
                {item.nombre} ? {item.cantidad} {item.unidad}
              </p>
            ))}
            <small>Billetera del comprador</small>
            <p className="account">{order.cuenta}</p>
            <p>Referencia: {order.memo}</p>
            {order.explorador && (
              <a href={order.explorador} target="_blank" rel="noreferrer">
                Ver pago en Stellar ?
              </a>
            )}
          </article>
        ))}
      </div>
      {!busy && !rows.length && <p>No hay compras todav?a.</p>}
      <p>
        Se muestran las ?ltimas 100 compras. Los pedidos de prueba se gestionan
        por separado.
      </p>
    </>
  );
}
