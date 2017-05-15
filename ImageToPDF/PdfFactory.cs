using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LoadFileAdapter;
using LoadFileAdapter.Importers;

namespace ImageToPDF
{
    /// <summary>
    /// Generates PDFs from image files where each image is a page in the document.
    /// </summary>
    public class PdfFactory
    {
        private const int POINTS = 72;

        /// <summary>
        /// Imports a LFP or OPT load file and converts the linked image files 
        /// to PDFs.
        /// </summary>
        /// <param name="loadfile">The load file to import.</param>
        /// <param name="outdir">The output directory.</param>
        /// <param name="throttle">The max degree of parallelism to use.</param>
        /// <returns>Returns a build log of the images and pdfs processed.</returns>
        public BuildLog Build(FileInfo loadfile, DirectoryInfo outdir, int throttle)
        {
            DocumentCollection docs = getDocuments(loadfile);
            BlockingCollection<string> pdfs = new BlockingCollection<string>();
            BlockingCollection<string> messages = new BlockingCollection<string>();
            BlockingCollection<string> errors = new BlockingCollection<string>();

            Parallel.ForEach(docs, new ParallelOptions { MaxDegreeOfParallelism = throttle }, doc =>
            {
                string[] files = doc.Representatives.Where(r => r.Type == Representative.FileType.Image).First()
                    .Files.Values.ToArray();
                string imgPath = (!files.First().Substring(0, 2).Equals(@"\\")) 
                    ? files.First().TrimStart('\\')
                    : files.First();
                string outfile = Path.Combine(outdir.FullName, imgPath);

                if (!outdir.FullName.Equals(outfile.Substring(0, outdir.FullName.Length)))
                {
                    outfile = Path.Combine(
                        outdir.FullName,
                        outfile.Substring(loadfile.Directory.FullName.Length).TrimStart('\\'));
                }

                FileInfo fi = new FileInfo(outfile);

                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                outfile = Path.Combine(fi.Directory.FullName, Path.GetFileNameWithoutExtension(outfile) + ".pdf");
                
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];

                    if (!file.Substring(0, 2).Equals(@"\\"))
                    {
                        file = Path.Combine(loadfile.Directory.FullName, file.TrimStart('\\'));
                    }
                }

                pdfs.Add(outfile);

                try
                {
                    List<string> procMsgs = Build(files, outfile);
                    procMsgs.ForEach(m => messages.Add(m));
                    int progress = (int)((double)(pdfs.Count + errors.Count) / (double)docs.Count * 100);
                    OnDocumentProcessed(progress);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            });

            return new BuildLog(docs, pdfs.ToArray(), messages.ToArray(), errors.ToArray());
        }

        /// <summary>
        /// Builds a PDF from a set of images where each image is a separate page 
        /// and saves the resulting PDF.
        /// </summary>
        /// <param name="infiles">The image files for each page in the PDF.</param>
        /// <param name="outfile">The output destination.</param>
        /// <returns>Returns messages about the images processed.</returns>
        public static List<string> Build(string[] infiles, string outfile)
        {
            List<string> messages = new List<string>();                            
            Rectangle rec = new Rectangle(0f, 0f, 8.5f * POINTS, 11f * POINTS);
            iTextSharp.text.Document doc = new iTextSharp.text.Document(rec, 0f, 0f, 0f, 0f);
            using (var ms = new MemoryStream())
            using (var writer = PdfWriter.GetInstance(doc, ms))
            {
                writer.SetFullCompression();

                foreach (string file in infiles)
                {
                    var image = Image.GetInstance(file);
                    float width = image.Width / image.DpiX * POINTS;
                    float height = image.Height / image.DpiY * POINTS;
                    rec = new Rectangle(0f, 0f, width, height);
                    image.ScaleToFit(rec);
                    doc.SetPageSize(rec);
                    doc.SetMargins(0f, 0f, 0f, 0f);

                    if (doc.IsOpen())
                    {
                        doc.NewPage();
                    }
                    else
                    {
                        doc.Open();
                    }

                    doc.Add(image);
                    string msg = String.Format("Processing image: {0}, DPI: {1}, Size: {2:0.0} x {3:0.0}",
                        Path.GetFileName(file), image.DpiX, width / POINTS, height / POINTS);
                    messages.Add(msg);
                }

                doc.Close();

                File.WriteAllBytes(outfile, ms.ToArray());
            }

            return messages;
        }

        /// <summary>
        /// Provides the progress percent of the documents processed by the Build method.
        /// </summary>
        public event EventHandler<int> DocumentProcessed;

        protected virtual void OnDocumentProcessed(int e)
        {
            DocumentProcessed?.Invoke(this, e);
        }

        protected DocumentCollection getDocuments(FileInfo file)
        {
            DocumentCollection docs = null;
            int codePage = Properties.Settings.Default.ImportEncoding;
            Encoding encoding = Encoding.GetEncoding(codePage);

            if (file.Extension.ToUpper().Equals(".LFP"))
            {
                LfpImporter importer = new LfpImporter();
                docs = importer.Import(file, encoding, null);
            }
            else if (file.Extension.ToUpper().Equals(".OPT"))
            {
                OptImporter importer = new OptImporter();
                docs = importer.Import(file, encoding, null);
            }
            else
            {
                throw new Exception("Unsupported file type.");
            }

            return docs;
        }        
    }
}
