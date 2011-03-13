using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace EffiaTruther
{
    public partial class CommandHelp : Form
    {
        public CommandHelp()
        {
            InitializeComponent();
        }
        public CommandHelp(string st)
        {
            richTextBox1.Text = st;
        }
    }
}
