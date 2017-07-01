namespace BDArmory.Multiplayer.Interface
{
    internal interface IBdaMessageHandler<in T> where T : class, new()
    {
        void ProcessMessage(T message);
    }
}