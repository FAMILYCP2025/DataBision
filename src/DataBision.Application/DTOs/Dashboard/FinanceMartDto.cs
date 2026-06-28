namespace DataBision.Application.DTOs.Dashboard;

public sealed record FinanceMartSummaryDto(
    decimal TotalOpenAr,
    decimal TotalOverdueAr,
    int ArCustomerCount,
    decimal? DsoDays,
    decimal TotalOpenAp,
    decimal TotalOverdueAp,
    int ApSupplierCount,
    decimal? DpoDays);

public sealed record ArAgingRowDto(
    string CardCode,
    string? CardName,
    decimal CurrentAmount,
    decimal Bucket1To30,
    decimal Bucket31To60,
    decimal Bucket61To90,
    decimal Bucket91To120,
    decimal BucketOver120,
    decimal TotalOpen,
    int InvoiceCount,
    DateOnly? OldestDueDate);

public sealed record ApAgingRowDto(
    string CardCode,
    string? CardName,
    decimal CurrentAmount,
    decimal Bucket1To30,
    decimal Bucket31To60,
    decimal Bucket61To90,
    decimal Bucket91To120,
    decimal BucketOver120,
    decimal TotalOpen,
    int InvoiceCount,
    DateOnly? OldestDueDate);

public sealed record FinancePeriodKpiDto(
    int Year,
    int Month,
    decimal ArBilled,
    decimal ArCreditMemo,
    decimal ArNet,
    int ArInvoiceCount,
    decimal ApBilled,
    decimal ApCreditMemo,
    decimal ApNet,
    int ApInvoiceCount);
