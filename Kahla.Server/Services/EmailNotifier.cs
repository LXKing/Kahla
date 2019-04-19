﻿using Aiursoft.Pylon.Services;
using Kahla.Server.Data;
using Kahla.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kahla.Server.Services
{
    public class EmailNotifier : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private IServiceScopeFactory _scopeFactory;

        public EmailNotifier(
            ILogger<EmailNotifier> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email notifier service is starting...");
            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            _logger.LogInformation("Cleaner task started!");
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<KahlaDbContext>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var emailSender = scope.ServiceProvider.GetRequiredService<AiurEmailSender>();
                var users = await dbContext
                                .Users
                                .Where(t => t.EmailConfirmed)
                                .Where(t => t.EnableEmailNotification)
                                // Only for users who did not send email for a long time.
                                .Where(t => t.LastEmailHimTime + TimeSpan.FromHours(23) < DateTime.UtcNow)
                                .ToListAsync();
                foreach (var user in users)
                {
                    var emailMessage = await BuildEmail(user, dbContext, configuration);
                    if (string.IsNullOrWhiteSpace(emailMessage))
                    {
                        continue;
                    }
                    try
                    {
                        await emailSender.SendEmail(user.Email, "New notifications in Kahla", emailMessage);
                        user.LastEmailHimTime = DateTime.UtcNow;
                        dbContext.Update(user);
                    }
                    catch (SmtpException) { }
                }
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<string> BuildEmail(KahlaUser user, KahlaDbContext dbContext, IConfiguration configuration)
        {
            int totalUnread = 0, inConversations = 0;
            var conversations = await dbContext.MyConversations(user.Id);
            var msg = new StringBuilder();
            foreach (var conversation in conversations)
            {
                // Ignore conversations muted.
                if (conversation is GroupConversation currentGroup)
                {
                    var relation = currentGroup
                        .Users
                        .FirstOrDefault(t => t.UserId == user.Id);
                    if (relation.Muted)
                    {
                        continue;
                    }
                }
                var currentUnread = conversation.GetUnReadAmount(user.Id);

                if (currentUnread <= 0) continue;
                totalUnread += currentUnread;
                inConversations++;
                if (inConversations > 50) {
                    continue;
                }

                if (inConversations == 50)
                {
                    msg.AppendLine(
                        "<li>Some conversations haven't been displayed because there are too many items.</li>");
                    continue;
                }
                msg.AppendLine($"<li>{currentUnread} unread message(s) in {(conversation is GroupConversation ? "group" : "friend")} <a href=\"{configuration["AppDomain"]}/talking/{conversation.Id}\">{conversation.DisplayName}</a>.</li>");
            }
            var pendingRequests = await dbContext
                .Requests
                .AsNoTracking()
                .Where(t => t.TargetId == user.Id)
                .CountAsync(t => t.Completed == false);

            if (inConversations > 0 || pendingRequests > 0)
            {
                if (inConversations > 0) {
                    msg.Insert(0,
                        $"<h4>You have {totalUnread} unread message(s) in {inConversations} conversation(s) from your Kahla friends!<h4>\r\n<ul>\r\n");
                    msg.AppendLine("</ul>");
                }

                if (pendingRequests > 0) {
                    msg.AppendLine($"<h4>You have {pendingRequests} pending friend request(s) in Kahla.<h4>");
                }

                msg.AppendLine($"Click to <a href='{configuration["AppDomain"]}'>Open Kahla Now</a>.");
                return msg.ToString();
            }
            return string.Empty;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email notifier service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
