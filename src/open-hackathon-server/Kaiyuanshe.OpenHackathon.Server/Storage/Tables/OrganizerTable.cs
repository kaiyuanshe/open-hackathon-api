using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IOrganizerTable : IAzureTableV2<OrganizerEntity>
    {
    }

    public class OrganizerTable : AzureTableV2<OrganizerEntity>, IOrganizerTable
    {
        protected override string TableName => TableNames.Organizer;
    }
}
