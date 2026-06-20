# Sprint 17B — SAP SL $metadata JournalEntry Inspection

**Date:** 2026-06-19  
**System:** SAP B1 HANA TST — CLTSTKSDEPOR  
**SL Version:** 1000290

## Context

After Sprint 17A confirmed that single-record GET works, Sprint 17B fetched `$metadata` to find the formal EntityType definition for JournalEntry and understand why `$expand=JournalEntryLines` is rejected on list endpoints.

## $metadata Response

| Property | Value |
|---|---|
| Total size | 2,053,232 characters |
| Content type | EDMX XML |

## Search Result

The search `Name="JournalEntry"` in the EDMX hit a `NavigationProperty` inside the `LandedCosts` EntityType block, not the JournalEntry EntityType itself:

```xml
<NavigationProperty FromRole="LandedCosts" Name="JournalEntry"
  Relationship="SAPB1.FK_LandedCosts_JournalEntries" ToRole="JournalEntries"/>
<NavigationProperty FromRole="LandedCosts" Name="PurchaseDeliveryNotes"
  Relationship="SAPB1.FK_Documents_LandedCosts" ToRole="Documents"/>
```

The actual `<EntityType Name="JournalEntry">` block was not located in the 306-char block extracted. The $metadata is 2M chars; searching by first occurrence of `Name="JournalEntry"` finds the NavigationProperty reference, not the EntityType definition.

## Practical Impact

Moot. Sprint 17A resolved JDT1 extraction before the $metadata finding. The metadata probe runs in diagnostic-only mode and does not affect the extraction flow.

## Note on $expand Rejection

SL v1000290 rejects `$expand=JournalEntryLines` on collection requests (`GET JournalEntries?$expand=...`). The single-entity endpoint (`GET JournalEntries(N)`) returns `JournalEntryLines` inline without requiring `$expand`. This is a version-specific SL behavior, not an OData spec violation.
