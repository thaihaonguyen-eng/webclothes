namespace webclothes.Services
{
    public interface INotificationService
    {
        Task NotifyUserAsync(string? userId, string title, string? message, string type = "info", string? linkUrl = null);
        Task NotifyRoleAsync(string roleName, string title, string? message, string type = "info", string? linkUrl = null);
    }
}
