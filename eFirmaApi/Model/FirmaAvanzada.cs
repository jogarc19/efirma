using eFirmaApi.Common;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace eFirmaApi.Model
{
    public class FirmaAvanzada
    {
        //private readonly IConfiguration _configuration;
        private readonly IhttpClientService _httpclient;
        public FirmaAvanzada(/*IConfiguration configuration,*/ IhttpClientService httpclient)
        {
            //_configuration = configuration;
            _httpclient = httpclient;
        }
        //CertificateRequest
        public async Task<CertificateResponse> GeneraEvidencia(IFormFile file)
        {
            CertificateRequest request = new CertificateRequest();
            CertificateResponse response = new CertificateResponse();


            request.CertificateType = Common.CerticateType.Multiple;
            //var base64Encoded= Encoding.UTF8.GetBytes(Convert.ToBase64String(file.ToByteArray()));

            //using (var ms = new MemoryStream())
            //{
            //    file.CopyTo(ms);
            //    var fileBytes = ms.ToArray();
            //    request.Pkcs7Original = Convert.ToBase64String(fileBytes);
            //}
            request.Pkcs7Original = Encoding.UTF8.GetString(file.ToByteArray());
            //byte[] bytes = Encoding.UTF8.GetBytes(base64String);

            //request.Pkcs7Original = Convert.ToBase64String(bytes);
            request.FileName = file.FileName;
            request.UserName = "Eliminar";

            //obtenemos la URL base del REST en el JSON
            //var apiUrlBase = _configuration["PJEO:urlFirmaEvidencia"];
            var apiUrlBase = staticClass.getURIGenerarEvidencia();


            var jsonParam = Newtonsoft.Json.JsonConvert.SerializeObject(request);
            //jsonParam.Replace("\\", "*");
            //var jsonParam = "{\"user\": \"" + sUser + "\",\"pass\": \"" + sPassword + "\"}";

            var responsehttp = _httpclient.requestWebApiMethodPost(apiUrlBase, "GenerarEvidencia", jsonParam).Result;
            if (responsehttp != "" && responsehttp != "faild")
            {
                response = Newtonsoft.Json.JsonConvert.DeserializeObject<CertificateResponse>(responsehttp);
            }
            else
            {
                response.IsSuccess = false;
                response.ErrorMessage = "No se pudo generar la evidencia";
            }
            return response;
        }
        public static X509Certificate2 isValidPFX(byte[] content, string password)
        {
            try
            {

                /*var cert = new X509Certificate2(content, password,
                    X509KeyStorageFlags.DefaultKeySet |
                    X509KeyStorageFlags.Exportable);*/
                
                var cert = new X509Certificate2(content, password,
                    X509KeyStorageFlags.EphemeralKeySet); // en produccion no es recomendable DefaultKeySet y si no se requiere persistencia tampoco se requiere el Exportable
                
                //return cert.HasPrivateKey;
                return cert;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
                //return null;
            }

        }
    }
}
