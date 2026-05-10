using webclothes.Data;
using webclothes.Models;

namespace webclothes.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task NotifyUserAsync(string? userId, string title, string? message, string type = "info", string? linkUrl = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                LinkUrl = linkUrl,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task NotifyRoleAsync(string roleName, string title, string? message, string type = "info", string? linkUrl = null)
        {
            if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            _context.Notifications.Add(new Notification
            {
                TargetRole = roleName,
                Title = title,
                Message = message,
                Type = type,
                LinkUrl = linkUrl,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}
