using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using OOP_1.Models;
using System.Text.Json;

namespace OOP_1.Services
{
    public class GoogleDriveService
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private static readonly string ApplicationName = "Spreadsheet App";
        private static readonly string CredentialsFileName = "credentials.json";
        private DriveService _driveService;

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                UserCredential credential;
                var credPath = Path.Combine(FileSystem.AppDataDirectory, CredentialsFileName);

                using (var stream = await FileSystem.OpenAppPackageFileAsync(CredentialsFileName))
                {
                    string credentialsPath = Path.Combine(FileSystem.CacheDirectory, "token.json");

                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credentialsPath, true));
                }

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка автентифікації: {ex.Message}");
                return false;
            }
        }

        public async Task<string> SaveSpreadsheetAsync(Spreadsheet spreadsheet, string fileName)
        {
            if (_driveService == null)
            {
                throw new InvalidOperationException("Спочатку потрібно авторизуватися");
            }

            try
            {
                var spreadsheetData = new SpreadsheetData
                {
                    RowCount = spreadsheet.RowCount,
                    ColumnCount = spreadsheet.ColumnCount,
                    Cells = spreadsheet.Cells
                        .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value.Expression))
                        .ToDictionary(
                            kvp => kvp.Key.ToString(),
                            kvp => kvp.Value.Expression
                        )
                };

                var json = JsonSerializer.Serialize(spreadsheetData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName.EndsWith(".json") ? fileName : $"{fileName}.json",
                    MimeType = "application/json"
                };

                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
                using var stream = new MemoryStream(byteArray);

                var request = _driveService.Files.Create(fileMetadata, stream, "application/json");
                request.Fields = "id, name";

                var file = await request.UploadAsync();

                if (file.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    return request.ResponseBody.Id;
                }
                else
                {
                    throw new Exception($"Помилка завантаження: {file.Exception?.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка збереження: {ex.Message}");
                throw;
            }
        }

        public async Task<SpreadsheetData> LoadSpreadsheetAsync(string fileId)
        {
            if (_driveService == null)
            {
                throw new InvalidOperationException("Спочатку потрібно авторизуватися");
            }

            try
            {
                var request = _driveService.Files.Get(fileId);
                using var stream = new MemoryStream();

                await request.DownloadAsync(stream);
                stream.Position = 0;

                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var spreadsheetData = JsonSerializer.Deserialize<SpreadsheetData>(json);
                return spreadsheetData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка завантаження: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Google.Apis.Drive.v3.Data.File>> ListSpreadsheetFilesAsync()
        {
            if (_driveService == null)
            {
                throw new InvalidOperationException("Спочатку потрібно авторизуватися");
            }

            try
            {
                var request = _driveService.Files.List();
                request.Q = "mimeType='application/json' and trashed=false";
                request.Fields = "files(id, name, createdTime, modifiedTime)";
                request.OrderBy = "modifiedTime desc";

                var result = await request.ExecuteAsync();
                return result.Files.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка отримання списку файлів: {ex.Message}");
                throw;
            }
        }
    }

    public class SpreadsheetData
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public Dictionary<string, string> Cells { get; set; } = new();
    }
}