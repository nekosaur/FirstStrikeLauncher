using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FirstStrikeLauncher
{
    public partial class UpdateDialog : Form
    {
        public UpdateDialog(List<string> changes)
        {
            InitializeComponent();

            foreach (string change in changes)
            {
                lbChangelog.Items.Add(change);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;

            this.Close();
        }
    }
}
