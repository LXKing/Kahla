﻿using Aiursoft.Pylon.Interfaces;
using Aiursoft.Pylon.Models.Stargate.ChannelViewModels;
using Aiursoft.Pylon.Services;
using Aiursoft.Pylon.Services.ToStargateServer;
using Kahla.SDK.Events;
using Kahla.SDK.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kahla.Server.Services
{
    public class KahlaPushService : IScopedDependency
    {
        private readonly PushMessageService _stargatePushService;
        private readonly AppsContainer _appsContainer;
        private readonly ChannelService _channelService;
        private readonly ThirdPartyPushService _thirdPartyPushService;

        public KahlaPushService(
            PushMessageService stargatePushService,
            AppsContainer appsContainer,
            ChannelService channelService,
            ThirdPartyPushService thirdPartyPushService)
        {
            _stargatePushService = stargatePushService;
            _appsContainer = appsContainer;
            _channelService = channelService;
            _thirdPartyPushService = thirdPartyPushService;
        }

        public async Task<CreateChannelViewModel> ReCreateStargateChannel(string userId)
        {
            var token = await _appsContainer.AccessToken();
            var channel = await _channelService.CreateChannelAsync(token, $"Kahla User Channel for Id: {userId}");
            return channel;
        }

        public async Task NewMessageEvent(int stargateChannel, IEnumerable<Device> devices, Conversation conversation, Message message, bool pushAlert, bool mentioned)
        {
            var token = await _appsContainer.AccessToken();
            var newMessageEvent = new NewMessageEvent
            {
                AESKey = conversation.AESKey,
                Muted = !pushAlert,
                Mentioned = mentioned,
                Message = message
            };
            var pushTasks = new List<Task>();
            if (stargateChannel != -1)
            {
                pushTasks.Add(_stargatePushService.PushMessageAsync(token, stargateChannel, JsonConvert.SerializeObject(newMessageEvent), true));
            }
            if (pushAlert)
            {
                pushTasks.Add(_thirdPartyPushService.PushAsync(devices, message.Sender.Email, JsonConvert.SerializeObject(newMessageEvent)));
            }
            await Task.WhenAll(pushTasks);
        }

        public async Task NewFriendRequestEvent(int stargateChannel, IEnumerable<Device> devices, KahlaUser requester, int requestId)
        {
            var token = await _appsContainer.AccessToken();
            var newFriendRequestEvent = new NewFriendRequestEvent
            {
                RequesterId = requester.Id,
                Requester = requester,
                RequestId = requestId
            };
            if (stargateChannel != -1)
            {
                await _stargatePushService.PushMessageAsync(token, stargateChannel, JsonConvert.SerializeObject(newFriendRequestEvent), true);
            }
            await _thirdPartyPushService.PushAsync(devices, requester.Email, JsonConvert.SerializeObject(newFriendRequestEvent));
        }

        public async Task WereDeletedEvent(int stargateChannel, IEnumerable<Device> devices, KahlaUser trigger)
        {
            var token = await _appsContainer.AccessToken();
            var wereDeletedEvent = new WereDeletedEvent
            {
                Trigger = trigger
            };
            if (stargateChannel != -1)
            {
                await _stargatePushService.PushMessageAsync(token, stargateChannel, JsonConvert.SerializeObject(wereDeletedEvent), true);
            }
            await _thirdPartyPushService.PushAsync(devices, "postermaster@aiursoft.com", JsonConvert.SerializeObject(wereDeletedEvent));
        }

        public async Task FriendAcceptedEvent(int stargateChannel, IEnumerable<Device> devices, KahlaUser accepter)
        {
            var token = await _appsContainer.AccessToken();
            var friendAcceptedEvent = new FriendAcceptedEvent
            {
                Target = accepter
            };
            if (stargateChannel != -1)
            {
                await _stargatePushService.PushMessageAsync(token, stargateChannel, JsonConvert.SerializeObject(friendAcceptedEvent), true);
            }
            await _thirdPartyPushService.PushAsync(devices, "postermaster@aiursoft.com", JsonConvert.SerializeObject(friendAcceptedEvent));
        }

        public async Task TimerUpdatedEvent(KahlaUser receiver, int newTimer, int conversationId)
        {
            var token = await _appsContainer.AccessToken();
            var channel = receiver.CurrentChannel;
            var timerUpdatedEvent = new TimerUpdatedEvent
            {
                NewTimer = newTimer,
                ConversationId = conversationId
            };
            if (channel != -1)
            {
                await _stargatePushService.PushMessageAsync(token, channel, JsonConvert.SerializeObject(timerUpdatedEvent), true);
            }
        }

        public async Task NewMemberEvent(KahlaUser receiver, KahlaUser newMember, int conversationId)
        {
            var token = await _appsContainer.AccessToken();
            var channel = receiver.CurrentChannel;
            var newMemberEvent = new NewMemberEvent
            {
                NewMember = newMember,
                ConversationId = conversationId
            };
            if (channel != -1)
            {
                await _stargatePushService.PushMessageAsync(token, channel, JsonConvert.SerializeObject(newMemberEvent), true);
            }
        }

        public async Task SomeoneLeftEvent(KahlaUser receiver, KahlaUser leftMember, int conversationId)
        {
            var token = await _appsContainer.AccessToken();
            var channel = receiver.CurrentChannel;
            var someoneLeftEvent = new SomeoneLeftEvent
            {
                LeftUser = leftMember,
                ConversationId = conversationId
            };
            if (channel != -1)
            {
                await _stargatePushService.PushMessageAsync(token, channel, JsonConvert.SerializeObject(someoneLeftEvent), true);
            }
        }

        public async Task DissolveEvent(KahlaUser receiver, int conversationId)
        {
            var token = await _appsContainer.AccessToken();
            var channel = receiver.CurrentChannel;
            var dissolvevent = new DissolveEvent
            {
                ConversationId = conversationId
            };

            if (channel != -1)
            {
                await _stargatePushService.PushMessageAsync(token, channel, JsonConvert.SerializeObject(dissolvevent), true);
            }
        }
    }
}
