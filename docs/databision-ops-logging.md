# DataBision OPS Logging

Sprint 8I — OPS logging integration for the extractor pipeline.

## Architecture

The OPS logging layer uses **Npgsql direct** (no EF Core) and **never throws** — all errors are swallowed and logged as warnings to keep extraction jobs alive.

### Interface

`IOperationsLogger` (`src/DataBision.Extractor/Operations/IOperationsLogger.cs`):

```csharp
LogExtractionRunAsync(runId, companyId, sapObject, status, ...)
LogExtractionPageAsync(runId, pageNumber, rowsOnPage, ...)
LogTransformRunAsync(runId, companyId, objects, ...)
RefreshPipelineHealthAsync(companyId)
EvaluateAlertRulesAsync(companyId)
```

### Implementation

`OperationsLogger` (`src/DataBision.Extractor/Operations/OperationsLogger.cs`) — direct Npgsql, connection opened once per call.

### Schema targets

| Table | Written by |
|---|---|
| `ops.extractor_run` | Every extraction job start/finish |
| `ops.extractor_page_log` | Every paginated page (optional, high-volume) |
| `ops.transform_run` | Every `--transform` invocation |
| `ops.pipeline_health` | `ops.refresh_pipeline_health(@company_id)` SP |
| `ops.alert_event` | `ops.evaluate_alert_rules(@company_id)` SP |

## CLI Command

```
dotnet run --project src/DataBision.Extractor -- --validate-ops --company <company_id>
```

Shows last 5 extraction runs, page log count, transform run count, alert event count.

## Integration points

- `ExtractorRunner` calls `IOperationsLogger` before and after each object extraction.
- `TransformationRunner` calls `IOperationsLogger` on transform start/finish.
- Both `--service` mode and CLI mode register `IOperationsLogger` via DI.

## Alert rules

`ops.evaluate_alert_rules` fires rules defined in `ops.alert_rule`. 8 rules seeded for `company-dev-001`. Alert events written to `ops.alert_event`. As of 2026-06-15, 2 rules fire per extraction run (expected — low-volume dev data).
