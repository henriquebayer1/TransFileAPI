using MongoDB.Bson;
using System.IO;
using System.Threading.Tasks;

namespace TranslateAPI.Interface
{
    public interface IFileRepository
    {
        Task<ObjectId> SaveFileAsync(Stream fileStream, string fileName, string fileType);
        Task<String> GetFileAsync(ObjectId fileId);
    }
}