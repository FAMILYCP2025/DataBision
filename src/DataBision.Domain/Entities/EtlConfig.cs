namespace DataBision.Domain.Entities;

public class EtlConfig
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int ModuleId { get; set; }
    public string? SapServer { get; set; }
    public string? SapDatabase { get; set; }
    public string? SapType { get; set; }
    public string? AdfPipelineName { get; set; }
    public string? ScheduleCron { get; set; } = "0 2 * * *";
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncStatus { get; set; }
    public bool IsActive { get; set; } = false;

    public Company Company { get; set; } = null!;
    public Module Module { get; set; } = null!;
}
