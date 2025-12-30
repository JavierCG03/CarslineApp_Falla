using CarslineApp.Models;
using CarslineApp.Services;
using CarslineApp.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CarslineApp.ViewModels.ViewModelsHome
{
    public class JefeMainViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private int _tipoOrdenSeleccionado = 1;
        private bool _isLoading;
        private string _nombreUsuarioActual = string.Empty;

        private ObservableCollection<OrdenConTrabajosDto> _ordenesPendientes = new();
        private ObservableCollection<OrdenConTrabajosDto> _ordenesProceso = new();
        private ObservableCollection<OrdenConTrabajosDto> _ordenesFinalizadas = new();

        public JefeMainViewModel()
        {
            _apiService = new ApiService();

            // Comandos de navegación
            VerServicioCommand = new Command(() => CambiarTipoOrden(1));
            VerDiagnosticoCommand = new Command(() => CambiarTipoOrden(2));
            VerReparacionCommand = new Command(() => CambiarTipoOrden(3));
            VerGarantiaCommand = new Command(() => CambiarTipoOrden(4));

            // Comandos de acciones
            RefreshCommand = new Command(async () => await CargarOrdenes());
            LogoutCommand = new Command(async () => await OnLogout());

            // Comandos para técnicos
            AsignarTecnicoCommand = new Command<TrabajoDto>(async (trabajo) => await AsignarTecnico(trabajo));
            ReasignarTecnicoCommand = new Command<TrabajoDto>(async (trabajo) => await ReasignarTecnico(trabajo));

            // Solo cargar nombre de usuario aquí
            NombreUsuarioActual = Preferences.Get("user_name", "Jefe de Taller");
        }

        public async Task InicializarAsync()
        {
            await CargarOrdenes();
        }

        #region Propiedades

        public string NombreUsuarioActual
        {
            get => _nombreUsuarioActual;
            set { _nombreUsuarioActual = value; OnPropertyChanged(); }
        }

        public int TipoOrdenSeleccionado
        {
            get => _tipoOrdenSeleccionado;
            set
            {
                _tipoOrdenSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloSeccion));
                OnPropertyChanged(nameof(EsServicio));
                OnPropertyChanged(nameof(EsDiagnostico));
                OnPropertyChanged(nameof(EsReparacion));
                OnPropertyChanged(nameof(EsGarantia));
            }
        }

        public string TituloSeccion => TipoOrdenSeleccionado switch
        {
            1 => "SERVICIO",
            2 => "DIAGNÓSTICO",
            3 => "REPARACIÓN",
            4 => "GARANTÍA",
            _ => "ÓRDENES"
        };

        public bool EsServicio => TipoOrdenSeleccionado == 1;
        public bool EsDiagnostico => TipoOrdenSeleccionado == 2;
        public bool EsReparacion => TipoOrdenSeleccionado == 3;
        public bool EsGarantia => TipoOrdenSeleccionado == 4;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<OrdenConTrabajosDto> OrdenesPendientes
        {
            get => _ordenesPendientes;
            set { _ordenesPendientes = value; OnPropertyChanged(); }
        }

        public ObservableCollection<OrdenConTrabajosDto> OrdenesProceso
        {
            get => _ordenesProceso;
            set { _ordenesProceso = value; OnPropertyChanged(); }
        }

        public ObservableCollection<OrdenConTrabajosDto> OrdenesFinalizadas
        {
            get => _ordenesFinalizadas;
            set { _ordenesFinalizadas = value; OnPropertyChanged(); }
        }

        public bool HayPendientes => OrdenesPendientes.Any();
        public bool HayProceso => OrdenesProceso.Any();
        public bool HayFinalizadas => OrdenesFinalizadas.Any();

        #endregion

        #region Comandos

        public ICommand VerServicioCommand { get; }
        public ICommand VerDiagnosticoCommand { get; }
        public ICommand VerReparacionCommand { get; }
        public ICommand VerGarantiaCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AsignarTecnicoCommand { get; }
        public ICommand ReasignarTecnicoCommand { get; }

        #endregion

        #region Métodos

        private async void CambiarTipoOrden(int tipoOrden)
        {
            TipoOrdenSeleccionado = tipoOrden;
            await CargarOrdenes();
        }

        private async Task CargarOrdenes()
        {
            IsLoading = true;

            try
            {
                // Primero obtenemos las órdenes básicas
                var ordenesList = await _apiService.ObtenerOrdenesPorTipo_JefeAsync(TipoOrdenSeleccionado);
                System.Diagnostics.Debug.WriteLine($"📦 Órdenes recibidas: {ordenesList?.Count ?? 0}");

                if (ordenesList == null || !ordenesList.Any())
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OrdenesPendientes.Clear();
                        OrdenesProceso.Clear();
                        OrdenesFinalizadas.Clear();
                        NotificarCambiosDashboards();
                    });
                    return;
                }

                // Ahora obtenemos el detalle completo con trabajos para cada orden
                var ordenesPendientes = new List<OrdenConTrabajosDto>();
                var ordenesProceso = new List<OrdenConTrabajosDto>();
                var ordenesFinalizadas = new List<OrdenConTrabajosDto>();

                foreach (var ordenBasica in ordenesList)
                {
                    // Obtener detalle completo con trabajos
                    var ordenCompleta = await _apiService.ObtenerOrdenCompletaAsync(ordenBasica.Id);

                    if (ordenCompleta != null)
                    {
                        if (ordenCompleta.EstadoOrdenId == 1)
                            ordenesPendientes.Add(ordenCompleta);
                        else if (ordenCompleta.EstadoOrdenId == 2)
                            ordenesProceso.Add(ordenCompleta);
                        else if (ordenCompleta.EstadoOrdenId == 3)
                            ordenesFinalizadas.Add(ordenCompleta);
                    }
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OrdenesPendientes.Clear();
                    OrdenesProceso.Clear();
                    OrdenesFinalizadas.Clear();

                    foreach (var orden in ordenesPendientes)
                        OrdenesPendientes.Add(orden);

                    foreach (var orden in ordenesProceso)
                        OrdenesProceso.Add(orden);

                    foreach (var orden in ordenesFinalizadas)
                        OrdenesFinalizadas.Add(orden);

                    NotificarCambiosDashboards();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al cargar órdenes: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NotificarCambiosDashboards()
        {
            OnPropertyChanged(nameof(OrdenesPendientes));
            OnPropertyChanged(nameof(OrdenesProceso));
            OnPropertyChanged(nameof(OrdenesFinalizadas));
            OnPropertyChanged(nameof(HayPendientes));
            OnPropertyChanged(nameof(HayProceso));
            OnPropertyChanged(nameof(HayFinalizadas));
        }

        /// <summary>
        /// Asignar técnico a un trabajo sin técnico asignado
        /// </summary>
        private async Task AsignarTecnico(TrabajoDto trabajo)
        {
            try
            {
                IsLoading = true;

                // Obtener lista de técnicos
                var tecnicos = await _apiService.ObtenerTecnicosAsync();

                if (tecnicos == null || !tecnicos.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Sin técnicos",
                        "No hay técnicos disponibles para asignar",
                        "OK");
                    return;
                }

                // Crear lista de nombres para selección
                var nombresTecnicos = tecnicos.Select(t => t.NombreCompleto).ToArray();

                string tecnicoSeleccionado = await Application.Current.MainPage.DisplayActionSheet(
                    $"Asignar técnico a:\n{trabajo.Trabajo}",
                    "Cancelar",
                    null,
                    nombresTecnicos);

                if (string.IsNullOrEmpty(tecnicoSeleccionado) || tecnicoSeleccionado == "Cancelar")
                    return;

                // Obtener ID del técnico seleccionado
                var tecnico = tecnicos.FirstOrDefault(t => t.NombreCompleto == tecnicoSeleccionado);
                if (tecnico == null) return;

                // Confirmar asignación
                bool confirmar = await Application.Current.MainPage.DisplayAlert(
                    "Confirmar asignación",
                    $"¿Asignar a {tecnico.NombreCompleto}?\n\nTrabajo: {trabajo.Trabajo}",
                    "Sí, asignar",
                    "Cancelar");

                if (!confirmar) return;

                // Realizar asignación
                int jefeId = Preferences.Get("user_id", 0);
                var response = await _apiService.AsignarTecnicoAsync(trabajo.Id, tecnico.Id, jefeId);

                if (response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "✅ Éxito",
                        $"Trabajo asignado a {tecnico.NombreCompleto}",
                        "OK");

                    // Recargar órdenes
                    await CargarOrdenes();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        response.Message,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al asignar técnico: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Reasignar técnico a un trabajo que ya tiene técnico asignado
        /// </summary>
        private async Task ReasignarTecnico(TrabajoDto trabajo)
        {
            try
            {
                // Verificar que el trabajo no esté en proceso
                if (trabajo.EnProceso)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "No disponible",
                        "No se puede reasignar un trabajo que ya está en proceso",
                        "OK");
                    return;
                }

                IsLoading = true;

                // Obtener lista de técnicos
                var tecnicos = await _apiService.ObtenerTecnicosAsync();

                if (tecnicos == null || !tecnicos.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Sin técnicos",
                        "No hay técnicos disponibles",
                        "OK");
                    return;
                }

                // Filtrar al técnico actual
                var tecnicosDisponibles = tecnicos
                    .Where(t => t.Id != trabajo.TecnicoAsignadoId)
                    .ToList();

                if (!tecnicosDisponibles.Any())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Sin opciones",
                        "No hay otros técnicos disponibles para reasignar",
                        "OK");
                    return;
                }

                var nombresTecnicos = tecnicosDisponibles.Select(t => t.NombreCompleto).ToArray();

                string nuevoTecnico = await Application.Current.MainPage.DisplayActionSheet(
                    $"Reasignar trabajo:\n{trabajo.Trabajo}\n\nTécnico actual: {trabajo.TecnicoNombre}",
                    "Cancelar",
                    null,
                    nombresTecnicos);

                if (string.IsNullOrEmpty(nuevoTecnico) || nuevoTecnico == "Cancelar")
                    return;

                var tecnico = tecnicosDisponibles.FirstOrDefault(t => t.NombreCompleto == nuevoTecnico);
                if (tecnico == null) return;

                bool confirmar = await Application.Current.MainPage.DisplayAlert(
                    "Confirmar reasignación",
                    $"Cambiar de:\n{trabajo.TecnicoNombre}\n\nA:\n{tecnico.NombreCompleto}\n\nTrabajo: {trabajo.Trabajo}",
                    "Sí, reasignar",
                    "Cancelar");

                if (!confirmar) return;

                int jefeId = Preferences.Get("user_id", 0);
                var response = await _apiService.ReasignarTecnicoAsync(trabajo.Id, tecnico.Id, jefeId);

                if (response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "✅ Éxito",
                        $"Trabajo reasignado a {tecnico.NombreCompleto}",
                        "OK");

                    await CargarOrdenes();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        response.Message,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al reasignar: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnLogout()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Cerrar Sesión",
                "¿Estás seguro que deseas cerrar sesión?",
                "Sí",
                "No");

            if (confirm)
            {
                Preferences.Clear();
                Application.Current.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#D60000"),
                    BarTextColor = Colors.White
                };
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}