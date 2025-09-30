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

    private static string GeneratePassword(int length = 10)
    {
        // Evita caracteres ambiguos (O/0, I/l, etc.)
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@$!%*?&";
        var rng = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[rng.Next(s.Length)]).ToArray());
    }

    [HttpPost]
    [RequestSizeLimit(long.MaxValue)] // habilitar grandes (configura Kestrel/IIS si aplica)
    public async Task<IActionResult> New(List<IFormFile> files, string? subject, string? message)
    {
        if (files is null || files.Count == 0)
            return BadRequest("Selecciona al menos un archivo.");

        // 1) Generar contraseña automática
        var password = GeneratePassword(10);

        // 2) Crear transfer con contraseña
        var meta = files.Select(f => (f.FileName, f.Length));
        var created = await _tn.CreateTransferAsync(meta, subject, message, password: password);


        // 3) Para cada archivo, subir sus partes
        foreach (var f in created.files)
        {
            var formFile = files.First(ff => ff.FileName == f.name && ff.Length == f.size);
            await using var stream = formFile.OpenReadStream();

            foreach (var part in f.multipartUpload.parts.OrderBy(p => p.partNumber))
            {
                // Pedir URL firmada
                var uploadUrl = await _tn.GetPartUploadUrlAsync(created.transferId, f.id, part.partNumber, f.multipartUpload.uploadId);

                // Cortar el stream a la porción pedida
                stream.Seek(part.start, SeekOrigin.Begin);
                using var slice = new ReadOnlySubstream(stream, part.size); // clase auxiliar abajo
                await TransferNowClient.UploadPartAsync(uploadUrl, slice, part.size);
            }

            // 3) Completar el archivo
            await _tn.CompleteFileAsync(created.transferId, f.id, f.multipartUpload.uploadId);
        }

        // 4) Completar transfer
        await _tn.CompleteTransferAsync(created.transferId);

        // 5 Armar datos para el log
        var winUser =
            _http.HttpContext?.User?.Identity?.Name // "DOMINIO\\usuario" si tienes Windows Auth
            ?? User?.Identity?.Name
            ?? Environment.UserName
            ?? "unknown";

        // Si hay varios archivos, puedes guardar el primero o concatenar
        var firstFileName = files.First().FileName;

        var log = new TransferLinkLog
        {
            WindowsUser = winUser,
            Link = created.link,
            CreatedAtUtc = DateTime.UtcNow,
            FileName = firstFileName,
            Status = "Created",
            Password = password
        };

        _db.TransferLinkLogs.Add(log);
        await _db.SaveChangesAsync();



        // Mostrar link final
        ViewBag.TransferLink = created.link;
        ViewBag.Password = password;
        ViewBag.ValidityDays = 7; // o desde Options si lo tienes
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
