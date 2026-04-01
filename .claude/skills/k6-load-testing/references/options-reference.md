# k6 Options Reference

Complete reference for k6 options. Options can be set in the script, CLI flags, environment
variables, or a config file. Precedence (highest to lowest): CLI flags > env vars > script > config file.

Source: https://grafana.com/docs/k6/latest/using-k6/k6-options/reference/

## Table of Contents
- [Load Configuration](#load-configuration)
- [Network and Connection](#network-and-connection)
- [Output and Logging](#output-and-logging)
- [Execution Control](#execution-control)
- [TLS and Security](#tls-and-security)
- [Commonly Used Option Combinations](#commonly-used-option-combinations)

---

## Load Configuration

### vus
Number of virtual users to run concurrently.
- **CLI:** `--vus`, `-u`  |  **Env:** `K6_VUS`  |  **Default:** `1`

### duration
Total test duration (e.g., `'30s'`, `'5m'`, `'1h'`).
- **CLI:** `--duration`, `-d`  |  **Env:** `K6_DURATION`  |  **Default:** `null`

### iterations
Total number of script iterations across all VUs.
- **CLI:** `--iterations`, `-i`  |  **Env:** `K6_ITERATIONS`  |  **Default:** `null`

### stages
Array of objects defining VU ramp-up/down stages. Shortcut for `ramping-vus` executor.
```javascript
stages: [
  { duration: '5m', target: 100 },
  { duration: '10m', target: 100 },
  { duration: '5m', target: 0 },
]
```
- **CLI:** `--stage`, `-s` (e.g., `-s 5m:100 -s 10m:100 -s 5m:0`)  |  **Env:** `K6_STAGES`

### scenarios
Advanced execution scenarios with multiple executors. See `executors.md` for full details.
```javascript
scenarios: {
  scenario_name: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [{ duration: '5m', target: 100 }],
    exec: 'myFunction',     // function to execute (default: 'default')
    startTime: '10s',        // delay before start
    gracefulStop: '30s',
    env: { KEY: 'value' },
    tags: { name: 'value' },
  },
}
```

### rps (discouraged)
Global max requests per second. **Use arrival-rate executors instead.**
- **CLI:** `--rps`  |  **Env:** `K6_RPS`  |  **Default:** `0` (unlimited)

---

## Network and Connection

### batch
Max simultaneous connections per `http.batch()` call.
- **Default:** `20`

### batchPerHost
Max simultaneous connections per host in `http.batch()`.
- **Default:** `6`

### dns
DNS resolution configuration.
```javascript
dns: {
  ttl: '5m',          // Cache duration ('0' = no cache, 'inf' = forever)
  select: 'random',   // 'first', 'random', 'roundRobin'
  policy: 'preferIPv4', // 'preferIPv4', 'preferIPv6', 'onlyIPv4', 'onlyIPv6', 'any'
}
```

### hosts
DNS overrides (like /etc/hosts):
```javascript
hosts: {
  'test.k6.io': '1.2.3.4',
  'test.k6.io:443': '1.2.3.4:8443',
  '*.example.com': '1.2.3.4',
}
```

### noConnectionReuse
Disable keep-alive connections. Forces new TCP connection per request.
- **Default:** `false`

### noVUConnectionReuse
Disable TCP connection reuse across VU iterations.
- **Default:** `false`

### maxRedirects
Maximum HTTP redirects to follow.
- **Default:** `10`

### blacklistIPs
Block requests to specific IP ranges:
```javascript
blacklistIPs: ['10.0.0.0/8', '192.168.0.0/16']
```

### blockHostnames
Block requests to specific hostnames (supports `*` wildcard):
```javascript
blockHostnames: ['*.internal.example.com']
```

### localIPs
Source IP addresses for outgoing requests:
```javascript
localIPs: ['192.168.1.10-192.168.1.20']
```

### userAgent
HTTP User-Agent header string.
- **Default:** `"k6/0.x.x (https://k6.io/)"`

---

## Output and Logging

### discardResponseBodies
Drop response bodies to reduce memory usage. **Recommended for large tests.**
```javascript
discardResponseBodies: true
```
Override per-request with `responseType: 'text'` or `'binary'` in params.
- **Default:** `false`

### httpDebug
Log all HTTP requests and responses.
```javascript
httpDebug: 'full'  // or just true for headers only
```

### noSummary
Disable end-of-test summary output.
- **Default:** `false`

### summaryTrendStats
Statistics to show for Trend metrics in the summary:
```javascript
summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)']
```

### summaryTimeUnit
Time unit for summary values: `'s'`, `'ms'`, or `'us'`.

### summaryMode
Summary detail level: `'compact'` (default), `'full'`, or `'legacy'`.

### logOutput
Where to send k6 logs:
```bash
k6 run --log-output=file=./k6.log script.js
```

### consoleOutput
Redirect console.log output to a file:
```bash
k6 run --console-output=loadtest.log script.js
```

---

## Execution Control

### thresholds
Pass/fail criteria for the test. See the main SKILL.md for syntax.
```javascript
thresholds: {
  http_req_failed: ['rate<0.01'],
  http_req_duration: ['p(95)<500'],
}
```

### setupTimeout / teardownTimeout
Max time allowed for setup/teardown functions.
- **Default:** `10s`

### noSetup / noTeardown
Skip setup or teardown execution.
```bash
k6 run --no-setup --no-teardown script.js
```

### noCookiesReset
Keep cookies across VU iterations.
- **Default:** `false`

### throw
Throw exceptions on failed HTTP requests (vs. returning error responses).
- **Default:** `false`

### tags
Test-wide tags applied to all metrics:
```javascript
tags: { env: 'staging', team: 'backend' }
```
Or via CLI: `k6 run --tag env=staging script.js`

### systemTags
Control which system tags are collected:
```javascript
systemTags: ['method', 'status', 'url', 'name', 'group', 'check', 'error', 'scenario']
```

### minIterationDuration
Minimum time per iteration (pads with sleep if iteration finishes early):
```javascript
minIterationDuration: '500ms'
```

### paused
Start the test in a paused state (resume via CLI or REST API).
- **Default:** `false`

---

## TLS and Security

### insecureSkipTLSVerify
Skip TLS certificate verification.
```javascript
insecureSkipTLSVerify: true
```

### tlsAuth
Client certificate authentication:
```javascript
tlsAuth: [
  {
    domains: ['api.example.com'],
    cert: open('client-cert.pem'),
    key: open('client-key.pem'),
  },
]
```

### tlsCipherSuites
Restrict allowed TLS cipher suites:
```javascript
tlsCipherSuites: ['TLS_RSA_WITH_AES_128_GCM_SHA256']
```

### tlsVersion
Restrict TLS version:
```javascript
tlsVersion: { min: 'tls1.2', max: 'tls1.3' }
// or just: tlsVersion: 'tls1.2'
```

---

## Commonly Used Option Combinations

### Simple quick test
```javascript
export const options = {
  vus: 10,
  duration: '30s',
};
```

### Average-load test with thresholds
```javascript
export const options = {
  stages: [
    { duration: '5m', target: 50 },
    { duration: '20m', target: 50 },
    { duration: '5m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
  },
};
```

### High-performance test (minimize k6 overhead)
```javascript
export const options = {
  discardResponseBodies: true,
  noConnectionReuse: false,
  scenarios: {
    load: {
      executor: 'constant-arrival-rate',
      rate: 1000,
      timeUnit: '1s',
      duration: '10m',
      preAllocatedVUs: 200,
      maxVUs: 500,
    },
  },
};
```

### CI/CD integration
```javascript
export const options = {
  stages: [
    { duration: '1m', target: 20 },
    { duration: '3m', target: 20 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
    checks: ['rate>0.95'],
  },
};
```
k6 exits with code 0 on success, non-zero if any threshold fails — perfect for CI pipelines.

Source: https://grafana.com/docs/k6/latest/using-k6/k6-options/reference/
