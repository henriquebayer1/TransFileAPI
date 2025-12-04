using TranslateAPI.Interface;
using TranslateAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TranslateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {

        private readonly IFileRepository _fileRepository;
        private readonly MongoDbService _mongoDbService;

        public FileController(MongoDbService mongoDbService, IFileRepository fileRepository)
        {
            _mongoDbService = mongoDbService;
            _fileRepository = fileRepository;

        }


        [HttpGet("DownloadFile/{fileId}")]
        public async Task<IActionResult> DownloadFile(ObjectId fileId)
        {
            try
            {
                var base64File = await _fileRepository.GetFileAsync(fileId);
                var fileBytes = Convert.FromBase64String(base64File);

                var collection = _mongoDbService.GetDatabase.GetCollection<BsonDocument>("historyTranslate");
                var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);
                var document = await collection.Find(filter).FirstOrDefaultAsync();

                if (document == null)
                {
                    return NotFound("Arquivo não encontrado.");
                }

                string fileName = document["name"].AsString;
                string fileExtension = document["fileExtension"].AsString;  // Recupera a extensão

                string fullFileName = $"{fileName}{fileExtension}";  // Reconstroi o nome completo com a extensão

                return File(fileBytes, "application/octet-stream", fullFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ocorreu um erro: {ex.Message}");
            }
        }




    }
}