namespace MQTTnet.DependencyInjection
{
    public interface IMqttConsumer
    {
        Task Handle(MqttApplicationMessage message, CancellationToken cancellationToken);
    }
}
