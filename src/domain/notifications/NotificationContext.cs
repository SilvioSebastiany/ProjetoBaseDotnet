using System.Collections.Generic;

namespace BaseDotNet.Domain.Notifications
{
    public class NotificationContext : INotificationContext
    {
        private readonly List<Notification> _notifications = new();

        public bool HasNotifications => _notifications.Count > 0;

        public IReadOnlyCollection<Notification> Notifications => _notifications;

        public void AddNotification(string chave, string mensagem)
        {
            _notifications.Add(new Notification(chave, mensagem));
        }
    }
}
