using System;

namespace UI.Dialogs
{
    public static class DialogNotificationManager
    {
        public static DialogsQueue DialogsQueue=>DialogCoordinator.Dialogs;
        public static DialogsQueue NotificationsQueue=>DialogCoordinator.Notifications;
        public static void EnqueueDialog(int priority,Action show)=>DialogsQueue.AddToQueue(priority,show);
        public static void EnqueueNotification(int priority,Action show)=>NotificationsQueue.AddToQueue(priority,show);
    }
}
