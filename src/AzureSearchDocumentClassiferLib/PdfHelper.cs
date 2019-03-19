using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AzureSearchDocumentClassiferLib
{
    public static class PdfHelper
    {
        public static string GetTextFromPdfBytes(byte[] byteArray)
        {
            string pdfText = ParsePdfFromText(byteArray);
            if (string.IsNullOrEmpty(pdfText))
            {
                // Can't get it normally, extract from images
                var images = ExtractImages(byteArray);
                if (images.Count > 0)
                {
                    Trace.TraceInformation("File has images. Trying to do OCR...");
                    foreach (var name in images.Keys)
                    {
                        //if there is a filetype save the file
                        if (name.LastIndexOf(".") + 1 != name.Length)
                        {
                            Trace.TraceInformation("Parsing " + name);

                            // Try to get the text
                            var converter = new System.Drawing.ImageConverter();
                            byte[] bytes = (byte[])converter.ConvertTo(images[name], typeof(byte[]));
                            string text = OcrHelper.DoOcr(bytes);
                            if (string.IsNullOrEmpty(text))
                            {
                                Debug.WriteLine("No text found in file " + name);
                                Trace.TraceWarning("No text found in file " + name);
                            }
                            pdfText += text;

                            // trace first 100 characters
                            var textSubString = (pdfText.Length < 100) ? pdfText : pdfText.Substring(0, 100);
                            Trace.TraceInformation("Text found: " + textSubString);
                        }
                        else
                        {
                            Trace.TraceInformation("Unknown file for " + name);
                        }
                    }
                }
            }

            return pdfText;
        }

        private static string ParsePdfFromText(byte[] fileBytes)
        {
            string retStr = string.Empty;
            using (PdfReader reader = new PdfReader(fileBytes))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy());
                    retStr += text;
                }
            }
            return retStr;
        }

        private static Dictionary<string, System.Drawing.Image> ExtractImages(byte[] fileBytes)
        {
            var images = new Dictionary<string, System.Drawing.Image>();
            using (var reader = new PdfReader(fileBytes))
            {
                var parser = new PdfReaderContentParser(reader);

                for (var i = 1; i <= reader.NumberOfPages; i++)
                {
                    ImageRenderListener listener = new ImageRenderListener();
                    parser.ProcessContent(i, listener);
                    var index = 1;

                    if (listener.Images.Count > 0)
                    {
                        foreach (var pair in listener.Images)
                        {
                            images.Add(string.Format("Page_{0}_Image_{1}{2}", i.ToString("D4"), index.ToString("D4"), pair.Value), pair.Key);
                            index++;
                        }
                    }
                }
                return images;
            }
        }

        internal class ImageRenderListener : IRenderListener
        {
            Dictionary<System.Drawing.Image, string> images = new Dictionary<System.Drawing.Image, string>();

            public Dictionary<System.Drawing.Image, string> Images
            {
                get { return images; }
            }

            public void BeginTextBlock() { }

            public void EndTextBlock() { }

            public void RenderImage(ImageRenderInfo renderInfo)
            {
                PdfImageObject image = renderInfo.GetImage();

                var pdfObject = image.Get(PdfName.FILTER);

                PdfName filter = null;
                if (pdfObject is PdfName)
                {
                    filter = (PdfName)pdfObject;
                    var drawingImage = GetImage(filter, image);
                    if (drawingImage != null)
                    {
                        Images.Add(drawingImage.Item2, drawingImage.Item1);
                    }
                }

                if (pdfObject is PdfArray)
                {
                    var pdfArray = (PdfArray)pdfObject;
                    foreach (PdfName pdfName in pdfArray)
                    {
                        var drawingImage = GetImage(pdfName, image);
                        Images.Add(drawingImage.Item2, drawingImage.Item1);
                    }
                }
            }

            public void RenderText(TextRenderInfo renderInfo) { }
        }

        private static Tuple<string, System.Drawing.Image> GetImage(PdfName filter, PdfImageObject pdfImageObject)
        {
            Tuple<string, System.Drawing.Image> image = null;

            System.Drawing.Image drawingImage = pdfImageObject.GetDrawingImage();

            string extension = ".";

            if (Equals(filter, PdfName.DCTDECODE))
            {
                Trace.TraceInformation("JPG image detected");
                extension += PdfImageObject.ImageBytesType.JPG.FileExtension;
            }
            else if (Equals(filter, PdfName.JBIG2DECODE))
            {
                Trace.TraceInformation("JBIG2 extension detected");
                extension += PdfImageObject.ImageBytesType.JBIG2.FileExtension;
            }
            else if (Equals(filter, PdfName.JPXDECODE))
            {
                Trace.TraceInformation("JP2 extension detected");
                extension += PdfImageObject.ImageBytesType.JP2.FileExtension;
            }
            else if (Equals(filter, PdfName.FLATEDECODE))
            {
                Trace.TraceInformation("PNG image detected");
                extension += PdfImageObject.ImageBytesType.PNG.FileExtension;
            }
            else if (Equals(filter, PdfName.LZWDECODE))
            {
                Trace.TraceInformation("LZWDECODE extension detected");
                extension += PdfImageObject.ImageBytesType.CCITT.FileExtension;
            }
            else if (Equals(filter, PdfName.CCITTFAXDECODE))
            {
                Trace.TraceInformation("CCITTFAXDECODE extension detected");
                extension += PdfImageObject.ImageBytesType.CCITT.FileExtension;
            }
            else
            {
                Debug.WriteLine("Unknown type: " + filter);
                Trace.TraceInformation("Unknown type: " + filter);
            }

            return new Tuple<string, System.Drawing.Image>(extension, drawingImage);
        }

        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
