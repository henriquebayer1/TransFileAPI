using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace TranslateAPI.Domains
{
    public class HistoryProps
    {
        [BsonId]
        [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string? Name { get; set; }


        [BsonElement("docs")]
        public byte[]? Docs { get; set; }

        [BsonElement("dateModify")]
        public DateTime Date { get; set; }

        //Referencia tabela de usuario
        [BsonElement("idUser")]
        public string? idUser { get; set; }
    }
}
