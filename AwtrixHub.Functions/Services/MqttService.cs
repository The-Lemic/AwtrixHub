using Microsoft.Extensions.Configuration;
using MQTTnet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AwtrixHub.Functions.Services
{
    public class MqttService : IMqttService
    {
        private string _brokerURL;
        private int _port;
        private string _username;
        private string _password;
        private bool _useTls;
        private string _topicPrefix;


        // Constructor

        public MqttService(IConfiguration configuration)
        {
            _brokerURL = configuration["MQTT:BrokerUrl"];
            _port = int.Parse(configuration["MQTT:Port"]);
            _username = configuration["MQTT:Username"];
            _password = configuration["MQTT:Password"];
            _ = bool.TryParse(configuration["MQTT:UseTls"], out _useTls);
            _topicPrefix = configuration["MQTT:TopicPrefix"];
        }

        public async Task PublishAsync(string topic, string payload)
        {
            var mqttFactory = new MqttClientFactory();

            using var mqttClient = mqttFactory.CreateMqttClient();


            var mqttClientTlsOptions = new MqttClientTlsOptionsBuilder()
                .UseTls(_useTls)
                .Build();

            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerURL, _port)
                .WithTlsOptions(mqttClientTlsOptions);

            if (!String.IsNullOrEmpty(_username) && !String.IsNullOrEmpty(_password))
                mqttClientOptionsBuilder.WithCredentials(_username, _password);

            var mqttClientOptions = mqttClientOptionsBuilder.Build();

            // This will throw an exception if the server is not available.
            // The result from this message returns additional data which was sent
            // from the server. Please refer to the MQTT protocol specification for details.
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");

            Console.WriteLine(response);

            var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(_topicPrefix + "/" + topic)
            .WithPayload(payload)
            .Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            await mqttClient.DisconnectAsync();

            Console.WriteLine("MQTT application message is published.");
        }
    }
}
