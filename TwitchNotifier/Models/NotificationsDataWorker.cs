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
        public static async Task<bool> EditNotificationAsync(Notification notification)
        {           
            try
            {
                using ApplicationContext db = new();

                Notification? notification1 = new();
                await Task.Run(()=> notification1 = db.Notifications.ToList().Find(n => n.Id == notification.Id));
                if (notification1 == null)
                    return false;

                int index = db.Notifications.ToList().IndexOf(notification1);

                db.Notifications.ToList()[index].DiscordChannelId = notification.DiscordChannelId;
                db.Notifications.ToList()[index].HEXEmbedColor = notification.HEXEmbedColor;
                db.Notifications.ToList()[index].Message = notification.Message;
                db.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to edit a notification in the database.\nException: {ex}");
                return false;
            }
        }


        public static List<Notification>? GetNotifications()
        {
            List<Notification> notifications;
            using ApplicationContext db = new();
            try
            {
                notifications = db.Notifications.ToList();
                return notifications;
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to get notifications.\nException: {ex}");
                return null;
            }
        }
        public static async Task<List<Notification>?> GetNotificationsAsync()
        {
            List<Notification>? notifications = new();
            await Task.Run(() => notifications = GetNotifications());
            return notifications;
        }


        public static  List<Notification>? GetNotificationsIn(ulong guildId)
        {
            List<Notification> guildNotifications;
            using ApplicationContext db = new();
            try
            {
                guildNotifications = db.Notifications.ToList().FindAll(n => n.DiscordGuildId == guildId);
                return guildNotifications;
            }
            catch (Exception ex)
            {
                ErrorMessageHelper.SendConsoleErrorMessage($"Something went wrong when trying to get notifications by guild id.\nException: {ex}");
                return null;
            }
        }
        public static async Task<List<Notification>?> GetNotificationsInAsync(ulong guildId)
        {
            List<Notification>? guildNotifications = new();
            await Task.Run(() => guildNotifications = GetNotificationsIn(guildId));
            return guildNotifications;
        }


        public static Notification? GetNotification(long id)
        {
            using ApplicationContext db = new();
            try
            {
                List<Notification> notifications;
                notifications = db.Notifications.ToList().FindAll(n => n.Id == id);

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
        public static async Task<Notification?> GetNotificationAsync(long id)
        {
            Notification? notification = new();
            await Task.Run(() => notification = GetNotification(id));
            return notification;
        }
    }
}
