using CarslineApp.Views;

namespace CarslineApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Solo iniciar con LoginPage, las demas se cargan bajo demanda
            //MainPage = new LoginPage();
            
            MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#D60000"),
                BarTextColor = Colors.White
            };
        }
    }
}