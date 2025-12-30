using CarslineApp.ViewModels;

namespace CarslineApp.Views
{
    public partial class CheckListServicioPage : ContentPage
    {
        private readonly CheckListServicioViewModel _viewModel;

        public CheckListServicioPage(int trabajoId, int ordenId, string trabajo, string vehiculo)
        {
            InitializeComponent();

            Title = $"{trabajo} - {vehiculo}";
            _viewModel = new CheckListServicioViewModel(trabajoId, ordenId, trabajo);
            BindingContext = _viewModel;
        }

        private void Radio_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;

            var radio = (RadioButton)sender;
            _viewModel.SetValor(radio.GroupName, radio.Value);
        }
    }
}