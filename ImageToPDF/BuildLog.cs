using LoadFileAdapter;

namespace ImageToPDF
{
    /// <summary>
    /// Contains logging information from PDF builds.
    /// </summary>
    public class BuildLog
    {
        private DocumentCollection docs;
        private string[] pdfs;
        private string[] processingMessages;
        private string[] errorMessages;

        /// <summary>
        /// Initializes a new instance of <see cref="BuildLog"/>.
        /// </summary>
        /// <param name="docs">The document collection that was built.</param>
        /// <param name="pdfs">The fille paths to the created PDFs.</param>
        /// <param name="msgs">Messages about the images used to create the PDFs.</param>
        /// <param name="errorMessages">Error messages from the build process.</param>
        public BuildLog(DocumentCollection docs, string[] pdfs, string[] msgs, string[] errorMessages)
        {
            this.docs = docs;
            this.pdfs = pdfs;
            this.processingMessages = msgs;
            this.errorMessages = errorMessages;
        }

        /// <summary>
        /// The documents used to build the PDFs.
        /// </summary>
        public DocumentCollection Documents
        {
            get
            {
                return this.docs;
            }

        }

        /// <summary>
        /// The file paths to the PDFs that were created.
        /// </summary>
        public string[] PDFs
        {
            get
            {
                return this.pdfs;
            }
        }

        /// <summary>
        /// Messages about the images used to build the PDF files. 
        /// Each message contains the DPI, image size, and image 
        /// key for each image used.
        /// </summary>
        public string[] ProcessingMessages
        {
            get
            {
                return this.processingMessages;
            }
        }

        /// <summary>
        /// Error messages if any from the build process.
        /// </summary>
        public string[] ErrorMessages
        {
            get

            {
                return this.errorMessages;
            }
        }
    }
}
