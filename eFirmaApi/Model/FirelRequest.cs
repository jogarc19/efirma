using eFirmaApi.Common;
using System.ComponentModel.DataAnnotations;

namespace eFirmaApi.Model
{
    public class CertificateRequest
    {
        public string FileName { get; set; }


        public string Pkcs7Original { get; set; }


        public CerticateType CertificateType { get; set; }


        public string UserName { get; set; }
    }
    /// <summary>
    /// request para generar la evidencia de un pkcs7
    /// </summary>
    public class pkcs7FileRequest
    {
        /// <summary>
        /// Archivo pkcs7 de la cual se le va generar su evidencia
        /// </summary>
        public IFormFile? file { get; set; }
    }
    public class firmarRequest
    {
        /// <summary>
        /// Archivo (pdf, doc, docx) que se desea firmar
        /// </summary>
        public IFormFile? archivoFirmar { get; set; }
        /*/// <summary>
        /// Archivo pfx del usuario que desea firmar
        /// </summary>
        public IFormFile? archivoPfx_Efirma { get; set; }*/
        /// <summary>
        /// Identificador del usuario que desea firmar
        /// </summary>
        public int idUsuario    { get; set; }
        /// <summary>
        /// Contraseña que corresponde al archivo pfx
        /// </summary>
        public string? password_Efirma { get; set; }
    }
    public class MultifirmaRequest
    {
        /// <summary>
        /// Documento que se desea firmar (pdf, doc, docx)
        /// </summary>
        public IFormFile? archivoFirmar { get; set; }
        /*/// <summary>
        /// Un array que contenga la lista de archivos pfx de los usuarios con las que se desea firmar
        /// </summary>
        public IFormFile[]? archivoPfx_Efirma { get; set; }*/
        /// <summary>
        /// Una lista de idUsuario que van a firmar el documento
        /// </summary>
        public required int[] idUsuario { get; set; }
        /// <summary>
        /// Un array que contenga la lista de contraseñas que correspondan al array de pfxs y en el mimo orden.
        /// </summary>
        public string[]? password_Efirma { get; set; }
    }
    public class ValidafirmaReques
    {
        /// <summary>
        /// id del usuario
        /// </summary>
        public int idUsuario { get; set; }
        /// <summary>
        /// contraseña de la firma del usuario
        /// </summary>
        public string password { get; set; }
    }
    public class GuardarPFXrequest
    {
        /// <summary>
        /// Id del usuario
        /// </summary>
        public int idUsuario { set; get; }
        /// <summary>
        /// Contraseña de la firma del usuario
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// Archivo pfx de la firma del usuario
        /// </summary>
        public /*byte[]*/ IFormFile? pfxFileContent { get; set; }

    }
}
