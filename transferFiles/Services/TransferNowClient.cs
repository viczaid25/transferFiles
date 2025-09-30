using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using transferFiles.Options;

public sealed class TransferNowClient
{
    private readonly HttpClient _http;
    private readonly TransferNowOptions _opt;

    public TransferNowClient(HttpClient http, IOptions<TransferNowOptions> opt)
    {
        _http = http;
        _opt = opt.Value;
    }

    // Paso 1: crear el transfer (con metadatos de archivos)
    public async Task<CreateTransferResponse> CreateTransferAsync(
    IEnumerable<(string Name, long Size)> files,
    string? subject = null,
    string? message = null,
    string? customId = null,
    DateTimeOffset? validityEnd = null,
    IEnumerable<string>? toEmails = null,
    string? password = null)   // <-- nuevo
    {
        var fileList = files
            .Select(f => new { name = f.Name, size = f.Size })
            .Where(f => !string.IsNullOrWhiteSpace(f.name) && f.size > 0)
            .ToArray();

        var payload = new Dictionary<string, object?>
        {
            ["langCode"] = "es",
            ["toEmails"] = toEmails ?? Array.Empty<string>(),
            ["files"] = fileList,
            ["message"] = message ?? "",
            ["subject"] = subject ?? "",
            ["allowPreview"] = true
        };

        if (!string.IsNullOrWhiteSpace(customId))
            payload["customId"] = customId; // string

        if (validityEnd.HasValue)
            payload["validityEnd"] = validityEnd.Value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"); // string

        if (!string.IsNullOrWhiteSpace(password))
            payload["password"] = password; // <-- contraseña al crear

        var endpoint = "transfers";
        var resp = await _http.PostAsJsonAsync(endpoint, payload);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync();
            var fullUrl = new Uri(_http.BaseAddress!, endpoint).AbsoluteUri;
            throw new HttpRequestException($"POST {fullUrl} -> {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {text}");
        }

        return (await resp.Content.ReadFromJsonAsync<CreateTransferResponse>())!;
    }




    // Paso 2: pedir URL de subida para cada parte
    public async Task<string> GetPartUploadUrlAsync(string transferId, string fileId, int partNumber, string uploadId)
    {
        var url = $"transfers/{transferId}/files/{fileId}/parts/{partNumber}?uploadId={Uri.EscapeDataString(uploadId)}";
        var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<UploadUrlResponse>();
        return json!.uploadUrl;
    }

    // Paso 3: subir la parte a la URL firmada (PUT/Transfer)
    public static async Task UploadPartAsync(string presignedUrl, Stream partStream, long partSize)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, presignedUrl)
        {
            Content = new StreamContent(partStream)
        };
        req.Content.Headers.ContentLength = partSize;
        using var http = new HttpClient(); // URL es externa (S3 compatible)
        var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode(); // respuesta XML 2xx/3xx
    }

    // Paso 4: marcar archivo completo
    public async Task CompleteFileAsync(string transferId, string fileId, string uploadId)
    {
        var url = $"transfers/{transferId}/files/{fileId}/upload-done?uploadId={Uri.EscapeDataString(uploadId)}";
        var resp = await _http.PutAsync(url, content: null);
        resp.EnsureSuccessStatusCode();
    }

    // Paso 5: marcar transfer completo
    public async Task CompleteTransferAsync(string transferId)
    {
        var url = $"transfers/{transferId}/upload-done";
        var resp = await _http.PutAsync(url, content: null);
        resp.EnsureSuccessStatusCode();
    }
}

// DTOs mínimos para parsear respuestas
public sealed class CreateTransferResponse
{
    public string transferId { get; set; } = "";
    public string link { get; set; } = "";
    public string senderSecret { get; set; } = "";
    public List<TransferFile> files { get; set; } = new();
}

public sealed class TransferFile
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public long size { get; set; }
    public MultipartUpload multipartUpload { get; set; } = new();
}

public sealed class MultipartUpload
{
    public string uploadId { get; set; } = "";
    public List<FilePart> parts { get; set; } = new();
}

public sealed class FilePart
{
    public int partNumber { get; set; }
    public long start { get; set; }
    public long size { get; set; }
}

public sealed class UploadUrlResponse { public string uploadUrl { get; set; } = ""; }
