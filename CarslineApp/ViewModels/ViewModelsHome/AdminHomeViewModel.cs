using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CarslineApp.Models;
using CarslineApp.Services;
using CarslineApp.Views;

namespace CarslineApp.ViewModels.ViewModelsHome
{
    public class AdminHomeViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private string _nombreUsuarioActual = string.Empty;
        private bool _isLoading;
        private ObservableCollection<RolDto> _rolesDisponibles = new();
        private RolDto? _rolSeleccionado;
        private string _nuevoNombreCompleto = string.Empty;
        private string _nuevoNombreUsuario = string.Empty;
        private string _nuevaPassword = string.Empty;
        private string _errorMessage = string.Empty;
        private string _successMessage = string.Empty;

        public AdminHomeViewModel()
        {
            _apiService = new ApiService();
            CrearUsuarioCommand = new Command(async () => await OnCrearUsuario(), () => !IsLoading);
            LogoutCommand = new Command(async () => await OnLogout());
            VerUsuariosCommand = new Command(async () => await OnVerUsuarios());
            VerInventarioCommand = new Command(async () => await OnVerInventario()); // NUEVO COMANDO

            CargarDatosIniciales();
        }

        public string NombreUsuarioActual
        {
            get => _nombreUsuarioActual;
            set
            {
                _nombreUsuarioActual = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                ((Command)CrearUsuarioCommand).ChangeCanExecute();
            }
        }

        public ObservableCollection<RolDto> RolesDisponibles
        {
            get => _rolesDisponibles;
            set
            {
                _rolesDisponibles = value;
                OnPropertyChanged();
            }
        }

        public RolDto? RolSeleccionado
        {
            get => _rolSeleccionado;
            set
            {
                _rolSeleccionado = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }

        public string NuevoNombreCompleto
        {
            get => _nuevoNombreCompleto;
            set
            {
                _nuevoNombreCompleto = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }

        public string NuevoNombreUsuario
        {
            get => _nuevoNombreUsuario;
            set
            {
                _nuevoNombreUsuario = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }

        public string NuevaPassword
        {
            get => _nuevaPassword;
            set
            {
                _nuevaPassword = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                _successMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand CrearUsuarioCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand VerUsuariosCommand { get; }
        public ICommand VerInventarioCommand { get; } // NUEVO COMANDO

        private async void CargarDatosIniciales()
        {
            NombreUsuarioActual = Preferences.Get("user_name", "Administrador");
            await CargarRoles();
        }

        private async Task CargarRoles()
        {
            try
            {
                var roles = await _apiService.ObtenerRolesAsync();
                RolesDisponibles.Clear();
                foreach (var rol in roles)
                {
                    RolesDisponibles.Add(rol);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar roles: {ex.Message}";
            }
        }

        private async Task OnCrearUsuario()
        {
            if (string.IsNullOrWhiteSpace(NuevoNombreCompleto))
            {
                ErrorMessage = "Por favor ingresa el nombre completo";
                return;
            }

            if (string.IsNullOrWhiteSpace(NuevoNombreUsuario))
            {
                ErrorMessage = "Por favor ingresa el nombre de usuario";
                return;
            }

            if (string.IsNullOrWhiteSpace(NuevaPassword))
            {
                ErrorMessage = "Por favor ingresa la contrasena";
                return;
            }

            if (NuevaPassword.Length < 6)
            {
                ErrorMessage = "La contrasena debe tener al menos 6 caracteres";
                return;
            }

            if (RolSeleccionado == null)
            {
                ErrorMessage = "Por favor selecciona un tipo de usuario";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                int adminId = Preferences.Get("user_id", 0);

                var response = await _apiService.CrearUsuarioAsync(
                    NuevoNombreCompleto,
                    NuevoNombreUsuario,
                    NuevaPassword,
                    RolSeleccionado.Id,
                    adminId);

                if (response.Success)
                {
                    SuccessMessage = "Usuario creado exitosamente";

                    NuevoNombreCompleto = string.Empty;
                    NuevoNombreUsuario = string.Empty;
                    NuevaPassword = string.Empty;
                    RolSeleccionado = null;

                    await Application.Current.MainPage.DisplayAlert(
                        "Exito",
                        $"El usuario ha sido creado correctamente",
                        "OK");
                }
                else
                {
                    ErrorMessage = response.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnVerUsuarios()
        {
            // Navegar a la pagina de lista de usuarios (carga bajo demanda)
            await Application.Current.MainPage.Navigation.PushAsync(new ListaUsuariosPage());
        }

        // NUEVO MÉTODO PARA NAVEGAR AL INVENTARIO
        private async Task OnVerInventario()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new InventarioPage());
        }

        private async Task OnLogout()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Cerrar Sesion",
                "Estas seguro que deseas cerrar sesion?",
                "Si",
                "No");

            if (confirm)
            {
                Preferences.Clear();

                // Navegar de vuelta al login
                Application.Current.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#512BD4"),
                    BarTextColor = Colors.White
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}