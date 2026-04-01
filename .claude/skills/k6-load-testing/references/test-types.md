# Test Types Reference

Complete templates for each k6 test type. Copy and adapt these for your use case.

Source: https://grafana.com/docs/k6/latest/testing-guides/test-types/

## Table of Contents
- [Smoke Test](#smoke-test)
- [Average-Load Test](#average-load-test)
- [Stress Test](#stress-test)
- [Spike Test](#spike-test)
- [Soak Test](#soak-test)
- [Breakpoint Test](#breakpoint-test)

---

## Smoke Test

**Purpose:** Validate the script works and gather baseline metrics under minimal load.
**When:** Every time a script is created or updated, or when application code changes.

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 3,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

export default function () {
  const res = http.get('https://your-api.example.com/health');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(1);
}
```

**Key points:**
- Keep VUs between 1–5
- Duration: 30 seconds to 3 minutes
- Fix any errors before running larger tests
- Use results as baseline for comparison

Source: https://grafana.com/docs/k6/latest/testing-guides/test-types/smoke-testing/

---

## Average-Load Test

**Purpose:** Assess performance under typical production traffic.
**When:** Regularly, to ensure the system handles normal load after changes.

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '5m', target: 100 },   // ramp up to 100 users over 5 minutes
    { duration: '30m', target: 100 },   // hold at 100 users for 30 minutes
    { duration: '5m', target: 0 },      // ramp down to 0
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

export default function () {
  const res = http.get('https://your-api.example.com/');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(1);
}
```

**Key points:**
- Set VU count to match typical production concurrency
- Ramp-up should be 5–15% of total duration
- Hold period should be at least 5× longer than ramp-up
- Include a ramp-down period (same length as ramp-up or shorter)
- Look for performance degradation during ramp-up and stability during the hold

Source: https://grafana.com/docs/k6/latest/testing-guides/test-types/load-testing/

---

## Stress Test

**Purpose:** Assess performance under above-average load.
**When:** To verify system stability under heavy use (rush hours, deadlines, peaks).

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '10m', target: 200 },   // ramp up to 200 users (above average)
    { duration: '30m', target: 200 },    // hold at 200 for 30 minutes
    { duration: '5m', target: 0 },       // ramp down
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<1000'],
  },
};

export default function () {
  const res = http.get('https://your-api.example.com/');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(1);
}
```

**Key points:**
- Load should be 1.5–2× (or more) of average production traffic
- Only run after successful average-load tests
- Expect some performance degradation — the question is how much
- Longer ramp-up allows gradual observation of degradation
- Thresholds can be more lenient than average-load tests

Source: https://grafana.com/docs/k6/latest/testing-guides/test-types/stress-testing/

---

## Spike Test

**Purpose:** Verify the system survives sudden, massive traffic surges.
**When:** Preparing for events like product launches, sales, or viral traffic.

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '2m', target: 2000 },   // fast ramp-up to extreme load
    { duration: '1m', target: 0 },       // quick ramp-down
  ],
  thresholds: {
    http_req_failed: ['rate<0.10'],       // more lenient error threshold
    http_req_duration: ['p(95)<2000'],
  },
};

export default function () {
  const res = http.get('https://your-api.example.com/');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(1);
}
```

**Key points:**
- Very fast ramp-up, little or no plateau
- Focus on the critical user paths that get hit during the event
- Errors are common and expected — the test measures survival and recovery
- Monitor recovery time after the spike subsides
- Run, tune, repeat until the system handles the spike acceptably

Source: https://grafana.com/docs/k6/latest/testing-guides/test-types/spike-testing/

---

## Soak Test

**Purpose:** Assess reliability and stability over extended periods.
**When:** To find memory leaks, resource exhaustion, and degradation over time.

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '5m', target: 100 },    // ramp up
    { duration: '8h', target: 100 },     // hold at average load for 8 hours
    { duration: '5m', target: 0 },       // ramp down
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

export default function () {
  const res = http.get('https://your-api.example.com/');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(1);
}
```

**Key points:**
- Same load as average-load test, but much longer (3h, 8h, 24h, 72h)
- Only run after smoke and average-load tests pass
- Monitor backend resources (RAM, CPU, disk, connections) throughout
- Look for gradual degradation trends, not just final values
- Common issues found: memory leaks, connection pool exhaustion, log storage filling up

Source: https://grafana.com/docs/k6/latest/testing-guides/test-types/soak-testing/

---

## Breakpoint Test

**Purpose:** Find the system's maximum capacity — where it breaks.
**When:** To determine upper limits after the system passes other test types.

```javascript
import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
  scenarios: {
    breakpoint: {
      executor: 'ramping-arrival-rate',
      preAllocatedVUs: 50,
      maxVUs: 2000,
      stages: [
        { duration: '2h', target: 20000 },  // slowly ramp to extreme load
      ],
    },
  },
  thresholds: {
    http_req_failed: [{ threshold: 'rate<0.05', abortOnFail: true, delayAbortEval: '30s' }],
    http_req_duration: [{ threshold: 'p(95)<5000', abortOnFail: true, delayAbortEval: '30s' }],
  },
};

export default function () {
  http.get('https://your-api.example.com/');
  // No sleep needed — arrival-rate executor handles pacing
}
```

**Key points:**
- Use `ramping-arrival-rate` so load keeps increasing even if the system slows down
- Use `abortOnFail: true` on thresholds to auto-stop when limits are found
- Disable autoscaling in cloud environments to find actual capacity limits
- Can be manually stopped with Ctrl+C when you see the system failing
- Repeat after tuning to measure improvement

Source: https://grafana.com/docs/k6/latest/testing-guides/test-types/breakpoint-testing/
