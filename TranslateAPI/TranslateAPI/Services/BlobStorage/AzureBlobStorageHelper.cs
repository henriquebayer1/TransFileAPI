using Azure.Storage.Blobs;

namespace TranslateAPI.Services.BlobStorage
{
    public static class AzureBlobStorageHelper
    {
        public static async Task<string> UploadImageBlobAsync(IFormFile file, string connectionString, string nameContainer )
        {
            try
            {
                if (file != null)
                {
                    var BlobName = Guid.NewGuid().ToString().Replace("-", "") + Path.GetExtension(file.FileName);

                    var blobServiceClient = new BlobServiceClient(connectionString);

                    var blobContainerClient = blobServiceClient.GetBlobContainerClient(nameContainer);

                    var blobClient = blobContainerClient.GetBlobClient(BlobName);

                    using (var stream = file.OpenReadStream() )
                    {
                        await blobClient.UploadAsync(stream);
                    }

                    return blobClient.Uri.ToString();

                }
                else
                {
                    return "https://blobstoragetransfile.blob.core.windows.net/blobstoragetransfile/no-profile.png";
                }

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
