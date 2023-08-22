using TwitchNotifier.Db;
using TwitchNotifier.Helpers;

namespace TwitchNotifier.Models
{
    internal class NotificationsDataWorker
    {
        public static async Task<bool> AddNotificationAsync(Notification notification)
        {
            if (notification == null)
                return false;

            using ApplicationContext db = new();
            try
            {
                await db.Notifications.AddAsync(notification);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to add notification to the database.\nException: {ex}");
                return false;
            }
        }

        public static async Task<bool> RemoveNotificationAsync(Notification notification)
        {
            if (notification == null)
                return false;

            using ApplicationContext db = new();
            try
            {
                await Task.Run(() => db.Notifications.Remove(notification));
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to remove notification from the database.\nException: {ex}");

                return false;
            }
        }

        public static async Task<List<Notification>?> GetNotificationsAsync()
        {
            List<Notification> notifications = new();
            using ApplicationContext db = new();
            try
            {
                await Task.Run(() => notifications = db.Notifications.ToList());
                return notifications;
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to get notifications.\nException: {ex}");
                return null;
            }
        }

        public static async Task<List<Notification>?> GetNotificationsAsync(ulong guildId)
        {
            List<Notification> guildNotifications = new();
            using ApplicationContext db = new();
            try
            {
                await Task.Run(() => guildNotifications = db.Notifications.ToList().FindAll(n => n.DiscordGuildId == guildId));

                return guildNotifications;
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to get notifications by guild id.\nException: {ex}");
                return null;
            }
        }

        public static async Task<Notification?> GetNotificationAsync(long id)
        {
            using ApplicationContext db = new();
            try
            {
                List<Notification> notifications = new();
                await Task.Run(() => notifications = db.Notifications.ToList().FindAll(n => n.Id == id));
                if (notifications.Count == 0)
                    return null;

                return notifications[0];
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to get notification from the database.\nException: {ex}");
                return null;
            }
        }
    }
}
