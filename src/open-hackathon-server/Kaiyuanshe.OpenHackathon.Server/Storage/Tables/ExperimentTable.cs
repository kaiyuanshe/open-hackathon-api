using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IExperimentTable : IAzureTableV2<ExperimentEntity>
    {
    }

    public class ExperimentTable : AzureTableV2<ExperimentEntity>, IExperimentTable
    {
        protected override string TableName => TableNames.Experiment;

        public ExperimentTable(ILogger<ExperimentTable> logger) : base(logger)
        {
        }
    }
}
