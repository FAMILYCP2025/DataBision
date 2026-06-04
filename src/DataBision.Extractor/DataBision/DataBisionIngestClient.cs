using System.Net.Http.Json;
using System.Text.Json;
using DataBision.Extractor.Options;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.DataBision;

/// <summary>
/// HTTP client for the DataBision Ingest API.
/// Sprint 3B: skeleton only — SendAsync is wired but not called yet.
/// Sprint 3D: full E2E implementation.
/// </summary>
public sealed class DataBisionIngestClient : IDataBisionIngestClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<DataBisionIngestClient> _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DataBisionIngestClient(DataBisionApiOptions options, ILogger<DataBisionIngestClient> log)
    {
        _log = log;
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        _http.DefaultRequestHeaders.Add("X-DataBision-ApiKey", options.ApiKey);
    }

    public async Task<IngestResponse> SendAsync<T>(
        string endpoint, IngestBatch<T> batch, CancellationToken ct = default)
        where T : class
    {
        _log.LogInformation("Sending {Count} rows to {Endpoint}", batch.Rows.Count, endpoint);

        using var response = await _http.PostAsJsonAsync(endpoint, batch, JsonOpts, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _log.LogError("Ingest API error {Status}: {Body}", (int)response.StatusCode, body);
            return new IngestResponse
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Error = body
            };
        }

        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);
            var data = doc.GetProperty("data");
            return new IngestResponse
            {
                Success = true,
                StatusCode = (int)response.StatusCode,
                RowsInserted = data.TryGetProperty("rowsInserted", out var ins) ? ins.GetInt32() : 0,
                RowsUpdated  = data.TryGetProperty("rowsUpdated",  out var upd) ? upd.GetInt32() : 0,
                RowsSkipped  = data.TryGetProperty("rowsSkipped",  out var skp) ? skp.GetInt32() : 0,
            };
        }
        catch (Exception ex)
        {
            _log.LogWarning("Could not parse Ingest API response: {Message}", ex.Message);
            return new IngestResponse { Success = true, StatusCode = (int)response.StatusCode };
        }
    }

    public async Task<ExtractorCheckpoint?> GetCheckpointAsync(
        string companyId, string sapObject, CancellationToken ct = default)
    {
        var url = $"api/ingest/checkpoint/{companyId}/{sapObject}";

        try
        {
            using var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _log.LogInformation("Checkpoint: no prior run for {Obj}", sapObject);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Checkpoint: API returned {Status} for {Obj} — treating as no checkpoint",
                    (int)response.StatusCode, sapObject);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var doc  = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

            if (!doc.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
            {
                _log.LogInformation("Checkpoint: no prior run for {Obj}", sapObject);
                return null;
            }

            var cp = new ExtractorCheckpoint
            {
                WatermarkDate = data.TryGetProperty("watermarkDate", out var wd) && wd.ValueKind != JsonValueKind.Null
                    ? wd.GetString() : null,
                WatermarkTs = data.TryGetProperty("watermarkTs", out var wt) && wt.ValueKind != JsonValueKind.Null
                    ? wt.GetString() : null,
                LastSuccessfulRunUtc = data.TryGetProperty("lastSuccessfulRunUtc", out var lr) && lr.ValueKind != JsonValueKind.Null
                    ? lr.GetDateTime() : null,
                TotalRowsIngested = data.TryGetProperty("totalRowsIngested", out var tr) ? tr.GetInt64() : 0
            };

            _log.LogInformation("Checkpoint: {Obj} — watermark={Wm}, totalIngested={Total}",
                sapObject, cp.WatermarkDate ?? "(none)", cp.TotalRowsIngested);
            return cp;
        }
        catch (Exception ex)
        {
            _log.LogWarning("Checkpoint: failed to read for {Obj} — {Msg}", sapObject, ex.Message);
            return null;
        }
    }

    public void Dispose() => _http.Dispose();
}
