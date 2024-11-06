namespace TranslateAPI.Domains
{
    public class HistoryProps
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? FileName { get; set; }

        public string? FileType { get; set; }

        public DateTime Date { get; set; }

        //Referencia tabela de usuario
        public virtual User? User { get; set; }
    }
}
