namespace MQTTnet.DependencyInjection
{
    public interface ISubscriptionScope : IAsyncDisposable
    {
        IMqttConsumer CreateConsumer();
    }
}
