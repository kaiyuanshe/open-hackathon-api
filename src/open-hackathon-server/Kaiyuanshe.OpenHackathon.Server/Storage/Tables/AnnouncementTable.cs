using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IAnnouncementTable : IAzureTableV2<AnnouncementEntity>
    {
    }

    public class AnnouncementTable : AzureTableV2<AnnouncementEntity>, IAnnouncementTable
    {
        protected override string TableName => TableNames.Announcement;
    }
}
