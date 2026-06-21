# Native BI — OJDT/JDT1 Individual GET Performance Optimization (Sprint 20C)

**Date:** 2026-06-20  
**Sprint:** 20C  
**Status:** IMPLEMENTED

---

## Problem

`OjdtExtractorJob.ExtractLinesViaIndividualGetAsync` used a sequential `foreach` loop to fetch JDT1 lines via individual `GET /b1s/v1/JournalEntries(N)` calls:

```csharp
foreach (var entry in allEntries)  // Sequential — N HTTP GETs, one-by-one
{
    var fullEntry = await _sl.GetObjectAsync($"JournalEntries({jdtNum})", ct);
}
```

**Performance impact for production scale:**
- 200 journal entries × ~300ms per SAP SL GET = ~60 seconds sequential
- 1,000 entries = ~5 minutes sequential
- SAP SL has no `$expand=JournalEntryLines` support on list endpoints (HTTP 400)
- Individual-GET is the only viable pattern — but must be parallelized

---

## Solution (Sprint 20C)

Replaced sequential `foreach` with concurrent `Task.WhenAll` + `SemaphoreSlim`:

```csharp
var concurrency = _options.JournalEntryLineFetchConcurrency;  // default 3
var sem = new SemaphoreSlim(concurrency, concurrency);
var bag = new ConcurrentBag<(int index, JsonNode line)>();

var tasks = allEntries
    .Select(async (entry, idx) =>
    {
        await sem.WaitAsync(ct);
        try
        {
            var fullEntry = await _sl.GetObjectAsync($"JournalEntries({jdtNum})", ct);
            // collect lines into thread-safe bag
            foreach (var line in lineArr) bag.Add((idx, lineClone));
        }
        finally { sem.Release(); }
    })
    .ToList();

await Task.WhenAll(tasks);
```

Line order is preserved via index-based sorting of the `ConcurrentBag` before returning.

---

## Configuration

| Setting | Default | Description |
|---|---|---|
| `Extractor:JournalEntryLineFetchConcurrency` | `3` | Max concurrent SAP SL GETs for JournalEntries(N) |

In `appsettings.json` or `appsettings.Development.json`:
```json
{
  "Extractor": {
    "JournalEntryLineFetchConcurrency": 3
  }
}
```

**Recommended values:**
- `3` — default, safe for most SAP SL configurations
- `5` — faster, test in staging before production
- `1` — effectively sequential, use if SAP SL returns 429 or session errors

---

## Performance Improvement

| Scenario | Sequential (1) | Concurrent (3) | Concurrent (5) |
|---|---|---|---|
| 50 entries (TST) | ~15s | ~5s | ~3s |
| 200 entries (small client) | ~60s | ~20s | ~12s |
| 1,000 entries (mid client) | ~300s (5 min) | ~100s | ~60s |
| 5,000 entries (large client) | ~25 min | ~8 min | ~5 min |

*Estimates assuming ~300ms per SAP SL GET. Actual times depend on network and SL load.*

---

## Performance Logging

The optimized function logs a summary after completion:

```
OJDT-17C: extracted 122 lines from 50/50 entries (0 failed) in 4823ms (~96ms/GET, concurrency=3)
```

Fields: `lines extracted`, `entries successful/total`, `entries failed`, `total time`, `avg ms/GET`, `concurrency setting`.

---

## Thread Safety Details

- `ConcurrentBag<(int index, JsonNode line)>` — thread-safe accumulator for line results
- `Interlocked.Increment` — atomic counters for success/failure counts
- `Interlocked.CompareExchange` — atomic flag for "log first line fields" (once only)
- `SemaphoreSlim` — controls max concurrent in-flight GETs
- Line order restored after all tasks complete: `bag.OrderBy(x => x.index)`

---

## Files Changed

- [src/DataBision.Extractor/Options/ExtractorOptions.cs](../src/DataBision.Extractor/Options/ExtractorOptions.cs) — added `JournalEntryLineFetchConcurrency` property
- [src/DataBision.Extractor/Extraction/Jobs/OjdtExtractorJob.cs](../src/DataBision.Extractor/Extraction/Jobs/OjdtExtractorJob.cs) — refactored `ExtractLinesViaIndividualGetAsync`
