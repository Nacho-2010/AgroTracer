import React, { useState, useEffect } from 'react';
import { createRoot } from 'react-dom/client';
import {
  BrowserRouter,
  Routes,
  Route,
  NavLink,
  Link,
  useParams,
  Navigate,
  useNavigate,
} from 'react-router-dom';
import './style.css';
import { Collection, Verify } from './management';
import { Shop, PaymentSettings, Purchases } from './shop';
type Row = Record<string, string | number>;
const base =
  (import.meta as unknown as { env: Record<string, string> }).env
    .VITE_API_URL || 'http://localhost:5080/api';
async function api(path: string, body?: unknown) {
  const r = await fetch(
    base + '/' + path,
    body === undefined
      ? undefined
      : {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(body),
        },
  );
  const data = await r.json();
  if (!r.ok) throw new Error(data.title || 'No se pudo completar la operación');
  return data;
}
function useData(path: string) {
  const [data, set] = useState<Row[]>([]),
    [error, err] = useState('');
  useEffect(() => {
    api(path)
      .then(set)
      .catch((e) => err(e.message));
  }, [path]);
  return { data, error };
}
function Status({ error }: { error: string }) {
  return error ? (
    <p role="alert" className="error">
      {error}. Comprueba que la API y SQL Server estén disponibles.
    </p>
  ) : null;
}
function Dashboard() {
  const { data: lotes, error } = useData('lotes');
  const { data: ganado } = useData('ganado');
  return (
    <>
      <div className="eyebrow">COSTA RICA · CAMPO CONECTADO</div>
      <h1>
        El origen importa.
        <br />
        <span>Hazlo verificable.</span>
      </h1>
      <p className="lead">
        Conecta tu producción con nuevas oportunidades y sigue cada paso desde
        la finca.
      </p>
      <Link className="button" to="/catalogo">
        Explorar catálogo ↗
      </Link>
      <div className="stats">
        <article>
          <b>{lotes.length}</b>
          <p>Lotes registrados</p>
        </article>
        <article>
          <b>{ganado.length}</b>
          <p>Bovinos identificados</p>
        </article>
        <article>
          <b>Testnet</b>
          <p>Red de demostración</p>
        </article>
      </div>
      <Status error={error} />
      <h2>Del campo a la confianza</h2>
      <div className="grid">
        <article>
          <i>01 / PRODUCCIÓN</i>
          <h3>Cada lote tiene una historia</h3>
          <p>Consulta su origen, cantidad y fecha de cosecha.</p>
          <Link to="/lotes">Ver lotes →</Link>
        </article>
        <article>
          <i>02 / IDENTIFICACIÓN</i>
          <h3>Ganado con identidad digital</h3>
          <p>Un certificado individual para acompañar su trazabilidad.</p>
          <Link to="/ganado">Explorar ganado →</Link>
        </article>
      </div>
    </>
  );
}
function Detail({ cow = false }: { cow?: boolean }) {
  const { data: products } = useData('productos');
  const { data: producers } = useData('productores');
  const { id } = useParams();
  const [data, set] = useState<any>(null),
    [error, err] = useState('');
  useEffect(() => {
    api((cow ? 'ganado/' : 'lotes/') + id)
      .then(set)
      .catch((e) => err(e.message));
  }, [id, cow]);
  const row = cow ? data?.vaca : data;
  return (
    <>
      <Link to={cow ? '/ganado' : '/lotes'}>← Volver</Link>
      <h1>{row?.codigo || 'Detalle'}</h1>
      <Status error={error} />
      {row && (
        <article>
          {Object.entries(row).map(([k, v]) => (
            <p key={k}>
              <small>
                {k === 'productoId'
                  ? 'Producto'
                  : k === 'productorId'
                    ? 'Productor'
                    : k}
              </small>
              <strong>
                {k === 'productoId'
                  ? products.find((p) => p.id === v)?.nombre || String(v)
                  : k === 'productorId'
                    ? producers.find((p) => p.id === v)?.nombre || String(v)
                    : String(v)}
              </strong>
            </p>
          ))}
        </article>
      )}
      {cow && (
        <section>
          <h2>Certificado digital</h2>
          <p>
            Identificación y trazabilidad. No acredita propiedad legal ni
            representa una inversión.
          </p>
          {data?.certificado ? (
            <>
              <p>Certificado #{data.certificado.id}</p>
              <Link
                to={'/verificar?tipo=certificado&id=' + data.certificado.id}
              >
                Verificar certificado →
              </Link>
            </>
          ) : (
            <button
              onClick={async () => {
                try {
                  await api('ganado/' + id + '/certificado', {});
                  set(await api('ganado/' + id));
                } catch (e) {
                  err((e as Error).message);
                }
              }}
            >
              Generar certificado
            </button>
          )}
        </section>
      )}
      {!cow && (
        <Link className="button" to={'/verificar?tipo=lote&id=' + id}>
          Verificar lote →
        </Link>
      )}
    </>
  );
}
function App() {
  const [mode, setMode] = useState(() =>
    localStorage.getItem('agro-mode') === 'admin' ? 'admin' : 'usuario',
  );
  const navigate = useNavigate();
  function changeMode(next: string) {
    localStorage.setItem('agro-mode', next);
    setMode(next);
    navigate(next === 'admin' ? '/' : '/catalogo');
  }
  return (
    <>
      <aside>
        <Link className="brand" to="/">
          ◈ AgroTrace<span>CR</span>
        </Link>
        <p className="tagline">EL CAMPO DEJA HUELLA</p>
        <nav>
          {(mode === 'admin'
            ? [
                ['/', 'Inicio'],
                ['/catalogo', 'Catálogo'],
                ['/lotes', 'Lotes'],
                ['/productores', 'Productores'],
                ['/compradores', 'Compradores'],
                ['/pedidos', 'Pedidos'],
                ['/ganado', 'Ganado'],
                ['/verificar', 'Verificar trazabilidad'],
                ['/pagos', 'Configurar pagos'],
                ['/ventas', 'Compras de la tienda'],
              ]
            : [
                ['/catalogo', 'Catálogo'],
                ['/carrito', 'Mi carrito'],
              ]
          ).map(([to, label]) => (
            <NavLink key={to} to={to} end>
              {label}
              <span>↗</span>
            </NavLink>
          ))}
        </nav>
        <div className="network">
          <span>●</span> Stellar Testnet<p>Solo fondos de prueba</p>
        </div>
      </aside>
      <div className="workspace">
        <header>
          <span>PLATAFORMA AGROPECUARIA B2B</span>
          <div className="mode-switch" role="group" aria-label="Cambiar vista">
            <button
              className={mode === 'admin' ? 'selected' : ''}
              aria-pressed={mode === 'admin'}
              onClick={() => changeMode('admin')}
            >
              Administrador
            </button>
            <button
              className={mode === 'usuario' ? 'selected' : ''}
              aria-pressed={mode === 'usuario'}
              onClick={() => changeMode('usuario')}
            >
              Usuario
            </button>
          </div>
        </header>
        <main>
          {mode === 'usuario' ? (
            <Routes>
              <Route path="/catalogo" element={<Shop />} />
              <Route path="/carrito" element={<Shop />} />
              <Route path="*" element={<Navigate to="/catalogo" replace />} />
            </Routes>
          ) : (
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route
                path="/catalogo"
                element={
                  <Collection
                    path="productos"
                    title="Cultivado con propósito"
                    fields={['unidad', 'precioReferencia', 'productorId']}
                  />
                }
              />
              <Route
                path="/productores"
                element={
                  <Collection
                    path="productores"
                    title="Personas detrás del origen"
                    fields={['finca', 'provincia']}
                  />
                }
              />
              <Route
                path="/lotes"
                element={
                  <Collection
                    path="lotes"
                    title="Lotes con historia"
                    fields={['cantidad', 'fechaCosecha']}
                    detail="lotes"
                  />
                }
              />
              <Route path="/lotes/:id" element={<Detail />} />
              <Route
                path="/compradores"
                element={
                  <Collection
                    path="compradores"
                    title="Compradores"
                    fields={['email']}
                  />
                }
              />
              <Route
                path="/pedidos"
                element={
                  <Collection
                    path="pedidos"
                    title="Pedidos de prueba"
                    fields={['loteId', 'cantidad', 'estado']}
                  />
                }
              />
              <Route
                path="/ganado"
                element={
                  <Collection
                    path="ganado"
                    title="Identidad del ganado"
                    fields={['raza', 'finca', 'estado']}
                    detail="ganado"
                  />
                }
              />
              <Route path="/ganado/:id" element={<Detail cow />} />
              <Route path="/verificar" element={<Verify />} />
              <Route path="/pagos" element={<PaymentSettings />} />
              <Route path="/ventas" element={<Purchases />} />
              <Route path="*" element={<h1>Página no encontrada</h1>} />
            </Routes>
          )}
        </main>
        <footer>
          AgroTraceCR · Costa Rica{' '}
          <span>Demo con pagos en Stellar Testnet. Solo fondos de prueba.</span>
        </footer>
      </div>
    </>
  );
}
createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>,
);
