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


namespace ConexionDGII
{
    public class Prueba
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly string tenantId = "Imocom.com.co";
        private static readonly string clientId = "c0c96a54-4c1a-4fbc-846b-11926cc304aa";
        private static readonly string clientSecret = "nNM8Q~-mDVMVJXAS74IR0UaiHWHhWztBgNY4faB9";
        private static readonly string resource = "https://proximoprd.operations.dynamics.com/.default";
        private static readonly string oDataUrl = "https://proximoprd.operations.dynamics.com/data/IMOCCertificadosProvEntity";

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

        public static async Task ObtenerSemilla()
        {
            string url = "https://ecf.dgii.gov.do/testecf/autenticacion/api/Autenticacion/Semilla";
            string filePath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\semilla.xml"; // Ruta donde guardar el archivo

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string xmlContent = await response.Content.ReadAsStringAsync();

                    // Guardar en un archivo
                    File.WriteAllText(filePath, xmlContent);

                    Console.WriteLine(xmlContent);
                    Console.WriteLine($"XML guardado en: {filePath}");
                }
                else
                {
                    Console.WriteLine($"Error al obtener el XML. Código: {response.StatusCode}");
                }
            }
        }



        public static async Task FirmarSemilla()
        {
            string xmlPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\semilla.xml";  // Ruta donde tienes tu semilla
            string signedXmlPath = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\semillaFirmada.xml"; // Archivo firmado
            string pathCert = "C:\\Users\\Admina167bb248c\\source\\repos\\ConexionDGII\\Archivos\\20250130-2113054-YAD25P5MJ.p12"; // Ruta de tu certificado
            string passCert = "LD271167"; // Contraseña del certificado

            try
            {
                // Cargar el certificado digital desde el archivo .pfx/.p12
                X509Certificate2 cert = new X509Certificate2(pathCert, passCert, X509KeyStorageFlags.Exportable);

                // Cargar el XML de la semilla
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlPath);

                // Firmar el XML
                SignXmlRepo(xmlDoc, pathCert, passCert);

                // Guardar el XML firmado
                xmlDoc.Save(signedXmlPath);
                Console.WriteLine("XML firmado y guardado en: " + signedXmlPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
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
            static XmlDocument SignXml(XmlDocument xmlDoc, string pathCert, string passCert)
        {
            try
            {
                if (!File.Exists(pathCert))
                    throw new Exception("El certificado para firma no existe");

                // Cargar el certificado con su clave privada
                X509Certificate2 cert = new X509Certificate2(pathCert, passCert,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                // Extraer clave privada
                RSACryptoServiceProvider key = (RSACryptoServiceProvider)cert.PrivateKey;

                if (key == null)
                    throw new Exception("No se pudo obtener la clave privada del certificado");

                SignedXml signedXml = new SignedXml(xmlDoc);
                signedXml.SigningKey = key;
                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                // Configurar la referencia para la firma
                Reference reference = new Reference
                {
                    Uri = "",
                    DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256"
                };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                // Agregar la información del certificado a la firma
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;

                // Generar la firma
                signedXml.ComputeSignature();

                // Obtener el XML firmado y agregarlo al documento original
                XmlElement xmlFirmaDigital = signedXml.GetXml();
                xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlFirmaDigital, true));

                return xmlDoc;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al firmar el XML: " + ex.Message, ex);
            }
        }

        public static XmlDocument SignXmlFail(XmlDocument xmlDoc, string pathCert, string passCert)
        {
            try
            {
                // Verificar si el archivo del certificado existe
                if (!File.Exists(pathCert))
                    throw new Exception("El certificado para firma no existe");

                // Cargar el certificado con la clave privada
                var cert = new X509Certificate2(pathCert, passCert, X509KeyStorageFlags.Exportable);
                RSA rsaKey = cert.GetRSAPrivateKey();

                // Crear el nodo <Signature>
                XmlElement signatureElement = xmlDoc.CreateElement("Signature");
                signatureElement.SetAttribute("xmlns", "http://www.w3.org/2000/09/xmldsig#");

                // Crear el nodo <SignedInfo>
                XmlElement signedInfoElement = xmlDoc.CreateElement("SignedInfo");

                // Crear el nodo <CanonicalizationMethod>
                XmlElement canonicalizationMethod = xmlDoc.CreateElement("CanonicalizationMethod");
                canonicalizationMethod.SetAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315");
                signedInfoElement.AppendChild(canonicalizationMethod);

                // Crear el nodo <SignatureMethod>
                XmlElement signatureMethod = xmlDoc.CreateElement("SignatureMethod");
                signatureMethod.SetAttribute("Algorithm", "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                signedInfoElement.AppendChild(signatureMethod);

                // Serializar el nodo <SignedInfo> y calcular su hash
                byte[] signedInfoBytes = Encoding.UTF8.GetBytes(signedInfoElement.OuterXml);
                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(signedInfoBytes);
                }

                // Firmar el hash con la clave privada RSA
                byte[] signedHash = rsaKey.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Crear el nodo <SignatureValue>
                XmlElement signatureValueElement = xmlDoc.CreateElement("SignatureValue");
                signatureValueElement.InnerText = Convert.ToBase64String(signedHash);
                signatureElement.AppendChild(signedInfoElement);
                signatureElement.AppendChild(signatureValueElement);

                // Agregar el nodo <KeyInfo> con el certificado
                XmlElement keyInfoElement = xmlDoc.CreateElement("KeyInfo");
                XmlElement x509DataElement = xmlDoc.CreateElement("X509Data");
                XmlElement x509CertificateElement = xmlDoc.CreateElement("X509Certificate");
                x509CertificateElement.InnerText = Convert.ToBase64String(cert.GetRawCertData());
                x509DataElement.AppendChild(x509CertificateElement);
                keyInfoElement.AppendChild(x509DataElement);
                signatureElement.AppendChild(keyInfoElement);

                // Anexar la firma al documento XML
                xmlDoc.DocumentElement.AppendChild(signatureElement);

                return xmlDoc;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al firmar el XML: " + ex.Message);
            }
        }


        public async Task<string> ObtenerDatosAsync(string url)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
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

    }
}
