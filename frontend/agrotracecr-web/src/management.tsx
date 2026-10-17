import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
type Row = Record<string, string | number>;
const base =
  (import.meta as unknown as { env: Record<string, string> }).env
    .VITE_API_URL || 'http://localhost:5080/api';
async function api(path: string, method = 'GET', body?: unknown) {
  let r: Response;
  try {
    r = await fetch(`${base}/${path}`, {
      method,
      headers:
        body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new Error(
      'No se pudo conectar con la API. Comprueba que esté encendida.',
    );
  }
  if (r.status === 204) return null;
  const data = await r.json();
  if (!r.ok)
    throw new Error(
      data.errors
        ? Object.values(data.errors).flat().join(' ')
        : data.title || 'No se pudo completar la operación',
    );
  return data;
}
const labels: Record<string, string> = {
  nombre: 'Nombre',
  email: 'Correo electrónico',
  finca: 'Finca',
  provincia: 'Provincia',
  unidad: 'Unidad',
  precioReferencia: 'Precio de referencia (₡)',
  productorId: 'Productor',
  productoId: 'Producto',
  compradorId: 'Comprador',
  loteId: 'Lote',
  codigo: 'Código',
  cantidad: 'Cantidad',
  fechaCosecha: 'Fecha de cosecha',
  fechaNacimiento: 'Fecha de nacimiento',
  raza: 'Raza',
  estado: 'Estado',
};
const forms: Record<string, string[]> = {
  productos: ['nombre', 'unidad', 'precioReferencia', 'productorId'],
  productores: ['nombre', 'email', 'finca', 'provincia'],
  compradores: ['nombre', 'email'],
  lotes: ['productoId', 'codigo', 'cantidad', 'fechaCosecha'],
  pedidos: ['compradorId', 'loteId', 'cantidad'],
  ganado: ['codigo', 'raza', 'finca', 'fechaNacimiento', 'productorId'],
};
const names: Record<string, string> = {
  productos: 'producto',
  productores: 'productor',
  compradores: 'comprador',
  lotes: 'lote',
  pedidos: 'pedido de prueba',
  ganado: 'registro bovino',
};
const sources: Record<string, string> = {
  productoId: 'productos',
  productorId: 'productores',
  compradorId: 'compradores',
  loteId: 'lotes',
};
function useLookups(refreshKey: string | number = 0) {
  const [data, set] = useState<Record<string, Row[]>>({}),
    [error, err] = useState('');
  useEffect(() => {
    let active = true;
    Promise.all(
      ['productos', 'productores', 'compradores', 'lotes'].map(
        async (k) => [k, await api(k)] as const,
      ),
    )
      .then((entries) => {
        if (active) set(Object.fromEntries(entries));
      })
      .catch((e) => {
        if (active) err(e.message);
      });
    return () => {
      active = false;
    };
  }, [refreshKey]);
  return { data, error };
}
function optionName(
  kind: string,
  row: Row,
  lookups: Record<string, Row[]>,
): string {
  if (kind === 'lotes') {
    const product = lookups.productos?.find((p) => p.id === row.productoId);
    return `${row.codigo} · ${product?.nombre || 'Producto'} · ${row.cantidad} ${product?.unidad || ''}`;
  }
  if (kind === 'productores') return `${row.nombre} · ${row.finca}`;
  return String(row.nombre || row.codigo || `#${row.id}`);
}
export function Collection({
  path,
  title,
  fields,
  detail,
}: {
  path: string;
  title: string;
  fields: string[];
  detail?: string;
}) {
  const [data, set] = useState<Row[]>([]),
    [error, err] = useState(''),
    [message, msg] = useState(''),
    [version, refresh] = useState(0),
    [editing, edit] = useState<Row | null>(null),
    [deleting, del] = useState<Row | null>(null),
    [busy, b] = useState(false),
    [loading, load] = useState(true);
  const lookups = useLookups(`${path}-${version}`);
  useEffect(() => {
    let active = true;
    load(true);
    err('');
    api(path)
      .then((rows) => {
        if (active) set(rows);
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
  }, [path, version]);
  useEffect(() => {
    edit(null);
    del(null);
    msg('');
  }, [path]);
  async function remove() {
    if (!deleting) return;
    b(true);
    err('');
    try {
      await api(`${path}/${deleting.id}`, 'DELETE');
      del(null);
      if (editing?.id === deleting.id) edit(null);
      refresh((v) => v + 1);
      msg('Registro eliminado.');
    } catch (e) {
      err((e as Error).message);
    } finally {
      b(false);
    }
  }
  return (
    <>
      <div className="eyebrow">RED AGROPECUARIA</div>
      <h1>{title}</h1>
      {(error || lookups.error) && (
        <p role="alert" className="error">
          {error || lookups.error}
        </p>
      )}
      {message && <p role="status">{message}</p>}
      {loading ? (
        <p role="status">Cargando registros…</p>
      ) : (
        <div className="grid">
          {data.map((r) => (
            <article key={r.id}>
              <i>#{r.id} · COSTA RICA</i>
              <h2>{r.nombre || r.codigo || `Pedido #${r.id}`}</h2>
              {fields.map((f) => (
                <p key={f}>
                  <small>{labels[f] || f}</small>
                  <strong>
                    {sources[f]
                      ? (() => {
                          const item = lookups.data[sources[f]]?.find(
                            (x) => x.id === r[f],
                          );
                          return item
                            ? optionName(sources[f], item, lookups.data)
                            : `#${r[f]}`;
                        })()
                      : r[f]}
                  </strong>
                </p>
              ))}
              {path === 'pedidos' && (
                <p>
                  <small>Comprador</small>
                  <strong>
                    {lookups.data.compradores?.find(
                      (x) => x.id === r.compradorId,
                    )?.nombre || `#${r.compradorId}`}
                  </strong>
                </p>
              )}
              {detail && <Link to={`/${detail}/${r.id}`}>Ver detalle ↗</Link>}
              <div className="actions">
                <button
                  className="secondary"
                  onClick={() => {
                    edit(r);
                    del(null);
                    msg('');
                  }}
                >
                  Editar
                </button>
                <button
                  className="danger"
                  onClick={() => {
                    del(r);
                    err('');
                  }}
                >
                  Eliminar
                </button>
              </div>
            </article>
          ))}
        </div>
      )}
      {!loading && !error && !data.length && (
        <p>No hay registros todavía. Crea el primero abajo.</p>
      )}
      {deleting && (
        <section role="alertdialog" aria-labelledby="delete-title">
          <h2 id="delete-title">Eliminar {names[path]}</h2>
          <p>
            ¿Eliminar{' '}
            {deleting.nombre || deleting.codigo || `pedido #${deleting.id}`}?
            Esta acción es permanente.
          </p>
          <div className="actions">
            <button className="danger" disabled={busy} onClick={remove}>
              Confirmar eliminación
            </button>
            <button
              className="secondary"
              disabled={busy}
              onClick={() => del(null)}
            >
              Cancelar
            </button>
          </div>
        </section>
      )}
      <Editor
        key={`${path}-${editing?.id || 'new'}-${version}`}
        kind={path}
        row={editing}
        onCancel={() => edit(null)}
        onSaved={() => {
          edit(null);
          refresh((v) => v + 1);
          msg(editing ? 'Cambios guardados.' : 'Registro creado.');
        }}
      />
    </>
  );
}
function Editor({
  kind,
  row,
  onSaved,
  onCancel,
}: {
  kind: string;
  row: Row | null;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const { data: lookups, error: lookupError } = useLookups();
  const [values, set] = useState<Row>(row || {}),
    [error, err] = useState(''),
    [busy, b] = useState(false);
  function change(field: string, value: string) {
    set((v) => ({
      ...v,
      [field]: value,
      ...(kind === 'ganado' && field === 'productorId'
        ? {
            finca:
              lookups.productores?.find((p) => String(p.id) === value)?.finca ||
              '',
          }
        : {}),
    }));
  }
  useEffect(() => {
    if (row) {
      const editor = document.getElementById('editor');
      editor?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      editor?.focus({ preventScroll: true });
    }
  }, [row]);
  const fields = forms[kind];
  return (
    <section id="editor" tabIndex={-1}>
      <h2>
        {row ? 'Editar' : 'Crear'} {names[kind]}
      </h2>
      {row && (
        <p>
          Registro #{row.id}. Los cambios en datos certificados pueden dejar de
          coincidir con la huella registrada.
        </p>
      )}
      <form
        onSubmit={async (e) => {
          e.preventDefault();
          b(true);
          err('');
          const body = Object.fromEntries(
            fields.map((f) => [
              f,
              f.endsWith('Id') || ['cantidad', 'precioReferencia'].includes(f)
                ? Number(values[f])
                : values[f],
            ]),
          );
          try {
            await api(
              kind + (row ? `/${row.id}` : ''),
              row ? 'PUT' : 'POST',
              body,
            );
            onSaved();
          } catch (e) {
            err((e as Error).message);
          } finally {
            b(false);
          }
        }}
      >
        {fields.map((f) => (
          <label key={f}>
            {labels[f]}
            {sources[f] ? (
              <select
                name={f}
                required
                value={values[f] ?? ''}
                onChange={(e) => change(f, e.target.value)}
                disabled={!lookups[sources[f]]}
              >
                <option value="">
                  {lookups[sources[f]]
                    ? lookups[sources[f]].length
                      ? 'Selecciona una opción'
                      : 'No hay registros disponibles'
                    : 'Cargando opciones…'}
                </option>
                {lookups[sources[f]]?.map((item) => (
                  <option key={item.id} value={item.id}>
                    {optionName(sources[f], item, lookups)} · #{item.id}
                  </option>
                ))}
              </select>
            ) : (
              <input
                name={f}
                required
                value={values[f] ?? ''}
                onChange={(e) => change(f, e.target.value)}
                type={
                  f.startsWith('fecha')
                    ? 'date'
                    : f === 'email'
                      ? 'email'
                      : ['cantidad', 'precioReferencia'].includes(f)
                        ? 'number'
                        : 'text'
                }
                min={
                  f === 'precioReferencia'
                    ? 0
                    : f === 'cantidad'
                      ? 0.01
                      : undefined
                }
                step={
                  ['cantidad', 'precioReferencia'].includes(f)
                    ? '0.01'
                    : undefined
                }
                max={
                  f === 'fechaNacimiento'
                    ? new Date().toLocaleDateString('en-CA')
                    : ['cantidad', 'precioReferencia'].includes(f)
                      ? 1000000
                      : undefined
                }
                pattern={
                  kind === 'ganado' && f === 'codigo'
                    ? 'BOV-CR-[0-9]{4,}'
                    : undefined
                }
                placeholder={
                  kind === 'ganado' && f === 'codigo'
                    ? 'BOV-CR-0003'
                    : undefined
                }
              />
            )}
          </label>
        ))}
        <button disabled={busy || !!lookupError}>
          {busy ? 'Guardando…' : row ? 'Guardar cambios' : 'Guardar'}
        </button>
        {row && (
          <button
            type="button"
            className="secondary"
            onClick={onCancel}
            disabled={busy}
          >
            Cancelar edición
          </button>
        )}
      </form>
      {(error || lookupError) && (
        <p className="error" role="alert">
          {error || lookupError}
        </p>
      )}
    </section>
  );
}
export function Verify() {
  const [contractInfo, setContractInfo] = useState<{
    contractId: string;
    admin: string;
    explorador: string;
  } | null>(null);
  const [contractError, setContractError] = useState('');
  useEffect(() => {
    api('contratos/configuracion')
      .then(setContractInfo)
      .catch((e) => setContractError(e.message));
  }, []);
  const q = new URLSearchParams(location.search);
  const [tipo, t] = useState(q.get('tipo') || 'lote'),
    [id, i] = useState(q.get('id') || ''),
    [options, opts] = useState<Row[]>([]),
    [loading, load] = useState(true),
    [result, set] = useState<any>(null),
    [error, err] = useState(''),
    [busy, b] = useState(false);
  const { data: lookups } = useLookups();
  useEffect(() => {
    let active = true;
    load(true);
    err('');
    opts([]);
    set(null);
    api(
      tipo === 'lote'
        ? 'lotes'
        : tipo === 'pedido'
          ? 'pedidos'
          : 'certificados',
    )
      .then((rows) => {
        if (active) {
          opts(rows);
          i((current) =>
            rows.some((r: Row) => String(r.id) === current) ? current : '',
          );
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
  }, [tipo]);
  function label(row: Row) {
    if (tipo === 'lote') return optionName('lotes', row, lookups);
    if (tipo === 'certificado')
      return `Certificado #${row.id} · ${row.codigo} · ${row.raza}`;
    const buyer = lookups.compradores?.find((x) => x.id === row.compradorId);
    const lot = lookups.lotes?.find((x) => x.id === row.loteId);
    return `Pedido #${row.id} · ${buyer?.nombre || 'Comprador'} · ${lot?.codigo || 'Lote'}`;
  }
  async function run(register = false) {
    if (!id) return;
    b(true);
    err('');
    set(null);
    try {
      if (register) await api(`trazabilidad/${tipo}/${id}`, 'POST', {});
      set(await api(`trazabilidad/${tipo}/${id}`));
    } catch (e) {
      err((e as Error).message);
    } finally {
      b(false);
    }
  }
  return (
    <>
      <div className="eyebrow">EVIDENCIA VERIFICABLE · STELLAR TESTNET</div>
      <h1>Verificar trazabilidad</h1>
      <p className="lead">
        Selecciona un registro por su nombre o código para consultar su huella y
        la evidencia en Stellar.
      </p>
      <section>
        <h2>Contrato inteligente de trazabilidad</h2>
        {contractInfo && (
          <>
            <p>
              Soroban en Stellar Testnet. El contrato conserva las versiones de
              cada huella.
            </p>
            <code>{contractInfo.contractId}</code>
            <a href={contractInfo.explorador} target="_blank" rel="noreferrer">
              Consultar contrato en Stellar Expert
            </a>
          </>
        )}
        {contractError && (
          <p className="error" role="alert">
            {contractError}
          </p>
        )}
        <form
          onSubmit={(e) => {
            e.preventDefault();
            run();
          }}
        >
          <label>
            Tipo
            <select
              value={tipo}
              disabled={busy}
              onChange={(e) => {
                t(e.target.value);
                i('');
              }}
            >
              <option value="lote">Lote</option>
              <option value="pedido">Pedido</option>
              <option value="certificado">Certificado</option>
            </select>
          </label>
          <label>
            Identificador
            <select
              required
              value={id}
              disabled={loading || busy}
              onChange={(e) => {
                i(e.target.value);
                set(null);
                err('');
              }}
            >
              <option value="">
                {loading
                  ? 'Cargando registros…'
                  : options.length
                    ? 'Selecciona un registro'
                    : 'No hay registros disponibles'}
              </option>
              {options.map((r) => (
                <option key={r.id} value={r.id}>
                  {label(r)}
                </option>
              ))}
            </select>
          </label>
          <button disabled={busy || loading || !id}>Consultar</button>
          <button
            className="secondary"
            type="button"
            disabled={busy || loading || !id}
            onClick={() => run(true)}
          >
            Registrar en contrato Soroban
          </button>
        </form>
        {!loading && !options.length && (
          <p>
            {tipo === 'certificado'
              ? 'Genera un certificado desde el detalle de un bovino para verificarlo.'
              : 'Crea un registro para poder verificarlo.'}
          </p>
        )}
        <p role="status">{busy ? 'Consultando…' : ''}</p>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
      </section>
      {result && (
        <article>
          <h2>
            {result.verificado
              ? '✓ Trazabilidad verificada'
              : result.registro
                ? 'No se pudo validar la trazabilidad'
                : 'Sin registro confirmado'}
          </h2>
          <p>
            Coincidencia de datos: {result.coincide ? 'Sí' : 'No'} · Evidencia
            en Stellar: {result.verificadoEnStellar ? 'Sí' : 'No'}
          </p>
          <p>
            Mecanismo: {result.mecanismo}
            {result.versionContrato
              ? ` / Versión ${result.versionContrato}`
              : ''}
          </p>
          {result.contrato && (
            <>
              <small>Contrato que conserva esta evidencia</small>
              <code>{result.contrato}</code>
            </>
          )}
          {result.pendiente && (
            <p role="status">
              Hay una transacción pendiente. Espera y pulsa Registrar en
              contrato Soroban para reconciliar el mismo intento.
            </p>
          )}
          <small>SHA-256 actual</small>
          <code>{result.hashActual}</code>
          {result.registro && (
            <>
              <small>SHA-256 registrado</small>
              <code>{result.registro.hash}</code>
              <p>
                {result.registro.fecha} · {result.registro.red}
              </p>
            </>
          )}
          {result.evidenciaContrato && (
            <p>
              Ledger: {result.evidenciaContrato.ledger} / Autor:{' '}
              <span className="account">{result.evidenciaContrato.author}</span>
            </p>
          )}
          {result.exploradorContrato && (
            <p>
              <a
                href={result.exploradorContrato}
                target="_blank"
                rel="noreferrer"
              >
                Ver contrato y versiones en Stellar
              </a>
            </p>
          )}
          {result.explorador && (
            <a href={result.explorador} target="_blank" rel="noreferrer">
              Consultar transacción en Stellar Expert ↗
            </a>
          )}
        </article>
      )}
    </>
  );
}
