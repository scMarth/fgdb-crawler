using System;
using ESRI.ArcGIS.Geodatabase;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.DataSourcesGDB;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;

namespace fgdb_crawler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            Application.EnableVisualStyles();
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
        }


        private string outfile = "";
        private string explorePath = "";
        private enum workType
        {
            rootDirectory,
            fgdbPath
        }

        int currentWorkType;

        // Select Folder
        private void button1_Click(object sender, EventArgs e)
        {
            // open the path that the user selected in the browser dialog
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    // clear the textBox
                    textBox1.Clear();

                    textBox1.Text = fbd.SelectedPath;
                }
            }
        }

        // Export to CSV
        private void button2_Click(object sender, EventArgs e)
        {
            explorePath = textBox1.Text;
            if (isValidPath(explorePath))
            {
                // Displays SaveFileDialog so the user can save the CSV
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV|*.csv";
                sfd.Title = "Export CSV";
                sfd.ShowDialog();

                outfile = sfd.FileName;

                if (outfile != "")
                {
                    // show the progress bar
                    progressBar1.Visible = true;
                    progressBar1.Style = ProgressBarStyle.Marquee;
                    progressBar1.MarqueeAnimationSpeed = 200;
                    button1.Enabled = false;
                    button2.Enabled = false;
                    textBox1.ReadOnly = true;

                    if (isFGDBPath(explorePath)) currentWorkType = (int)workType.fgdbPath;
                    else currentWorkType = (int)workType.rootDirectory;

                    backgroundWorker1.RunWorkerAsync();
                }
            }else
            {
                MessageBox.Show("Error: Invalid path. Aborting.");
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.MarqueeAnimationSpeed = 0;
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.Value = progressBar1.Minimum;
            progressBar1.Visible = false;
            button1.Enabled = true;
            button2.Enabled = true;
            textBox1.ReadOnly = false;
            MessageBox.Show("Done.");
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (currentWorkType == (int)workType.fgdbPath)
            {
                printCSVHeader(outfile);
                exploreFGDB(explorePath, outfile);
            }else
            {
                exploreRootDirectory(explorePath, outfile);
            }
        }

        private bool isValidPath(string path)
        {

            try
            {
                string[] folders = Directory.GetDirectories(path);
            }catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool isFGDBPath(string path)
        {
            string fileExt = Path.GetExtension(path);
            if (fileExt == ".gdb") return true;
            else return false;
        }

        private static void exploreRootDirectory(string rootPath, string outfile)
        {
            string[] folders = null;
            try
            {
                folders = Directory.GetDirectories(rootPath);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error: Could not find the specified path: \"" + rootPath + "\"");
                Console.WriteLine(exc.InnerException.Message);
                System.Environment.Exit(1);
            }
            Console.WriteLine();
            Console.WriteLine("Processing... please wait.");
            // Print the CSV Header
            printCSVHeader(outfile);

            foreach (string folder in folders) // For each folder in the root path
            {
                try
                {
                    string fileExt = Path.GetExtension(folder); // Get this folder's file extension
                    if (fileExt != ".gdb") continue; // If the folder is not a File GeoDatabase, skip it
                    exploreFGDB(folder, outfile); // Process the File GeoDatabase
                }
                catch (Exception)
                {
                    continue;
                }

            }
        }

        // Print the CSV Header to the output CSV file 'outfile'
        private static void printCSVHeader(string outfile)
        {
            string csvHeader = "Full Path,Local Path,Name,Category,Type,Full Name,Last Accessed (GMT),Time Created (GMT),Date Modified (GMT),File Size on Disk";
            csvHeader += Environment.NewLine;
            System.IO.File.WriteAllText(outfile, csvHeader);
        }


        // Append string 'str' to output CSV file 'filename'
        private static void appendToFile(string filename, string str)
        {
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(filename, true))
            {
                file.Write(str);
                file.Flush();
                file.Close();
            }
        }

        // Explore the File GeoDatabase specified in fgdbPath, print information about its contents to the CSV file 'outfile'
        private static void exploreFGDB(string fgdbPath, string outfile)
        {
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(typeof(FileGDBWorkspaceFactoryClass));

            IFeatureWorkspace ifw;

            try
            {
                ifw = (IFeatureWorkspace)workspaceFactory.OpenFromFile(fgdbPath, 0);
            }catch (COMException)
            {
                return;
            }
            
            if (ifw == null) return;

            IWorkspace fw = workspaceFactory.OpenFromFile(fgdbPath, 0);

            IEnumDataset datasets = fw.Datasets[esriDatasetType.esriDTAny];

            processIEnumDataset(outfile, ifw, datasets, 0, "", fgdbPath);
        }

        // Recursively explore an IEnumDataset
        private static void processIEnumDataset(string outfile, IFeatureWorkspace ifw, IEnumDataset datasets, int depth, string localPath, string fgdbPath)
        {
            if (datasets == null) return;

            datasets.Reset();
            IDataset dataset;

            // Iterate through the entire dataset
            while (null != (dataset = datasets.Next()))
            {
                // Process the current dataset
                processIDataset(outfile, ifw, dataset, depth, localPath, fgdbPath);
            }
        }

        private static void processIDataset(string outfile, IFeatureWorkspace ifw, IDataset dataset, int depth, string localPath, string fgdbPath)
        {
            string newLocalPath = localPath + "/" + dataset.Name.ToString();

            // Print information about this dataset to the CSV
            appendToFile(outfile, fgdbPath + ",");
            appendToFile(outfile, newLocalPath + ",");
            appendToFile(outfile, dataset.Name.ToString() + ",");
            appendToFile(outfile, dataset.Category.ToString() + ",");
            appendToFile(outfile, dataset.Type.ToString() + ",");
            appendToFile(outfile, dataset.FullName.ToString() + ",");

            printIDatasetSizeAndTime(outfile, ifw, dataset.Name.ToString(), depth);

            // If this dataset has children, iterate through its children
            IEnumDataset children = dataset.Subsets;
            processIEnumDataset(outfile, ifw, children, depth + 1, newLocalPath, fgdbPath);
        }

        // Print the IDataset's size and time. If it doesn't have any, print a message and then return
        private static void printIDatasetSizeAndTime(string outfile, IFeatureWorkspace ifw, string name, int depth)
        {

            try
            {
                var pTable = ifw.OpenTable(name);
                if (pTable == null) return;

                var pDFS = (IDatasetFileStat)pTable;
                if (pDFS == null) return;

                // Date Modified
                var unixtimestmap = pDFS.StatTime[esriDatasetFileStatTimeMode.esriDatasetFileStatTimeLastModification];
                appendToFile(outfile, unixTimeStampToString(unixtimestmap) + ",");

                // Time Created
                unixtimestmap = pDFS.StatTime[esriDatasetFileStatTimeMode.esriDatasetFileStatTimeCreation];
                appendToFile(outfile, unixTimeStampToString(unixtimestmap) + ",");

                // Last Accessed
                unixtimestmap = pDFS.StatTime[esriDatasetFileStatTimeMode.esriDatasetFileStatTimeLastAccess];
                appendToFile(outfile, unixTimeStampToString(unixtimestmap) + ",");

                // File Size on Disk
                string sizeBytes = pDFS.StatSize.ToString();
                appendToFile(outfile, convertBytes(sizeBytes) + Environment.NewLine);
            }
            catch (COMException)
            {
                // Can't open table ; can't get info about date modified / time created / last accessed / file size on disk
                // so fields will be left blank in the CSV
                appendToFile(outfile, ",,," + Environment.NewLine);
                return;
            }
        }

        // Convert a Unix timestaop to a readable string
        private static string unixTimeStampToString(int timestamp)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime dtResult = dt.AddSeconds(Convert.ToDouble(timestamp));
            return "'" + dtResult.ToString() + "'";
            //return "'" + dtResult.ToShortDateString() + " " + dtResult.ToLongTimeString();
        }

        // Given a number of bytes, return a readable string to 2 decimal places
        private static string convertBytes(string numBytes)
        {
            double bytes = double.Parse(numBytes);

            string result;

            if (bytes >= 1099511627776)
            {
                double terabytes = bytes / 1099511627776;
                result = Math.Round(Convert.ToDecimal(terabytes), 2).ToString() + " TB";
            }
            else if (bytes >= 1073741824)
            {
                double gigabytes = bytes / 1073741824;
                result = Math.Round(Convert.ToDecimal(gigabytes), 2).ToString() + " GB";
            }
            else if (bytes >= 1048576)
            {
                double megabytes = bytes / 1048576;
                result = Math.Round(Convert.ToDecimal(megabytes), 2).ToString() + " MB";
            }
            else if (bytes >= 1024)
            {
                double kilobytes = bytes / 1024;
                result = Math.Round(Convert.ToDecimal(kilobytes), 2).ToString() + " KB";
            }
            else
            {
                result = Math.Round(Convert.ToDecimal(bytes), 2).ToString() + " bytes";
            }
            return result;
        }
    }
}