
namespace MQTTnet.DependencyInjection
{
    public interface IMqttPublisher
    {
        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken);
    }
}