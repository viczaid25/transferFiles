using Microsoft.AspNetCore.Mvc;
using transferFiles.Data;
using transferFiles.Models;

public class TransfersController : Controller
{
    private readonly TransferNowClient _tn;
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public TransfersController(TransferNowClient tn, AppDbContext db, IHttpContextAccessor http)
    {
        _tn = tn;
        _db = db;
        _http = http;
    }

    [HttpGet]
    public IActionResult New() => View();

    private static string NormalizeTransferNowLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return "";

        link = link.Trim();

        // Asegura esquema
        if (!link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            link = "https://" + link;
        }

        var uri = new Uri(link);

        // Si viene como meax.transfernow.net, convertir a www.transfernow.net
        var host = uri.Host;
        if (host.EndsWith(".transfernow.net", StringComparison.OrdinalIgnoreCase) &&
            !host.Equals("www.transfernow.net", StringComparison.OrdinalIgnoreCase))
        {
            // fuerza www
            var builder = new UriBuilder(uri) { Host = "www.transfernow.net" };
            return builder.Uri.ToString();
        }

        return uri.ToString();
    }


    private static string GeneratePassword(int length = 10)
    {
        // Evita caracteres ambiguos (O/0, I/l, etc.)
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@$!%*?&";
        var rng = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[rng.Next(s.Length)]).ToArray());
    }

    [HttpPost]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> New(List<IFormFile> files, string? message)
    {
        if (files is null || files.Count == 0)
            return BadRequest("Selecciona al menos un archivo.");

        if (string.IsNullOrWhiteSpace(message))
            return BadRequest("La Razón es obligatoria.");

        var password = GeneratePassword(10);

        // 👇 Forzar vigencia de 7 días en TransferNow
        var validityEnd = DateTimeOffset.UtcNow.AddDays(7);

        var meta = files.Select(f => (f.FileName, f.Length));

        var created = await _tn.CreateTransferAsync(
            meta,
            subject: null,
            message: message,
            validityEnd: validityEnd,
            password: password
        );

        var finalLink = NormalizeTransferNowLink(created.link);

        foreach (var f in created.files)
        {
            var formFile = files.First(ff => ff.FileName == f.name && ff.Length == f.size);
            await using var stream = formFile.OpenReadStream();

            foreach (var part in f.multipartUpload.parts.OrderBy(p => p.partNumber))
            {
                var uploadUrl = await _tn.GetPartUploadUrlAsync(created.transferId, f.id, part.partNumber, f.multipartUpload.uploadId);

                stream.Seek(part.start, SeekOrigin.Begin);
                using var slice = new ReadOnlySubstream(stream, part.size);
                await TransferNowClient.UploadPartAsync(uploadUrl, slice, part.size);
            }

            await _tn.CompleteFileAsync(created.transferId, f.id, f.multipartUpload.uploadId);
        }

        await _tn.CompleteTransferAsync(created.transferId);

        var winUser = (User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity!.Name))
            ? User.Identity!.Name!
            : "unknown";

        var firstFileName = files.First().FileName;

        var log = new TransferLinkLog
        {
            WindowsUser = winUser,
            Link = finalLink,
            CreatedAtUtc = DateTime.UtcNow,
            FileName = firstFileName,
            Status = "Created",
            Password = password,
            Reason = message,

            // (Opcional recomendado) Guarda expiración real (UTC)
            // ExpiresAtUtc = validityEnd.UtcDateTime
        };

        _db.TransferLinkLogs.Add(log);
        await _db.SaveChangesAsync();

        ViewBag.TransferLink = finalLink;
        ViewBag.Password = password;
        ViewBag.ValidityDays = 7;

        return View("Success");
    }


}

// Utilidad para leer un segmento del stream sin copiar a disco
public sealed class ReadOnlySubstream : Stream
{
    private readonly Stream _base;
    private readonly long _length;
    private long _position;

    public ReadOnlySubstream(Stream @base, long length)
    {
        _base = @base;
        _length = length;
    }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position { get => _position; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = (int)Math.Min(count, _length - _position);
        if (remaining <= 0) return 0;
        var read = _base.Read(buffer, offset, remaining);
        _position += read;
        return read;
    }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
