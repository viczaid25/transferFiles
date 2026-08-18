using System.Text;

namespace transferFiles.Diagnostics
{
    /// <summary>
    /// Log de excepciones a archivo, best-effort.
    ///
    /// Por qué existe: bajo IIS la carpeta de la app es de solo lectura para el
    /// app pool, así que el log de ANCM (stdoutLogEnabled) no se puede escribir
    /// y las excepciones se pierden — el navegador solo muestra "HTTP ERROR 500".
    /// Esta clase busca la primera carpeta donde SÍ pueda escribir y deja ahí un
    /// archivo por día. Nunca lanza: si no puede escribir en ningún lado, se
    /// queda callada (Ruta == null) y la app sigue funcionando igual.
    ///
    /// Orden de búsqueda:
    ///   1. Diagnostics:LogPath de configuración (si está definida)
    ///   2. &lt;raíz de la app&gt;\logs
    ///   3. %TEMP%\transferFiles-logs   (en IIS suele ser C:\Windows\Temp)
    /// </summary>
    public sealed class ErrorLog
    {
        private readonly object _candado = new();

        private ErrorLog(string? ruta) => Ruta = ruta;

        /// <summary>Carpeta donde se está escribiendo, o null si no se pudo abrir ninguna.</summary>
        public string? Ruta { get; }

        public static ErrorLog Crear(IConfiguration config, IWebHostEnvironment env)
        {
            var candidatas = new[]
            {
                config["Diagnostics:LogPath"],
                Path.Combine(env.ContentRootPath, "logs"),
                Path.Combine(Path.GetTempPath(), "transferFiles-logs")
            };

            foreach (var candidata in candidatas)
            {
                if (string.IsNullOrWhiteSpace(candidata)) continue;
                if (EsEscribible(candidata)) return new ErrorLog(candidata);
            }

            return new ErrorLog(null);
        }

        /// <summary>
        /// Escribe una entrada. Devuelve el archivo donde quedó, o null si no
        /// hay carpeta escribible.
        /// </summary>
        public string? Escribir(string correlationId, string mensaje)
        {
            if (Ruta is null) return null;

            var archivo = Path.Combine(Ruta, $"errores-{DateTime.Now:yyyy-MM-dd}.log");

            var texto = new StringBuilder()
                .AppendLine(new string('─', 78))
                .AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  id={correlationId}")
                .AppendLine(mensaje)
                .AppendLine()
                .ToString();

            try
            {
                lock (_candado)
                {
                    File.AppendAllText(archivo, texto, Encoding.UTF8);
                }

                return archivo;
            }
            catch
            {
                // El log nunca debe tumbar la petición.
                return null;
            }
        }

        private static bool EsEscribible(string carpeta)
        {
            try
            {
                Directory.CreateDirectory(carpeta);

                var prueba = Path.Combine(carpeta, $".escritura-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(prueba, "ok");
                File.Delete(prueba);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
