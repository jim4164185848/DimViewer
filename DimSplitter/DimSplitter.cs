using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using System.Xml;

namespace DimSplitter
{
    /// <summary>
    /// Main Windows Forms application for DimViewer - Image and PDF Tiling System
    /// 
    /// DimSplitter is a professional image and PDF processing tool that converts large images
    /// and multi-page PDFs into tiled web-viewable formats. The application splits source files
    /// into 260x260 pixel tiles at multiple zoom levels for optimal web performance.
    /// 
    /// Key Features:
    /// - Multi-format support: JPEG, PNG, GIF, BMP, PDF
    /// - Ghostscript integration for high-quality PDF processing
    /// - Multiple zoom levels (1-5) for detailed viewing
    /// - Thumbnail generation for quick navigation
    /// - Batch processing capabilities
    /// - Trial/licensed version support with watermarking
    /// - Rich text annotations per file
    /// 
    /// The application generates JavaScript configuration files and web assets for
    /// seamless integration with web-based image viewers.
    /// </summary>
    public partial class DimSplitter : Form
    {
        #region Application Constants
        
        /// <summary>
        /// Maximum number of pages that can be processed in a single batch operation
        /// Limit imposed for performance and memory management
        /// </summary>
        public const int PAGE_MAX = 300;
        
        /// <summary>
        /// Maximum resolution DPI allowed for image processing
        /// Higher resolutions may cause memory issues
        /// </summary>
        public const int MAX_RESO = 300;
        
        /// <summary>
        /// Minimum value for initial dimensions (width/height)
        /// Prevents creation of unusably small images
        /// </summary>
        public const int MIN_INI = 10;
        
        /// <summary>
        /// Default maximum resolution DPI used when application starts
        /// Balances quality and performance for typical use cases
        /// </summary>
        public const int D_MAX_RESO = 180;
        
        /// <summary>
        /// Standard tile width in pixels
        /// Fixed size ensures consistent web viewer performance
        /// </summary>
        public const int TILEX = 260;
        
        /// <summary>
        /// Standard tile height in pixels
        /// Fixed size ensures consistent web viewer performance
        /// </summary>
        public const int TILEY = 260;
        
        /// <summary>
        /// Default initial height for generated images
        /// </summary>
        public const int INI_HIGHT = 600;
        
        /// <summary>
        /// Default initial width for generated images
        /// </summary>
        public const int INI_WEIGHT = 800;
        
        /// <summary>
        /// Default thumbnail height in pixels
        /// Used for navigation thumbnails in web viewer
        /// </summary>
        public const int THUMB_HIGHT = 100;
        
        /// <summary>
        /// Default thumbnail width in pixels
        /// Used for navigation thumbnails in web viewer
        /// </summary>
        public const int THUMB_WEIGHT = 100;
        
        /// <summary>
        /// Current version of DimViewer application
        /// Used in watermarking and file generation
        /// </summary>
        public const string dimversion = "2.1";
        
        /// <summary>
        /// Watermark text applied to trial version images
        /// Identifies unlicensed usage for protection
        /// </summary>
        public const string watermarkText = "dimviewer " + dimversion + " trial version";
        
        #endregion

        #region Public Instance Fields
        
        /// <summary>
        /// Stores the file extension of the last selected file
        /// Used to optimize file filter selection in open dialogs
        /// </summary>
        public string lastfiletype = "";
        
        /// <summary>
        /// Current file filter string for open dialogs
        /// Dynamically adjusted based on last file type selected
        /// </summary>
        public string filterstring = "";
        
        /// <summary>
        /// Path to the Ghostscript DLL file
        /// Required for PDF processing functionality
        /// </summary>
        public string apppathfile = "";
        
        /// <summary>
        /// Total number of pages processed in current session
        /// Used for progress tracking and validation
        /// </summary>
        public int totalPage = 0;
        
        /// <summary>
        /// Flag indicating whether thumbnail generation is enabled
        /// 0 = disabled, 1 = enabled
        /// </summary>
        public int thumb = 0;
        
        /// <summary>
        /// Processing start time for performance tracking
        /// Displayed in completion message
        /// </summary>
        public string starttime;
        
        /// <summary>
        /// TextWriter for generating JavaScript configuration files
        /// Contains viewer settings and tile information
        /// </summary>
        public TextWriter tw;
        
        #endregion

        #region Static Fields
        
        /// <summary>
        /// Collection of page information strings for each zoom level
        /// Contains tile coordinates and references for web viewer
        /// </summary>
        public static ArrayList pagestring = new ArrayList();
        
        /// <summary>
        /// Rich text annotations associated with each input file
        /// Allows users to add descriptions or notes per file
        /// </summary>
        public static ArrayList richTextString = new ArrayList();
        
        /// <summary>
        /// Rich text content for page-level annotations
        /// Used in generated JavaScript configuration
        /// </summary>
        public static ArrayList richTextPageString = new ArrayList();
        
        /// <summary>
        /// Zoom ratio information for X-axis scaling
        /// Used by web viewer for proper image scaling
        /// </summary>
        public static string zoomratiox;
        
        /// <summary>
        /// Zoom ratio information for Y-axis scaling
        /// Used by web viewer for proper image scaling
        /// </summary>
        public static string zoomratioy;
        
        /// <summary>
        /// Trial mode flag: 1 = trial version, 0 = licensed version
        /// Controls watermarking and feature restrictions
        /// </summary>
        public static int trial = 1;
        
        /// <summary>
        /// URL for application help and support resources
        /// Can be customized via license file configuration
        /// </summary>
        public static string strHelp = "http://www.dimviewer.com/support.aspx";
        
        /// <summary>
        /// License key for registered versions
        /// Default GUID indicates trial version
        /// </summary>
        public static string strKey = "00000000-0000-0000-0000-000000000000";
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the DimSplitter main form
        /// Sets up the user interface and default values
        /// </summary>
        public DimSplitter()
        {
            InitializeComponent();
        }

        #endregion

        #region File Management Event Handlers
        
        /// <summary>
        /// Handles the Add Files button click event
        /// Opens a file dialog to select multiple image or PDF files for processing
        /// 
        /// Features:
        /// - Multi-file selection support
        /// - Dynamic file filter based on last selected file type
        /// - Inserts files at selected position or appends to end
        /// - Maintains rich text annotations list in sync with file list
        /// - Optimizes file filter order based on user workflow
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void button1_Click(object sender, EventArgs e)
        {
            // Initialize file dialog with multi-select capability
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open Image Files";
            fDialog.Filter = filterstring;
            fDialog.Multiselect = true;
            
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                // Process each selected file
                for (int i = 0; i < fDialog.FileNames.Length; i++)
                {
                    // Insert at selected position if an item is selected
                    if (listBox1.SelectedIndex != -1)
                    {
                        int isel = listBox1.SelectedIndex;
                        listBox1.Items.Insert(isel,fDialog.FileNames[i].ToString());
                        richTextString.Insert(isel, ""); // Insert empty annotation
                        
                        // Update rich text display if selection is maintained
                        if (listBox1.SelectedIndex != -1)
                            richTextBox1.Text = richTextString[listBox1.SelectedIndex].ToString();
                    }
                    else
                    {
                        // Append to end of list if no selection
                        listBox1.Items.Add(fDialog.FileNames[i].ToString());
                        richTextString.Add(""); // Add empty annotation
                        richTextBox1.Text = "";
                    }
                    
                    // Extract file extension for filter optimization
                    lastfiletype = fDialog.FileNames[i].ToString().Substring(fDialog.FileNames[i].ToString().LastIndexOf("."), fDialog.FileNames[i].ToString().Length - fDialog.FileNames[i].ToString().LastIndexOf("."));
                }
                
                // Optimize file filter based on last selected file type
                // PDF files first if user selected PDF, images first otherwise
                if (lastfiletype == ".pdf")
                    filterstring = "PDF|*.pdf|Image Files|*.jpg;*.gif;*.bmp;*.png;*.jpeg";
                else
                    filterstring = "Image Files|*.jpg;*.gif;*.bmp;*.png;*.jpeg|PDF|*.pdf";
            }
        }
        
        /// <summary>
        /// Handles the Remove Selected File button click event
        /// Removes the currently selected file from the processing list
        /// Also removes the corresponding rich text annotation
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void button2_Click(object sender, EventArgs e)
        {
            // Only remove if a file is actually selected
            if (listBox1.SelectedIndex != -1)
            {
                int isel = listBox1.SelectedIndex;
                listBox1.Items.RemoveAt(isel);          // Remove from file list
                richTextString.RemoveAt(isel);          // Remove corresponding annotation
                richTextBox1.Text = "";                 // Clear annotation display
            }
        }

        /// <summary>
        /// Handles the Clear All Files button click event
        /// Removes all files from the processing list and clears all annotations
        /// Provides a quick way to start fresh with a new batch
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();      // Clear all files from list
            richTextString.Clear();      // Clear all annotations
            richTextBox1.Text = "";      // Clear annotation display
        }

        /// <summary>
        /// Handles the Move Up button click event
        /// Moves the selected file one position up in the processing order
        /// Maintains the association between files and their rich text annotations
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void moveup_Click(object sender, EventArgs e)
        {
            // Can only move up if item is selected and not already at top
            if ((listBox1.SelectedIndex != -1) && (listBox1.SelectedIndex != 0))
            {
                int index = listBox1.SelectedIndex;
                
                // Move the file item up one position
                listBox1.Items.Insert(index - 1, listBox1.SelectedItem);
                richTextString.Insert(index - 1, richTextString[index]);
                
                // Remove the original items (now at index + 1 due to insertion)
                listBox1.Items.RemoveAt(index + 1);
                richTextString.RemoveAt(index + 1);
                
                // Maintain selection on the moved item
                listBox1.SelectedIndex = index - 1;
            }
        }

        /// <summary>
        /// Handles the Move Down button click event
        /// Moves the selected file one position down in the processing order
        /// Maintains the association between files and their rich text annotations
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void movedown_Click(object sender, EventArgs e)
        {
            // Can only move down if item is selected and not already at bottom
            if ((listBox1.SelectedIndex != -1) && (listBox1.SelectedIndex != listBox1.Items.Count-1))
            {
                int index = listBox1.SelectedIndex;
                
                // Move the file item down one position
                listBox1.Items.Insert(index + 2, listBox1.SelectedItem);
                richTextString.Insert(index + 2, richTextString[index]);
                
                // Remove the original items
                listBox1.Items.RemoveAt(index);
                richTextString.RemoveAt(index);
                
                // Maintain selection on the moved item
                listBox1.SelectedIndex = index + 1;
            }
        }

        #endregion

        #region Form Events and Initialization
        
        /// <summary>
        /// Handles the form load event - initializes the application
        /// 
        /// Performs the following initialization tasks:
        /// - Sets up Ghostscript DLL path and cleanup
        /// - Configures default UI values and settings
        /// - Initializes zoom level and PDF box options
        /// - Checks for license file and validates registration
        /// - Sets trial mode watermarking if unlicensed
        /// - Configures help URL from license settings
        /// </summary>
        /// <param name="sender">The form that triggered this event</param>
        /// <param name="e">Event arguments containing form load details</param>
        private void DimSplitter_Load(object sender, EventArgs e)
        {
            // Clean up any existing Ghostscript DLL from previous sessions
            apppathfile = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) +"\\gsdll32.dll";
            if (System.IO.File.Exists(apppathfile))
                System.IO.File.Delete(apppathfile);
            
            // Initialize default UI values
            textBox1.Text = @"C:\DimImages";                    // Default output folder
            textBox2.Text = "ImagePrefix";                      // Default file prefix
            textBox3.Text = D_MAX_RESO.ToString();              // Default max resolution (180 DPI)
            textBox5.Text = INI_WEIGHT.ToString();              // Default initial width (800px)
            textBox4.Text = INI_HIGHT.ToString();               // Default initial height (600px)
            textBox6.Text = THUMB_WEIGHT.ToString();            // Default thumbnail width (100px)
            textBox7.Text = THUMB_HIGHT.ToString();             // Default thumbnail height (100px)
            
            // Configure UI behavior
            textBox1.ReadOnly = true;                           // Output folder is browse-only
            listBox1.Select();                                  // Focus on file list
            progressBar1.Visible = false;                       // Hide progress bar initially
            
            // Set up file filters for open dialog
            filterstring = "PDF/Image Files|*.jpg;*.gif;*.bmp;*.png;*.jpeg;*.pdf";
            
            // Initialize zoom level dropdown (1-5 levels)
            comboBox1.Items.Add("1");
            comboBox1.Items.Add("2");
            comboBox1.Items.Add("3");
            comboBox1.Items.Add("4");
            comboBox1.Items.Add("5");
            comboBox1.SelectedIndex = 2;                           // Default to 3 zoom levels
            
            // Initialize PDF box type options for PDF processing
            comboBox2.Items.Add("MediaBox");                    // Full page boundary
            comboBox2.Items.Add("CropBox");                     // Visible page area
            comboBox2.Items.Add("TrimBox");                     // Final trimmed area
            comboBox2.SelectedIndex = 0;                        // Default to MediaBox
            
            // Configure dropdown styles (no manual text entry)
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            
            // Enable thumbnail generation by default
            checkBox2.Checked = true;
            label8.Visible = false;                             // Hide processing status initially

            // License validation and trial mode detection
            try
            {
                string licensefile = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\dimviewer.lic";
                if (File.Exists(licensefile))
                {
                    // Parse XML license file
                    XmlDocument document = new XmlDocument();
                    StreamReader license = null;
                    license = new StreamReader(licensefile);
                    document.LoadXml(license.ReadToEnd());
                    
                    // Validate license key against known valid keys
                    XmlNode licensenode = document.SelectSingleNode("//Data/Key/text()");
                    if (key.keyList.Contains(licensenode.Value))
                    {
                        strKey = licensenode.Value;
                        trial = 0;                              // Disable trial mode
                    }
                    
                    // Extract custom help URL if provided
                    try
                    {
                        XmlNode helpnode = document.SelectSingleNode("//Data/Help/text()");
                        strHelp = helpnode.Value;
                    }
                    catch (Exception)
                    {
                        // Use default help URL if custom URL not found
                    }
                }
            }
            catch(Exception)
            {
                // License file parsing failed - remain in trial mode
            }
            
            // Update window title to indicate trial mode
            if (trial == 1)
                this.Text = this.Text + " Trial";
        }

        /// <summary>
        /// Handles the Browse Output Folder button click event
        /// Opens a folder browser dialog to select the output directory
        /// where processed tiles and web assets will be generated
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void output_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fDialog = new FolderBrowserDialog();
            fDialog.RootFolder = Environment.SpecialFolder.Desktop;  // Start at desktop
            fDialog.SelectedPath = textBox1.Text.ToString();         // Use current path
            fDialog.Description = "Select the source directory";
            
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fDialog.SelectedPath.ToString();     // Update output path
            }
        }
        
        #endregion

        #region UI State Management
        
        /// <summary>
        /// Disables all user interface controls during processing
        /// Prevents user interaction that could interfere with background operations
        /// Shows processing status indicator
        /// </summary>
        public void disablebutton()
        {
            // Disable all file management buttons
            button1.Enabled = false;        // Add Files
            button2.Enabled = false;        // Remove Selected
            button3.Enabled = false;        // Start Processing
            button4.Enabled = false;        // Clear All
            output.Enabled = false;         // Browse Output Folder
            moveup.Enabled = false;         // Move Up
            movedown.Enabled = false;       // Move Down
            
            // Disable all input controls
            textBox1.Enabled = false;       // Output folder
            textBox2.Enabled = false;       // Image prefix
            textBox3.Enabled = false;       // Max resolution
            textBox4.Enabled = false;       // Initial height
            textBox5.Enabled = false;       // Initial width
            textBox6.Enabled = false;       // Thumbnail width
            textBox7.Enabled = false;       // Thumbnail height
            
            // Disable dropdown and list controls
            comboBox1.Enabled = false;      // Zoom levels
            comboBox2.Enabled = false;      // PDF box type
            listBox1.Enabled = false;       // File list
            richTextBox1.Enabled = false;   // Rich text annotations
            
            // Disable checkboxes and show processing indicator
            checkBox1.Enabled = false;      // Use initial size for PDFs
            checkBox2.Enabled = false;      // Generate thumbnails
            label8.Visible = true;          // Show "Processing..." indicator
        }
        
        /// <summary>
        /// Re-enables all user interface controls after processing completion
        /// Restores normal user interaction capabilities
        /// Hides processing status indicator
        /// </summary>
        public void enablebutton()
        {
            // Re-enable all file management buttons
            button1.Enabled = true;         // Add Files
            button2.Enabled = true;         // Remove Selected
            button3.Enabled = true;         // Start Processing
            button4.Enabled = true;         // Clear All
            output.Enabled = true;          // Browse Output Folder
            moveup.Enabled = true;          // Move Up
            movedown.Enabled = true;        // Move Down
            
            // Re-enable all input controls
            textBox1.Enabled = true;        // Output folder
            textBox2.Enabled = true;        // Image prefix
            textBox3.Enabled = true;        // Max resolution
            textBox4.Enabled = true;        // Initial height
            textBox5.Enabled = true;        // Initial width
            textBox6.Enabled = true;        // Thumbnail width
            textBox7.Enabled = true;        // Thumbnail height
            
            // Re-enable dropdown and list controls
            comboBox1.Enabled = true;       // Zoom levels
            comboBox2.Enabled = true;       // PDF box type
            listBox1.Enabled = true;        // File list
            richTextBox1.Enabled = true;    // Rich text annotations
            
            // Re-enable checkboxes and hide processing indicator
            checkBox1.Enabled = true;       // Use initial size for PDFs
            checkBox2.Enabled = true;       // Generate thumbnails
            label8.Visible = false;         // Hide "Processing..." indicator
        }
        
        #endregion

        #region Resource Management
        
        /// <summary>
        /// Extracts embedded resource content as a string
        /// Used to retrieve JavaScript templates and other text resources
        /// embedded within the application assembly
        /// </summary>
        /// <param name="resourceName">Full name of the embedded resource</param>
        /// <returns>String content of the embedded resource</returns>
        private string GetFromResources(string resourceName)
        {
            Assembly assem = this.GetType().Assembly;
            using (Stream stream = assem.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        
        #endregion

        #region Processing Control
        
        /// <summary>
        /// Handles the Start Processing button click event
        /// Validates all input parameters and initiates the tile generation process
        /// 
        /// Validation includes:
        /// - Resolution limits (1-300 DPI)
        /// - Minimum dimension requirements (>10 pixels)
        /// - Output folder existence
        /// - Conflicting output files check
        /// 
        /// Processing workflow:
        /// 1. Extract and validate all user parameters
        /// 2. Check output folder for existing files
        /// 3. Extract embedded resources (JS, images)
        /// 4. Initialize background worker with parameters
        /// 5. Start tile generation process
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void button3_Click(object sender, EventArgs e)
        {
            // Initialize processing parameters from UI controls
            string folder = "";
            string prefixname = "";
            string iniwidth = "";
            string iniheight = "";
            string thumbwidth = "";
            string thumbheight = "";
            int zoom = 1;
            int pdfbox = 0;
            int useinipdf = 0;
            int maxresolution = 0;
            
            // Extract basic parameters
            folder = textBox1.Text.ToString();
            prefixname = textBox2.Text.ToString();
            iniwidth = textBox5.Text.ToString();
            iniheight = textBox4.Text.ToString();
            starttime = DateTime.Now.ToString();
            
            // Validate maximum resolution parameter
            try
            {
                maxresolution = Convert.ToInt16(textBox3.Text.ToString());
                if ((maxresolution > MAX_RESO) || (maxresolution <= 0))
                {
                    MessageBox.Show("Max resolution should be between 0 and " + MAX_RESO.ToString() + ".", "Can not process...");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Max resolution should be an integer.", "Can not process...");
                return;
            }
            try
            {
                iniwidth = textBox5.Text.ToString();
                if (Convert.ToInt16(iniwidth) < MIN_INI)
                {
                    MessageBox.Show("Initial width should be more than " + MIN_INI.ToString() + ".", "Can not process...");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initial width should be an integer.", "Can not process...");
                return;
            }
            try
            {
                iniheight = textBox4.Text.ToString();
                if (Convert.ToInt16(iniheight) < MIN_INI)
                {
                    MessageBox.Show("Initial height should be more than " + MIN_INI.ToString() + ".", "Can not process...");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initial heigh should be an integer.", "Can not process...");
                return;
            }

            try
            {
                thumbwidth = textBox6.Text.ToString();
                if (Convert.ToInt16(thumbwidth) < MIN_INI)
                {
                    MessageBox.Show("Thumbnail width should be more than " + MIN_INI.ToString() + ".", "Can not process...");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Thumbnail width should be an integer.", "Can not process...");
                return;
            }
            try
            {
                thumbheight = textBox7.Text.ToString();
                if (Convert.ToInt16(thumbheight) < MIN_INI)
                {
                    MessageBox.Show("Thumbnail height should be more than " + MIN_INI.ToString() + ".", "Can not process...");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Thumbnail heigh should be an integer.", "Can not process...");
                return;
            }
            if (!Directory.Exists(folder))
            {
                MessageBox.Show("Output folder doesn't exist.", "Can not process...");
                return;
            }
            string[] files = Directory.GetFiles(folder, textBox2.Text.ToString() + "*.*", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                MessageBox.Show("Output images exist in the output folder,Please change output folder or image pre-fix name.", "Can not process...");
            }
            else
            {
                listBox1.SelectedIndex = -1;
                if (listBox1.Items.Count != 0)
                {
                    zoom = Convert.ToInt16(comboBox1.SelectedItem.ToString());
                    if (comboBox2.SelectedItem.ToString() == "MediaBox")
                        pdfbox = 0;
                    if (comboBox2.SelectedItem.ToString() == "CropBox")
                        pdfbox = 1;
                    if (comboBox2.SelectedItem.ToString() == "TrimBox")
                        pdfbox = 2;
                    if (checkBox1.Checked == true)
                        useinipdf = 1;
                    else
                        useinipdf = 0;
                    if (checkBox2.Checked == true)
                        thumb = 1;
                    else
                        thumb = 0;
                    disablebutton();
                    progressBar1.Visible = true;
                    TextWriter dimviewerjs = null;
                    try
                    {
                        var a = this.GetType().Assembly.GetManifestResourceNames();

                        dimviewerjs = new StreamWriter(folder + "\\dimviewer" + dimversion + ".js");
                        dimviewerjs.Write(GetFromResources("DimViewer.Resources.dimviewer.txt"));
                        dimviewerjs.Close();
                        dimviewerjs.Dispose();

                        Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DimViewer.Resources.fullscreen.png");
                        FileStream fileStream = new FileStream(folder + "\\fullscreen.png", FileMode.CreateNew);
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(fileStream);
                        fileStream.Close();
                        fileStream.Dispose();

                        Stream stream1 = Assembly.GetExecutingAssembly().GetManifestResourceStream("DimViewer.Resources.zoomin.png");
                        FileStream fileStream1 = new FileStream(folder + "\\zoomin.png", FileMode.CreateNew);
                        stream1.Seek(0, SeekOrigin.Begin);
                        stream1.CopyTo(fileStream1);
                        fileStream1.Close();
                        fileStream1.Dispose();

                        Stream stream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("DimViewer.Resources.zoomout.png");
                        FileStream fileStream2 = new FileStream(folder + "\\zoomout.png", FileMode.CreateNew);
                        stream2.Seek(0, SeekOrigin.Begin);
                        stream2.CopyTo(fileStream2);
                        fileStream2.Close();
                        fileStream2.Dispose();

                        pagestring.Clear();
                        richTextPageString.Clear();
                        for (int i = 0; i < zoom; i++)
                        {
                            pagestring.Add("");
                        }
                        if (thumb == 1)
                            pagestring.Add("");
                        zoomratiox = "";
                        zoomratioy = "";
                        tw = new StreamWriter(folder + "\\" + prefixname + "_viewer.js");
                        tw.WriteLine("{");
                        tw.WriteLine("\"1\": \"0\",");
                        tw.WriteLine("\"2\": \"" + zoom + "\",");
                        tw.WriteLine("\"3\": \"" + TILEX + "\",");
                        tw.WriteLine("\"4\": \"" + TILEY + "\",");
                        tw.WriteLine("\"5\": \"20\",");
                        tw.WriteLine("\"6\": \"300\",");
                        List<string> imagelist = listBox1.Items.OfType<string>().ToList();

                        // The parameters you want to pass to the do work event of the background worker.
                        object[] parameters = new object[] { imagelist, folder, prefixname, maxresolution, zoom, iniwidth, iniheight, pdfbox, useinipdf, thumb, thumbwidth, thumbheight};

                        // This runs the event on new background worker thread.
                        button5.Text = "Cancel";
                        backgroundWorker1.RunWorkerAsync(parameters);
                    }
                    catch(Exception ex)
                    {
                        tw.Close();
                        tw.Dispose();
                        dimviewerjs.Close();
                        dimviewerjs.Dispose();
                        MessageBox.Show("Error message:" + ex.Message, "Uncomplete");
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Cancel/Exit button click event
        /// This button serves dual purposes:
        /// - During processing: Offers to cancel the current operation
        /// - When idle: Exits the application
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void button5_Click(object sender, EventArgs e)
        {
            bool bDone;
            if (backgroundWorker1.IsBusy)
            {
                // Background processing is active - offer to cancel
                DialogResult result = MessageBox.Show("Are you sure you want to cancel the process?", "Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                bDone = (result.ToString() == "Yes") ? true : false;
                if (bDone)
                {
                    backgroundWorker1.CancelAsync();      // Request cancellation
                    button5.Text = "Cancelling...";       // Update button text
                }
            }
            else
                Application.Exit();                       // No processing active - exit application
        }

        /// <summary>
        /// Handles file list selection changes
        /// Loads the rich text annotation for the newly selected file
        /// Ensures single selection mode for consistent behavior
        /// </summary>
        /// <param name="sender">The list box that triggered this event</param>
        /// <param name="e">Event arguments containing selection details</param>
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.SelectionMode = SelectionMode.One;   // Ensure single selection
            if (listBox1.SelectedIndex != -1)
                richTextBox1.Text = richTextString[listBox1.SelectedIndex].ToString();
        }

        #endregion

        #region Background Processing
        
        /// <summary>
        /// Background worker DoWork event handler - performs the actual tile generation
        /// Runs on a separate thread to prevent UI blocking during intensive processing
        /// 
        /// Processing workflow:
        /// 1. Extract parameters passed from UI thread
        /// 2. Create CreateTiles processor instance with all settings
        /// 3. Wire up progress reporting to update UI progress bar
        /// 4. Process each file in the list sequentially
        /// 5. Handle cancellation requests and error conditions
        /// 6. Enforce page limits for performance management
        /// </summary>
        /// <param name="sender">The background worker that triggered this event</param>
        /// <param name="e">Event arguments containing parameters and result handling</param>
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Unpack parameters passed from UI thread
            object[] parameters = e.Argument as object[];
            List<string> imagelist = (List<string>)parameters[0];
            string folder = parameters[1].ToString();
            string prefixname = parameters[2].ToString();
            int maxresolution = Convert.ToInt16(parameters[3].ToString());
            int zoom = Convert.ToInt16(parameters[4].ToString());
            string iniwidth = parameters[5].ToString();
            string iniheight = parameters[6].ToString();
            int pdfbox = Convert.ToInt16(parameters[7].ToString());
            int useinipdf = Convert.ToInt16(parameters[8].ToString());
            int thumb = Convert.ToInt16(parameters[9].ToString());
            string thumbwidth = parameters[10].ToString();
            string thumbheight = parameters[11].ToString();
            int totalPage = 0;

            // Create tile processor with all configuration parameters
            CreateTiles oCreateTiles = new CreateTiles(imagelist, folder, prefixname, maxresolution, zoom, iniwidth, iniheight, pdfbox, useinipdf, thumb, thumbwidth, thumbheight, richTextString);
            
            // Wire up progress reporting to update UI progress bar
            oCreateTiles.ProgressChanged += (s, pe) => backgroundWorker1.ReportProgress(pe.ProgressPercentage);
            oCreateTiles.ComputeProgress(0);
            
            // Process each file in the input list
            for (int i = 0; i < imagelist.Count; i++)
            {
                // Check for user cancellation request
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                
                try
                {
                    // Process current file and get total page count
                    totalPage = oCreateTiles.DoWork(this);

                    // Negative result indicates processing error
                    if (totalPage < 0)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Handle file processing errors
                    MessageBox.Show("Invalid image exist.\nIt get highlights in the Listbox.\nFile name:" + imagelist[i].ToString(), "Can not process...");
                    totalPage = -1 * i; // Negative index indicates which file failed
                    break;
                }
                
                // Enforce maximum page limit for performance
                if (totalPage >= PAGE_MAX)
                {
                    MessageBox.Show("DimViewer only process maxinum " + PAGE_MAX.ToString() + " pages each time due to performance and capacity concerns.\nPlease contact support@dimviewer.com if you need to process over maximum amount pages.", "Completed and Warning");
                    break;
                }
            }
            e.Result = totalPage;
        }
        /// <summary>
        /// Background worker completion handler - finalizes processing and updates UI
        /// Handles successful completion, user cancellation, and error conditions
        /// Generates final JavaScript configuration file with all tile information
        /// </summary>
        /// <param name="sender">The background worker that triggered this event</param>
        /// <param name="e">Event arguments containing completion status and results</param>
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                // Handle user cancellation
                MessageBox.Show("You've cancelled the process.", "Cancel");
                enablebutton();
                progressBar1.Visible = false;
            }
            else
            {
                if (Convert.ToInt16(e.Result) < 0)
                {
                    // Negative result indicates error - highlight problematic file
                    listBox1.SetSelected(Convert.ToInt16(e.Result) * -1, true);
                }
                else
                {
                    // Show successful completion with timing information
                    MessageBox.Show("Process completed.\nStart time " + starttime + "\nEnd time " + DateTime.Now.ToString(), "Completed");
                }
                enablebutton();
                progressBar1.Visible = false;
            }
            if (zoomratiox.ToString().Length > 0)
            {
                zoomratiox = zoomratiox.ToString().Substring(0, zoomratiox.ToString().Length - 1);
                tw.WriteLine("\"7\": \"" + zoomratiox.ToString() + "\",");
            }
            if (zoomratioy.ToString().Length > 0)
            {
                zoomratioy = zoomratioy.ToString().Substring(0, zoomratioy.ToString().Length - 1);
                tw.WriteLine("\"8\": \"" + zoomratioy.ToString() + "\",");
            }
            string keystring2 = "0";
            string keystring3 = "0";
            string keystring4 = "0";
            tw.WriteLine("\"9\": \"" + strKey + "\",");
            tw.WriteLine("\"10\": \"" + thumb.ToString() + "\",");
            tw.WriteLine("\"11\": \"" + keystring2.ToString() + "\",");
            tw.WriteLine("\"12\": \"" + keystring3.ToString() + "\",");
            tw.WriteLine("\"13\": \"" + keystring4.ToString() + "\",");
            for (int i = 0; i < pagestring.Count; i++)
            {
                if (pagestring[i].ToString().Length > 0)
                {
                    pagestring[i] = pagestring[i].ToString().Substring(0, pagestring[i].ToString().Length - 1);
                    tw.WriteLine("\"" + (i + 14).ToString() + "\": \"" + pagestring[i].ToString() + "\",");
                }
            }
            int k = pagestring.Count + 14;
            for (int j = 0; j < richTextPageString.Count; j++)
            {
                if (richTextPageString.Count == (j + 1))
                    tw.WriteLine("\"" + (j + k).ToString() + "\": \"" + richTextPageString[j].ToString() + "\"");
                else
                    tw.WriteLine("\"" + (j + k).ToString() + "\": \"" + richTextPageString[j].ToString() + "\",");
            }
            tw.WriteLine("}");
            tw.Close();
            tw.Dispose();
            button5.Text = "Exit";
        }

        /// <summary>
        /// Background worker progress changed handler - updates UI progress bar
        /// Provides visual feedback to user during lengthy processing operations
        /// </summary>
        /// <param name="sender">The background worker reporting progress</param>
        /// <param name="e">Event arguments containing progress percentage (0-100)</param>
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
        
        #endregion

        #region UI Event Handlers
        
        /// <summary>
        /// Handles the Help button click event
        /// Opens the default web browser to the help/support URL
        /// URL can be customized via license file configuration
        /// </summary>
        /// <param name="sender">The button that triggered this event</param>
        /// <param name="e">Event arguments containing button click details</param>
        private void button6_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo(strHelp);
            Process.Start(sInfo);
            // Alternative: Show built-in help dialog
            //Help helpForm = new Help();
            //helpForm.ShowDialog();
        }

        /// <summary>
        /// Handles zoom level selection changes
        /// Updates the number of zoom levels to generate during processing
        /// </summary>
        /// <param name="sender">The combo box that triggered this event</param>
        /// <param name="e">Event arguments containing selection details</param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            comboBox1.SelectedIndex = comboBox.SelectedIndex;
        }
        
        /// <summary>
        /// Handles PDF box type selection changes
        /// Determines which PDF bounding box to use for page extraction
        /// MediaBox = full page, CropBox = visible area, TrimBox = final trimmed area
        /// </summary>
        /// <param name="sender">The combo box that triggered this event</param>
        /// <param name="e">Event arguments containing selection details</param>
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            comboBox2.SelectedIndex = comboBox.SelectedIndex;
        }

        /// <summary>
        /// Handles rich text annotation changes
        /// Automatically saves annotation text for the currently selected file
        /// Maintains association between files and their descriptions
        /// </summary>
        /// <param name="sender">The rich text box that triggered this event</param>
        /// <param name="e">Event arguments containing text change details</param>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            // Only save if a file is selected
            if (listBox1.SelectedIndex != -1)
                richTextString[listBox1.SelectedIndex] = richTextBox1.Text;
        }
        
        #endregion
    }
}
