using System;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public class AzureStorageException : Exception
    {
        public int Status { get; set; }
        public string ErrorCode { get; set; }

        public AzureStorageException(int status, string message, string errorCode, Exception inner)
            : base(message, inner)
        {
            Status = status;
            ErrorCode = errorCode;
        }
    }
}
