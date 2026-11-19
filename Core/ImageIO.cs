using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using WicDecoder = System.Windows.Media.Imaging.BitmapDecoder;
using WicCreate = System.Windows.Media.Imaging.BitmapCreateOptions;
using WicCache = System.Windows.Media.Imaging.BitmapCacheOption;
using WicConvert = System.Windows.Media.Imaging.FormatConvertedBitmap;
using WicFormats = System.Windows.Media.PixelFormats;

namespace OpenCVMaster.Core;

/// <summary>
/// Loads images through three decoders in turn: OpenCV, Windows Imaging Component (WIC), then GDI+.
/// Files are read as bytes so non-ASCII (e.g. Korean) paths work.
/// </summary>
public static class ImageIO
{
    public static readonly string[] Extensions =
    [
        // OpenCV
        "bmp", "dib", "jpg", "jpeg", "jpe", "jfif", "jp2", "png", "webp", "avif", "gif", "tif", "tiff",
        "pbm", "pgm", "ppm", "pxm", "pnm", "pfm", "pam", "sr", "ras", "exr", "hdr", "pic",
        // WIC (Windows codecs: HEIF/HEIC requires the HEIF extension from Microsoft Store)
        "heic", "heif", "jxr", "wdp", "hdp", "dds", "ico", "cur", "jxl",
        // GDI+
        "emf", "wmf",
    ];

    public static string OpenFilter =>
        "모든 이미지|" + string.Join(";", Extensions.Select(e => "*." + e)) +
        "|JPEG|*.jpg;*.jpeg;*.jpe;*.jfif|PNG|*.png|BMP|*.bmp;*.dib|TIFF|*.tif;*.tiff|WebP|*.webp|GIF|*.gif" +
        "|HEIC/HEIF|*.heic;*.heif|JPEG 2000|*.jp2|HDR/EXR|*.hdr;*.pic;*.exr|PNM|*.pbm;*.pgm;*.ppm;*.pnm;*.pfm;*.pam|모든 파일|*.*";

    public const string SaveFilter = "PNG|*.png|JPEG|*.jpg|BMP|*.bmp|TIFF|*.tif|WebP|*.webp|JPEG 2000|*.jp2";

    public static Mat Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Mat? m = null;
        try { m = Cv2.ImDecode(bytes, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation); } catch { }
        if (m == null || m.Empty()) m = TryWic(bytes);
        if (m == null || m.Empty()) m = TryGdi(bytes);
        if (m == null || m.Empty()) throw new InvalidDataException("지원하지 않는 형식이거나 손상된 이미지입니다.");
        return Pipeline.ToDisplayable(m);
    }

    public static void Save(string path, Mat mat)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) { ext = ".png"; path += ext; }
        Cv2.ImEncode(ext, mat, out var buf);
        File.WriteAllBytes(path, buf);
    }

    static Mat? TryWic(byte[] bytes)
    {
        try
        {
            using var ms = new MemoryStream(bytes);
            var dec = WicDecoder.Create(ms, WicCreate.PreservePixelFormat, WicCache.OnLoad);
            var bmp = new WicConvert(dec.Frames[0], WicFormats.Bgra32, null, 0);
            var m = new Mat(bmp.PixelHeight, bmp.PixelWidth, MatType.CV_8UC4);
            int stride = (int)m.Step();
            bmp.CopyPixels(System.Windows.Int32Rect.Empty, m.Data, stride * m.Rows, stride);
            return m;
        }
        catch { return null; }
    }

    static Mat? TryGdi(byte[] bytes)
    {
        try
        {
            using var ms = new MemoryStream(bytes);
            using var src = Image.FromStream(ms);
            using var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);
            return bmp.ToMat();
        }
        catch { return null; }
    }

    /// <summary>Synthetic test image with shapes, coins, lines and text so every feature has something to work on.</summary>
    public static Mat CreateSample()
    {
        using var corners = new Mat(2, 2, MatType.CV_8UC3);
        corners.Set(0, 0, new Vec3b(70, 40, 25));
        corners.Set(0, 1, new Vec3b(110, 70, 40));
        corners.Set(1, 0, new Vec3b(40, 30, 30));
        corners.Set(1, 1, new Vec3b(90, 50, 60));
        var m = new Mat();
        Cv2.Resize(corners, m, new OpenCvSharp.Size(800, 520), 0, 0, InterpolationFlags.Linear);
        var aa = LineTypes.AntiAlias;

        Cv2.Rectangle(m, new Rect(50, 60, 180, 130), new Scalar(80, 200, 255), -1, aa);
        Cv2.Rectangle(m, new Rect(80, 90, 60, 50), new Scalar(40, 40, 40), -1, aa);
        Cv2.Circle(m, new OpenCvSharp.Point(380, 130), 75, new Scalar(90, 220, 120), -1, aa);
        Cv2.Ellipse(m, new OpenCvSharp.Point(600, 130), new OpenCvSharp.Size(120, 55), 25, 0, 360, new Scalar(230, 120, 90), -1, aa);
        Cv2.FillPoly(m, new[] { new[] { new OpenCvSharp.Point(110, 460), new OpenCvSharp.Point(200, 270), new OpenCvSharp.Point(290, 460) } }, new Scalar(200, 90, 230), aa);
        Cv2.FillPoly(m, new[] { new[] { new OpenCvSharp.Point(330, 300), new OpenCvSharp.Point(420, 260), new OpenCvSharp.Point(480, 330), new OpenCvSharp.Point(450, 440), new OpenCvSharp.Point(350, 430) } }, new Scalar(60, 170, 240), aa);
        foreach (var (x, y, r) in new[] { (560, 330, 38), (650, 300, 30), (720, 380, 42), (600, 440, 28), (690, 470, 22) })
        {
            Cv2.Circle(m, new OpenCvSharp.Point(x, y), r, new Scalar(160, 200, 220), -1, aa);
            Cv2.Circle(m, new OpenCvSharp.Point(x, y), r - 6, new Scalar(120, 160, 190), 3, aa);
        }
        for (int i = 0; i < 5; i++)
            Cv2.Line(m, new OpenCvSharp.Point(30 + i * 14, 500), new OpenCvSharp.Point(260 + i * 14, 230), new Scalar(255, 255, 255), 1, aa);
        Cv2.PutText(m, "OpenCV Master", new OpenCvSharp.Point(300, 500), HersheyFonts.HersheyDuplex, 1.1, new Scalar(245, 245, 245), 2, aa);

        using var noise = new Mat(m.Size(), MatType.CV_16SC3);
        Cv2.Randn(noise, Scalar.All(0), Scalar.All(6));
        using var m16 = new Mat();
        m.ConvertTo(m16, MatType.CV_16SC3);
        Cv2.Add(m16, noise, m16);
        m16.ConvertTo(m, MatType.CV_8UC3);
        return m;
    }
}
