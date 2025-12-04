using Azure;
using Azure.AI.Translation.Document;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using TranslateAPI.Interface;
using MongoDB.Bson;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using Azure.Storage.Sas;
using Azure.Storage;
using TranslateAPI.Domains;

namespace TranslateAPI.Services.AzureTranslator
{
    public class AzureTranslatorRepository : IAzureTranslatorInterface
    {
        public IFileRepository _fileRepository;

        public MongoDbService _mongoDbService;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AzureTranslatorRepository(MongoDbService mongoDbService, IHttpContextAccessor httpContextAccessor)
        {
            _mongoDbService = mongoDbService;
            _httpContextAccessor = httpContextAccessor;
        }



        private string _connectionString = "DefaultEndpointsProtocol=https;AccountName=transfilestoragetest;AccountKey=IsbcuPou48XNu9CG3c1Bxp9ruMLBtGzUJFUcszbR/EaYxrNt2uirawgLM9nZJOn9Do6HtiwFilGT+ASt+YCm0g==;EndpointSuffix=core.windows.net";
        public async Task<Task> Translation(string language, IList<IFormFile> files, bool useGlossary, string userId)
        {
            string accountNameSAS = "transfilestoragetest";
            string accountKeySAS = "IsbcuPou48XNu9CG3c1Bxp9ruMLBtGzUJFUcszbR/EaYxrNt2uirawgLM9nZJOn9Do6HtiwFilGT+ASt+YCm0g==";



            string endpoint = "https://transfiletraductorservice.cognitiveservices.azure.com/";
            string apiKey = "5h0vXYEap24E0I2yapvylq762lRrPp0BcLgOPr2ILirHIhA93hZmJQQJ99ALACZoyfiXJ3w3AAAbACOGAHFt";
            BlobContainerClient blobContainerClientVerify = new BlobContainerClient(_connectionString, "traductions");


            BlobContainerClient blobContainerClientBeforeTraduction = new BlobContainerClient(_connectionString, "filetest");
            List<string> blobNameSAS = new List<string>();
            List<string> SASUri = new List<string>();
            List<string> blobUris = new List<string>();


            // Generate SAS Token
            await foreach (BlobItem blobItem in blobContainerClientBeforeTraduction.GetBlobsAsync())
            {

                blobNameSAS.Add(blobItem.Name);
            }



            await foreach (BlobItem blobItem in blobContainerClientBeforeTraduction.GetBlobsAsync())
            {

                BlobClient blobClient = blobContainerClientBeforeTraduction.GetBlobClient(blobItem.Name);

                var blobProperties = await blobClient.GetPropertiesAsync();

                string metaDataValue = "";

                foreach (var metadataItem in blobProperties.Value.Metadata)
                {
                    metaDataValue = metadataItem.Value;
                }
                if (metaDataValue == userId)
                {

                    blobUris.Add(blobItem.Name);

                }
                else
                { bool userTraduction = false; }

            }


            if (useGlossary == true)
            {
                try
                {
                    await UploadDocuments(files, true, userId);



                    BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

                    //  Create a Container
                    Console.WriteLine("Creating container...");
                    BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync($"usercontainer{userId}");

                    //GERAR CHAVE SAS PARA O CONTAINER DO USUARIO CRIADO
                    string sasKeyTemp = GenerateSasTokenContainer(accountNameSAS, accountKeySAS, $"usercontainer{userId}");
                    string sasSourceTemp = $"https://transfilestoragetest.blob.core.windows.net/usercontainer{userId}?{sasKeyTemp}";

                    foreach (var metadataItem in blobUris)
                    {
                        //  Create BlobServiceClient
                        BlobServiceClient blobServiceClientTemp = new BlobServiceClient(_connectionString);

                        //  Access Source Blob
                        BlobContainerClient sourceContainerClient = blobServiceClientTemp.GetBlobContainerClient("filetest");
                        BlobClient sourceBlobClient = sourceContainerClient.GetBlobClient(metadataItem);

                        //  Access Target Container
                        BlobContainerClient targetContainerClient = blobServiceClientTemp.GetBlobContainerClient($"usercontainer{userId}");


                        await targetContainerClient.CreateIfNotExistsAsync();

                        BlobClient targetBlobClient = targetContainerClient.GetBlobClient(metadataItem);

                        // Step 4: Copy Blob
                        await targetBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                    }


                    //LOGICA DE TRADUCAO E TRANSAÇAO PRO BANCO

                    var client = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                    Uri sourceUri = new Uri($"{sasSourceTemp}");
                    Uri targetUri = new Uri("https://transfilestoragetest.blob.core.windows.net/traductions?sp=racwdli&st=2024-12-12T13:35:52Z&se=2024-12-30T21:35:52Z&sv=2022-11-02&sr=c&sig=eg0%2BlgDn%2BdY8yZvaiRxOmbJ04QgIqrxYAnGLs8VWP54%3D");


                    // Glossary URL
                    string GlossaryBlobName = $"glossary{userId}.tsv";
                    string glossarySasUrl = GenerateBlobFileSasUri(_connectionString, GlossaryBlobName);


                    Uri glossaryUri = new Uri($"{glossarySasUrl}");
                    TranslationGlossary glossary = new TranslationGlossary(glossaryUri, "tsv");

                    var input = new DocumentTranslationInput(
                   sourceUri,
                   targetUri,
                   language,
                   glossary
                   );


                    DocumentTranslationOperation operation = await client.StartTranslationAsync(input);
                    await operation.WaitForCompletionAsync();

                    Console.WriteLine($"  Status: {operation.Status}");
                    Console.WriteLine($"  Created on: {operation.CreatedOn}");
                    Console.WriteLine($"  Last modified: {operation.LastModified}");
                    await foreach (DocumentStatusResult document in operation.Value)
                    {
                        Console.WriteLine($"Document with Id: {document.Id}");
                        Console.WriteLine($"  Status:{document.Status}");

                        if (document.Status == DocumentTranslationStatus.Succeeded)
                        {
                            Console.WriteLine($"  foi essa porra");
                        }
                        if (document.Status == DocumentTranslationStatus.Failed)
                        {
                            Console.WriteLine($"Error Code: {document.Error}, Message: {document.Error.Message}");
                        }
                    }

                    await foreach (DocumentStatusResult document in operation.Value)
                    {
                        Console.WriteLine($"Document with Id: {document.Id}");
                        Console.WriteLine($"  Status:{document.Status}");

                        if (document.Status == DocumentTranslationStatus.Succeeded)
                        {
                            foreach (var metadataItemAdd in blobUris)//ADICIONAR METADATA NO ARQUIVO TRADUZIDO
                            {


                                BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "traductions");
                                BlobClient blobClientAdd = blobContainerClient.GetBlobClient(metadataItemAdd);



                                var metadata = new Dictionary<string, string>
                                {
                                    { "ID", $"{userId}" } // Adicionando uma propriedade personalizada
                                };

                                // Atualizar os metadados do blob
                                await blobClientAdd.SetMetadataAsync(metadata);
                            }//ADICIONAR METADATA NO ARQUIVO TRADUZIDO FIM



                            if (document.Status == DocumentTranslationStatus.Failed)
                            {
                                Console.WriteLine($"Error Code: {document.Error}, Message: {document.Error.Message}");
                            }
                            try
                            {
                                Console.WriteLine($"Translated Document Uri: {document.TranslatedDocumentUri}");

                                var blobClient = new BlobClient(new Uri(document.TranslatedDocumentUri.ToString()));

                                if (await blobClient.ExistsAsync())  // Verifica se o blob existe antes de baixar
                                {
                                    using (var memoryStream = new MemoryStream())
                                    {
                                        await blobClient.DownloadToAsync(memoryStream);
                                        memoryStream.Position = 0;

                                        byte[] fileBytes = memoryStream.ToArray();
                                        if (fileBytes.Length == 0)
                                        {
                                            Console.WriteLine("Erro: O arquivo baixado está vazio.");
                                            continue;
                                        }

                                        Console.WriteLine($"Tamanho do documento em bytes: {fileBytes.Length}");

                                        var fileExtension = Path.GetExtension(document.TranslatedDocumentUri.ToString());  // Extrai a extensão

                                        var translationInfo = new BsonDocument
                        {
                            { "docs", fileBytes },
                            { "name", Path.GetFileNameWithoutExtension(document.TranslatedDocumentUri.ToString()) },
                            { "fileExtension", fileExtension },
                            { "fileLanguage", document.TranslatedToLanguageCode },
                            { "dateModify", DateTime.UtcNow },
                            { "idUser", userId }

                                                };

                                        // Inicia uma transação
                                        using (var session = await _mongoDbService.GetDatabase.Client.StartSessionAsync())
                                        {
                                            session.StartTransaction();
                                            try
                                            {
                                                var collection = _mongoDbService.GetDatabase.GetCollection<BsonDocument>("historyTranslate");
                                                await collection.InsertOneAsync(session, translationInfo);

                                                await session.CommitTransactionAsync();
                                                Console.WriteLine("Inserção no MongoDB realizada com sucesso.");
                                            }
                                            catch (Exception ex)
                                            {
                                                await session.AbortTransactionAsync();
                                                Console.WriteLine($"Erro na transação do MongoDB: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                                // IF blobClient.ExistsAsync() TERMINA AQUI
                                else
                                {
                                    Console.WriteLine("Erro: O arquivo traduzido não foi encontrado no Blob Storage.");
                                }
                            }
                            // TRY TERMINA AQUI
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao processar documento: {ex.Message}");
                                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                            }

                        } // IF document.Status TERMINA AQUI
                        else
                        {
                            Console.WriteLine($"Error Code: {document.Error.Code}");
                            Console.WriteLine($"Message: {document.Error.Message}");
                        }
                    }

                    //LOGICA DE TRADUCAO E TRANSAÇAO PRO BANCO ACABA AQUI


                    //Delete the Container
                    Console.WriteLine("Deleting container...");
                    await blobServiceClient.DeleteBlobContainerAsync($"usercontainer{userId}");
                    await DeleteAllFilesId(userId);
                    await DeleteAllFilesGlossaryId(userId);
                    return Task.CompletedTask;
                }
                catch (Exception e)
                {

                    Console.WriteLine(e.Message);
                }

                return Task.CompletedTask;
            }
            else
            {
                try
                {
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

                    //  Create a Container
                    Console.WriteLine("Creating container...");
                    BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync($"usercontainer{userId}");

                    //GERAR CHAVE SAS PARA O CONTAINER DO USUARIO CRIADO
                    string sasKeyTemp = GenerateSasTokenContainer(accountNameSAS, accountKeySAS, $"usercontainer{userId}");
                    string sasSourceTemp = $"https://transfilestoragetest.blob.core.windows.net/usercontainer{userId}?{sasKeyTemp}";
                    foreach (var metadataItem in blobUris)
                    {
                        //  Create BlobServiceClient
                        BlobServiceClient blobServiceClientTemp = new BlobServiceClient(_connectionString);

                        //  Access Source Blob
                        BlobContainerClient sourceContainerClient = blobServiceClientTemp.GetBlobContainerClient("filetest");
                        BlobClient sourceBlobClient = sourceContainerClient.GetBlobClient(metadataItem);

                        //  Access Target Container
                        BlobContainerClient targetContainerClient = blobServiceClientTemp.GetBlobContainerClient($"usercontainer{userId}");


                        await targetContainerClient.CreateIfNotExistsAsync();

                        BlobClient targetBlobClient = targetContainerClient.GetBlobClient(metadataItem);

                        // Step 4: Copy Blob
                        await targetBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                    }






                    //LOGICA DE TRADUCAO E TRANSAÇAO PRO BANCO

                    var client = new DocumentTranslationClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                    Uri sourceUri = new Uri($"{sasSourceTemp}");
                    Uri targetUri = new Uri("https://transfilestoragetest.blob.core.windows.net/traductions?sp=racwdli&st=2024-12-12T13:35:52Z&se=2024-12-30T21:35:52Z&sv=2022-11-02&sr=c&sig=eg0%2BlgDn%2BdY8yZvaiRxOmbJ04QgIqrxYAnGLs8VWP54%3D");


                    var input = new DocumentTranslationInput(
                   sourceUri,
                   targetUri,
                   language

                   );


                    DocumentTranslationOperation operation = await client.StartTranslationAsync(input);
                    await operation.WaitForCompletionAsync();

                    Console.WriteLine($"  Status: {operation.Status}");
                    Console.WriteLine($"  Created on: {operation.CreatedOn}");
                    Console.WriteLine($"  Last modified: {operation.LastModified}");
                    await foreach (DocumentStatusResult document in operation.Value)
                    {
                        Console.WriteLine($"Document with Id: {document.Id}");
                        Console.WriteLine($"  Status:{document.Status}");

                        if (document.Status == DocumentTranslationStatus.Succeeded)
                        {
                            Console.WriteLine($"  foi essa porra");
                        }
                        if (document.Status == DocumentTranslationStatus.Failed)
                        {
                            Console.WriteLine($"Error Code: {document.Error}, Message: {document.Error.Message}");
                        }
                    }

                    await foreach (DocumentStatusResult document in operation.Value)
                    {
                        Console.WriteLine($"Document with Id: {document.Id}");
                        Console.WriteLine($"  Status:{document.Status}");

                        if (document.Status == DocumentTranslationStatus.Succeeded)
                        {
                            foreach (var metadataItemAdd in blobUris)//ADICIONAR METADATA NO ARQUIVO TRADUZIDO
                            {


                                BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "traductions");
                                BlobClient blobClientAdd = blobContainerClient.GetBlobClient(metadataItemAdd);



                                var metadata = new Dictionary<string, string>
                                {
                                    { "ID", $"{userId}" } // Adicionando uma propriedade personalizada
                                };

                                // Atualizar os metadados do blob
                                await blobClientAdd.SetMetadataAsync(metadata);
                            }//ADICIONAR METADATA NO ARQUIVO TRADUZIDO FIM



                            if (document.Status == DocumentTranslationStatus.Failed)
                            {
                                Console.WriteLine($"Error Code: {document.Error}, Message: {document.Error.Message}");
                            }
                            try
                            {
                                Console.WriteLine($"Translated Document Uri: {document.TranslatedDocumentUri}");

                                var blobClient = new BlobClient(new Uri(document.TranslatedDocumentUri.ToString()));

                                if (await blobClient.ExistsAsync())  // Verifica se o blob existe antes de baixar
                                {
                                    using (var memoryStream = new MemoryStream())
                                    {
                                        await blobClient.DownloadToAsync(memoryStream);
                                        memoryStream.Position = 0;

                                        byte[] fileBytes = memoryStream.ToArray();
                                        if (fileBytes.Length == 0)
                                        {
                                            Console.WriteLine("Erro: O arquivo baixado está vazio.");
                                            continue;
                                        }

                                        Console.WriteLine($"Tamanho do documento em bytes: {fileBytes.Length}");

                                        var fileExtension = Path.GetExtension(document.TranslatedDocumentUri.ToString());  // Extrai a extensão

                                        var translationInfo = new BsonDocument
                        {
                            { "docs", fileBytes },
                            { "name", Path.GetFileNameWithoutExtension(document.TranslatedDocumentUri.ToString()) },
                            { "fileExtension", fileExtension },
                            { "fileLanguage", document.TranslatedToLanguageCode },
                            { "dateModify", DateTime.UtcNow },
                            { "idUser", userId }

                                                };

                                        // Inicia uma transação
                                        using (var session = await _mongoDbService.GetDatabase.Client.StartSessionAsync())
                                        {
                                            session.StartTransaction();
                                            try
                                            {
                                                var collection = _mongoDbService.GetDatabase.GetCollection<BsonDocument>("historyTranslate");
                                                await collection.InsertOneAsync(session, translationInfo);

                                                await session.CommitTransactionAsync();
                                                Console.WriteLine("Inserção no MongoDB realizada com sucesso.");
                                            }
                                            catch (Exception ex)
                                            {
                                                await session.AbortTransactionAsync();
                                                Console.WriteLine($"Erro na transação do MongoDB: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                                // IF blobClient.ExistsAsync() TERMINA AQUI
                                else
                                {
                                    Console.WriteLine("Erro: O arquivo traduzido não foi encontrado no Blob Storage.");
                                }
                            }
                            // TRY TERMINA AQUI
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao processar documento: {ex.Message}");
                                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                            }

                        } // IF document.Status TERMINA AQUI
                        else
                        {
                            Console.WriteLine($"Error Code: {document.Error.Code}");
                            Console.WriteLine($"Message: {document.Error.Message}");
                        }
                    }

                    //LOGICA DE TRADUCAO E TRANSAÇAO PRO BANCO ACABA AQUI


                    //Delete the Container
                    Console.WriteLine("Deleting container...");
                    await blobServiceClient.DeleteBlobContainerAsync($"usercontainer{userId}");
                    await DeleteAllFilesId(userId);
                    return Task.CompletedTask;
                }
                catch (Exception e)
                {

                    Console.WriteLine(e.Message);
                }

                return Task.CompletedTask;
            }//ELSE useGlossary == true TERMINA AQUI


        } // FUNCAO TRANSLATE FINALIZA AQUI

        public Task DeleteAllFiles()
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "filetest");

            var allFiles = blobContainerClient.GetBlobs();

            foreach (var file in allFiles)
            {

                var blobClient = blobContainerClient.GetBlobClient(file.Name);
                blobClient.Delete();


            }

            return Task.CompletedTask;
        }

        public Task DeleteAllFilesGlossary()
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "glossary");

            var allFiles = blobContainerClient.GetBlobs();

            foreach (var file in allFiles)
            {

                var blobClient = blobContainerClient.GetBlobClient(file.Name);
                blobClient.Delete();


            }

            return Task.CompletedTask;
        }

        public async Task DeleteFile(string fileName)
        {

            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "filetest");

            BlobClient file = blobContainerClient.GetBlobClient(fileName);

            try
            {

                await file.DeleteAsync();

            }
            catch (RequestFailedException ex)
            {

                if (ex.ErrorCode == BlobErrorCode.BlobNotFound)

                {


                }
            }



        }

        public async Task<List<ViewModels.FileInfo>> GetFiles()
        {


            string containerName = "filetest";

            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            List<ViewModels.FileInfo> li = new List<ViewModels.FileInfo>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {

                var fileInfo = new ViewModels.FileInfo();
                fileInfo.Name = blobItem.Name;
                fileInfo.Date = blobItem.Properties.LastModified.ToString();

                li.Add(fileInfo);



                if (li.Count > 0)
                {


                    string blobName = blobItem.Name;

                    BlobClient blobClient = containerClient.GetBlobClient(blobName);

                    var blobProperties = await blobClient.GetPropertiesAsync();

                    foreach (var metadataItem in blobProperties.Value.Metadata)
                    {


                        fileInfo.UserId = metadataItem.Value;

                        Console.WriteLine($"{metadataItem.Key}: {metadataItem.Value}");
                    }


                }

            }

            return (li);
        }



        public async Task<IActionResult> UploadDocuments(IList<IFormFile> files, bool glossaryUsage = false, string userId = null)
        {
            if (glossaryUsage == true)
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "glossary");

                foreach (IFormFile file in files)
                {
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        stream.Position = 0;
                        await blobContainerClient.UploadBlobAsync(file.FileName, stream);
                    }


                    //aqui comeca uma coisa
                    string containerName = "glossary";

                    BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);



                    await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                    {



                        string blobName = file.FileName;
                        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);



                        var metadata = new Dictionary<string, string>
                        {
                            { "ID", $"{userId}" } // Adicionando uma propriedade personalizada
        };

                        // Atualizar os metadados do blob
                        await blobClient.SetMetadataAsync(metadata);


                    }

                }
            }
            else
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "filetest");



                foreach (IFormFile file in files)
                {
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        stream.Position = 0;
                        await blobContainerClient.UploadBlobAsync(file.FileName, stream);
                    }

                    //aqui comeca uma coisa
                    string containerName = "filetest";

                    BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);



                    await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                    {



                        string blobName = file.FileName;
                        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);



                        var metadata = new Dictionary<string, string>
        {
            { "ID", $"{userId}" } // Adicionando uma propriedade personalizada
        };

                        // Atualizar os metadados do blob
                        await blobClient.SetMetadataAsync(metadata);


                    }


                    //coisa termina
                }
            }



            return null;
        }

        public Task DeleteAllFilesTraductions()
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "traductions");

            var allFiles = blobContainerClient.GetBlobs();

            foreach (var file in allFiles)
            {

                var blobClient = blobContainerClient.GetBlobClient(file.Name);
                blobClient.Delete();


            }

            return Task.CompletedTask;
        }

        public async Task<Task> DeleteAllFilesTraductionsId(string userId)
        {

            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "traductions");
            //PEGAR META DATA
            string containerName = "traductions";

            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            List<ViewModels.FileInfo> li = new List<ViewModels.FileInfo>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {

                var fileInfo = new ViewModels.FileInfo();
                fileInfo.Name = blobItem.Name;
                fileInfo.Date = blobItem.Properties.LastModified.ToString();

                li.Add(fileInfo);

                if (li.Count > 0)
                {
                    string blobName = blobItem.Name;

                    BlobClient blobClient = containerClient.GetBlobClient(blobName);

                    var blobProperties = await blobClient.GetPropertiesAsync();

                    foreach (var metadataItem in blobProperties.Value.Metadata)
                    {


                        fileInfo.UserId = metadataItem.Value;

                        Console.WriteLine($"{metadataItem.Key}: {metadataItem.Value}");
                    }


                }

            }
            //FIM PEGAR METADATA




            foreach (var item in li)
            {
                if (item.UserId == userId)
                {
                    var blobClient = blobContainerClient.GetBlobClient(item.Name);



                    blobClient.Delete();

                }
            }




            return Task.CompletedTask;
        }

        public async Task<Task> DeleteAllFilesGlossaryId(string userId)
        {

            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "glossary");
            //PEGAR META DATA
            string containerName = "glossary";

            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            List<ViewModels.FileInfo> li = new List<ViewModels.FileInfo>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {

                var fileInfo = new ViewModels.FileInfo();
                fileInfo.Name = blobItem.Name;
                fileInfo.Date = blobItem.Properties.LastModified.ToString();

                li.Add(fileInfo);

                if (li.Count > 0)
                {
                    string blobName = blobItem.Name;

                    BlobClient blobClient = containerClient.GetBlobClient(blobName);

                    var blobProperties = await blobClient.GetPropertiesAsync();

                    foreach (var metadataItem in blobProperties.Value.Metadata)
                    {


                        fileInfo.UserId = metadataItem.Value;

                        Console.WriteLine($"{metadataItem.Key}: {metadataItem.Value}");
                    }


                }

            }
            //FIM PEGAR METADATA




            foreach (var item in li)
            {
                if (item.UserId == userId)
                {
                    var blobClient = blobContainerClient.GetBlobClient(item.Name);



                    blobClient.Delete();

                }
            }




            return Task.CompletedTask;
        }

        public async Task<Task> DeleteAllFilesId(string userId)
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, "filetest");
            //PEGAR META DATA
            string containerName = "filetest";

            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            List<ViewModels.FileInfo> li = new List<ViewModels.FileInfo>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {

                var fileInfo = new ViewModels.FileInfo();
                fileInfo.Name = blobItem.Name;
                fileInfo.Date = blobItem.Properties.LastModified.ToString();

                li.Add(fileInfo);

                if (li.Count > 0)
                {
                    string blobName = blobItem.Name;

                    BlobClient blobClient = containerClient.GetBlobClient(blobName);

                    var blobProperties = await blobClient.GetPropertiesAsync();

                    foreach (var metadataItem in blobProperties.Value.Metadata)
                    {


                        fileInfo.UserId = metadataItem.Value;

                        Console.WriteLine($"{metadataItem.Key}: {metadataItem.Value}");
                    }


                }

            }
            //FIM PEGAR METADATA




            foreach (var item in li)
            {
                if (item.UserId == userId)
                {
                    var blobClient = blobContainerClient.GetBlobClient(item.Name);



                    blobClient.Delete();

                }
            }








            return Task.CompletedTask;
        }

        public async Task<List<ViewModels.FileInfo>> GetFilesTraduction()
        {

            string containerName = "traductions";

            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            List<ViewModels.FileInfo> li = new List<ViewModels.FileInfo>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {

                var fileInfo = new ViewModels.FileInfo();
                fileInfo.Name = blobItem.Name;
                fileInfo.Date = blobItem.Properties.LastModified.ToString();

                li.Add(fileInfo);



                if (li.Count > 0)
                {


                    string blobName = blobItem.Name;

                    BlobClient blobClient = containerClient.GetBlobClient(blobName);

                    var blobProperties = await blobClient.GetPropertiesAsync();

                    foreach (var metadataItem in blobProperties.Value.Metadata)
                    {


                        fileInfo.UserId = metadataItem.Value;

                        Console.WriteLine($"{metadataItem.Key}: {metadataItem.Value}");
                    }


                }

            }






            return (li);
        }

        private string GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null)
                throw new UnauthorizedAccessException("Usuário não autenticado.");

            // Usa o "jti" como identificador do usuário
            var userId = user.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("ID do usuário não encontrado no token.");

            return userId;
        }








        static string GenerateSasToken(string accountName, string accountKey, string containerName, string blobName)
        {
            // Create a BlobSasBuilder for an existing blob
            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(3) // Set expiry time
            };

            // Set permissions to allow all possible actions
            sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List | BlobContainerSasPermissions.Write);

            // Generate the SAS token using the storage account key
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(accountName, accountKey);
            return sasBuilder.ToSasQueryParameters(credential).ToString();
        }

        // Método para gerar um SAS URI para o arquivo de glossário
        static string GenerateBlobFileSasUri(string connectionString, string blobName)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("glossary");
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            if (!blobClient.Exists())
            {
                throw new InvalidOperationException($"O arquivo '{blobName}' não existe no container de glossários.");
            }

            var sasUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            return sasUri.ToString();
        }


        static string GenerateSasTokenContainer(string accountName, string accountKey, string containerName)
        {
            // Create a BlobSasBuilder for an existing blob
            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                Resource = "c",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(3) // Set expiry time
            };

            // Set permissions to allow all possible actions
            sasBuilder.SetPermissions(BlobContainerSasPermissions.All);

            // Generate the SAS token using the storage account key
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(accountName, accountKey);
            return sasBuilder.ToSasQueryParameters(credential).ToString();
        }
    }
}