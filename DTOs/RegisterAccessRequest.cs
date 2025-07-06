using System.ComponentModel.DataAnnotations;


namespace ControlAccesos.WebApi.DTOs
{
        public class RegisterAccessRequest : IValidatableObject 
        {
            [StringLength(10, ErrorMessage = "El código QR del invitado no puede exceder los 10 caracteres.")]
            public string? InvitadoQrCode { get; set; } // Para identificar a un invitado

            [StringLength(50, ErrorMessage = "El nombre de usuario del residente no puede exceder los 50 caracteres.")]
            public string? ResidentUsername { get; set; } // Para identificar a un residente por su usuario

            [Required(ErrorMessage = "El tipo de acceso (Entrada/Salida) es requerido.")]
            [StringLength(20, ErrorMessage = "El tipo de acceso no puede exceder los 20 caracteres.")]
            public string TipoAcceso { get; set; } // "Entrada" o "Salida"

            [StringLength(20, ErrorMessage = "Las placas del vehículo no pueden exceder los 20 caracteres.")]
            public string? PlacasVehiculo { get; set; } // Placas del vehículo que está entrando/saliendo

            [StringLength(500, ErrorMessage = "Las notas no pueden exceder los 500 caracteres.")]
            public string? Notas { get; set; } // Notas adicionales del guardia

            // Validación personalizada para asegurar que exactamente un identificador sea proporcionado
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                int identifiersProvided = 0;
                if (!string.IsNullOrWhiteSpace(InvitadoQrCode)) identifiersProvided++;
                if (!string.IsNullOrWhiteSpace(ResidentUsername)) identifiersProvided++;

                if (identifiersProvided == 0)
                {
                    yield return new ValidationResult(
                        "Se debe proporcionar exactamente un método de identificación: 'InvitadoQrCode' o 'ResidentUsername'.",
                        new[] { nameof(InvitadoQrCode), nameof(ResidentUsername) });
                }
                
                yield return new ValidationResult(
                    "Solo se debe proporcionar un método de identificación: 'InvitadoQrCode' o 'ResidentUsername'.",
                    new[] { nameof(InvitadoQrCode), nameof(ResidentUsername)});
                
            }
        }
    
}
