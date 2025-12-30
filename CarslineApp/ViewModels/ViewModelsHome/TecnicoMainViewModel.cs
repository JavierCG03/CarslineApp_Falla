using CarslineApp.Models;
using CarslineApp.Services;
using CarslineApp.Views;
using CarslineApp.Views.ChecksList;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CarslineApp.ViewModels.ViewModelsHome
{
    public class TecnicoMainViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private int _estadoTrabajoSeleccionado = 2;
        private bool _isLoading;
        private string _nombreUsuarioActual = string.Empty;


        private ObservableCollection<MiTrabajoDto> _trabajosServicio = new();
        private ObservableCollection<MiTrabajoDto> _trabajosReparacion = new();
        private ObservableCollection<MiTrabajoDto> _trabajosDiagnostico = new();
        private ObservableCollection<MiTrabajoDto> _trabajosGarantia = new();
        private ObservableCollection<MiTrabajoDto> _trabajosReacondicionamiento = new();


        public TecnicoMainViewModel()
        {
            _apiService = new ApiService();

            // Comandos de navegación
            VerPendientesCommand = new Command(() => CambiarEstadoTrabajo(2));
            VerPausadosCommand = new Command(() => CambiarEstadoTrabajo(5));
            VerFinalizadosCommand = new Command(() => CambiarEstadoTrabajo(4));

            // Comandos de acciones
            RefreshCommand = new Command(async () => await CargarTrabajos());
            LogoutCommand = new Command(async () => await OnLogout());

            // Comandos para Trabajos
            IniciarTrabajoCommand = new Command<MiTrabajoDto>(async (trabajo) => await IniciarTrabajo(trabajo));
            ReanudarTrabajoCommand = new Command<MiTrabajoDto>(async (trabajo) => await ReanudarTrabajo(trabajo));

            // Solo cargar nombre de usuario aquí
            NombreUsuarioActual = Preferences.Get("user_name", "Tecnico");
        }

        public async Task InicializarAsync()
        {
            await CargarTrabajos();
        }

        #region Propiedades

        public string NombreUsuarioActual
        {
            get => _nombreUsuarioActual;
            set { _nombreUsuarioActual = value; OnPropertyChanged(); }
        }

        public int EstadoTrabajoSeleccionado
        {
            get => _estadoTrabajoSeleccionado;
            set
            {
                _estadoTrabajoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloSeccion));
                OnPropertyChanged(nameof(EsPendiente));
                OnPropertyChanged(nameof(EsPausado));
                OnPropertyChanged(nameof(EsFinalizado));
                OnPropertyChanged(nameof(MensajeNoTrabajos));
                OnPropertyChanged(nameof(NoHayTrabajos));
            }
        }

        public string TituloSeccion => EstadoTrabajoSeleccionado switch
        {

            2 => "PENDIENTES",
            5 => "PAUSADOS",
            4 => "FINALIZADOS",
            _ => "TRABAJOS"
        };

        public bool EsPendiente => EstadoTrabajoSeleccionado == 2;
        public bool EsPausado => EstadoTrabajoSeleccionado == 5;
        public bool EsFinalizado => EstadoTrabajoSeleccionado == 4;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MiTrabajoDto> TrabajosServicio
        {
            get => _trabajosServicio;
            set { _trabajosServicio = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MiTrabajoDto> TrabajosReparacion
        {
            get => _trabajosReparacion;
            set { _trabajosReparacion = value; OnPropertyChanged(); }
        }
        public ObservableCollection<MiTrabajoDto> TrabajosDiagnostico
        {
            get => _trabajosDiagnostico;
            set { _trabajosDiagnostico = value; OnPropertyChanged(); }
        }
        public ObservableCollection<MiTrabajoDto> TrabajosGarantia
        {
            get => _trabajosGarantia;
            set { _trabajosGarantia = value; OnPropertyChanged(); }
        }
        public ObservableCollection<MiTrabajoDto> TrabajosReacondicionamiento
        {
            get => _trabajosReacondicionamiento;
            set { _trabajosReacondicionamiento = value; OnPropertyChanged(); }
        }



        public bool HayTrabajosServicio => TrabajosServicio.Any();
        public bool HayTrabajosReparacion => TrabajosReparacion.Any();
        public bool HayTrabajosDiagnostico => TrabajosDiagnostico.Any();
        public bool HayTrabajosGarantia => TrabajosGarantia.Any();
        public bool HayTrabajosReacondicionamiento => TrabajosReacondicionamiento.Any();

        // 🆕 Propiedades para mostrar mensaje cuando no hay trabajos
        public bool NoHayTrabajos => !HayTrabajosServicio &&
                                     !HayTrabajosReparacion &&
                                     !HayTrabajosDiagnostico &&
                                     !HayTrabajosGarantia &&
                                     !HayTrabajosReacondicionamiento;

        public string MensajeNoTrabajos => EstadoTrabajoSeleccionado switch
        {
            2 => "📋 No tienes trabajos pendientes",
            5 => "⏸️ No tienes trabajos pausados",
            4 => "✅ No tienes trabajos finalizados",
            _ => "📋 No hay trabajos disponibles"
        };

        #endregion

        #region Comandos

        public ICommand VerPendientesCommand { get; }
        public ICommand VerPausadosCommand { get; }
        public ICommand VerFinalizadosCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand IniciarTrabajoCommand { get; }
        public ICommand ReanudarTrabajoCommand { get; }



        #endregion

        #region Métodos
        private async Task NavegarACheckList(MiTrabajoDto trabajo)
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new CheckListServicioPage(
                    trabajo.Id,
                    trabajo.OrdenGeneralId,
                    trabajo.Trabajo,
                    trabajo.VehiculoCompleto
                )
            );
        }
        private async Task NavegarACheckListReparacion(MiTrabajoDto trabajo)
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new CheckListReparacion(
                    trabajo.Id,
                    trabajo.OrdenGeneralId,
                    trabajo.Trabajo,
                    trabajo.VehiculoCompleto,
                    trabajo.IndicacionesTrabajo
                )

            );
        }
        private async Task NavegarACheckListDiagnostico(MiTrabajoDto trabajo)
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new CheckListDiagnostico(
                    trabajo.Id,
                    trabajo.OrdenGeneralId,
                    trabajo.Trabajo,
                    trabajo.VehiculoCompleto,
                    trabajo.IndicacionesTrabajo
                )

            );
        }
        private async Task NavegarACheckListGarantia(MiTrabajoDto trabajo)
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new CheckListGarantia(
                    trabajo.Id,
                    trabajo.OrdenGeneralId,
                    trabajo.Trabajo,
                    trabajo.VehiculoCompleto,
                    trabajo.IndicacionesTrabajo
                )
            );
        }
        private async Task NavegarACheckListReacondicionamiento()
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new CheckListReacondicionamiento()
            );
        }


        private async void CambiarEstadoTrabajo(int estadoTrabajo)
        {
            EstadoTrabajoSeleccionado = estadoTrabajo;
            await CargarTrabajos();
        }

        private async Task CargarTrabajos()
        {
            IsLoading = true;

            try
            {
                int tecnicoId = Preferences.Get("user_id", 0);

                var response = await _apiService.ObtenerMisTrabajosAsync(
                    tecnicoId,
                    EstadoTrabajoSeleccionado
                );

                if (response == null || response.Trabajos == null)
                    return;

                // 🔹 LIMPIAR LISTAS
                TrabajosServicio.Clear();
                TrabajosDiagnostico.Clear();
                TrabajosReparacion.Clear();
                TrabajosGarantia.Clear();
                TrabajosReacondicionamiento.Clear();

                // 🔹 SEPARAR POR TIPO DE ORDEN
                foreach (var trabajo in response.Trabajos)
                {
                    switch (trabajo.TipoOrden)
                    {
                        case 1:
                            TrabajosServicio.Add(trabajo);
                            break;

                        case 2:
                            TrabajosDiagnostico.Add(trabajo);
                            break;

                        case 3:
                            TrabajosReparacion.Add(trabajo);
                            break;

                        case 4:
                            TrabajosGarantia.Add(trabajo);
                            break;

                        case 5:
                            TrabajosReacondicionamiento.Add(trabajo);
                            break;
                    }
                }

                // 🔹 NOTIFICAR DASHBOARD
                NotificarCambiosDashboards();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error CargarTrabajos: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task ReanudarTrabajo(MiTrabajoDto trabajo)
        {
            if (trabajo == null)
                return;
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
            "Reanudar trabajo",
            $"¿Deseas reanudar el trabajo:\n{trabajo.VehiculoCompleto}\n{trabajo.Trabajo}?",
            "Reanudar",
            "Cancelar");
            if (!confirmar)
                return;


            try
            {
                IsLoading = true;

                int tecnicoId = Preferences.Get("user_id", 0);

                var response = await _apiService.ReanudarTrabajoAsync(
                    trabajo.Id,
                    tecnicoId
                );

                if (!response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        response.Message,
                        "OK");
                    return;
                }


                // ✅ Si es SERVICIO → navegar a checklist
                if (trabajo.TipoOrden == 1 && (trabajo.Trabajo == "1er Servicio" || trabajo.Trabajo == "2do Servicio" || trabajo.Trabajo == "3er Servicio" || trabajo.Trabajo == "Servicio Externo"))
                {
                    await NavegarACheckList(trabajo);
                }
                else
                {
                    switch (trabajo.TipoOrden)
                    {
                        case 1:
                            await NavegarACheckListReparacion(trabajo);
                            break;
                        case 2:
                            await NavegarACheckListDiagnostico(trabajo);
                            break;
                        case 3:
                            await NavegarACheckListReparacion(trabajo);
                            break;
                        case 4:
                            await NavegarACheckListGarantia(trabajo);
                            break;
                        case 5:
                            await NavegarACheckListReacondicionamiento();
                            break;
                    }

                    await Application.Current.MainPage.DisplayAlert(
                        "Trabajo Reanudado",
                        response.Message,
                        "OK");
                }

                await CargarTrabajos();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }

        }

        private async Task IniciarTrabajo(MiTrabajoDto trabajo)
        {
            if (trabajo == null)
                return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Iniciar trabajo",
                $"¿Deseas iniciar el trabajo:\n{trabajo.VehiculoCompleto}\n{trabajo.Trabajo}?",
                "Iniciar",
                "Cancelar");

            if (!confirmar)
                return;


            try
            {
                IsLoading = true;

                int tecnicoId = Preferences.Get("user_id", 0);

                var response = await _apiService.IniciarTrabajoAsync(
                    trabajo.Id,
                    tecnicoId
                );

                if (!response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        response.Message,
                        "OK");
                    return;
                }


                // ✅ Si es SERVICIO → navegar a checklist
                if (trabajo.TipoOrden == 1 && (trabajo.Trabajo == "1er Servicio" || trabajo.Trabajo == "2do Servicio" || trabajo.Trabajo == "3er Servicio" || trabajo.Trabajo == "Servicio Externo"))
                {
                    await NavegarACheckList(trabajo);
                }
                else
                {
                    switch (trabajo.TipoOrden)
                    {
                        case 1:
                            await NavegarACheckListReparacion(trabajo);
                            break;
                        case 2:
                            await NavegarACheckListDiagnostico(trabajo);
                            break;
                        case 3:
                            await NavegarACheckListReparacion(trabajo);
                            break;
                        case 4:
                            await NavegarACheckListGarantia(trabajo);
                            break;
                        case 5:
                            await NavegarACheckListReacondicionamiento();
                            break;
                    }

                    await Application.Current.MainPage.DisplayAlert(
                        "Trabajo iniciado",
                        response.Message,
                        "OK");
                }

                await CargarTrabajos();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }


        private void NotificarCambiosDashboards()
        {
            OnPropertyChanged(nameof(TrabajosServicio));
            OnPropertyChanged(nameof(TrabajosReparacion));
            OnPropertyChanged(nameof(TrabajosDiagnostico));
            OnPropertyChanged(nameof(TrabajosGarantia));
            OnPropertyChanged(nameof(TrabajosReacondicionamiento));
            OnPropertyChanged(nameof(HayTrabajosServicio));
            OnPropertyChanged(nameof(HayTrabajosReparacion));
            OnPropertyChanged(nameof(HayTrabajosDiagnostico));
            OnPropertyChanged(nameof(HayTrabajosGarantia));
            OnPropertyChanged(nameof(HayTrabajosReacondicionamiento));
            OnPropertyChanged(nameof(NoHayTrabajos));
            OnPropertyChanged(nameof(MensajeNoTrabajos));
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