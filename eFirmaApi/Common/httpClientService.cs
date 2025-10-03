using eFirmaApi.Model;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace eFirmaApi.Common
{
    public interface IhttpClientService
    {
        //Task<string> requestAuthentication();
        //Task<string> requestSkuProducto(List<TBL_SAPPrecios> precios);
        Task<string> requestWebApiMethodPost(string apiUrlBase, string methodApi, string jsonParam);
        Task<string> requestWebApiMethodGet(string apiUrlBase, string methodApi, string jsonParam);

        Task<string> requestWebApiNasLog_in_out(string apiUrl);
        Task<GenericResponse<Firma_API_NOdeJSResponse>> requestApiFirmaPkcs7(string apiUrlBase, MultipartFormDataContent content, string ApiKey = "");
    }
    public class httpClientService : IhttpClientService
    {
        //private readonly ILogDatabaseService _logDatabase;
        private readonly IConfiguration _configuration;

        static string g_sToken;
        public httpClientService(
            IConfiguration configuration)
        {

            _configuration = configuration;


        }


        public async Task<string> requestWebApiMethodPost(string apiUrlBase, string methodApi, string jsonParam)
        {
            Task<string> sMessage = Task.FromResult("faild");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };

            var data = new StringContent(jsonParam, Encoding.UTF8, "application/json");
            //string logPath = @"e:\logs\miLog.txt";


            HttpClientHandler handler = new HttpClientHandler();
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    var apiUrl = $"{apiUrlBase}{methodApi}";

                    httpClient.BaseAddress = new Uri(apiUrl);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.Timeout = TimeSpan.FromSeconds(600);

                    //using (StreamWriter writer = new StreamWriter(logPath, append: true)) // "append: true" para no sobrescribir
                    //{
                    //    writer.WriteLine("llamando...");
                    //}
                    var respuesta = await httpClient.PostAsync(methodApi, data);

                    //using (StreamWriter writer = new StreamWriter(logPath, append: true)) // "append: true" para no sobrescribir
                    //{
                    //    writer.WriteLine("finalizó: "+ respuesta.Content.Headers.ToString() + "----" + respuesta.Content.ReadAsStringAsync().Result);
                    //}
                    string result = await respuesta.Content.ReadAsStringAsync();

                    //EventLog.WriteEntry(@"e:\log2.txt", respuesta.Content.ReadAsStringAsync().Result);
                    return result;

                }
                catch (Exception ex)
                {

                    //using (StreamWriter writer = new StreamWriter(logPath, append: true)) // "append: true" para no sobrescribir
                    //{
                    //    writer.WriteLine(ex.Message);
                    //}
                    throw new Exception(ex.Message, ex);
                    //_logDatabase.logError("httpClientService", "requestWebApiMethodPost", ex.Message);
                }
                return await sMessage;
            }

        }
        public async Task<string> requestWebApiMethodGet(string apiUrlBase, string methodApi, string jsonParam)
        {
            Task<string> sMessage = Task.FromResult("faild");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };

            var data = new StringContent(jsonParam, Encoding.UTF8, "application/json");



            HttpClientHandler handler = new HttpClientHandler();
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    var apiUrl = $"{apiUrlBase}{methodApi}";

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", g_sToken);

                    httpClient.BaseAddress = new Uri(apiUrl);
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    var respuesta = await httpClient.GetAsync(methodApi);
                    string result = await respuesta.Content.ReadAsStringAsync();
                    return result;

                }
                catch (Exception ex)
                {
                    //_logDatabase.logError("httpClientService", "requestWebApiMethodPost", ex.Message);
                }
                return await sMessage;
            }

        }
        public async Task<string> requestWebApiNasLog_in_out(string apiUrl)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            string result = "";


            HttpClientHandler handler = new HttpClientHandler();
            using (var httpClient = new HttpClient(handler))
            {
                try
                {

                    //string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(ApiKey));
                    //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", ApiKey);

                    httpClient.BaseAddress = new Uri(apiUrl);


                    var respuesta = await httpClient.GetAsync(apiUrl);
                    if (Convert.ToInt32(respuesta.StatusCode) == StatusCodes.Status200OK)
                    {
                        result = await respuesta.Content.ReadAsStringAsync();
                    }
                    else
                    {

                        Generic obj = new Generic
                        {
                            success = false,
                            message = (Convert.ToInt32(respuesta.StatusCode)).ToString() + " " + respuesta.StatusCode.ToString(),
                            errors = new List<string> { await respuesta.Content.ReadAsStringAsync() }
                        };
                        result = JsonConvert.SerializeObject(obj);

                    }
                    return result;

                }
                catch (Exception ex)
                {
                    //_logDatabase.logError("httpClientService", "requestWebApiMethodPost", ex.Message);
                    throw new Exception(ex.Message);
                }

            }

        }
        

        public async Task<GenericResponse<Firma_API_NOdeJSResponse>> requestApiFirmaPkcs7(string apiUrlBase, MultipartFormDataContent content, string ApiKey = "")
        {
            GenericResponse<Firma_API_NOdeJSResponse> response = new GenericResponse<Firma_API_NOdeJSResponse>();
            //System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;


            //var data = new StringContent(jsonParam, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage();
            request.Content = content;
            var header = new ContentDispositionHeaderValue("form-data");
            request.Content.Headers.ContentDisposition = header;

            HttpClientHandler handler = new HttpClientHandler();
            //handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    var apiUrl = $"{apiUrlBase}";

                    httpClient.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
                    httpClient.BaseAddress = new Uri(apiUrl);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                    var respuesta = await httpClient.PostAsync(apiUrl, request.Content);
                    if (Convert.ToInt32(respuesta.StatusCode) == StatusCodes.Status200OK)
                    {

                        response.message = "OK";
                        response.success = true;
                        response.data = new Firma_API_NOdeJSResponse();

                        //System.IO.File.WriteAllText(@"E:\log1.txt", respuesta.Content.ReadAsStringAsync().Result.ToString());
                        byte[] bytes = await respuesta.Content.ReadAsByteArrayAsync();

                        // Obtener las cabeceras
                        var contentDisposition = respuesta.Content.Headers.ContentDisposition;
                        var contentType = respuesta.Content.Headers.ContentType;
                        string nombre = respuesta.Content.Headers.ContentDisposition?.FileName.Replace("\"", "");

                        // Crear un MemoryStream a partir de los bytes del PDF
                        var memoryStream = new MemoryStream(bytes);
                        IFormFile formFile = new FormFile(memoryStream, 0, bytes.Length, nombre, nombre)
                        {
                            Headers = new HeaderDictionary
                            {
                                { "Content-Disposition", contentDisposition.ToString() },
                                { "Content-Type", contentType.ToString() }
                            }
                        };
                        response.data.pkcs7Blob = formFile;
                        // Asegúrate de que el MemoryStream no se cierre prematuramente
                        memoryStream.Position = 0;

                        /*using (var memoryStream = new MemoryStream(bytes))
                        {
                            // Crear un IFormFile
                            IFormFile formFile = new FormFile(memoryStream, 0, bytes.Length, nombre, nombre)
                            {
                                Headers = new HeaderDictionary
                                {
                                    { "Content-Disposition", contentDisposition.ToString() },
                                    { "Content-Type", contentType.ToString() }
                                }
                            };
                            response.data.pkcs7Blob = formFile;
                            // Aquí puedes usar formFile según lo necesites
                            // Por ejemplo, guardarlo o procesarlo en tu aplicación
                            //Console.WriteLine($"Archivo: {formFile.FileName}");
                            //Console.WriteLine($"Content-Disposition: {formFile.ContentDisposition}");
                            //Console.WriteLine($"Content-Type: {formFile.ContentType}");
                        }*/

                    }
                    else
                    {
                        response.message = $"Surgió un error al intentar firmar: => {(Convert.ToInt32(respuesta.StatusCode)).ToString()} {respuesta.StatusCode.ToString()}";
                        response.errors = new List<string> { await respuesta.Content.ReadAsStringAsync() };

                    }

                }
                catch (Exception ex)
                {
                    //_logDatabase.logError("httpClientService", "requestWebApiMethodPost", ex.Message);
                    throw new Exception(ex.Message);
                }

            }
            return response;
        }

    }
}
