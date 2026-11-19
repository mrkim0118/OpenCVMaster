using System;
using System.Drawing;
using System.Linq;
using OpenCvSharp;

namespace OpenCVMaster.Core;

public enum ParamKind { Int, Double, Bool, Choice }

/// <summary>One tunable parameter of an operation. All values are stored as double.</summary>
public sealed class Param
{
    public string Label = "";
    public ParamKind Kind;
    public double Min, Max, Default, Step = 1;
    public bool Odd;
    public string[] Options = [];

    public int Decimals => Kind != ParamKind.Double ? 0 : Step < 0.01 ? 3 : Step < 0.1 ? 2 : 1;

    public static Param Int(string label, int min, int max, int def, bool odd = false) =>
        new() { Label = label, Kind = ParamKind.Int, Min = min, Max = max, Default = def, Odd = odd, Step = odd ? 2 : 1 };

    public static Param Dbl(string label, double min, double max, double def, double step = 0.1) =>
        new() { Label = label, Kind = ParamKind.Double, Min = min, Max = max, Default = def, Step = step };

    public static Param Bool(string label, bool def) =>
        new() { Label = label, Kind = ParamKind.Bool, Min = 0, Max = 1, Default = def ? 1 : 0 };

    public static Param Choice(string label, int def, params string[] options) =>
        new() { Label = label, Kind = ParamKind.Choice, Min = 0, Max = options.Length - 1, Default = def, Options = options };

    public static Param Enum<T>(string label, T def) where T : struct, System.Enum =>
        Choice(label, Array.IndexOf(System.Enum.GetValues<T>(), def), System.Enum.GetNames<T>());

    public double Clamp(double v)
    {
        v = Math.Clamp(v, Min, Max);
        if (Kind != ParamKind.Double) v = Math.Round(v);
        else v = Math.Round(v / Step) * Step;
        if (Odd && ((int)v) % 2 == 0) v = v + 1 <= Max ? v + 1 : v - 1;
        return Math.Clamp(v, Min, Max);
    }
}

/// <summary>Parameter values handed to an operation, plus the original image and an info slot.</summary>
public sealed class Args(double[] values, Mat original)
{
    public Mat Original { get; } = original;
    /// <summary>Optional text the operation reports back (counts, decoded text, computed thresholds...).</summary>
    public string? Info { get; set; }
    public int I(int i) => (int)Math.Round(values[i]);
    public double D(int i) => values[i];
    public float F(int i) => (float)values[i];
    public bool B(int i) => values[i] != 0;
    public T E<T>(int i) where T : struct, System.Enum => System.Enum.GetValues<T>()[I(i)];
}

public sealed class OpDef(string category, string name, string api, string description, Param[] parameters, Func<Mat, Args, Mat> run)
{
    public string Category { get; } = category;
    public string Name { get; } = name;
    public string Api { get; } = api;
    public string Description { get; } = description;
    public Param[] Params { get; } = parameters;
    public Func<Mat, Args, Mat> Run { get; } = run;
    public override string ToString() => Name;
}

/// <summary>One step of the user's processing sequence and its latest result.</summary>
public sealed class Step(OpDef op)
{
    public OpDef Op { get; } = op;
    public double[] Values { get; } = op.Params.Select(p => p.Default).ToArray();
    public bool Enabled = true;
    public Mat? Result;
    public Bitmap? Bitmap;
    public string? Error, Info;
    public double Ms;
}
