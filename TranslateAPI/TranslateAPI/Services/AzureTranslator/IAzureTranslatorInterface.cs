using Microsoft.AspNetCore.Mvc;

namespace TranslateAPI.Services.AzureTranslator
{
    public interface IAzureTranslatorInterface
    {
        //Funcao de retorno de documentos e data de upload/modificacao
        public Task<List<ViewModels.FileInfo>> GetFiles();

        public Task<List<ViewModels.FileInfo>> GetFilesTraduction();

        //Funcao de upload de documentos no blob container
        public Task<IActionResult> UploadDocuments(IList<IFormFile> file, bool glossaryUsage = false, string userId = null);

        //Funcao de deletar arquivo pelo nome

        public Task DeleteFile(string fileName);

        //Funcao de deletar todos os arquivos
        public Task DeleteAllFiles();

        public Task DeleteAllFilesGlossary();

        //Funcao de deletar todos os arquivos das traducoes
        public Task DeleteAllFilesTraductions();

        public Task<Task> DeleteAllFilesId(string userId);

        public Task<Task> DeleteAllFilesTraductionsId(string userId);
        public Task<Task> DeleteAllFilesGlossaryId(string userId);


        //Funcao de Traduzir todos os documentos carregados
        public Task<Task> Translation(string language, IList<IFormFile> files, bool useGlossary, string userId);


    }
}