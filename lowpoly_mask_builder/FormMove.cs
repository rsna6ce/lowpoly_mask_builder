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
    public partial class FormMove : Form
    {
        public int MoveX => (int)numericUpDownX.Value;
        public int MoveY => (int)numericUpDownY.Value;
        public int MoveZ => (int)numericUpDownZ.Value;

        public FormMove()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = buttonOK;
            this.CancelButton = buttonCancel;
        }
    }
}
