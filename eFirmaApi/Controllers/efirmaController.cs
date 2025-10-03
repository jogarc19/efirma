using Azure;
using Azure.Core;
using eFirmaApi.Common;
using eFirmaApi.Context;
using eFirmaApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Net.Http.Headers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace eFirmaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class efirmaController : ControllerBase
    {
        private readonly DBContextNotificacionAPI _DBContextNotificacionAPI;
        private readonly IhttpClientService _httpclient;
        public efirmaController(DBContextNotificacionAPI dbContextNotificacionAPI, IhttpClientService httpclient) 
        {
            _DBContextNotificacionAPI = dbContextNotificacionAPI;
            _httpclient = httpclient;
        }
        /// <summary>
        /// Endpoint para generar una evidencia
        /// </summary>
        /// <remarks>
        /// Usar este endpoint cuando se tiene un archivo pkcs y se desea generar una evidencia legible para el usuario final
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        // POST api/<FirmaElectronicaController>
        [HttpPost("Evidencia")]
        public async Task<CertificateResponse> GeneraEvidencia([FromForm] pkcs7FileRequest request)
        {
            CertificateResponse response = new CertificateResponse();
            if (request == null)
            {
                response.ErrorMessage = "El archivo pkcs7 es obligatorio";
                return response;
            }
            if (request.file == null)
            {
                response.ErrorMessage = "La propiedad file es obligatorio";
                return response;
            }
            FirmaAvanzada firma = new FirmaAvanzada(_httpclient);
            response = await firma.GeneraEvidencia(request.file);
            return response;
        }
        /// <summary>
        /// Endpoint para firmar un documento con una sola firma
        /// </summary>
        /// <remarks>
        /// Utilizar este endpoint cuando se desea firmar un documento (pdf, doc, docx) con una sola firma
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Firmar")]
        public async Task<IActionResult> firma([FromForm] firmarRequest request)
        {
            GenericResponse<FirmaResponse> response = new GenericResponse<FirmaResponse>();
            response.errors = new List<string>();
            //primero validamos el request
            if (request.archivoFirmar == null || request.archivoFirmar.Length == 0)
            {
                response.errors.Add("Debe especificar la propiedad archivoFirmar");
            }
            /*if (request.archivoPfx_Efirma == null || request.archivoPfx_Efirma.Length == 0)
            {
                response.errors.Add("Debe especificar la propiedad archivoPfx_Efirma");
            }*/
            if(request.idUsuario<=0)
                response.errors.Add("Debe especificar la propiedad idUsuario");

            if (string.IsNullOrEmpty(request.password_Efirma))
            {
                response.errors.Add("Debe especificar la propiedad password_Efirma");
            }

            if (response.errors.Count > 0)
            {
                response.message = "Algunas propiedades son obligatorias, favor de verificar";
                return Ok(response);
            }
            // Lista de extensiones permitidas
            var permittedExtensions = new[] { ".pdf", ".doc", ".docx" };

            // Obtén la extensión del archivo a firmar
            var ext = Path.GetExtension(request.archivoFirmar!.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            {
                response.errors.Add("archivoFirmar: (extensión) tipo de archivo no válido solo se permiten pdf, doc, docx");
            }

            // Lista de tipos MIME permitidos
            var permittedMimeTypes = new[] { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

            // Verifica el tipo MIME del archivo
            if (!permittedMimeTypes.Contains(request.archivoFirmar.ContentType))
            {
                response.errors.Add("archivoFirmar: (MimeTypes) tipo de archivo no válido solo se permiten pdf, doc, docx ");
            }
            // Obtén la extensión del archivo a pfx
            /*var extPfx = Path.GetExtension(request.archivoPfx_Efirma!.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extPfx) || !".pfx".Contains(extPfx))
            {
                response.errors.Add("archivoPfx_Efirma: El archivo efirma debe ser de tipo .pfx ");
            }
            */

            if (response.errors.Count > 0)
            {
                response.message = "Algunas propiedades no cumplen con los requisitos, favor de verificar";
                return Ok(response);
            }
            response.errors = null;
            ///fin de validacion request

            var content = new MultipartFormDataContent();

            content.Add(new StringContent(request.password_Efirma), "pass");

            //leemos los archivos
            var streamFile = request.archivoFirmar!.OpenReadStream();
            //var streamPfx = request.archivoPfx_Efirma!.OpenReadStream();
            try
            {
                var pfxUsr = _DBContextNotificacionAPI.FIRMA_PFX.Find(request.idUsuario);
                if (pfxUsr == null)
                {
                    response.message = $"El usuario: {request.idUsuario} no existe";
                    return Ok(response);
                }
                if(pfxUsr.pfxFileContent == null)
                {
                    response.message = $"El usuario: {request.idUsuario} no cuenta con su eFIRMA";
                    return Ok(response);
                }

                byte[] file = pfxUsr.pfxFileContent;
                using var streamPfx = new MemoryStream(file);
                //cargamos los archivos al content para enviarlos al request
                content.Add(new StreamContent(streamFile), "ArchivoFirmar", request.archivoFirmar.FileName);
                content.Add(new StreamContent(streamPfx), "ArchivoPFX", pfxUsr.pfxFileName);

                GenericResponse<Firma_API_NOdeJSResponse> responseFirma = new GenericResponse<Firma_API_NOdeJSResponse>();
                //invocamos la api que genera el pkcs7
                responseFirma = await _httpclient.requestApiFirmaPkcs7(staticClass.getURIGenerarPKCS7() + "upload", content);

                //si falla asignamos los mensajes y finaliza
                if (!responseFirma.success)
                {
                    response.message = responseFirma.message;
                    response.errors = responseFirma.errors;
                }
                else
                {
                    //si llega aqui es porque el pkcs7 si se generó satisfactoriamente

                    //creamos el objeto request que vamos a enviar para generar la evidencia
                    pkcs7FileRequest evidenciaRequest = new pkcs7FileRequest();

                    //el pkcs7 biene en un arreglo de bytes, y aqui lo convertmos en IFormFile
                    //var streamPkcs7 = new MemoryStream(responseFirma.data.pkcs7Blob);
                    //IFormFile filePkcs7 = new FormFile(streamPkcs7, 0, responseFirma.data.pkcs7Blob.Length, request.file.Name, "pkcs7_"+request.file.FileName);

                    //
                    //asignamos el pkcs7 para generar su evidencia
                    evidenciaRequest.file = responseFirma.data.pkcs7Blob;
                    //System.IO.File.WriteAllText(@"E:\log.txt", evidenciaRequest.file.Length.ToString());
                    response.data = new FirmaResponse();

                    //comenzamos a rellenar el response
                    byte[] fileBytes;

                    using (var memoryStream = new MemoryStream())
                    {
                        await responseFirma.data.pkcs7Blob.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    response.data.pkcs7FileContent = fileBytes;
                    response.data.pkcs7ContentType = request.archivoFirmar.ContentType;
                    response.data.pkcs7FileName = request.archivoFirmar.FileName.Split(".")[0].Replace(" ", "_") + "_pkcs7." + request.archivoFirmar.FileName.Split(".")[1];


                    //creamos el objeto response que devuelve el endint de generar evidencia
                    CertificateResponse responseEvidencia = new CertificateResponse();
                    //invocamos el endpoint que genera la evidencia, le pasamos como parametro el pkcs7
                    responseEvidencia = await GeneraEvidencia(evidenciaRequest);

                    if (responseEvidencia.IsSuccess)
                    {
                        //si todo sale bien, aqui convertimos la evidencia en formato IFormFile
                        byte[] bytesEvidencia = Convert.FromBase64String(responseEvidencia.PdfBase64);
                        //MemoryStream streamEvidencia = new MemoryStream(bytesEvidencia);

                        //IFormFile fileEvidencia = new FormFile(streamEvidencia, 0, bytesEvidencia.Length, "Name", "Name");

                        // Crear un MemoryStream a partir de los bytes del PDF

                        /*IFormFile formFile = new FormFile(streamEvidencia, 0, bytesEvidencia.Length, request.archivoFirmar.Name, request.archivoFirmar.FileName)
                        {
                            Headers = new HeaderDictionary
                        {
                                { "Content-Disposition", $"form-data; filename={request.archivoFirmar.FileName}" },
                                { "Content-Type","application/pdf" }
                            }
                        };*/
                        //la evidenia siempre debe ser pdf
                        response.data.evidenciaFileContent = bytesEvidencia;
                        response.data.evidenciaContentType = "application/pdf";
                        response.data.evidenciaFileName = request.archivoFirmar.FileName.Split(".")[0].Replace(" ", "_") + "_Evidencia.pdf";

                        // Asegúrate de que el MemoryStream no se cierre prematuramente
                        //streamEvidencia.Position = 0;

                        response.success = true;
                        //vamos a borrar los datos que trae el certificado, porque ya los devulevo como arreglo de bytes;
                        responseEvidencia.PdfBase64 = "";
                        responseEvidencia.Certificates[0].Pkcs7CheckSum = "";
                        responseEvidencia.Certificates[0].OcspResponse = null;
                        response.data.evidenciaCertificate = responseEvidencia;
                        //System.IO.File.WriteAllText(@"E:\log1.txt", Newtonsoft.Json.JsonConvert.SerializeObject(responseEvidencia));
                    }
                    else
                    {
                        response.errors = new List<string> { responseEvidencia.ErrorMessage };
                    }
                }
            }
            catch (Exception ex)
            {
                response.errors = new List<string> { ex.Message };
            }

            return Ok(response);
        }
        /// <summary>
        /// Endpoint para firmar un documento con multiples firmas en un mismo momento. 
        /// </summary>
        /// <remarks>
        /// Utilizar este endpoint cuando se deasea firmar un documento (pdf, doc, docx) con dos o tres firmas en un mismo momento
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("multiFirma")]
        public async Task<IActionResult> MultiFirma([FromForm] MultifirmaRequest request)
        {
            GenericResponse<FirmaResponse> response = new GenericResponse<FirmaResponse>();
            response.errors = new List<string>();
            //primero validamos el request
            if (request.archivoFirmar == null || request.archivoFirmar.Length == 0)
            {
                response.errors.Add("Debe especificar la propiedad archivoFirmar");
            }
            
            /*if (request.archivoPfx_Efirma == null || request.archivoPfx_Efirma.Length == 0)
            {
                response.errors.Add("Debe especificar la propiedad archivoPfx_Efirma");
            }
            if (request.archivoPfx_Efirma.Length < 2)
            {
                response.errors.Add("Para usar este endpoint es necesario al menos 2 firmantes");
            }*/
            if(request.idUsuario ==null || request.idUsuario.Length==0)
                response.errors.Add("Debe especificar la propiedad idUsuario");
            if (request.idUsuario.Length < 2)
                response.errors.Add("Para usar este endpoint es necesario al menos 2 firmantes");

            if (request.password_Efirma == null || request.password_Efirma.Length == 0)
            {
                response.errors.Add("Debe especificar la propiedad password_Efirma");
            }
            if(request.password_Efirma.Length != request.idUsuario.Length)
                response.errors.Add($"El número de usuarios [{request.idUsuario.Length}] no coincide con el número de contraseñas [{request.password_Efirma.Length}] recibidos");

            /*if (request.archivoPfx_Efirma.Length != request.password_Efirma.Length)
            {
                response.errors.Add($"El número de firmas [{request.archivoPfx_Efirma.Length}] no coincide con el número de contraseñas [{request.password_Efirma.Length}] recibidos");
            }*/
            //RECORREMOS EL ARREGLO DE USUARIO PARA VER QUE TRAIGAN VALOR VALIDO
            foreach(var usr in request.idUsuario)
            {
                if(usr<=0)
                    response.errors.Add("Debe especificar un valor válido para idUsuario");
            }
            //recorremos el password para validar que tenga valor válido
            foreach(var pass in request.password_Efirma)
            {
                if(string.IsNullOrEmpty(pass))
                    response.errors.Add("Debe especificar un valor válido para password_Efirma");
            }
            if(request.idUsuario.Length>3 || request.password_Efirma.Length>3)
                response.errors.Add("Solo se permite como máximo 3 firmas");

            if (response.errors.Count > 0)
            {
                response.message = "Algunas propiedades son obligatorias, favor de verificar";
                return Ok(response);
            }


            // validamos las extensiones permitidas para el archivo a firmar
            var permittedExtensions = new[] { ".pdf", ".doc", ".docx" }; // Lista de extensiones permitidas
            var permittedMimeTypes = new[] { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }; // Lista de tipos MIME permitidos

            var ext = Path.GetExtension(request.archivoFirmar!.FileName).ToLowerInvariant();
            //validamos extension
            if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            {
                response.errors.Add($"archivoFirmar: La extensión del archivo {request.archivoFirmar.FileName} no es válido, sólo se permiten .pdf, .doc, .docx");
            }
            // Verifica el tipo MIME del archivo
            if (!permittedMimeTypes.Contains(request.archivoFirmar.ContentType))
            {
                response.errors.Add($"archivoFirmar: El tipo MimeTypes del archivo {request.archivoFirmar.FileName} no es válido, sólo se permiten .pdf, .doc, .docx ");
            }




            //Validamos el archivo a pfx
            /*var permittedMimeTypesPfx = new[] { "application/x-pkcs12" }; // Lista de extensiones permitidas para el pfx
            foreach (var file in request.archivoPfx_Efirma)
            {
                var extPfx = Path.GetExtension(file!.FileName).ToLowerInvariant();
                string type = file.ContentType;
                if (string.IsNullOrEmpty(extPfx) || !".pfx".Contains(extPfx))
                {
                    response.errors.Add("archivoPfx_Efirma: El archivo efirma debe ser de tipo .pfx ");
                }
                // Verifica el tipo MIME del archivo
                if (!permittedMimeTypesPfx.Contains(file.ContentType))
                {
                    response.errors.Add($"archivoFirmar: El tipo MimeTypes del archivo {request.archivoFirmar.FileName} no es válido, sólo se permiten .pfx ");
                }
            }*/

            if (response.errors.Count > 0)
            {
                response.message = "Algunas propiedades no cumplen con los requisitos, favor de verificar";
                return Ok(response);
            }
            response.errors = null;
            ///fin de validacion request

            try
            {
                //validamos y obtiene los pfx de los usuarios
                List<FIRMA_PFX> pfxs = this.obtienePfxs(request.idUsuario, request.password_Efirma);


                var content = new MultipartFormDataContent();
                //agregamos en el content los archivos uno por uno
                /*foreach (var file in request.archivoPfx_Efirma)
                {
                    var streamPfx = file!.OpenReadStream();
                    content.Add(new StreamContent(streamPfx), "ArchivoPFX", file.FileName);
                }*/
                foreach(var usrPfx in pfxs)
                {
                    var streamPfx = new MemoryStream(usrPfx.pfxFileContent);
                    //content.Add(new StreamContent(streamPfx), "ArchivoPFX", usrPfx.pfxFileName);
                    var streamContent = new StreamContent(streamPfx);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-pkcs12");
                    content.Add(streamContent, "ArchivoPFX", usrPfx.pfxFileName);
                }
                //agregamos en el content los password, uno por uno
                foreach (var p in request.password_Efirma)
                {
                    content.Add(new StringContent(p), "pass");
                }



                //leemos los archivos
                var streamFile = request.archivoFirmar!.OpenReadStream();



                //cargamos los archivos al content para enviarlos al request
                content.Add(new StreamContent(streamFile), "ArchivoFirmar", request.archivoFirmar.FileName);


                GenericResponse<Firma_API_NOdeJSResponse> responseFirma = new GenericResponse<Firma_API_NOdeJSResponse>();
                //invocamos la api que genera el pkcs7
                responseFirma = await _httpclient.requestApiFirmaPkcs7(staticClass.getURIGenerarPKCS7() + "uploadMulti", content);

                //si falla asignamos los mensajes y finaliza
                if (!responseFirma.success)
                {
                    response.message = responseFirma.message;
                    response.errors = responseFirma.errors;
                }
                else
                {
                    //si llega aqui es porque el pkcs7 si se generó satisfactoriamente

                    //pero vamos a validar si la longitud del pkcs7 es una longitud razonable.
                    if (responseFirma.data.pkcs7Blob.Length < 20)// si es menos de 20 significa que surgió un error al firmar
                    {
                        response.message = "Error al firmar el documento, verifique que la contraseña corresponda al archivo pfx.";
                        response.errors = new List<string> { "Longitud no válida para una matriz o cadena de caracteres Base-64." };
                    }
                    else
                    {
                        //creamos el objeto request que vamos a enviar para generar la evidencia
                        pkcs7FileRequest evidenciaRequest = new pkcs7FileRequest();

                        //el pkcs7 biene en un arreglo de bytes, y aqui lo convertmos en IFormFile
                        //var streamPkcs7 = new MemoryStream(responseFirma.data.pkcs7Blob);
                        //IFormFile filePkcs7 = new FormFile(streamPkcs7, 0, responseFirma.data.pkcs7Blob.Length, request.file.Name, "pkcs7_"+request.file.FileName);

                        //
                        //asignamos el pkcs7 para generar su evidencia
                        evidenciaRequest.file = responseFirma.data.pkcs7Blob;
                        //System.IO.File.WriteAllText(@"E:\log.txt", evidenciaRequest.file.Length.ToString());
                        response.data = new FirmaResponse();

                        //comenzamos a rellenar el response
                        byte[] fileBytes;

                        using (var memoryStream = new MemoryStream())
                        {
                            await responseFirma.data.pkcs7Blob.CopyToAsync(memoryStream);
                            fileBytes = memoryStream.ToArray();
                        }

                        response.data.pkcs7FileContent = fileBytes;
                        response.data.pkcs7ContentType = request.archivoFirmar.ContentType;
                        response.data.pkcs7FileName = request.archivoFirmar.FileName.Split(".")[0].Replace(" ", "_") + "_pkcs7." + request.archivoFirmar.FileName.Split(".")[1];


                        //creamos el objeto response que devuelve el endint de generar evidencia
                        CertificateResponse responseEvidencia = new CertificateResponse();
                        //invocamos el endpoint que genera la evidencia, le pasamos como parametro el pkcs7
                        responseEvidencia = await GeneraEvidencia(evidenciaRequest);

                        if (responseEvidencia.IsSuccess)
                        {
                            //si todo sale bien, aqui convertimos la evidencia en formato IFormFile
                            byte[] bytesEvidencia = Convert.FromBase64String(responseEvidencia.PdfBase64);
                            //MemoryStream streamEvidencia = new MemoryStream(bytesEvidencia);

                            //IFormFile fileEvidencia = new FormFile(streamEvidencia, 0, bytesEvidencia.Length, "Name", "Name");

                            // Crear un MemoryStream a partir de los bytes del PDF

                            /*IFormFile formFile = new FormFile(streamEvidencia, 0, bytesEvidencia.Length, request.archivoFirmar.Name, request.archivoFirmar.FileName)
                            {
                                Headers = new HeaderDictionary
                            {
                                    { "Content-Disposition", $"form-data; filename={request.archivoFirmar.FileName}" },
                                    { "Content-Type","application/pdf" }
                                }
                            };*/
                            //la evidenia siempre debe ser pdf
                            response.data.evidenciaFileContent = bytesEvidencia;
                            response.data.evidenciaContentType = "application/pdf";
                            response.data.evidenciaFileName = request.archivoFirmar.FileName.Split(".")[0].Replace(" ", "_") + "_Evidencia.pdf";

                            // Asegúrate de que el MemoryStream no se cierre prematuramente
                            //streamEvidencia.Position = 0;

                            response.success = true;
                            response.message = "Firma realizada: OK";
                            //vamos a borrar los datos que trae el certificado, porque ya los devulevo como arreglo de bytes;
                            responseEvidencia.PdfBase64 = "";
                            responseEvidencia.Certificates[0].Pkcs7CheckSum = "";
                            responseEvidencia.Certificates[0].OcspResponse = null;
                            response.data.evidenciaCertificate = responseEvidencia;
                            //System.IO.File.WriteAllText(@"E:\log1.txt", Newtonsoft.Json.JsonConvert.SerializeObject(responseEvidencia));
                        }
                        else
                        {
                            response.errors = new List<string> { responseEvidencia.ErrorMessage };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.errors = new List<string> { ex.Message };
            }

            return Ok(response);
        }
        /// <summary>
        /// Endpoint para validar una firma
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("ValidaFima")]
        public async Task<IActionResult> validateFirma([FromBody] ValidafirmaReques request)
        {
            GenericResponse<PFXInfoResponse> response = new GenericResponse<PFXInfoResponse>();
            response.errors = new List<string>();

            if (request == null)
                response.errors.Add("No se ha especificado ningun parámetro");
            else
            {
                if (request.idUsuario <= 0)
                    response.errors.Add("Debe especificar la propiedad idUsuaio");
                if (string.IsNullOrEmpty(request.password))
                    response.errors.Add("Debe especificar la propiedad password");

            }
            if (response.errors.Count > 0)
            {
                response.message = "Algunas propiedades no cumplen con los requisitos, favor de verificar";
                return Ok(response);
            }
            response.errors = null;
            try
            {
                var pfxUsr = _DBContextNotificacionAPI.FIRMA_PFX.Find(request.idUsuario);
                if (pfxUsr == null)
                {
                    response.message = $"El usuario: {request.idUsuario} no existe";
                    return Ok(response);
                }

                byte[] file = pfxUsr.pfxFileContent;
                
                
                //validamos si el pfx es valido, y obtenemos los datos del cenrtificado
                var cer = FirmaAvanzada.isValidPFX(file, request.password);
                if (cer == null)
                {
                    response.message = "La contraseña no corresponde con la Firma";
                    return Ok(response);
                }
                if (!cer.HasPrivateKey)
                {
                    response.message = "La contraseña no corresponde con la Firma";
                    return Ok(response);
                }
                if (cer.NotAfter <= DateTime.Now)
                {
                    response.message = "La firma ha vencido";
                    return Ok(response);
                }

                response.success = true;
                response.message = "La firma y la contraseña son válidos";
            }
            catch (Exception ex)
            {
                response.errors = new List<string>();
                response.errors.Add(ex.Message);
                response.message = "Surgió un error al validar la firma";
            }


            return Ok(response);
        }
        /// <summary>
        /// endpoint para guardar una firma PFX de un usuario
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("guardarPFX")]
        public async Task<IActionResult> GuardarPFX([FromForm] GuardarPFXrequest request)
        {
            GenericResponse<PFXInfoResponse> response = new GenericResponse<PFXInfoResponse>();
            response.errors = new List<string>();

            if (request == null)
                response.errors.Add("No se ha especificado ningun parámetro");
            else
            {
                if (request.idUsuario <= 0)
                    response.errors.Add("Debe especificar la propiedad idUsuaio");
                if (string.IsNullOrEmpty(request.password))
                    response.errors.Add("Debe especificar la propiedad password");
                if (request.pfxFileContent == null)
                    response.errors.Add("Debe especificar la propiedad pfxFileContent");
                if (request.pfxFileContent.Length == 0 || !request.pfxFileContent.FileName.EndsWith(".pfx"))
                    response.errors.Add("Archivo PFX no es válido");

                var permittedMimeTypesPfx = new[] { "application/x-pkcs12" }; // mimetype del pfx, no solo validar la extension
                // Verifica el tipo MIME del archivo
                if (!permittedMimeTypesPfx.Contains(request.pfxFileContent.ContentType))
                {
                    response.errors.Add($"El tipo MimeTypes del archivo {request.pfxFileContent.FileName} no es válido, sólo se permiten .pfx");
                }

            }
            if (response.errors.Count > 0)
            {
                response.message = "Algunas propiedades no cumplen con los requisitos, favor de verificar";
                return Ok(response);
            }
            response.errors = null;

            try
            {
                byte[] file = request.pfxFileContent.ToByteArray();
                //validamos si el pfx es valido, y obtenemos los datos del cenrtificado
                var cer = FirmaAvanzada.isValidPFX(file, request.password);
                if (cer == null)
                {
                    response.message = "La contraseña no corresponde a la Firma";
                    return Ok(response);
                }
                if (!cer.HasPrivateKey)
                {
                    response.message = "La contraseña no corresponde a la Firma";
                    return Ok(response);
                }
                if (cer.NotAfter <= DateTime.Now)
                {
                    response.message = "La firma ha vencido";
                    return Ok(response);
                }

                //buscamos el registro del usuario
                var usrFirma = _DBContextNotificacionAPI.FIRMA_PFX.Find(request.idUsuario);

                if (usrFirma == null)//no existe
                {
                    usrFirma = new FIRMA_PFX();
                    usrFirma.pfxFileContent = file;
                    usrFirma.pfxFileName = request.pfxFileContent.FileName;
                    usrFirma.idUsuario = request.idUsuario;
                    usrFirma.pfxVigencia = cer.NotAfter;
                    usrFirma.FechaAlta = DateTime.Now;

                    _DBContextNotificacionAPI.FIRMA_PFX.Add(usrFirma);
                }
                else //ya existe y se actualiza
                {
                    //usrFirma = new FIRMA_PFX();
                    usrFirma.pfxFileContent = file;
                    usrFirma.pfxFileName = request.pfxFileContent.FileName;
                    usrFirma.pfxVigencia = cer.NotAfter;
                    usrFirma.FechaAlta = DateTime.Now;

                }

                await _DBContextNotificacionAPI.SaveChangesAsync();

                response.success = true;
                response.message = "Firma guardado correctamente";

            }
            catch (Exception ex)
            {
                response.errors = new List<string>();
                response.errors.Add(ex.Message);
                response.message = "Surgió un error al guardar la firma";
            }

            return Ok(response);
        }
        private List<FIRMA_PFX> obtienePfxs(int[] usuarios, string[] password)
        {
            List<FIRMA_PFX> pfxs = new List<FIRMA_PFX>();
            try
            {
                FIRMA_PFX? usrpfx;
                int index = 0;
                foreach (var usr in usuarios)
                {
                    usrpfx = null;
                    usrpfx = _DBContextNotificacionAPI.FIRMA_PFX.Find(usr);
                    if (usrpfx == null)
                        throw new Exception($"1.- El usuario {usr} no cuenta con su FIRMA, se sugiere cargarla desde el perfil de usuario.");
                    if(usrpfx.pfxFileContent==null)
                        throw new Exception($"2.- El usuario {usr} no cuenta con su FIRMA, se sugiere cargarla desde el perfil de usuario.");

                    byte[] file = usrpfx.pfxFileContent;

                    var cer = FirmaAvanzada.isValidPFX(file, password[index]);
                    if (cer == null)
                    {
                        throw new Exception($"1.- La contraseña del usuario {usr} no corresponde con la Firma segun el orden proporcionado");
                    }
                    if (!cer.HasPrivateKey)
                    {
                        throw new Exception($"2.- La contraseña del usuario {usr} no corresponde con la Firma segun el orden proporcionado");
                    }
                    if (cer.NotAfter <= DateTime.Now)
                    {
                        throw new Exception($"La firma del usuario {usr} ha vencido");
                    }
                    pfxs.Add(usrpfx);
                    index++;
                }
                

            }
            catch (Exception ex) 
            {
                throw new Exception(ex.Message);   
            }
            return pfxs;
        }
    }
}
