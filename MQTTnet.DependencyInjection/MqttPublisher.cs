namespace MQTTnet.DependencyInjection
{
    public class MqttPublisher : IMqttPublisher
    {
        private readonly IMqttClient _client;

        public MqttPublisher(IMqttClient client)
            => _client = client;

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
            => _client.PublishAsync(applicationMessage, cancellationToken);
    }
}
