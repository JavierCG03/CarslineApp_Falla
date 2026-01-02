using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CarslineApp.Models;
using CarslineApp.Services;

namespace CarslineApp.ViewModels
{
    public class InventarioViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private ObservableCollection<RefaccionDto> _refacciones = new();
        private string _textoBusqueda = string.Empty;
        private string _filtroTipo = "Todos";
        private System.Threading.CancellationTokenSource? _searchCts;

        // ✅ Paginación
        private int _paginaActual = 1;
        private int _totalPaginas = 1;
        private bool _tieneMasPaginas = false;
        private bool _cargandoMas = false;
        private const int ITEMS_POR_PAGINA = 20;

        public List<string> TiposRefaccion { get; } = new()
        {
            "Filtro Aceite",
            "Filtro Aire Cabina",
            "Filtro Aire Motor",
            "Balatas delanteras",
            "Balatas traseras",
            "Bujias",
            "Amortiguadores",
            "Otro"
        };

        public List<string> TiposFiltro { get; } = new()
        {
            "Todos",
            "Filtro Aceite",
            "Filtro Aire Cabina",
            "Filtro Aire Motor",
            "Balatas delanteras",
            "Balatas traseras",
            "Bujias",
            "Amortiguadores",
            "Otro"
        };

        public InventarioViewModel()
        {
            _apiService = new ApiService();

            // Comandos básicos
            RefreshCommand = new Command(async () => await RefrescarDatos());
            AgregarRefaccionCommand = new Command(async () => await OnAgregarRefaccion());
            AumentarCommand = new Command<RefaccionDto>(async (r) => await AumentarCantidad(r));
            DisminuirCommand = new Command<RefaccionDto>(async (r) => await DisminuirCantidad(r));
            EliminarCommand = new Command<RefaccionDto>(async (r) => await EliminarRefaccion(r));
            VolverCommand = new Command(async () => await OnVolver());
            ExportarExcelCommand = new Command(async () => await ExportarExcel());
            AumentarUnoCommand = new Command<RefaccionDto>(async (r) => await AumentarUnoRapido(r));
            DisminuirUnoCommand = new Command<RefaccionDto>(async (r) => await DisminuirUnoRapido(r));
            LimpiarBusquedaCommand = new Command(() => LimpiarBusqueda());

            // ✅ Comando de búsqueda con debounce
            BuscarCommand = new Command(async () => await BuscarConDebounce());

            // ✅ Comando para cargar más (scroll infinito)
            CargarMasCommand = new Command(async () => await CargarMasPagina(), () => !CargandoMas && TieneMasPaginas);
        }

        #region Propiedades

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<RefaccionDto> Refacciones
        {
            get => _refacciones;
            set
            {
                if (_refacciones != value)
                {
                    _refacciones = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HayRefacciones));
                    OnPropertyChanged(nameof(CantidadMostrada));
                }
            }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                if (_textoBusqueda != value)
                {
                    _textoBusqueda = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FiltroTipo
        {
            get => _filtroTipo;
            set
            {
                if (_filtroTipo != value)
                {
                    _filtroTipo = value;
                    OnPropertyChanged();
                    _ = RefrescarDatos(); // Recargar con el nuevo filtro
                }
            }
        }

        public bool HayRefacciones => Refacciones?.Any() ?? false;
        public string CantidadMostrada => Refacciones?.Count.ToString() ?? "0";

        // ✅ Nuevas propiedades de paginación
        public int PaginaActual
        {
            get => _paginaActual;
            set
            {
                if (_paginaActual != value)
                {
                    _paginaActual = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalPaginas
        {
            get => _totalPaginas;
            set
            {
                if (_totalPaginas != value)
                {
                    _totalPaginas = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(InfoPaginacion));
                }
            }
        }

        public bool TieneMasPaginas
        {
            get => _tieneMasPaginas;
            set
            {
                if (_tieneMasPaginas != value)
                {
                    _tieneMasPaginas = value;
                    OnPropertyChanged();
                    ((Command)CargarMasCommand).ChangeCanExecute();
                }
            }
        }

        public bool CargandoMas
        {
            get => _cargandoMas;
            set
            {
                if (_cargandoMas != value)
                {
                    _cargandoMas = value;
                    OnPropertyChanged();
                    ((Command)CargarMasCommand).ChangeCanExecute();
                }
            }
        }

        public string InfoPaginacion => $"Página {PaginaActual} de {TotalPaginas}";

        #endregion

        #region Comandos

        public ICommand RefreshCommand { get; }
        public ICommand AgregarRefaccionCommand { get; }
        public ICommand AumentarCommand { get; }
        public ICommand DisminuirCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand VolverCommand { get; }
        public ICommand ExportarExcelCommand { get; }
        public ICommand AumentarUnoCommand { get; }
        public ICommand DisminuirUnoCommand { get; }
        public ICommand BuscarCommand { get; }
        public ICommand LimpiarBusquedaCommand { get; }
        public ICommand CargarMasCommand { get; }

        #endregion

        #region Métodos Principales

        public async Task InicializarAsync()
        {
            await CargarRefacciones();
        }

        // ✅ Método optimizado con paginación
        private async Task CargarRefacciones(bool esRecarga = false)
        {
            if (IsLoading) return;

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                System.Diagnostics.Debug.WriteLine("📥 Iniciando carga de refacciones...");

                if (esRecarga)
                {
                    PaginaActual = 1;
                    Refacciones.Clear();
                }

                string? terminoBusqueda = null;

                if (FiltroTipo != "Todos")
                {
                    terminoBusqueda = FiltroTipo;
                }
                else if (!string.IsNullOrWhiteSpace(TextoBusqueda))
                {
                    terminoBusqueda = TextoBusqueda.Trim();
                }

                System.Diagnostics.Debug.WriteLine($"🔍 Búsqueda: '{terminoBusqueda}', Página: {PaginaActual}");

                var response = await _apiService.ObtenerRefaccionesPaginadasAsync(
                    PaginaActual,
                    ITEMS_POR_PAGINA,
                    terminoBusqueda
                );

                System.Diagnostics.Debug.WriteLine($"📦 Respuesta recibida: Success={response?.Success}, Count={response?.Refacciones?.Count ?? 0}");

                if (response?.Success == true && response.Refacciones != null)
                {
                    TotalPaginas = response.TotalPaginas;
                    TieneMasPaginas = response.TienePaginaSiguiente;
                    Refacciones = new ObservableCollection<RefaccionDto>(response.Refacciones);

                    System.Diagnostics.Debug.WriteLine($"✅ {Refacciones.Count} refacciones cargadas");
                }
                else
                {
                    ErrorMessage = response?.Message ?? "No se pudieron cargar las refacciones";
                    Refacciones = new ObservableCollection<RefaccionDto>();

                    System.Diagnostics.Debug.WriteLine($"❌ Error: {ErrorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión";
                System.Diagnostics.Debug.WriteLine($"❌ HttpRequestException: {ex.Message}");
                await MostrarError("Sin conexión", "No se pudo conectar al servidor");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error inesperado";
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex}");
                await MostrarError("Error", $"Ocurrió un error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        // ✅ Cargar más páginas (scroll infinito)
        private async Task CargarMasPagina()
        {
            if (CargandoMas || !TieneMasPaginas || IsLoading) return;

            CargandoMas = true;

            try
            {
                PaginaActual++;

                string? terminoBusqueda = FiltroTipo != "Todos" ? FiltroTipo :
                    !string.IsNullOrWhiteSpace(TextoBusqueda) ? TextoBusqueda.Trim() : null;

                var response = await _apiService.ObtenerRefaccionesPaginadasAsync(
                    PaginaActual,
                    ITEMS_POR_PAGINA,
                    terminoBusqueda
                );

                if (response?.Success == true && response.Refacciones != null)
                {
                    TotalPaginas = response.TotalPaginas;
                    TieneMasPaginas = response.TienePaginaSiguiente;

                    // Agregar nuevos items a la lista existente
                    foreach (var refaccion in response.Refacciones)
                    {
                        Refacciones.Add(refaccion);
                    }
                }
            }
            catch (Exception ex)
            {
                PaginaActual--; // Revertir el incremento
                System.Diagnostics.Debug.WriteLine($"Error al cargar más: {ex.Message}");
            }
            finally
            {
                CargandoMas = false;
            }
        }

        // ✅ Búsqueda con debounce (evita búsquedas mientras se escribe)
        private async Task BuscarConDebounce()
        {
            // Cancelar búsqueda anterior si existe
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();

            try
            {
                // Esperar 500ms antes de buscar
                await Task.Delay(500, _searchCts.Token);
                await RefrescarDatos();
            }
            catch (TaskCanceledException)
            {
                // Búsqueda cancelada, ignorar
            }
        }

        // ✅ Refrescar datos (llamar siempre esto en lugar de CargarRefacciones directamente)
        private async Task RefrescarDatos()
        {
            await CargarRefacciones(esRecarga: true);
        }

        private void LimpiarBusqueda()
        {
            TextoBusqueda = string.Empty;
            FiltroTipo = "Todos";
            _ = RefrescarDatos();
        }

        #endregion

        #region Operaciones CRUD

        private async Task AumentarUnoRapido(RefaccionDto refaccion)
        {
            if (refaccion == null) return;

            try
            {
                var response = await _apiService.AumentarCantidadAsync(refaccion.Id, 1);

                if (response.Success)
                {
                    // Actualizar localmente
                    refaccion.Cantidad += 1;
                    OnPropertyChanged(nameof(Refacciones));
                }
                else
                {
                    await MostrarError("Error", response.Message);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error", $"No se pudo aumentar: {ex.Message}");
            }
        }

        private async Task DisminuirUnoRapido(RefaccionDto refaccion)
        {
            if (refaccion == null || refaccion.Cantidad == 0) return;

            try
            {
                var response = await _apiService.DisminuirCantidadAsync(refaccion.Id, 1);

                if (response.Success)
                {
                    // Actualizar localmente
                    refaccion.Cantidad -= 1;
                    OnPropertyChanged(nameof(Refacciones));
                }
                else
                {
                    await MostrarError("Error", response.Message);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error", $"No se pudo disminuir: {ex.Message}");
            }
        }

        private async Task AumentarCantidad(RefaccionDto refaccion)
        {
            if (refaccion == null) return;

            string? result = await Application.Current.MainPage.DisplayPromptAsync(
                "Aumentar Stock",
                $"¿Cuántas unidades deseas agregar a {refaccion.NumeroParte}?",
                placeholder: "Cantidad",
                keyboard: Keyboard.Numeric,
                maxLength: 4);

            if (string.IsNullOrWhiteSpace(result)) return;

            if (!int.TryParse(result, out int cantidad) || cantidad <= 0)
            {
                await MostrarError("Error", "Ingresa una cantidad válida");
                return;
            }

            try
            {
                var response = await _apiService.AumentarCantidadAsync(refaccion.Id, cantidad);

                if (response.Success)
                {
                    await MostrarExito("Éxito", response.Message);
                    await RefrescarDatos();
                }
                else
                {
                    await MostrarError("Error", response.Message);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error", $"Error: {ex.Message}");
            }
        }

        private async Task DisminuirCantidad(RefaccionDto refaccion)
        {
            if (refaccion == null || refaccion.Cantidad == 0)
            {
                await MostrarError("Advertencia", "No hay stock disponible");
                return;
            }

            string? result = await Application.Current.MainPage.DisplayPromptAsync(
                "Disminuir Stock",
                $"¿Cuántas unidades deseas quitar?\nStock actual: {refaccion.Cantidad}",
                placeholder: "Cantidad",
                keyboard: Keyboard.Numeric,
                maxLength: 4);

            if (string.IsNullOrWhiteSpace(result)) return;

            if (!int.TryParse(result, out int cantidad) || cantidad <= 0)
            {
                await MostrarError("Error", "Ingresa una cantidad válida");
                return;
            }

            try
            {
                var response = await _apiService.DisminuirCantidadAsync(refaccion.Id, cantidad);

                if (response.Success)
                {
                    await MostrarExito("Éxito", response.Message);
                    await RefrescarDatos();
                }
                else
                {
                    await MostrarError("Error", response.Message);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error", $"Error: {ex.Message}");
            }
        }

        private async Task EliminarRefaccion(RefaccionDto refaccion)
        {
            if (refaccion == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                $"¿Eliminar {refaccion.NumeroParte}?",
                "Sí",
                "No");

            if (!confirm) return;

            IsLoading = true;

            try
            {
                var response = await _apiService.EliminarRefaccionAsync(refaccion.Id);

                if (response.Success)
                {
                    await MostrarExito("Éxito", "Refacción eliminada");
                    await RefrescarDatos();
                }
                else
                {
                    await MostrarError("Error", response.Message);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error", $"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Otros Métodos

        private async Task OnAgregarRefaccion()
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new Views.AgregarRefaccionPage());
        }

        private async Task OnVolver()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }

        private async Task ExportarExcel()
        {
            try
            {
                if (!HayRefacciones)
                {
                    await MostrarError("Sin datos", "No hay refacciones para exportar");
                    return;
                }

                IsLoading = true;

                // Generar CSV
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Número,Tipo,Marca,Modelo,Año,Cantidad,Registro,Modificación");

                foreach (var r in Refacciones)
                {
                    csv.AppendLine($"\"{r.NumeroParte}\"," +
                                  $"\"{r.TipoRefaccion}\"," +
                                  $"\"{r.MarcaVehiculo ?? ""}\"," +
                                  $"\"{r.Modelo ?? ""}\"," +
                                  $"{r.Anio?.ToString() ?? ""}," +
                                  $"{r.Cantidad}," +
                                  $"{r.FechaRegistro:yyyy-MM-dd}," +
                                  $"{r.FechaUltimaModificacion:yyyy-MM-dd}");
                }

                var fileName = $"Inventario_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllTextAsync(filePath, csv.ToString());

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Exportar Inventario",
                    File = new ShareFile(filePath)
                });

                await MostrarExito("Exportado", $"Archivo: {fileName}");
            }
            catch (Exception ex)
            {
                await MostrarError("Error", $"Error al exportar: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Métodos auxiliares para mostrar alertas
        private async Task MostrarError(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "OK");
            }
        }

        private async Task MostrarExito(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "OK");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}