using AutoFixture;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Moq;

namespace TTSS.RealTimeUpdate.Services.Tests
{
    public class MessagingCenterHubTests
    {
        private IFixture fixture;
        private bool isHubClientNull = false;
        private Mock<IHubClients> hubClientsMock;
        private IMessagingCenterHub sut => fixture.Create<MessagingCenterHub>();

        public MessagingCenterHubTests()
        {
            fixture = new Fixture();
            fixture.Register<IHubClients>(() => isHubClientNull ? null : hubClientsMock.Object);
            hubClientsMock = fixture.Create<Mock<IHubClients>>();
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

        [Fact]
        public async Task RequestOTP_AllDataValid_ButTheHubClientsIsNull_ThenSystemMustNotCrash()
        {
            isHubClientNull = true;
            await validateInvalidParam_MustDoNothing(new InvocationContext { ConnectionId = "valid" });
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
    }
}
