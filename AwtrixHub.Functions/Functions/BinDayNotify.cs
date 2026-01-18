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

            var Details = await GetNextBinDetails();

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

        private async Task<BinDetails> GetNextBinDetails()
        {
            var htmlResponse = await GetBinData();

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

        private static async Task<string> GetBinData()
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

    public class BinDetails
    {
        public DateTime Date { get; set; }
        public Colour Colour { get; set; }
        internal BinDetails(DateTime date, Colour colour)
        {
            Date = date;
            Colour = colour;
        }
    }

    public enum Colour
    {
        Black,
        Green
    }
}
