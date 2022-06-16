using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IExperimentTable : IAzureTableV2<ExperimentEntity>
    {
    }

    public class ExperimentTable : AzureTableV2<ExperimentEntity>, IExperimentTable
    {
        protected override string TableName => TableNames.Experiment;
    }
}
