using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TranslateAPI.ViewModels
{
    public class UserViewModel
    {
        [NotMapped]
        [JsonIgnore]
        public IFormFile? File { get; set; }
        public string? Photo { get; set; }
    }
}
