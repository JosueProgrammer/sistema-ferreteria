using Sistema_Ferreteria.Models.Configuracion;

namespace Sistema_Ferreteria.ViewModels;

public class ConfiguracionSeccionViewModel
{
    public string NombreModulo { get; set; } = string.Empty;
    public List<Configuracion> Configuraciones { get; set; } = new();
}
