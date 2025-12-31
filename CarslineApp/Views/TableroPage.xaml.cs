using System.Collections.ObjectModel;
using CarslineApp.Models;
using CarslineApp.Services;

namespace CarslineApp.Views;
public partial class TableroPage : ContentPage
{
    private readonly ApiService _apiService;

    public ObservableCollection<TecnicoTableroDetalle> Tecnicos { get; set; } = new();

    public TableroPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarTableroAsync();
    }

    private async Task CargarTableroAsync()
    {
        Tecnicos.Clear();

        var trabajos = await _apiService.ObtenerTrabajosTecnicosAsync();
        if (trabajos == null) return;

        var grupos = trabajos
            .Where(t => !string.IsNullOrWhiteSpace(t.TecnicoNombre))
            .GroupBy(t => t.TecnicoNombre);

        foreach (var grupo in grupos)
        {
            var tecnico = new TecnicoTableroDetalle
            {
                TecnicoNombre = grupo.Key
            };

            foreach (var t in grupo.OrderBy(x => x.FechaHoraPromesaEntrega))
            {
                tecnico.Trabajos.Add(new TrabajoTableroItem
                {
                    Trabajo = t.Trabajo,
                    Estado = t.EstadoTrabajoNombre,
                    FechaEntrega = t.FechaHoraPromesaEntrega
                });
            }

            Tecnicos.Add(tecnico);
        }
    }
}
