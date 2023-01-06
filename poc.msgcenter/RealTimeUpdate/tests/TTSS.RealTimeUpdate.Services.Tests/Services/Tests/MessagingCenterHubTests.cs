using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using System.Linq.Expressions;
using TTSS.Infrastructure.Data.Mongo;
using TTSS.Infrastructure.Services;
using TTSS.Infrastructure.Services.Models;
using TTSS.RealTimeUpdate.Services.DbModels;

namespace TTSS.RealTimeUpdate.Services.Tests
{
    public class MessagingCenterHubTests
    {
        private DateTime currentTime = DateTime.UtcNow;
        private readonly IFixture fixture;
        private readonly Mock<IHubClients> hubClientsMock;
        private readonly Mock<IGroupManager> groupManagerMock;
        private readonly Mock<IDateTimeService> datetimeSvcMock;
        private readonly Mock<IMongoRepository<MessageInfo, string>> mongoRepoMock;
        private readonly Mock<IServiceManager> serviceManagerMock;
        private readonly Mock<IServiceHubContext> serviceHubContextMock;
        private readonly Func<DateTime> GetCurrentTime;

        private IMessagingCenterHub sut => fixture.Create<MessagingCenterHub>();

        public MessagingCenterHubTests()
        {
            fixture = new Fixture();
            fixture.Register<IHubClients>(() => hubClientsMock.Object);
            fixture.Register<IGroupManager>(() => groupManagerMock.Object);
            fixture.Register<IDateTimeService>(() => datetimeSvcMock.Object);
            fixture.Register<IMongoRepository<MessageInfo, string>>(() => mongoRepoMock.Object);
            fixture.Register<IServiceManager>(() => serviceManagerMock.Object);
            fixture.Register<IServiceHubContext>(() => serviceHubContextMock.Object);
            hubClientsMock = fixture.Create<Mock<IHubClients>>();
            groupManagerMock = fixture.Create<Mock<IGroupManager>>();
            datetimeSvcMock = fixture.Create<Mock<IDateTimeService>>();
            mongoRepoMock = fixture.Create<Mock<IMongoRepository<MessageInfo, string>>>();
            serviceManagerMock = fixture.Create<Mock<IServiceManager>>();
            serviceHubContextMock = fixture.Create<Mock<IServiceHubContext>>();
            GetCurrentTime = () => currentTime;
            datetimeSvcMock.Setup(it => it.UtcNow).Returns(GetCurrentTime);
            serviceHubContextMock
                .Setup(it => it.Clients)
                .Returns(hubClientsMock.Object);
            serviceHubContextMock
                .Setup(it => it.Groups)
                .Returns(groupManagerMock.Object);
        }

        #region RequestOTP

        [Fact]
        public async Task RequestOTP_AllDataValid_ThenSendASecretCodeBackToTheCaller()
        {
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.Client(It.IsAny<string>()))
                .Returns<string>(_ => clientMock.Object);

            var req = fixture.Create<InvocationContext>();
            await sut.SendClientSecret(req);

            clientMock
                .Verify(it => it.SendCoreAsync(
                    It.Is<string>(actual => actual == "setClientId"),
                    It.Is<object[]>(actual => actual.All(arg => arg.ToString().Contains(req.ConnectionId))),
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task RequestOTP_AllDataValid_ButClientDoesNotExisting_ThenSystemMustNotCrash()
        {
            hubClientsMock
                .Setup(it => it.Client(It.IsAny<string>()))
                .Returns<string>(_ => null);

            await sut.SendClientSecret(fixture.Create<InvocationContext>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public Task RequestOTP_WithInvalidParam_ThenSystemDoNothing(string connId)
            => validateInvalidParam_MustDoNothing(new InvocationContext { ConnectionId = null });

        [Fact]
        public Task RequestOTP_WithParamIsNull_ThenSystemDoNothing()
            => validateInvalidParam_MustDoNothing(null);

        private async Task validateInvalidParam_MustDoNothing(InvocationContext req)
        {
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.Client(It.IsAny<string>()))
                .Returns<string>(_ => clientMock.Object);

            await sut.SendClientSecret(req);

            clientMock
                .Verify(it => it.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()), Times.Never());
        }

        #endregion

        #region JoinGroup

        [Fact]
        public async Task JoinGroup_AllDataValid_ThenAddTheClientToTheGroup()
        {
            var secret = Guid.NewGuid().ToString();
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.User(It.IsAny<string>()))
                .Returns<string>(cid => cid == secret ? clientMock.Object : null);

            var req = fixture.Create<JoinGroupRequest>();
            req.Secret = secret;
            var actual = await sut.JoinGroup(req);
            actual.Should().NotBeNull();
            actual.ErrorMessage.Should().BeNull();
            actual.Nonce.Should().Be(req.Nonce);
            actual.JoinGroupName.Should().Be(req.GroupName);

            hubClientsMock
                .Verify(it => it.User(It.Is<string>(actual => actual == secret)), Times.Exactly(1));
            groupManagerMock
                .Verify(it => it.AddToGroupAsync(
                    It.Is<string>(actual => actual == secret),
                    It.Is<string>(actual => actual == req.GroupName),
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task JoinGroup_AllDataValid_ButTheClientDoesNotExisting_ThenSystemDoNothing()
        {
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.User(It.IsAny<string>()))
                .Returns<string>(_ => null);

            var req = fixture.Create<JoinGroupRequest>();
            var actual = await sut.JoinGroup(req);
            actual.Should().NotBeNull();
            actual.ErrorMessage.Should().NotBeNullOrEmpty();
            actual.Nonce.Should().Be(req.Nonce);
            actual.JoinGroupName.Should().BeNullOrEmpty();

            hubClientsMock
                .Verify(it => it.User(It.IsAny<string>()), Times.Exactly(1));
            groupManagerMock
                .Verify(it => it.AddToGroupAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Never());
        }

        [Theory]
        [InlineData(null, "valid")]
        [InlineData("", "valid")]
        [InlineData(" ", "valid")]
        [InlineData("valid", null)]
        [InlineData("valid", "")]
        [InlineData("valid", " ")]
        public Task JoinGroup_WithInvalidParam_ThenSystemDoNothing(string secret, string groupName)
            => validateJoinGroupWithInvalidParam(new()
            {
                Secret = secret,
                GroupName = groupName,
            });

        [Fact]
        public Task JoinGroup_WithParameterIsNull_ThenSystemDoNothing()
            => validateJoinGroupWithInvalidParam(null);

        private async Task validateJoinGroupWithInvalidParam(JoinGroupRequest req)
        {
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.User(It.IsAny<string>()))
                .Returns<string>(cid => clientMock.Object);

            var actual = await sut.JoinGroup(req);
            actual.Should().NotBeNull();
            actual.ErrorMessage.Should().NotBeNullOrEmpty();
            actual.Nonce.Should().Be(req?.Nonce);
            actual.JoinGroupName.Should().BeNullOrEmpty();

            hubClientsMock
                .Verify(it => it.User(It.IsAny<string>()), Times.Never());
            groupManagerMock
                .Verify(it => it.AddToGroupAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Never());
        }

        #endregion

        #region Send

        [Theory, AutoData]
        public Task Send_NotificationContent_AllDataValid_ThenSendMessageToTheGroup(SendMessage<NotificationContent> message)
            => validateSendMessages_AllDataValid_ThenTheMessageMustBeSend(new[] { message });

        [Theory, AutoData]
        public Task Send_DynamicContent_AllDataValid_ThenSendTheMessageToTheGroup(SendMessage<DynamicContent> message)
            => validateSendMessages_AllDataValid_ThenTheMessageMustBeSend(new[] { message });

        [Theory, AutoData]
        public Task Send_NotificationContents_AllDataValid_ThenSendMessagesToTheGroup(IEnumerable<SendMessage<NotificationContent>> messages)
            => validateSendMessages_AllDataValid_ThenTheMessageMustBeSend(messages);

        [Theory, AutoData]
        public Task Send_DynamicContents_AllDataValid_ThenSendTheMessagesToTheGroup(IEnumerable<SendMessage<DynamicContent>> messages)
            => validateSendMessages_AllDataValid_ThenTheMessageMustBeSend(messages);

        private async Task validateSendMessages_AllDataValid_ThenTheMessageMustBeSend<T>(IEnumerable<SendMessage<T>> messages)
            where T : MessageContent
        {
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.Group(It.IsAny<string>()))
                .Returns<string>(_ => clientMock.Object);
            mongoRepoMock
                .Setup(it => it.Get(It.IsAny<Expression<Func<MessageInfo, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(() => Enumerable.Empty<MessageInfo>());
            var result = await sut.Send(messages);
            result.Should().NotBeNull();
            result.ErrorMessage.Should().BeNullOrWhiteSpace();
            result.NonceStatus.Should().BeEquivalentTo(
                messages.Select(it => it.Nonce)
                .ToDictionary(it => it, _ => true));

            mongoRepoMock
               .Verify(it => it.Get(
                   It.IsAny<Expression<Func<MessageInfo, bool>>>(),
                   It.IsAny<CancellationToken>()), Times.Exactly(messages.Count()));
            mongoRepoMock
                .Verify(it =>
                    it.InsertAsync(It.Is<MessageInfo>(actual => validateInsertAsync(actual, messages)),
                    It.IsAny<CancellationToken>()), Times.Exactly(messages.Count()));
            mongoRepoMock
                .Verify(it =>
                    it.InsertAsync(It.IsAny<MessageInfo>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(messages.Count()));
            var targetGroupQry = messages.SelectMany(it => it.TargetGroups).Distinct();
            var expectedTotalSendToGroups = targetGroupQry.Count();
            hubClientsMock
                   .Verify(it => it.Group(It.Is<string>(actual => targetGroupQry.Contains(actual))), Times.Exactly(expectedTotalSendToGroups));
            clientMock
                .Verify(it => it.SendCoreAsync(
                    It.Is<string>(actual => actual == "update"),
                    It.Is<object[]>(actual => validateSyncCoreAsyncParam(actual, messages, currentTime.Ticks)),
                    It.IsAny<CancellationToken>()), Times.Exactly(expectedTotalSendToGroups));
            clientMock
                .Verify(it => it.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(expectedTotalSendToGroups));
        }

        [Theory]
        [InlineData("g1,g2,g1,g3", "s1", "a1")]
        [InlineData("g1", "s1,s2,s1,s3", "a1")]
        [InlineData("g1", "s1", "a1,a2,a1,a3")]
        [InlineData("g1,g2,g1,g3", "s1,s2,s1,s3", "a1")]
        [InlineData("g1,g2,g1,g3", "s1", "a1,a2,a1,a3")]
        [InlineData("g1", "s1,s2,s1,s3", "a1,a2,a1,a3")]
        [InlineData("g1,g2,g1,g3", "s1,s2,s1,s3", "a1,a2,a1,a3")]
        public async Task Send_NotificationContents_WithDuplicatedValue_ThenSystemMustRemoveTheDuplicatedValue(string groups, string scopes, string activities)
        {
            var message = fixture.Create<SendMessage<NotificationContent>>();
            message.TargetGroups = groups.Split(',', StringSplitOptions.RemoveEmptyEntries);
            message.Filter = new()
            {
                Scopes = scopes.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Activities = activities.Split(',', StringSplitOptions.RemoveEmptyEntries),
            };
            await validateSendMessages_AllDataValid_ThenTheMessageMustBeSend(new[] { message });
        }

        [Theory]
        [InlineData("g1,g2,g1,g3", "s1", "a1")]
        [InlineData("g1", "s1,s2,s1,s3", "a1")]
        [InlineData("g1", "s1", "a1,a2,a1,a3")]
        [InlineData("g1,g2,g1,g3", "s1,s2,s1,s3", "a1")]
        [InlineData("g1,g2,g1,g3", "s1", "a1,a2,a1,a3")]
        [InlineData("g1", "s1,s2,s1,s3", "a1,a2,a1,a3")]
        [InlineData("g1,g2,g1,g3", "s1,s2,s1,s3", "a1,a2,a1,a3")]
        public async Task Send_DynamicContents_WithDuplicatedValue_ThenSystemMustRemoveTheDuplicatedValue(string groups, string scopes, string activities)
        {
            var message = fixture.Create<SendMessage<DynamicContent>>();
            message.TargetGroups = groups.Split(',', StringSplitOptions.RemoveEmptyEntries);
            message.Filter = new()
            {
                Scopes = scopes.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Activities = activities.Split(',', StringSplitOptions.RemoveEmptyEntries),
            };
            await validateSendMessages_AllDataValid_ThenTheMessageMustBeSend(new[] { message });
        }

        [Theory, AutoData]
        public Task Send_NotificationContent_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(SendMessage<NotificationContent> message)
            => validateSendMessages_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(new[] { message });

        [Theory, AutoData]
        public Task Send_DynamicContent_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(SendMessage<DynamicContent> message)
            => validateSendMessages_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(new[] { message });

        [Theory, AutoData]
        public Task Send_NotificationContents_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(IEnumerable<SendMessage<NotificationContent>> messages)
            => validateSendMessages_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(messages);

        [Theory, AutoData]
        public Task Send_DynamicContents_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(IEnumerable<SendMessage<DynamicContent>> messages)
            => validateSendMessages_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend(messages);

        private async Task validateSendMessages_AllDataValid_ButGroupNotFound_ThenTheMessageMustNotBeSend<T>(IEnumerable<SendMessage<T>> messages)
            where T : MessageContent
        {
            hubClientsMock
                .Setup(it => it.Group(It.IsAny<string>()))
                .Returns<string>(_ => null);
            mongoRepoMock
                .Setup(it => it.Get(It.IsAny<Expression<Func<MessageInfo, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(() => Enumerable.Empty<MessageInfo>());
            var result = await sut.Send(messages);
            result.Should().NotBeNull();
            result.ErrorMessage.Should().BeNullOrWhiteSpace();
            result.NonceStatus.Should().BeEquivalentTo(
                messages.Select(it => it.Nonce)
                .ToDictionary(it => it, _ => true));

            mongoRepoMock
               .Verify(it => it.Get(
                   It.IsAny<Expression<Func<MessageInfo, bool>>>(),
                   It.IsAny<CancellationToken>()), Times.Exactly(messages.Count()));
            mongoRepoMock
                .Verify(it =>
                    it.InsertAsync(It.Is<MessageInfo>(actual => validateInsertAsync(actual, messages)),
                    It.IsAny<CancellationToken>()), Times.Exactly(messages.Count()));
            mongoRepoMock
                .Verify(it =>
                    it.InsertAsync(It.IsAny<MessageInfo>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(messages.Count()));
            var targetGroupQry = messages.SelectMany(it => it.TargetGroups).Distinct();
            var expectedTotalSendToGroups = targetGroupQry.Count();
            hubClientsMock
                   .Verify(it => it.Group(It.Is<string>(actual => targetGroupQry.Contains(actual))), Times.Exactly(expectedTotalSendToGroups));
        }

        [Theory]
        [ClassData(typeof(InvalidDynamicMessages))]
        [ClassData(typeof(InvalidNotificationMessages))]
        public async Task Send_InvalidMessages_ThenThoseMessagesMustNotBeSend(IEnumerable<SendMessage> messages)
        {
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.Group(It.IsAny<string>()))
                .Returns<string>(_ => clientMock.Object);
            mongoRepoMock
                .Setup(it => it.Get(It.IsAny<Expression<Func<MessageInfo, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(() => Enumerable.Empty<MessageInfo>());
            var result = await sut.Send(messages);
            result.Should().NotBeNull();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            var expectedNonceStatus = messages?
                .Where(it => !string.IsNullOrWhiteSpace(it?.Nonce))
                .Select(it => it.Nonce)
                .ToDictionary(it => it, _ => false) ?? new();
            result.NonceStatus.Should().BeEquivalentTo(expectedNonceStatus);

            mongoRepoMock
                .Verify(it => it.Get(
                    It.IsAny<Expression<Func<MessageInfo, bool>>>(),
                    It.IsAny<CancellationToken>()), Times.Never());
            mongoRepoMock
                .Verify(it =>
                    it.InsertAsync(It.IsAny<MessageInfo>(),
                    It.IsAny<CancellationToken>()), Times.Never());
            hubClientsMock
                   .Verify(it => it.Group(It.IsAny<string>()), Times.Never());
            clientMock
                .Verify(it => it.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()), Times.Never());
        }
        public class InvalidDynamicMessages : TheoryData<IEnumerable<SendMessage<DynamicContent>>>
        {
            private Fixture fixture;

            public InvalidDynamicMessages()
            {
                fixture = new Fixture();
                Send_DynamicContent_With_NonceIsNull();
                Send_DynamicContent_With_NonceIsEmpty();
                Send_DynamicContent_With_FilterIsNull();
                Send_DynamicContent_With_FilterActivitiesAreNull();
                Send_DynamicContent_With_FilterActivitiesAreEmpty();
                Send_DynamicContent_With_FilterScopesAreNull();
                Send_DynamicContent_With_FilterScopesAreEmpty();
                Send_DynamicContent_With_TargetGroupsAreNull();
                Send_DynamicContent_With_TargetGroupsAreEmpty();
                Send_DynamicContents_With_Null();
                Send_DynamicContents_With_EmptyList();
                Send_DynamicContent_With_SomeRecordsAreNull();
                Send_DynamicContent_With_DataIsNull();
            }
            void Send_DynamicContent_With_NonceIsNull()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Nonce = null;
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_NonceIsEmpty()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Nonce = string.Empty;
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_FilterIsNull()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Filter = null;
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_FilterActivitiesAreNull()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Filter.Activities = null;
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_FilterActivitiesAreEmpty()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Filter.Activities = Enumerable.Empty<string>();
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_FilterScopesAreNull()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Filter.Scopes = null;
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_FilterScopesAreEmpty()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Filter.Scopes = Enumerable.Empty<string>();
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_TargetGroupsAreNull()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.TargetGroups = null;
                Add(new[] { msg });
            }
            void Send_DynamicContent_With_TargetGroupsAreEmpty()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.TargetGroups = Enumerable.Empty<string>();
                Add(new[] { msg });
            }
            void Send_DynamicContents_With_Null()
            {
                Add(null);
            }
            void Send_DynamicContents_With_EmptyList()
            {
                Add(Enumerable.Empty<SendMessage<DynamicContent>>());
            }
            void Send_DynamicContent_With_SomeRecordsAreNull()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.TargetGroups = Enumerable.Empty<string>();
                Add(new[] { msg, null });
            }
            void Send_DynamicContent_With_DataIsNull()
            {
                var msg = fixture.Create<SendMessage<DynamicContent>>();
                msg.Content.Data = null;
                Add(new[] { msg });
            }
        }
        public class InvalidNotificationMessages : TheoryData<IEnumerable<SendMessage<NotificationContent>>>
        {
            private Fixture fixture;

            public InvalidNotificationMessages()
            {
                fixture = new Fixture();
                Send_NotificationContent_With_NonceIsNull();
                Send_NotificationContent_With_NonceIsEmpty();
                Send_NotificationContent_With_FilterIsNull();
                Send_NotificationContent_With_FilterActivitiesAreNull();
                Send_NotificationContent_With_FilterActivitiesAreEmpty();
                Send_NotificationContent_With_FilterScopesAreNull();
                Send_NotificationContent_With_FilterScopesAreEmpty();
                Send_NotificationContent_With_TargetGroupsAreNull();
                Send_NotificationContent_With_TargetGroupsAreEmpty();
                Send_NotificationContents_With_Null();
                Send_NotificationContents_With_EmptyList();
                Send_NotificationContent_With_SomeRecordsAreNull();
                Send_NotificationContent_With_MessageIsNull();
                Send_NotificationContent_With_MessageIsEmpty();
                Send_NotificationContent_With_MessageIsWhitespace();
                Send_NotificationContent_With_EndpointUrlIsNull();
                Send_NotificationContent_With_EndpointUrlIsEmpty();
                Send_NotificationContent_With_EndpointUrlIsWhitespace();
            }
            void Send_NotificationContent_With_NonceIsNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Nonce = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_NonceIsEmpty()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Nonce = string.Empty;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_FilterIsNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Filter = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_FilterActivitiesAreNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Filter.Activities = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_FilterActivitiesAreEmpty()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Filter.Activities = Enumerable.Empty<string>();
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_FilterScopesAreNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Filter.Scopes = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_FilterScopesAreEmpty()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Filter.Scopes = Enumerable.Empty<string>();
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_TargetGroupsAreNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.TargetGroups = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_TargetGroupsAreEmpty()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.TargetGroups = Enumerable.Empty<string>();
                Add(new[] { msg });
            }
            void Send_NotificationContents_With_Null()
            {
                Add(null);
            }
            void Send_NotificationContents_With_EmptyList()
            {
                Add(Enumerable.Empty<SendMessage<NotificationContent>>());
            }
            void Send_NotificationContent_With_SomeRecordsAreNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.TargetGroups = Enumerable.Empty<string>();
                Add(new[] { msg, null });
            }
            void Send_NotificationContent_With_MessageIsNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Content.EndpointUrl = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_MessageIsEmpty()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Content.EndpointUrl = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_MessageIsWhitespace()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Content.EndpointUrl = " ";
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_EndpointUrlIsNull()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Content.EndpointUrl = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_EndpointUrlIsEmpty()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Content.EndpointUrl = null;
                Add(new[] { msg });
            }
            void Send_NotificationContent_With_EndpointUrlIsWhitespace()
            {
                var msg = fixture.Create<SendMessage<NotificationContent>>();
                msg.Content.EndpointUrl = " ";
                Add(new[] { msg });
            }
        }

        [Theory, AutoData]
        public async Task Send_DynamicContent_WithDuplicatedNonce_ThenSendErrorBackToTheCaller(SendMessage<DynamicContent> message)
            => await validateDuplicatedNonce(new[] { message });

        [Theory, AutoData]
        public async Task Send_NotificationContent_WithDuplicatedNonce_ThenSendErrorBackToTheCaller(SendMessage<NotificationContent> message)
            => await validateDuplicatedNonce(new[] { message });

        private async Task validateDuplicatedNonce(IEnumerable<SendMessage> messages)
        {
            fixture.Register<MessageContent>(() => fixture.Create<NotificationContent>());
            var clientMock = fixture.Create<Mock<IClientProxy>>();
            hubClientsMock
                .Setup(it => it.Group(It.IsAny<string>()))
                .Returns<string>(_ => clientMock.Object);
            mongoRepoMock
                .Setup(it => it.Get(It.IsAny<Expression<Func<MessageInfo, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(() => fixture.Create<IEnumerable<MessageInfo>>());
            var result = await sut.Send(messages);
            result.Should().NotBeNull();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            var expectedNonceStatus = messages?
                .Where(it => !string.IsNullOrWhiteSpace(it?.Nonce))
                .Select(it => it.Nonce)
                .ToDictionary(it => it, _ => false) ?? new();
            result.NonceStatus.Should().BeEquivalentTo(expectedNonceStatus);

            mongoRepoMock
                .Verify(it => it.Get(
                    It.IsAny<Expression<Func<MessageInfo, bool>>>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(1));
            mongoRepoMock
                .Verify(it =>
                    it.InsertAsync(It.IsAny<MessageInfo>(),
                    It.IsAny<CancellationToken>()), Times.Never());
            hubClientsMock
                   .Verify(it => it.Group(It.IsAny<string>()), Times.Never());
            clientMock
                .Verify(it => it.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()), Times.Never());
        }

        private bool validateInsertAsync<T>(MessageInfo actual, IEnumerable<SendMessage<T>> messages)
            where T : MessageContent
        {
            actual.Should().NotBeNull();
            return messages.Any(it =>
                it.Nonce == actual.Nonce
                && it.TargetGroups.Distinct().SequenceEqual(actual.TargetGroups)
                && it.Filter.Scopes.Distinct().SequenceEqual(actual.Filter.Scopes)
                && it.Filter.Activities.Distinct().SequenceEqual(actual.Filter.Activities)
                && it.Content == actual.Content);
        }

        private bool validateSyncCoreAsyncParam<T>(object[] args, IEnumerable<SendMessage<T>> messages, long currentTime)
            where T : MessageContent
        {
            args.Should().HaveCount(2);
            args.First().Should().Be(currentTime);
            return messages.Any(it => it.Filter == args.Last());
        }

        #endregion
    }
}
