using CsvHelper;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports
{
    public abstract class ReportsBaseJob<T> : NonConcurrentCronJob
    {
        protected override TimeSpan Interval => TimeSpan.FromHours(18);

        internal abstract ReportType ReportType { get; }

        protected abstract Task<IList<T>> GenerateReport(HackathonEntity hackathon, CancellationToken token);

        public IUserManagement UserManagement { get; set; }

        internal virtual bool IsEligibleForReport(HackathonEntity hackathon)
        {
            // no report for readOnly hackathons
            if (hackathon.ReadOnly)
            {
                return false;
            }

            // no report: 1 month after hackathon ends 
            if (hackathon.EventEndedAt.HasValue)
            {
                return hackathon.EventEndedAt.Value.AddDays(30) > DateTime.UtcNow;
            }

            // if no end date: only reports online hackathons that were created within 1 year. 
            return hackathon.Status == HackathonStatus.online
                && hackathon.CreatedAt.AddYears(1) > DateTime.UtcNow;
        }


        protected override async Task ExecuteExclusivelyAsync(CancellationToken token)
        {
            var hackathons = await StorageContext.HackathonTable.ListAllHackathonsAsync(token);

            await Parallel.ForEachAsync(hackathons.Values, token, async (hackathon, t) =>
            {
                try
                {
                    var container = StorageContext.ReportsContainer;
                    var blobName = $"{hackathon.Name}/{ReportType}.csv";
                    var reportExists = await container.ExistsAsync(blobName, token);

                    if (!reportExists || IsEligibleForReport(hackathon))
                    {
                        var report = await GenerateReport(hackathon, token);

                        using (var writer = new StringWriter())
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecords(report);
                            await StorageContext.ReportsContainer.UploadBlockBlobAsync(blobName, writer.ToString(), token);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.TraceError($"[{GetType().Name}]Failed to generate report `{ReportType}` for hackathon {hackathon.Name}", e);
                }
            });
        }
    }
}
