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
    public class CreateTiles
    {
        List<string> l_iWorkToDo;
        string apppathfile = "";
        int i_iWorkDone = 0;
        int i_iWorkToDo = 0;
        System.Type MyDLLFormType = null;
        System.Type MyDLLFormPage = null;
        bool loadPDFApi;
        string folder = "";
        string prefixname = "";
        string iniwidth = "";
        string iniheight = "";
        string thumbwidth = "";
        string thumbheight = "";
        ArrayList irichTextString = new ArrayList();
        int maxresolution;
        int totalPage;
        int zoom;
        int pdfbox;
        int useinipdf;
        int thumb;
        decimal totalworknumber = 0;
        decimal currentworknumber = 0;
        public event ProgressChangedEventHandler ProgressChanged;

        protected virtual void OnProgressChanged(int progress)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, new ProgressChangedEventArgs(progress, null));
            }
        }

        public int ComputeProgress(int input)
        {
            //<Calculate stuff>
            OnProgressChanged(input);
            //...
            return 30;
        }
        public CreateTiles(List<string> iWorkToDo, string strfolder, string strprefixname, int imaxresolution, int izoom, string striniwidth, string striniheight, int intpdfbox, int intuseinipdf, int intthumb, string strthumbwidth, string strthumbheight, ArrayList richTextString)
        {

            l_iWorkToDo = iWorkToDo;
            i_iWorkToDo = l_iWorkToDo.Count;
            totalworknumber = l_iWorkToDo.Count;
            apppathfile = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\gsdll32.dll";
            loadPDFApi = false;
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
        public void CreateTile(string image)
        {
            Bitmap src = new Bitmap(image);
            string totile = "";
            int imagex = 0;
            int imagey = 0;
            int imageposx = 0;
            int imageposy = 0;
            int tilex = 0;
            int tiley = 0;
            int tilexindex = 0;
            int tileyindex = 0;
            try
            {
                Font font = new Font("Myriad Pro Light", 16, FontStyle.Bold, GraphicsUnit.Pixel);
                Color color = Color.FromArgb(100, 0, 0, 0);
                Point pt = new Point(10, 30);
                SolidBrush sbrush = new SolidBrush(color);
                imagex = src.Width;
                while (imagex>0)
                {
                    if (imagex > DimSplitter.TILEX)
                        tilex = DimSplitter.TILEX;
                    else
                        tilex = imagex;
                    imagey = src.Height;
                    imageposy = 0;
                    tileyindex = 0;
                    while (imagey > 0)
                    {
                        if (imagey > DimSplitter.TILEY)
                            tiley = DimSplitter.TILEY;
                        else
                            tiley = imagey;
                        Bitmap tile = new Bitmap(tilex, tiley);
                        using (Graphics g = Graphics.FromImage(tile))
                        {
                            g.DrawImage(src, new Rectangle(0, 0, tilex, tiley),
                                new Rectangle(imageposx, imageposy,
                                         tilex, tiley), GraphicsUnit.Pixel);
                            if (DimSplitter.trial == 1)
                                g.DrawString(DimSplitter.watermarkText, font, sbrush, pt);
                        }
                        totile = image.ToLower().Replace(".png", "_" + tilexindex + "_" + tileyindex + ".png");
                        tile.Save(totile);
                        tileyindex++;
                        imageposy = imageposy + tiley;
                        imagey = imagey - tiley;
                    }
                    tilexindex++;
                    imageposx = imageposx + tilex;
                    imagex = imagex - tilex;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                src.Dispose();
                if (System.IO.File.Exists(image))
                    System.IO.File.Delete(image);
            }
        }
        public bool ResizeImage(string strImageName, DimSplitter obj)
        {
            int zoomx = 0;
            int zoomtox = 0;
            int zoomtoy = 0;
            decimal ratiox = 0;
            decimal ratioy = 0;
            decimal ratiomx = 0;
            decimal ratiomy = 0;
            string strImageNameZ;
            string replacestr = "";
            string toreplacestr = "";
            Image image = Image.FromFile(strImageName);
            replacestr = strImageName.Substring(strImageName.Length - 10);
            toreplacestr = "_" + Convert.ToString(Convert.ToInt16(strImageName.Substring(strImageName.Length - 10, 3)) + Convert.ToInt16(strImageName.Substring(strImageName.Length - 7, 3)));
            currentworknumber++;
            ComputeProgress((Convert.ToInt16((currentworknumber/totalworknumber)*100)>100)?100:(Convert.ToInt16((currentworknumber/totalworknumber)*100)));
            if ((bool)((System.ComponentModel.BackgroundWorker)obj.backgroundWorker1).CancellationPending == true)
                return false;
            try
            {
                DimSplitter.zoomratiox = "";
                DimSplitter.zoomratioy = "";
                if (zoom == 1)
                {
                    strImageNameZ = strImageName.ToLower().Replace(replacestr, "_1" + toreplacestr + ".png");
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
        public int DoWork(DimSplitter obj)
        {
            object objPdfReader;
            bool ret = true;
            if (i_iWorkDone < i_iWorkToDo)
            {
                if (l_iWorkToDo[i_iWorkDone].ToString().ToLower().IndexOf(".pdf") != -1)
                {
                    if (!loadPDFApi)
                    {
                        ResourceExtractor.ExtractResourceToFile("DimViewer.Resources.gsdll32.dll", apppathfile);
                        System.Reflection.Assembly myDllAssembly = ResourceExtractor.ExtractResourceToAssembly("DimViewer.Resources.itextsharp.dll");
                        MyDLLFormType = myDllAssembly.GetType("iTextSharp.text.pdf.PdfReader");
                        MyDLLFormPage = myDllAssembly.GetType("iTextSharp.text.Rectangle");
                        loadPDFApi = true;
                    }
                    objPdfReader = Activator.CreateInstance(MyDLLFormType, new object[] { l_iWorkToDo[i_iWorkDone].ToString() });
                    int pdfpagecount = Convert.ToInt16(MyDLLFormType.GetProperty("NumberOfPages").GetValue(objPdfReader, null).ToString());
                    MethodInfo Method = MyDLLFormType.GetMethod("GetPageSize", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(int) }, null);
                    object[] mParam = new object[] { 1 };
                    object insMyDLLFormPage = Method.Invoke(objPdfReader, mParam);
                    Int16 pdfwidth = Convert.ToInt16(MyDLLFormPage.GetProperty("Width").GetValue(insMyDLLFormPage, null));
                    Int16 pdfheight = Convert.ToInt16(MyDLLFormPage.GetProperty("Height").GetValue(insMyDLLFormPage, null));
                    if (useinipdf == 0)
                    {
                        iniwidth = pdfwidth.ToString();
                        iniheight = pdfheight.ToString();
                    }
                    GhostscriptWrapper.GeneratePageThumbs(l_iWorkToDo[i_iWorkDone].ToString(), folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + "%03d.png", 1, pdfpagecount, maxresolution, maxresolution, pdfbox);
                    totalworknumber = totalworknumber + pdfpagecount - 1;
                    for (int i = 0; i < pdfpagecount; i++)
                    {
                        if ((totalPage + i) > DimSplitter.PAGE_MAX)
                        {
                            break;
                        }
                        ret = ResizeImage(folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + String.Format("{0:000}", i + 1) + ".png",obj);
                        if (ret == false)
                            break;
                    }
                    totalPage = totalPage + pdfpagecount;
                    for(int i = 0;i<pdfpagecount;i++)
                    {
                         DimSplitter.richTextPageString.Insert(DimSplitter.richTextPageString.Count, irichTextString[i_iWorkDone]);
                    }
                    if (ret == false)
                        totalPage = totalPage * -1;
                }
                else
                {
                    try
                    {
                        totalPage++;
                        Image image = Image.FromFile(l_iWorkToDo[i_iWorkDone].ToString());
                        image.Save(folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + "000.png", ImageFormat.Png);
                        ret = ResizeImage(folder + "\\" + prefixname + String.Format("{0:000}", totalPage) + "000.png",obj);
                        DimSplitter.richTextPageString.Insert(DimSplitter.richTextPageString.Count, irichTextString[i_iWorkDone]);
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
        public int PercentComplete()
        {
            int ret = (int)Math.Round(((double)i_iWorkDone / (double)i_iWorkToDo) * 100) + 1;
            if (ret > 100)
                ret = 100;
            return ret;

        }

    }
}
