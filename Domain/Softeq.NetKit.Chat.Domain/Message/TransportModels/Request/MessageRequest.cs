﻿// Developed by Softeq Development Corporation
// http://www.softeq.com

using System;
using Softeq.NetKit.Chat.Domain.Member.TransportModels.Request;
using Softeq.QueryUtils;

namespace Softeq.NetKit.Chat.Domain.Message.TransportModels.Request
{
    public class MessageRequest : UserRequest, IPagedQuery
    {
        public MessageRequest(string userId, Guid channelId, int page, int pageSize) : base(userId)
        {
            ChannelId = channelId;
            Page = page;
            PageSize = pageSize;
        }

        public Guid ChannelId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}