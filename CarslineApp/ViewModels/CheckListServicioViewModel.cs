using CarslineApp.Models;
using CarslineApp.Services;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CarslineApp.ViewModels
{
    public class CheckListServicioViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private bool _isLoading;
        private string _comentariosDireccion = string.Empty;
        private string _comentariosSuspension = string.Empty;
        private string _comentariosFrenado = string.Empty;
        private string _comentariosNeumaticos = string.Empty;
        private string _comentariosRefacciones = string.Empty;
        private string _comentariosNiveles = string.Empty;
        private string _comentariosTrabajos = string.Empty;
        private string _comentariosGenerales = string.Empty;
        private bool _trabajoFinalizado = false;


        public string Orden { get; }
        public string Vehiculo { get; }
        public string Indicaciones { get; }
        public string Trabajo { get; }
        public string VehiculoVIN { get; }

        public CheckListServicioModel CheckList { get; }

        public CheckListServicioViewModel(int trabajoId, int ordenId, string orden, string trabajo, string vehiculo, string indicaciones, string VIN)
        {
            _apiService = new ApiService();
            CheckList = new CheckListServicioModel
            {
                TrabajoId = trabajoId,
                OrdenId = ordenId,
                Trabajo = trabajo
            };


            Orden = orden;
            Vehiculo = vehiculo;
            Trabajo = trabajo;
            VehiculoVIN = VIN;

            GuardarCommand = new Command(async () => await GuardarCheckList(), () => !IsLoading);
        }
        public string ComentariosDireccion
        {
            get => _comentariosDireccion;
            set { _comentariosDireccion = value; OnPropertyChanged(); }
        }
        public string ComentariosSuspension
        {
            get => _comentariosSuspension;
            set { _comentariosSuspension = value; OnPropertyChanged(); }
        }
        public string ComentariosFrenado
        {
            get => _comentariosFrenado;
            set { _comentariosFrenado = value; OnPropertyChanged(); }
        }
        public string ComentariosNeumaticos
        {
            get => _comentariosNeumaticos;
            set { _comentariosNeumaticos = value; OnPropertyChanged(); }
        }
        public string ComentariosRefacciones
        {
            get => _comentariosRefacciones;
            set { _comentariosRefacciones = value; OnPropertyChanged(); }
        }
        public string ComentariosNiveles
        {
            get => _comentariosNiveles;
            set { _comentariosNiveles = value; OnPropertyChanged(); }
        }
        public string ComentariosTrabajos
        {
            get => _comentariosTrabajos;
            set { _comentariosTrabajos = value; OnPropertyChanged(); }
        }
        public string ComentariosGenerales
        {
            get => _comentariosGenerales;
            set { _comentariosGenerales = value; OnPropertyChanged(); }

        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                ((Command)GuardarCommand).ChangeCanExecute();
            }
        }

        public ICommand GuardarCommand { get; }

        public void SetValor(string campo, object valor)
        {
            var prop = typeof(CheckListServicioModel).GetProperty(campo);
            if (prop == null) return;

            try
            {
                if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(CheckList, valor?.ToString() ?? string.Empty);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    if (valor is string strValor)
                    {
                        // Convertir "true"/"false" string a bool
                        prop.SetValue(CheckList, strValor.ToLower() == "true");
                    }
                    else
                    {
                        prop.SetValue(CheckList, Convert.ToBoolean(valor));
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Campo '{campo}' = '{valor}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al asignar {campo}: {ex.Message}");
            }
        }
        private string ConstruirComentarios()
        {
            var sb = new System.Text.StringBuilder();

            void Agregar(string titulo, string contenido)
            {
                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    sb.AppendLine($"- {titulo}:");
                    sb.AppendLine(contenido.Trim());
                }
            }

            Agregar("Sistema de Direccion", ComentariosDireccion);
            Agregar("Sistema de Suspension ", ComentariosSuspension);
            Agregar("Sistema de Frenado", ComentariosFrenado);
            Agregar("Neumáticos", ComentariosNeumaticos);
            Agregar("Refacciones", ComentariosRefacciones);
            Agregar("Niveles", ComentariosNiveles);
            Agregar("Trabajos realizados", ComentariosTrabajos);
            Agregar("Comentarios generales", ComentariosGenerales);

            return sb.ToString().Trim();
        }

        
        public async Task RestablecerEstadoTrabajo()
        {
            if (_trabajoFinalizado) return; // Si ya finalizó, no hacer nada

            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Restableciendo trabajo {CheckList.TrabajoId} a PENDIENTE");
                int tecnicoId = Preferences.Get("user_id", 0);
                // Llamar al servicio para restablecer el estado
                var response = await _apiService.RestablecerTrabajoAsync(CheckList.TrabajoId, tecnicoId);

                if (!response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", response.Message, "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al restablecer estado: {ex.Message}");
            }
        }


        private async Task GuardarCheckList()
        {
            if (IsLoading) return;

            try
            {
                // Validar que todos los campos requeridos estén completos
                if (!ValidarCheckList())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "⚠️ Campos incompletos",
                        "Por favor completa todos los campos del checklist antes de Finalizar",
                        "OK");
                    return;
                }

                if (!ValidarTrabajos())
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "⚠️ Campos incompletos",
                        "Por favor revisa los niveles del vehiculo y realiza los trabajos Faltantes",
                        "OK");
                    return;
                }
                // Confirmar guardado
                bool confirmar = await Application.Current.MainPage.DisplayAlert(
                    "Confirmar",
                    "¿Estás seguro de Finalizar el checklist?\n\n✅ Esto marcará el trabajo como COMPLETADO",
                    "Sí, guardar",
                    "Cancelar");

                if (!confirmar) return;

                // 🧩 Construir comentario único
                CheckList.ComentariosTecnico = ConstruirComentarios();

                if (string.IsNullOrWhiteSpace(CheckList.ComentariosTecnico))
                {
                    CheckList.ComentariosTecnico = $"{CheckList.Trabajo} Realizado con Exito, el vehiculo no presenta fallas";
                }

                // 🔍 Debug opcional
                System.Diagnostics.Debug.WriteLine("📝 COMENTARIOS ENVIADOS:");
                System.Diagnostics.Debug.WriteLine(CheckList.ComentariosTecnico);

                IsLoading = true;

                // 🔍 Debug: Imprimir datos antes de enviar
                System.Diagnostics.Debug.WriteLine("📤 ENVIANDO CHECKLIST:");
                System.Diagnostics.Debug.WriteLine($"TrabajoId: {CheckList.TrabajoId}");
                System.Diagnostics.Debug.WriteLine($"OrdenId: {CheckList.OrdenId}");
                System.Diagnostics.Debug.WriteLine($"Trabajo: {CheckList.Trabajo}");

                // Guardar en el servidor
                var response = await _apiService.GuardarCheckListAsync(CheckList);

                if (response.Success)
                {
                    _trabajoFinalizado = true;

                    await Application.Current.MainPage.DisplayAlert(
                        "✅ Éxito",
                        "Checklist guardado exitosamente\n\nEl trabajo ha sido marcado como completado",
                        "OK");

                    // Regresar a la página anterior
                    await Application.Current.MainPage.Navigation.PopAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "❌ Error",
                        $"Error al Finalizar:\n{response.Message}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ EXCEPCIÓN: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");

                await Application.Current.MainPage.DisplayAlert(
                    "❌ Error",
                    $"Error al Finalizar:\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidarCheckList()
        {
            // Validar campos de string (sistema de dirección, suspensión, etc.)
            if (string.IsNullOrWhiteSpace(CheckList.Bieletas) ||
                string.IsNullOrWhiteSpace(CheckList.Terminales) ||
                string.IsNullOrWhiteSpace(CheckList.CajaDireccion) ||
                string.IsNullOrWhiteSpace(CheckList.Volante) ||
                string.IsNullOrWhiteSpace(CheckList.AmortiguadoresDelanteros) ||
                string.IsNullOrWhiteSpace(CheckList.AmortiguadoresTraseros) ||
                string.IsNullOrWhiteSpace(CheckList.BarraEstabilizadora) ||
                string.IsNullOrWhiteSpace(CheckList.Horquillas) ||
                string.IsNullOrWhiteSpace(CheckList.NeumaticosDelanteros) ||
                string.IsNullOrWhiteSpace(CheckList.NeumaticosTraseros) ||
                string.IsNullOrWhiteSpace(CheckList.Balanceo) ||
                string.IsNullOrWhiteSpace(CheckList.Alineacion) ||
                string.IsNullOrWhiteSpace(CheckList.LucesAltas) ||
                string.IsNullOrWhiteSpace(CheckList.LucesBajas) ||
                string.IsNullOrWhiteSpace(CheckList.LucesAntiniebla) ||
                string.IsNullOrWhiteSpace(CheckList.LucesReversa) ||
                string.IsNullOrWhiteSpace(CheckList.LucesDireccionales) ||
                string.IsNullOrWhiteSpace(CheckList.LucesIntermitentes) ||
                string.IsNullOrWhiteSpace(CheckList.DiscosTamboresDelanteros) ||
                string.IsNullOrWhiteSpace(CheckList.DiscosTamboresTraseros) ||
                string.IsNullOrWhiteSpace(CheckList.BalatasDelanteras) ||
                string.IsNullOrWhiteSpace(CheckList.BalatasTraseras))
            {
                return false;
            }

            return true;
        }
        private bool ValidarTrabajos()
        {
            if(CheckList.NivelLiquidoFrenos == false ||
                CheckList.NivelAnticongelante == false ||
                CheckList.NivelDepositoLimpiaparabrisas == false ||
                CheckList.NivelAceiteMotor == false ||
                //Trabajos realizados al vehiculo
                CheckList.DescristalizacionTamboresDiscos == false ||
                CheckList.AjusteFrenos == false ||
                CheckList.CalibracionPresionNeumaticos == false ||
                CheckList.TorqueNeumaticos == false)
            
            {
                return false;
            }

           return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}