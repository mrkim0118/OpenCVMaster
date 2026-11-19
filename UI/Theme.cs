using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OpenCVMaster.UI;

static class Theme
{
    public static readonly Color Bg = Color.FromArgb(15, 16, 20);
    public static readonly Color Panel = Color.FromArgb(23, 24, 30);
    public static readonly Color Card = Color.FromArgb(33, 35, 43);
    public static readonly Color CardHover = Color.FromArgb(42, 45, 55);
    public static readonly Color Border = Color.FromArgb(50, 53, 65);
    public static readonly Color Text = Color.FromArgb(232, 234, 240);
    public static readonly Color Sub = Color.FromArgb(145, 150, 168);
    public static readonly Color Accent = Color.FromArgb(108, 99, 255);
    public static readonly Color AccentHover = Color.FromArgb(128, 120, 255);
    public static readonly Color AccentDim = Color.FromArgb(44, 42, 78);
    public static readonly Color Danger = Color.FromArgb(244, 92, 92);
    public static readonly Color Success = Color.FromArgb(52, 211, 153);

    public static readonly Font Body = new("Segoe UI", 9.5f);
    public static readonly Font Small = new("Segoe UI", 8.5f);
    public static readonly Font Bold = new("Segoe UI Semibold", 9.5f);
    public static readonly Font Section = new("Segoe UI Semibold", 8.5f);
    public static readonly Font Title = new("Segoe UI Semibold", 12f);
    public static readonly Font Mono = new("Consolas", 9f);

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string app, string? idList);

    /// <summary>Dark native scrollbars / drop-down chrome (Windows 10 1809+; ignored elsewhere).</summary>
    public static void DarkNative(Control c, string theme = "DarkMode_Explorer")
    {
        try { SetWindowTheme(c.Handle, theme, null); } catch { }
    }

    public static GraphicsPath Round(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        float d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static Button Button(string text, bool primary = false)
    {
        var b = new Button
        {
            Text = text, FlatStyle = FlatStyle.Flat, Font = primary ? Bold : Body, Height = 34, Cursor = Cursors.Hand,
            BackColor = primary ? Accent : Card, ForeColor = primary ? Color.White : Text, UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = primary ? AccentHover : CardHover;
        b.FlatAppearance.MouseDownBackColor = primary ? Accent : Border;
        b.Width = TextRenderer.MeasureText(text, b.Font).Width + 30;
        return b;
    }

    public static Label Label(string text, Font? font = null, Color? color = null, int height = 22) => new()
    {
        Text = text, Font = font ?? Body, ForeColor = color ?? Text, AutoSize = false, Height = height,
        TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent,
    };

    public static Panel Spacer(int h) => new() { Height = h, BackColor = Color.Transparent };

    /// <summary>Dark owner-drawn drop-down. <paramref name="right"/> optionally draws dim text right-aligned in the list.</summary>
    public static ComboBox Combo(Func<object, string>? right = null)
    {
        var c = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 24, BackColor = Card, ForeColor = Text, Font = Body, MaxDropDownItems = 20, IntegralHeight = false,
        };
        c.HandleCreated += (_, _) => DarkNative(c, "DarkMode_CFD");
        c.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool edit = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool sel = !edit && (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(sel ? Accent : Card)) e.Graphics.FillRectangle(b, e.Bounds);
            var item = c.Items[e.Index]!;
            var r = Rectangle.Inflate(e.Bounds, -8, 0);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            if (right != null && !edit)
                TextRenderer.DrawText(e.Graphics, right(item), Small, r, sel ? Color.FromArgb(220, 220, 255) : Sub, flags | TextFormatFlags.Right);
            TextRenderer.DrawText(e.Graphics, c.GetItemText(item), Body, r, sel ? Color.White : Text, flags | TextFormatFlags.Left);
        };
        return c;
    }
}
