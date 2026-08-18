using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using transferFiles.Options;

public sealed class TransferNowClient
{
    /// <summary>Nombre del HttpClient usado para subir las partes (ver Program.cs).</summary>
    public const string UploadClientName = "transfernow-upload";

    private readonly HttpClient _http;
    private readonly TransferNowOptions _opt;
    private readonly IHttpClientFactory _clientFactory;

    public TransferNowClient(
        HttpClient http,
        IOptions<TransferNowOptions> opt,
        IHttpClientFactory clientFactory)
    {
        _http = http;
        _opt = opt.Value;
        _clientFactory = clientFactory;
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
            throw new HttpRequestException($"POST {fullUrl} -> {(int)resp.StatusCode} {resp.ReasonPhrase}. {Resumir(text)}");
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
    // Instancia (antes era static con su propio HttpClient) para que la subida
    // salga por el mismo proxy configurado en TransferNow:ProxyUrl.
    public async Task UploadPartAsync(string presignedUrl, Stream partStream, long partSize)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, presignedUrl)
        {
            Content = new StreamContent(partStream)
        };
        req.Content.Headers.ContentLength = partSize;

        var http = _clientFactory.CreateClient(UploadClientName); // URL externa (S3 compatible)

        // El host de las URLs firmadas NO es api.transfernow.net, y necesita su
        // propia autorización en el filtro de salida. Si no se nombra en el error,
        // el diagnóstico es a ciegas: un corte de TLS no dice a dónde iba.
        var host = Uri.TryCreate(presignedUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : "(url no parseable)";

        HttpResponseMessage resp;

        try
        {
            resp = await http.SendAsync(req);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException(
                $"Falló la subida de una parte ({partSize} bytes) a '{host}'. " +
                "Si es corte de TLS o timeout, hay que autorizar la salida del servidor a ese host " +
                $"en el proxy/filtro. Detalle: {ex.Message}", ex);
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"PUT a '{host}' -> {(int)resp.StatusCode} {resp.ReasonPhrase}. {Resumir(body)}");
        }
    }

    /// <summary>
    /// Deja el cuerpo de una respuesta de error en algo legible: los filtros de
    /// salida devuelven páginas HTML de ~20 KB que inundan el log.
    /// </summary>
    private static string Resumir(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "(sin cuerpo)";

        var esBloqueo =
            body.Contains("Zscaler", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("don't have permission to visit", StringComparison.OrdinalIgnoreCase);

        if (esBloqueo)
        {
            var categoria = Regex.Match(
                body, @"Not allowed to browse <B>(.*?)</B>", RegexOptions.IgnoreCase);

            return "El filtro de salida bloqueó la petición" +
                   (categoria.Success ? $" — categoría '{categoria.Groups[1].Value}'." : ".") +
                   " Hay que autorizar ese host para la salida del servidor.";
        }

        var texto = body.Replace("\r", " ").Replace("\n", " ");

        return "Body: " + (texto.Length > 500 ? texto[..500] + "… (recortado)" : texto);
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
