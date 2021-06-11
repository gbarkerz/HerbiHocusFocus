using System;
using System.Drawing;
using System.Windows.Forms;

namespace HerbiHocusFocus
{
    public partial class ColorSelector : Form
    {
        // Barker: Consider incorporating the colors into the ListView item definitions.
        private Color[] colors = new Color[] {
                Color.FromArgb(255, 255, 255, 0),
                Color.FromArgb(255, 0, 255, 0),
                Color.FromArgb(255, 0, 255, 255),
                Color.FromArgb(255, 255, 0, 255),
                Color.FromArgb(255, 0, 0, 255),
                Color.FromArgb(255, 255, 0, 0),
                Color.FromArgb(255, 0, 0, 128),
                Color.FromArgb(255, 0, 128, 128),
                Color.FromArgb(255, 0, 128, 0),
                Color.FromArgb(255, 128, 0, 128),
                Color.FromArgb(255, 128, 0, 0),
                Color.FromArgb(255, 128, 128, 0),
                Color.FromArgb(255, 128, 128, 128),
                Color.FromArgb(255, 192, 192, 192),
                Color.FromArgb(255, 0, 0, 0),
                Color.FromArgb(255, 255, 255, 255) };

        private int tilesPerRow = 4;

        private Color currentColor;
        private Color selectedColor = Color.Black;
        public Color SelectedColor
        {
            get
            {
                return selectedColor;
            }
            set
            {
                selectedColor = value;
            }
        }

        public ColorSelector(Color currentColor)
        {
            this.currentColor = currentColor;

            InitializeComponent();

            // Account for display scaling affecting the required size of the tiles in the list.
            int tileWidth = ((listViewColors.Width - (tilesPerRow - 1)) / tilesPerRow);
            int tileHeight = ((listViewColors.Height - (tilesPerRow - 1)) / tilesPerRow); 
            listViewColors.TileSize = new Size(tileWidth, tileHeight);
        }

        private void ColorSelector_Load(object sender, EventArgs e)
        {
            // If we can't find the supplied color, select Black instead.
            if (!SelectColor(this.currentColor))
            {
                SelectColor(SelectedColor);
            }
        }

        private bool SelectColor(Color col)
        {
            bool foundColor = false;

            for (int i = 0; i < colors.Length; ++i)
            {
                if (col.ToArgb() == colors[i].ToArgb())
                {
                    listViewColors.Items[i].Focused = true;
                    listViewColors.Items[i].Selected = true;

                    foundColor = true;

                    break;
                }
            }

            return foundColor;
        }

        private void ListView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Display the tiles in a grid.
            int x = ((e.ItemIndex % tilesPerRow) * listViewColors.TileSize.Width);
            int y = ((e.ItemIndex / tilesPerRow) * listViewColors.TileSize.Height);

            Rectangle rect = new Rectangle(x, y, listViewColors.TileSize.Width, listViewColors.TileSize.Height);

            int itemOffset = 10;
            rect.Inflate(-itemOffset, -itemOffset);
            e.Graphics.FillRectangle(new SolidBrush(colors[e.ItemIndex]), rect);

            e.DrawFocusRectangle();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;

            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            // We should always have a selection.
            if (listViewColors.SelectedIndices.Count == 1)
            {
                this.selectedColor = colors[listViewColors.SelectedIndices[0]];

                this.DialogResult = DialogResult.OK;
            }

            this.Close();
        }
    }
}
