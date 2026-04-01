# Protocols Reference

k6 supports HTTP/1.1, HTTP/2, WebSockets, and gRPC out of the box. Additional protocols
are available through extensions.

Source: https://grafana.com/docs/k6/latest/using-k6/protocols/

## Table of Contents
- [HTTP](#http)
- [WebSockets](#websockets)
- [gRPC](#grpc)
- [Extensions for Other Protocols](#extensions-for-other-protocols)

---

## HTTP

HTTP/1.1 is the default. k6 automatically upgrades to HTTP/2 if the server supports it.

### Full API reference

```javascript
import http from 'k6/http';

// Methods
http.get(url, [params])
http.post(url, [body], [params])
http.put(url, [body], [params])
http.patch(url, [body], [params])
http.del(url, [body], [params])
http.head(url, [params])
http.options(url, [body], [params])
http.request(method, url, [body], [params])
http.asyncRequest(method, url, [body], [params])  // returns Promise
http.batch(requests)                                // parallel requests
```

### Batch requests (parallel)

```javascript
const responses = http.batch([
  ['GET', 'https://api.example.com/users'],
  ['GET', 'https://api.example.com/products'],
  ['POST', 'https://api.example.com/orders', JSON.stringify({item: 1}), {
    headers: { 'Content-Type': 'application/json' },
  }],
]);
// responses is an array, same order as requests
```

### Response object

```javascript
const res = http.get('https://api.example.com/data');

res.status;              // HTTP status code (number)
res.body;                // Response body (string)
res.json();              // Parse body as JSON
res.json('user.name');   // JSONPath extraction
res.headers;             // Response headers (object)
res.timings.duration;    // Total request time in ms
res.timings.blocked;     // Time waiting for connection slot
res.timings.connecting;  // TCP connection time
res.timings.tls_handshaking; // TLS handshake time
res.timings.sending;     // Time sending data
res.timings.waiting;     // TTFB (time to first byte)
res.timings.receiving;   // Time receiving data
```

### File upload (multipart)

```javascript
import http from 'k6/http';
import { open } from 'k6/init';

const binFile = open('/path/to/file.bin', 'b');

export default function () {
  const data = {
    file: http.file(binFile, 'file.bin', 'application/octet-stream'),
    field: 'value',
  };
  http.post('https://api.example.com/upload', data);
}
```

### Cookie handling

```javascript
// Cookies are automatically managed per-VU
// Access the cookie jar:
const jar = http.cookieJar();
jar.set('https://api.example.com', 'session_id', 'abc123');

// Cookies reset between iterations by default
// To persist cookies across iterations:
export const options = { noCookiesReset: true };
```

Source: https://grafana.com/docs/k6/latest/javascript-api/k6-http/

---

## WebSockets

k6 provides two WebSocket APIs. Prefer `k6/websockets` (newer, standard-compliant).

### k6/websockets (recommended)

```javascript
import { WebSocket } from 'k6/websockets';
import { check } from 'k6';

export default function () {
  const url = 'wss://echo.websocket.org';
  const params = {
    headers: { 'X-Custom-Header': 'value' },
  };

  const ws = new WebSocket(url, null, params);

  ws.addEventListener('open', () => {
    console.log('Connected');
    ws.send('Hello server!');
  });

  ws.addEventListener('message', (e) => {
    console.log(`Received: ${e.data}`);
    ws.close();
  });

  ws.addEventListener('close', () => {
    console.log('Disconnected');
  });

  ws.addEventListener('error', (e) => {
    console.error(`Error: ${e.error}`);
  });

  // ws.ping() — send a ping
}
```

### k6/ws (legacy, blocking)

```javascript
import ws from 'k6/ws';
import { check } from 'k6';

export default function () {
  const url = 'wss://echo.websocket.org';

  const res = ws.connect(url, {}, function (socket) {
    socket.on('open', () => {
      socket.send('Hello!');
    });

    socket.on('message', (data) => {
      console.log(`Received: ${data}`);
      socket.close();
    });

    socket.setTimeout(function () {
      socket.close();
    }, 5000);
  });

  check(res, { 'status is 101': (r) => r && r.status === 101 });
}
```

**Key difference:** `k6/ws` blocks the VU until the connection closes. `k6/websockets` is
non-blocking and follows the browser WebSocket API.

### WebSocket metrics

| Metric | Type | Description |
|--------|------|-------------|
| `ws_connecting` | Trend | Connection handshake time |
| `ws_msgs_received` | Counter | Messages received |
| `ws_msgs_sent` | Counter | Messages sent |
| `ws_ping` | Trend | Ping round-trip time |
| `ws_session_duration` | Trend | Total session duration |
| `ws_sessions` | Counter | Sessions started |

Source: https://grafana.com/docs/k6/latest/javascript-api/k6-websockets/

---

## gRPC

k6 includes a native gRPC client for testing gRPC services.

### Basic usage

```javascript
import grpc from 'k6/net/grpc';
import { check } from 'k6';

const client = new grpc.Client();
client.load(['definitions'], 'hello.proto');

export default function () {
  client.connect('grpc.example.com:443', { plaintext: false });

  const response = client.invoke('hello.HelloService/SayHello', {
    greeting: 'World',
  });

  check(response, {
    'status is OK': (r) => r && r.status === grpc.StatusOK,
  });

  console.log(JSON.stringify(response.message));

  client.close();
}
```

### Async gRPC

```javascript
const response = await client.asyncInvoke('hello.HelloService/SayHello', {
  greeting: 'World',
});
```

### gRPC streaming

```javascript
import grpc from 'k6/net/grpc';

const client = new grpc.Client();
client.load(['definitions'], 'route_guide.proto');

export default function () {
  client.connect('localhost:10000', { plaintext: true });

  const stream = new grpc.Stream(client, 'routeguide.RouteGuide/RouteChat');

  stream.on('data', (msg) => {
    console.log(`Received: ${JSON.stringify(msg)}`);
  });

  stream.on('end', () => {
    console.log('Stream ended');
    client.close();
  });

  stream.on('error', (err) => {
    console.error(`Error: ${err.message}`);
  });

  stream.write({ message: 'Hello' });
  stream.write({ message: 'World' });
  stream.end();
}
```

### gRPC metrics

| Metric | Type | Description |
|--------|------|-------------|
| `grpc_req_duration` | Trend | Response time |
| `grpc_streams` | Counter | Streams started |
| `grpc_streams_msgs_received` | Counter | Messages received |
| `grpc_streams_msgs_sent` | Counter | Messages sent |

Source: https://grafana.com/docs/k6/latest/javascript-api/k6-net-grpc/

---

## Extensions for Other Protocols

k6 can be extended with custom protocols using xk6. Popular extensions include:

- **SQL databases** — `xk6-sql` for PostgreSQL, MySQL, SQLite, MS SQL
- **Kafka** — `xk6-kafka` for Apache Kafka producers/consumers
- **Redis** — `xk6-redis` for Redis commands
- **AMQP** — `xk6-amqp` for RabbitMQ
- **MQTT** — `xk6-mqtt` for IoT protocols

Build a custom k6 binary with extensions:
```bash
xk6 build --with github.com/grafana/xk6-sql@latest
```

Full list of extensions: https://grafana.com/docs/k6/latest/extensions/explore/
