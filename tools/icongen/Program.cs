using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

var fontPath = args.Length > 0 ? args[0] : @"E:\Scripts\acorda\assets\fonts\Lexend.ttf";
var pngDir = args.Length > 1 ? args[1] : @"E:\Scripts\acorda\assets\logo";
var icoPath = args.Length > 2 ? args[2] : @"E:\Scripts\acorda\assets\logo\acorda.ico";
var sizes = new[] { 16, 32, 48, 64, 128, 256 };

var pfc = new PrivateFontCollection();
pfc.AddFontFile(fontPath);
var family = pfc.Families[0];

var corFundo = ColorTranslator.FromHtml("#E8C84A");
var corLetra = ColorTranslator.FromHtml("#3B3320");

Directory.CreateDirectory(pngDir);

foreach (var size in sizes)
{
    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp))
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        using var brushFundo = new SolidBrush(corFundo);
        g.FillEllipse(brushFundo, 0, 0, size, size);

        float fontSize = size * 0.60f;
        using var font = new Font(family, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brushLetra = new SolidBrush(corLetra);
        var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var rect = new RectangleF(0, size * 0.06f, size, size);
        g.DrawString("a", font, brushLetra, rect, formato);
    }

    bmp.Save(Path.Combine(pngDir, $"icon_{size}.png"), ImageFormat.Png);
}

using (var fs = new FileStream(icoPath, FileMode.Create))
using (var bw = new BinaryWriter(fs))
{
    var dados = sizes.Select(s => File.ReadAllBytes(Path.Combine(pngDir, $"icon_{s}.png"))).ToList();

    bw.Write((short)0);
    bw.Write((short)1);
    bw.Write((short)sizes.Length);

    int offset = 6 + 16 * sizes.Length;
    for (int i = 0; i < sizes.Length; i++)
    {
        int size = sizes[i];
        bw.Write((byte)(size >= 256 ? 0 : size));
        bw.Write((byte)(size >= 256 ? 0 : size));
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((short)1);
        bw.Write((short)32);
        bw.Write(dados[i].Length);
        bw.Write(offset);
        offset += dados[i].Length;
    }

    foreach (var d in dados)
        bw.Write(d);
}

Console.WriteLine("Ícone gerado em " + icoPath);
