using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MQTTnet.DependencyInjection
{
    internal class MqttClientLifetimeService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;
        private readonly Subscription[] _subscriptions;
        private readonly IMqttConsumer _consumer;

        private bool _clientStarted = false;

        public MqttClientLifetimeService(IMqttClient mqttClient, IOptions<MqttClientOptionsBuilder> options, IEnumerable<Subscription> subscriptions, IMqttConsumer consumer)
        {
            _mqttClient = mqttClient;
            _options = options.Value.Build();
            _subscriptions = subscriptions.ToArray();
            _consumer = consumer;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var _ = cancellationToken.Register(() => _mqttClient.Dispose());

            await _mqttClient.ConnectAsync(_options, cancellationToken).ConfigureAwait(false);

            _clientStarted = true;

            _mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
            _mqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
            _mqttClient.ConnectingAsync += MqttClient_ConnectingAsync;
            _mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;

            foreach(var subscription in _subscriptions)
            {
                var sbubscribeBuilder = new MqttClientSubscribeOptionsBuilder();
                sbubscribeBuilder.WithTopicFilter(subscription.Filter);
                await _mqttClient.SubscribeAsync(sbubscribeBuilder.Build(), cancellationToken);
            }
        }

        private Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private Task MqttClient_ConnectingAsync(MqttClientConnectingEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            await _consumer.Handle(arg.ApplicationMessage, default);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_clientStarted)
                return;

            _mqttClient.ApplicationMessageReceivedAsync -= MqttClient_ApplicationMessageReceivedAsync;
            _mqttClient.ConnectedAsync -= MqttClient_ConnectedAsync;
            _mqttClient.ConnectingAsync -= MqttClient_ConnectingAsync;
            _mqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;

            using var _ = cancellationToken.Register(() => _mqttClient.Dispose());
            
            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
