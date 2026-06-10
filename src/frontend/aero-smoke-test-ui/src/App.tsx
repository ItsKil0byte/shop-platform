import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  Activity,
  AlertTriangle,
  Check,
  CheckCircle2,
  Clipboard,
  ClipboardCheck,
  Database,
  ExternalLink,
  Eye,
  EyeOff,
  Gauge,
  Globe2,
  KeyRound,
  Link2,
  Loader2,
  LockKeyhole,
  Network,
  Play,
  RefreshCcw,
  Search,
  Server,
  ShieldCheck,
  ShoppingBag,
  ShoppingCart,
  Sparkles,
  TerminalSquare,
  Trash2,
  User,
  UserPlus,
  Waves,
  XCircle,
} from 'lucide-react';

type HttpMethod = 'GET' | 'POST';
type RequestState = 'idle' | 'loading' | 'success' | 'error';

type ApiResult = {
  id: string;
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

type RequestKey = 'register' | 'login' | 'me' | 'health' | 'order';

const STORAGE_TOKEN = 'shop-platform.jwt';
const STORAGE_BASE_URL = 'shop-platform.baseUrl';
const DEFAULT_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';

const checklistItems: ChecklistItem[] = [
  { key: 'dockerCompose', label: 'Docker Compose поднят', hint: 'Проверяется по контейнерам и доступности UI/сервисов.' },
  { key: 'gatewayHealth', label: 'Gateway отвечает', hint: 'Автоматически отмечается после успешного GET /health.', auto: true },
  { key: 'authRegister', label: 'Auth register работает', hint: 'Автоматически отмечается после успешного POST /auth/register.', auto: true },
  { key: 'authLogin', label: 'Auth login работает', hint: 'Автоматически отмечается после успешного POST /auth/login.', auto: true },
  { key: 'jwtSaved', label: 'JWT сохраняется', hint: 'Автоматически отмечается, когда token найден и записан в localStorage.', auto: true },
  { key: 'authMe', label: '/auth/me работает по JWT', hint: 'Автоматически отмечается после успешного GET /auth/me.', auto: true },
  { key: 'authDb', label: 'Данные пользователя сохраняются в AuthDb', hint: 'Отметь вручную после проверки БД или успешного /auth/me.' },
  { key: 'ordersGateway', label: 'Order API доступен через Gateway', hint: 'Отмечается после HTTP-ответа от POST /orders через Gateway.', auto: true },
  { key: 'ordersGrpc', label: 'Order вызывает внутренние gRPC сервисы', hint: 'Отметь после проверки Seq logs order/payment/moderation.' },
  { key: 'paymentGrpc', label: 'Payment service работает через gRPC', hint: 'Отметь после проверки логов или статуса order flow.' },
  { key: 'moderationGrpc', label: 'Moderation service работает через gRPC', hint: 'Отметь после проверки логов или статуса order flow.' },
  { key: 'rabbitMq', label: 'RabbitMQ доступен', hint: 'Открой RabbitMQ UI и проверь connections/queues.' },
  { key: 'workerRabbitMq', label: 'Notification worker подключается к RabbitMQ', hint: 'Проверяется по RabbitMQ connections и логам worker.' },
  { key: 'consul', label: 'Consul доступен', hint: 'Открой Consul UI и проверь services.' },
  { key: 'paymentConsul', label: 'Payment service регистрируется в Consul', hint: 'Проверяется в Consul UI.' },
  { key: 'seqLogs', label: 'Seq получает логи', hint: 'Открой Seq и проверь события после Auth/Order запросов.' },
  { key: 'sagaFlow', label: 'Saga-like flow заказа виден по статусам/логам', hint: 'Отмечается при Paid/Cancelled/Pending или вручную по логам.', auto: true },
];

const infraLinks = [
  { label: 'Seq logs', description: 'structured events', url: 'http://localhost:5341', icon: Activity },
  { label: 'RabbitMQ UI', description: 'queues / connections', url: 'http://localhost:15672', icon: Network },
  { label: 'Consul UI', description: 'service discovery', url: 'http://localhost:8500', icon: Server },
  { label: 'Gateway', description: 'public entrypoint', url: 'http://localhost:5000', icon: Link2 },
  { label: 'Auth direct', description: 'direct service check', url: 'http://localhost:5050', icon: ShieldCheck },
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
  if (record.data && typeof record.data === 'object') return findToken(record.data);
  if (record.result && typeof record.result === 'object') return findToken(record.result);
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
  if (record.result && typeof record.result === 'object') return findStatus(record.result);
  return null;
}

function isSagaStatus(value: string) {
  return ['paid', 'cancelled', 'canceled', 'pending'].includes(value.toLowerCase());
}

function detectDiagnostic(result: Pick<ApiResult, 'status' | 'error' | 'responseBody' | 'rawText'>) {
  const source = `${result.status ?? ''} ${result.error ?? ''} ${prettyJson(result.responseBody)} ${result.rawText ?? ''}`.toLowerCase();
  if (source.includes('cart') || source.includes('cart-api')) {
    return 'Diagnostic: order flow упал на cart-api. UI живой, это интеграционная ошибка, а не падение фронтенда.';
  }
  if (source.includes('grpc') || source.includes('rpc')) {
    return 'Diagnostic: похоже, проблема во внутреннем gRPC вызове. Проверь Order/Payment/Moderation logs в Seq.';
  }
  if (source.includes('failed to fetch') || source.includes('networkerror') || source.includes('cors')) {
    return 'Diagnostic: Gateway не отвечает, заблокирован CORS или указан неверный Base URL.';
  }
  if (result.status && result.status >= 500) {
    return 'Diagnostic: Gateway или сервис вернул 5xx. Смотри Seq logs и контейнеры, фронт только показывает пожар.';
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

function Badge({ children, variant = 'idle' }: { children: ReactNode; variant?: RequestState | 'token' | 'warn' }) {
  return <span className={`aero-badge aero-badge-${variant}`}>{children}</span>;
}

function Panel({ title, subtitle, icon, children, className = '' }: { title: string; subtitle?: string; icon: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={`glass-panel ${className}`}>
      <div className="panel-head">
        <div className="panel-icon">{icon}</div>
        <div>
          <h2>{title}</h2>
          {subtitle ? <p>{subtitle}</p> : null}
        </div>
      </div>
      {children}
    </section>
  );
}

function TextInput({ label, value, onChange, type = 'text', placeholder }: { label: string; value: string; onChange: (value: string) => void; type?: string; placeholder?: string }) {
  return (
    <label className="field">
      <span>{label}</span>
      <input type={type} value={value} placeholder={placeholder} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function PrimaryButton({ children, onClick, loading = false, disabled = false, variant = 'blue' }: { children: ReactNode; onClick: () => void; loading?: boolean; disabled?: boolean; variant?: 'blue' | 'green' | 'ghost' | 'danger' }) {
  return (
    <button className={`aero-button aero-button-${variant}`} onClick={onClick} disabled={disabled || loading}>
      {loading ? <Loader2 className="spin" size={16} /> : null}
      {children}
    </button>
  );
}

function App() {
  const [baseUrl, setBaseUrl] = useState(() => localStorage.getItem(STORAGE_BASE_URL) || DEFAULT_BASE_URL);
  const [token, setToken] = useState(() => localStorage.getItem(STORAGE_TOKEN) || '');
  const [showToken, setShowToken] = useState(false);
  const [copied, setCopied] = useState(false);
  const [lastResult, setLastResult] = useState<ApiResult | null>(null);
  const [history, setHistory] = useState<ApiResult[]>([]);
  const [states, setStates] = useState<Record<RequestKey, RequestState>>({ register: 'idle', login: 'idle', me: 'idle', health: 'idle', order: 'idle' });
  const [orderStatus, setOrderStatus] = useState('not tested');

  const [registerForm, setRegisterForm] = useState({ email: 'alex@example.com', password: 'Password123!', name: 'Alex' });
  const [loginForm, setLoginForm] = useState({ email: 'alex@example.com', password: 'Password123!' });
  const [orderForm, setOrderForm] = useState({ userId: '1' });

  const [checklist, setChecklist] = useState<Record<ChecklistKey, boolean>>(() => {
    const initial = {} as Record<ChecklistKey, boolean>;
    checklistItems.forEach((item) => {
      initial[item.key] = false;
    });
    initial.jwtSaved = Boolean(localStorage.getItem(STORAGE_TOKEN));
    return initial;
  });

  const normalizedBaseUrl = useMemo(() => normalizeBaseUrl(baseUrl), [baseUrl]);
  const passedCount = Object.values(checklist).filter(Boolean).length;
  const completion = Math.round((passedCount / checklistItems.length) * 100);

  function setCheck(key: ChecklistKey, value = true) {
    setChecklist((previous) => ({ ...previous, [key]: value }));
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
    window.setTimeout(() => setCopied(false), 1400);
  }

  function saveBaseUrl() {
    const normalized = normalizeBaseUrl(baseUrl || DEFAULT_BASE_URL);
    localStorage.setItem(STORAGE_BASE_URL, normalized);
    setBaseUrl(normalized);
  }

  async function requestApi(options: {
    key: RequestKey;
    title: string;
    method: HttpMethod;
    path: string;
    body?: unknown;
    auth?: boolean;
    onSuccess?: (result: ApiResult) => void;
    onAnyResponse?: (result: ApiResult) => void;
  }) {
    const started = performance.now();
    setStates((previous) => ({ ...previous, [options.key]: 'loading' }));

    const url = `${normalizedBaseUrl}${options.path}`;
    const headers: HeadersInit = { Accept: 'application/json' };
    if (options.method === 'POST') headers['Content-Type'] = 'application/json';
    if (options.auth && token) headers.Authorization = `Bearer ${token}`;

    let result: ApiResult;
    try {
      const response = await fetch(url, {
        method: options.method,
        headers,
        body: options.body === undefined ? undefined : JSON.stringify(options.body),
      });
      const parsed = await parseResponse(response);
      result = {
        id: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
        title: options.title,
        method: options.method,
        path: options.path,
        url,
        status: response.status,
        ok: response.ok,
        durationMs: Math.round(performance.now() - started),
        timestamp: new Date().toLocaleTimeString(),
        requestBody: options.body,
        responseBody: parsed.body,
        rawText: parsed.rawText,
      };
      result.diagnostic = detectDiagnostic(result);
      setStates((previous) => ({ ...previous, [options.key]: response.ok ? 'success' : 'error' }));
      if (response.ok) options.onSuccess?.(result);
      options.onAnyResponse?.(result);
    } catch (error) {
      result = {
        id: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
        title: options.title,
        method: options.method,
        path: options.path,
        url,
        ok: false,
        durationMs: Math.round(performance.now() - started),
        timestamp: new Date().toLocaleTimeString(),
        requestBody: options.body,
        error: error instanceof Error ? error.message : String(error),
      };
      result.diagnostic = detectDiagnostic(result);
      setStates((previous) => ({ ...previous, [options.key]: 'error' }));
      options.onAnyResponse?.(result);
    }

    setLastResult(result);
    setHistory((previous) => [result, ...previous].slice(0, 12));
  }

  function register() {
    requestApi({
      key: 'register',
      title: 'Auth register',
      method: 'POST',
      path: '/auth/register',
      body: registerForm,
      onSuccess: (result) => {
        setCheck('authRegister');
        const nextToken = findToken(result.responseBody);
        if (nextToken) saveToken(nextToken);
        const userId = findUserId(result.responseBody);
        if (userId) setOrderForm({ userId });
      },
    });
  }

  function login() {
    requestApi({
      key: 'login',
      title: 'Auth login',
      method: 'POST',
      path: '/auth/login',
      body: loginForm,
      onSuccess: (result) => {
        setCheck('authLogin');
        const nextToken = findToken(result.responseBody);
        if (nextToken) saveToken(nextToken);
        const userId = findUserId(result.responseBody);
        if (userId) setOrderForm({ userId });
      },
    });
  }

  function getMe() {
    requestApi({
      key: 'me',
      title: 'Get current user',
      method: 'GET',
      path: '/auth/me',
      auth: true,
      onSuccess: (result) => {
        setCheck('authMe');
        const userId = findUserId(result.responseBody);
        if (userId) setOrderForm({ userId });
      },
    });
  }

  function health() {
    requestApi({
      key: 'health',
      title: 'Gateway health',
      method: 'GET',
      path: '/health',
      onSuccess: () => setCheck('gatewayHealth'),
    });
  }

  function createOrder() {
    const body = { userId: orderForm.userId.trim() };
    requestApi({
      key: 'order',
      title: 'Create order',
      method: 'POST',
      path: '/orders',
      body,
      onAnyResponse: (result) => {
        if (result.status) setCheck('ordersGateway');
        const status = findStatus(result.responseBody) || (result.error ? 'error' : result.ok ? 'Pending' : 'error');
        setOrderStatus(status);
        if (isSagaStatus(status)) setCheck('sagaFlow');
      },
    });
  }

  const sagaSteps = [
    { label: 'Gateway', active: checklist.gatewayHealth },
    { label: 'Order API', active: checklist.ordersGateway },
    { label: 'gRPC', active: checklist.paymentGrpc || checklist.moderationGrpc || checklist.ordersGrpc },
    { label: orderStatus, active: orderStatus !== 'not tested' },
  ];

  return (
    <div className="aero-page">
      <div className="bg-droplet bg-droplet-a" />
      <div className="bg-droplet bg-droplet-b" />
      <div className="bg-droplet bg-droplet-c" />
      <div className="aero-shell">
        <aside className="sidebar glass-panel">
          <div className="brand">
            <div className="brand-icon"><ShoppingBag size={28} /></div>
            <div>
              <strong>FuturaShop</strong>
              <span>shop-platform test console</span>
            </div>
          </div>

          <nav className="side-nav">
            {[
              ['Gateway', Globe2],
              ['Auth', ShieldCheck],
              ['Orders', ShoppingCart],
              ['Infrastructure', Network],
              ['Checklist', CheckCircle2],
              ['Raw response', TerminalSquare],
            ].map(([label, Icon]) => {
              const IconCmp = Icon as typeof Globe2;
              return (
                <a key={String(label)} href={`#${String(label).toLowerCase().replace(' ', '-')}`} className={label === 'Auth' ? 'active' : ''}>
                  <IconCmp size={19} />
                  <span>{String(label)}</span>
                </a>
              );
            })}
          </nav>

          <div className="smoke-card">
            <Waves size={24} />
            <b>Smoke-test mode</b>
            <p>Это не лендинг, а жидко-стеклянная панель проверки сервисов, чтобы дизайн не улетел в мусорку второй раз.</p>
            <div className="mini-progress"><i style={{ width: `${completion}%` }} /></div>
            <span>{passedCount}/{checklistItems.length} criteria</span>
          </div>
        </aside>

        <main className="main-area">
          <header className="topbar">
            <div className="search-glass">
              <Search size={20} />
              <input value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} onBlur={saveBaseUrl} placeholder="/api" />
              <button onClick={saveBaseUrl}>Save</button>
            </div>
            <div className="top-actions glass-panel">
              <Badge variant={states.health === 'success' ? 'success' : states.health === 'error' ? 'error' : 'idle'}>{states.health === 'success' ? 'Gateway OK' : 'Gateway'}</Badge>
              <Badge variant={token ? 'token' : 'warn'}>{token ? 'JWT saved' : 'No token'}</Badge>
              <div className="avatar">A</div>
              <div className="avatar-text"><strong>Hi, Alex</strong><span>Admin</span></div>
            </div>
          </header>

          <section className="hero-grid">
            <Panel title="Saga-like Order Flow" subtitle="Ручная проверка order flow через Gateway, gRPC-сервисы и логи" icon={<ShoppingCart size={22} />} className="pipeline-panel" >
              <div className="pipeline-row">
                {sagaSteps.map((step, index) => (
                  <div className="pipeline-step" key={`${step.label}-${index}`}>
                    <div className={`liquid-orb ${step.active ? 'active' : ''}`}><span>{index + 1}</span></div>
                    <b>{step.label}</b>
                    {index < sagaSteps.length - 1 ? <div className={`flow-line ${step.active ? 'active' : ''}`} /> : null}
                  </div>
                ))}
              </div>
              <div className="order-status-strip">
                <span>Order status</span>
                <strong className={orderStatus.toLowerCase()}>{orderStatus}</strong>
              </div>
            </Panel>

            <Panel title="Test Overview" subtitle="Автоматические статусы текущей проверки" icon={<Gauge size={22} />} className="overview-panel">
              <div className="overview-grid">
                <Metric label="Gateway" value={states.health} state={states.health} />
                <Metric label="Auth" value={checklist.authLogin || checklist.authRegister ? 'success' : 'idle'} state={checklist.authLogin || checklist.authRegister ? 'success' : 'idle'} />
                <Metric label="JWT" value={token ? 'saved' : 'empty'} state={token ? 'success' : 'idle'} />
                <Metric label="Orders" value={orderStatus} state={orderStatus === 'error' ? 'error' : orderStatus !== 'not tested' ? 'success' : 'idle'} />
              </div>
            </Panel>
          </section>

          <section className="work-grid">
            <Panel title="Gateway Health" subtitle="GET /health, запрос идет через Gateway" icon={<Globe2 size={21} />} className="gateway-panel" >
              <div className="endpoint-line"><code>GET</code><span>{normalizedBaseUrl}/health</span></div>
              <PrimaryButton onClick={health} loading={states.health === 'loading'}><Play size={16} /> Check Gateway</PrimaryButton>
            </Panel>

            <Panel title="Auth" subtitle="Register, Login, Get current user, JWT localStorage" icon={<ShieldCheck size={21} />} className="auth-panel" >
              <div className="forms-two">
                <div className="form-box">
                  <h3><UserPlus size={17} /> Register</h3>
                  <TextInput label="email" value={registerForm.email} onChange={(email) => setRegisterForm((previous) => ({ ...previous, email }))} />
                  <TextInput label="password" type="password" value={registerForm.password} onChange={(password) => setRegisterForm((previous) => ({ ...previous, password }))} />
                  <TextInput label="name" value={registerForm.name} onChange={(name) => setRegisterForm((previous) => ({ ...previous, name }))} />
                  <PrimaryButton onClick={register} loading={states.register === 'loading'}><Play size={16} /> POST /auth/register</PrimaryButton>
                </div>

                <div className="form-box">
                  <h3><KeyRound size={17} /> Login</h3>
                  <TextInput label="email" value={loginForm.email} onChange={(email) => setLoginForm((previous) => ({ ...previous, email }))} />
                  <TextInput label="password" type="password" value={loginForm.password} onChange={(password) => setLoginForm((previous) => ({ ...previous, password }))} />
                  <PrimaryButton onClick={login} loading={states.login === 'loading'}><Play size={16} /> POST /auth/login</PrimaryButton>
                  <PrimaryButton variant="green" onClick={getMe} loading={states.me === 'loading'} disabled={!token}><User size={16} /> GET /auth/me</PrimaryButton>
                </div>
              </div>

              <div className="token-console">
                <div>
                  <span>JWT token</span>
                  <code>{token ? (showToken ? token : `${token.slice(0, 26)}...${token.slice(-12)}`) : 'empty'}</code>
                </div>
                <div className="token-actions">
                  <PrimaryButton variant="ghost" onClick={() => setShowToken((value) => !value)} disabled={!token}>{showToken ? <EyeOff size={16} /> : <Eye size={16} />} {showToken ? 'Hide' : 'Show'}</PrimaryButton>
                  <PrimaryButton variant="ghost" onClick={copyToken} disabled={!token}>{copied ? <ClipboardCheck size={16} /> : <Clipboard size={16} />} Copy token</PrimaryButton>
                  <PrimaryButton variant="danger" onClick={clearToken} disabled={!token}><Trash2 size={16} /> Clear token</PrimaryButton>
                </div>
              </div>
            </Panel>

            <Panel title="Orders" subtitle="POST /orders через Gateway, diagnostic error не ломает UI" icon={<ShoppingCart size={21} />} className="orders-panel" >
              <TextInput label="userId" value={orderForm.userId} onChange={(userId) => setOrderForm({ userId })} />
              <div className="endpoint-line"><code>POST</code><span>{normalizedBaseUrl}/orders</span></div>
              <PrimaryButton variant="green" onClick={createOrder} loading={states.order === 'loading'}><Play size={16} /> Create order</PrimaryButton>
              <div className="status-card">
                <span>Current order status</span>
                <strong className={orderStatus.toLowerCase()}>{orderStatus}</strong>
              </div>
            </Panel>
          </section>

          <section className="bottom-grid">
            <Panel title="Infrastructure Links" subtitle="Быстрые входы в сервисы инфраструктуры" icon={<Network size={21} />} className="infra-panel" >
              <div className="infra-grid">
                {infraLinks.map((link) => {
                  const Icon = link.icon;
                  return (
                    <a href={link.url} target="_blank" rel="noreferrer" className="infra-link" key={link.label}>
                      <div><Icon size={22} /></div>
                      <strong>{link.label}</strong>
                      <span>{link.description}</span>
                      <small>{link.url}</small>
                      <ExternalLink size={15} />
                    </a>
                  );
                })}
              </div>
            </Panel>

            <Panel title="Criteria Checklist" subtitle="Авто-пункты отмечаются запросами, инфраструктуру можно отметить вручную" icon={<CheckCircle2 size={21} />} className="checklist-panel" >
              <div className="checklist-toolbar">
                <span>{passedCount}/{checklistItems.length} passed</span>
                <button onClick={() => {
                  const reset = {} as Record<ChecklistKey, boolean>;
                  checklistItems.forEach((item) => { reset[item.key] = false; });
                  reset.jwtSaved = Boolean(token);
                  setChecklist(reset);
                }}><RefreshCcw size={15} /> Reset manual checks</button>
              </div>
              <div className="checklist-list">
                {checklistItems.map((item) => (
                  <label className={`check-row ${checklist[item.key] ? 'checked' : ''}`} key={item.key}>
                    <input type="checkbox" checked={checklist[item.key]} onChange={(event) => setCheck(item.key, event.target.checked)} />
                    <span><Check size={14} /></span>
                    <div>
                      <b>{item.label}</b>
                      <small>{item.hint}</small>
                    </div>
                    {item.auto ? <em>auto</em> : <em>manual</em>}
                  </label>
                ))}
              </div>
            </Panel>
          </section>

          <section id="raw-response" className="raw-grid">
            <Panel title="Raw API response" subtitle="Последний HTTP status, response body и ошибка, если она есть" icon={<TerminalSquare size={21} />} className="raw-panel" >
              {lastResult ? <ResultView result={lastResult} /> : <div className="empty-state"><TerminalSquare size={28} />Запусти любой запрос, тут появится сырой ответ API.</div>}
            </Panel>

            <Panel title="Request History" subtitle="Последние запросы без угадывания и косметики" icon={<Activity size={21} />} className="history-panel" >
              <div className="history-list">
                {history.length === 0 ? <div className="empty-state small">История пока пустая.</div> : history.map((item) => (
                  <button key={item.id} className={`history-item ${item.ok ? 'ok' : 'bad'}`} onClick={() => setLastResult(item)}>
                    <span>{item.method}</span>
                    <b>{item.title}</b>
                    <small>{item.status ?? 'ERR'} · {item.durationMs}ms · {item.timestamp}</small>
                  </button>
                ))}
              </div>
            </Panel>
          </section>
        </main>
      </div>
    </div>
  );
}

function Metric({ label, value, state }: { label: string; value: string; state: RequestState | 'saved' | 'empty' | 'success' }) {
  const isOk = state === 'success' || value === 'saved';
  const isError = state === 'error';
  return (
    <div className={`metric-card ${isOk ? 'ok' : isError ? 'bad' : ''}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <svg viewBox="0 0 100 30" aria-hidden="true">
        <polyline points="4,22 19,18 33,20 48,13 64,15 80,9 96,5" />
      </svg>
    </div>
  );
}

function ResultView({ result }: { result: ApiResult }) {
  return (
    <div className="result-view">
      <div className="result-head">
        <div>
          <strong>{result.title}</strong>
          <span>{result.method} {result.url}</span>
        </div>
        <Badge variant={result.ok ? 'success' : 'error'}>{result.status ?? 'NETWORK ERROR'}</Badge>
      </div>

      <div className="meta-grid">
        <span>Duration: <b>{result.durationMs}ms</b></span>
        <span>Time: <b>{result.timestamp}</b></span>
        <span>Path: <b>{result.path}</b></span>
      </div>

      {result.error ? <div className="diagnostic error"><AlertTriangle size={18} />{result.error}</div> : null}
      {result.diagnostic ? <div className="diagnostic"><AlertTriangle size={18} />{result.diagnostic}</div> : null}

      {result.requestBody !== undefined ? (
        <div className="code-section">
          <h3>Request body</h3>
          <pre>{prettyJson(result.requestBody)}</pre>
        </div>
      ) : null}

      <div className="code-section">
        <h3>Response body</h3>
        <pre>{prettyJson(result.responseBody ?? result.rawText ?? result.error ?? 'empty')}</pre>
      </div>
    </div>
  );
}

export default App;
