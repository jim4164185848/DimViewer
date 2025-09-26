using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GhostscriptSharp;
using System.Drawing.Imaging;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;
using System.Collections;

namespace DimSplitter
{
    /// <summary>
    /// Core class responsible for processing images and PDFs into tiled formats.
    /// This class handles the conversion of large images into smaller tiles for web viewing,
    /// supports multiple zoom levels, and includes PDF processing capabilities.
    /// </summary>
    public class CreateTiles
    {
        #region Private Fields
        
        /// <summary>List of file paths to be processed</summary>
        List<string> l_iWorkToDo;
        
        /// <summary>Path to the Ghostscript DLL for PDF processing</summary>
        string apppathfile = "";
        
        /// <summary>Counter for completed work items</summary>
        int i_iWorkDone = 0;
        
        /// <summary>Total number of work items to process</summary>
        int i_iWorkToDo = 0;
        
        /// <summary>Type reference for PDF processing DLL forms</summary>
        System.Type MyDLLFormType = null;
        
        /// <summary>Type reference for PDF page processing</summary>
        System.Type MyDLLFormPage = null;
        
        /// <summary>Flag indicating whether PDF API is successfully loaded</summary>
        bool loadPDFApi;
        
        /// <summary>Output directory path for processed tiles</summary>
        string folder = "";
        
        /// <summary>Prefix name for generated tile files</summary>
        string prefixname = "";
        
        /// <summary>Initial width setting for image processing</summary>
        string iniwidth = "";
        
        /// <summary>Initial height setting for image processing</summary>
        string iniheight = "";
        
        /// <summary>Thumbnail width setting</summary>
        string thumbwidth = "";
        
        /// <summary>Thumbnail height setting</summary>
        string thumbheight = "";
        
        /// <summary>Rich text strings associated with each image</summary>
        ArrayList irichTextString = new ArrayList();
        
        /// <summary>Maximum resolution for image processing (DPI)</summary>
        int maxresolution;
        
        /// <summary>Total number of pages processed</summary>
        int totalPage;
        
        /// <summary>Current zoom level being processed (1-5)</summary>
        int zoom;
        
        /// <summary>PDF bounding box type (0=MediaBox, 1=CropBox, 2=TrimBox)</summary>
        int pdfbox;
        
        /// <summary>Flag for using initial PDF settings</summary>
        int useinipdf;
        
        /// <summary>Flag for generating thumbnails</summary>
        int thumb;
        
        /// <summary>Total number of work items (decimal for progress calculation)</summary>
        decimal totalworknumber = 0;
        
        /// <summary>Current work item number (decimal for progress calculation)</summary>
        decimal currentworknumber = 0;

        #endregion

        #region Events and Progress Handling

        /// <summary>
        /// Event raised when processing progress changes
        /// </summary>
        public event ProgressChangedEventHandler ProgressChanged;

        /// <summary>
        /// Raises the ProgressChanged event with the specified progress percentage
        /// </summary>
        /// <param name="progress">Progress percentage (0-100)</param>
        protected virtual void OnProgressChanged(int progress)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, new ProgressChangedEventArgs(progress, null));
            }
        }

        /// <summary>
        /// Computes and reports progress for the current operation
        /// </summary>
        /// <param name="input">Input progress value</param>
        /// <returns>Computed progress value</returns>
        public int ComputeProgress(int input)
        {
            // Calculate and report progress to UI
            OnProgressChanged(input);
            return 30; // Default return value
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the CreateTiles class with specified processing parameters
        /// </summary>
        /// <param name="iWorkToDo">List of file paths to process</param>
        /// <param name="strfolder">Output folder path</param>
        /// <param name="strprefixname">Prefix for generated file names</param>
        /// <param name="imaxresolution">Maximum resolution in DPI</param>
        /// <param name="izoom">Number of zoom levels to generate</param>
        /// <param name="striniwidth">Initial width for image sizing</param>
        /// <param name="striniheight">Initial height for image sizing</param>
        /// <param name="intpdfbox">PDF bounding box type (0=MediaBox, 1=CropBox, 2=TrimBox)</param>
        /// <param name="intuseinipdf">Use initial PDF settings flag</param>
        /// <param name="intthumb">Generate thumbnails flag</param>
        /// <param name="strthumbwidth">Thumbnail width</param>
        /// <param name="strthumbheight">Thumbnail height</param>
        /// <param name="richTextString">Rich text annotations for images</param>
        public CreateTiles(List<string> iWorkToDo, string strfolder, string strprefixname, int imaxresolution, int izoom, string striniwidth, string striniheight, int intpdfbox, int intuseinipdf, int intthumb, string strthumbwidth, string strthumbheight, ArrayList richTextString)
        {
            // Initialize file processing list
            l_iWorkToDo = iWorkToDo;
            i_iWorkToDo = l_iWorkToDo.Count;
            totalworknumber = l_iWorkToDo.Count;
            
            // Set up Ghostscript DLL path for PDF processing
            apppathfile = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\gsdll32.dll";
            loadPDFApi = false;
            
            // Initialize processing parameters
            folder = strfolder;
            prefixname = strprefixname;
            maxresolution = imaxresolution;
            totalPage = 0;
            zoom = izoom;
            iniwidth = striniwidth;
            iniheight = striniheight;
            pdfbox = intpdfbox;
            useinipdf = intuseinipdf;
            thumb = intthumb;
            thumbwidth = strthumbwidth;
            thumbheight = strthumbheight;
            irichTextString = richTextString;
        }

        #endregion

        #region Image Tiling Methods

        /// <summary>
        /// Creates tiles from a source image by splitting it into smaller rectangular pieces.
        /// Supports watermarking for trial versions and handles cleanup of temporary files.
        /// </summary>
        /// <param name="image">Path to the source image file</param>
        public void CreateTile(string image)
        {
            Bitmap src = new Bitmap(image);
            string totile = "";
            
            // Initialize position and size variables for tile processing
            int imagex = 0;          // Remaining width to process
            int imagey = 0;          // Remaining height to process  
            int imageposx = 0;       // Current X position in source image
            int imageposy = 0;       // Current Y position in source image
            int tilex = 0;           // Width of current tile
            int tiley = 0;           // Height of current tile
            int tilexindex = 0;      // X index of current tile
            int tileyindex = 0;      // Y index of current tile
            
            try
            {
                // Set up watermark font and brush for trial version
                Font font = new Font("Myriad Pro Light", 16, FontStyle.Bold, GraphicsUnit.Pixel);
                Color color = Color.FromArgb(100, 0, 0, 0); // Semi-transparent black
                Point pt = new Point(10, 30);
                SolidBrush sbrush = new SolidBrush(color);
                
                // Process image in horizontal strips
                imagex = src.Width;
                while (imagex > 0)
                {
                    // Determine width of current tile (max TILEX pixels)
                    if (imagex > DimSplitter.TILEX)
                        tilex = DimSplitter.TILEX;
                    else
                        tilex = imagex;
                    
                    // Process current horizontal strip in vertical segments
                    imagey = src.Height;
                    imageposy = 0;
                    tileyindex = 0;
                    while (imagey > 0)
                    {
                        // Determine height of current tile (max TILEY pixels)
                        if (imagey > DimSplitter.TILEY)
                            tiley = DimSplitter.TILEY;
                        else
                            tiley = imagey;
                            
                        // Create new tile bitmap and draw portion of source image
                        Bitmap tile = new Bitmap(tilex, tiley);
                        using (Graphics g = Graphics.FromImage(tile))
                        {
                            // Copy rectangular portion from source to tile
                            g.DrawImage(src, new Rectangle(0, 0, tilex, tiley),
                                new Rectangle(imageposx, imageposy,
                                         tilex, tiley), GraphicsUnit.Pixel);
                            
                            // Add watermark if this is a trial version
                            if (DimSplitter.trial == 1)
                                g.DrawString(DimSplitter.watermarkText, font, sbrush, pt);
                        }
                        
                        // Generate filename with tile coordinates and save
                        totile = image.ToLower().Replace(".png", "_" + tilexindex + "_" + tileyindex + ".png");
                        tile.Save(totile);
                        
                        // Move to next vertical tile
                        tileyindex++;
                        imageposy = imageposy + tiley;
                        imagey = imagey - tiley;
                    }
                    
                    // Move to next horizontal tile
                    tilexindex++;
                    imageposx = imageposx + tilex;
                    imagex = imagex - tilex;
                }
            }
            catch (Exception)
            {
                // Silent exception handling - errors are ignored during tile creation
            }
            finally
            {
                // Clean up resources
                src.Dispose();
                
                // Delete the source image file after tiling
                if (System.IO.File.Exists(image))
                    System.IO.File.Delete(image);
            }
        }

        #endregion

        #region Image Resizing and Zoom Level Methods

        /// <summary>
        /// Resizes an image and creates multiple zoom levels for web viewing.
        /// Handles aspect ratio preservation and creates appropriately sized thumbnails.
        /// </summary>
        /// <param name="strImageName">Path to the source image file</param>
        /// <param name="obj">Reference to the main DimSplitter object for progress updates</param>
        /// <returns>True if processing should continue, false if cancelled</returns>
        public bool ResizeImage(string strImageName, DimSplitter obj)
        {
            // Variables for zoom level processing
            int zoomx = 0;              // Current zoom level index
            int zoomtox = 0;            // Target width for current zoom level
            int zoomtoy = 0;            // Target height for current zoom level
            decimal ratiox = 0;         // X scaling ratio
            decimal ratioy = 0;         // Y scaling ratio
            decimal ratiomx = 0;        // Maximum X ratio
            decimal ratiomy = 0;        // Maximum Y ratio
            string strImageNameZ;
            string replacestr = "";     // String pattern to replace in filename
            string toreplacestr = "";   // Replacement string for filename
            
            // Load source image
            Image image = Image.FromFile(strImageName);
            
            // Parse filename components for naming scheme
            replacestr = strImageName.Substring(strImageName.Length - 10);
            toreplacestr = "_" + Convert.ToString(Convert.ToInt16(strImageName.Substring(strImageName.Length - 10, 3)) + Convert.ToInt16(strImageName.Substring(strImageName.Length - 7, 3)));
            
            // Update progress
            currentworknumber++;
            ComputeProgress((Convert.ToInt16((currentworknumber/totalworknumber)*100)>100)?100:(Convert.ToInt16((currentworknumber/totalworknumber)*100)));
            
            // Check for cancellation request
            if ((bool)((System.ComponentModel.BackgroundWorker)obj.backgroundWorker1).CancellationPending == true)
                return false;
                
            try
            {
                // Initialize zoom ratio tracking
                DimSplitter.zoomratiox = "";
                DimSplitter.zoomratioy = "";
                
                // Process based on zoom level
                if (zoom == 1)
                {
                    // Zoom level 1: Create thumbnail version
                    strImageNameZ = strImageName.ToLower().Replace(replacestr, "_1" + toreplacestr + ".png");
                    
                    // Calculate thumbnail dimensions while preserving aspect ratio
                    if (image.Width > Convert.ToInt16(iniwidth))
                    {
                        zoomtox = Convert.ToInt16(iniwidth);
                    }
                    else
                        zoomtox = image.Width;
                    if (image.Height > Convert.ToInt16(iniheight))
                    {
                        zoomtoy = Convert.ToInt16(iniheight);
                    }
                    else
                        zoomtoy = image.Height;
                    Image thumbnail = image.GetThumbnailImage(zoomtox, zoomtoy, null, IntPtr.Zero);
                    DimSplitter.pagestring[zoom - 1] = DimSplitter.pagestring[zoom - 1] + thumbnail.Width.ToString() + "," + thumbnail.Height.ToString() + ";";
                    DimSplitter.zoomratiox = DimSplitter.zoomratiox + "1;";
                    DimSplitter.zoomratioy = DimSplitter.zoomratioy + "1;";
                    thumbnail.Save(strImageNameZ);
                    CreateTile(strImageNameZ);
                }
                else if (zoom == 2)
                {
                    strImageNameZ = strImageName.ToLower().Replace(replacestr, "_2" + toreplacestr + ".png");
                    DimSplitter.pagestring[zoom - 1] = DimSplitter.pagestring[zoom - 1] + image.Width.ToString() + "," + image.Height.ToString() + ";";
                    image.Save(strImageNameZ);
                    CreateTile(strImageNameZ);
                    if (image.Width > Convert.ToInt16(iniwidth))
                    {
                        zoomtox = Convert.ToInt16(iniwidth);
                    }
                    else
                        zoomtox = image.Width;
                    ratiox = image.Width / (decimal)zoomtox;
                    if (image.Height > Convert.ToInt16(iniheight))
                    {
                        zoomtoy = Convert.ToInt16(iniheight);
                    }
                    else
                        zoomtoy = image.Height;
                    ratioy = image.Height/(decimal)zoomtoy;
                    strImageNameZ = strImageName.ToLower().Replace(replacestr, "_1" + toreplacestr + ".png");
                    Image thumbnail = image.GetThumbnailImage(zoomtox, zoomtoy, null, IntPtr.Zero);
                    DimSplitter.zoomratiox = DimSplitter.zoomratiox + "1;";
                    DimSplitter.zoomratiox = DimSplitter.zoomratiox + String.Format("{0:0.000000}", ratiox)+";";
                    DimSplitter.zoomratioy = DimSplitter.zoomratioy + "1;";
                    DimSplitter.zoomratioy = DimSplitter.zoomratioy + String.Format("{0:0.000000}", ratioy) + ";";
                    DimSplitter.pagestring[0] = DimSplitter.pagestring[0] + thumbnail.Width.ToString() + "," + thumbnail.Height.ToString() + ";";
                    thumbnail.Save(strImageNameZ);
                    CreateTile(strImageNameZ);
                }
                else
                {
                    strImageNameZ = strImageName.ToLower().Replace(replacestr, "_" + Convert.ToString(zoom) + toreplacestr + ".png");
                    DimSplitter.pagestring[zoom - 1] = DimSplitter.pagestring[zoom - 1] + image.Width.ToString() + "," + image.Height.ToString() + ";";
                    image.Save(strImageNameZ);
                    CreateTile(strImageNameZ);
                    if (image.Width > Convert.ToInt16(iniwidth))
                    {
                        zoomtox = Convert.ToInt16(iniwidth);
                    }
                    else
                        zoomtox = image.Width;
                    ratiox = image.Width / (decimal)zoomtox;
                    if (image.Height > Convert.ToInt16(iniheight))
                    {
                        zoomtoy = Convert.ToInt16(iniheight);
                    }
                    else
                        zoomtoy = image.Height;
                    ratioy = image.Height / (decimal)zoomtoy;
                    ratiomx = ratiox;
                    ratiomy = ratioy;
                    strImageNameZ = strImageName.ToLower().Replace(replacestr, "_1" + toreplacestr + ".png");
                    Image thumbnail = image.GetThumbnailImage(zoomtox, zoomtoy, null, IntPtr.Zero);
                    DimSplitter.pagestring[0] = DimSplitter.pagestring[0] + thumbnail.Width.ToString() + "," + thumbnail.Height.ToString() + ";";
                    thumbnail.Save(strImageNameZ);
                    CreateTile(strImageNameZ);
                    DimSplitter.zoomratiox = DimSplitter.zoomratiox + "1;";
                    DimSplitter.zoomratioy = DimSplitter.zoomratioy + "1;";
                    for (int j = (zoom - 3); j >= 0; j--)
                    {
                        if (image.Width > Convert.ToInt16(iniwidth))
                        {
                            zoomx = (image.Width - Convert.ToInt16(iniwidth)) / (zoom - 1);
                            zoomtox = image.Width - zoomx * (j + 1);
                            ratiox = zoomtox / (decimal)Convert.ToInt16(iniwidth);
                        }
                        else
                        {
                            zoomtox = image.Width;
                            ratiox = zoomtox / (decimal)zoomtox;
                        }
                        if (image.Height > Convert.ToInt16(iniheight))
                        {
                            zoomx = (image.Height - Convert.ToInt16(iniheight)) / (zoom - 1);
                            zoomtoy = image.Height - zoomx * (j + 1);
                            ratioy = zoomtoy / (decimal)Convert.ToInt16(iniheight);
                        }
                        else
                        {
                            zoomtoy = image.Height;
                            ratioy = zoomtoy / (decimal)zoomtoy;
                        }
                        strImageNameZ = strImageName.ToLower().Replace(replacestr, "_" + Convert.ToString(zoom - j - 1) + toreplacestr + ".png");
                        Image thumbnailz = image.GetThumbnailImage(zoomtox, zoomtoy, null, IntPtr.Zero);
                        DimSplitter.pagestring[zoom - j - 2] = DimSplitter.pagestring[zoom - j - 2] + thumbnailz.Width.ToString() + "," + thumbnailz.Height.ToString() + ";";
                        thumbnailz.Save(strImageNameZ);
                        CreateTile(strImageNameZ);
                        DimSplitter.zoomratiox = DimSplitter.zoomratiox + String.Format("{0:0.000000}", ratiox) + ";";
                        DimSplitter.zoomratioy = DimSplitter.zoomratioy + String.Format("{0:0.000000}", ratioy) + ";";
                    }
                    DimSplitter.zoomratiox = DimSplitter.zoomratiox + String.Format("{0:0.000000}", ratiomx) + ";";
                    DimSplitter.zoomratioy = DimSplitter.zoomratioy + String.Format("{0:0.000000}", ratiomy) + ";";
                }
                if (thumb == 1)
                {
                    strImageNameZ = strImageName.ToLower().Replace(replacestr, "_thumb" + toreplacestr + ".png");
                    Image thumbnail = image.GetThumbnailImage(Convert.ToInt16(thumbwidth), Convert.ToInt16(thumbheight), null, IntPtr.Zero);
                    DimSplitter.pagestring[DimSplitter.pagestring.Count-1] = DimSplitter.pagestring[DimSplitter.pagestring.Count-1] + thumbnail.Width.ToString() + "," + thumbnail.Height.ToString() + ";";
                    thumbnail.Save(strImageNameZ);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                image.Dispose();
                if (System.IO.File.Exists(strImageName))
                    System.IO.File.Delete(strImageName);
            }
            return true;
        }

        /// <summary>
        /// Main processing method that handles both PDF and image files.
        /// For PDFs: Extracts pages using Ghostscript and processes each page as an image.
        /// For Images: Converts to PNG format and processes for tiling.
        /// </summary>
        /// <param name="obj">Reference to the main DimSplitter object for progress updates</param>
        /// <returns>Total number of pages processed (negative if cancelled)</returns>
        public int DoWork(DimSplitter obj)
        {
            object objPdfReader;
            bool ret = true;
            
            // Process next file in queue
            if (i_iWorkDone < i_iWorkToDo)
            {
                // Handle PDF files
                if (l_iWorkToDo[i_iWorkDone].ToString().ToLower().IndexOf(".pdf") != -1)
                {
                    // Initialize PDF processing APIs if not already loaded
                    if (!loadPDFApi)
                    {
                        // Extract Ghostscript DLL from resources
                        ResourceExtractor.ExtractResourceToFile("DimViewer.Resources.gsdll32.dll", apppathfile);
                        
                        // Load iTextSharp library for PDF manipulation
                        System.Reflection.Assembly myDllAssembly = ResourceExtractor.ExtractResourceToAssembly("DimViewer.Resources.itextsharp.dll");
                        MyDLLFormType = myDllAssembly.GetType("iTextSharp.text.pdf.PdfReader");
                        MyDLLFormPage = myDllAssembly.GetType("iTextSharp.text.Rectangle");
                        loadPDFApi = true;
                    }
                    
                    // Open PDF and get page information
                    objPdfReader = Activator.CreateInstance(MyDLLFormType, new object[] { l_iWorkToDo[i_iWorkDone].ToString() });
                    int pdfpagecount = Convert.ToInt16(MyDLLFormType.GetProperty("NumberOfPages").GetValue(objPdfReader, null).ToString());
                    
                    // Get page dimensions from first page
                    MethodInfo Method = MyDLLFormType.GetMethod("GetPageSize", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(int) }, null);
                    object[] mParam = new object[] { 1 };
                    object insMyDLLFormPage = Method.Invoke(objPdfReader, mParam);
                    Int16 pdfwidth = Convert.ToInt16(MyDLLFormPage.GetProperty("Width").GetValue(insMyDLLFormPage, null));
                    Int16 pdfheight = Convert.ToInt16(MyDLLFormPage.GetProperty("Height").GetValue(insMyDLLFormPage, null));
                    
                    // Use PDF dimensions if not overridden
                    if (useinipdf == 0)
                    {
                        iniwidth = pdfwidth.ToString();
                        iniheight = pdfheight.ToString();
                    }
                    
                    // Convert PDF pages to PNG images using Ghostscript
                    GhostscriptWrapper.GeneratePageThumbs(l_iWorkToDo[i_iWorkDone].ToString(), folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + "%03d.png", 1, pdfpagecount, maxresolution, maxresolution, pdfbox);
                    totalworknumber = totalworknumber + pdfpagecount - 1;
                    
                    // Process each extracted PDF page
                    for (int i = 0; i < pdfpagecount; i++)
                    {
                        // Respect page limit
                        if ((totalPage + i) > DimSplitter.PAGE_MAX)
                        {
                            break;
                        }
                        
                        // Process page image and check for cancellation
                        ret = ResizeImage(folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + String.Format("{0:000}", i + 1) + ".png", obj);
                        if (ret == false)
                            break;
                    }
                    
                    totalPage = totalPage + pdfpagecount;
                    
                    // Associate rich text with each PDF page
                    for (int i = 0; i < pdfpagecount; i++)
                    {
                        DimSplitter.richTextPageString.Insert(DimSplitter.richTextPageString.Count, irichTextString[i_iWorkDone]);
                    }
                    
                    // Mark as cancelled if processing stopped early
                    if (ret == false)
                        totalPage = totalPage * -1;
                }
                else
                {
                    // Handle regular image files
                    try
                    {
                        totalPage++;
                        
                        // Load image and save as PNG for consistent processing
                        Image image = Image.FromFile(l_iWorkToDo[i_iWorkDone].ToString());
                        image.Save(folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + "000.png", ImageFormat.Png);
                        
                        // Process the image for tiling
                        ret = ResizeImage(folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + "000.png", obj);
                        
                        // Associate rich text with the image
                        DimSplitter.richTextPageString.Insert(DimSplitter.richTextPageString.Count, irichTextString[i_iWorkDone]);
                        
                        // Mark as cancelled if processing stopped
                        if (ret == false)
                            totalPage = totalPage * -1;
                    }
                    catch (Exception ex)
                    {
                        i_iWorkDone++;
                        throw ex;
                    }
                }
                i_iWorkDone++;
            }
            return totalPage;
        }

        /// <summary>
        /// Calculates the percentage of work completed
        /// </summary>
        /// <returns>Completion percentage (0-100)</returns>
        public int PercentComplete()
        {
            int ret = (int)Math.Round(((double)i_iWorkDone / (double)i_iWorkToDo) * 100) + 1;
            if (ret > 100)
                ret = 100;
            return ret;
        }

        #endregion
    }
}
