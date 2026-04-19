using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.DependencyInjection.Options;

namespace MQTTnet.DependencyInjection
{
    internal class MqttClientLifetimeService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;
        private readonly MqttLifetimeOptions _mqttLifetimeOptions;
        private readonly Subscription[] _subscriptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttClientLifetimeService> _logger;
        private readonly CancellationTokenSource _lifetimeCts = new CancellationTokenSource();

        public MqttClientLifetimeService(
            IMqttClient mqttClient,
            IOptions<MqttClientOptionsBuilder> options,
            IEnumerable<Subscription> subscriptions,
            IServiceProvider serviceProvider,
            ILogger<MqttClientLifetimeService> logger,
            IOptions<MqttLifetimeOptions> mqttLifetimeOptions)
        {
            _mqttClient = mqttClient;
            _options = options.Value.Build();
            _subscriptions = subscriptions.ToArray();
            _serviceProvider = serviceProvider;
            _logger = logger;
            _mqttLifetimeOptions = mqttLifetimeOptions.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
            _mqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
            _mqttClient.ConnectingAsync += MqttClient_ConnectingAsync;
            _mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;

            try
            {
                await ReconnectAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _mqttClient.Dispose();
            }
        }

        private async Task ReconnectAsync(CancellationToken cancellationToken)
        {
            await _mqttClient.ConnectAsync(_options, cancellationToken).ConfigureAwait(false);

            for (uint subscriptionIndex = 0; subscriptionIndex < _subscriptions.Length; subscriptionIndex++)
            {
                var subscription = _subscriptions[subscriptionIndex];

                var sbubscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(subscription.Filter)
                    .WithSubscriptionIdentifier(subscriptionIndex + 1)
                    .Build();

                await _mqttClient.SubscribeAsync(sbubscribeOptions, cancellationToken);
            }
        }

        private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(_mqttLifetimeOptions.AutoReconnectDelay, _lifetimeCts.Token);
                    try
                    {
                        await ReconnectAsync(_lifetimeCts.Token);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Mqtt reconnect error");
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
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
                await consumer.Handle(message, _lifetimeCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Incoming message processing error");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _lifetimeCts.Cancel();

            _mqttClient.ApplicationMessageReceivedAsync -= MqttClient_ApplicationMessageReceivedAsync;
            _mqttClient.ConnectedAsync -= MqttClient_ConnectedAsync;
            _mqttClient.ConnectingAsync -= MqttClient_ConnectingAsync;
            _mqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;

            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            
            _mqttClient.Dispose();
        }
    }
}
