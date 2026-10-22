using System.Text.Json.Serialization;

namespace RunPodClient.Models;

public enum EndpointType { Image, Video }

public class EndpointConfig
{
    public string Name       { get; set; } = string.Empty;
    public EndpointType Type { get; set; }
    public string EndpointId { get; set; } = string.Empty;  // RunPod endpoint ID
}

// ── Request ──────────────────────────────────────────────────────────────────

public class RunPodRequest
{
    [JsonPropertyName("input")]
    public RunPodInput Input { get; set; } = new();
}

public class RunPodInput
{
    [JsonPropertyName("workflow")]
    public object? Workflow { get; set; }

    // For simple text-to-image without a full workflow JSON
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
}

// ── Response ─────────────────────────────────────────────────────────────────

public class RunPodJobResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class RunPodStatusResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("output")]
    public RunPodOutput? Output { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("executionTime")]
    public int? ExecutionTime { get; set; }

    public bool IsTerminal =>
        Status is "COMPLETED" or "FAILED" or "CANCELLED" or "TIMED_OUT";
}

public class RunPodOutput
{
    // worker-comfyui returns images as a list of base64 strings or URLs
    [JsonPropertyName("images")]
    public List<RunPodImage>? Images { get; set; }

    // Video result — URL or base64
    [JsonPropertyName("video")]
    public string? Video { get; set; }

    // Generic message
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class RunPodImage
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    // base64-encoded image data
    [JsonPropertyName("image")]
    public string? Base64 { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }
}

// ── App settings (persisted) ──────────────────────────────────────────────────

public class AppSettings
{
    public string ApiKey          { get; set; } = string.Empty;
    public string ImageEndpointId { get; set; } = string.Empty;
    public string VideoEndpointId { get; set; } = string.Empty;
    public string OutputFolder    { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    public int    PollIntervalMs  { get; set; } = 3000;
    public int    TimeoutSeconds  { get; set; } = 300;
}
