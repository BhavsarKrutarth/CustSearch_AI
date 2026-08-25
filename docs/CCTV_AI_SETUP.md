# CCTV and AI Setup

Use Demo Mode first. Set an API key and start FastAPI as shown in the root README. Configure the
.NET CCTV service identity/secret and camera RTSP reference through environment/secret storage.

The Python service normalizes anonymous detector metadata; .NET validates service credentials,
clock/body limits, idempotency and camera/store ownership. Recognition is a separate consent-gated
workflow. Never send SQL credentials to Python or store raw RTSP credentials in camera rows.

Real camera rollout requires zone configuration, lighting/occlusion testing, time synchronization,
retention/privacy approval, model provenance and store-specific calibration. Phase 18 item pickup/
put-back/security observations are not implemented in this source.
