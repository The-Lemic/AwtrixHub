using AwtrixHub.Functions.DTOs;
using AwtrixHub.Functions.Enums;
using AwtrixHub.Functions.Services;
using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AwtrixHub.Functions.Functions
{
    public class BinDayNotify
    {
        private readonly IMqttService _mqttService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly string _uprn;
        private readonly string _apiUrl;

        public BinDayNotify(
            IMqttService mqttService,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<BinDayNotify>();
            _mqttService = mqttService;
            _httpClientFactory = httpClientFactory;
            _uprn = configuration["BinCollection:UPRN"]
                ?? throw new InvalidOperationException("BinCollection:UPRN configuration is required");
            _apiUrl = configuration["BinCollection:ApiUrl"]
                ?? throw new InvalidOperationException("BinCollection:ApiUrl configuration is required");
        }

        [Function("BinDayNotify")]
        public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
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

            // Check the council website to get the next bin day and the color
            var binDetails = await GetNextBinDetails();
            var message = CreateMessage(binDetails, DateTime.Now);
            
            // Send Bin Day Notification
            if (message != null)
            {
                // Call publish message with topic and message
                await _mqttService.PublishAsync("indicator2", JsonSerializer.Serialize(message));
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
            var captionNodes = htmlDoc.DocumentNode.SelectNodes("//p[@class='caption']");
            if (captionNodes == null || captionNodes.Count == 0)
                throw new InvalidOperationException("Could not find bin collection date element in HTML response");

            var binCollectionDateString = captionNodes[0].InnerText
                .Replace("Next collection", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!DateTime.TryParse(binCollectionDateString, out var nextCollectionDate))
                throw new InvalidOperationException($"Could not parse bin collection date: '{binCollectionDateString}'");

            return nextCollectionDate;
        }

        private static Colour GetBinColour(HtmlDocument htmlDoc)
        {
            var headingNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='heading']");
            if (headingNodes == null || headingNodes.Count == 0)
                throw new InvalidOperationException("Could not find bin colour element in HTML response");

            var binColourElement = headingNodes[0].InnerText.Trim();

            return binColourElement switch
            {
                "Green Bin" => Colour.Green,
                "Grey Bin" => Colour.Black,
                _ => Colour.Black
            };
        }

        private async Task<string> GetBinCollectionDetailsHTML()
        {
            using var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_apiUrl),
                Content = new StringContent($"{{\"UPRN\": {_uprn}}}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
