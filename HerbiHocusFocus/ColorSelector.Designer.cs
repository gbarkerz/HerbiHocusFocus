namespace HerbiHocusFocus
{
    partial class ColorSelector
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColorSelector));
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.labelColors = new System.Windows.Forms.Label();
            this.listViewColors = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // labelColors
            // 
            resources.ApplyResources(this.labelColors, "labelColors");
            this.labelColors.Name = "labelColors";
            // 
            // listViewColors
            // 
            resources.ApplyResources(this.listViewColors, "listViewColors");
            this.listViewColors.BackColor = System.Drawing.SystemColors.Control;
            this.listViewColors.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items1"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items2"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items3"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items4"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items5"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items6"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items7"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items8"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items9"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items10"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items11"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items12"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items13"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items14"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewColors.Items15")))});
            this.listViewColors.MultiSelect = false;
            this.listViewColors.Name = "listViewColors";
            this.listViewColors.OwnerDraw = true;
            this.listViewColors.Scrollable = false;
            this.listViewColors.TileSize = new System.Drawing.Size(150, 150);
            this.listViewColors.UseCompatibleStateImageBehavior = false;
            this.listViewColors.View = System.Windows.Forms.View.Tile;
            this.listViewColors.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.ListView1_DrawItem);
            // 
            // ColorSelector
            // 
            this.AcceptButton = this.buttonOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.Controls.Add(this.labelColors);
            this.Controls.Add(this.listViewColors);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.DoubleBuffered = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ColorSelector";
            this.Load += new System.EventHandler(this.ColorSelector_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label labelColors;
        private System.Windows.Forms.ListView listViewColors;
    }
}