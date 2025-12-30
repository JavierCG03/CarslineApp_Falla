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
        private ObservableCollection<RefaccionDto> _refaccionesFiltradas = new();
        private string _textoBusqueda = string.Empty;
        private string _filtroTipo = "Todos";

        // Tipos de refacción comunes
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

        // Lista de filtros por tipo (incluye "Todos")
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

            // Comandos
            RefreshCommand = new Command(async () => await CargarRefacciones());
            AgregarRefaccionCommand = new Command(async () => await OnAgregarRefaccion());
            AumentarCommand = new Command<RefaccionDto>(async (r) => await AumentarCantidad(r));
            DisminuirCommand = new Command<RefaccionDto>(async (r) => await DisminuirCantidad(r));
            EliminarCommand = new Command<RefaccionDto>(async (r) => await EliminarRefaccion(r));
            VolverCommand = new Command(async () => await OnVolver());
            ExportarExcelCommand = new Command(async () => await ExportarExcel());
            AumentarUnoCommand = new Command<RefaccionDto>(async (r) => await AumentarUnoRapido(r));
            DisminuirUnoCommand = new Command<RefaccionDto>(async (r) => await DisminuirUnoRapido(r));
            BuscarCommand = new Command(async () => await BuscarRefaccion());
            LimpiarBusquedaCommand = new Command(() => LimpiarBusqueda());
        }

        #region Propiedades

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
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

        public ObservableCollection<RefaccionDto> Refacciones
        {
            get => _refacciones;
            set
            {
                _refacciones = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public ObservableCollection<RefaccionDto> RefaccionesFiltradas
        {
            get => _refaccionesFiltradas;
            set
            {
                _refaccionesFiltradas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HayRefacciones));
                OnPropertyChanged(nameof(CantidadMostrada));
            }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public string FiltroTipo
        {
            get => _filtroTipo;
            set
            {
                _filtroTipo = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public bool HayRefacciones => RefaccionesFiltradas?.Any() ?? false;

        public string CantidadMostrada => RefaccionesFiltradas?.Count.ToString() ?? "0";

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

        #endregion

        #region Métodos

        // ✅ NUEVO: Buscar refacción específica por número de parte
        private async Task BuscarRefaccion()
        {
            if (string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Búsqueda",
                    "Ingresa un número de parte para buscar",
                    "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var refaccion = await _apiService.BuscarPorNumeroParteAsync(TextoBusqueda.Trim());

                if (refaccion != null)
                {
                    // Mostrar solo la refacción encontrada
                    RefaccionesFiltradas = new ObservableCollection<RefaccionDto> { refaccion };
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "No encontrado",
                        $"No se encontró la refacción '{TextoBusqueda}'",
                        "OK");

                    // Recargar toda la lista
                    await CargarRefacciones();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al buscar: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ✅ NUEVO: Limpiar búsqueda y mostrar todo
        private void LimpiarBusqueda()
        {
            TextoBusqueda = string.Empty;
            FiltroTipo = "Todos";
            AplicarFiltros();
        }

        // ✅ NUEVO: Aplicar filtros de búsqueda y tipo
        private void AplicarFiltros()
        {
            if (Refacciones == null || !Refacciones.Any())
            {
                RefaccionesFiltradas = new ObservableCollection<RefaccionDto>();
                return;
            }

            var filtradas = Refacciones.AsEnumerable();

            // Filtrar por tipo
            if (FiltroTipo != "Todos")
            {
                filtradas = filtradas.Where(r => r.TipoRefaccion == FiltroTipo);
            }

            // Filtrar por texto de búsqueda
            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.ToLower();
                filtradas = filtradas.Where(r =>
                    (r.NumeroParte?.ToLower().Contains(busqueda) ?? false) ||
                    (r.TipoRefaccion?.ToLower().Contains(busqueda) ?? false) ||
                    (r.MarcaVehiculo?.ToLower().Contains(busqueda) ?? false) ||
                    (r.Modelo?.ToLower().Contains(busqueda) ?? false));
            }

            RefaccionesFiltradas = new ObservableCollection<RefaccionDto>(filtradas);
        }
        private async Task AumentarUnoRapido(RefaccionDto refaccion)
        {
            try
            {
                var response = await _apiService.AumentarCantidadAsync(refaccion.Id, 1);

                if (response.Success)
                {

                    // Recargar datos del servidor
                    await CargarRefacciones();
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
                    $"Error: {ex.Message}",
                    "OK");
            }
        }

        private async Task DisminuirUnoRapido(RefaccionDto refaccion)
        {
            if (refaccion.Cantidad == 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Advertencia",
                    "No hay stock disponible",
                    "OK");
                return;
            }

            try
            {
                var response = await _apiService.DisminuirCantidadAsync(refaccion.Id, 1);

                if (response.Success)
                {
                    // Actualizar localmente
                    refaccion.Cantidad -= 1;
                    OnPropertyChanged(nameof(RefaccionesFiltradas));

                    // Recargar datos del servidor
                    await CargarRefacciones();
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
                    $"Error: {ex.Message}",
                    "OK");
            }
        }

        private async Task ExportarExcel()
        {
            try
            {
                if (!HayRefacciones)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Sin datos",
                        "No hay refacciones para exportar",
                        "OK");
                    return;
                }

                IsLoading = true;

                // Generar contenido CSV (compatible con Excel)
                var csv = new System.Text.StringBuilder();

                // Encabezados
                csv.AppendLine("Número de Parte,Tipo,Marca,Modelo,Año,Cantidad,Fecha Registro,Última Modificación");

                // Datos - usar RefaccionesFiltradas para exportar solo lo visible
                foreach (var refaccion in RefaccionesFiltradas)
                {
                    csv.AppendLine($"\"{refaccion.NumeroParte}\"," +
                                  $"\"{refaccion.TipoRefaccion}\"," +
                                  $"\"{refaccion.MarcaVehiculo ?? ""}\"," +
                                  $"\"{refaccion.Modelo ?? ""}\"," +
                                  $"{refaccion.Anio?.ToString() ?? ""}," +
                                  $"{refaccion.Cantidad}," +
                                  $"{refaccion.FechaRegistro:yyyy-MM-dd}," +
                                  $"{refaccion.FechaUltimaModificacion:yyyy-MM-dd}");
                }

                // Guardar archivo
                var fileName = $"Inventario_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllTextAsync(filePath, csv.ToString());

                // Compartir archivo
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Exportar Inventario",
                    File = new ShareFile(filePath)
                });

                await Application.Current.MainPage.DisplayAlert(
                    "✅ Exportado",
                    $"Inventario exportado: {fileName}",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al exportar: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task InicializarAsync()
        {
            await CargarRefacciones();
        }

        private async Task CargarRefacciones()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                //var response = await _apiService.ObtenerRefaccionesAsync();
                var refacciones = await _apiService.ObtenerTodasRefaccionesAsync();
                Refacciones = new ObservableCollection<RefaccionDto>(refacciones);
                /*
                if (response == null || !response.Success)
                {
                    ErrorMessage = "No se pudieron cargar las refacciones";
                    return;
                }

                Refacciones = new ObservableCollection<RefaccionDto>(
                    response.Refacciones
                );*/
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    ErrorMessage,
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }


        private async Task OnAgregarRefaccion()
        {
            await Application.Current.MainPage.Navigation.PushAsync(
                new Views.AgregarRefaccionPage());
        }

        private async Task AumentarCantidad(RefaccionDto refaccion)
        {
            string result = await Application.Current.MainPage.DisplayPromptAsync(
                "Aumentar Stock",
                $"¿Cuántas unidades deseas agregar a {refaccion.NumeroParte}?",
                placeholder: "Cantidad",
                keyboard: Keyboard.Numeric,
                maxLength: 4);

            if (string.IsNullOrWhiteSpace(result))
                return;

            if (!int.TryParse(result, out int cantidad) || cantidad <= 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "Ingresa una cantidad válida",
                    "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var response = await _apiService.AumentarCantidadAsync(refaccion.Id, cantidad);

                if (response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Éxito",
                        response.Message,
                        "OK");
                    await CargarRefacciones();
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
                    $"Error: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DisminuirCantidad(RefaccionDto refaccion)
        {
            if (refaccion.Cantidad == 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Advertencia",
                    "No hay stock disponible",
                    "OK");
                return;
            }

            string result = await Application.Current.MainPage.DisplayPromptAsync(
                "Disminuir Stock",
                $"¿Cuántas unidades deseas quitar de {refaccion.NumeroParte}?\nStock actual: {refaccion.Cantidad}",
                placeholder: "Cantidad",
                keyboard: Keyboard.Numeric,
                maxLength: 4);

            if (string.IsNullOrWhiteSpace(result))
                return;

            if (!int.TryParse(result, out int cantidad) || cantidad <= 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "Ingresa una cantidad válida",
                    "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var response = await _apiService.DisminuirCantidadAsync(refaccion.Id, cantidad);

                if (response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Éxito",
                        response.Message,
                        "OK");
                    await CargarRefacciones();
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
                    $"Error: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task EliminarRefaccion(RefaccionDto refaccion)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirmar Eliminación",
                $"¿Estás seguro de eliminar la refacción {refaccion.NumeroParte}?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsLoading = true;

            try
            {
                var response = await _apiService.EliminarRefaccionAsync(refaccion.Id);

                if (response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Éxito",
                        "Refacción eliminada correctamente",
                        "OK");
                    await CargarRefacciones();
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
                    $"Error: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnVolver()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}