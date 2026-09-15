using System.Collections.Generic;

namespace BaseDotnet.Domain.Notifications
{
    public interface INotificationContext
    {
        bool HasNotifications { get; }

        IReadOnlyCollection<Notification> Notifications { get; }

        void AddNotification(string chave, string mensagem);
    }
}
