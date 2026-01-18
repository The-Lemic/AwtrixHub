using NUnit.Framework;
using System.Threading.Tasks;
using AwtrixHub.Functions.Functions;
using AwtrixHub.Functions.Services;
using Microsoft.Extensions.Logging;


namespace AwtrixHub.Functions.Tests
{
    public class BinDayNotifyTests
    {
        private class FakeMqttService : IMqttService
        {
            public Task PublishAsync(string topic, string payload) => Task.CompletedTask;
        }

        [Test]
        public async Task ParseHTML_GreenBinNext_ReturnsGreenBinDate()
        {
            // Arrange
            string html = "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"utf-8\" />\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\r\n    <title>View Collections - My ASP.NET Application</title>\r\n    <link href=\"/Content/css?v=XxROPP_DgsURaOMCJakNXzJuemwUhFomQYZeFO_eV9I1\" rel=\"stylesheet\"/>\r\n\r\n    <script src=\"/bundles/modernizr?v=inCVuEFe6J4Q07A0AcRsbJic_UE5MwpRMNGcOtk94TE1\"></script>\r\n\r\n</head>\r\n<body>\r\n    <div>\r\n  \r\n        \r\n\r\n\r\n<div>\r\n    <h3>Bin collection dates for DY9 0GG</h3>\r\n</div>\r\n\r\n\r\n\r\n    <div class=\"collection-container\">\r\n        <div class=\"heading-container\">\r\n            <div class=\"left\">\r\n                <img class=\"image-container\" src=\"/Content/Images/Green.png\" alt=Green />\r\n            </div>\r\n            <div class=\"heading\">\r\n                Green Bin\r\n            </div>\r\n        </div>\r\n        <div class=\"collection-details-container\">\r\n            <p class=\"caption\">Next collection Thursday, 22 January 2026</p>\r\n            <p class=\"collection-details\">Your next collections on:</p>\r\n            <ul>\r\n                <li>Thursday, 05 February 2026</li>\r\n            </ul>\r\n        </div>\r\n    </div>\r\n    <div class=\"collection-container\">\r\n        <div class=\"heading-container\">\r\n            <div class=\"left\">\r\n                <img class=\"image-container\" src=\"/Content/Images/Grey.png\" alt=Grey />\r\n            </div>\r\n            <div class=\"heading\">\r\n                Grey Bin\r\n            </div>\r\n        </div>\r\n        <div class=\"collection-details-container\">\r\n            <p class=\"caption\">Next collection Thursday, 15 January 2026</p>\r\n            <p class=\"collection-details\">Your next collections on:</p>\r\n            <ul>\r\n                <li>Thursday, 29 January 2026</li>\r\n            </ul>\r\n        </div>\r\n    </div>\r\n\r\n<a href=\"http://e-services.worcestershire.gov.uk/MyLocalArea/MyLocalAreaResults.aspx?uprn=10000214236\" title=\"More information on your postcode area\" target=\"_blank\">More information on your postcode area</a>\r\n<br />\r\n<br />\r\n<a href=\"/\">Check Bin Dates For Different PostCode</a>\r\n\r\n    </div>\r\n\r\n    <script src=\"/bundles/jquery?v=Mardnt_0xoSJeIoTfaOWPwanQXeEpftRR57qSOBVpCg1\"></script>\r\n\r\n    <script src=\"/bundles/jqueryval?v=UxjNb1Shrqn9S1DqCOV4T4wVKXuTZKgdFSq4EV9tyvM1\"></script>\r\n\r\n\r\n    \r\n    \r\n</body>\r\n</html>\r\n"; // use real test HTML when available
            var mqtt = new FakeMqttService();
            using var loggerFactory = LoggerFactory.Create(builder => { });
            var sut = new BinDayNotify(mqtt, loggerFactory);

            // Act
            BinDetails result = sut.ParseHtml(html);

            // Assert
            Assert.That(result, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Colour, Is.EqualTo(Colour.Green));
                Assert.That(result.Date, Is.EqualTo(new DateTime(2026, 1, 22)));
            }
        }

        [Test]
        public async Task CreateMessage_GreenBinCollectionIsTwoDaysAgo_ReturnsNull()
        {

        }

        [Test]
        public async Task CreateMessage_GreenBinCollectionIsTomorrow_GreenBinNotificationCreated()
        {
            // Arrange
            BinDetails binDetails = new(new DateTime(2026, 1, 22), Colour.Green);

            DateTime now = new(2026, 1, 21, 12, 0, 0);

            // Act
            var result = BinDayNotify.CreateMessage(binDetails, now);


            // Act
        }

        public async Task CreateMessage_GreenBinCollectionIsToday_ReturnsNull()
        {
            // Arrange
            BinDetails binDetails = new(new DateTime(2026, 1, 22), Colour.Green);

            DateTime now = new(2026, 1, 21, 12, 0, 0);
        }

        public async Task CreateMessage_GreenBinCollectionWasYesterday_ReturnsNull()
        {

        }
    }
}