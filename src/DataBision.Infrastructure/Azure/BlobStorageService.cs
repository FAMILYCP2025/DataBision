using Azure.Storage.Blobs;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataBision.Infrastructure.Azure;

public class BlobStorageService(AppDbContext db, IConfiguration config) : IBrandingService
{
    public async Task<CompanyBranding> UpdateAsync(int companyId, CompanyBranding branding)
    {
        var existing = await db.CompanyBrandings.FirstOrDefaultAsync(b => b.CompanyId == companyId);
        if (existing is null)
        {
            branding.CompanyId = companyId;
            db.CompanyBrandings.Add(branding);
        }
        else
        {
            existing.PrimaryColor = branding.PrimaryColor;
            existing.SecondaryColor = branding.SecondaryColor;
            existing.AccentColor = branding.AccentColor;
            existing.BackgroundColor = branding.BackgroundColor;
            existing.SidebarColor = branding.SidebarColor;
            existing.CompanyDisplayName = branding.CompanyDisplayName;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return existing ?? branding;
    }

    // Placeholder — requires Azure:BlobStorageConnectionString + Azure:BlobContainerName
    public Task<string> UploadLogoAsync(int companyId, Stream fileStream, string fileName, string contentType)
    {
        var connectionString = config["Azure:BlobStorageConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            throw new NotImplementedException("Azure Blob Storage is not configured. Set Azure:BlobStorageConnectionString to enable logo uploads.");

        return UploadAsync(companyId, fileStream, fileName, connectionString);
    }

    private async Task<string> UploadAsync(int companyId, Stream fileStream, string fileName, string connectionString)
    {
        var container = config["Azure:BlobContainerName"] ?? "branding";
        var blobName = $"{companyId}/{Guid.NewGuid()}-{fileName}";
        var blob = new BlobServiceClient(connectionString)
            .GetBlobContainerClient(container)
            .GetBlobClient(blobName);

        await blob.UploadAsync(fileStream, overwrite: true);
        var url = blob.Uri.ToString();

        var branding = await db.CompanyBrandings.FirstOrDefaultAsync(b => b.CompanyId == companyId)
            ?? new CompanyBranding { CompanyId = companyId };

        branding.LogoUrl = url;
        branding.UpdatedAt = DateTime.UtcNow;

        if (branding.Id == 0) db.CompanyBrandings.Add(branding);
        await db.SaveChangesAsync();

        return url;
    }
}
