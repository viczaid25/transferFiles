using System.ComponentModel.DataAnnotations;

namespace transferFiles.Models.ViewModels
{
    /// <summary>
    /// Datos del login de desarrollo (no se usa en producción: ahí autentica el hub).
    /// </summary>
    public class DevLoginVm
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
