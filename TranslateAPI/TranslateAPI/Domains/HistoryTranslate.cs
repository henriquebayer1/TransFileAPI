using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace TranslateAPI.Domains
{
    public class HistoryTranslate : HistoryProps
    {

        [BsonElement("fileLanguage")]
        public string? FileLanguage { get; set; }

        [BsonElement("fileExtension")]
        public string? fileExtension { get; set; }


    }
}
