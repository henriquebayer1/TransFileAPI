using TranslateAPI.Domains;

namespace TranslateAPI.Interfaces
{
    public interface IHistoryTranslateRepository
    {
        public List<HistoryTranslate> ListAll();

    }
}
