using TranslateAPI.Domains;

namespace TranslateAPI.Interfaces
{
    public interface IHistoryConvertRepository
    {
        public List<HistoryTranslate> ListAll();
    }
}
