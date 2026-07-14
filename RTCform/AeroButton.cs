using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RTCVideoChat
{
    public class AeroButton : Button
    {
        private Color _color1 = Color.FromArgb(0, 122, 255);
        private Color _color2 = Color.FromArgb(0, 80, 210);

        public Color Color1
        {
            get { return _color1; }
            set { _color1 = value; Invalidate(); }
        }

        public Color Color2
        {
            get { return _color2; }
            set { _color2 = value; Invalidate(); }
        }

        public AeroButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.Size = new Size(100, 35); // Varsayılan boyut
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            // Temel boyama işlemini çağırarak başlayalım, bu bazı kenar durumlarını ele alır.
            base.OnPaint(pevent);

            pevent.Graphics.Clear(this.Parent.BackColor); // Arka planı temizle
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            
            // Düğme devre dışıysa farklı boya
            if (!this.Enabled)
            {
                 using (SolidBrush brush = new SolidBrush(Color.FromArgb(204, 204, 204)))
                 {
                    pevent.Graphics.FillRectangle(brush, rect);
                 }
            }
            else 
            {
                // Ana gradyan
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, _color1, _color2, 90F))
                {
                    pevent.Graphics.FillRectangle(brush, rect);
                }
                // Üstüne parlama efekti için ikinci, daha açık bir gradyan
                using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, this.Width, this.Height / 2),
                    Color.FromArgb(50, Color.White),
                    Color.FromArgb(0, Color.White), 
                    90F))
                {
                    pevent.Graphics.FillRectangle(glossBrush, new Rectangle(0, 0, this.Width, this.Height / 2));
                }
            }

            // Kenarlık
            pevent.Graphics.DrawRectangle(new Pen(Color.FromArgb(100, Color.Black), 1), rect);
            
            // Metin
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;
            TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font, ClientRectangle, this.ForeColor, flags);
        }
    }
} 