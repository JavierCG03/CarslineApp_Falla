using CarslineApp.ViewModels.ViewModelsHome;

namespace CarslineApp.Views
{
    public partial class AdminHomePage : ContentPage
    {
        public AdminHomePage()
        {
            InitializeComponent();
            BindingContext = new AdminHomeViewModel();
        }
    }
}