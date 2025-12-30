

namespace CarslineApp.Models
{
    // ============================================
    // MODELOS DE REFACCIONES (SIN CAMBIOS)
    // ============================================

    public class RefaccionDto
    {
        public int Id { get; set; }
        public string NumeroParte { get; set; } = string.Empty;
        public string TipoRefaccion { get; set; } = string.Empty;
        public string? MarcaVehiculo { get; set; }
        public string? Modelo { get; set; }
        public int? Anio { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }

        public string DescripcionCompleta => $"{TipoRefaccion}" +
            (string.IsNullOrEmpty(MarcaVehiculo) ? "" : $" - {MarcaVehiculo}") +
            (string.IsNullOrEmpty(Modelo) ? "" : $" {Modelo}") +
            (Anio.HasValue ? $" {Anio}" : "");

        public Color ColorStock => Cantidad == 0 ? Colors.Red :
                                   Cantidad < 5 ? Color.FromArgb("#E67E22") :
                                   Color.FromArgb("#27AE60");

        public Color ColorStockFondo => Cantidad == 0 ? Color.FromArgb("#FADBD8") :
                                        Cantidad < 5 ? Color.FromArgb("#FCF3CF") :
                                        Color.FromArgb("#D5F4E6");

        public Color ColorTipo
        {
            get
            {
                var tipo = TipoRefaccion?.ToLower() ?? string.Empty;

                return tipo switch
                {
                    var t when t.Contains("balatas delanteras") => Color.FromArgb("#E74C3C"),
                    var t when t.Contains("balatas traseras") => Color.FromArgb("#C0392B"),
                    var t when t.Contains("filtro aceite") => Color.FromArgb("#F39C12"),
                    var t when t.Contains("filtro aire cabina") => Color.FromArgb("#3498DB"),
                    var t when t.Contains("filtro aire motor") => Color.FromArgb("#2980B9"),
                    _ => Color.FromArgb("#95A5A6")
                };
            }
        }


        public string IconoTipo
        {
            get
            {
                var tipo = TipoRefaccion?.ToLower() ?? string.Empty;

                return tipo switch
                {
                    var t when t.Contains("balatas") => "🔴",
                    var t when t.Contains("filtro aceite") => "🛢️",
                    var t when t.Contains("filtro aire") => "💨",
                    _ => "🔧"
                };
            }
        }


        public string TextoStock => $"Stock: {Cantidad}";
    }

    public class CrearRefaccionRequest
    {
        public string NumeroParte { get; set; } = string.Empty;
        public string TipoRefaccion { get; set; } = string.Empty;
        public string? MarcaVehiculo { get; set; }
        public string? Modelo { get; set; }
        public int? Anio { get; set; }
        public int Cantidad { get; set; }
    }

    public class RefaccionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public RefaccionDto? Refaccion { get; set; }
    }
    // ✅ NUEVO DTO para respuesta paginada
    public class RefaccionesPaginadasResponse
    {
        public bool Success { get; set; }
        public List<RefaccionDto> Refacciones { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItems { get; set; }
        public int PorPagina { get; set; }
        public bool TienePaginaAnterior { get; set; }
        public bool TienePaginaSiguiente { get; set; }
    }
}
