namespace DataBision.Domain.Entities;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }

    public ICollection<Report> Reports { get; set; } = [];
    public ICollection<UserPermission> Permissions { get; set; } = [];
}
