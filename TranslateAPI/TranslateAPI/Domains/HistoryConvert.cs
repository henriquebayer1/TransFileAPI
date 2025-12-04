using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace TranslateAPI.Domains
{
    public class HistoryConvert : HistoryProps
    {
        [BsonElement("fileType")]
        public string? FileType { get; set; }

        [BsonElement("fileConvertType")]
        public string? FileConvertType { get; set; }

    }
}
