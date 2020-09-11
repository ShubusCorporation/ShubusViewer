using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace ExtListBox1
{
    sealed class ExtListBox : ListBox
    {
        const int cornerRadius = 4;
        int x, y, rectWidth, rectHeight;

        public ExtListBox()
        {
            this.DrawMode = DrawMode.OwnerDrawFixed;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index <= -1 || this.Items.Count == 0) return;
            // Getting the text and the color.
            string s = Items[e.Index].ToString();

            Color rectColor = SystemColors.Window;
            try
            {
                KnownColor selectedColor = (KnownColor)System.Enum.Parse(typeof(KnownColor), s);
                rectColor = System.Drawing.Color.FromKnownColor(selectedColor);
            }
            catch (Exception) { }

            // String format to draw test
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            
            // Create defined color brush 
            Brush solidBrush = new SolidBrush(Color.FromArgb(45, 131, 218));
            
            // Create a brush with a vertical gradient
            Brush gradientBrush = new LinearGradientBrush(e.Bounds, Color.FromArgb(121, 187, 255), Color.FromArgb(65, 151, 238), LinearGradientMode.Horizontal);
            
            // Determine which element is to draw
            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
            {
                // Fill the rectangle with selected color
                e.Graphics.FillRectangle(new SolidBrush(SystemColors.Window), e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height + 1);
            }
            else // The item is active
            {
                // Create a path to repeat loop with rounded corners
                GraphicsPath gfxPath = new GraphicsPath();
                
                // Determine the coordinates of an element in the list
                x = e.Bounds.X;
                y = e.Bounds.Y;
                
                // Also calculate the width and height
                rectWidth = e.Bounds.Width - 2;
                rectHeight = e.Bounds.Height;
                #region Draw a rectangle with rounded corners
                gfxPath.AddLine(x + cornerRadius, y, x + rectWidth - (cornerRadius * 2), y);
                gfxPath.AddArc(x + rectWidth - (cornerRadius * 2), y, cornerRadius * 2, cornerRadius * 2, 270, 90);
                gfxPath.AddLine(x + rectWidth, y + cornerRadius, x + rectWidth, y + rectHeight - (cornerRadius * 2));
                gfxPath.AddArc(x + rectWidth - (cornerRadius * 2), y + rectHeight - (cornerRadius * 2), cornerRadius * 2, cornerRadius * 2, 0, 90);
                gfxPath.AddLine(x + rectWidth - (cornerRadius * 2), y + rectHeight, x + cornerRadius, y + rectHeight);
                gfxPath.AddArc(x, y + rectHeight - (cornerRadius * 2), cornerRadius * 2, cornerRadius * 2, 90, 90);
                gfxPath.AddLine(x, y + rectHeight - (cornerRadius * 2), x, y + cornerRadius);
                gfxPath.AddArc(x, y, cornerRadius * 2, cornerRadius * 2, 180, 90);
                gfxPath.CloseFigure();
                e.Graphics.DrawPath(new Pen(solidBrush, 1), gfxPath);
                // Fill the region
                e.Graphics.FillPath(gradientBrush, gfxPath);
                gfxPath.Dispose();
                #endregion
            }
            // Draw text.
            e.Graphics.DrawString(s, Font, new SolidBrush(SystemColors.WindowText), new RectangleF(0, e.Bounds.Y, e.Bounds.Width, 16), sf);
            // Color rectangle
            try
            {
                // Draw border rectangle.
                e.Graphics.FillRectangle(new SolidBrush(Color.Black)
                    , e.Bounds.X + 9, e.Bounds.Y + 1, 22, e.Bounds.Height - 2);
                // Draw color rectangle.
                e.Graphics.FillRectangle(new SolidBrush(rectColor)
                    , e.Bounds.X + 10, e.Bounds.Y + 2, 20, e.Bounds.Height - 4);
            }
            catch (Exception) { }
        }

        // After size changing
        protected override void OnSizeChanged(EventArgs e)
        {
            // Update the component
            Refresh();
            base.OnSizeChanged(e);
        }
    }
}