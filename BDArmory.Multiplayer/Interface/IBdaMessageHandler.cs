namespace BDArmory.Multiplayer.Interface
{
    internal interface IBdaMessageHandler<in T> where T : class, new()
    {
        bool ProcessMessage(T message);
    }
}