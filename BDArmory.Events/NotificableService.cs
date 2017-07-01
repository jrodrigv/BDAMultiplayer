using System;

namespace BDArmory.Events
{
    public abstract class NotificableService : INotificableService 
    {
        public event EventHandler<EventArgs> OnActionExecuted;

        public void PublishEvent(EventArgs t)
        {
            OnActionExecuted?.Invoke(this, t);
        }
    }
}