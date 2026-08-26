# Camera Connection and RTSP Guide

Last verified: 2026-08-26

## 1. Intended connection

```text
Powered IP camera
  -> Ethernet switch/router or laptop LAN adapter
  -> camera HTTPS/RTSP endpoint
  -> Python capture/detection process
  -> signed anonymous metadata
  -> ASP.NET `/api/internal/cctv/events`
  -> SQL Server
  -> Angular `/customer-admin/cameras`
```

The Angular camera form registers camera metadata. Angular does not display or process the RTSP
stream, and Python never receives SQL credentials.

## 2. Observed camera on this workstation

Read-only LAN discovery found this strong Hikvision candidate:

| Setting | Observed value |
|---|---|
| Camera IP | Runtime/admin supplied; never hard-coded in application source |
| MAC address | `08-54-11-1E-5C-F0` |
| HTTPS | Port `443` open, HTTP redirects to HTTPS |
| RTSP | Port `554` open; Digest authentication required |
| Hikvision SDK | Port `8000` open |
| Browser URL | `https://<camera-ip>` |

Confirm that the MAC printed on the physical camera/box matches before changing its settings. The LAN
also contains multiple other RTSP devices; do not assume an IP solely from its open port.

## 3. Physical/network checklist

1. Connect camera power/PoE correctly.
2. Connect camera and laptop to the same authorized LAN/switch.
3. Confirm Ethernet link is `Up`:

```powershell
Get-NetAdapter | Select-Object Name,Status,LinkSpeed,MacAddress
Get-NetIPConfiguration
```

4. Confirm camera reachability:

```powershell
Test-NetConnection <camera-ip> -Port 443
Test-NetConnection <camera-ip> -Port 554
Test-NetConnection <camera-ip> -Port 8000
```

5. Open `https://<camera-ip>` and sign in with the camera's authorized administrator credentials.
6. A self-signed certificate warning is common locally; verify the IP/device before proceeding.
7. Change any factory-default password, set correct time/NTP and create a least-privilege stream user.

Do not send the camera password in chat and do not commit it to this repository.

## 4. Typical Hikvision RTSP paths

Main stream:

```text
rtsp://<camera-ip>:554/Streaming/Channels/101
```

Sub-stream:

```text
rtsp://<camera-ip>:554/Streaming/Channels/102
```

Use the sub-stream first for development because it requires less CPU/network bandwidth. Prefer a
client that prompts for credentials. Embedding `username:password@` in a command can expose the secret
in shell history and process lists.

VLC check:

```text
Media -> Open Network Stream -> paste RTSP URL -> enter credentials when prompted
```

If FFmpeg/ffprobe is installed and a credential-safe local method is available:

```powershell
ffprobe -rtsp_transport tcp -v error "rtsp://<camera-ip>:554/Streaming/Channels/102"
```

An RTSP `401 Unauthorized` response proves the service is reachable but not that stream credentials
are correct. The observed camera currently returns this expected authentication challenge.

## 5. Register camera in Angular Admin

1. Login as Tenant Admin/Shop Owner with `Cameras.View` and `Cameras.Manage`.
2. Open `http://localhost:4200/customer-admin/cameras`.
3. Select the authorized Store ID.
4. Use a stable camera code such as `ENTRY-01`.
5. Enter name/location/direction.
6. In **RTSP configuration reference**, enter only an opaque reference, for example:

```text
env:CUSTSEARCH_CAMERA_ENTRY01_RTSP
```

7. Do not enter `rtsp://user:password@...` into this database field.
8. Save the camera and create normalized zone polygons as required.

The API stores the opaque reference and returns only a masked hint. Actual credentials belong in an
environment variable or approved secret vault resolved by the Python host at runtime.

Set a source dynamically on the Python server (example variable name only):

```powershell
$env:CUSTSEARCH_CAMERA_ENTRY01_RTSP = "rtsp://<authorized-user>:<password>@<camera-ip>:554/<stream-path>"
$env:CUSTSEARCH_AI_API_KEY = "<local-service-api-key>"
```

Restart Python after changing environment variables, then probe one frame without returning the URL
or image:

```powershell
$headers = @{ "X-CustSearch-AI-Key" = $env:CUSTSEARCH_AI_API_KEY }
$body = @{ configuration_reference = "env:CUSTSEARCH_CAMERA_ENTRY01_RTSP"; timeout_seconds = 5 } |
  ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:8000/v1/cctv/cameras/probe" `
  -Headers $headers -ContentType "application/json" -Body $body
```

Every camera may use a different allow-listed `CUSTSEARCH_CAMERA_*` environment variable. Nothing in
the application assumes a specific IP, vendor, camera ID, tenant or store.

## 6. Configure the .NET CCTV ingestion identity

The API accepts Python events only after HMAC validation. Before starting the API, use one local
service ID and one strong secret. Example names below are placeholders:

```powershell
$env:CctvServices__laptop_camera__Secret = "<local-random-secret>"
$env:CctvServices__laptop_camera__TenantId = "<tenant-database-id>"
$env:CctvServices__laptop_camera__StoreIds = "<authorized-store-id>"
```

Restart the API after setting them. Do not set `AllowAllStores=true` for a normal camera service.

Python-side matching values:

```powershell
$env:CUSTSEARCH_AI_DOTNET_EVENT_URL = "https://localhost:7277/api/internal/cctv/events"
$env:CUSTSEARCH_AI_SERVICE_ID = "laptop_camera"
$env:CUSTSEARCH_AI_SERVICE_SECRET = "<same-local-random-secret>"
```

The camera's `CameraCode`, event TenantId and StoreId must match an active camera registered in .NET.

## 7. Signed event contract

The future Python publisher must send:

```text
X-CustSearch-Service-Id
X-CustSearch-Timestamp
X-CustSearch-Signature
X-CustSearch-Event-Id
Idempotency-Key
Content-Type: application/json
```

The HMAC-SHA256 canonical bytes are:

```text
serviceId + "\n"
timestamp + "\n"
eventId + "\n"
idempotencyKey + "\n"
raw JSON body bytes
```

The API verifies timestamp skew, signature, idempotency, tenant/store service scope, active camera and
allowed anonymous event schema before writing through .NET to SQL Server.

## 8. Current executable vs pending connection

| Layer | Status |
|---|---|
| Camera network/HTTPS/RTSP reachability | Runtime environment check required per camera |
| Angular camera registration | Implemented |
| .NET HMAC event ingestion | Implemented and tested |
| Python Demo Mode/normalization | Implemented and tested |
| Python authenticated dynamic one-frame RTSP probe | Implemented and tested; physical frame needs authorized runtime secret |
| Python continuous RTSP capture/reconnect | Not implemented |
| Python ONNX inference orchestration | Adapter exists; output pipeline not implemented |
| Python HMAC publisher to .NET | Settings exist; publisher not implemented |

This means the camera can be accessed with authorized credentials, but the current Python process will
not automatically show live camera events in Angular until the RTSP runner and publisher are built.

## 9. Recommended live implementation order

1. Verify sub-stream in VLC without changing camera configuration.
2. Create a least-privilege camera stream account.
3. Put the RTSP URL/credential in local secret storage.
4. Register matching `StoreId + CameraCode` in Angular.
5. Configure matching .NET/Python HMAC service identity.
6. Implement one-camera RTSP runner with reconnect and frame limits.
7. Validate anonymous `person.entered/updated/exited/lost` events.
8. Verify events appear in Camera Operations and remain tenant/store isolated.
9. Add zones and calibrate lighting/occlusion using the sub-stream.
10. Add more cameras only after one-camera soak testing passes.

## 10. Troubleshooting

| Problem | Check |
|---|---|
| Camera page does not open | IP/MAC, subnet, port 443 and browser certificate warning |
| RTSP returns 401 | username/password and user stream permission |
| RTSP times out | port 554, firewall, VLAN, camera gateway and RTSP enabled setting |
| VLC works but Python does not | OpenCV backend, TCP transport, codec and URL secret resolution |
| .NET returns 401 | service ID, timestamp and HMAC secret/signature |
| .NET returns 403 | service TenantId/StoreIds do not authorize event scope |
| .NET returns 404 | matching active `CameraCode` not registered in that store |
| .NET returns 409 | idempotency key reused with different JSON |
| Angular shows no tracks | publisher is not implemented/running or event was rejected |

## 11. Security and privacy checklist

- [ ] Default/factory camera password changed.
- [ ] Camera management UI is not exposed to the public internet.
- [ ] RTSP account has least privilege.
- [ ] Password/RTSP URL is absent from Git, SQL and application logs.
- [ ] Camera clock/NTP is correct.
- [ ] Tracking begins anonymous.
- [ ] Raw frames are not persisted by default.
- [ ] Recognition remains separately consent-gated and human-reviewed.
- [ ] Store scope is server-authorized.
- [ ] Demo Mode is disabled in Production.

Read `PYTHON_AI_PROJECT_GUIDE.md` for Python setup, endpoints, tests and the remaining live-runner work.
