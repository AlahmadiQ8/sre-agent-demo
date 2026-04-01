# Executors Reference

Executors control how k6 schedules VUs and iterations. Choose based on what you're modeling.

Source: https://grafana.com/docs/k6/latest/using-k6/scenarios/executors/

## Table of Contents
- [shared-iterations](#shared-iterations)
- [per-vu-iterations](#per-vu-iterations)
- [constant-vus](#constant-vus)
- [ramping-vus](#ramping-vus)
- [constant-arrival-rate](#constant-arrival-rate)
- [ramping-arrival-rate](#ramping-arrival-rate)
- [externally-controlled](#externally-controlled)
- [Choosing the Right Executor](#choosing-the-right-executor)

---

## shared-iterations

A fixed total number of iterations shared among VUs. Fastest VUs pick up more work.

```javascript
export const options = {
  scenarios: {
    my_test: {
      executor: 'shared-iterations',
      vus: 10,
      iterations: 200,
      maxDuration: '10m',
    },
  },
};
```

**Options:** `vus` (default: 1), `iterations` (required), `maxDuration` (default: 10m)
**Use when:** You want exactly N total iterations, and don't care which VU runs which.

---

## per-vu-iterations

Each VU runs a fixed number of iterations independently.

```javascript
export const options = {
  scenarios: {
    my_test: {
      executor: 'per-vu-iterations',
      vus: 10,
      iterations: 20,      // each VU runs 20 iterations = 200 total
      maxDuration: '10m',
    },
  },
};
```

**Options:** `vus` (default: 1), `iterations` (default: 1), `maxDuration` (default: 10m)
**Use when:** Each VU should complete the same amount of work (e.g., each VU = one user journey).

---

## constant-vus

A fixed number of VUs runs for a fixed duration. This is what `vus` + `duration` in options uses.

```javascript
export const options = {
  scenarios: {
    my_test: {
      executor: 'constant-vus',
      vus: 50,
      duration: '5m',
    },
  },
};
```

**Options:** `vus` (default: 1), `duration` (required)
**Use when:** You want steady concurrent load for a period (simplest time-based test).

---

## ramping-vus

VU count changes over time through stages. This is what `stages` in options uses.

```javascript
export const options = {
  scenarios: {
    my_test: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '5m', target: 100 },   // ramp up
        { duration: '10m', target: 100 },   // hold
        { duration: '5m', target: 0 },      // ramp down
      ],
      gracefulRampDown: '30s',
    },
  },
};
```

**Options:** `startVUs` (default: 1), `stages` (required), `gracefulRampDown` (default: 30s)
**Use when:** Average-load, stress, spike, or soak tests where you model concurrent users.

**Tip:** Use `getCurrentStageIndex()` from `https://jslib.k6.io/k6-utils/1.3.0/index.js`
to tag metrics by stage.

---

## constant-arrival-rate

Starts iterations at a fixed rate, regardless of response time. Open model.

```javascript
export const options = {
  scenarios: {
    my_test: {
      executor: 'constant-arrival-rate',
      duration: '5m',
      rate: 30,                // 30 iterations per timeUnit
      timeUnit: '1s',          // = 30 iterations/second
      preAllocatedVUs: 10,
      maxVUs: 100,
    },
  },
};
```

**Options:** `duration` (required), `rate` (required), `timeUnit` (default: '1s'),
`preAllocatedVUs` (required), `maxVUs` (default: same as preAllocatedVUs)

**Use when:** You want a fixed request rate (e.g., "30 RPS") regardless of how fast the
server responds. k6 automatically adjusts VU count to maintain the rate.

**Important:** Don't add `sleep()` at the end of iterations — the executor handles pacing.
Set `preAllocatedVUs` high enough to avoid dropped iterations during startup.

---

## ramping-arrival-rate

Iteration rate changes over time through stages. Open model.

```javascript
export const options = {
  scenarios: {
    my_test: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 500,
      stages: [
        { duration: '2m', target: 50 },     // hold at 50 iter/s
        { duration: '5m', target: 200 },     // ramp to 200 iter/s
        { duration: '5m', target: 200 },     // hold at 200 iter/s
        { duration: '2m', target: 0 },       // ramp down
      ],
    },
  },
};
```

**Options:** `startRate` (default: 0), `timeUnit` (default: '1s'), `stages` (required),
`preAllocatedVUs` (required), `maxVUs` (default: same as preAllocatedVUs)

**Use when:** You want to ramp request rate up/down over time. Ideal for breakpoint tests
and scenarios where you care about throughput rather than concurrent users.

**Important:** Don't add `sleep()` — the executor handles pacing.

---

## externally-controlled

VU count is controlled at runtime via k6's REST API or CLI.

```javascript
export const options = {
  scenarios: {
    my_test: {
      executor: 'externally-controlled',
      vus: 10,
      maxVUs: 100,
      duration: '1h',
    },
  },
};
```

Then control via CLI or REST API:
```bash
# Scale up VUs
k6 scale --vus 50

# Or via REST API
curl -X PATCH http://localhost:6565/v1/status -H 'Content-Type: application/json' \
  -d '{"data":{"attributes":{"vus":50}}}'
```

**Use when:** You want manual control during the test (e.g., interactive debugging, demos).

---

## Choosing the Right Executor

| Question | Recommendation |
|----------|---------------|
| "Run this N times total" | `shared-iterations` |
| "Each user does N things" | `per-vu-iterations` |
| "50 concurrent users for 5 min" | `constant-vus` |
| "Ramp from 0 to 100 users" | `ramping-vus` |
| "Maintain 30 req/s" | `constant-arrival-rate` |
| "Ramp from 10 to 200 req/s" | `ramping-arrival-rate` |
| "I'll control it manually" | `externally-controlled` |

### Common scenario options (apply to all executors)

| Option | Type | Description | Default |
|--------|------|-------------|---------|
| `executor` | string | Executor name (required) | — |
| `startTime` | string | Delay before scenario starts | `"0s"` |
| `gracefulStop` | string | Time to wait for iterations to finish | `"30s"` |
| `exec` | string | JS function to execute | `"default"` |
| `env` | object | Scenario-specific env vars | `{}` |
| `tags` | object | Scenario-specific tags | `{}` |

Source: https://grafana.com/docs/k6/latest/using-k6/scenarios/executors/
