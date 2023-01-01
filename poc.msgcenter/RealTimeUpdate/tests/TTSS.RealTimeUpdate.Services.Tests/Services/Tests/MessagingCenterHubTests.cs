using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;
using TTSS.RealTimeUpdate.Services.Models;

namespace TTSS.RealTimeUpdate.Services.Tests
{
    public class MessagingCenterHubTests
    {
        private IFixture fixture;
        private Mock<IHubClients> hubClientsMock;
        private Mock<IGroupManager> groupManagerMock;
        private IMessagingCenterHub sut => fixture.Create<MessagingCenterHub>();

        public MessagingCenterHubTests()
        {
            fixture = new Fixture();
            fixture.Register<IHubClients>(() => hubClientsMock.Object);
            fixture.Register<IGroupManager>(() => groupManagerMock.Object);
            hubClientsMock = fixture.Create<Mock<IHubClients>>();
            groupManagerMock = fixture.Create<Mock<IGroupManager>>();
        }

        #region RequestOTP

        [Fact]
        public async Task RequestOTP_AllDataValid_ThenSendASecretCodeBackToTheCaller()
        {
            var clientMock = fixture.Create<Mock<ISingleClientProxy>>();
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
            var clientMock = fixture.Create<Mock<ISingleClientProxy>>();
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
            (await sut.JoinGroup(req)).Should().BeTrue();

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
            (await sut.JoinGroup(req)).Should().BeFalse();

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
            => validateJoinGroupWithInvalidParam(new JoinGroupRequest
            {
                Secret = secret,
                GroupName = groupName,
            });

        [Fact]
        public Task JoinGroup_WithParameterIsNull_ThenSystemDoNothing()
            => validateJoinGroupWithInvalidParam(null);

        private async Task validateJoinGroupWithInvalidParam(JoinGroupRequest req)
        {
            {
                var clientMock = fixture.Create<Mock<IClientProxy>>();
                hubClientsMock
                    .Setup(it => it.User(It.IsAny<string>()))
                    .Returns<string>(cid => clientMock.Object);

                (await sut.JoinGroup(req)).Should().BeFalse();

                hubClientsMock
                    .Verify(it => it.User(It.IsAny<string>()), Times.Never());
                groupManagerMock
                    .Verify(it => it.AddToGroupAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()), Times.Never());
            }
        }

        #endregion
    }
}
