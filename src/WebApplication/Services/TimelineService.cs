using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using WebApplication.Entities;
using WebApplication.Models.Timeline;

namespace WebApplication.Services
{
    public class TimelineService
    {
        private readonly DatabaseContext _databaseContext;
        private readonly UserService _userService;

        public TimelineService(DatabaseContext dbContext, UserService userService)
        {
            _databaseContext = dbContext;
            _userService = userService;
        }

        public async Task<List<Message>> GetMessagesForAnonymousUser(int resultsPerPage, CancellationToken ct)
        {
            var messages = await  _databaseContext.Messages
                .Include(message => message.Author)
                .Where(message => !message.IsFlagged)
                .OrderByDescending(message => message.PublishDate)
                .Take(resultsPerPage)
                .ToListAsync(ct);

            return messages;
        }
// TODO This should probably use a string as the others.. or at least this class should have uniform standards
        public async Task<List<Message>> GetMessagesForUser(User author, int resultsPerPage, CancellationToken ct)
        {
            var messages = await _databaseContext.Messages
                .Include(message => message.Author)
                .Where(message => message.IsFlagged == false)
                .Where(message => message.AuthorID == author.ID)
                .OrderByDescending(message => message.PublishDate)
                .Take(resultsPerPage)
                .ToListAsync(ct);

            return messages;
        }

        public async Task<List<Message>> GetFollowerMessagesForUser(string username, int resultsPerPage, CancellationToken ct)
        {
            var user = await _userService.GetUserFromUsername(username, ct);

            if (user == null)
            {
                return new List<Message>();
            }
            
            return await GetFollowerMessagesForUser(user.ID, resultsPerPage, ct);
        }

        public async Task<List<Message>> GetFollowerMessagesForUser(int userID, int resultsPerPage, CancellationToken ct)
        {
            var messages = await _databaseContext.Messages
                .Include(message => message.Author)
                .Where(message => !message.IsFlagged)
                .Where(message => _databaseContext.Followers
                    .Where(f => f.WhomID == userID)
                    .Select(f => f.WhoID)
                    .Contains(message.AuthorID))
                .OrderByDescending(message => message.PublishDate)
                .Take(resultsPerPage)
                .ToListAsync(ct);

            return messages;
        }

        public async Task CreateMessage(CreateMessageModel model, string username, CancellationToken ct)
        {
            var user = await _userService.GetUserFromUsername(username, ct);

            await CreateMessage(model, user.ID, ct);
        }
        
        public async Task CreateMessage(CreateMessageModel model, int userID, CancellationToken ct)
        {
            _databaseContext.Messages.Add(new Message
            {
                AuthorID = userID,
                Text = model.Content.Trim(),
                PublishDate = DateTimeOffset.Now,
                IsFlagged = false
            });

            await _databaseContext.SaveChangesAsync(ct);
        }
    }
}
