﻿// Developed by Softeq Development Corporation
// http://www.softeq.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Softeq.NetKit.Chat.Domain.Channel;
using Softeq.NetKit.Chat.Domain.Channel.TransportModels.Request;
using Softeq.NetKit.Chat.Domain.Member;
using Softeq.NetKit.Chat.Domain.Member.TransportModels.Response;
using Softeq.NetKit.Chat.Domain.Message;
using Softeq.NetKit.Chat.Domain.Message.TransportModels.Request;
using Softeq.NetKit.Chat.Domain.Message.TransportModels.Response;
using Softeq.NetKit.Chat.Domain.Services.Exceptions;
using Softeq.NetKit.Chat.Infrastructure.SignalR.Hubs.Notifications;
using Softeq.NetKit.Chat.Infrastructure.SignalR.Resources;
using Softeq.Serilog.Extension;

namespace Softeq.NetKit.Chat.Infrastructure.SignalR.Sockets
{
    internal class MessageSocketService : IMessageSocketService
    {
        private readonly IChannelService _channelService;
        private readonly IMemberService _memberService;
        private readonly IMessageService _messageService;
        private readonly ILogger _logger;
        private readonly IMessageNotificationService _messageNotificationService;

        public MessageSocketService(
            IChannelService channelService,
            ILogger logger,
            IMemberService memberService,
            IMessageService messageService,
            IMessageNotificationService messageNotificationService)
        {
            _channelService = channelService;
            _logger = logger;
            _memberService = memberService;
            _messageService = messageService;
            _messageNotificationService = messageNotificationService;
        }

        public async Task<MessageResponse> AddMessageAsync(CreateMessageRequest createMessageRequest)
        {
            var channel = await _channelService.GetChannelByIdAsync(new ChannelRequest(createMessageRequest.SaasUserId, createMessageRequest.ChannelId));
            var member = await _memberService.GetMemberSummaryBySaasUserIdAsync(createMessageRequest.SaasUserId);

            if (string.IsNullOrEmpty(createMessageRequest.Body))
            {
                throw new Exception(string.Format(LanguageResources.Msg_MessageRequired, channel.Name));
            }

            var message = await _messageService.CreateMessageAsync(createMessageRequest);

            await _messageNotificationService.OnAddMessage(member, message, createMessageRequest.ClientConnectionId);

            return message;
        }

        public async Task DeleteMessageAsync(DeleteMessageRequest request)
        {
            try
            {
                var member = await _memberService.GetMemberSummaryBySaasUserIdAsync(request.SaasUserId);
                var message = await _messageService.GetMessageByIdAsync(request.MessageId);
                if (message.Sender.Id != member.Id)
                {
                    throw new Exception(string.Format(LanguageResources.Msg_AccessPermission, message.Id));
                }
                await _messageService.DeleteMessageAsync(request);

                await _messageNotificationService.OnDeleteMessage(member, message);
            }
            catch (NotFoundException ex)
            {
                if (ex.Errors.Any(x => x.Description == "Message does not exist."))
                {
                    _logger.Event("MessageDoesNotExist").With.Message("{@MessageId}", request.MessageId).Exception(ex).AsError();
                    throw new Exception(string.Format(LanguageResources.Msg_NotFound, request.MessageId));
                }
            }
        }

        public async Task UpdateMessageAsync(UpdateMessageRequest request)
        {
            var member = await _memberService.GetMemberSummaryBySaasUserIdAsync(request.SaasUserId);
            var message = await _messageService.GetMessageByIdAsync(request.MessageId);
            if (message.Sender.Id != member.Id)
            {
                throw new Exception(string.Format(LanguageResources.Msg_AccessPermission, message.Id));
            }
            if (string.IsNullOrEmpty(request.Body))
            {
                throw new Exception(LanguageResources.Msg_MessageRequired);
            }

            var updatedMessage = await _messageService.UpdateMessageAsync(request);

            await _messageNotificationService.OnUpdateMessage(member, updatedMessage);
        }

        public async Task AddMessageAttachmentAsync(AddMessageAttachmentRequest request)
        {
            try
            {
                var member = await _memberService.GetMemberSummaryBySaasUserIdAsync(request.SaasUserId);
                var message = await _messageService.GetMessageByIdAsync(request.MessageId);
                if (message.Sender.Id != member.Id)
                {
                    throw new Exception(string.Format(LanguageResources.Msg_AccessPermission, message.Id));
                }

                var attachmentsCount = await _messageService.GetMessageAttachmentsCount(message.Id);
                if (attachmentsCount == 10)
                {
                    throw new Exception(LanguageResources.Msg_LimitedAttachments);
                }

                await _messageService.AddMessageAttachmentAsync(request);

                await _messageNotificationService.OnAddMessageAttachment(member, message);
            }
            catch (ServiceException ex)
            {
                if (ex.Errors.Any(x => x.Description == "Message does not exist."))
                {
                    _logger.Event("MessageDoesNotExist").With.Message("{@MessageId}", request.MessageId).Exception(ex).AsError();
                    throw new Exception(string.Format(LanguageResources.Msg_NotFound, request.MessageId));
                }
            }
        }

        public async Task DeleteMessageAttachmentAsync(DeleteMessageAttachmentRequest request)
        {
            try
            {
                var member = await _memberService.GetMemberSummaryBySaasUserIdAsync(request.SaasUserId);
                var message = await _messageService.GetMessageByIdAsync(request.MessageId);
                if (message.Sender.Id != member.Id)
                {
                    throw new Exception(string.Format(LanguageResources.Msg_AccessPermission, message.Id));
                }

                await _messageService.DeleteMessageAttachmentAsync(request);

                await _messageNotificationService.OnDeleteMessageAttachment(member, message);
            }
            catch (ServiceException ex)
            {
                if (ex.Errors.Any(x => x.Description == "Message does not exist."))
                {
                    _logger.Event("MessageDoesNotExist").With.Message("{@MessageId}", request.MessageId).Exception(ex).AsError();
                    throw new Exception(string.Format(LanguageResources.Msg_NotFound, request.MessageId));
                }
                if (ex.Errors.Any(x => x.Description == "Attachment does not exist."))
                {
                    _logger.Event("AttachmentDoesNotExist").With.Message("{@AttachmentId}", request.AttachmentId).Exception(ex).AsError();
                    throw new Exception(string.Format(LanguageResources.Msg_AttachmentNotFound, request.AttachmentId));
                }
            }
        }

        public async Task AddLastReadMessageAsync(AddLastReadMessageRequest request)
        {
            try
            {
                var member = await _memberService.GetMemberSummaryBySaasUserIdAsync(request.SaasUserId);
                var message = await _messageService.GetMessageByIdAsync(request.MessageId);

                await _messageService.AddLastReadMessageAsync(request);

                var messageOwner = await _memberService.GetMemberByIdAsync(message.Sender.Id);

                var members = new List<MemberSummary>
                {
                    member,
                    messageOwner
                };

                await _messageNotificationService.OnChangeLastReadMessage(members, message);
            }
            catch (NotFoundException ex)
            {
                _logger.Event("MessageDoesNotExist").With.Message("{@MessageId}", request.MessageId).Exception(ex).AsError();
                throw new Exception(string.Format(LanguageResources.RoomMemberButNotExists, request.SaasUserId));
            }
        }
    }
}