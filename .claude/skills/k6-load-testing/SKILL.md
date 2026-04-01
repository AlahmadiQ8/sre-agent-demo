---
name: k6-load-testing
description: >
  Creates and runs Grafana k6 load testing scripts for traffic simulation, performance testing,
  and stress testing. Use this skill whenever the user wants to load test an API, simulate traffic
  against a service, write a k6 script, do performance testing, stress testing, spike testing,
  soak testing, smoke testing, breakpoint testing, or any kind of traffic simulation. Also use
  when the user mentions k6, VUs (virtual users), ramping load, arrival rate, or wants to
  benchmark an endpoint's response time under load. Even if the user just says "test my API
  under load" or "how many requests can my server handle" — this skill is the right choice.
---

# Grafana k6 Load Testing

You are an expert at writing and running Grafana k6 load testing scripts. k6 is an open-source,
developer-friendly load testing tool that uses JavaScript for scripting and runs from the CLI
via `k6 run script.js`.

## Quick start

Before writing any script, confirm:
1. **What URL(s) / endpoint(s)** to test
2. **What kind of test** the user wants (smoke, average-load, stress, spike, soak, breakpoint)
3. **Any specific thresholds** (e.g., "p95 < 500ms", "error rate < 1%")

If the user is vague, default to a **smoke test** first (3 VUs, 1 minute) so they can validate the script works, then suggest scaling up.

## k6 Script Anatomy

Every k6 script follows this lifecycle:

```javascript
// 1. Init — runs once per VU, before the test
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  // test configuration goes here
};

// 2. (Optional) Setup — runs once before all VUs
export function setup() {
  // e.g., authenticate and return a token
}

// 3. VU code — runs repeatedly for each VU
export default function (data) {
  // the actual requests to test
}

// 4. (Optional) Teardown — runs once after all VUs finish
export function teardown(data) {
  // cleanup
}
```

Key rules:
- `import` statements and file reads (`open()`) work **only** in init context
- HTTP requests work in VU code, `setup()`, and `teardown()` — but **not** in init context
- Data returned from `setup()` is passed as an argument to `default` and `teardown`

## Test Types and Load Profiles

Choose the right test type based on what the user wants to learn. Read `references/test-types.md`
for full details on each type with ready-to-use templates.

| Type | Purpose | VUs | Duration |
|------|---------|-----|----------|
| **Smoke** | Validate script works, baseline metrics | 1–5 | 30s–3m |
| **Average-load** | Normal day traffic | Production average | 5–60m |
| **Stress** | Above-average load | 1.5–2× average | 5–60m |
| **Spike** | Sudden massive traffic | Very high | 1–5m |
| **Soak** | Extended reliability | Average | Hours |
| **Breakpoint** | Find system limits | Ramps until break | Until failure |

## Executors (How to Model Load)

k6 has two models for generating load:

### Closed model (VU-based) — use when simulating users
- **`constant-vus`** — fixed VU count for a duration
- **`ramping-vus`** — ramp VUs up/down through stages
- **`shared-iterations`** — fixed total iterations split across VUs
- **`per-vu-iterations`** — each VU runs N iterations

### Open model (arrival-rate) — use when simulating requests per second
- **`constant-arrival-rate`** — fixed iteration rate (e.g., 50 req/s)
- **`ramping-arrival-rate`** — ramp iteration rate through stages

**When to use which:** If the user talks about "concurrent users", use VU-based. If they talk
about "requests per second" or "throughput", use arrival-rate. For breakpoint tests, prefer
`ramping-arrival-rate` because it keeps increasing load regardless of response time.

Read `references/executors.md` for configuration details and examples.

## Writing HTTP Requests

```javascript
import http from 'k6/http';

// GET
const res = http.get('https://api.example.com/users');

// POST with JSON
const payload = JSON.stringify({ name: 'test', email: 'test@example.com' });
const params = { headers: { 'Content-Type': 'application/json' } };
const res = http.post('https://api.example.com/users', payload, params);

// All methods: get, post, put, patch, del, head, options, request
```

### Authentication patterns

```javascript
// Bearer token
const params = {
  headers: { Authorization: `Bearer ${token}` },
};

// Basic auth (in setup, pass token to VU code via return value)
export function setup() {
  const loginRes = http.post('https://api.example.com/login', JSON.stringify({
    username: 'user', password: 'pass'
  }), { headers: { 'Content-Type': 'application/json' } });
  return { token: loginRes.json('token') };
}

export default function (data) {
  const res = http.get('https://api.example.com/protected', {
    headers: { Authorization: `Bearer ${data.token}` },
  });
}
```

### URL grouping (for dynamic paths)

When testing endpoints with dynamic IDs, group them so metrics aren't fragmented:
```javascript
// Option 1: Use http.url tagged template
http.get(http.url`https://api.example.com/users/${userId}`);

// Option 2: Use name tag
http.get(`https://api.example.com/users/${userId}`, {
  tags: { name: 'GetUser' },
});
```

## Checks and Thresholds

**Checks** validate individual responses (don't abort on failure):
```javascript
import { check } from 'k6';

check(res, {
  'status is 200': (r) => r.status === 200,
  'response time < 500ms': (r) => r.timings.duration < 500,
  'body contains expected data': (r) => r.body.includes('success'),
});
```

**Thresholds** define pass/fail criteria for the entire test:
```javascript
export const options = {
  thresholds: {
    http_req_failed: ['rate<0.01'],           // <1% errors
    http_req_duration: ['p(95)<500'],          // 95th percentile < 500ms
    http_req_duration: ['p(95)<500', 'p(99)<1000'], // multiple on same metric
    'http_req_duration{name:GetUser}': ['p(95)<300'], // per-endpoint
    checks: ['rate>0.95'],                     // 95% of checks must pass
  },
};
```

Threshold expressions use: `avg`, `min`, `max`, `med`, `p(N)` for Trends; `rate` for Rates; `count`/`rate` for Counters; `value` for Gauges.

## Key Built-in Metrics

| Metric | Type | What it measures |
|--------|------|------------------|
| `http_reqs` | Counter | Total HTTP requests |
| `http_req_duration` | Trend | Total request time (send + wait + receive) |
| `http_req_failed` | Rate | Ratio of failed requests |
| `http_req_waiting` | Trend | Time to first byte (TTFB) |
| `iterations` | Counter | Completed VU iterations |
| `iteration_duration` | Trend | Time per full iteration |
| `checks` | Rate | Ratio of successful checks |
| `data_received` / `data_sent` | Counter | Network data volume |

### Custom metrics
```javascript
import { Trend, Counter, Rate, Gauge } from 'k6/metrics';

const apiDuration = new Trend('api_duration');
const errorCount = new Counter('errors');
const successRate = new Rate('success_rate');

export default function () {
  const res = http.get('https://api.example.com/data');
  apiDuration.add(res.timings.duration);
  errorCount.add(res.status !== 200 ? 1 : 0);
  successRate.add(res.status === 200);
}
```

## Scenarios (Multiple Workloads)

Scenarios let you define multiple independent workloads in a single script:

```javascript
export const options = {
  scenarios: {
    browse: {
      executor: 'constant-vus',
      vus: 10,
      duration: '5m',
      exec: 'browseProducts',
    },
    purchase: {
      executor: 'ramping-arrival-rate',
      startRate: 1,
      timeUnit: '1s',
      preAllocatedVUs: 20,
      stages: [
        { duration: '2m', target: 10 },
        { duration: '3m', target: 10 },
        { duration: '1m', target: 0 },
      ],
      exec: 'makePurchase',
    },
  },
};

export function browseProducts() { /* ... */ }
export function makePurchase() { /* ... */ }
```

## Environment Variables

Make scripts reusable across environments:
```javascript
const BASE_URL = __ENV.BASE_URL || 'https://api.staging.example.com';

export default function () {
  http.get(`${BASE_URL}/users`);
}
```

Run with: `k6 run -e BASE_URL=https://api.prod.example.com script.js`

## Groups and Tags

Use groups to organize requests into logical transactions:
```javascript
import { group } from 'k6';

export default function () {
  group('user_login', function () {
    http.post(/*...*/);
  });
  group('browse_products', function () {
    http.get(/*...*/);
    http.get(/*...*/);
  });
}
```

Use tags to filter metrics:
```javascript
http.get(url, { tags: { type: 'API' } });

// Then set thresholds on tagged sub-metrics
export const options = {
  thresholds: {
    'http_req_duration{type:API}': ['p(95)<300'],
  },
};
```

## Other Protocols

k6 supports more than HTTP:
- **WebSockets**: `import { WebSocket } from 'k6/websockets';` — for real-time connections
- **gRPC**: `import grpc from 'k6/net/grpc';` — for gRPC services
- Read `references/protocols.md` for WebSocket and gRPC examples

## Running k6

```bash
# Basic run
k6 run script.js

# With options
k6 run --vus 10 --duration 30s script.js

# With environment variables
k6 run -e BASE_URL=https://api.example.com script.js

# Output to JSON
k6 run --out json=results.json script.js

# Output to CSV
k6 run --out csv=results.csv script.js
```

## Best Practices

1. **Always start with a smoke test** — validate the script before scaling up
2. **Use `sleep()` between requests** in VU-based executors to simulate realistic think time.
   Don't use sleep in arrival-rate executors (they already pace iterations).
3. **Set `discardResponseBodies: true`** if you don't need response bodies — reduces memory
4. **Use `check()` + thresholds together** — checks alone don't fail the test
5. **Group dynamic URLs** with `http.url` or name tags to prevent metric explosion
6. **Use `setup()` for auth** — authenticate once, share the token across VUs
7. **Use `SharedArray`** for large test data — shares memory across VUs:
   ```javascript
   import { SharedArray } from 'k6/data';
   const users = new SharedArray('users', function () {
     return JSON.parse(open('./users.json'));
   });
   ```

## Reference Documentation

For detailed information, consult these reference files:
- `references/test-types.md` — Complete templates for each test type
- `references/executors.md` — All executor configurations with examples
- `references/protocols.md` — WebSocket and gRPC usage
- `references/options-reference.md` — Full list of k6 options

Source documentation: https://grafana.com/docs/k6/latest/
