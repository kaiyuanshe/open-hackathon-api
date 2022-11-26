using Azure.Storage.Sas;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IFileManagement
    {
        /// <summary>
        /// Get the upload URL.
        /// </summary>
        /// <returns></returns>
        FileUpload GetUploadUrl(ClaimsPrincipal user, FileUpload request);

        Task<byte[]?> DownloadReport(string hackathonName, ReportType reportType, CancellationToken token);
    }

    public class FileManagement : ManagementClient<FileManagement>, IFileManagement
    {
        public static readonly string HackathonApiStaticSite = "https://hackathon-api.static.kaiyuanshe.cn";

        /// <summary>
        /// The minimum SAS expiration time in minutes
        /// </summary>
        static readonly int BlobContainerMinSasExpiration = 2; // minutes

        /// <summary>
        /// The maximum SAS expiration time in minutes
        /// </summary>
        static readonly int BlobContainerMaxSasExpiration = 30; // minutes

        /// <summary>
        /// The default SAS expiration time in minutes
        /// </summary>
        static readonly int BlobContainerDefaultSasExpiration = 5; // minutes

        #region GetUploadUrlAsync
        internal int GetSASExpirationMinitues(FileUpload fileUpload)
        {
            var expirationInMinutes = fileUpload.expiration.GetValueOrDefault(BlobContainerDefaultSasExpiration);
            if (expirationInMinutes > BlobContainerMaxSasExpiration)
            {
                expirationInMinutes = BlobContainerMaxSasExpiration;
            }
            if (expirationInMinutes < BlobContainerMinSasExpiration)
            {
                expirationInMinutes = BlobContainerMinSasExpiration;
            }

            return expirationInMinutes;
        }

        private (string readUrlBase, string writeUrlBase) GetBlobStorageBaseUrls()
        {
            var blobEndpoint = StorageContext.UserBlobContainer.BlobContainerUri;

            // read Url base
            // TODO make it configurable to support different static site.
            string readUrlBase = HackathonApiStaticSite;
            if (EnvironmentHelper.IsDevelopment() && !EnvironmentHelper.IsRunningInTests())
            {
                // return Static WebSite url. Make sure it's enabled.
                // blob endpoint: https://accountName.blob.core.chinacloudapi.cn/
                // static website: https://accountName.z4.web.core.chinacloudapi.cn/
                readUrlBase = blobEndpoint.Replace(".blob.", ".z4.web.");
            }

            // write url base
            string writeUrlBase = blobEndpoint;

            return (readUrlBase, writeUrlBase);
        }

        public FileUpload GetUploadUrl(ClaimsPrincipal user, FileUpload fileUpload)
        {
            var expiration = GetSASExpirationMinitues(fileUpload);
            var leadingTime = BlobContainerMinSasExpiration; // avoid inconsistent timestamp between client and server
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(expiration + leadingTime);

            var permissions = BlobSasPermissions.Read | BlobSasPermissions.Write;

            // TOTO save upload history and apply throttling rules
            // TODO check file exitance

            string userId = ClaimsHelper.GetUserId(user);
            string blobName = $"{userId}/{DateTime.UtcNow.ToString("yyyy/MM/dd")}/{fileUpload.filename}";
            var sasToken = StorageContext.UserBlobContainer.CreateBlobSasToken(blobName, permissions, expiresOn);

            // generate URLs
            var baseUrls = GetBlobStorageBaseUrls();
            fileUpload.expiration = expiration;
            fileUpload.url = $"{baseUrls.readUrlBase}/{blobName}";
            fileUpload.uploadUrl = $"{baseUrls.writeUrlBase}/{BlobContainerNames.StaticWebsite}/{blobName}{sasToken}";
            return fileUpload;
        }
        #endregion

        #region DownloadReport
        public async Task<byte[]?> DownloadReport(string hackathonName, ReportType reportType, CancellationToken token)
        {
            var blobName = $"{hackathonName}/{reportType}.csv";
            var blobExists = await StorageContext.ReportsContainer.ExistsAsync(blobName, token);
            if (!blobExists)
            {
                return null;
            }

            return await StorageContext.ReportsContainer.DownloadBlockBlobAsBytesAsync(blobName, token);
        }
        #endregion
    }
}
