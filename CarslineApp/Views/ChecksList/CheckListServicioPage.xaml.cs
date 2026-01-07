using CarslineApp.ViewModels;

namespace CarslineApp.Views
{
    public partial class CheckListServicioPage : ContentPage
    {
        private readonly CheckListServicioViewModel _viewModel;

        public CheckListServicioPage(int trabajoId, int ordenId,string orden, string trabajo, string vehiculo, string Indicaciones, string VIN)
        {
            InitializeComponent();
            _viewModel = new CheckListServicioViewModel(trabajoId, ordenId, orden, trabajo, vehiculo, Indicaciones,VIN);
            BindingContext = _viewModel;
        }

        private void Radio_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;

            var radio = (RadioButton)sender;
            _viewModel.SetValor(radio.GroupName, radio.Value);
        }
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await _viewModel.RestablecerEstadoTrabajo();
        }
    }
}