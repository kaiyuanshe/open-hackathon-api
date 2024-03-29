﻿using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITemplateTable : IAzureTableV2<TemplateEntity>
    {
    }

    public class TemplateTable : AzureTableV2<TemplateEntity>, ITemplateTable
    {
        protected override string TableName => TableNames.Template;
    }
}
