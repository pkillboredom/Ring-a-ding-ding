﻿using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Ring_a_ding_ding
{
    public class BlazorChatSampleHub : Hub
    {
        public const string HubUrl = "/chat";
        public async Task Broadcast(string username, string message) => await Clients.All.SendAsync("Broadcast", username, message);
        public async Task JoinGroup(string groupName) => await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception e)
        {
            Console.WriteLine($"Disconnected {e?.Message} {Context.ConnectionId}");
            await base.OnDisconnectedAsync(e);
        }
    }
}
