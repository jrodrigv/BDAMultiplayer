using System;

namespace BDArmory.Events
{
    public interface INotificableService
    {
        event EventHandler<EventArgs> OnActionExecuted;

        void PublishEvent(EventArgs t);
    }
}