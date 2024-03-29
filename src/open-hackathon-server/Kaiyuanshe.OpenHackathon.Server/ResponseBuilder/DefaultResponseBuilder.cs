﻿using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.ResponseBuilder
{
    public interface IResponseBuilder
    {
        ActivityLog BuildActivityLog(ActivityLogEntity activityLogEntity);
        Announcement BuildAnnouncement(AnnouncementEntity announcementEntity);
        Award BuildAward(AwardEntity awardEntity);
        AwardAssignment BuildAwardAssignment(AwardAssignmentEntity awardAssignmentEntity, Award award, Team? team, UserInfo? user);
        Enrollment BuildEnrollment(EnrollmentEntity enrollmentEntity, UserInfo userInfo);
        Experiment BuildExperiment(ExperimentContext context, UserInfo userInfo);
        GuacamoleConnection BuildGuacamoleConnection(ExperimentContext context, TemplateContext? template);
        Hackathon BuildHackathon(HackathonEntity hackathonEntity, HackathonRoles? roles);
        HackathonAdmin BuildHackathonAdmin(HackathonAdminEntity hackathonAdminEntity, UserInfo userInfo);
        Judge BuildJudge(JudgeEntity judgeEntity, UserInfo userInfo);
        Organizer BuildOrganizer(OrganizerEntity organizerEntity);
        Questionnaire BuildQuestionnaire(QuestionnaireEntity questionnaireEntity);
        Rating BuildRating(RatingEntity ratingEntity, UserInfo judge, Team team, RatingKind ratingKind);
        RatingKind BuildRatingKind(RatingKindEntity ratingKindEntity);
        Team BuildTeam(TeamEntity teamEntity, UserInfo creator);
        TeamMember BuildTeamMember(TeamMemberEntity teamMemberEntity, UserInfo member);
        TeamWork BuildTeamWork(TeamWorkEntity teamWorkEntity);
        Template BuildTemplate(TemplateContext context);
        TemplateRepo BuildTemplateRepo(TemplateRepoEntity organizerEntity);
        TopUser BuildTopUser(TopUserEntity topUserEntity, UserInfo userInfo);
        UserInfo BuildUser(UserEntity userEntity);

        TResult BuildResourceList<TSrcItem, TResultItem, TResult>(
            IEnumerable<TSrcItem> items,
            Func<TSrcItem, TResultItem> converter,
            string? nextLink)
            where TResult : IResourceList<TResultItem>, new();

        TResult BuildResourceList<TSrcItem1, TSrcItem2, TResultItem, TResult>(
            IEnumerable<Tuple<TSrcItem1, TSrcItem2>> items,
            Func<TSrcItem1, TSrcItem2, TResultItem> converter,
            string? nextLink)
            where TResult : IResourceList<TResultItem>, new();

        TResult BuildResourceList<TSrcItem1, TSrcItem2, TSrcItem3, TResultItem, TResult>(
            IEnumerable<Tuple<TSrcItem1, TSrcItem2, TSrcItem3>> items,
            Func<TSrcItem1, TSrcItem2, TSrcItem3, TResultItem> converter,
            string? nextLink)
            where TResult : IResourceList<TResultItem>, new();

        Task<TResult> BuildResourceListAsync<TSrcItem, TResultItem, TResult>(
            IEnumerable<TSrcItem> items,
            Func<TSrcItem, CancellationToken, Task<TResultItem>> converter,
            string? nextLink,
            CancellationToken cancellationToken = default)
            where TResult : IResourceList<TResultItem>, new();

        Task<TResult> BuildResourceListAsync<TSrcItem1, TSrcItem2, TResultItem, TResult>(
            IEnumerable<Tuple<TSrcItem1, TSrcItem2>> items,
            Func<TSrcItem1, TSrcItem2, CancellationToken, Task<TResultItem>> converter,
            string? nextLink,
            CancellationToken cancellationToken = default)
            where TResult : IResourceList<TResultItem>, new();
    }

    public class DefaultResponseBuilder : IResponseBuilder
    {
        public ActivityLog BuildActivityLog(ActivityLogEntity activityLogEntity)
        {
            return activityLogEntity.As<ActivityLog>(p =>
            {
                p.updatedAt = activityLogEntity.Timestamp.UtcDateTime;
                p.message = activityLogEntity.GetMessage() ?? "";
                p.messageFormat = activityLogEntity.GetMessageFormat() ?? "";
            });
        }

        public Announcement BuildAnnouncement(AnnouncementEntity announcementEntity)
        {
            return announcementEntity.As<Announcement>(p =>
            {
                p.updatedAt = announcementEntity.Timestamp.UtcDateTime;
            });
        }

        public Award BuildAward(AwardEntity awardEntity)
        {
            return awardEntity.As<Award>(p =>
            {
                p.updatedAt = awardEntity.Timestamp.UtcDateTime;
            });
        }

        public AwardAssignment BuildAwardAssignment(AwardAssignmentEntity awardAssignmentEntity, Award award, Team? team, UserInfo? user)
        {
            return awardAssignmentEntity.As<AwardAssignment>((p) =>
            {
                p.updatedAt = awardAssignmentEntity.Timestamp.UtcDateTime;
                p.award = award;
                p.user = user;
                p.team = team;
            });
        }

        public Enrollment BuildEnrollment(EnrollmentEntity enrollment, UserInfo userInfo)
        {
            return enrollment.As<Enrollment>(p =>
            {
                p.updatedAt = enrollment.Timestamp.UtcDateTime;
                p.user = userInfo;
            });
        }

        public Experiment BuildExperiment(ExperimentContext context, UserInfo userInfo)
        {
            return context.ExperimentEntity.As<Experiment>(p =>
            {
                p.updatedAt = context.ExperimentEntity.Timestamp.UtcDateTime;
                p.user = userInfo;
                p.status = Status.FromV1Status(context.Status);
                p.remoteConnectionUri = String.Format(Guacamole.UriFormat, context.ExperimentEntity.HackathonName, context.ExperimentEntity.Id);
            });
        }

        public GuacamoleConnection BuildGuacamoleConnection(ExperimentContext context, TemplateContext? template)
        {
            GuacamoleConnection conn;
            switch (context.Status.protocol)
            {
                case IngressProtocol.vnc:
                    conn = new VncConnection
                    {
                        name = template?.TemplateEntity?.DisplayName ?? context.ExperimentEntity.TemplateId,
                        protocol = IngressProtocol.vnc,
                        hostname = context.Status.ingressIPs?.FirstOrDefault(),
                        port = context.Status.ingressPort,
                        username = context.Status.vnc?.username,
                        password = context.Status.vnc?.password,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"protocal {context.Status.protocol} is not supported.");
            }

            return conn;
        }

        public Hackathon BuildHackathon(HackathonEntity hackathonEntity, HackathonRoles? roles)
        {
            return hackathonEntity.As<Hackathon>(h =>
            {
                h.updatedAt = hackathonEntity.Timestamp.UtcDateTime;
                h.roles = roles;
            });
        }

        public HackathonAdmin BuildHackathonAdmin(HackathonAdminEntity hackathonAdminEntity, UserInfo userInfo)
        {
            return hackathonAdminEntity.As<HackathonAdmin>(h =>
            {
                h.updatedAt = hackathonAdminEntity.Timestamp.UtcDateTime;
                h.user = userInfo;
            });
        }

        public Judge BuildJudge(JudgeEntity judgeEntity, UserInfo userInfo)
        {
            return judgeEntity.As<Judge>((p) =>
            {
                p.updatedAt = judgeEntity.Timestamp.UtcDateTime;
                p.user = userInfo;
            });
        }

        public Organizer BuildOrganizer(OrganizerEntity organizerEntity)
        {
            return organizerEntity.As<Organizer>(p =>
            {
                p.updatedAt = organizerEntity.Timestamp.UtcDateTime;
            });
        }

        public Questionnaire BuildQuestionnaire(QuestionnaireEntity questionnaireEntity)
        {
            return questionnaireEntity.As<Questionnaire>(p =>
            {
                p.updatedAt = questionnaireEntity.Timestamp.UtcDateTime;
            });
        }

        public Rating BuildRating(RatingEntity ratingEntity, UserInfo judge, Team team, RatingKind ratingKind)
        {
            return ratingEntity.As<Rating>((p) =>
            {
                p.updatedAt = ratingEntity.Timestamp.UtcDateTime;
                p.judge = judge;
                p.team = team;
                p.ratingKind = ratingKind;
            });
        }

        public RatingKind BuildRatingKind(RatingKindEntity ratingKindEntity)
        {
            return ratingKindEntity.As<RatingKind>((p) =>
            {
                p.updatedAt = ratingKindEntity.Timestamp.UtcDateTime;
            });
        }

        public Team BuildTeam(TeamEntity teamEntity, UserInfo creator)
        {
            return teamEntity.As<Team>(p =>
            {
                p.updatedAt = teamEntity.Timestamp.UtcDateTime;
                p.creator = creator;
            });
        }

        public TeamMember BuildTeamMember(TeamMemberEntity teamMemberEntity, UserInfo member)
        {
            return teamMemberEntity.As<TeamMember>(p =>
            {
                p.updatedAt = teamMemberEntity.Timestamp.UtcDateTime;
                p.user = member;
            });
        }

        public TeamWork BuildTeamWork(TeamWorkEntity teamWorkEntity)
        {
            return teamWorkEntity.As<TeamWork>(p =>
            {
                p.updatedAt = teamWorkEntity.Timestamp.UtcDateTime;
            });
        }

        public Template BuildTemplate(TemplateContext context)
        {
            return context.TemplateEntity.As<Template>(p =>
            {
                p.updatedAt = context.TemplateEntity.Timestamp.UtcDateTime;
                p.status = Status.FromV1Status(context.Status);
            });
        }

        public TemplateRepo BuildTemplateRepo(TemplateRepoEntity projectTemplateEntity)
        {
            return projectTemplateEntity.As<TemplateRepo>(p =>
            {
                p.updatedAt = projectTemplateEntity.Timestamp.UtcDateTime;
            });
        }

        public TopUser BuildTopUser(TopUserEntity topUserEntity, UserInfo userInfo)
        {
            return topUserEntity.As<TopUser>(t =>
            {
                t.updatedAt = topUserEntity.Timestamp.UtcDateTime;
                t.user = userInfo;
            });
        }

        public UserInfo BuildUser(UserEntity userEntity)
        {
            return userEntity.As<UserInfo>((p) =>
            {
                p.updatedAt = userEntity.Timestamp.UtcDateTime;
                p.Id = userEntity.PartitionKey;
                p.Token = null;
                p.Password = null;
            });
        }

        public TResult BuildResourceList<TSrcItem, TResultItem, TResult>(
            IEnumerable<TSrcItem> items,
            Func<TSrcItem, TResultItem> converter,
            string? nextLink)
            where TResult : IResourceList<TResultItem>, new()
        {
            return new TResult
            {
                value = items.Select(p => converter(p)).ToArray(),
                nextLink = nextLink,
            };
        }

        public TResult BuildResourceList<TSrcItem1, TSrcItem2, TResultItem, TResult>(
            IEnumerable<Tuple<TSrcItem1, TSrcItem2>> items,
            Func<TSrcItem1, TSrcItem2, TResultItem> converter,
            string? nextLink)
           where TResult : IResourceList<TResultItem>, new()
        {
            return new TResult
            {
                value = items.Select(p => converter(p.Item1, p.Item2)).ToArray(),
                nextLink = nextLink,
            };
        }

        public TResult BuildResourceList<TSrcItem1, TSrcItem2, TSrcItem3, TResultItem, TResult>(
            IEnumerable<Tuple<TSrcItem1, TSrcItem2, TSrcItem3>> items,
            Func<TSrcItem1, TSrcItem2, TSrcItem3, TResultItem> converter,
            string? nextLink)
           where TResult : IResourceList<TResultItem>, new()
        {
            return new TResult
            {
                value = items.Select(p => converter(p.Item1, p.Item2, p.Item3)).ToArray(),
                nextLink = nextLink,
            };
        }

        public async Task<TResult> BuildResourceListAsync<TSrcItem, TResultItem, TResult>(
            IEnumerable<TSrcItem> items,
            Func<TSrcItem, CancellationToken, Task<TResultItem>> converter,
            string? nextLink,
            CancellationToken cancellationToken = default)
            where TResult : IResourceList<TResultItem>, new()
        {
            List<TResultItem> list = new List<TResultItem>();
            foreach (var src in items)
            {
                var resultItem = await converter(src, cancellationToken);
                if (resultItem != null)
                {
                    list.Add(resultItem);
                }
            }

            return new TResult
            {
                value = list.ToArray(),
                nextLink = nextLink,
            };
        }

        public async Task<TResult> BuildResourceListAsync<TSrcItem1, TSrcItem2, TResultItem, TResult>(
            IEnumerable<Tuple<TSrcItem1, TSrcItem2>> items,
            Func<TSrcItem1, TSrcItem2, CancellationToken, Task<TResultItem>> converter,
            string? nextLink,
            CancellationToken cancellationToken = default)
            where TResult : IResourceList<TResultItem>, new()
        {
            List<TResultItem> list = new List<TResultItem>();
            foreach (var src in items)
            {
                var resultItem = await converter(src.Item1, src.Item2, cancellationToken);
                if (resultItem != null)
                {
                    list.Add(resultItem);
                }
            }

            return new TResult
            {
                value = list.ToArray(),
                nextLink = nextLink,
            };
        }

    }
}
