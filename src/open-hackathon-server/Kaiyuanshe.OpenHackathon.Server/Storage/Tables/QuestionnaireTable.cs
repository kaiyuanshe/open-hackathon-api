using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IQuestionnaireTable : IAzureTableV2<QuestionnaireEntity>
    {
    }

    public class QuestionnaireTable : AzureTableV2<QuestionnaireEntity>, IQuestionnaireTable
    {
        protected override string TableName => TableNames.Questionnaire;
    }
}
