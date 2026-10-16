using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace apiBask.Models.Storage
{
    public class FileService
    {
        private static readonly Dictionary<string, string> Extensiones = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
        };

        private readonly Lazy<BlobContainerClient> _filesContainer;

        // La cadena de conexión va en ConnectionStrings:Storage (user-secrets en local, App Settings en Azure).
        public FileService(IConfiguration configuration)
        {
            _filesContainer = new Lazy<BlobContainerClient>(() =>
            {
                var connectionString = configuration.GetConnectionString("Storage");
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Falta configurar ConnectionStrings:Storage.");
                var container = configuration["Storage:Container"] ?? "apibasket";
                return new BlobContainerClient(connectionString, container);
            });
        }

        public static bool EsImagen(IFormFile archivo) => Extensiones.ContainsKey(archivo.ContentType);

        public async Task<Response.Response> UploadAsync(IFormFile archivo)
        {
            // El nombre lo genera el servidor; el del cliente no se usa.
            var nombre = $"{DateTime.UtcNow:yyyy-MM-dd}-{Guid.NewGuid():N}{Extensiones[archivo.ContentType]}";
            var client = _filesContainer.Value.GetBlobClient(nombre);

            await using (var data = archivo.OpenReadStream())
            {
                await client.UploadAsync(data, new BlobHttpHeaders { ContentType = archivo.ContentType });
            }

            var response = new BlobResponseDto { Status = $"File {archivo.FileName} Uploaded Successfully", Error = false };
            response.Blob.Uri = client.Uri.AbsoluteUri;
            response.Blob.Name = client.Name;
            return new Response.Response { exito = 1, mensaje = "Imágen subida con éxito", data = response };
        }
    }
}
