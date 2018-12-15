using System;

namespace BDArmory.Events
{
    public abstract class NotificableService <T> where T : EventArgs
    {
        public event EventHandler OnActionExecuted;

        public void PublishEvent(T t)
        {
            OnActionExecuted?.Invoke(this, t);
        }
    }
}