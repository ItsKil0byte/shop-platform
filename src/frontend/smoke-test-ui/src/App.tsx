import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  Clipboard,
  ClipboardCheck,
  Database,
  ExternalLink,
  Eye,
  EyeOff,
  KeyRound,
  Link2,
  Loader2,
  Lock,
  Network,
  Play,
  RefreshCcw,
  Server,
  ShieldCheck,
  ShoppingCart,
  Trash2,
  User,
  UserPlus,
  XCircle,
} from 'lucide-react';

type HttpMethod = 'GET' | 'POST';
type RequestState = 'idle' | 'loading' | 'success' | 'error';

type ApiResult = {
  title: string;
  method: HttpMethod;
  path: string;
  url: string;
  status?: number;
  ok: boolean;
  durationMs: number;
  timestamp: string;
  requestBody?: unknown;
  responseBody?: unknown;
  rawText?: string;
  error?: string;
  diagnostic?: string;
};

type ChecklistKey =
  | 'dockerCompose'
  | 'gatewayHealth'
  | 'authRegister'
  | 'authLogin'
  | 'jwtSaved'
  | 'authMe'
  | 'authDb'
  | 'ordersGateway'
  | 'ordersGrpc'
  | 'paymentGrpc'
  | 'moderationGrpc'
  | 'rabbitMq'
  | 'workerRabbitMq'
  | 'consul'
  | 'paymentConsul'
  | 'seqLogs'
  | 'sagaFlow';

type ChecklistItem = {
  key: ChecklistKey;
  label: string;
  hint: string;
  auto?: boolean;
};

const STORAGE_TOKEN = 'shop-platform.jwt';
const STORAGE_BASE_URL = 'shop-platform.baseUrl';
const DEFAULT_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';

const checklistItems: ChecklistItem[] = [
  { key: 'dockerCompose', label: 'Docker Compose поднят', hint: 'Проверяется по контейнерам и доступности UI/сервисов.' },
  { key: 'gatewayHealth', label: 'Gateway отвечает', hint: 'Автоматически отмечается после успешного GET /health.', auto: true },
  { key: 'authRegister', label: 'Auth register работает', hint: 'Автоматически отмечается после успешного POST /auth/register.', auto: true },
  { key: 'authLogin', label: 'Auth login работает', hint: 'Автоматически отмечается после успешного POST /auth/login.', auto: true },
  { key: 'jwtSaved', label: 'JWT сохраняется', hint: 'Автоматически отмечается, если token найден и сохранен в localStorage.', auto: true },
  { key: 'authMe', label: '/auth/me работает по JWT', hint: 'Автоматически отмечается после успешного GET /auth/me.', auto: true },
  { key: 'authDb', label: 'Данные пользователя сохраняются в AuthDb', hint: 'Отметь вручную после проверки БД или успешного /auth/me с созданным пользователем.' },
  { key: 'ordersGateway', label: 'Order API доступен через Gateway', hint: 'Автоматически отмечается, если POST /orders вернул HTTP-ответ через Gateway.', auto: true },
  { key: 'ordersGrpc', label: 'Order вызывает внутренние gRPC сервисы', hint: 'Отметь после просмотра логов order/payment/moderation.' },
  { key: 'paymentGrpc', label: 'Payment service работает через gRPC', hint: 'Отметь после проверки логов или результата order flow.' },
  { key: 'moderationGrpc', label: 'Moderation service работает через gRPC', hint: 'Отметь после проверки логов или результата order flow.' },
  { key: 'rabbitMq', label: 'RabbitMQ доступен', hint: 'Открой RabbitMQ UI и проверь соединения/очереди.' },
  { key: 'workerRabbitMq', label: 'Notification worker подключается к RabbitMQ', hint: 'Проверяется по RabbitMQ connections и логам worker.' },
  { key: 'consul', label: 'Consul доступен', hint: 'Открой Consul UI и проверь список сервисов.' },
  { key: 'paymentConsul', label: 'Payment service регистрируется в Consul', hint: 'Проверяется в Consul UI.' },
  { key: 'seqLogs', label: 'Seq получает логи', hint: 'Открой Seq и проверь события после Auth/Order запросов.' },
  { key: 'sagaFlow', label: 'Saga-like flow заказа виден по статусам/логам', hint: 'Автоматически отмечается при статусе Paid/Cancelled/Pending, либо вручную по логам.', auto: true },
];

const infraLinks = [
  { label: 'Seq logs', url: 'http://localhost:5341', icon: Activity },
  { label: 'RabbitMQ UI', url: 'http://localhost:15672', icon: Network },
  { label: 'Consul UI', url: 'http://localhost:8500', icon: Server },
  { label: 'Gateway', url: 'http://localhost:5000', icon: Link2 },
  { label: 'Auth direct', url: 'http://localhost:5050', icon: ShieldCheck },
];

function normalizeBaseUrl(value: string) {
  return value.trim().replace(/\/+$/, '');
}

function prettyJson(value: unknown) {
  if (value === undefined || value === null || value === '') return '';
  if (typeof value === 'string') return value;
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

function findToken(payload: unknown): string | null {
  if (!payload || typeof payload !== 'object') return null;
  const record = payload as Record<string, unknown>;
  const candidates = [record.token, record.accessToken, record.access_token, record.jwt];
  for (const candidate of candidates) {
    if (typeof candidate === 'string' && candidate.length > 10) return candidate;
  }
  if (record.data && typeof record.data === 'object') {
    return findToken(record.data);
  }
  return null;
}

function findUserId(payload: unknown): string | null {
  if (!payload || typeof payload !== 'object') return null;
  const record = payload as Record<string, unknown>;
  const candidates = [record.id, record.userId, record.user_id];
  for (const candidate of candidates) {
    if (typeof candidate === 'string' && candidate.trim()) return candidate;
  }
  if (record.user && typeof record.user === 'object') return findUserId(record.user);
  if (record.data && typeof record.data === 'object') return findUserId(record.data);
  if (record.result && typeof record.result === 'object') return findUserId(record.result);
  return null;
}

function findStatus(payload: unknown): string | null {
  if (!payload || typeof payload !== 'object') return null;
  const record = payload as Record<string, unknown>;
  const keys = ['status', 'orderStatus', 'paymentStatus', 'state', 'sagaStatus'];
  for (const key of keys) {
    const value = record[key];
    if (typeof value === 'string') return value;
  }
  if (record.data && typeof record.data === 'object') return findStatus(record.data);
  if (record.order && typeof record.order === 'object') return findStatus(record.order);
  return null;
}

function detectDiagnostic(result: Pick<ApiResult, 'status' | 'error' | 'responseBody' | 'rawText'>) {
  const source = `${result.status ?? ''} ${result.error ?? ''} ${prettyJson(result.responseBody)} ${result.rawText ?? ''}`.toLowerCase();
  if (source.includes('cart') || source.includes('cart-api')) {
    return 'Diagnostic: order flow упал на cart-api. UI живой, это инфраструктурная/интеграционная ошибка, а не падение фронтенда.';
  }
  if (source.includes('grpc') || source.includes('rpc')) {
    return 'Diagnostic: похоже, проблема во внутреннем gRPC вызове. Проверь Order/Payment/Moderation logs в Seq.';
  }
  if (source.includes('failed to fetch') || source.includes('networkerror') || source.includes('cors')) {
    return 'Diagnostic: Gateway не отвечает, заблокирован CORS или указан неверный Base URL.';
  }
  if (result.status && result.status >= 500) {
    return 'Diagnostic: Gateway/сервис вернул 5xx. Смотри Seq logs и контейнеры, фронт только честно показывает пожар.';
  }
  return undefined;
}

async function parseResponse(response: Response) {
  const rawText = await response.text();
  if (!rawText) return { body: null, rawText: '' };
  try {
    return { body: JSON.parse(rawText), rawText };
  } catch {
    return { body: rawText, rawText };
  }
}

function App() {
  const [baseUrl, setBaseUrl] = useState(() => localStorage.getItem(STORAGE_BASE_URL) || DEFAULT_BASE_URL);
  const [token, setToken] = useState(() => localStorage.getItem(STORAGE_TOKEN) || '');
  const [showToken, setShowToken] = useState(false);
  const [copied, setCopied] = useState(false);
  const [lastResult, setLastResult] = useState<ApiResult | null>(null);
  const [history, setHistory] = useState<ApiResult[]>([]);
  const [states, setStates] = useState<Record<string, RequestState>>({});
  const [orderStatus, setOrderStatus] = useState<string>('not tested');

  const [registerForm, setRegisterForm] = useState({
    email: 'alex@example.com',
    password: 'Password123!',
    name: 'Alex',
  });
  const [loginForm, setLoginForm] = useState({
    email: 'alex@example.com',
    password: 'Password123!',
  });
  const [orderForm, setOrderForm] = useState({ userId: '1' });

  const [checklist, setChecklist] = useState<Record<ChecklistKey, boolean>>(() => {
    const start = {} as Record<ChecklistKey, boolean>;
    checklistItems.forEach((item) => {
      start[item.key] = false;
    });
    start.jwtSaved = Boolean(localStorage.getItem(STORAGE_TOKEN));
    return start;
  });

  const normalizedBaseUrl = useMemo(() => normalizeBaseUrl(baseUrl), [baseUrl]);
  const passedCount = Object.values(checklist).filter(Boolean).length;

  function setCheck(key: ChecklistKey, value = true) {
    setChecklist((prev) => ({ ...prev, [key]: value }));
  }

  function saveToken(nextToken: string) {
    localStorage.setItem(STORAGE_TOKEN, nextToken);
    setToken(nextToken);
    setCheck('jwtSaved', true);
  }

  function clearToken() {
    localStorage.removeItem(STORAGE_TOKEN);
    setToken('');
    setCheck('jwtSaved', false);
  }

  async function copyToken() {
    if (!token) return;
    await navigator.clipboard.writeText(token);
    setCopied(true);
    setTimeout(() => setCopied(false), 1200);
  }

  function saveBaseUrl() {
    const normalized = normalizeBaseUrl(baseUrl) || DEFAULT_BASE_URL;
    localStorage.setItem(STORAGE_BASE_URL, normalized);
    setBaseUrl(normalized);
  }

  function resetBaseUrl() {
    localStorage.removeItem(STORAGE_BASE_URL);
    setBaseUrl(DEFAULT_BASE_URL);
  }

  async function request(title: string, method: HttpMethod, path: string, body?: unknown, useJwt = false) {
    const requestKey = `${method} ${path}`;
    const url = `${normalizedBaseUrl}${path}`;
    const started = performance.now();
    setStates((prev) => ({ ...prev, [requestKey]: 'loading' }));

    try {
      const headers: Record<string, string> = { Accept: 'application/json' };
      if (body !== undefined) headers['Content-Type'] = 'application/json';
      if (useJwt && token) headers.Authorization = `Bearer ${token}`;

      const response = await fetch(url, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
      });

      const parsed = await parseResponse(response);
      const result: ApiResult = {
        title,
        method,
        path,
        url,
        status: response.status,
        ok: response.ok,
        durationMs: Math.round(performance.now() - started),
        timestamp: new Date().toISOString(),
        requestBody: body,
        responseBody: parsed.body,
        rawText: parsed.rawText,
      };
      result.diagnostic = detectDiagnostic(result);

      setLastResult(result);
      setHistory((prev) => [result, ...prev].slice(0, 12));
      setStates((prev) => ({ ...prev, [requestKey]: response.ok ? 'success' : 'error' }));
      return result;
    } catch (err) {
      const result: ApiResult = {
        title,
        method,
        path,
        url,
        ok: false,
        durationMs: Math.round(performance.now() - started),
        timestamp: new Date().toISOString(),
        requestBody: body,
        error: err instanceof Error ? err.message : String(err),
      };
      result.diagnostic = detectDiagnostic(result);

      setLastResult(result);
      setHistory((prev) => [result, ...prev].slice(0, 12));
      setStates((prev) => ({ ...prev, [requestKey]: 'error' }));
      return result;
    }
  }

  async function checkHealth() {
    const result = await request('Gateway Health', 'GET', '/health');
    if (result.ok) setCheck('gatewayHealth', true);
  }

  async function register() {
    const result = await request('Auth Register', 'POST', '/auth/register', registerForm);
    if (result.ok) {
      setCheck('authRegister', true);
      const nextToken = findToken(result.responseBody);
      if (nextToken) saveToken(nextToken);
      const userId = findUserId(result.responseBody);
      if (userId) setOrderForm({ userId });
    }
  }

  async function login() {
    const result = await request('Auth Login', 'POST', '/auth/login', loginForm);
    if (result.ok) {
      setCheck('authLogin', true);
      const nextToken = findToken(result.responseBody);
      if (nextToken) saveToken(nextToken);
      const userId = findUserId(result.responseBody);
      if (userId) setOrderForm({ userId });
    }
  }

  async function getMe() {
    const result = await request('Get Current User', 'GET', '/auth/me', undefined, true);
    if (result.ok) {
      setCheck('authMe', true);
      setCheck('authDb', true);
      const userId = findUserId(result.responseBody);
      if (userId) setOrderForm({ userId });
    }
  }

  async function createOrder() {
    setOrderStatus('loading');
    const payload = { userId: orderForm.userId.trim() };
    const result = await request('Create Order', 'POST', '/orders', payload, true);

    if (result.status) setCheck('ordersGateway', true);
    const status = findStatus(result.responseBody);
    if (status) {
      setOrderStatus(status);
      if (['paid', 'cancelled', 'pending'].includes(status.toLowerCase())) {
        setCheck('sagaFlow', true);
      }
    } else if (result.diagnostic) {
      setOrderStatus('diagnostic error');
    } else if (!result.ok) {
      setOrderStatus('error');
    } else {
      setOrderStatus('response received');
    }
  }

  function resetSmokeState() {
    setLastResult(null);
    setHistory([]);
    setStates({});
    setOrderStatus('not tested');
    setChecklist((prev) => {
      const next = { ...prev };
      checklistItems.forEach((item) => {
        next[item.key] = false;
      });
      next.jwtSaved = Boolean(token);
      return next;
    });
  }

  return (
    <main className="app-shell">
      <section className="hero-panel">
        <div>
          <div className="eyebrow">shop-platform smoke-test ui</div>
          <h1>Проверка Auth, Gateway, REST, Docker, gRPC, Worker, Consul, Seq и Saga flow</h1>
          <p>
            Это не production UI и не маркетинговая витрина. Это пульт ручной проверки, где формы,
            статусы, JSON и ошибки важнее красоты, наконец-то интерфейс делает полезную работу, а не позирует.
          </p>
        </div>
        <div className="score-card">
          <span>Criteria passed</span>
          <strong>{passedCount}/{checklistItems.length}</strong>
          <div className="progress"><i style={{ width: `${(passedCount / checklistItems.length) * 100}%` }} /></div>
        </div>
      </section>

      <section className="card config-card">
        <div className="section-title">
          <Server size={20} />
          <div>
            <h2>Base URL</h2>
            <p>По умолчанию Gateway proxy: /api</p>
          </div>
        </div>
        <div className="base-url-row">
          <input value={baseUrl} onChange={(e) => setBaseUrl(e.target.value)} placeholder="/api" />
          <button onClick={saveBaseUrl} className="btn primary">Save</button>
          <button onClick={resetBaseUrl} className="btn ghost">Reset</button>
        </div>
        <div className="token-row">
          <div className="token-box">
            <KeyRound size={16} />
            <span>{token ? (showToken ? token : `${token.slice(0, 22)}...${token.slice(-12)}`) : 'JWT token not saved yet'}</span>
          </div>
          <button className="btn ghost" onClick={() => setShowToken((value) => !value)} disabled={!token}>
            {showToken ? <EyeOff size={16} /> : <Eye size={16} />} {showToken ? 'Hide token' : 'Show token'}
          </button>
          <button className="btn ghost" onClick={copyToken} disabled={!token}>
            {copied ? <ClipboardCheck size={16} /> : <Clipboard size={16} />} {copied ? 'Copied' : 'Copy token'}
          </button>
          <button className="btn danger" onClick={clearToken} disabled={!token}>
            <Trash2 size={16} /> Clear token
          </button>
        </div>
      </section>

      <section className="dashboard-grid">
        <div className="stack">
          <Panel title="Auth" subtitle="Все auth-запросы идут через Gateway" icon={<ShieldCheck size={20} />}>
            <div className="two-cols">
              <div className="mini-card">
                <h3><UserPlus size={18} /> Register</h3>
                <input value={registerForm.email} onChange={(e) => setRegisterForm({ ...registerForm, email: e.target.value })} placeholder="email" />
                <input value={registerForm.name} onChange={(e) => setRegisterForm({ ...registerForm, name: e.target.value })} placeholder="name" />
                <input type="password" value={registerForm.password} onChange={(e) => setRegisterForm({ ...registerForm, password: e.target.value })} placeholder="password" />
                <ActionButton state={states['POST /auth/register']} onClick={register} label="POST /auth/register" />
              </div>

              <div className="mini-card">
                <h3><Lock size={18} /> Login</h3>
                <input value={loginForm.email} onChange={(e) => setLoginForm({ ...loginForm, email: e.target.value })} placeholder="email" />
                <input type="password" value={loginForm.password} onChange={(e) => setLoginForm({ ...loginForm, password: e.target.value })} placeholder="password" />
                <ActionButton state={states['POST /auth/login']} onClick={login} label="POST /auth/login" />
                <ActionButton state={states['GET /auth/me']} onClick={getMe} label="GET /auth/me" disabled={!token} tone="dark" />
              </div>
            </div>
          </Panel>

          <div className="split-grid">
            <Panel title="Gateway Health" subtitle="Проверка, что запрос идет через Gateway" icon={<Activity size={20} />}>
              <div className="endpoint-line"><b>GET</b><span>{normalizedBaseUrl}/health</span></div>
              <ActionButton state={states['GET /health']} onClick={checkHealth} label="Check Gateway Health" />
            </Panel>

            <Panel title="Orders" subtitle="Saga-like order flow через Gateway" icon={<ShoppingCart size={20} />}>
              <label className="field-label">userId</label>
              <input value={orderForm.userId} onChange={(e) => setOrderForm({ userId: e.target.value })} placeholder="user id from Auth" />
              <div className="endpoint-line"><b>POST</b><span>{normalizedBaseUrl}/orders</span></div>
              <ActionButton state={states['POST /orders']} onClick={createOrder} label="Create order" />
              <div className={`order-status ${orderStatus.toLowerCase().replace(/\s+/g, '-')}`}>
                <span>Order status</span>
                <strong>{orderStatus}</strong>
              </div>
            </Panel>
          </div>
        </div>

        <div className="stack">
          <Panel title="Infrastructure Links" subtitle="Открывай руками и сверяй логи, очереди, сервисы" icon={<ExternalLink size={20} />}>
            <div className="links-grid">
              {infraLinks.map((link) => {
                const Icon = link.icon;
                return (
                  <a key={link.url} className="infra-link" href={link.url} target="_blank" rel="noreferrer">
                    <Icon size={18} />
                    <span>{link.label}</span>
                    <small>{link.url}</small>
                    <ExternalLink size={14} className="external" />
                  </a>
                );
              })}
            </div>
          </Panel>

          <Panel title="Criteria Checklist" subtitle="Авто-пункты отмечаются запросами, инфраструктуру можно отмечать руками" icon={<CheckCircle2 size={20} />}>
            <div className="checklist">
              {checklistItems.map((item) => (
                <label key={item.key} className={`check-item ${checklist[item.key] ? 'done' : ''}`}>
                  <input
                    type="checkbox"
                    checked={checklist[item.key]}
                    onChange={(e) => setCheck(item.key, e.target.checked)}
                  />
                  <span>
                    <b>{item.label}</b>
                    <small>{item.hint}{item.auto ? ' Auto.' : ' Manual.'}</small>
                  </span>
                </label>
              ))}
            </div>
            <button className="btn ghost wide" onClick={resetSmokeState}><RefreshCcw size={16} /> Reset smoke state</button>
          </Panel>
        </div>
      </section>

      <section className="responses-grid">
        <Panel title="Raw API response" subtitle="Последний HTTP status, response body и ошибка, если она есть" icon={<Database size={20} />}>
          <RawResponse result={lastResult} />
        </Panel>

        <Panel title="Request History" subtitle="Последние 12 запросов, чтобы не играть в угадайку по памяти" icon={<Clipboard size={20} />}>
          <div className="history-list">
            {history.length === 0 && <div className="empty">Запросов пока нет.</div>}
            {history.map((item, index) => (
              <button key={`${item.timestamp}-${index}`} className="history-item" onClick={() => setLastResult(item)}>
                <StatusIcon ok={item.ok} status={item.status} />
                <span><b>{item.method}</b> {item.path}</span>
                <small>{item.status ?? 'ERR'} · {item.durationMs}ms</small>
              </button>
            ))}
          </div>
        </Panel>
      </section>
    </main>
  );
}

function Panel({ title, subtitle, icon, children }: { title: string; subtitle: string; icon: ReactNode; children: ReactNode }) {
  return (
    <section className="card">
      <div className="section-title">
        {icon}
        <div>
          <h2>{title}</h2>
          <p>{subtitle}</p>
        </div>
      </div>
      {children}
    </section>
  );
}

function ActionButton({ state, onClick, label, disabled, tone = 'primary' }: { state?: RequestState; onClick: () => void; label: string; disabled?: boolean; tone?: 'primary' | 'dark' }) {
  const loading = state === 'loading';
  return (
    <button className={`btn ${tone}`} onClick={onClick} disabled={disabled || loading}>
      {loading ? <Loader2 className="spin" size={16} /> : <Play size={16} />}
      {label}
    </button>
  );
}

function StatusIcon({ ok, status }: { ok: boolean; status?: number }) {
  if (ok) return <CheckCircle2 className="ok-icon" size={18} />;
  if (status) return <XCircle className="bad-icon" size={18} />;
  return <AlertTriangle className="warn-icon" size={18} />;
}

function RawResponse({ result }: { result: ApiResult | null }) {
  if (!result) {
    return <div className="empty">Сначала нажми любую кнопку запроса. Тут появится полный сырой ответ API.</div>;
  }

  return (
    <div className="raw-wrap">
      <div className="result-head">
        <StatusIcon ok={result.ok} status={result.status} />
        <div>
          <strong>{result.title}</strong>
          <span>{result.method} {result.url}</span>
        </div>
        <div className="status-code">{result.status ?? 'NETWORK ERROR'}</div>
      </div>

      <div className="meta-grid">
        <span><b>ok:</b> {String(result.ok)}</span>
        <span><b>duration:</b> {result.durationMs}ms</span>
        <span><b>time:</b> {result.timestamp}</span>
      </div>

      {result.diagnostic && <div className="diagnostic"><AlertTriangle size={18} /> {result.diagnostic}</div>}
      {result.error && <div className="error-box"><b>Error:</b> {result.error}</div>}

      {result.requestBody !== undefined && (
        <>
          <h3>Request body</h3>
          <pre>{prettyJson(result.requestBody)}</pre>
        </>
      )}

      <h3>Response body</h3>
      <pre>{prettyJson(result.responseBody ?? result.rawText ?? 'No response body')}</pre>
    </div>
  );
}

export default App;
