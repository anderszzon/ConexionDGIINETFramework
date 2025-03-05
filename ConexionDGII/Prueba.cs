using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.Xml;
using Newtonsoft.Json;
using System.Xml.Linq;


namespace ConexionDGII
{
    public class Prueba
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _tokenGlobal;
        private static string _trackIdGlobal;

        private static readonly string tenantId = "Imocom.com.co";
        private static readonly string clientId = "c0c96a54-4c1a-4fbc-846b-11926cc304aa";
        private static readonly string clientSecret = "nNM8Q~-mDVMVJXAS74IR0UaiHWHhWztBgNY4faB9";
        private static readonly string resource = "https://proximoprd.operations.dynamics.com/.default";
        private static readonly string oDataUrl = "https://proximoprd.operations.dynamics.com/data/IMOCCertificadosProvEntity";


        public static string EnviarTokenSincrona(string urlSemilla, string passCert)
        {
            return ObtenerSemilla(urlSemilla, passCert).GetAwaiter().GetResult();
        }

        public static async Task<string> ObtenerSemilla(string urlSemilla, string passCert)
        {
            string filePath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\semilla.xml"; // Ruta donde guardar el archivo

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(urlSemilla);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string xmlContent = await response.Content.ReadAsStringAsync();

                    // Guardar en un archivo
                    File.WriteAllText(filePath, xmlContent);

                    Console.WriteLine(xmlContent);
                    Console.WriteLine($"XML guardado en: {filePath}");

                    // ✅ Llamar al método y recibir el JSON
                    string rutaSemillaFirmada = await FirmarSemilla(passCert);
                    return rutaSemillaFirmada; // ✅ Devolver el JSON recibido
                }
                else
                {
                    Console.WriteLine($"Error al obtener el XML. Código: {response.StatusCode}");
                    return $"Error: {response.StatusCode} - {responseBody}"; 
                }
            }
        }

        public static async Task<string> FirmarSemilla(string passCert)
        {
            string xmlPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\semilla.xml";  // Ruta donde tienes tu semilla
            string signedXmlPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\semillaFirmada.xml"; // Archivo firmado
            string pathCert = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\20250130-2113054-YAD25P5MJ.p12"; // Ruta de tu certificado

            string jsonPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\facturajson31.json"; // Ruta del JSON
            string filePath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\facturajson31.xml"; // Ruta donde guardar el archivo

            try
            {
                // X509Certificate2 cert = new X509Certificate2(pathCert, passCert, X509KeyStorageFlags.Exportable);

                XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlPath);

                //SignXmlRepo(xmlDoc, pathCert, passCert);


                SignXmlDISCORD(xmlDoc, pathCert, passCert);


                // Guardar el XML firmado
                xmlDoc.Save(signedXmlPath);
                Console.WriteLine("XML firmado y guardado en: " + signedXmlPath);

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                
                // Leer el archivo JSON
                string jsonContent = File.ReadAllText(jsonPath);

                // Convertir JSON a XML
                JObject jsonObj = JObject.Parse(jsonContent); // Convertir JSON a JObject

                XmlDocument xmlDocument = JsonConvert.DeserializeXmlNode(jsonContent);

                // Agregar la declaración XML estándar
                XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
                XmlElement root = xmlDocument.DocumentElement;
                xmlDocument.InsertBefore(xmlDeclaration, root);

                // Guardar el XML en un archivo con la declaración XML
                using (XmlWriter writer = XmlWriter.Create(filePath, new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8 }))
                {
                    xmlDocument.WriteTo(writer);
                }

                //string xmlContent = xmlDocument.OuterXml;
                //File.WriteAllText(filePath, xmlContent);

                Console.WriteLine(signedXmlPath);

                string rutaFacturaFirmada = await FirmarFactura(passCert);
                return rutaFacturaFirmada; // ✅ Devolver el JSON recibido

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return $"Error: {ex.Message}"; // ✅ Devuelve error como string

            }
        }

        public static async Task<string> FirmarFactura(string passCert)
        {
            string xmlPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\facturajson31.xml";  // Ruta donde tienes tu semilla
            string signedXmlPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\130322791E310000000001.xml"; // Archivo firmado
            string pathCert = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\20250130-2113054-YAD25P5MJ.p12"; // Ruta de tu certificado

            string invoice;

            try
            {
                // X509Certificate2 cert = new X509Certificate2(pathCert, passCert, X509KeyStorageFlags.Exportable);

                XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlPath);

                //SignXmlRepo(xmlDoc, pathCert, passCert);


                SignXmlDISCORD(xmlDoc, pathCert, passCert);


                // Guardar el XML firmado
                xmlDoc.Save(signedXmlPath);
                Console.WriteLine("XML firmado y guardado en: " + signedXmlPath);

                invoice = "Factura & Semilla Firmada";
                return invoice; // Devuelve el JSON como string

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return $"Error: {ex.Message}"; // ✅ Devuelve error como string
            }
        }


        static XmlDocument SignXmlDISCORD(XmlDocument xmlDoc, string pathCert, string passCert)
        {
            if (!File.Exists(pathCert))
                throw new FileNotFoundException("El certificado para firma no existe", pathCert);

            var cert = new X509Certificate2(pathCert, passCert, X509KeyStorageFlags.Exportable);

            if (cert.PrivateKey == null)
                throw new Exception("El certificado no contiene una clave privada.");

            var key = cert.GetRSAPrivateKey();

            if (key == null)
                throw new Exception("No se pudo obtener la clave privada RSA del certificado.");

            var signedXml = new SignedXml(xmlDoc)
            {
                SigningKey = key
            };

            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

            var reference = new Reference
            {
                Uri = "",
                DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256"
            };

            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();

            XmlElement xmlFirmaDigital = signedXml.GetXml();
            xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlFirmaDigital, true));

            // Genera el hash SHA-256 de la firma digital en formato XML
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] firmaBytes = Encoding.UTF8.GetBytes(xmlFirmaDigital.OuterXml);
                byte[] hashBytes = sha256.ComputeHash(firmaBytes);

                // Convierte el hash a una cadena hexadecimal
                string hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                // Extrae los primeros 6 caracteres del hash
                string codigoSeguridad = hashHex.Substring(0, 6);

                // Modifica el nodo <CodigoSeguridadeCF> en el XML

                XmlNode nodoCodigoSeguridad = xmlDoc.SelectSingleNode("//CodigoSeguridadeCF");
                if (nodoCodigoSeguridad != null)
                {
                    nodoCodigoSeguridad.InnerText = codigoSeguridad;

                }
                //this.codigoSeguridad = codigoSeguridad;
            }

            return xmlDoc;
        }

        static XmlDocument SignXmlRepo(XmlDocument xmlDoc, string pathCert, string passCert)
        {
           try 
            {
                if (!File.Exists(pathCert)) throw new Exception("El certificado para firma no existe");

                var cert = new X509Certificate2(pathCert, passCert, X509KeyStorageFlags.Exportable);
                var exportedKeyMaterial = cert.PrivateKey.ToXmlString(true);
                var key = new RSACryptoServiceProvider(new CspParameters(24));
                key.PersistKeyInCsp = false;
                key.FromXmlString(exportedKeyMaterial);
                SignedXml signedXml = new SignedXml(xmlDoc);
                signedXml.SigningKey = key;
                signedXml.SignedInfo.SignatureMethod ="http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                // Se agrega la referencia del algoritmo de firma utilizado.
                Reference reference = new Reference();
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
                reference.Uri = "";
                signedXml.AddReference(reference);

                // Se agrega la información del certificado utilizado para la firma.
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;
                // Generate the signature.
                signedXml.ComputeSignature();
                //Obtenemos la representación del XML firmado y la guardamos en un XmlElement object
                XmlElement xmlFirmaDigital = signedXml.GetXml();

                //Adicionamos el elemento de la firma al documento XML.
                xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlFirmaDigital, true));
                return xmlDoc;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string EnviarFacturaElectronicaSincrona( string urlValidarSemilla, string urlRecepcionFactura, string urlConsultaFactura)
        {
            return ValidarSemilla(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura).GetAwaiter().GetResult();
        }

        public static async Task<string> ValidarSemilla(string urlValidarSemilla, string urlRecepcionFactura, string urlConsultaFactura)
        {

            string filePath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\semillaFirmada.xml"; // Ruta donde guardar el archivo

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Crear el contenido multipart/form-data
                    using (var form = new MultipartFormDataContent())
                    {
                        // Leer el archivo XML
                        var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

                        // Agregar el archivo al formulario con el nombre "xml"
                        form.Add(fileContent, "xml", Path.GetFileName(filePath));

                        // Agregar encabezados
                        client.DefaultRequestHeaders.Add("accept", "application/json");

                        // Enviar la solicitud POST
                        HttpResponseMessage response = await client.PostAsync(urlValidarSemilla, form);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine(responseBody);

                            var json = JObject.Parse(responseBody);
                            _tokenGlobal = json["token"]?.ToString();

                            // ✅ Llamar al método y recibir el JSON
                            string JsonFinal = await EnviarFacturaElectronica(urlRecepcionFactura, urlConsultaFactura);
                            return JsonFinal; // ✅ Devolver el JSON recibido

                        }
                        else
                        {
                            Console.WriteLine(response.StatusCode);
                            Console.WriteLine(responseBody);
                            return $"Error: {response.StatusCode} - {responseBody}"; // ✅ Devuelve error
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return $"❌ Error: {ex.Message}"; // ✅ Devuelve error como string

            }
        }

        public static async Task<string> EnviarFacturaElectronica(string urlRecepcionFactura, string urlConsultaFactura)
        {

            string xmlPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\130322791E310000000001.xml"; // Ruta del XML

            try
            {

                using (HttpClient client = new HttpClient())
                {
                    // Agregar el token de autorización
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenGlobal);
                    client.DefaultRequestHeaders.Add("accept", "application/json");

                    // Crear el contenido multipart/form-data
                    using (var form = new MultipartFormDataContent())
                    {
                        // Leer el archivo XML
                        var fileContent = new ByteArrayContent(File.ReadAllBytes(xmlPath));
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

                        // Agregar el archivo al formulario
                        form.Add(fileContent, "xml", Path.GetFileName(xmlPath));

                        // Enviar la solicitud POST
                        HttpResponseMessage response = await client.PostAsync(urlRecepcionFactura, form);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine(responseBody);

                            var json = JObject.Parse(responseBody);
                            _trackIdGlobal = json["trackId"]?.ToString();

                            // ✅ Llamar al método y recibir el JSON
                            string estadoFacturaJson = await ConsultarEstadoFacturaElectronica(urlConsultaFactura);
                            return estadoFacturaJson; // ✅ Devolver el JSON recibido

                        }
                        else
                        {
                            Console.WriteLine(response.StatusCode);
                            Console.WriteLine(responseBody);

                            return $"Error: {response.StatusCode} - {responseBody}"; // ✅ Devuelve error

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return $"❌ Error: {ex.Message}"; // ✅ Devuelve error como string

            }
        }

        public static async Task<string> ConsultarEstadoFacturaElectronica(string urlConsultaFactura)
        {
            string url = $"{urlConsultaFactura}?TrackId={_trackIdGlobal}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Agregar encabezados
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_tokenGlobal}");

                    // Enviar solicitud GET
                    HttpResponseMessage response = await client.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(responseBody);

                        var json = JObject.Parse(responseBody);

                        return responseBody; // Devuelve el JSON como string

                    }
                    else
                    {
                        Console.WriteLine(response.StatusCode);
                        Console.WriteLine(responseBody);

                        return $"Error: {response.StatusCode} - {responseBody}"; // Retorna el mensaje de error
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return $"❌ Error: {ex.Message}"; // Retorna el error

            }
        }

















        /*                   PRUEBAS PARA CONEXION A LAS APIS DE DYNAMICS 365 FINANCE AND OPERATIONS                 */

        private static async Task<string> ObtenerToken()
        {
            using (HttpClient client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "scope", resource }
                };

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error obteniendo el token: " + responseString);
                }

                JObject jsonResponse = JObject.Parse(responseString);
                return jsonResponse["access_token"]?.ToString(); // Retorna el access_token o null si no existe
            }
        }

        public static async Task ObtenerDatosOData()
        {
            try
            {
                string token = await ObtenerToken();
                Console.WriteLine("Token: " + token);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    string filter = "$filter=dataAreaId eq 'IMMC' and TransDate ge 2024-01-01 and TransDate le 2024-12-31 and AccountNum eq '890330378'";

                    string requestUrl = oDataUrl.Contains("?")
                        ? $"{oDataUrl}&{filter}&cross-company=true"
                        : $"{oDataUrl}?{filter}&cross-company=true";

                    HttpResponseMessage response = await client.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Respuesta de OData:");
                    Console.WriteLine(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public static string ObtenerTokenSync()
        {
            return ObtenerToken().Result; // Ejecuta la tarea y obtiene el resultado inmediatamente
        }

        public async Task<string> ObtenerDatosAsync(string url)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        }

        // Método para hacer la petición a la API
        public static async Task ObtenerDatosAPI(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode(); // Lanza una excepción si hay error en la respuesta HTTP

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Respuesta de la API:");
                    Console.WriteLine(jsonResponse);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al llamar a la API: " + ex.Message);
                }
            }
        }

        public string usuario = "root";
        public string conexion = "localhost";

        public static decimal Suma(decimal a, decimal b) => a + b;
        public static double Resta(double a, double b) => a - b;
        public static double Producto(double a, double b) => a * b;
        public static double Division(double a, double b) => b != 0 ? a / b : throw new ArgumentException("No se puede dividir por cero.");

        public static double DivisionMetodo(Double a, Double b)
        {
            return a / b;

        }

    }
}
