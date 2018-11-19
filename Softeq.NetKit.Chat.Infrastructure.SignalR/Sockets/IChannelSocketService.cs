﻿// Developed by Softeq Development Corporation
// http://www.softeq.com

using System.Threading.Tasks;
using Softeq.NetKit.Chat.Domain.Channel.TransportModels.Request;
using Softeq.NetKit.Chat.Domain.Channel.TransportModels.Response;

namespace Softeq.NetKit.Chat.Infrastructure.SignalR.Sockets
{
    public interface IChannelSocketService
    {
        Task<ChannelSummaryResponse> CreateChannelAsync(CreateChannelRequest createChannelRequest);
        Task<ChannelSummaryResponse> UpdateChannelAsync(UpdateChannelRequest request);
        Task CloseChannelAsync(ChannelRequest request);
        Task JoinToChannelAsync(JoinToChannelRequest request);
        Task LeaveChannelAsync(ChannelRequest request);
        Task<ChannelResponse> InviteMemberAsync(InviteMemberRequest request);
        Task InviteMembersAsync(InviteMembersRequest request);
        Task MuteChannelAsync(ChannelRequest request);
    }
}