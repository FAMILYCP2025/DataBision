# ksdepor OPS Validation Results

Date: 2026-06-15  
Company: `company-dev-001` (CLTSTKSDEPOR)  
Sprint: 8I (OPS logging integration)

## --validate-ops output

```
[OPS-01] Connection open
[OPS-02] extractor_run: total=31, errors=4
[OPS-03] extractor_page_log: 0 pages logged
[OPS-04] transform_run: 10 runs
[OPS-05] alert_event: 40 events fired

run: obj=OWTR status=SUCCESS pages=2 rows=20 at=2026-06-15 23:21:48
run: obj=ODLN status=SUCCESS pages=2 rows=19 at=2026-06-15 23:21:24
run: obj=ORDR status=SUCCESS pages=2 rows=20 at=2026-06-15 23:20:58
run: obj=OPCH status=SUCCESS pages=2 rows=20 at=2026-06-15 23:20:31
run: obj=OPDN status=SUCCESS pages=1 rows=9  at=2026-06-15 23:05:14
```

## Notes

- `errors=4` — 4 failed runs from earlier field validation (DocTotalSy, DocCur, DocStatus, CreateDate removed from OPOR/OPDN/OPCH/ORDR/ODLN FullSelect during Sprint 8J field discovery). Expected.
- `extractor_page_log: 0` — page logging not activated (high-volume table, skipped).
- `alert_event: 40` — 2 alert rules fire per extraction run (low-volume dev data threshold). Expected in DEV.
- `transform_run: 10` — includes runs from base mart + process mart testing cycles.
