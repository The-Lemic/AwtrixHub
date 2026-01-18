using AwtrixHub.Functions.DTOs;
using AwtrixHub.Functions.Enums;
using AwtrixHub.Functions.Services;
using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AwtrixHub.Functions.Functions
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
        public async Task Run([TimerTrigger("0 25 22 * * *")] TimerInfo myTimer)
        {
            // Log that the function has started
            _logger.LogInformation("C# Timer trigger function executed at: {Time}", DateTime.Now);

            // if we are running a timer
            if (myTimer.ScheduleStatus is not null)
            {
                // log when next call will be
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Next timer schedule at: {Next}", myTimer.ScheduleStatus.Next);
                }
            }

            // Step 1

            // Check the council website to get the next bin day and the color

            var binDetails = await GetNextBinDetails();

            var message = CreateMessage(binDetails, DateTime.Now);
            // Send Bin Day Notification

            if (message != null)
            {
                // Call publish message with topic and message
                await mqttService.PublishAsync("indicator2", JsonSerializer.Serialize(message));
            }
        }

        /// <summary>
        /// This is the actual buissness logic for if we would like to send a notification
        /// </summary>
        /// <param name="binDetails"></param>
        public static IndicatorDTO CreateMessage(BinDetails binDetails, DateTime now)
        {
            // if bin day is today
            if (binDetails != null)
            {
                // If bin colleciton date is within 2 days
                if (binDetails.Date - now.Date >= TimeSpan.FromDays(1))
                {
                    // Return Notification
                    return new IndicatorDTO()
                    {
                        IndicatorNumber = 2,
                        Color = [0, 100, 0],
                        Blink = 550
                    };
                }
                else if (binDetails.Date - now.Date == TimeSpan.FromDays(-1)) {
                    // Return Clear Notification
                    return new IndicatorDTO()
                    {
                        IndicatorNumber = 2,
                        Color = [0, 0, 0]
                    };
                }
            }

            return null;
        }

        private async Task<BinDetails> GetNextBinDetails()
        {
            var htmlResponse = await GetBinCollectionDetailsHTML();

            return ParseHtml(htmlResponse);
        }

        public BinDetails ParseHtml(string htmlResponse)
        {

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlResponse);

            DateTime nextCollectionDate = ExtractNextBinCollectionDate(htmlDoc);
            Colour colour = GetBinColour(htmlDoc);

            return new BinDetails(nextCollectionDate, colour);
        }

        private static DateTime ExtractNextBinCollectionDate(HtmlDocument htmlDoc)
        {
            var binCollectionDateString = htmlDoc.DocumentNode.SelectNodes("//p[@class='caption']")[0].InnerText.Trim("Next collection");
            DateTime nextCollectionDate = DateTime.Parse(binCollectionDateString);
            return nextCollectionDate;
        }

        private static Colour GetBinColour(HtmlDocument htmlDoc)
        {
            htmlDoc.DocumentNode.SelectNodes("//div[@class='heading']");
            var binColourElement = htmlDoc.DocumentNode.SelectNodes("//div[@class='heading']").First().InnerText.Trim();

            Colour colour;
            if (binColourElement == "Green Bin")
                colour = Colour.Green;
            else
                colour = Colour.Black;
            return colour;
        }

        private static async Task<string> GetBinCollectionDetailsHTML()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://bincollections.bromsgrove.gov.uk/BinCollections/Details"),
                Content = new StringContent("{\"UPRN\": 10000214236}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
