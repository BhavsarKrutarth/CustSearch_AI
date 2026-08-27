# Live Camera Connection Rollout Plan

- Prepared: 2026-08-27 (Asia/Calcutta)
- Tenant: `TEN-35D77F00D7F0`
- Application/AI host LAN IP: `192.168.1.30`
- Current execution boundary: planning and non-physical verification only; no live camera probe or frame test

## Architecture decision

`192.168.1.30` is the CustSearch application/AI host, not an assumed camera address. Browsers connect
to CustSearch over HTTPS. Only the Python AI service resolves and opens private RTSP sources. The
browser, Angular application, API responses, SQL database, Git repository and logs must never contain
camera usernames, passwords or full RTSP URLs.

Each physical camera receives:

1. A static IP or DHCP reservation on the office camera LAN/VLAN.
2. One database `Cameras` row scoped to its tenant and store.
3. One unique opaque reference such as `env:CUSTSEARCH_CAMERA_STORE13_ENTRY01_RTSP`.
4. One matching RTSP secret on the AI host or approved secret manager.
5. Explicit user-camera preview grants; role permission alone is insufficient.

The current preview implementation supports up to 16 simultaneous configured sources per Python
process and uses authenticated server-mediated JPEG frames. For higher frame rate, audio or many
concurrent viewers, retain the same authorization model and place a WebRTC media gateway behind the
API boundary.

## Information required before configuration

- Camera count, store placement and a stable camera code for each device.
- Vendor/model, camera or NVR IP, RTSP port, channel and main/sub-stream path.
- A dedicated read-only camera account for each device or approved credential group.
- Whether cameras are direct RTSP devices or channels exposed through one NVR.
- Which tenant users may view live video, tracking overlays or camera controls.
- Whether `192.168.1.30` will remain static; reserve it in DHCP if it is the permanent host.
- Production HTTPS DNS name/certificate and the LAN clients that may reach the application.

Camera credentials must be entered only in a private administrator session or secret manager. Do not
put them in a ticket, chat, SQL script, screenshot, Git commit or command transcript.

## Multi-camera configuration matrix

Maintain one row per camera. Do not grant access by an IP range.

| Camera | Store | Example private address | Database reference | User assignment |
|---|---|---|---|---|
| Entry 01 | UAT store 13 | supplied later | `env:CUSTSEARCH_CAMERA_STORE13_ENTRY01_RTSP` | explicit grants |
| Exit 01 | UAT store 13 | supplied later | `env:CUSTSEARCH_CAMERA_STORE13_EXIT01_RTSP` | explicit grants |
| Checkout 01 | UAT store 13 | supplied later | `env:CUSTSEARCH_CAMERA_STORE13_CHECKOUT01_RTSP` | explicit grants |

If an NVR exposes multiple channels at one IP, keep separate database rows and secret references per
channel. This preserves per-camera authorization, audit and revoke behavior even when the network host
is shared.

## Deployment sequence

### 1. Network preparation

- Reserve static addresses for `192.168.1.30`, cameras and NVR.
- Prefer a camera VLAN. Permit AI host to camera/NVR RTSP only; deny browser-to-camera access.
- Permit LAN clients to the CustSearch HTTPS endpoint only.
- Synchronize host, camera and NVR time through NTP.

### 2. Application host preparation

- Run Angular behind IIS or another HTTPS reverse proxy on the application host.
- Run ASP.NET API and Python AI as managed services, not interactive terminals.
- Keep API-to-Python traffic on loopback when both run on `192.168.1.30`.
- Configure matching `CUSTSEARCH_AI_API_KEY` and `CctvPreview__ApiKey` secrets.
- Enable `CctvPreview__Enabled=true` only after the secret boundary is ready.

### 3. Camera registration

- Create the store and camera metadata through the tenant-admin camera page or approved SQL seed.
- Store only the opaque `env:CUSTSEARCH_CAMERA_*` reference in SQL.
- Add each matching RTSP URL to the AI-host secret environment/store.
- Never copy the RTSP URL into the Angular camera form after initial secret provisioning.

### 4. User-wise authorization

- Keep `Cameras.View` for metadata visibility and `Cameras.Preview` for session creation.
- Add an explicit `CameraUserPreviewGrants` row for every allowed user-camera pair.
- Grant `CanViewTracking` only when needed and `CanControl` only for approved operators.
- Use `ValidUntilUtc` for temporary users and deactivate/revoke access immediately when duties change.

### 5. Audit and operations

- Preview start/end and grant changes remain in `AuditLogs` with tenant, store, user, IP, user agent and correlation ID.
- Active preview ownership and expiry remain in `CameraPreviewSessions`.
- Review user-wise access and sessions periodically; remove orphaned grants.
- Do not log frames, RTSP URLs, camera credentials or authorization tokens.

## Deferred approved validation

No step below is executed in the current work. When an authorized maintenance window is approved:

1. Confirm HTTPS login and camera metadata without opening a preview.
2. Probe one low-resolution sub-stream from the AI host.
3. Open a preview as one explicitly granted user and verify an ungranted user is denied.
4. Confirm audit rows for start/end and verify no secret appears in API, Python or IIS logs.
5. Run a 30-minute reconnect soak, then add remaining cameras one at a time.
6. Record measured latency, CPU, memory and concurrent viewer limits before production sign-off.

## Rollback

- Disable `CctvPreview__Enabled` to stop new previews without deleting camera metadata.
- Revoke the affected user-camera grants and end active sessions.
- Remove the corresponding AI-host secret reference; do not delete unrelated cameras or tenant data.
- Preserve audit rows for investigation and document the rollback reason.
