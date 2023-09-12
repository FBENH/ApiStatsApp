using apiBask.Models.Response;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace apiBask.Models.Storage
{
    public class FileService
    {
        private readonly string _storageAccount = "statapistorage";
        private readonly string _key = "XBdQIk61TCCFByFdggigFMjsRJXfMiSMwuIXPPDS/bKYpjezw28YuiFkO85ooT1sdSxJN0+3kPng+AStyn/R4g==";
        private readonly BlobContainerClient _filesContainer;
        private readonly BasketContext _dbContext;

        public FileService(BasketContext dbContext)
        {
            var credential = new StorageSharedKeyCredential(_storageAccount, _key);
            var blobUri = $"https://{_storageAccount}.blob.core.windows.net";
            var blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential);
            _filesContainer = blobServiceClient.GetBlobContainerClient("apibasket");
            _dbContext = dbContext;
        }

        public async Task<List<BlobDto>> ListAsync() 
        {
            List<BlobDto> files = new List<BlobDto>();

            await foreach(var file in _filesContainer.GetBlobsAsync()) 
            {
                string uri = _filesContainer.Uri.ToString();
                var name = file.Name;
                var fullUri = $"{uri}/{name}";

                files.Add(new BlobDto
                {
                    Uri = fullUri,
                    Name = name,
                    ContentType = file.Properties.ContentType
                });

            }
            return files;
        }

        public async Task<Response.Response> UploadAsync(IFormFile blob) 
        {
            apiBask.Models.Response.Response res = new Response.Response();
            BlobResponseDto response = new();
            string uniqueBlobName = $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-{blob.FileName}";
            //BlobClient client = _filesContainer.GetBlobClient(blob.FileName);
            BlobClient client = _filesContainer.GetBlobClient(uniqueBlobName);

            await using(Stream? data = blob.OpenReadStream()) 
            {
                await client.UploadAsync(data);
            }

            response.Status = $"File {blob.FileName} Uploaded Successfully";
            response.Error = false;
            response.Blob.Uri = client.Uri.AbsoluteUri;
            response.Blob.Name = client.Name;
            res.exito = 1;
            res.mensaje = "Imágen subida con éxito";
            res.data = response;

            return res;
        }

        public async Task<BlobDto?> DownloadAsync(string blobFileName) 
        {
            BlobClient file = _filesContainer.GetBlobClient(blobFileName);

            if(await file.ExistsAsync()) 
            {
                var data = await file.OpenReadAsync();
                Stream blobContent = data;

                var content = await file.DownloadContentAsync();

                string name = blobFileName;
                string contentType = content.Value.Details.ContentType;

                return new BlobDto { Content= blobContent, Name= name , ContentType = contentType };
            }
            return null;
        }

        public async Task<BlobResponseDto> DeleteAsync(string blobFileName) 
        {
            BlobClient file = _filesContainer.GetBlobClient(blobFileName);

            await file.DeleteAsync();

            return new BlobResponseDto { Error = false, Status = $"File: {blobFileName} has been successfully deleted" };
        }
    }
}
