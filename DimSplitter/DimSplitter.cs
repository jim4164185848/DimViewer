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
    public partial class DimSplitter : Form
    {
        public const int PAGE_MAX = 300;
        public const int MAX_RESO = 300;
        public const int MIN_INI = 10;
        public const int D_MAX_RESO = 180;
        public const int TILEX = 260;
        public const int TILEY = 260;
        public const int INI_HIGHT = 600;
        public const int INI_WEIGHT = 800;
        public const int THUMB_HIGHT = 100;
        public const int THUMB_WEIGHT = 100;
        public const string dimversion = "2.1";
        public const string watermarkText = "dimviewer " + dimversion + " trial version";
        public string lastfiletype = "";
        public string filterstring = "";
        public string apppathfile = "";
        public static ArrayList pagestring = new ArrayList();
        public static ArrayList richTextString = new ArrayList();
        public static ArrayList richTextPageString = new ArrayList();
        public static string zoomratiox;
        public static string zoomratioy;
        public int totalPage = 0;
        public int thumb = 0;
        public static int trial = 1;
        public static string strHelp = "http://www.dimviewer.com/support.aspx";
        public static string strKey = "00000000-0000-0000-0000-000000000000";
        public string starttime;
        public TextWriter tw;
        public DimSplitter()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open Image Files";
            fDialog.Filter = filterstring;
            fDialog.Multiselect = true;
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < fDialog.FileNames.Length; i++)
                {
                      if (listBox1.SelectedIndex != -1)
                      {
                          int isel = listBox1.SelectedIndex;
                          listBox1.Items.Insert(isel,fDialog.FileNames[i].ToString());
                          richTextString.Insert(isel, "");
                          if (listBox1.SelectedIndex != -1)
                            richTextBox1.Text = richTextString[listBox1.SelectedIndex].ToString();
                      }
                      else
                      {
                          listBox1.Items.Add(fDialog.FileNames[i].ToString());
                          richTextString.Add("");
                          richTextBox1.Text = "";
                      }
                      lastfiletype = fDialog.FileNames[i].ToString().Substring(fDialog.FileNames[i].ToString().LastIndexOf("."), fDialog.FileNames[i].ToString().Length - fDialog.FileNames[i].ToString().LastIndexOf("."));
                }
                if (lastfiletype == ".pdf")
                    filterstring = "PDF|*.pdf|Image Files|*.jpg;*.gif;*.bmp;*.png;*.jpeg";
                else
                    filterstring = "Image Files|*.jpg;*.gif;*.bmp;*.png;*.jpeg|PDF|*.pdf";
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                int isel = listBox1.SelectedIndex;
                listBox1.Items.RemoveAt(isel);
                richTextString.RemoveAt(isel);
                richTextBox1.Text = "";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            richTextString.Clear();
            richTextBox1.Text = "";
        }

        private void moveup_Click(object sender, EventArgs e)
        {
            if ((listBox1.SelectedIndex != -1) && (listBox1.SelectedIndex != 0))
            {
                int index = listBox1.SelectedIndex;
                listBox1.Items.Insert(index - 1, listBox1.SelectedItem);
                richTextString.Insert(index - 1, richTextString[index]);
                listBox1.Items.RemoveAt(index + 1);
                richTextString.RemoveAt(index + 1);
                listBox1.SelectedIndex = index - 1;
            }
        }

        private void movedown_Click(object sender, EventArgs e)
        {
            if ((listBox1.SelectedIndex != -1) && (listBox1.SelectedIndex != listBox1.Items.Count-1))
            {
                int index = listBox1.SelectedIndex;
                listBox1.Items.Insert(index + 2, listBox1.SelectedItem);
                richTextString.Insert(index + 2, richTextString[index]);
                listBox1.Items.RemoveAt(index);
                richTextString.RemoveAt(index);
                listBox1.SelectedIndex = index + 1;
            }
        }

        private void DimSplitter_Load(object sender, EventArgs e)
        {
            apppathfile = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) +"\\gsdll32.dll";
            if (System.IO.File.Exists(apppathfile))
                System.IO.File.Delete(apppathfile);
            textBox1.Text = @"C:\DimImages";
            textBox2.Text = "ImagePrefix";
            textBox3.Text = D_MAX_RESO.ToString();
            textBox5.Text = INI_WEIGHT.ToString();
            textBox4.Text = INI_HIGHT.ToString();
            textBox6.Text = THUMB_WEIGHT.ToString();
            textBox7.Text = THUMB_HIGHT.ToString();
            textBox1.ReadOnly = true;
            listBox1.Select();
            progressBar1.Visible = false;
            filterstring = "PDF/Image Files|*.jpg;*.gif;*.bmp;*.png;*.jpeg;*.pdf";
            comboBox1.Items.Add("1");
            comboBox1.Items.Add("2");
            comboBox1.Items.Add("3");
            comboBox1.Items.Add("4");
            comboBox1.Items.Add("5");
            comboBox1.SelectedIndex = 2;
            comboBox2.Items.Add("MediaBox");
            comboBox2.Items.Add("CropBox");
            comboBox2.Items.Add("TrimBox");
            comboBox2.SelectedIndex = 0;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            checkBox2.Checked = true;
            label8.Visible = false;

            try
            {
                string licensefile = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\dimviewer.lic";
                if (File.Exists(licensefile))
                {
                    XmlDocument document = new XmlDocument();
                    StreamReader license = null;
                    license = new StreamReader(licensefile);
                    document.LoadXml(license.ReadToEnd());
                    XmlNode licensenode = document.SelectSingleNode("//Data/Key/text()");
                    if (key.keyList.Contains(licensenode.Value))
                    {
                        strKey = licensenode.Value;
                        trial = 0;
                    }
                    try
                    {
                        XmlNode helpnode = document.SelectSingleNode("//Data/Help/text()");
                        strHelp = helpnode.Value;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch(Exception)
            {
            }
            if (trial == 1)
                this.Text = this.Text + " Trial";
        }

        private void output_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fDialog = new FolderBrowserDialog();
            fDialog.RootFolder = Environment.SpecialFolder.Desktop;
            fDialog.SelectedPath = textBox1.Text.ToString();
            fDialog.Description = "Select the source directory";
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fDialog.SelectedPath.ToString();
            }
        }
        public void disablebutton()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            output.Enabled = false;
            moveup.Enabled = false;
            movedown.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            textBox4.Enabled = false;
            textBox5.Enabled = false;
            textBox6.Enabled = false;
            textBox7.Enabled = false;
            comboBox1.Enabled = false;
            comboBox2.Enabled = false;
            listBox1.Enabled = false;
            richTextBox1.Enabled = false;
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            label8.Visible = true;
        }
        public void enablebutton()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            output.Enabled = true;
            moveup.Enabled = true;
            movedown.Enabled = true;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox3.Enabled = true;
            textBox4.Enabled = true;
            textBox5.Enabled = true;
            textBox6.Enabled = true;
            textBox7.Enabled = true;
            comboBox1.Enabled = true;
            comboBox2.Enabled = true;
            listBox1.Enabled = true;
            richTextBox1.Enabled = true;
            checkBox1.Enabled = true;
            checkBox2.Enabled = true;
            label8.Visible = false;
        }
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
        private void button3_Click(object sender, EventArgs e)
        {
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
            folder = textBox1.Text.ToString();
            prefixname = textBox2.Text.ToString();
            iniwidth = textBox5.Text.ToString();
            iniheight = textBox4.Text.ToString();
            starttime = DateTime.Now.ToString();
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

        private void button5_Click(object sender, EventArgs e)
        {
            bool bDone;
            if (backgroundWorker1.IsBusy)
            {
                DialogResult result = MessageBox.Show("Are you sure you want to cancel the process?", "Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                bDone = (result.ToString() == "Yes") ? true : false;
                if (bDone)
                {
                    backgroundWorker1.CancelAsync();
                    button5.Text = "Cancelling...";
                }
            }
            else
                Application.Exit();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.SelectionMode = SelectionMode.One;
            if (listBox1.SelectedIndex != -1)
                richTextBox1.Text = richTextString[listBox1.SelectedIndex].ToString();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
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

            CreateTiles oCreateTiles = new CreateTiles(imagelist, folder, prefixname, maxresolution, zoom, iniwidth, iniheight, pdfbox, useinipdf, thumb, thumbwidth, thumbheight, richTextString);
            oCreateTiles.ProgressChanged += (s, pe) => backgroundWorker1.ReportProgress(pe.ProgressPercentage);
            oCreateTiles.ComputeProgress(0);
            for (int i = 0; i < imagelist.Count; i++)
            {
                if (backgroundWorker1.CancellationPending)//checks for cancel request
                {
                    e.Cancel = true;
                    break;
                }
                //backgroundWorker1.ReportProgress(i / 5);//reports a percentage between 0 and 100
                try
                {
                    totalPage = oCreateTiles.DoWork(this);

                    if (totalPage < 0)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid image exist.\nIt get highlights in the Listbox.\nFile name:" + imagelist[i].ToString(), "Can not process...");
                    totalPage = -1 * i;
                    break;
                }
                if (totalPage >= PAGE_MAX)
                {
                    MessageBox.Show("DimViewer only process maxinum " + PAGE_MAX.ToString() + " pages each time due to performance and capacity concerns.\nPlease contact support@dimviewer.com if you need to process over maximum amount pages.", "Completed and Warning");
                    break;
                }
                //backgroundWorker1.ReportProgress(i / 5);//reports a percentage between 0 and 100
            }
            e.Result = totalPage;
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("You've cancelled the process.", "Cancel");
                enablebutton();
                progressBar1.Visible = false;
            }
            else
            {
                if (Convert.ToInt16(e.Result) < 0)
                {
                    listBox1.SetSelected(Convert.ToInt16(e.Result) * -1, true);
                }
                else
                {
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

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
        private void button6_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo(strHelp);
            Process.Start(sInfo);
            //Help helpForm = new Help();
            //helpForm.ShowDialog();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            comboBox1.SelectedIndex = comboBox.SelectedIndex;
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            comboBox2.SelectedIndex = comboBox.SelectedIndex;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
                richTextString[listBox1.SelectedIndex] = richTextBox1.Text;
        }
    }
}
