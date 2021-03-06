﻿using Kahla.SDK.Abstract;
using Kahla.SDK.Events;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Kahla.Bot.Bots
{
    public class EchoBot : BotBase
    {
        public override Task OnBotInit()
        {
            var profilestring = JsonConvert.SerializeObject(Profile, Formatting.Indented);
            Console.WriteLine(profilestring);
            return Task.CompletedTask;
        }

        public override async Task OnMessage(string inputMessage, NewMessageEvent eventContext)
        {
            await Task.Delay(0);
            if (eventContext.Muted)
            {
                return;
            }
            if (eventContext.Message.SenderId == Profile.Id)
            {
                return;
            }
            var replaced = inputMessage
                    .Replace("吗", "")
                    .Replace('？', '！')
                    .Replace('?', '!');
            if (eventContext.Mentioned)
            {
                replaced += $" @{eventContext.Message.Sender.NickName.Replace(" ", "")}";
            }
            replaced.Replace($"@{Profile.NickName.Replace(" ", "")}", "");
            await Task.Delay(700);
            await SendMessage(replaced, eventContext.Message.ConversationId, eventContext.AESKey);
        }

        public override Task OnFriendRequest(NewFriendRequestEvent arg)
        {
            return CompleteRequest(arg.RequestId, true);
        }
    }
}
