using System;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace OpenCVMaster.Core;

/// <summary>Headless check: every operation on color and gray input with default params, plus a save/load round trip.</summary>
public static class SelfTest
{
    public static int Run()
    {
        var log = new StringBuilder();
        int fails = 0;
        log.AppendLine($"OpenCV {Cv2.GetVersionString()} · {Catalog.All.Count} operations");
        using var color = ImageIO.CreateSample();
        using var gray = new Mat();
        Cv2.CvtColor(color, gray, ColorConversionCodes.BGR2GRAY);

        foreach (var input in new[] { color, gray })
            foreach (var op in Catalog.All)
            {
                var steps = new[] { (op, op.Params.Select(p => p.Default).ToArray(), true) };
                var r = Pipeline.Run(steps, 0, input, color)[0];
                bool ok = r.Error == null && r.Mat.Depth() == MatType.CV_8U && r.Mat.Channels() is 1 or 3;
                if (!ok) fails++;
                log.AppendLine($"{(ok ? "OK  " : "FAIL")} [{input.Channels()}ch] {op.Category} / {op.Name}  {r.Ms,7:0.0} ms  {r.Error}{(r.Info != null ? "  → " + r.Info : "")}");
                r.Mat.Dispose(); r.Bitmap.Dispose();
            }

        // Parameter extremes must not crash the pipeline (errors are allowed, exceptions are not)
        foreach (var op in Catalog.All)
            foreach (var pick in new Func<Param, double>[] { p => p.Min, p => p.Max })
            {
                var steps = new[] { (op, op.Params.Select(p => p.Clamp(pick(p))).ToArray(), true) };
                var r = Pipeline.Run(steps, 0, color, color)[0];
                if (r.Error != null) log.AppendLine($"note: {op.Name} at extreme → {r.Error}");
                r.Mat.Dispose(); r.Bitmap.Dispose();
            }

        // every drop-down label must have an English name
        foreach (var name in Catalog.Categories.Concat(Catalog.All.Select(o => o.Name)))
            if (Catalog.English(name).Any(c => c is >= '가' and <= '힣')) { fails++; log.AppendLine($"FAIL no English name: {name}"); }

        var dir = Path.Combine(Path.GetTempPath(), "OpenCVMaster 테스트");
        Directory.CreateDirectory(dir);
        foreach (var ext in new[] { ".png", ".jpg", ".bmp", ".tif", ".webp", ".jp2" })
        {
            var path = Path.Combine(dir, "샘플" + ext);
            try
            {
                ImageIO.Save(path, color);
                using var back = ImageIO.Load(path);
                bool ok = back.Size() == color.Size();
                if (!ok) fails++;
                log.AppendLine($"{(ok ? "OK  " : "FAIL")} round trip {ext}");
            }
            catch (Exception ex) { fails++; log.AppendLine($"FAIL round trip {ext}: {ex.Message}"); }
        }
        // GDI+/WIC fallback paths: write a GIF and ICO with System.Drawing and read them back
        foreach (var fmt in new[] { System.Drawing.Imaging.ImageFormat.Gif, System.Drawing.Imaging.ImageFormat.Icon })
        {
            var path = Path.Combine(dir, "fallback." + fmt);
            try
            {
                using (var bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(color)) bmp.Save(path, fmt);
                using var back = ImageIO.Load(path);
                log.AppendLine($"OK   load {fmt} → {back.Width}x{back.Height}");
            }
            catch (Exception ex) { fails++; log.AppendLine($"FAIL load {fmt}: {ex.Message}"); }
        }

        log.AppendLine(fails == 0 ? "ALL PASSED" : $"{fails} FAILED");
        File.WriteAllText("selftest.log", log.ToString());
        return fails;
    }
}
