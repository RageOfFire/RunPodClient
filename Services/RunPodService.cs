using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RunPodClient.Models;

namespace RunPodClient.Services;

public class RunPodService : IDisposable
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.runpod.io/v2";

    public RunPodService(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    // ── Submit job ────────────────────────────────────────────────────────────

    public async Task<RunPodJobResponse> SubmitJobAsync(
        string endpointId,
        object inputPayload,
        CancellationToken ct = default)
    {
        var request = new RunPodRequest
        {
            Input = new RunPodInput { Workflow = inputPayload }
        };

        var url = $"{BaseUrl}/{endpointId}/run";
        var response = await _http.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RunPodJobResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty response from RunPod.");

        return result;
    }

    // ── Poll status ───────────────────────────────────────────────────────────

    public async Task<RunPodStatusResponse> GetStatusAsync(
        string endpointId,
        string jobId,
        CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/{endpointId}/status/{jobId}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RunPodStatusResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty status response.");

        return result;
    }

    // ── Cancel job ────────────────────────────────────────────────────────────

    public async Task CancelJobAsync(
        string endpointId,
        string jobId,
        CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/{endpointId}/cancel/{jobId}";
        var response = await _http.PostAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Poll until terminal ───────────────────────────────────────────────────

    public async Task<RunPodStatusResponse> WaitForCompletionAsync(
        string endpointId,
        string jobId,
        int pollIntervalMs,
        int timeoutSeconds,
        Action<string> onStatusUpdate,
        CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (!ct.IsCancellationRequested)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Job {jobId} did not complete within {timeoutSeconds}s.");

            await Task.Delay(pollIntervalMs, ct);

            var status = await GetStatusAsync(endpointId, jobId, ct);
            onStatusUpdate($"Status: {status.Status}");

            if (status.IsTerminal)
                return status;
        }

        throw new OperationCanceledException(ct);
    }

    // ── Download helpers ──────────────────────────────────────────────────────

    public async Task<byte[]> DownloadBytesAsync(string url, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public static byte[] DecodeBase64(string base64)
    {
        // Strip data URI prefix if present (data:image/png;base64,...)
        var idx = base64.IndexOf(',');
        if (idx >= 0) base64 = base64[(idx + 1)..];
        return Convert.FromBase64String(base64);
    }

    public void Dispose() => _http.Dispose();
}
