using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace ImageToPDF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        private BackgroundWorker worker = new BackgroundWorker();
        public MainWindow()
        {
            InitializeComponent();

            int max = Properties.Settings.Default.MaxThrottle;
            throttleSlider.Maximum = (Environment.ProcessorCount < max) 
                ? max 
                : Environment.ProcessorCount;
            throttleTextbox.Text = throttleSlider.Value.ToString();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {            
            convertProgress.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int errorCount = (int)e.Result;
            convertProgress.Value = 100;

            if (errorCount == 0)
            {
                MessageBox.Show(
                    "The process has completed successfully.", 
                    "Conversion Success", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "The process has completed with " + errorCount.ToString() + " errors.", 
                    "Completed with Errors", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            Tuple<FileInfo, DirectoryInfo, int> args = (Tuple<FileInfo, DirectoryInfo, int>)e.Argument;
            FileInfo infile = args.Item1;
            DirectoryInfo outdir = args.Item2;
            int throttle = args.Item3;
            PdfFactory factory = new PdfFactory();
            factory.DocumentProcessed += Factory_DocumentProcessed;
            BuildLog buildLog = factory.Build(infile, outdir, throttle);
            timer.Stop();
            string logfile = System.IO.Path.Combine(infile.Directory.FullName, "ImageToPDF.log");

            using (StreamWriter writer = new StreamWriter(logfile))
            {
                string msg = String.Format("The file {0} with {1} documents and {2} images was processed in {3:0.0} minutes.", 
                    infile.Name, buildLog.Documents.Count, buildLog.Documents.ImageCount, timer.Elapsed.TotalMinutes);
                writer.WriteLine(msg);

                writer.WriteLine();

                if (buildLog.ErrorMessages.Length > 0)
                {
                    writer.WriteLine("========== ERROR LOG ==========");
                }                

                foreach (string message in buildLog.ErrorMessages)
                {
                    writer.WriteLine(message);
                }

                writer.WriteLine();
                writer.WriteLine("========== PROCESS LOG ==========");

                foreach (string message in buildLog.ProcessingMessages)
                {
                    writer.WriteLine(message);
                }
            }

            e.Result = buildLog.ErrorMessages.Length;
        }

        private void Factory_DocumentProcessed(object sender, int e)
        {
            worker.ReportProgress(e);
        }

        private void throttleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            throttleTextbox.Text = e.NewValue.ToString();
        }

        private void textbox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void textbox_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            TextBox tb = (TextBox)e.Source;
            tb.Text = files.First();                        
        }

        private void infileButton_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    infileTextbox.Text = dialog.FileName;
                }
            }
        }

        private void outfileButton_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    outfileTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void convertButton_Click(object sender, RoutedEventArgs e)
        {
            bool hasInfile = File.Exists(infileTextbox.Text);
            bool hasOutDir = Directory.Exists(outfileTextBox.Text);

            if (hasInfile && hasOutDir)
            {
                infileTextbox.IsEnabled = false;
                infileButton.IsEnabled = false;
                outfileTextBox.IsEnabled = false;
                outfileButton.IsEnabled = false;
                throttleSlider.IsEnabled = false;

                FileInfo infile = new FileInfo(infileTextbox.Text);
                DirectoryInfo outdir = new DirectoryInfo(outfileTextBox.Text);
                int throttle = (int)throttleSlider.Value;
                Tuple<FileInfo, DirectoryInfo, int> args = new Tuple<FileInfo, DirectoryInfo, int>(infile, outdir, throttle);
                worker.RunWorkerAsync(args);
            }
            else
            {
                string msg = (hasInfile) 
                    ? "The selected load file does not exist." 
                    : "The selected destination does not exist.";
                MessageBox.Show(msg);
            }
        }
    }
}
