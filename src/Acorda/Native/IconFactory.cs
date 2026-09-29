using System.Drawing;

namespace Acorda.Native;

// Ícone placeholder (círculo sólido) gerado em memória, até existir um logo de verdade.
public static class IconFactory
{
    public static Icon CriarIconePlaceholder()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(232, 200, 74));
            g.FillEllipse(brush, 2, 2, 28, 28);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }
}
