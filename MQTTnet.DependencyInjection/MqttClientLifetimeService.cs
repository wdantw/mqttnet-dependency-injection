using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet.Extensions.ManagedClient;

namespace MQTTnet.DependencyInjection
{
    internal class MqttClientLifetimeService : IHostedService
    {
        private readonly IManagedMqttClient _mqttClient;
        private readonly IOptions<ManagedMqttClientOptionsBuilder> _options;

        private bool _clientStarted = false;

        public MqttClientLifetimeService(IManagedMqttClient mqttClient, IOptions<ManagedMqttClientOptionsBuilder> options)
        {
            _mqttClient = mqttClient;
            _options = options;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var _ = cancellationToken.Register(() => _mqttClient.Dispose());
            await _mqttClient.StartAsync(_options.Value.Build()).ConfigureAwait(false);
            _clientStarted = true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_clientStarted)
                return;

            using var _ = cancellationToken.Register(() => _mqttClient.Dispose());
            await _mqttClient.StopAsync().ConfigureAwait(false);
        }
    }
}
