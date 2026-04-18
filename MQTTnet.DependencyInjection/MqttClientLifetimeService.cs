using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MQTTnet.DependencyInjection
{
    internal class MqttClientLifetimeService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;
        private readonly Subscription[] _subscriptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttClientLifetimeService> _logger;


        private bool _clientStarted = false;

        public MqttClientLifetimeService(IMqttClient mqttClient, IOptions<MqttClientOptionsBuilder> options, IEnumerable<Subscription> subscriptions, IServiceProvider serviceProvider, ILogger<MqttClientLifetimeService> logger)
        {
            _mqttClient = mqttClient;
            _options = options.Value.Build();
            _subscriptions = subscriptions.ToArray();
            _serviceProvider = serviceProvider;
            _logger = logger;
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

            for(uint subscriptionIndex = 0; subscriptionIndex < _subscriptions.Length; subscriptionIndex++)
            {
                var subscription = _subscriptions[subscriptionIndex];

                var sbubscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(subscription.Filter)
                    .WithSubscriptionIdentifier(subscriptionIndex + 1)
                    .Build();

                await _mqttClient.SubscribeAsync(sbubscribeOptions, cancellationToken);
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
            try
            {
                var message = arg.ApplicationMessage;
                var subsription = _subscriptions[message.SubscriptionIdentifiers.Min() - 1];
                var consumer = subsription.ConsumerFactory(_serviceProvider);
                await consumer.Handle(message, default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Incoming message processing error");
            }
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
