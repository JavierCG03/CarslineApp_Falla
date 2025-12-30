using Microsoft.Maui.Controls;

namespace CarslineApp.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Si hay sesión activa, saltar login
            var token = Preferences.Get("auth_token", string.Empty);
            if (!string.IsNullOrEmpty(token))
            {
                var rolName = Preferences.Get("user_role_name", string.Empty);
                NavigateByRole(rolName);
                return;
            }

            // Animaciones de entrada
            await LogoImage.FadeTo(1, 500);
            LoginCard.TranslationY = 50;
            await LoginCard.FadeTo(1, 500);
            await LoginCard.TranslateTo(0, 0, 300, Easing.CubicOut);



            // Animación botón presionado
            LoginButton.Pressed += async (s, e) =>
            {
                await LoginButton.ScaleTo(0.95, 80);
                await LoginButton.ScaleTo(1, 80);
            };
        }

        private void NavigateByRole(string rolNombre)
        {
            if (string.IsNullOrEmpty(rolNombre)) return;

            Page targetPage = rolNombre switch
            {
                "Administrador" => new AdminHomePage(),
                "Asesor de servicio" => new AsesorHomePage(),
                "Jefe de Taller" => new JefeHomePage(),
                "Gerente" => new GerenteHomePage(),
                "Tecnico de mantenimiento" => new TecnicoHomePage(),
                _ => null
            };

            if (targetPage != null)
            {
                Application.Current.MainPage = new NavigationPage(targetPage)
                {
                    BarBackgroundColor = Color.FromArgb("#D60000"),
                    BarTextColor = Colors.White
                };
            }
        }
    }
}
