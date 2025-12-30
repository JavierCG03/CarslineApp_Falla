
namespace CarslineApp.Services
{
    public partial class ApiService
    {
        private readonly HttpClient _httpClient;

        //private const string BaseUrl = "http://10.22.16.32:5293/api"; //hotspot celular
        //private const string BaseUrl = "http://192.168.1.72:5293/api"; //Url para conecion Wifi casa
        private const string BaseUrl = "http://192.168.3.95:5293/api"; // Url_ Oficina CARSLINE

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }


    }
}
        
