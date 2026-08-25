# SignalR Event Catalog

| Event/facility | Purpose | Recovery/security |
|---|---|---|
| `RealtimeReady` | confirms authenticated hub connection | server assigns tenant/store groups from current identity |
| alert event payload | new/update alert notification | durable `RealtimeEvents` cursor and REST recovery |
| report export progress | queued/processing/completed/failure progress | requester/tenant-bound job lookup and download |

Connections join the authenticated tenant group and only authoritative assigned-store groups.
Clients reconnect, de-duplicate event IDs and reload authoritative state from REST. Planned
`security.incident.*` events are not implemented in the selected source.
