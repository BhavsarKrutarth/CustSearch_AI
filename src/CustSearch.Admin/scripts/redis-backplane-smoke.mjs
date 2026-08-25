import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

// This probe proves that an alert created on API node B reaches a SignalR client
// connected to node A through the configured Redis backplane.
const token = process.env.CUSTSEARCH_SIGNALR_TOKEN;
const nodeA = process.env.CUSTSEARCH_SIGNALR_NODE_A;
const nodeB = process.env.CUSTSEARCH_SIGNALR_NODE_B;
if (!token || !nodeA || !nodeB) {
  throw new Error(
    'CUSTSEARCH_SIGNALR_TOKEN, CUSTSEARCH_SIGNALR_NODE_A and CUSTSEARCH_SIGNALR_NODE_B are required.',
  );
}

const deduplicationKey = `redis-backplane:${Date.now()}:${crypto.randomUUID()}`;
let readyResolve;
let eventResolve;
const ready = new Promise((resolve) => { readyResolve = resolve; });
const delivered = new Promise((resolve) => { eventResolve = resolve; });
const connection = new HubConnectionBuilder()
  .withUrl(`${nodeA}/hubs/alerts`, { accessTokenFactory: () => token })
  .configureLogging(LogLevel.Warning)
  .build();

connection.on('RealtimeReady', (message) => readyResolve(message));
connection.on('AlertEvent', (message) => {
  if (message?.alert?.deduplicationKey === deduplicationKey) eventResolve(message);
});

const timeout = (label, milliseconds) => new Promise((_, reject) =>
  setTimeout(() => reject(new Error(`${label} timed out after ${milliseconds}ms.`)), milliseconds));

try {
  await connection.start();
  const readyMessage = await Promise.race([ready, timeout('RealtimeReady', 10_000)]);
  const response = await fetch(`${nodeB}/api/tenant/alerts`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({
      alertType: 'RedisBackplaneSmoke',
      storeId: null,
      severity: 1,
      title: 'Redis multi-node smoke',
      message: 'Created on node B and observed by a client connected to node A.',
      entityType: 'ValidationRun',
      entityId: deduplicationKey,
      deduplicationKey,
    }),
  });
  if (!response.ok) throw new Error(`Node B alert creation failed: ${response.status} ${await response.text()}`);
  const event = await Promise.race([delivered, timeout('cross-node AlertEvent', 20_000)]);
  if (event.contractVersion !== 1 || event.tenantId <= 0 || event.eventId <= 0) {
    throw new Error('Cross-node event contract was invalid.');
  }
  await connection.invoke('ReportReconnect', event.eventId);
  console.log(JSON.stringify({
    result: 'PASS',
    nodeA,
    nodeB,
    connectionId: readyMessage.connectionId,
    eventId: event.eventId,
    tenantId: event.tenantId,
    redisBackplaneRequired: true,
  }));
} finally {
  await connection.stop();
}
