using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DimSplitter
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Help_Load(object sender, EventArgs e)
        {
            string gs = "";
            Guid g;
            g = Guid.NewGuid();
            for (int i = 0; i < 1000; i++)
            {
                gs = gs + Guid.NewGuid().ToString();
                gs = gs + "|\"+\n";
            }
            richTextBox1.Text = gs.ToUpper();
        }
    }
}
