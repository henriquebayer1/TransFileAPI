using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;
using MongoDB.Driver.Core.Configuration;
using System.Text.RegularExpressions;
using TranslateAPI.Interface;
using TranslateAPI.Repository;

namespace TranslateAPI.Services.AzureTranslator
{
    [Route("api/[controller]")]
    [ApiController]
    public class AzureTranslatorController : ControllerBase
    {

        private readonly IAzureTranslatorInterface _AzureRepository;
        private readonly IFileRepository _fileRepository;

        private readonly MongoDbService _mongoDbService;

        public AzureTranslatorController(IAzureTranslatorInterface azureRepository)
        {
            _AzureRepository = azureRepository;
        }

        [HttpGet("UploadedFiles")]
        public async Task<List<ViewModels.FileInfo>> GetBlobs()

        {
            try
            {
                return (await _AzureRepository.GetFiles());


            }
            catch (Exception)
            {

                throw;
            }


        }

        [HttpPost("TranslateDocuments")]
        public async Task<IActionResult> Translation(string language, IList<IFormFile> files, bool useGlossary, string userId)
        {
            try
            {
                await _AzureRepository.Translation(language, files, useGlossary, userId);
                return Ok();
            }
            catch (Exception)
            {

                throw;
            }


        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadDocuments(IList<IFormFile> file, string userId)
        {
            try
            {
                await _AzureRepository.UploadDocuments(file, false, userId);
                return Ok();
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }



        }


        [HttpDelete("DeleteByName")]
        public async Task<IActionResult> Delete(string file)
        {
            try
            {
                await _AzureRepository.DeleteFile(file);
                return Ok();
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }



        }

        [HttpDelete("DeleteFiles")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _AzureRepository.DeleteAllFiles();
                return Ok();
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }



        }

        [HttpGet("DownloadFiles")]
        public async Task<IActionResult> DowloadFile(string userId)
        {
            string _connectionString = "DefaultEndpointsProtocol=https;AccountName=transfilestoragetest;AccountKey=IsbcuPou48XNu9CG3c1Bxp9ruMLBtGzUJFUcszbR/EaYxrNt2uirawgLM9nZJOn9Do6HtiwFilGT+ASt+YCm0g==;EndpointSuffix=core.windows.net";

            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "traductions");

            var blobs = blobContainerClient.GetBlobs();



            var pessoas = new List<string>();


            foreach (var blobL in blobs)
                pessoas.Add(blobL.Name);

            if (pessoas.Count == 1)
            {
                string containerName = "traductions";

                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                StringBuilder blobList = new StringBuilder();


                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    blobList.AppendLine(blobItem.Name);

                }

                var name = blobList.ToString();

                string result = Regex.Replace(name, @"\r\n|\r|\n", "");

                BlobClient blobDowload = new BlobClient(_connectionString, "traductions", result);

                using (var stream = new MemoryStream())
                {

                    await blobDowload.DownloadToAsync(stream);
                    stream.Position = 0;
                    var contentType = (await blobDowload.GetPropertiesAsync()).Value.ContentType;
                    await _AzureRepository.DeleteAllFilesTraductionsId(userId);
                    return File(stream.ToArray(), contentType, blobDowload.Name);

                }

            }

            else
            {
                //AQUI E A OUTRA LOGICA DE DOWLOAD ALL
                using (var memoryStream = new MemoryStream())
                {
                    using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var blob in blobs)
                        {

                            var fileDowloadZip = zipArchive.CreateEntry(blob.Name);
                            var blobClient = blobContainerClient.GetBlobClient(blob.Name);

                            using (var fileStream = fileDowloadZip.Open())
                            {

                                await blobClient.DownloadToAsync(fileStream);


                            }


                        }

                    }
                    memoryStream.Position = 0;
                    var file = File(memoryStream.ToArray(), "application/zip", "TemplateFiles.zip");
                    await _AzureRepository.DeleteAllFilesTraductionsId(userId);
                    return file;


                }

            }
            return null;

        }

        [HttpGet("UploadedFilesTraduction")]
        public async Task<List<ViewModels.FileInfo>> GetBlobs2()

        {
            try
            {
                return (await _AzureRepository.GetFilesTraduction());


            }
            catch (Exception)
            {

                throw;
            }


        }


    }
}