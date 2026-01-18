using AwtrixHub.Functions.Enums;
using System;

namespace AwtrixHub.Functions.Functions
{
    public class BinDetails
    {
        public DateTime Date { get; set; }
        public Colour Colour { get; set; }
        public BinDetails(DateTime date, Colour colour)
        {
            Date = date;
            Colour = colour;
        }
    }
}
