using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// Bina Google Drive Desktop — browser sign-in se seedha Drive par upload.
    /// App.config mein GoogleDriveClientId / GoogleDriveClientSecret (ek dafa developer set kare).
    /// </summary>
    public static class GoogleDriveApiUploader
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private const string AppName = "Al Madina Shezad Chhutta Services";
        private const string BackupFolderName = "Al Madina Shezad Chhutta Backup";

        private static readonly string TokenStorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZaibPetroleum",
            "google_drive_oauth");

        public static bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(GetClientId());
        }

        public static string UploadBackupFile(string localFilePath)
        {
            string clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException(
                    "Google Drive direct upload ke liye App.config mein GoogleDriveClientId set karein.\n" +
                    "(Google Cloud Console → OAuth Desktop Client — sirf ek dafa setup)");
            }

            string clientSecret = ConfigurationManager.AppSettings["GoogleDriveClientSecret"] ?? "";

            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(TokenStorePath, true)).GetAwaiter().GetResult();

            using (var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName
            }))
            {
                string folderId = GetOrCreateFolder(service, BackupFolderName);
                string fileName = Path.GetFileName(localFilePath);

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new List<string> { folderId }
                };

                using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    FilesResource.CreateMediaUpload request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                    request.Fields = "id, name, webViewLink";
                    IUploadProgress progress = request.Upload();

                    if (progress.Status != UploadStatus.Completed)
                    {
                        Exception ex = progress.Exception ?? new Exception("Google Drive upload fail.");
                        throw new InvalidOperationException("Drive upload error: " + ex.Message, ex);
                    }

                    Google.Apis.Drive.v3.Data.File uploaded = request.ResponseBody;
                    if (uploaded != null && !string.IsNullOrEmpty(uploaded.WebViewLink))
                        return uploaded.WebViewLink;
                    return "https://drive.google.com/drive/my-drive";
                }
            }
        }

        private static string GetOrCreateFolder(DriveService service, string folderName)
        {
            string query = "mimeType='application/vnd.google-apps.folder' and trashed=false and name='" +
                           folderName.Replace("'", "\\'") + "'";
            var listRequest = service.Files.List();
            listRequest.Q = query;
            listRequest.Fields = "files(id, name)";
            FileList list = listRequest.Execute();
            if (list.Files != null && list.Files.Count > 0)
                return list.Files[0].Id;

            var folder = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };
            Google.Apis.Drive.v3.Data.File created = service.Files.Create(folder).Execute();
            return created.Id;
        }

        private static string GetClientId()
        {
            return (ConfigurationManager.AppSettings["GoogleDriveClientId"] ?? "").Trim();
        }
    }
}
