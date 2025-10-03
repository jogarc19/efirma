using eFirmaApi.Common;

namespace eFirmaApi.Model
{
    public class Generic
    {
        public bool success { get; set; } = false;
        public string message { get; set; }
        public List<string> errors { get; set; }
    }
    public class GenericResponse<T> where T : class
    {
        public bool success { get; set; } = false;
        public string message { get; set; }
        public List<string> errors { get; set; }
        public T data { get; set; }
    }
    public class CertificateResponse
    {
        public List<Certificate> Certificates { get; set; }


        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }


        public string PdfBase64 { get; set; }
        /// <summary>
        /// esta propiedad es nueva, para obtener el idDocumento insertado y poder recuperarla JGG
        /// </summary>

        public int idDocumento { get; set; }

        public CertificateResponse()
        {
            ErrorMessage = string.Empty;
            PdfBase64 = string.Empty;
            IsSuccess = false;
            Certificates = new List<Certificate>();
        }

    }
    public class Certificate
    {
        public byte Secuence { get; set; }

        public CerticateType CertificateType { get; set; }

        public CertificateStatus CertificateStatus { get; set; }

        public string CertificateStatusDescription { get; set; }

        public string DateSignInfo { get; set; }

        public string DateProcessInfo { get; set; }

        public string CommonName { get; set; }

        public string Rfc { get; set; }

        public string SerieOcsp { get; set; }

        public string SerieFirma { get; set; }

        public string DateRevocateInfo { get; set; }

        public string SubjectName { get; set; }

        public string Pkcs7CheckSum { get; set; }

        public string Algorithm { get; set; }

        public string Organization { get; set; }

        public string OrganizationUnit { get; set; }

        public string FechaInicioVigencia { get; set; }

        public string FechaFinVigencia { get; set; }

        public byte[] OcspResponse { get; set; }
    }
    public class Firma_API_NOdeJSResponse
    {
        public IFormFile pkcs7Blob { get; set; }
    }
    public class FirmaResponse
    {
        public byte[] pkcs7FileContent { get; set; }
        public string pkcs7ContentType { get; set; }
        public string pkcs7FileName { get; set; }
        public byte[] evidenciaFileContent { get; set; }
        public string evidenciaContentType { get; set; }
        public string evidenciaFileName { get; set; }
        public CertificateResponse evidenciaCertificate { get; set; }
    }
    public class PFXInfoResponse
    {
        public string vigencia { get; set; }
    }
    
}
