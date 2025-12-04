using TranslateAPI.Interface;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using System.IO;
using System.Threading.Tasks;
using TranslateAPI.Services;
using MongoDB.Driver;

namespace TranslateAPI.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly MongoDbService _mongoDbService;


        public FileRepository(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }
        public async Task<ObjectId> SaveFileAsync(Stream fileStream, string fileName, string fileLanguage)
        {
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            string name = Path.GetFileNameWithoutExtension(fileName);
            string fileExtension = Path.GetExtension(fileName);  // Captura a extensão

            var conversionInfo = new BsonDocument
    {
        { "docs", fileBytes },
        { "name", name },
        { "fileExtension", fileExtension },  // Adiciona a extensão ao documento
        { "type", BsonNull.Value },
        { "dateModify", DateTime.UtcNow },
        { "idUser", BsonNull.Value },
        { "fileLanguage", fileLanguage },
    };

            var collection = _mongoDbService.GetDatabase.GetCollection<BsonDocument>("historyTranslate");
            await collection.InsertOneAsync(conversionInfo);

            return conversionInfo["_id"].AsObjectId;
        }
 

        public async Task<string> GetFileAsync(ObjectId fileId)
        {
            var collection = _mongoDbService.GetDatabase.GetCollection<BsonDocument>("historyTranslate");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                throw new Exception("Arquivo não encontrado.");
            }


            var fileBytes = document["docs"].AsByteArray;


            string base64File = Convert.ToBase64String(fileBytes);


            return base64File;
        }

    }
}