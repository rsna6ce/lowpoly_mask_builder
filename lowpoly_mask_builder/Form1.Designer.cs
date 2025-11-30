
namespace lowpoly_mask_builder
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.pictureBoxLeft = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pictureBoxRight = new System.Windows.Forms.PictureBox();
            this.vScrollBarZ = new System.Windows.Forms.VScrollBar();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.numericUpDownZ = new System.Windows.Forms.NumericUpDown();
            this.buttonPreview = new System.Windows.Forms.Button();
            this.optionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.transparentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.surfaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.export2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unifyTriangleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flipAllTriangleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).BeginInit();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownZ)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxLeft
            // 
            this.pictureBoxLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.pictureBoxLeft.Location = new System.Drawing.Point(4, 28);
            this.pictureBoxLeft.Name = "pictureBoxLeft";
            this.pictureBoxLeft.Size = new System.Drawing.Size(600, 900);
            this.pictureBoxLeft.TabIndex = 0;
            this.pictureBoxLeft.TabStop = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1252, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.exportToolStripMenuItem,
            this.export2ToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.newToolStripMenuItem.Text = "New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveAsToolStripMenuItem.Text = "Save as";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // exportToolStripMenuItem
            // 
            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            this.exportToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exportToolStripMenuItem.Text = "Export Surface STL";
            this.exportToolStripMenuItem.Click += new System.EventHandler(this.exportToolStripMenuItem_Click);
            // 
            // pictureBoxRight
            // 
            this.pictureBoxRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.pictureBoxRight.Location = new System.Drawing.Point(604, 28);
            this.pictureBoxRight.Name = "pictureBoxRight";
            this.pictureBoxRight.Size = new System.Drawing.Size(600, 900);
            this.pictureBoxRight.TabIndex = 0;
            this.pictureBoxRight.TabStop = false;
            this.pictureBoxRight.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxRight_Paint);
            this.pictureBoxRight.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxRight_MouseDown);
            this.pictureBoxRight.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxRight_MouseMove);
            this.pictureBoxRight.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxRight_MouseUp);
            // 
            // vScrollBarZ
            // 
            this.vScrollBarZ.LargeChange = 1;
            this.vScrollBarZ.Location = new System.Drawing.Point(1212, 28);
            this.vScrollBarZ.Name = "vScrollBarZ";
            this.vScrollBarZ.Size = new System.Drawing.Size(32, 864);
            this.vScrollBarZ.TabIndex = 2;
            this.vScrollBarZ.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBarZ_Scroll);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel1});
            this.statusStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.statusStrip1.Location = new System.Drawing.Point(0, 936);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1252, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel1
            // 
            this.statusLabel1.Name = "statusLabel1";
            this.statusLabel1.Size = new System.Drawing.Size(22, 17);
            this.statusLabel1.Text = "     ";
            // 
            // numericUpDownZ
            // 
            this.numericUpDownZ.Location = new System.Drawing.Point(1208, 900);
            this.numericUpDownZ.Name = "numericUpDownZ";
            this.numericUpDownZ.Size = new System.Drawing.Size(40, 19);
            this.numericUpDownZ.TabIndex = 4;
            this.numericUpDownZ.ValueChanged += new System.EventHandler(this.numericUpDownZ_ValueChanged);
            // 
            // buttonPreview
            // 
            this.buttonPreview.Location = new System.Drawing.Point(24, 40);
            this.buttonPreview.Name = "buttonPreview";
            this.buttonPreview.Size = new System.Drawing.Size(108, 40);
            this.buttonPreview.TabIndex = 5;
            this.buttonPreview.Text = "preveiw debug";
            this.buttonPreview.UseVisualStyleBackColor = true;
            this.buttonPreview.Visible = false;
            this.buttonPreview.Click += new System.EventHandler(this.buttonPreview_Click);
            // 
            // optionToolStripMenuItem
            // 
            this.optionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.transparentToolStripMenuItem,
            this.surfaceToolStripMenuItem,
            this.unifyTriangleToolStripMenuItem,
            this.flipAllTriangleToolStripMenuItem});
            this.optionToolStripMenuItem.Name = "optionToolStripMenuItem";
            this.optionToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.optionToolStripMenuItem.Text = "Option";
            // 
            // transparentToolStripMenuItem
            // 
            this.transparentToolStripMenuItem.Name = "transparentToolStripMenuItem";
            this.transparentToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.transparentToolStripMenuItem.Text = "Transparent";
            this.transparentToolStripMenuItem.Click += new System.EventHandler(this.transparentToolStripMenuItem_Click);
            // 
            // surfaceToolStripMenuItem
            // 
            this.surfaceToolStripMenuItem.Name = "surfaceToolStripMenuItem";
            this.surfaceToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.surfaceToolStripMenuItem.Text = "Surface";
            // 
            // export2ToolStripMenuItem
            // 
            this.export2ToolStripMenuItem.Name = "export2ToolStripMenuItem";
            this.export2ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.export2ToolStripMenuItem.Text = "Export Volume STL";
            this.export2ToolStripMenuItem.Click += new System.EventHandler(this.export2ToolStripMenuItem_Click);
            // 
            // unifyTriangleToolStripMenuItem
            // 
            this.unifyTriangleToolStripMenuItem.Name = "unifyTriangleToolStripMenuItem";
            this.unifyTriangleToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.unifyTriangleToolStripMenuItem.Text = "Unify triangle";
            this.unifyTriangleToolStripMenuItem.Click += new System.EventHandler(this.unifyTriangleToolStripMenuItem_Click);
            // 
            // flipAllTriangleToolStripMenuItem
            // 
            this.flipAllTriangleToolStripMenuItem.Name = "flipAllTriangleToolStripMenuItem";
            this.flipAllTriangleToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.flipAllTriangleToolStripMenuItem.Text = "Flip all triangle";
            this.flipAllTriangleToolStripMenuItem.Click += new System.EventHandler(this.flipAllTriangleToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1252, 958);
            this.Controls.Add(this.buttonPreview);
            this.Controls.Add(this.numericUpDownZ);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.vScrollBarZ);
            this.Controls.Add(this.pictureBoxRight);
            this.Controls.Add(this.pictureBoxLeft);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Lowpoly mask builder";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownZ)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxLeft;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.PictureBox pictureBoxRight;
        private System.Windows.Forms.VScrollBar vScrollBarZ;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel1;
        private System.Windows.Forms.NumericUpDown numericUpDownZ;
        private System.Windows.Forms.Button buttonPreview;
        private System.Windows.Forms.ToolStripMenuItem optionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem transparentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem export2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem surfaceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unifyTriangleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem flipAllTriangleToolStripMenuItem;
    }
}

