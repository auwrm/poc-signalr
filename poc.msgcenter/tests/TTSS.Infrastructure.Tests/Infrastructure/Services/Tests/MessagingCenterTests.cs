using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using TTSS.Infrastructure.Services.Models;
using TTSS.Infrastructure.Services.Models.Configs;
using TTSS.TestHelpers.XUnit;
using Xunit.Abstractions;

namespace TTSS.Infrastructure.Services.Tests
{
    public class MessagingCenterTests : XUnitTestBase
    {
        private IFixture fixture;
        private IMessagingCenter sut;
        private Mock<IRestService> restServiceMock;
        private MessagingCenterOptions msgCenterOpt;
        private const string HostFQDN = "www.msgcenter.com";
        private string ExpectedHostUrl => $"https://{HostFQDN}/";

        public MessagingCenterTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            restServiceMock = fixture.Freeze<Mock<IRestService>>();
            msgCenterOpt = fixture.Freeze<MessagingCenterOptions>();
            msgCenterOpt.HostUrl = HostFQDN;
            sut = fixture.Create<MessagingCenter>();
        }

        #region Send

        [Theory, AutoData]
        public Task Send_DynamicContentWithAllDataValid_ThenAllMessagesShouldBeSuccess(SendMessage<DynamicContent> message)
            => validate_AllMessagesHaveBeenSent(new[] { message });

        [Theory, AutoData]
        public Task Send_NotificationContentWithAllDataValid_ThenAllMessagesShouldBeSuccess(SendMessage<NotificationContent> message)
            => validate_AllMessagesHaveBeenSent(new[] { message });

        [Theory, AutoData]
        public Task Send_DynamicContentsWithAllDataValid_ThenAllMessagesShouldBeSuccess(IEnumerable<SendMessage<DynamicContent>> messages)
            => validate_AllMessagesHaveBeenSent(messages);

        [Theory, AutoData]
        public Task Send_NotificationContentsWithAllDataValid_ThenAllMessagesShouldBeSuccess(IEnumerable<SendMessage<NotificationContent>> messages)
            => validate_AllMessagesHaveBeenSent(messages);

        private async Task validate_AllMessagesHaveBeenSent(IEnumerable<SendMessage> messages)
        {
            var result = fixture.Create<RestResponse<SendMessageResponse>>();
            restServiceMock
                .Setup(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(It.IsAny<string>(), It.IsAny<IEnumerable<SendMessage>>()))
                .Returns<string, IEnumerable<SendMessage>>((_, _) => Task.FromResult(result));
            (await sut.Send(messages)).Should().BeEquivalentTo(result.Data);
            restServiceMock.Verify(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(
                It.Is<string>(actual => actual == ExpectedHostUrl),
                It.Is<IEnumerable<SendMessage>>(actual => actual == messages)), Times.Exactly(1));
        }

        [Theory]
        [ClassData(typeof(InvalidDynamicMessages))]
        [ClassData(typeof(InvalidNotificationMessages))]
        public async Task Send_InvalidMessages_ThenThoseMessagesMustNotBeSend(IEnumerable<SendMessage> messages)
        {
            var rsp = await sut.Send(messages);
            rsp.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            restServiceMock.Verify(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(
                It.Is<string>(actual => actual == ExpectedHostUrl),
                It.IsAny<IEnumerable<SendMessage>>()), Times.Never());
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

        [Fact]
        public Task SendMessageFailed_WithResponse_ThenReceivedTheFailedResult()
            => validateFailedSendMessages(new RestResponse<SendMessageResponse>
            {
                Data = null,
                StatusCode = 401,
                IsSuccessStatusCode = false,
            });

        [Fact]
        public Task SendMessageFailed_WithoutResponse_ThenReceivedTheFailedResult()
            => validateFailedSendMessages(null);

        private async Task validateFailedSendMessages(RestResponse<SendMessageResponse> response)
        {
            restServiceMock
                .Setup(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(It.IsAny<string>(), It.IsAny<IEnumerable<SendMessage>>()))
                .Returns<string, IEnumerable<SendMessage>>((_, _) => Task.FromResult(response));
            var actual = await sut.Send(fixture.Create<IEnumerable<SendMessage<NotificationContent>>>());
            actual.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            actual.NonceStatus.Should().BeEmpty();
        }

        #endregion

        #region SyncMessage

        [Fact]
        public async Task SyncMessage_WithAllDataValid_ThenSystemCallSync()
        {
            var result = fixture.Create<RestResponse<MessagePack>>();
            restServiceMock
                .Setup(it => it.Get<MessagePack>(It.IsAny<string>()))
                .Returns<string>(_ => Task.FromResult(result));
            var actual = await sut.SyncMessage(new GetMessages
            {
                UserId = "u1",
                FromGroup = "g1",
                FromMessageId = 0,
                Filter = new()
                {
                    Scopes = new[] { "s1", "s2" },
                    Activities = new[] { "a1", "a2", "a3" },
                }
            });
            actual.Should().Be(result.Data);
            var expectedCallEndpoint = $"{ExpectedHostUrl}u1/g1/0?scopes=s1,s2&activities=a1,a2,a3";
            restServiceMock.Verify(it => it.Get<MessagePack>(It.Is<string>(actual => actual == expectedCallEndpoint)), Times.Exactly(1));
        }

        [Theory]
        [InlineData("s1,s2,s1,s3,s2", "a0", "s1,s2,s3", "a0")]
        [InlineData("s0", "a1,a2,a1,a3,a2", "s0", "a1,a2,a3")]
        public async Task SyncMessage_WithSomeDuplicatedValue_ThenSystemMustRemoveTheDuplicatedValue(string scopes, string activities, string expectedScope, string expectedActivity)
        {
            var result = fixture.Create<RestResponse<MessagePack>>();
            restServiceMock
                .Setup(it => it.Get<MessagePack>(It.IsAny<string>()))
                .Returns<string>(_ => Task.FromResult(result));

            var input = fixture.Create<GetMessages>();
            input.Filter.Scopes = scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            input.Filter.Activities = activities.Split(',', StringSplitOptions.RemoveEmptyEntries);
            (await sut.SyncMessage(input)).Should().Be(result.Data);
            restServiceMock.Verify(it => it.Get<MessagePack>(It.Is<string>(actual => actual.Contains(expectedScope) && actual.Contains(expectedActivity))), Times.Exactly(1));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("valid", "")]
        [InlineData("valid", " ")]
        [InlineData("valid", null)]
        [InlineData("valid", "valid", -1)]
        [InlineData("valid", "valid", 0, "")]
        [InlineData("valid", "valid", 0, " ")]
        [InlineData("valid", "valid", 0, null)]
        [InlineData("valid", "valid", 0, "valid", "")]
        [InlineData("valid", "valid", 0, "valid", " ")]
        [InlineData("valid", "valid", 0, "valid", null)]
        public Task SyncMessage_WithDataInvalid_ThenReceiveAnError(string userId = "valid", string group = "valid", long msgId = 0, string scope = "valid", string activity = "valid")
            => validateSyncMessageWithInvalidInput_ItMustNotCrashThisModule(new GetMessages
            {
                UserId = userId,
                FromGroup = group,
                FromMessageId = msgId,
                Filter = new()
                {
                    Scopes = new[] { scope },
                    Activities = new[] { activity },
                }
            });

        [Fact]
        public Task SyncMessage_WithParameterIsNull_ThenTheSystemMustNotCrash()
            => validateSyncMessageWithInvalidInput_ItMustNotCrashThisModule(null);

        private async Task validateSyncMessageWithInvalidInput_ItMustNotCrashThisModule(GetMessages param)
        {
            var actual = await sut.SyncMessage(param);
            actual.Messages.Should().BeEmpty();
            actual.HasMorePages.Should().BeFalse();
            actual.LastMessageId.Should().Be(0);
            restServiceMock.Verify(it => it.Get<MessagePack>(It.IsAny<string>()), Times.Never());
        }

        #endregion

        #region GetMoreMessages

        [Fact]
        public async Task GetMoreMessages_AllDataValid_ThenCallGetMore()
        {
            var req = fixture.Create<GetMessages>();
            var result = fixture.Create<RestResponse<MessagePack>>();
            restServiceMock
                .Setup(it => it.Get<MessagePack>(It.IsAny<string>()))
                .Returns<string>(_ => Task.FromResult(result));
            var actual = await sut.GetMoreMessages(new GetMessages
            {
                UserId = "u1",
                FromGroup = "g1",
                FromMessageId = 0,
                Filter = new()
                {
                    Scopes = new[] { "s1", "s2" },
                    Activities = new[] { "a1", "a2", "a3" },
                }
            });
            actual.Should().BeEquivalentTo(result.Data);

            var expectedCallEndpoint = $"{ExpectedHostUrl}u1/g1/more/0?scopes=s1,s2&activities=a1,a2,a3";
            restServiceMock.Verify(it => it.Get<MessagePack>(It.Is<string>(actual => actual == expectedCallEndpoint)), Times.Exactly(1));
        }

        [Theory]
        [InlineData("s1,s2,s1,s3,s2", "a0", "s1,s2,s3", "a0")]
        [InlineData("s0", "a1,a2,a1,a3,a2", "s0", "a1,a2,a3")]
        public async Task GetMoreMessages_WithSomeDuplicatedValue_ThenSystemMustRemoveTheDuplicatedValue(string scopes, string activities, string expectedScope, string expectedActivity)
        {
            var result = fixture.Create<RestResponse<MessagePack>>();
            restServiceMock
                .Setup(it => it.Get<MessagePack>(It.IsAny<string>()))
                .Returns<string>(_ => Task.FromResult(result));

            var input = fixture.Create<GetMessages>();
            input.Filter.Scopes = scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            input.Filter.Activities = activities.Split(',', StringSplitOptions.RemoveEmptyEntries);
            (await sut.GetMoreMessages(input)).Should().Be(result.Data);
            restServiceMock.Verify(it => it.Get<MessagePack>(It.Is<string>(actual => actual.Contains(expectedScope) && actual.Contains(expectedActivity))), Times.Exactly(1));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("valid", "")]
        [InlineData("valid", " ")]
        [InlineData("valid", null)]
        [InlineData("valid", "valid", -1)]
        [InlineData("valid", "valid", 0, "")]
        [InlineData("valid", "valid", 0, " ")]
        [InlineData("valid", "valid", 0, null)]
        [InlineData("valid", "valid", 0, "valid", "")]
        [InlineData("valid", "valid", 0, "valid", " ")]
        [InlineData("valid", "valid", 0, "valid", null)]
        public Task GetMoreMessages_WithDataInvalid_ThenReceiveAnError(string userId = "valid", string group = "valid", long msgId = 0, string scope = "valid", string activity = "valid")
            => validateGetMoreMessagesWithInvalidInput_ItMustNotCrashThisModule(new GetMessages
            {
                UserId = userId,
                FromGroup = group,
                FromMessageId = msgId,
                Filter = new()
                {
                    Scopes = new[] { scope },
                    Activities = new[] { activity },
                }
            });

        [Fact]
        public Task GetMoreMessages_WithParameterIsNull_ThenTheSystemMustNotCrash()
            => validateGetMoreMessagesWithInvalidInput_ItMustNotCrashThisModule(null);

        private async Task validateGetMoreMessagesWithInvalidInput_ItMustNotCrashThisModule(GetMessages param)
        {
            var actual = await sut.GetMoreMessages(param);
            actual.Messages.Should().BeEmpty();
            actual.HasMorePages.Should().BeFalse();
            actual.LastMessageId.Should().Be(0);
            restServiceMock.Verify(it => it.Get<MessagePack>(It.IsAny<string>()), Times.Never());
        }

        #endregion

        #region UpdateMessageTracker

        [Fact]
        public async Task UpdateMessageTracker_AllDataValid_ThenCallTheApi()
        {
            var req = fixture.Create<UpdateMessageTracker>();
            var result = fixture.Create<RestResponse<bool>>();
            restServiceMock
                .Setup(it => it.Put(It.IsAny<string>()))
                .Returns<string>(_ => Task.CompletedTask);
            var actual = await sut.UpdateMessageTracker(new UpdateMessageTracker
            {
                UserId = "u1",
                FromMessageId = 10,
                ThruMessageId = 30,
            });
            actual.Should().BeTrue();

            var expectedCallEndpoint = $"{ExpectedHostUrl}u1?from=10&thru=30";
            restServiceMock.Verify(it => it.Put(It.Is<string>(actual => actual == expectedCallEndpoint)), Times.Exactly(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("valid", -1)]
        [InlineData("valid", 0, -1)]
        public async Task UpdateMessageTracker_DataInvalid_ThenDoNothing(string userId, long fromMsgId = 0, long thruMsgId = 0)
        {
            var req = fixture.Create<UpdateMessageTracker>();
            var result = fixture.Create<RestResponse<bool>>();
            restServiceMock
                .Setup(it => it.Put(It.IsAny<string>()))
                .Returns<string>(_ => Task.CompletedTask);
            var actual = await sut.UpdateMessageTracker(new UpdateMessageTracker
            {
                UserId = userId,
                FromMessageId = fromMsgId,
                ThruMessageId = thruMsgId,
            });
            actual.Should().BeFalse();

            restServiceMock.Verify(it => it.Put(It.IsAny<string>()), Times.Never());
        }

        #endregion
    }
}
