using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IEnrollmentTable : IAzureTableV2<EnrollmentEntity>
    {
    }

    public class EnrollmentTable : AzureTableV2<EnrollmentEntity>, IEnrollmentTable
    {
        protected override string TableName => TableNames.Enrollment;

        public EnrollmentTable(ILogger<EnrollmentTable> logger) : base(logger)
        {

        }
    }
}
