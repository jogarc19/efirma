using System.ComponentModel.DataAnnotations;

namespace eFirmaApi.Model
{
    public class FIRMA_PFX
    {
        [Key]
        public int idUsuario { set; get; }
        public string pfxFileName { get; set; }
        public byte[] pfxFileContent { get; set; }
        public DateTime? pfxVigencia { get; set; }
        public DateTime? FechaAlta { get; set; }
    }
}
