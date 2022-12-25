﻿using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using TTSS.Infrastructure.Services.Models;
using TTSS.TestHelpers.XUnit;
using Xunit.Abstractions;

namespace TTSS.Infrastructure.Services.Tests
{
    public class MessagingCenterTests : XUnitTestBase
    {
        private IFixture fixture;
        private IMessagingCenter sut;
        private Mock<IRestService> restServiceMock;

        public MessagingCenterTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            restServiceMock = fixture.Freeze<Mock<IRestService>>();
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
            var data = fixture.Create<SendMessageResponse>();
            restServiceMock
                .Setup(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(It.IsAny<string>(), It.IsAny<IEnumerable<SendMessage>>()))
                .Returns<string, IEnumerable<SendMessage>>((url, req) => Task.FromResult(new RestResponse<SendMessageResponse>
                {
                    Data = data,
                    StatusCode = 200,
                    IsSuccessStatusCode = true,
                }));
            (await sut.Send(messages)).Should().BeEquivalentTo(data);
            restServiceMock.Verify(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(
                It.IsAny<string>(),
                It.Is<IEnumerable<SendMessage>>(actual => actual == messages)), Times.Once());
        }

        [Theory]
        [ClassData(typeof(InvalidDynamicMessages))]
        [ClassData(typeof(InvalidNotificationMessages))]
        public async Task Send_InvalidMessages_ThenThoseMessagesMustNotBeSend(IEnumerable<SendMessage> messages)
        {
            var rsp = await sut.Send(messages);
            rsp.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            restServiceMock.Verify(it => it.Post<IEnumerable<SendMessage>, SendMessageResponse>(
                It.IsAny<string>(),
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
                .Returns<string, IEnumerable<SendMessage>>((url, req) => Task.FromResult(response));
            var actual = await sut.Send(fixture.Create<IEnumerable<SendMessage<NotificationContent>>>());
            actual.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            actual.NonceStatus.Should().BeEmpty();
        }

        #endregion
    }
}
