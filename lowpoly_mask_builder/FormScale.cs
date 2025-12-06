using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lowpoly_mask_builder
{
    public partial class FormScale : Form
    {
        public float ScaleX => (float)numericUpDownX.Value;
        public float ScaleY => (float)numericUpDownY.Value;
        public float ScaleZ => (float)numericUpDownZ.Value;

        public FormScale()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = buttonOK;
            this.CancelButton = buttonCancel;
        }
    }
}
