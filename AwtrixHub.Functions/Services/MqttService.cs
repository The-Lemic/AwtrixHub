using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AwtrixHub.Functions.Services
{
    public class MqttService : IMqttService
    {
        private readonly string _brokerURL;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _useTls;
        private readonly string _topicPrefix;
        private readonly ILogger<MqttService> _logger;

        public MqttService(IConfiguration configuration, ILogger<MqttService> logger)
        {
            _logger = logger;

            _brokerURL = configuration["MQTT:BrokerUrl"]
                ?? throw new InvalidOperationException("MQTT:BrokerUrl configuration is required");

            var portString = configuration["MQTT:Port"]
                ?? throw new InvalidOperationException("MQTT:Port configuration is required");
            if (!int.TryParse(portString, out _port) || _port < 1 || _port > 65535)
                throw new InvalidOperationException($"MQTT:Port must be a valid port number (1-65535), got: {portString}");

            _username = configuration["MQTT:Username"] ?? string.Empty;
            _password = configuration["MQTT:Password"] ?? string.Empty;
            _ = bool.TryParse(configuration["MQTT:UseTls"], out _useTls);

            _topicPrefix = configuration["MQTT:TopicPrefix"]
                ?? throw new InvalidOperationException("MQTT:TopicPrefix configuration is required");
        }

        public async Task PublishAsync(string topic, string payload)
        {
            var fullTopic = $"{_topicPrefix}/{topic}";
            _logger.LogDebug("Publishing to MQTT topic {Topic}", fullTopic);

            var mqttFactory = new MqttClientFactory();
            using var mqttClient = mqttFactory.CreateMqttClient();

            try
            {
                var mqttClientTlsOptions = new MqttClientTlsOptionsBuilder()
                    .UseTls(_useTls)
                    .Build();

                var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(_brokerURL, _port)
                    .WithTlsOptions(mqttClientTlsOptions);

                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                    mqttClientOptionsBuilder.WithCredentials(_username, _password);

                var mqttClientOptions = mqttClientOptionsBuilder.Build();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await mqttClient.ConnectAsync(mqttClientOptions, cts.Token);

                _logger.LogInformation("MQTT client connected to {Broker}:{Port}", _brokerURL, _port);

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(fullTopic)
                    .WithPayload(payload)
                    .Build();

                await mqttClient.PublishAsync(applicationMessage, cts.Token);
                _logger.LogInformation("MQTT message published to {Topic}", fullTopic);

                await mqttClient.DisconnectAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("MQTT operation timed out connecting to {Broker}:{Port}", _brokerURL, _port);
                throw new InvalidOperationException($"MQTT connection to {_brokerURL}:{_port} timed out after 30 seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish MQTT message to {Topic}", fullTopic);
                throw;
            }
        }
    }
}
