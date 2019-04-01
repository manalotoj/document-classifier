using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Configuration;
using System.Web;
using System.Diagnostics;

namespace AzureSearchDocumentClassiferLib
{
    internal class OcrHelper
    {
        public static string DoOcr(byte[] fileBytes)
        {
            // Request parameters
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["language"] = "unk";
            queryString["detectOrientation "] = "true";

            var region = ConfigurationManager.AppSettings["ComputerVisionRegion"];
            if (!string.IsNullOrEmpty(region))
            {
                region = region.Replace('-', ' ').Trim().ToLower();
            }
            else
            {
                throw new ArgumentException($"Invalid appSetting.ComputerVisionRegion value of '{region}' detected.");
            }

            var url = $"https://{region}.api.cognitive.microsoft.com/vision/v1.0/ocr?{queryString}";
            Trace.WriteLine($"Cognitive Services Url: {url}");

            var request = WebRequest.Create(url) as HttpWebRequest;

            if (request != null)
            {
                string apiKey = ConfigurationManager.AppSettings["AzureCognitiveApiKey"];
                request.Accept = "application/json";
                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
                request.ContentType = "application/octet-stream";
                request.Method = "POST";
                request.KeepAlive = true;

                request.ContentLength = fileBytes.Length;

                var requestStream = request.GetRequestStreamAsync().Result;
                requestStream.Write(fileBytes, 0, fileBytes.Length);
                requestStream.Close();

                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        if (response != null && response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var stream = response.GetResponseStream())
                            {
                                if (stream != null)
                                {
                                    StreamReader sr = new StreamReader(stream);
                                    string outputJson = sr.ReadToEnd();
                                    OcrResponseObject resp = JsonConvert.DeserializeObject<OcrResponseObject>(outputJson);
                                    return resp.ToString();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error with OCR request: " + ex.Message);
                    throw;
                }
            }
            return string.Empty;
        }

        public class OcrResponseObject
        {
            public string language;
            public string textAngle;
            public string orientation;
            public Region[] regions;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var region in regions)
                {
                    foreach (var line in region.lines)
                    {
                        foreach (var word in line.words)
                        {
                            sb.Append(word.text + " ");
                        }
                        sb.AppendLine();
                    }
                }
                return sb.ToString();
            }
        }

        public class Region
        {
            public string boundingBox;
            public Line[] lines;
        }
        public class Line
        {
            public string boundingBox;
            public Word[] words;
        }
        public class Word
        {
            public string boundingBox;
            public string text;
        }
    }
}
