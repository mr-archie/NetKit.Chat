﻿// Developed by Softeq Development Corporation
// http://www.softeq.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Softeq.NetKit.Chat.Data.Interfaces.Repository;
using Softeq.NetKit.Chat.Data.Repositories.Infrastructure;
using Softeq.NetKit.Chat.Domain.Attachment;

namespace Softeq.NetKit.Chat.Data.Repositories.Repositories
{
    internal class AttachmentRepository : IAttachmentRepository
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public AttachmentRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }

        public async Task AddAttachmentAsync(Attachment attachment)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"
                    INSERT INTO Attachments(Id, ContentType, Created, FileName, MessageId, Size) 
                    VALUES (@Id, @ContentType, @Created, @FileName, @MessageId, @Size);";

                await connection.ExecuteScalarAsync(sqlQuery, attachment);      
            }
        }

        public async Task DeleteAttachmentAsync(Guid attachmentId)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"DELETE FROM Attachments WHERE Id = @attachmentId";
                
                await connection.ExecuteScalarAsync<Attachment>(sqlQuery, new { attachmentId });
            }
        }

        public async Task<Attachment> GetAttachmentByIdAsync(Guid attachmentId)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"
                    SELECT Id, ContentType, Created, FileName, MessageId, Size
                    FROM Attachments
                    WHERE Id = @attachmentId";

                var data = (await connection.QueryAsync<Attachment>(sqlQuery, new { attachmentId }))
                    .FirstOrDefault();

                return data;
            }
        }

        public async Task<List<Attachment>> GetMessageAttachmentsAsync(Guid messageId)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"
                     SELECT Id, ContentType, Created, FileName, MessageId, Size
                    FROM Attachments
                    WHERE MessageId = @messageId";

                var data = (await connection.QueryAsync<Attachment>(sqlQuery, new { messageId })).ToList();

                return data;
            }
        }

        public async Task DeleteMessageAttachmentsAsync(Guid messageId)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"DELETE FROM Attachments WHERE MessageId = @messageId";

                await connection.ExecuteScalarAsync<Attachment>(sqlQuery, new { messageId });
            }
        }
    }
}