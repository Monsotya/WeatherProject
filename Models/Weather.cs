namespace WeatherProject.Models
{
    public class Weather
    {
        public int WeatherId { get; set; }
        public DateTime Time { get; set; }
        public string WindDirection { get; set; }
        public decimal Visibility { get; set; }
        public int Temperature { get; set; }
        public string WeatherCode { get; set; }
        public int WindSpeed { get; set; }
        public int CloudNumber { get; set; }
        public int CloudBottomLine { get; set; }
    }
}
