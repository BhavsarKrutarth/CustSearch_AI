# Foundation architecture

- `CustSearch.Domain` owns business entities and rules without infrastructure dependencies.
- `CustSearch.Contracts` owns API request/response contracts; EF entities are never returned directly.
- `CustSearch.Application` owns use cases and infrastructure abstractions.
- `CustSearch.Infrastructure` implements EF Core and Dapper access against SQL Server.
- `CustSearch.Integrations` isolates POS, webhook and Python AI clients.
- `CustSearch.API` is the only Admin backend entry point.
- `CustSearch.Worker` hosts reliable background workloads.
- `CustSearch.Admin` is an Angular SPA and is not a .NET project.
- `CustSearch.AI` is the Python AI service boundary.

Schema changes are owned by `/database` scripts. Runtime database creation and EF migrations are forbidden.
