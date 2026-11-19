using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OpenCVMaster.UI;

/// <summary>Flat dark slider (track + accent fill + round thumb) with mouse, wheel and arrow-key input.</summary>
sealed class FlatSlider : Control
{
    double min, max = 100, cur, step = 1;
    bool dragging;
    public event EventHandler? ValueChanged;

    public FlatSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Height = 24;
        Cursor = Cursors.Hand;
    }

    public void Setup(double minimum, double maximum, double smallStep, double v)
    {
        min = minimum; max = Math.Max(maximum, minimum + 1e-9); step = smallStep; cur = Math.Clamp(v, min, max);
        Invalidate();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Value
    {
        get => cur;
        set
        {
            var v = Math.Clamp(min + Math.Round((value - min) / step) * step, min, max);
            if (v == cur) return;
            cur = v;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    const int Pad = 9;
    float X(double v) => Pad + (float)((v - min) / (max - min)) * (Width - 2 * Pad);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cy = Height / 2f, x = X(cur);
        using (var track = Theme.Round(new RectangleF(Pad, cy - 2, Width - 2 * Pad, 4), 2))
        using (var b = new SolidBrush(Theme.Border)) g.FillPath(b, track);
        if (x - Pad > 1)
            using (var fill = Theme.Round(new RectangleF(Pad, cy - 2, x - Pad, 4), 2))
            using (var b = new SolidBrush(Enabled ? Theme.Accent : Theme.Sub)) g.FillPath(b, fill);
        float r = dragging || Focused ? 8 : 7;
        using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, x - r, cy - r, 2 * r, 2 * r);
        using (var pen = new Pen(Theme.Accent, 2.5f)) g.DrawEllipse(pen, x - r, cy - r, 2 * r, 2 * r);
    }

    void SetFromMouse(int mx) => Value = min + (mx - Pad) / (double)Math.Max(1, Width - 2 * Pad) * (max - min);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        dragging = true;
        SetFromMouse(e.X);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) SetFromMouse(e.X); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Invalidate(); }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Value += Math.Sign(e.Delta) * step; ((HandledMouseEventArgs)e).Handled = true; }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override bool IsInputKey(Keys k) => k is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(k);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Left or Keys.Down) Value -= step;
        if (e.KeyCode is Keys.Right or Keys.Up) Value += step;
    }
}

/// <summary>Image viewer with fit-to-window, wheel zoom around the cursor, drag pan and a linked partner view.</summary>
sealed class ImageView : Control
{
    Bitmap? img;
    Size imgSize;
    float scale = 1;
    PointF org;
    bool fit = true, dragging;
    Point last;

    public string Caption = "";
    public string EmptyText = "";
    public ImageView? Partner;
    /// <summary>Image coordinate under the mouse, or null when outside the image.</summary>
    public event Action<Point?>? Hover;

    public ImageView()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        Cursor = Cursors.Cross;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Bitmap? Image
    {
        get => img;
        set
        {
            var newSize = value?.Size ?? Size.Empty;
            bool sizeChanged = newSize != imgSize;
            img = value;
            imgSize = newSize;
            if (fit || sizeChanged) FitNow();
            Invalidate();
        }
    }

    public void Fit() { fit = true; FitNow(); Invalidate(); Partner?.SyncFrom(this); }

    void FitNow()
    {
        if (imgSize.IsEmpty || Width < 20 || Height < 20) return;
        scale = Math.Min((Width - 24f) / imgSize.Width, (Height - 24f) / imgSize.Height);
        if (scale <= 0) scale = 1;
        org = new PointF((Width - imgSize.Width * scale) / 2, (Height - imgSize.Height * scale) / 2);
    }

    void SyncFrom(ImageView o)
    {
        fit = o.fit;
        if (fit) FitNow(); else { scale = o.scale; org = o.org; }
        Invalidate();
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); if (fit) FitNow(); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        if (img == null)
        {
            TextRenderer.DrawText(g, EmptyText, Theme.Body, ClientRectangle, Theme.Sub, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            return;
        }
        g.InterpolationMode = scale >= 2 ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        var dest = new RectangleF(org.X, org.Y, imgSize.Width * scale, imgSize.Height * scale);
        try { g.DrawImage(img, dest); }
        catch (ArgumentException) { return; } // bitmap was swapped out mid-paint; the next paint has the new one
        using (var pen = new Pen(Theme.Border)) g.DrawRectangle(pen, dest.X - 1, dest.Y - 1, dest.Width + 1, dest.Height + 1);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (Caption.Length > 0) Pill(g, Caption, new Point(12, 12), false);
        Pill(g, $"{scale * 100:0}%  ·  {imgSize.Width}×{imgSize.Height}", new Point(Width - 12, Height - 12), true);
    }

    static void Pill(Graphics g, string text, Point anchor, bool bottomRight)
    {
        var sz = TextRenderer.MeasureText(text, Theme.Small);
        var r = new Rectangle(bottomRight ? anchor.X - sz.Width - 16 : anchor.X, bottomRight ? anchor.Y - 24 : anchor.Y, sz.Width + 16, 24);
        using (var path = Theme.Round(r, 12))
        using (var b = new SolidBrush(Color.FromArgb(200, 20, 21, 27))) g.FillPath(b, path);
        TextRenderer.DrawText(g, text, Theme.Small, r, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button is MouseButtons.Left or MouseButtons.Middle) { dragging = true; last = e.Location; Cursor = Cursors.SizeAll; }
    }

    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Cursor = Cursors.Cross; }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging && img != null)
        {
            org = new PointF(org.X + e.X - last.X, org.Y + e.Y - last.Y);
            last = e.Location;
            fit = false;
            Invalidate();
            Partner?.SyncFrom(this);
        }
        if (img == null) { Hover?.Invoke(null); return; }
        int ix = (int)Math.Floor((e.X - org.X) / scale), iy = (int)Math.Floor((e.Y - org.Y) / scale);
        Hover?.Invoke(ix >= 0 && iy >= 0 && ix < imgSize.Width && iy < imgSize.Height ? new Point(ix, iy) : null);
    }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Hover?.Invoke(null); }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (img == null) return;
        float ns = Math.Clamp(scale * (e.Delta > 0 ? 1.25f : 0.8f), 0.02f, 64f);
        org = new PointF(e.X - (e.X - org.X) * ns / scale, e.Y - (e.Y - org.Y) * ns / scale);
        scale = ns;
        fit = false;
        Invalidate();
        Partner?.SyncFrom(this);
    }

    protected override void OnDoubleClick(EventArgs e) { base.OnDoubleClick(e); Fit(); }
}

/// <summary>Owner-drawn list that skips the full background erase (the source of flicker) and paints only the empty area below the items.</summary>
sealed class StepListBox : ListBox
{
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0014) // WM_ERASEBKGND
        {
            int itemsBottom = Math.Max(0, Items.Count - TopIndex) * ItemHeight;
            if (itemsBottom < ClientSize.Height)
                using (var g = Graphics.FromHdc(m.WParam))
                using (var b = new SolidBrush(BackColor))
                    g.FillRectangle(b, 0, itemsBottom, ClientSize.Width, ClientSize.Height - itemsBottom);
            m.Result = 1;
            return;
        }
        base.WndProc(ref m);
    }
}

/// <summary>One stage thumbnail in the pipeline film strip.</summary>
sealed class ThumbCard : Control
{
    public Bitmap? Image;
    public string Title = "", Sub = "";
    public bool Selected, Faded, Error;
    bool hover;

    public ThumbCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(150, 124);
        Cursor = Cursors.Hand;
        BackColor = Theme.Panel;
        Margin = new Padding(0, 4, 0, 4);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var path = Theme.Round(r, 10))
        {
            using (var b = new SolidBrush(Selected ? Theme.AccentDim : hover ? Theme.CardHover : Theme.Card)) g.FillPath(b, path);
            if (Selected) using (var pen = new Pen(Theme.Accent, 2)) g.DrawPath(pen, path);
        }
        var area = new Rectangle(8, 8, Width - 17, 72);
        using (var b = new SolidBrush(Theme.Bg)) g.FillRectangle(b, area);
        if (Image != null)
        {
            try
            {
                float s = Math.Min((float)area.Width / Image.Width, (float)area.Height / Image.Height);
                float w = Image.Width * s, h = Image.Height * s;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(Image, area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
            }
            catch (ArgumentException) { }
        }
        if (Faded) using (var b = new SolidBrush(Color.FromArgb(170, Theme.Card))) g.FillRectangle(b, area);
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.Left;
        TextRenderer.DrawText(g, Title, Theme.Bold, new Rectangle(8, 84, Width - 16, 18), Error ? Theme.Danger : Theme.Text, flags);
        TextRenderer.DrawText(g, Sub, Theme.Small, new Rectangle(8, 102, Width - 16, 16), Error ? Theme.Danger : Theme.Sub, flags);
    }
}
