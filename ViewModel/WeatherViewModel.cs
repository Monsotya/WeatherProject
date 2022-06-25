using WeatherProject.Models;

namespace WeatherProject.ViewModel
{
    public class WeatherViewModel
    {
        public IEnumerable<Weather> Weathers { get; set; }
        public int WeatherPerPage { get; set; }
        public int CurrentPage { get; set; }

        public int PageCount()
        {
            return Convert.ToInt32(Math.Ceiling(Weathers.Count() / (double)WeatherPerPage));
        }
        public IEnumerable<Weather> PaginatedWeathers()
        {
            int start = (CurrentPage - 1) * WeatherPerPage;
            return Weathers.OrderBy(b => b.Time).Skip(start).Take(WeatherPerPage);
        }
    }
}
