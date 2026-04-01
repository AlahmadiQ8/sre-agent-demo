import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5170';
const DURATION = __ENV.DURATION || '30m';

export const options = {
  scenarios: {
    browse: {
      executor: 'constant-vus',
      vus: 5,
      duration: DURATION,
      exec: 'browse',
    },
    transact: {
      executor: 'constant-vus',
      vus: 2,
      duration: DURATION,
      exec: 'transact',
    },
    health: {
      executor: 'constant-vus',
      vus: 1,
      duration: DURATION,
      exec: 'healthCheck',
    },
  },
};

const endpoints = [
  '/api/accounts',
  '/api/transactions',
  '/api/transfers',
  '/api/settings/profile',
];

export function browse() {
  const endpoint = endpoints[Math.floor(Math.random() * endpoints.length)];
  const res = http.get(`${BASE_URL}${endpoint}`, { tags: { name: endpoint } });
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(2 + Math.random() * 3); // 2–5s
}

export function transact() {
  const fromId = Math.floor(Math.random() * 5) + 1; // 1–5
  let toId = Math.floor(Math.random() * 5) + 1;
  while (toId === fromId) {
    toId = Math.floor(Math.random() * 5) + 1;
  }

  const payload = JSON.stringify({
    fromAccountId: fromId,
    toAccountId: toId,
    amount: Math.floor(Math.random() * 500) + 10,
  });

  const res = http.post(`${BASE_URL}/api/transfers`, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: '/api/transfers' },
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(5 + Math.random() * 5); // 5–10s
}

export function healthCheck() {
  const ready = http.get(`${BASE_URL}/health/ready`, { tags: { name: '/health/ready' } });
  check(ready, { 'ready status is 200': (r) => r.status === 200 });

  const live = http.get(`${BASE_URL}/health/live`, { tags: { name: '/health/live' } });
  check(live, { 'live status is 200': (r) => r.status === 200 });

  sleep(10);
}
