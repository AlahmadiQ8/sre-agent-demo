import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5170';
const DURATION = __ENV.DURATION || '3m';
const ENABLED_SCENARIOS = (__ENV.ENABLED_SCENARIOS || '').split(',').filter(Boolean);

// Scenario definitions: each has an executor config and an exec function
const allScenarios = {
  'memory-leak': {
    executor: 'constant-vus',
    vus: 1,
    duration: DURATION,
    exec: 'memoryLeak',
    gracefulStop: '10s',
  },
  'cpu-spike': {
    executor: 'constant-vus',
    vus: 1,
    duration: DURATION,
    exec: 'cpuSpike',
    gracefulStop: '10s',
  },
  'http-500': {
    executor: 'constant-vus',
    vus: 2,
    duration: DURATION,
    exec: 'http500',
    gracefulStop: '10s',
  },
  'db-failure': {
    executor: 'constant-vus',
    vus: 2,
    duration: DURATION,
    exec: 'dbFailure',
    gracefulStop: '10s',
  },
  'slow-api': {
    executor: 'constant-vus',
    vus: 5,
    duration: DURATION,
    exec: 'slowApi',
    gracefulStop: '35s',
  },
  'dependency-timeout': {
    executor: 'constant-vus',
    vus: 1,
    duration: DURATION,
    exec: 'dependencyTimeout',
    gracefulStop: '35s',
  },
  'log-flood': {
    executor: 'constant-vus',
    vus: 1,
    duration: DURATION,
    exec: 'logFlood',
    gracefulStop: '10s',
  },
  'exception-storm': {
    executor: 'constant-vus',
    vus: 2,
    duration: DURATION,
    exec: 'exceptionStorm',
    gracefulStop: '10s',
  },
};

// Build scenarios object dynamically based on ENABLED_SCENARIOS
function buildScenarios() {
  if (ENABLED_SCENARIOS.length === 0) {
    return {};
  }

  const scenarios = {};
  let startOffset = 0;

  for (const name of ENABLED_SCENARIOS) {
    if (allScenarios[name]) {
      scenarios[name] = Object.assign({}, allScenarios[name], {
        startTime: `${startOffset}s`,
      });
      // Parse duration to calculate offset for sequential execution with 30s gap
      startOffset += parseDuration(DURATION) + 30;
    }
  }
  return scenarios;
}

function parseDuration(d) {
  const match = d.match(/^(\d+)(s|m|h)$/);
  if (!match) return 180; // default 3 minutes in seconds
  const val = parseInt(match[1], 10);
  switch (match[2]) {
    case 's': return val;
    case 'm': return val * 60;
    case 'h': return val * 3600;
    default: return 180;
  }
}

export const options = {
  scenarios: buildScenarios(),
  // No thresholds — these are traffic generators, not performance tests
};

// Track whether each scenario's chaos trigger has been fired
const triggered = {};

// ── Scenario: memory-leak (A1, A2) ──────────────────────────────────────────

export function memoryLeak() {
  if (!triggered['memory-leak']) {
    triggered['memory-leak'] = true;
    const res = http.post(
      `${BASE_URL}/api/reports/annual-statement?accountId=1`,
      null,
      { tags: { name: 'POST /api/reports/annual-statement' } }
    );
    check(res, { 'memory-leak trigger sent': (r) => r.status < 500 || r.status >= 200 });
  }
  // Poll health to observe degradation
  const health = http.get(`${BASE_URL}/health/ready`, { tags: { name: '/health/ready' } });
  check(health, { 'health check responded': (r) => r.status !== 0 });
  sleep(5);
}

// ── Scenario: cpu-spike (A3) ────────────────────────────────────────────────

export function cpuSpike() {
  if (!triggered['cpu-spike']) {
    triggered['cpu-spike'] = true;
    const res = http.post(
      `${BASE_URL}/api/accounts/fraud-detection`,
      null,
      { tags: { name: 'POST /api/accounts/fraud-detection' } }
    );
    check(res, { 'cpu-spike trigger sent': (r) => r.status >= 200 });
  }
  // Send requests to show latency impact
  const res = http.get(`${BASE_URL}/api/accounts`, { tags: { name: '/api/accounts' } });
  check(res, { 'accounts responded': (r) => r.status !== 0 });
  sleep(2);
}

// ── Scenario: http-500 (A4) ─────────────────────────────────────────────────

export function http500() {
  if (!triggered['http-500']) {
    triggered['http-500'] = true;
    const payload = JSON.stringify({
      fromAccountId: 1,
      toAccountId: 2,
      amount: 100,
    });
    http.post(`${BASE_URL}/api/transfers/wire`, payload, {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'POST /api/transfers/wire' },
    });
  }
  // Rapid-fire wire transfer POSTs to accumulate 5xx responses (need >10)
  const payload = JSON.stringify({
    fromAccountId: 1,
    toAccountId: 2,
    amount: 100,
  });
  const res = http.post(`${BASE_URL}/api/transfers/wire`, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'POST /api/transfers/wire' },
  });
  check(res, { 'wire transfer responded': (r) => r.status !== 0 });
  sleep(1);
}

// ── Scenario: db-failure (A5, A10) ──────────────────────────────────────────

export function dbFailure() {
  if (!triggered['db-failure']) {
    triggered['db-failure'] = true;
    const res = http.post(
      `${BASE_URL}/api/accounts/refresh`,
      null,
      { tags: { name: 'POST /api/accounts/refresh' } }
    );
    check(res, { 'db-failure trigger sent': (r) => r.status >= 200 });
  }
  // Rapid GET on DB-dependent endpoints to accumulate SQL failures
  http.get(`${BASE_URL}/api/accounts`, { tags: { name: '/api/accounts' } });
  http.get(`${BASE_URL}/api/transactions`, { tags: { name: '/api/transactions' } });
  http.get(`${BASE_URL}/health/ready`, { tags: { name: '/health/ready' } });
  sleep(1);
}

// ── Scenario: slow-api (A6) ─────────────────────────────────────────────────

export function slowApi() {
  if (!triggered['slow-api']) {
    triggered['slow-api'] = true;
    const payload = JSON.stringify({
      fromAccountId: 1,
      toAccountId: 2,
      amount: 500,
    });
    http.post(`${BASE_URL}/api/transfers/international`, payload, {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'POST /api/transfers/international' },
      timeout: '60s',
    });
  }
  // Keep sending concurrent requests (each takes ~30s, stacking P95)
  const payload = JSON.stringify({
    fromAccountId: Math.floor(Math.random() * 5) + 1,
    toAccountId: Math.floor(Math.random() * 5) + 1,
    amount: 500,
  });
  const res = http.post(`${BASE_URL}/api/transfers/international`, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'POST /api/transfers/international' },
    timeout: '60s',
  });
  check(res, { 'international transfer responded': (r) => r.status !== 0 });
  sleep(1);
}

// ── Scenario: dependency-timeout (A7) ───────────────────────────────────────

export function dependencyTimeout() {
  // Each call ties up a thread; need >3 timeouts
  const res = http.post(
    `${BASE_URL}/api/settings/verify-identity`,
    null,
    {
      tags: { name: 'POST /api/settings/verify-identity' },
      timeout: '60s',
    }
  );
  check(res, { 'verify-identity responded': (r) => r.status !== 0 });
  sleep(10);
}

// ── Scenario: log-flood (A8) ────────────────────────────────────────────────

export function logFlood() {
  if (!triggered['log-flood']) {
    triggered['log-flood'] = true;
    const res = http.post(
      `${BASE_URL}/api/transactions/export`,
      null,
      { tags: { name: 'POST /api/transactions/export' } }
    );
    check(res, { 'log-flood trigger sent': (r) => r.status >= 200 });
  }
  // Send requests to show sluggishness while logs flood
  const res = http.get(`${BASE_URL}/api/accounts`, { tags: { name: '/api/accounts' } });
  check(res, { 'accounts responded during log flood': (r) => r.status !== 0 });
  sleep(2);
}

// ── Scenario: exception-storm (A9) ──────────────────────────────────────────

export function exceptionStorm() {
  // Fire reconciliation repeatedly — each spawns parallel exception tasks (need >50 total)
  const res = http.post(
    `${BASE_URL}/api/reports/reconciliation`,
    null,
    { tags: { name: 'POST /api/reports/reconciliation' } }
  );
  check(res, { 'reconciliation responded': (r) => r.status !== 0 });
  sleep(5);
}
