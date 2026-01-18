using System;

namespace AwtrixHub.Functions.DTOs
{
    public class IndicatorDTO
    {
        public int IndicatorNumber { get; set; }
        public int[] Color { get; set; }
        public int Blink { get; set; }

        public IndicatorDTO() { }

        public string GetEndpoint()
        {
            return IndicatorNumber switch
            {
                1 or 2 or 3 => $"indicator{IndicatorNumber}",
                _ => throw new Exception("Indicator Number Not Valid"),
            };
        }
    }
}
