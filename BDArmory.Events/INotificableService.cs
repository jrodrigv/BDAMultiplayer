using System;

namespace BDArmory.Events
{
    public interface INotificableService <T> where T : EventArgs
    {
        event EventHandler<T> OnActionExecuted;

        void PublishEvent(T t);
    }
}