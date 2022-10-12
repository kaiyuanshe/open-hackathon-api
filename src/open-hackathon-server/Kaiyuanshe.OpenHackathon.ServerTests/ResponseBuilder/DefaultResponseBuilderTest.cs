using Authing.ApiClient.Types;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Kaiyuanshe.OpenHackathon.ServerTests.ResponseBuilder
{
    [TestFixture]
    public class DefaultResponseBuilderTest
    {
        #region BuildActivityLog
        [Test]
        public void BuildActivityLog()
        {
            var entity = new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.createHackathon.ToString(),
                Category = ActivityLogCategory.Hackathon,
                CreatedAt = DateTime.UtcNow,
                HackathonName = "hack",
                PartitionKey = "pk",
                RowKey = "rk",
                Timestamp = DateTimeOffset.UtcNow,
                OperatorId = "uid",
                MessageResourceKey = "User_NotFound",
                Messages = new Dictionary<string, string>
                {
                    ["en-US"] = "en"
                }
            };

            var resp = new DefaultResponseBuilder().BuildActivityLog(entity);
            Assert.AreEqual("createHackathon", resp.activityLogType);
            Assert.AreEqual(entity.CreatedAt, resp.createdAt);
            Assert.AreEqual("hack", resp.hackathonName);
            Assert.AreEqual("rk", resp.activityId);
            Assert.AreEqual(entity.Timestamp.DateTime, resp.updatedAt);
            Assert.AreEqual("uid", resp.operatorId);
            Assert.AreEqual("en", resp.message);
            Assert.AreEqual("The specified user is not found.", resp.messageFormat);
        }
        #endregion

        #region BuildAnnouncement
        [Test]
        public void BuildAnnouncement()
        {
            var entity = new AnnouncementEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Content = "content",
                Title = "title",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };

            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildAnnouncement(entity);

            Assert.AreEqual("pk", result.hackathonName);
            Assert.AreEqual("rk", result.id);
            Assert.AreEqual("title", result.title);
            Assert.AreEqual("content", result.content);
            Assert.AreEqual(entity.CreatedAt, result.createdAt);
            Assert.AreEqual(entity.Timestamp.UtcDateTime, result.updatedAt);
        }
        #endregion

        #region BuildAward
        [Test]
        public void BuildAward()
        {
            AwardEntity awardAssignment = new AwardEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Description = "desc",
                Name = "award",
                Quantity = 5,
                Target = AwardTarget.team,
                Pictures = new PictureInfo[]
                {
                    new PictureInfo{ name="pic", uri="uri", description="a pic" }
                },
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };

            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildAward(awardAssignment);

            Assert.AreEqual("pk", result.hackathonName);
            Assert.AreEqual("rk", result.id);
            Assert.AreEqual("award", result.name);
            Assert.AreEqual("desc", result.description);
            Assert.AreEqual(5, result.quantity);
            Assert.AreEqual(AwardTarget.team, result.target);
            Assert.AreEqual(1, result.pictures.Length);
            Assert.AreEqual("pic", result.pictures[0].name);
            Assert.AreEqual("uri", result.pictures[0].uri);
            Assert.AreEqual("a pic", result.pictures[0].description);
            Assert.AreEqual(awardAssignment.CreatedAt, result.createdAt);
            Assert.AreEqual(awardAssignment.Timestamp.UtcDateTime, result.updatedAt);
        }
        #endregion

        #region BuildAwardAssignmentTest
        [Test]
        public void BuildAwardAssignmentTest()
        {
            AwardAssignmentEntity awardAssignment = new AwardAssignmentEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                AssigneeId = "assignee",
                AwardId = "award",
                Description = "desc",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };
            var user = new UserInfo { Device = "device" };
            var team = new Team { id = "teamid" };

            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildAwardAssignment(awardAssignment, team, user);

            Assert.AreEqual("pk", result.hackathonName);
            Assert.AreEqual("rk", result.assignmentId);
            Assert.AreEqual("assignee", result.assigneeId);
            Assert.AreEqual("award", result.awardId);
            Assert.AreEqual("desc", result.description);
            Assert.AreEqual(awardAssignment.CreatedAt, result.createdAt);
            Assert.AreEqual(awardAssignment.Timestamp.UtcDateTime, result.updatedAt);
            Debug.Assert(result.user != null);
            Debug.Assert(result.team != null);
            Assert.AreEqual("device", result.user.Device);
            Assert.AreEqual("teamid", result.team.id);
        }
        #endregion

        #region BuildEnrollment
        [Test]
        public void BuildEnrollmentTest()
        {
            EnrollmentEntity entity = new EnrollmentEntity
            {
                PartitionKey = "hack",
                RowKey = "uid",
                Status = EnrollmentStatus.approved,
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow,
            };
            UserInfo userInfo = new UserInfo
            {
                Name = "name"
            };

            var respBuilder = new DefaultResponseBuilder();
            var enrollment = respBuilder.BuildEnrollment(entity, userInfo);

            Assert.AreEqual("hack", enrollment.hackathonName);
            Assert.AreEqual("uid", enrollment.userId);
            Assert.AreEqual(EnrollmentStatus.approved, enrollment.status);
            Assert.AreEqual(entity.CreatedAt, enrollment.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, enrollment.updatedAt);
            Assert.AreEqual("name", enrollment.user.Name);
        }
        #endregion

        #region BuildExperiment
        [Test]
        public void BuildExperiment()
        {
            ExperimentEntity entity = new ExperimentEntity
            {
                PartitionKey = "hack",
                RowKey = "eid",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow,
                TemplateId = "tn",
                UserId = "uid",
            };
            var context = new ExperimentContext
            {
                ExperimentEntity = entity,
                Status = new ExperimentStatus
                {
                    Code = 204,
                    Message = "msg"
                }
            };
            UserInfo userInfo = new UserInfo
            {
                PostalCode = "code"
            };

            var respBuilder = new DefaultResponseBuilder();
            var experiment = respBuilder.BuildExperiment(context, userInfo);

            Assert.AreEqual("hack", experiment.hackathonName);
            Assert.AreEqual("uid", experiment.userId);
            Assert.AreEqual("eid", experiment.id);
            Assert.AreEqual("tn", experiment.templateId);
            Assert.AreEqual(204, experiment.status.code);
            Assert.AreEqual("msg", experiment.status.message);
            Assert.AreEqual(entity.CreatedAt, experiment.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, experiment.updatedAt);
            Assert.AreEqual("code", experiment.user.PostalCode);
            Assert.AreEqual("https://guacamole.kaiyuanshe.cn/guacamole/#/?hackathon=hack&experiment=eid&token=", experiment.remoteConnectionUri);
        }
        #endregion

        #region BuildGuacamoleConnection
        [Test]
        public void BuildGuacamoleConnection_Vnc()
        {
            var template = new TemplateContext
            {
                TemplateEntity =
                new TemplateEntity { DisplayName = "dn", RowKey = "name" }
            };
            var status = new ExperimentStatus
            {
                ingressIPs = new string[] { "10.0.0.1", "10.0.0.2" },
                ingressPort = 15901,
                protocol = IngressProtocol.vnc,
                vnc = new Vnc
                {
                    username = "un",
                    password = "pwd"
                },
            };
            var context = new ExperimentContext { Status = status };

            var respBuilder = new DefaultResponseBuilder();
            var conn = respBuilder.BuildGuacamoleConnection(context, template);

            Assert.IsTrue(conn is VncConnection);
            Assert.AreEqual(IngressProtocol.vnc, conn.protocol);
            Assert.AreEqual("dn", conn.name);
            VncConnection? vnc = conn as VncConnection;
            Debug.Assert(vnc != null);
            Assert.AreEqual("10.0.0.1", vnc.hostname);
            Assert.AreEqual(15901, vnc.port);
            Assert.AreEqual("un", vnc.username);
            Assert.AreEqual("pwd", vnc.password);
        }
        #endregion

        #region BuildHackathon
        [Test]
        public void BuildHackathonTest()
        {
            var entity = new HackathonEntity
            {
                PartitionKey = "pk",
                AutoApprove = true,
                CreatorId = "abc",
                Location = "loc",
                JudgeStartedAt = DateTime.UtcNow,
                Status = HackathonStatus.online,
                Enrollment = 100,
                ReadOnly = true,
            };

            var roles = new HackathonRoles
            {
                isAdmin = true,
                isEnrolled = true,
                isJudge = true,
            };

            var builder = new DefaultResponseBuilder();
            var hack = builder.BuildHackathon(entity, roles);

            Assert.IsNull(hack.tags);
            Assert.AreEqual("pk", hack.name);
            Assert.IsTrue(hack.autoApprove.Value);
            Assert.AreEqual("loc", hack.location);
            Assert.AreEqual("abc", hack.creatorId);
            Assert.IsTrue(hack.judgeStartedAt.HasValue);
            Assert.IsFalse(hack.judgeEndedAt.HasValue);
            Assert.AreEqual(100, hack.enrollment);
            Assert.IsTrue(hack.roles.isAdmin);
            Assert.IsTrue(hack.roles.isEnrolled);
            Assert.IsTrue(hack.roles.isJudge);
            Assert.IsTrue(hack.readOnly);
        }
        #endregion

        #region BuildHackathonAdmin
        [Test]
        public void BuildHackathonAdmin()
        {
            HackathonAdminEntity entity = new HackathonAdminEntity
            {
                PartitionKey = "hack",
                RowKey = "uid",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow,
            };
            UserInfo userInfo = new UserInfo
            {
                PreferredUsername = "pun"
            };

            var respBuilder = new DefaultResponseBuilder();
            var admin = respBuilder.BuildHackathonAdmin(entity, userInfo);

            Assert.AreEqual("hack", admin.hackathonName);
            Assert.AreEqual("uid", admin.userId);
            Assert.AreEqual(entity.CreatedAt, admin.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, admin.updatedAt);
            Assert.AreEqual("pun", admin.user.PreferredUsername);
        }
        #endregion

        #region BuildJudge
        [Test]
        public void BuildJudgeTest()
        {
            JudgeEntity entity = new JudgeEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Description = "desc",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };
            var user = new UserInfo { MiddleName = "mn" };

            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildJudge(entity, user);

            Assert.AreEqual("pk", result.hackathonName);
            Assert.AreEqual("rk", result.userId);
            Assert.AreEqual("desc", result.description);
            Assert.AreEqual(entity.CreatedAt, result.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, result.updatedAt);
            Assert.AreEqual("mn", result.user.MiddleName);
        }
        #endregion

        #region BuildOrganizer
        [Test]
        public void BuildOrganizer()
        {
            OrganizerEntity entity = new OrganizerEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Description = "desc",
                Name = "kaiyuanshe",
                Type = OrganizerType.host,
                Logo = new PictureInfo
                {
                    description = "logodesc",
                    name = "logoname",
                    uri = "logouri"
                },
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };

            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildOrganizer(entity);

            Assert.AreEqual("pk", result.hackathonName);
            Assert.AreEqual("rk", result.id);
            Assert.AreEqual("desc", result.description);
            Assert.AreEqual("kaiyuanshe", result.name);
            Assert.AreEqual(OrganizerType.host, result.type);
            Assert.AreEqual("logoname", result.logo.name);
            Assert.AreEqual("logodesc", result.logo.description);
            Assert.AreEqual("logouri", result.logo.uri);
            Assert.AreEqual(entity.CreatedAt, result.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, result.updatedAt);
        }
        #endregion

        #region BuildRating
        [Test]
        public void BuildRating()
        {
            RatingEntity entity = new RatingEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Description = "desc",
                JudgeId = "jid",
                RatingKindId = "kid",
                TeamId = "tid",
                Score = 5,
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };
            UserInfo judge = new UserInfo { Country = "country" };
            Team team = new Team { displayName = "myteam" };
            RatingKind kind = new RatingKind { maximumScore = 10 };

            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildRating(entity, judge, team, kind);

            Assert.AreEqual("pk", result.hackathonName);
            Assert.AreEqual("rk", result.id);
            Assert.AreEqual("desc", result.description);
            Assert.AreEqual("jid", result.judgeId);
            Assert.AreEqual("country", result.judge.Country);
            Assert.AreEqual("kid", result.ratingKindId);
            Assert.AreEqual(10, result.ratingKind.maximumScore);
            Assert.AreEqual("tid", result.teamId);
            Assert.AreEqual("myteam", result.team.displayName);
            Assert.AreEqual(5, result.score);
            Assert.AreEqual(entity.CreatedAt, result.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, result.updatedAt);
        }
        #endregion

        #region BuildRatingKind
        [Test]
        public void BuildRatingKind()
        {
            RatingKindEntity entity = new RatingKindEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Description = "desc",
                Name = "name",
                MaximumScore = 20,
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };

            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildRatingKind(entity);

            Assert.AreEqual("pk", result.hackathonName);
            Assert.AreEqual("rk", result.id);
            Assert.AreEqual("desc", result.description);
            Assert.AreEqual("name", result.name);
            Assert.AreEqual(20, result.maximumScore);
            Assert.AreEqual(entity.CreatedAt, result.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, result.updatedAt);
        }
        #endregion

        #region BuildTeam
        [Test]
        public void BuildTeamTest()
        {
            var entity = new TeamEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                AutoApprove = false,
                CreatorId = "uid",
                Description = "desc",
                DisplayName = "dp",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };
            var userInfo = new UserInfo
            {
                Gender = "male"
            };

            var respBuilder = new DefaultResponseBuilder();
            var team = respBuilder.BuildTeam(entity, userInfo);

            Assert.AreEqual("pk", team.hackathonName);
            Assert.AreEqual("rk", team.id);
            Assert.AreEqual(false, team.autoApprove.Value);
            Assert.AreEqual("uid", team.creatorId);
            Assert.AreEqual("desc", team.description);
            Assert.AreEqual("dp", team.displayName);
            Assert.AreEqual(entity.CreatedAt, team.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, team.updatedAt);
            Assert.AreEqual("male", team.creator.Gender);
        }
        #endregion

        #region BuildTeamMember
        [Test]
        public void BuildTeamMemberTest()
        {
            var entity = new TeamMemberEntity
            {
                PartitionKey = "hack",
                RowKey = "uid",
                TeamId = "tid",
                Description = "desc",
                Role = TeamMemberRole.Admin,
                Status = TeamMemberStatus.pendingApproval,
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };
            var user = new UserInfo
            {
                City = "city",
            };

            var respBuilder = new DefaultResponseBuilder();
            var teamMember = respBuilder.BuildTeamMember(entity, user);

            Assert.AreEqual("hack", teamMember.hackathonName);
            Assert.AreEqual("tid", teamMember.teamId);
            Assert.AreEqual("uid", teamMember.userId);
            Assert.AreEqual("desc", teamMember.description);
            Assert.AreEqual(TeamMemberRole.Admin, teamMember.role);
            Assert.AreEqual(TeamMemberStatus.pendingApproval, teamMember.status);
            Assert.AreEqual(entity.CreatedAt, teamMember.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, teamMember.updatedAt);
            Assert.AreEqual("city", teamMember.user.City);
        }
        #endregion

        #region BuildTeamWork
        [Test]
        public void BuildTeamWorkTest()
        {
            var entity = new TeamWorkEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                TeamId = "tid",
                Description = "desc",
                Title = "title",
                Type = TeamWorkType.word,
                Url = "url",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            var respBuilder = new DefaultResponseBuilder();
            var teamWork = respBuilder.BuildTeamWork(entity);

            Assert.AreEqual("tid", teamWork.teamId);
            Assert.AreEqual("rk", teamWork.id);
            Assert.AreEqual("pk", teamWork.hackathonName);
            Assert.AreEqual("desc", teamWork.description);
            Assert.AreEqual("title", teamWork.title);
            Assert.AreEqual(TeamWorkType.word, teamWork.type);
            Assert.AreEqual("url", teamWork.url);
            Assert.AreEqual(entity.CreatedAt, teamWork.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, teamWork.updatedAt);
        }
        #endregion

        #region BuildTemplate
        [Test]
        public void BuildTemplate()
        {
            var entity = new TemplateEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Commands = new string[] { "a", "b", "c" },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "e1", "v1" },
                    { "e2", "v2" }
                },
                DisplayName = "dp",
                Image = "image",
                IngressPort = 22,
                IngressProtocol = IngressProtocol.ssh,
                Vnc = new VncSettings { userName = "un", password = "pw" },
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };
            var status = new k8s.Models.V1Status
            {
                Code = 200,
                Kind = "kind",
            };
            var context = new TemplateContext
            {
                TemplateEntity = entity,
                Status = status
            };

            var respBuilder = new DefaultResponseBuilder();
            var template = respBuilder.BuildTemplate(context);

            Assert.AreEqual("pk", template.hackathonName);
            Assert.AreEqual("rk", template.id);
            Assert.AreEqual(3, template.commands.Length);
            Assert.AreEqual("a", template.commands[0]);
            Assert.AreEqual(2, template.environmentVariables.Count);
            Assert.AreEqual("e1", template.environmentVariables.First().Key);
            Assert.AreEqual("v1", template.environmentVariables.First().Value);
            Assert.AreEqual("image", template.image);
            Assert.AreEqual("dp", template.displayName);
            Assert.AreEqual(22, template.ingressPort);
            Assert.AreEqual(IngressProtocol.ssh, template.ingressProtocol);
            Assert.AreEqual("un", template.vnc.userName);
            Assert.AreEqual("pw", template.vnc.password);
            Assert.AreEqual(entity.CreatedAt, template.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, template.updatedAt);
            Assert.AreEqual(200, template.status.code);
            Assert.AreEqual("kind", template.status.kind);
        }
        #endregion

        #region BuildTopUser
        [Test]
        public void BuildTopUser()
        {
            var entity = new TopUserEntity
            {
                PartitionKey = "2",
                Score = 10,
                UserId = "uid",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };
            var userInfo = new UserInfo
            {
                Name = "un"
            };

            var responseBuilder = new DefaultResponseBuilder();
            var topUser = responseBuilder.BuildTopUser(entity, userInfo);

            Assert.AreEqual(2, topUser.rank);
            Assert.AreEqual(10, topUser.score);
            Assert.AreEqual("uid", topUser.userId);
            Assert.AreEqual(entity.CreatedAt, topUser.createdAt);
            Assert.AreEqual(entity.Timestamp.DateTime, topUser.updatedAt);
            Assert.AreEqual("un", topUser.user.Name);
        }
        #endregion

        #region BuildUser
        [Test]
        public void BuildUser()
        {
            UserEntity entity = new UserEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
                Address = "address",
                Arn = "arn",
                Birthdate = "birthdate",
                Blocked = true,
                Browser = "browser",
                City = "city",
                Company = "company",
                Country = "country",
                Device = "device",
                Email = "email",
                EmailVerified = true,
                ETag = "etag",
                FamilyName = "familyname",
                Formatted = "formatted",
                Gender = "gender",
                GivenName = "givenname",
                Identities = new List<Identity> {
                    new Identity
                    {
                        ConnectionId="connectionId",
                        IsSocial=true,
                        Openid="openid",
                        Provider="provider",
                        UserId="userid",
                        UserIdInIdp="useridinidp",
                        UserPoolId="userpoolid"
                    }
                },
                IsDeleted = true,
                LastIp = "lastip",
                LastLogin = "lastlogin",
                Locale = "locale",
                Locality = "locality",
                LoginsCount = 10,
                MiddleName = "middlename",
                Name = "name",
                Nickname = "nickname",
                OAuth = "oauth",
                OpenId = "openid",
                Password = "password",
                Phone = "phone",
                PhoneVerified = true,
                Photo = "photo",
                PostalCode = "postalcode",
                PreferredUsername = "preferredusername",
                Profile = "profile",
                Province = "province",
                Region = "region",
                RegisterSource = new string[] { "registersource" },
                SignedUp = "signedup",
                StreetAddress = "streetaddress",
                Token = "token",
                TokenExpiredAt = DateTime.UtcNow,
                Unionid = "unionid",
                Username = "username",
                UserPoolId = "userpoolid",
                Website = "website",
                Zoneinfo = "zoneinfo",
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };
            var responseBuilder = new DefaultResponseBuilder();
            var result = responseBuilder.BuildUser(entity);

            Assert.AreEqual("pk", result.Id);
            Assert.AreEqual("address", result.Address);
            Assert.AreEqual("arn", result.Arn);
            Assert.AreEqual("birthdate", result.Birthdate);
            Assert.AreEqual(true, result.Blocked);
            Assert.AreEqual("browser", result.Browser);
            Assert.AreEqual("city", result.City);
            Assert.AreEqual("company", result.Company);
            Assert.AreEqual("country", result.Country);
            Assert.AreEqual("device", result.Device);
            Assert.AreEqual("email", result.Email);
            Assert.AreEqual(true, result.EmailVerified);
            Assert.AreEqual("familyname", result.FamilyName);
            Assert.AreEqual("formatted", result.Formatted);
            Assert.AreEqual("gender", result.Gender);
            Assert.AreEqual("givenname", result.GivenName);
            Assert.AreEqual(1, result.Identities.Count());
            Assert.AreEqual("connectionId", result.Identities.First().ConnectionId);
            Assert.AreEqual(true, result.Identities.First().IsSocial);
            Assert.AreEqual("openid", result.Identities.First().Openid);
            Assert.AreEqual("provider", result.Identities.First().Provider);
            Assert.AreEqual("userid", result.Identities.First().UserId);
            Assert.AreEqual("useridinidp", result.Identities.First().UserIdInIdp);
            Assert.AreEqual("userpoolid", result.Identities.First().UserPoolId);
            Assert.AreEqual(true, result.IsDeleted);
            Assert.AreEqual("lastip", result.LastIp);
            Assert.AreEqual("lastlogin", result.LastLogin);
            Assert.AreEqual("locale", result.Locale);
            Assert.AreEqual("locality", result.Locality);
            Assert.AreEqual(10, result.LoginsCount);
            Assert.AreEqual("middlename", result.MiddleName);
            Assert.AreEqual("name", result.Name);
            Assert.AreEqual("nickname", result.Nickname);
            Assert.AreEqual("oauth", result.OAuth);
            Assert.AreEqual("openid", result.OpenId);
            Assert.AreEqual(null, result.Password); // should be filtered
            Assert.AreEqual("phone", result.Phone);
            Assert.AreEqual(true, result.PhoneVerified);
            Assert.AreEqual("photo", result.Photo);
            Assert.AreEqual("postalcode", result.PostalCode);
            Assert.AreEqual("preferredusername", result.PreferredUsername);
            Assert.AreEqual("profile", result.Profile);
            Assert.AreEqual("province", result.Province);
            Assert.AreEqual("region", result.Region);
            Assert.AreEqual(1, result.RegisterSource.Count());
            Assert.AreEqual("registersource", result.RegisterSource.First());
            Assert.AreEqual("signedup", result.SignedUp);
            Assert.AreEqual("streetaddress", result.StreetAddress);
            Assert.AreEqual(null, result.Token); // should be filtered
            Assert.AreEqual(entity.TokenExpiredAt, result.TokenExpiredAt);
            Assert.AreEqual("unionid", result.Unionid);
            Assert.AreEqual("username", result.Username);
            Assert.AreEqual("userpoolid", result.UserPoolId);
            Assert.AreEqual("website", result.Website);
            Assert.AreEqual("zoneinfo", result.Zoneinfo);
            Assert.AreEqual(entity.CreatedAt, result.createdAt);
            Assert.AreEqual(entity.Timestamp.UtcDateTime, result.updatedAt);
        }
        #endregion

        #region BuildResourceListTest
        [Test]
        public void BuildResourceListTest()
        {
            string nextLink = "nextlink";
            List<Tuple<EnrollmentEntity, UserInfo>> enrollments = new List<Tuple<EnrollmentEntity, UserInfo>>
            {
                Tuple.Create(new EnrollmentEntity
                {
                    PartitionKey = "pk1",
                    RowKey = "rk1",
                    Status = EnrollmentStatus.approved,
                },
                new UserInfo
                {
                    Id = "uid"
                }),
                Tuple.Create(new EnrollmentEntity
                {
                    PartitionKey = "pk2",
                    RowKey = "rk2",
                    Status = EnrollmentStatus.rejected,
                },
                new UserInfo{
                    Email = "email"
                })
            };

            var builder = new DefaultResponseBuilder();
            var result = builder.BuildResourceList<EnrollmentEntity, UserInfo, Enrollment, EnrollmentList>(
                    enrollments,
                    builder.BuildEnrollment,
                    nextLink);

            Assert.AreEqual("nextlink", result.nextLink);
            Assert.AreEqual(2, result.value.Length);
            Assert.AreEqual("pk1", result.value[0].hackathonName);
            Assert.AreEqual("rk1", result.value[0].userId);
            Assert.AreEqual(EnrollmentStatus.approved, result.value[0].status);
            Assert.AreEqual("uid", result.value[0].user.Id);
            Assert.AreEqual("pk2", result.value[1].hackathonName);
            Assert.AreEqual("rk2", result.value[1].userId);
            Assert.AreEqual(EnrollmentStatus.rejected, result.value[1].status);
            Assert.AreEqual("email", result.value[1].user.Email);
        }
        #endregion
    }
}
