using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MetadataExtractor;




namespace SD_Quick_View
{
    public partial class frmMain : Form
    {

        // PictureBoxes we use to display thumbnails.
        private List<PictureBox> PictureBoxes = new List<PictureBox>();

        // Thumbnail sizes.
        private int ThumbWidth = 10;
        private int ThumbHeight = 10;
        private int ThumbMultiplier = 5;

        // Import used for tooltip
        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern Int32 StrFormatByteSize(
            long fileSize,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer,
            int bufferSize);

        // Main form
        public frmMain()
        {
            InitializeComponent();
            
        }

        // Start with the startup path selected.
        private void frmMain_Load(object sender, EventArgs e)
        {
            // Comment out these three lines if desired to have the application start with empty path
            string path = Path.Combine(Application.StartupPath, "..\\..");
            DirectoryInfo dir_info = new DirectoryInfo(path);
            txtDirectory.Text = dir_info.FullName;
        }



        // Let the user select a folder.
        private void btnPickDirectory_Click(object sender, EventArgs e)
        {
            fbdDirectory.SelectedPath = txtDirectory.Text;
            if (fbdDirectory.ShowDialog() == DialogResult.OK)
            {
                txtDirectory.Text = fbdDirectory.SelectedPath;

            }
        }

     

        // Display thumbnails for the selected directory.
        private void txtDirectory_TextChanged(object sender, EventArgs e)
        {

            ThumbWidth = margVal.Value * ThumbMultiplier;
            ThumbHeight = margVal.Value * ThumbMultiplier;

            // Delete the old PictureBoxes and clear the display
            flpThumbnails.Controls.Clear();
            foreach (PictureBox pic in PictureBoxes)
            {
                pic.Image.Dispose();
                
                // Remove events              
                pic.DoubleClick -= PictureBox_DoubleClick!;
                pic.Click -= PictureBox_Click!;
                pic.MouseEnter -= PictureBox_MouseOver!;
                pic.Dispose();
                
                
            }

            //flpThumbnails.Controls.Clear();
            
            // Get the PictureBoxes array ready for a new folder.  Just clear it, no need to redefine and leave the old one lingering.  MEMORY LEAK FIX
            //PictureBoxes = new List<PictureBox>();
            PictureBoxes.Clear();

            // Clear old text
            txtParams.Text = "";

            // If the directory doesn't exist, do nothing else.
            if (!System.IO.Directory.Exists(txtDirectory.Text)) return;

            // Get the names of the files in the directory.
            List<string> filenames = new List<string>();
            string[] patterns = { "*.png", "*.gif", "*.jpg", "*.bmp", "*.tif" };
            foreach (string pattern in patterns)
            {
                filenames.AddRange(System.IO.Directory.GetFiles(txtDirectory.Text,
                    pattern, SearchOption.TopDirectoryOnly));
            }
            filenames.Sort();

            // Load the files.
            foreach (string filename in filenames)
            {
                // Load the picture into a PictureBox.
                PictureBox pic = new PictureBox();

                // Check to ensure filename and path limit lengths are OK

                bool badImage = false;

                // Setup our string to display is it's too long
                string errorNotifier = "Certain images were not loaded because their filepath and filename are too long for your OS:\r\n";


                if (filename.Length > 256)
                {
                    // Add the filepath and filename to the error message
                    errorNotifier += filename + "\r\n";

                    // Set that this iteration through the foreach is no good
                    badImage = true;

                    // Show error
                    txtParams.Text += errorNotifier + "\r\n";
                }

                if (badImage == false)
                {

                    pic.ClientSize = new Size(ThumbWidth, ThumbHeight);

                    pic.Image = new Bitmap(filename);

                    // If the image is too big, zoom.
                    if ((pic.Image.Width > ThumbWidth) ||
                        (pic.Image.Height > ThumbHeight))
                    {
                        pic.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                    else
                    {
                        pic.SizeMode = PictureBoxSizeMode.CenterImage;
                    }

                    // Add the Single Click event handler.
                    pic.Click += PictureBox_Click!;


                    // Add the DoubleClick event handler.
                    pic.DoubleClick += PictureBox_DoubleClick!;

                    // Add mouseover handler
                    pic.MouseEnter += PictureBox_MouseOver!;

                    // Add a tooltip.
                    FileInfo file_info = new FileInfo(filename);
                    tipPicture.SetToolTip(pic, file_info.Name +
                        "\nCreated: " + file_info.CreationTime.ToShortDateString() +
                        "\n(" + pic.Image.Width + " x " + pic.Image.Height + ") " +
                        ToFileSizeApi(file_info.Length));
                    pic.Tag = file_info;

                    // Add the PictureBox to the FlowLayoutPanel.
                    pic.Parent = flpThumbnails;
                    
                    // Add pic to our array so we can dispose it later (MEMORY LEAK FIX! YAY!)
                    PictureBoxes.Add(pic);
                }
            }
        }

        // Return a file size created by the StrFormatByteSize API function.
        public static string ToFileSizeApi(long file_size)
        {
            StringBuilder sb = new StringBuilder(20);
            StrFormatByteSize(file_size, sb, 20);
            return sb.ToString();
        }

        private void PictureBox_MouseOver(object sender, EventArgs e)
        {
            PictureBox pic = sender as PictureBox ?? throw new ArgumentException(); ;
            FileInfo file_into = pic.Tag as FileInfo ?? throw new ArgumentException(); 
            showData(file_into);

        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            // Save params to clipboard
            // Grab the full prompt text, leave if it is empty
            string s = txtParams.Text;
            if (s == "") { return; }

            // Copies to clipboard
            Clipboard.SetText(s);

        }

        // Open the file.
        private void PictureBox_DoubleClick(object sender, EventArgs e)
        {
            // Get the file's information.
            PictureBox pic = sender as PictureBox ?? throw new ArgumentException(); ;
            FileInfo file_into = pic.Tag as FileInfo ?? throw new ArgumentException(); ;

            // "Start" the file.
            //Process.Start(file_into.FullName);

            // Fix for newer .NET
            Process.Start("explorer.exe", file_into.FullName);

        }

        private void margVal_MouseUp(object sender, MouseEventArgs e)
        {
            txtDirectory_TextChanged(txtDirectory.Text, e);
        }

        private void margVal_KeyUp(object sender, KeyEventArgs e)
        {
            txtDirectory_TextChanged(txtDirectory.Text, e);
        }


        private void showData(FileInfo f)
        {
            // Clear
            txtParams.Text = "";

            // Grab the EXIF data and show it in the parameters textbox
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(f.FullName.ToString());
            foreach (var directory in directories)
                foreach (var tag in directory.Tags)
                {
                    if (tag.Name == "Textual Data")
                    {
                        // Old style of EXIF pull.  Left for reference.
                        //textBox1.Text += ($"{directory.Name} \r\n - {tag.Name} = {tag.Description}");
                        //textBox1.Text += "\n";

                        // Parse text
                        string parsed = ($"{tag.Description}").Replace("Negative prompt:", "\r\n\r\nNegative Prompt:");
                        parsed = parsed.Replace("Steps:", "\r\n\r\nSteps:");
                        parsed = parsed.Replace("Sampler:", "\r\nSampler:");
                        parsed = parsed.Replace("CFG scale:", "\r\nCFG scale:");
                        parsed = parsed.Replace("Seed:", "\r\nSeed:");
                        parsed = parsed.Replace("Size:", "\r\nSize:");
                        parsed = parsed.Replace("Model hash:", "\r\nModel hash:");
                        parsed = parsed.Replace("Model:", "\r\nModel:");
                        parsed = parsed.Replace("Denoising strength:", "\r\nDenoising strength:");

                        // Update the textbox with the parsed string
                        txtParams.Text += parsed;
                    }
                }
        }
    }
}