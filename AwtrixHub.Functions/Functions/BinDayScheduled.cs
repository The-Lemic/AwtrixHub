using AwtrixHub.Functions.Services;
using Azure;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Threading.Tasks;
using static Google.Protobuf.WellKnownTypes.Field.Types;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AwtrixHub
{
    public class BinDayNotify
    {
        private readonly IMqttService mqttService;

        private readonly ILogger _logger;

        public BinDayNotify(IMqttService mqttService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BinDayNotify>();
            this.mqttService = mqttService;
        }

        [Function("BinDayNotify")]
        public async Task Run([TimerTrigger("0 51 21 * * *")] TimerInfo myTimer)
        {
            // Log that the function has started
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // if we are running a timer
            if (myTimer.ScheduleStatus is not null)
            {
                // log when next call will be
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }

            // Step 1

            // Check the council website to get the next bin day and the color

            // if bin day is today

            // Send Bin Day Notification

            // Create the message to send
            var test = new
            {
              text = "Suck my fat one! 8===D",
              rainbow = true,
              duration = 10
            };



            // Call publish message with topic and message
            await mqttService.PublishAsync("notify", JsonSerializer.Serialize(test));
        }
    }
}
