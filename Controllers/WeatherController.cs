using Highsoft.Web.Mvc.Charts;
using IronXL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelectPdf;
using System.Data;
using System.Net.Mail;
using System.Net.Mime;
using WeatherProject.Models;
using WeatherProject.ViewModel;

namespace WeatherProject.Controllers
{
    public class WeatherController : Controller
    {
        private WeatherContext _db;
        public WeatherController(WeatherContext db)
        {
            _db = db;
        }
        public IActionResult ChooseConditionsForWeather(DateTime dateFrom, DateTime dateTo, int page = 1)
        {
            if (dateFrom > dateTo)
            {
                return RedirectToAction("Index", "Home", new { str = "Неправильно заданий проміжок!" });
            }
            else if (dateFrom.Year != 2012 || dateTo.Year != 2012)
            {
                return RedirectToAction("Index", "Home", new { str = "Має бути 2012 рік!" });
            }
            _db.Weathers.RemoveRange(_db.Weathers);
            _db.SaveChanges();
            string[] files = Directory.GetFiles(@".\cities\" + Request.Form["cities"], "*.xlsx");
            var f = files.OrderBy(x => Int32.Parse(x.Split('-')[1].Split('.')[0]));
            //_db.Weathers.Delete();
            foreach (var file in f)
            {
                WorkBook workbook = WorkBook.Load(file);
                WorkSheet sheet = workbook.WorkSheets.First();
                List<RangeRow> rows = sheet.Rows.Skip(1).ToList<RangeRow>();
                
                int month = Int32.Parse(file.Split('-')[1].Split('.')[0]);
                foreach (RangeRow row in rows)
                {
                    string wind = GetWindDirection(row.Columns[3].ToString());
                    decimal visibility = GetVisibility(row.Columns[7].ToString());
                    int hour = ((DateTime)row.Columns[1].DateTimeValue).Hour;
                    int minute = ((DateTime)row.Columns[1].DateTimeValue).Minute;
                    DateTime dateTime = new DateTime(2012, month, row.Columns[0].Int32Value, hour, minute, 0, DateTimeKind.Utc);
                        
                    _db.Weathers.Add(new Weather { Temperature = Int32.Parse(string.IsNullOrEmpty(row.Columns[2].ToString()) ? "-100" : row.Columns[2].Int32Value.ToString()),
                        CloudBottomLine = Int32.Parse(string.IsNullOrEmpty(row.Columns[10].ToString()) ? "-100" : row.Columns[10].Int32Value.ToString()),
                        CloudNumber = Int32.Parse(string.IsNullOrEmpty(row.Columns[6].ToString()) ? "-100" : row.Columns[6].Int32Value.ToString()), 
                        Time = dateTime, 
                        Visibility = visibility, 
                        WeatherCode = row.Columns[5].ToString(), 
                        WindDirection = wind, 
                        WindSpeed = Int32.Parse(string.IsNullOrEmpty(row.Columns[4].ToString()) ? "-100" : row.Columns[4].Int32Value.ToString())
                    });
                    
                }
                _db.SaveChanges();
                break;
            }
            var dates = _db.Weathers.Where(x => x.Time < dateFrom);
            _db.Weathers.RemoveRange(dates);
            _db.SaveChanges();
            dates = _db.Weathers.Where(x => x.Time > dateTo);
            _db.Weathers.RemoveRange(dates);
            _db.SaveChanges();
            for (int i = 0; i < _db.Weathers.Count(); i++)
            {
                if (_db.Weathers.AsEnumerable().ElementAt(i).WindDirection == "")
                {
                    FixWindDirection(_db.Weathers.AsEnumerable().ElementAt(i).WeatherId);
                }
                if (_db.Weathers.AsEnumerable().ElementAt(i).Temperature == -100)
                {
                    FixTemperature(_db.Weathers.AsEnumerable().ElementAt(i).WeatherId);
                }
                if (_db.Weathers.AsEnumerable().ElementAt(i).WeatherCode == "")
                {
                    FixWeatherCode(_db.Weathers.AsEnumerable().ElementAt(i).WeatherId);
                }
                if (_db.Weathers.AsEnumerable().ElementAt(i).WindSpeed == -100)
                {
                    FixWindSpeed(_db.Weathers.AsEnumerable().ElementAt(i).WeatherId);
                }
                if (_db.Weathers.AsEnumerable().ElementAt(i).CloudNumber == -100)
                {
                    FixCloudNumber(_db.Weathers.AsEnumerable().ElementAt(i).WeatherId);
                }
                if (_db.Weathers.AsEnumerable().ElementAt(i).CloudBottomLine == -100)
                {
                    FixCloudBottomLine(_db.Weathers.AsEnumerable().ElementAt(i).WeatherId);
                }


            }
            _db.SaveChanges();
            var weathersView = new WeatherViewModel
            {
                WeatherPerPage = 60,
                Weathers = _db.Weathers.OrderBy(d => d.Time),
                CurrentPage = page
            };
            return View("ChosenWeather", weathersView);
        }
        public ActionResult GeneratePDF()
        {
            var fullView = new HtmlToPdf();
            var pdf = (fullView.ConvertUrl("https://localhost:7073/Weather/TemperatureChart"));
            pdf.Append(fullView.ConvertUrl("https://localhost:7073/Weather/TemperatureTableChart"));
            pdf.Append(fullView.ConvertUrl("https://localhost:7073/Weather/WindChart"));
            pdf.Append(fullView.ConvertUrl("https://localhost:7073/Weather/WindTableChart"));
            var pdfBytes = pdf.Save();

            return File(pdfBytes, "application/pdf");
        }
        public ActionResult SendEmail()
        {
            return View();
        }
        public ActionResult SendEmailConfirm(string email)
        {
            string from = "";  
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress("");
            mail.To.Add(email);
            mail.Subject = "Weather Project";

            System.Net.Mail.Attachment attachment;
            attachment = new System.Net.Mail.Attachment("c:/report.pdf");
            mail.Attachments.Add(attachment);

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("s61788466@gmail.com", "**********");
            SmtpServer.EnableSsl = true;

            
            if (attachment.Name != null)
                {
                    Attachment attachmentFilename = new Attachment(attachment.Name, MediaTypeNames.Application.Octet);
                    ContentDisposition disposition = attachment.ContentDisposition;
                    disposition.FileName = Path.GetFileName(attachment.Name);
                    disposition.Size = new FileInfo(attachment.Name).Length;
                    disposition.DispositionType = DispositionTypeNames.Attachment;
                    mail.Attachments.Add(attachment);
                }
            SmtpServer.Send(mail);
            return RedirectToAction("ChooseConditionsForWeather");
        }
        public ActionResult TemperatureChart() {

            return View(_db.Weathers.ToList());
        }
        public ActionResult TemperatureTableChart()
        {

            return View(_db.Weathers.ToList());
        }
        public ActionResult WindChart()
        {

            return View(_db.Weathers.ToList());
        }
        public ActionResult WindTableChart()
        {

            return View(_db.Weathers.ToList());
        }

        public void FixTemperature(int index)
        {
            List<int> indexes = new List<int>();
            indexes.Add(index);
            if (index == _db.Weathers.OrderBy(x => x.WeatherId).First().WeatherId)
            {
                for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
                {
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().Temperature == -100)
                    {
                        indexes.Add(i);
                    }
                    else
                    {
                        foreach (int j in indexes)
                        {
                            Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                            weather.Temperature = _db.Weathers.Where(x => x.WeatherId == i).First().Temperature;
                        }
                        _db.SaveChanges();
                        return;
                    }
                }
            }

            int minTime = _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            if (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.ToString("tt") == "PM")
            {
                minTime = (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour + 12) *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            }
            int minTemperature = _db.Weathers.Where(x => x.WeatherId == index - 1).First().Temperature;
            int time;
            for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
            {
                if (i == _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId && _db.Weathers.Where(x => x.WeatherId == i).First().Temperature == -100)
                {
                    indexes.Add(i);
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.Temperature = _db.Weathers.Where(x => x.WeatherId == index - 1).First().Temperature;
                    }
                    _db.SaveChanges();
                    return;
                }
                if (_db.Weathers.Where(x => x.WeatherId == i).First().Temperature == -100)
                {
                    indexes.Add(i);
                }
                else
                {
                    int maxTime = _db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour *
                    (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                    _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().Time.ToString("tt") == "PM")
                    {
                        maxTime = (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour + 12) *
                        (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                        _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    }
                    if (maxTime - minTime == 0)
                    {
                        maxTime = 2;
                        minTime = 1;
                    }
                    int maxTemperature = _db.Weathers.Where(x => x.WeatherId == i).First().Temperature;
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        time = _db.Weathers.Where(x => x.WeatherId == j).First().Time.Hour *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Minute *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Day;
                        weather.Temperature = minTemperature + (time - minTime) * (maxTemperature - minTemperature) / (maxTime - minTime);
                    }
                    _db.SaveChanges();
                    return;
                }
            }

        }
        public void FixCloudNumber(int index)
        {
            List<int> indexes = new List<int>();
            indexes.Add(index);
            if (index == _db.Weathers.OrderBy(x => x.WeatherId).First().WeatherId)
            {
                for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
                {
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().CloudNumber == -100)
                    {
                        indexes.Add(i);
                    }
                    else
                    {
                        foreach (int j in indexes)
                        {
                            Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                            weather.CloudNumber = _db.Weathers.Where(x => x.WeatherId == i).First().CloudNumber;
                        }
                        _db.SaveChanges();
                        return;
                    }
                }
            }

            int minTime = _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            if (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.ToString("tt") == "PM")
            {
                minTime = (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour + 12) *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            }
            int minCloudNumber = _db.Weathers.Where(x => x.WeatherId == index - 1).First().CloudNumber;
            int time;
            for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
            {
                if (i == _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId && _db.Weathers.Where(x => x.WeatherId == i).First().CloudNumber == -100)
                {
                    indexes.Add(i);
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.CloudNumber = _db.Weathers.Where(x => x.WeatherId == index - 1).First().CloudNumber;
                    }
                    _db.SaveChanges();
                    return;
                }
                if (_db.Weathers.Where(x => x.WeatherId == i).First().CloudNumber == -100)
                {
                    indexes.Add(i);
                }
                else
                {
                    int maxTime = _db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour *
                    (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                    _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().Time.ToString("tt") == "PM")
                    {
                        maxTime = (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour + 12) *
                        (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                        _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    }
                    if (maxTime - minTime == 0)
                    {
                        maxTime = 2;
                        minTime = 1;
                    }
                    int maxCloudNumber = _db.Weathers.Where(x => x.WeatherId == i).First().WindSpeed;
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        time = _db.Weathers.Where(x => x.WeatherId == j).First().Time.Hour *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Minute *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Day;
                        weather.CloudNumber = minCloudNumber + (time - minTime) * (maxCloudNumber - minCloudNumber) / (maxTime - minTime);
                    }
                    _db.SaveChanges();
                    return;
                }
            }

        }

        public void FixCloudBottomLine(int index)
        {
            List<int> indexes = new List<int>();
            indexes.Add(index);
            if (index == _db.Weathers.OrderBy(x => x.WeatherId).First().WeatherId)
            {
                for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
                {
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().CloudBottomLine == -100)
                    {
                        indexes.Add(i);
                    }
                    else
                    {
                        foreach (int j in indexes)
                        {
                            Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                            weather.CloudBottomLine = _db.Weathers.Where(x => x.WeatherId == i).First().CloudBottomLine;
                        }
                        _db.SaveChanges();
                        return;
                    }
                }
            }

            int minTime = _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            if (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.ToString("tt") == "PM")
            {
                minTime = (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour + 12) *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            }
            int minCloudBottomLine = _db.Weathers.Where(x => x.WeatherId == index - 1).First().CloudBottomLine;
            int time;
            for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
            {
                if (i == _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId && _db.Weathers.Where(x => x.WeatherId == i).First().CloudBottomLine == -100)
                {
                    indexes.Add(i);
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.CloudBottomLine = _db.Weathers.Where(x => x.WeatherId == index - 1).First().CloudBottomLine;
                    }
                    _db.SaveChanges();
                    return;
                }
                if (_db.Weathers.Where(x => x.WeatherId == i).First().CloudBottomLine == -100)
                {
                    indexes.Add(i);
                }
                else
                {
                    int maxTime = _db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour *
                    (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                    _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().Time.ToString("tt") == "PM")
                    {
                        maxTime = (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour + 12) *
                        (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                        _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    }
                    if(maxTime - minTime == 0)
                    {
                        maxTime = 2;
                        minTime = 1;
                    }
                    int maxCloudBottomLine = _db.Weathers.Where(x => x.WeatherId == i).First().CloudBottomLine;
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        time = _db.Weathers.Where(x => x.WeatherId == j).First().Time.Hour *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Minute *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Day;
                        weather.CloudBottomLine = minCloudBottomLine + (time - minTime) * (maxCloudBottomLine - minCloudBottomLine) / (maxTime - minTime);
                    }
                    _db.SaveChanges();
                    return;
                }
            }

        }
        public void FixWindSpeed(int index)
        {
            List<int> indexes = new List<int>();
            indexes.Add(index);
            if (index == _db.Weathers.OrderBy(x => x.WeatherId).First().WeatherId)
            {
                for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
                {
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().WindSpeed == -100)
                    {
                        indexes.Add(i);
                    }
                    else
                    {
                        foreach (int j in indexes)
                        {
                            Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                            weather.WindSpeed = _db.Weathers.Where(x => x.WeatherId == i).First().WindSpeed;
                        }
                        _db.SaveChanges();
                        return;
                    }
                }
            }

            int minTime = _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            if (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.ToString("tt") == "PM")
            {
                minTime = (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Hour + 12) *
                (_db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Minute == 0 ? 60 : 30) *
                _db.Weathers.Where(x => x.WeatherId == index - 1).First().Time.Day;
            }
            int minSpeed = _db.Weathers.Where(x => x.WeatherId == index - 1).First().WindSpeed;
            int time;
            for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
            {
                if (i == _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId && _db.Weathers.Where(x => x.WeatherId == i).First().WindSpeed == -100)
                {
                    indexes.Add(i);
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.WindSpeed = _db.Weathers.Where(x => x.WeatherId == index - 1).First().WindSpeed;
                    }
                    _db.SaveChanges();
                    return;
                }
                if (_db.Weathers.Where(x => x.WeatherId == i).First().WindSpeed == -100)
                {
                    indexes.Add(i);
                }
                else
                {
                    int maxTime = _db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour *
                    (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                    _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    if (_db.Weathers.Where(x => x.WeatherId == i).First().Time.ToString("tt") == "PM")
                    {
                        maxTime = (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Hour + 12) *
                        (_db.Weathers.Where(x => x.WeatherId == i).First().Time.Minute == 0 ? 60 : 30) *
                        _db.Weathers.Where(x => x.WeatherId == i).First().Time.Day;
                    }
                    if (maxTime - minTime == 0)
                    {
                        maxTime = 2;
                        minTime = 1;
                    }
                    int maxSpeed = _db.Weathers.Where(x => x.WeatherId == i).First().WindSpeed;
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        time = _db.Weathers.Where(x => x.WeatherId == j).First().Time.Hour *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Minute *
                        _db.Weathers.Where(x => x.WeatherId == j).First().Time.Day;
                        weather.WindSpeed = minSpeed + (time - minTime)*(maxSpeed - minSpeed)/(maxTime - minTime);
                    }
                    _db.SaveChanges();
                    return;
                }
            }

        }
        public void FixWeatherCode(int index)
        {
            List<int> indexes = new List<int>();
            indexes.Add(index);
            for (int i = index + 1; i <= _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId; i++)
            {
                if (i == _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId && _db.Weathers.Where(x => x.WeatherId == i).First().WeatherCode == "")
                {
                    indexes.Add(i);
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.WeatherCode = _db.Weathers.Where(x => x.WeatherId == index - 1).First().WeatherCode;
                    }
                    _db.SaveChanges();
                    return;
                }
                if (_db.Weathers.Where(x => x.WeatherId == i).First().WeatherCode == "")
                {
                    indexes.Add(i);
                }
                else
                {
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.WeatherCode = _db.Weathers.Where(x => x.WeatherId == i).First().WeatherCode;
                    }
                    _db.SaveChanges();
                    return;
                }
            }
        }

        public void FixWindDirection(int index)
        {
            List<int> indexes = new List<int>();
            indexes.Add(index);
            for (int i = index + 1; i <= _db.Weathers.OrderByDescending( x => x.WeatherId).First().WeatherId; i++)
            {
                if (i == _db.Weathers.OrderByDescending(x => x.WeatherId).First().WeatherId && _db.Weathers.Where(x => x.WeatherId == i).First().WindDirection == "")
                {
                    indexes.Add(i);
                    foreach (int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.WindDirection = _db.Weathers.Where(x => x.WeatherId == index - 1).First().WindDirection;
                    }
                    _db.SaveChanges();
                    return;
                }
                if (_db.Weathers.Where(x => x.WeatherId == i).First().WindDirection == "")
                {
                    indexes.Add(i);
                }
                else
                {
                    foreach(int j in indexes)
                    {
                        Weather weather = _db.Weathers.First(x => x.WeatherId == j);
                        weather.WindDirection = _db.Weathers.Where(x => x.WeatherId == i).First().WindDirection;
                    }
                    _db.SaveChanges();
                    return;
                }
            }
        }

        public decimal GetVisibility(string visibility)
        {
            if (Decimal.TryParse(visibility, out decimal result))
            {
                return result;
            }
            if(visibility == String.Empty)
            {
                return -1;
            }
            if (visibility.Split('.').Count() == 1)
            {
                return Convert.ToDecimal(visibility.Split('/')[1] + visibility.Split('/')[0]);
            }
            string month = visibility.Split('.')[1];

            month = month switch
            {
                "Січ" => "1",
                "Лют" => "2",
                "Бер" => "3",
                "Кві" => "4",
                "Тра" => "5",
                "Чер" => "6",
                "Лип" => "7",
                "Сер" => "8",
                "Вер" => "9",
                "Жов" => "10",
                "Лис" => "11",
                "Гру" => "12",
                _ => "0"
            };
            return Convert.ToDecimal(month + "." + visibility.Split('.')[0][1]);    

        }
        public string GetWindDirection(string wind)
        {
            wind = wind switch
            {
                "Северый" => "Північний",
                "Южный" => "Південний",
                "Западый" => "Західний",
                "Восточный" => "Східний",
                "Ю-В" => "Пд-Сх",
                "С-В" => "Пн-Сх",
                "С-З" => "Пн-Зх",
                "Ю-З" => "Пд-Зх",
                "В-Ю" => "Сх-Пд",
                "З-Ю" => "Зх-Пд",
                "В-С" => "Сх-Пн",
                "З-С" => "Зх-Пн",
                "Переменный" => "Мінливий",
                _ => "",
            };
            return wind;
        }
    }
    
}
