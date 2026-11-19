using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace OpenCVMaster.Core;

public sealed record StageResult(Mat Mat, Bitmap Bitmap, string? Error, string? Info, double Ms);

public static class Pipeline
{
    /// <summary>Runs steps[from..] starting from <paramref name="input"/>. Never throws: a failing step passes its input through.</summary>
    public static List<StageResult> Run(IReadOnlyList<(OpDef Op, double[] Values, bool Enabled)> steps, int from, Mat input, Mat original)
    {
        var results = new List<StageResult>();
        var cur = input;
        for (int i = from; i < steps.Count; i++)
        {
            var (op, values, enabled) = steps[i];
            Mat output;
            string? error = null, info = null;
            var sw = Stopwatch.StartNew();
            if (!enabled) output = cur.Clone();
            else
            {
                try
                {
                    var args = new Args(values, original);
                    output = ToDisplayable(op.Run(cur, args));
                    info = args.Info;
                }
                catch (Exception ex)
                {
                    output = cur.Clone();
                    error = ex is OpenCVException cv ? $"{cv.FuncName}: {cv.ErrMsg}" : ex.Message;
                }
            }
            sw.Stop();
            results.Add(new StageResult(output, BitmapConverter.ToBitmap(output), error, info, sw.Elapsed.TotalMilliseconds));
            cur = output;
        }
        return results;
    }

    /// <summary>Every stage is kept as 8-bit, 1 or 3 channel so any operation can follow any other.</summary>
    public static Mat ToDisplayable(Mat m)
    {
        if (m.Empty()) throw new InvalidOperationException("결과 이미지가 비어 있습니다.");
        var r = m;
        if (r.Depth() != MatType.CV_8U)
        {
            var t = new Mat();
            Cv2.Normalize(r, t, 0, 255, NormTypes.MinMax, MatType.CV_8U);
            r = t;
        }
        int ch = r.Channels();
        if (ch == 4)
        {
            var t = new Mat();
            Cv2.CvtColor(r, t, ColorConversionCodes.BGRA2BGR);
            r = t;
        }
        else if (ch == 2 || ch > 4)
        {
            var t = new Mat();
            Cv2.ExtractChannel(r, t, 0);
            r = t;
        }
        if (!r.IsContinuous()) r = r.Clone();
        return r;
    }
}
