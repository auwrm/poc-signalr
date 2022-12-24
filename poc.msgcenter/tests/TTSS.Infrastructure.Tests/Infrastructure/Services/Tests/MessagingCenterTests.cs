using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Moq;
using TTSS.Infrastructure.Services.Models;
using TTSS.TestHelpers.XUnit;
using Xunit.Abstractions;

namespace TTSS.Infrastructure.Services.Tests
{
    public class MessagingCenterTests : XUnitTestBase
    {
        private IMessagingCenter sut;
        private Mock<IRestService> restServiceMock;

        public MessagingCenterTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            restServiceMock = fixture.Freeze<Mock<IRestService>>();
            sut = fixture.Create<MessagingCenter>();
        }

        [Theory, AutoData]
        public Task Send_DynamicContentWithAllDataValid_ThenAllMessageShouldBeSuccess(SendMessage<DynamicContent<SimpleModel>> message)
            => validateSendMessage(new[] { message });

        [Theory, AutoData]
        public Task Send_NotificationContentWithAllDataValid_ThenAllMessageShouldBeSuccess(SendMessage<NotificationContent> message)
            => validateSendMessage(new[] { message });

        [Theory, AutoData]
        public Task Send_DynamicContentsWithAllDataValid_ThenAllMessageShouldBeSuccess(IEnumerable<SendMessage<DynamicContent<SimpleModel>>> messages)
            => validateSendMessage(messages);

        [Theory, AutoData]
        public Task Send_NotificationContentsWithAllDataValid_ThenAllMessageShouldBeSuccess(IEnumerable<SendMessage<NotificationContent>> messages)
            => validateSendMessage(messages);

        private async Task validateSendMessage(IEnumerable<SendMessage> messages)
        {
            _ = await sut.Send(messages);
            restServiceMock.Verify(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SendMessage>>()), Times.Once());
        }
    }
}
