# Troubleshooting

| Symptom | Likely check | Recovery |
|---|---|---|
| API exits before host builds | empty connection/signing options | set required environment variables; do not edit committed secrets |
| SQL login/connect error | service/alias/Windows permission/certificate | run the README `sqlcmd -E -C` probe |
| `/health/ready` 503 | SQL, Redis or current worker heartbeat | inspect health and Worker logs; do not bypass readiness |
| Refresh HTTP 500 on SQL Server | source older than retry-strategy fix | use audit branch containing `AuthenticationService` transaction repair |
| Angular 401 loop | expired/revoked cookie or session stamp | clear session, sign in again; inspect auth event audit |
| SignalR misses events | disconnect/group/backplane issue | reconnect then restore from alerts/report REST cursor |
| Export unavailable | job not complete/expired/requester mismatch | inspect job status; re-request rather than sharing a token |
| CCTV 401/409 | service key/signature/time/replay/camera mismatch | verify clock, key reference and camera/store ownership |
| Recognition disabled | consent/key configuration absent | keep disabled until lawful consent and key storage exist |
| Phase 18 objects appear only in SQL | known code/schema drift | inspect `origin/AIMainBranch` commit `055b052`; integrate only after review |
