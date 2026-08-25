# Open Issues

## Critical

None confirmed.

## High

None currently confirmed.

## Medium

| Issue | Phase | Affected files | Reason | Required fix | Blocking |
|---|---:|---|---|---|---|
| VIP and high-value report semantics are incomplete | 18 | security settings/reports and future customer classification | `HighValueThreshold` now exists but customer aggregation/manual VIP truth is not implemented | Implement factual spend threshold aggregation and explicit audited VIP designation before exposing these reports | Blocks those Phase 18 reports only |
| Local SQL engine is version 17, not SQL Server 2022 | 15-17 | SQL validation | Compatibility 160 is not proof against the requested production engine | Execute upgrade/fresh install on actual SQL Server 2022 | Blocks production-readiness claim only |
| Redis/backplane connectivity is not locally exercised | 16-17 | `CustSearch.API` Redis configuration | No Redis server is configured on this workstation | Run enabled cache/backplane health and multi-node SignalR tests in deployment environment | Blocks horizontally scaled production-readiness claim only |
| Older phase summaries contain stale statuses | 5-14 | Phase files/README | Intermediate documents were not normalized after merges | Reconcile without erasing history | No |

## Low

| Issue | Phase | Affected files | Reason | Required fix | Blocking |
|---|---:|---|---|---|---|
| Angular SCSS budget warning | 15 | `admin-shell.scss` | Existing bundle exceeds component style budget by 61 bytes | Reduce style output or adjust justified budget | No |
| SQL Browser and Agent stopped | Baseline | Local services | Default instance works and app Worker does not require Agent | None unless discovery/Agent jobs are introduced | No |
