using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCVMaster.Core;
using Mat = OpenCvSharp.Mat;
using Vec3b = OpenCvSharp.Vec3b;

namespace OpenCVMaster.UI;

sealed class MainForm : Form
{
    readonly List<Step> steps = [];
    Mat? source;
    Bitmap? sourceBmp;
    string sourceName = "";
    int sel; // selected stage: 0 = original, k = steps[k-1]

    // async pipeline state: only the UI thread touches these
    bool running, syncing;
    int pendingFrom = int.MaxValue;
    readonly List<IDisposable> trash = [];

    // controls
    readonly ComboBox catCombo, opCombo;
    readonly Label opHint, seqTitle, stageTitle, statusInfo, statusPixel, statusTime;
    readonly ListBox stepList;
    readonly FlowLayoutPanel paramHost, strip;
    readonly ImageView inView, outView;
    readonly Panel viewHost;
    readonly CheckBox compareToggle;
    readonly List<ThumbCard> cards = [];
    Label? infoLabel, errorLabel;

    const int SideW = 344, ParamW = 296;

    public MainForm(string? startupFile)
    {
        Text = "OpenCV Master";
        Size = new Size(1560, 940);
        MinimumSize = new Size(1180, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        KeyPreview = true;
        AllowDrop = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        // ── header ───────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Panel, Padding = new Padding(20, 0, 16, 0) };
        header.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Theme.Round(new RectangleF(20, 14, 32, 32), 9))
            using (var b = new LinearGradientBrush(new RectangleF(20, 14, 32, 32), Theme.Accent, Color.FromArgb(56, 189, 248), 45f)) g.FillPath(b, path);
            TextRenderer.DrawText(g, "CV", Theme.Bold, new Rectangle(20, 14, 32, 32), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "OpenCV Master", Theme.Title, new Point(62, 12), Theme.Text);
            TextRenderer.DrawText(g, $"OpenCV {OpenCvSharp.Cv2.GetVersionString()}", Theme.Small, new Point(64, 34), Theme.Sub);
            using var pen = new Pen(Theme.Border);
            g.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        var headerButtons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 13, 0, 0), BackColor = Color.Transparent };
        var openBtn = Theme.Button("이미지 열기", primary: true);
        var sampleBtn = Theme.Button("샘플 이미지");
        var saveBtn = Theme.Button("현재 단계 저장");
        foreach (var b in new[] { sampleBtn, saveBtn, openBtn }) { b.Margin = new Padding(6, 0, 0, 0); headerButtons.Controls.Add(b); }
        header.Controls.Add(headerButtons);
        openBtn.Click += (_, _) => OpenImage();
        sampleBtn.Click += (_, _) => SetSource(ImageIO.CreateSample(), "샘플 이미지");
        saveBtn.Click += (_, _) => SaveStage();

        // ── left: add operation + sequence ───────────────────
        var left = new Panel { Dock = DockStyle.Left, Width = SideW, BackColor = Theme.Panel, Padding = new Padding(18, 16, 18, 14) };
        left.Paint += (_, e) => { using var pen = new Pen(Theme.Border); e.Graphics.DrawLine(pen, left.Width - 1, 0, left.Width - 1, left.Height); };

        catCombo = Theme.Combo();
        catCombo.Items.AddRange(Catalog.Categories.Cast<object>().ToArray());
        opCombo = Theme.Combo(o => ((OpDef)o).Params.Length switch { 0 => "", 1 => "1 param", var n => $"{n} params" });
        // drop-downs show English names; the rest of the UI stays Korean
        catCombo.FormattingEnabled = opCombo.FormattingEnabled = true;
        catCombo.Format += (_, e) => e.Value = Catalog.English((string)e.ListItem!);
        opCombo.Format += (_, e) => e.Value = Catalog.English(((OpDef)e.ListItem!).Name);
        opHint = Theme.Label("", Theme.Small, Theme.Sub, 58);
        opHint.TextAlign = ContentAlignment.TopLeft;
        opHint.AutoEllipsis = true;
        var addBtn = Theme.Button("＋  시퀀스에 추가", primary: true);
        addBtn.Height = 38;
        catCombo.SelectedIndexChanged += (_, _) =>
        {
            opCombo.Items.Clear();
            opCombo.Items.AddRange(Catalog.All.Where(o => o.Category == (string)catCombo.SelectedItem!).Cast<object>().ToArray());
            opCombo.SelectedIndex = 0;
        };
        opCombo.SelectedIndexChanged += (_, _) =>
        {
            if (opCombo.SelectedItem is OpDef op) opHint.Text = $"{op.Api}\n{op.Description}";
        };
        opCombo.DoubleClick += (_, _) => AddStep();
        addBtn.Click += (_, _) => AddStep();

        seqTitle = Theme.Label("처리 시퀀스", Theme.Section, Theme.Sub, 26);
        stepList = new StepListBox
        {
            DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 52, BorderStyle = BorderStyle.None, BackColor = Theme.Panel,
            ForeColor = Theme.Text, IntegralHeight = false, Dock = DockStyle.Fill,
        };
        stepList.DrawItem += DrawStepItem;
        stepList.SelectedIndexChanged += (_, _) => { if (!syncing && stepList.SelectedIndex >= 0) Select(stepList.SelectedIndex + 1); };
        stepList.DoubleClick += (_, _) => ToggleStep();
        stepList.MouseDown += (_, e) =>
        {
            // clicking empty space below the items selects the original image
            if (stepList.IndexFromPoint(e.Location) < 0) Select(0);
        };

        var listButtons = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 40, ColumnCount = 5, Padding = new Padding(0, 6, 0, 0), BackColor = Color.Transparent };
        var tip = new ToolTip();
        (string text, string tipText, Action act)[] lb =
        [
            ("▲", "위로 이동", () => MoveStep(-1)),
            ("▼", "아래로 이동", () => MoveStep(1)),
            ("켜기/끄기", "선택한 단계 사용/건너뛰기 (더블클릭)", ToggleStep),
            ("삭제", "선택한 단계 삭제 (Delete)", DeleteStep),
            ("비우기", "시퀀스 전체 삭제", ClearSteps),
        ];
        foreach (var (i, item) in lb.Select((x, i) => (i, x)))
        {
            listButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, i < 2 ? 14 : 24));
            var b = Theme.Button(item.text);
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(i == 0 ? 0 : 3, 0, 0, 0);
            b.Font = Theme.Small;
            b.Click += (_, _) => item.act();
            tip.SetToolTip(b, item.tipText);
            listButtons.Controls.Add(b, i, 0);
        }

        left.Controls.Add(stepList);
        left.Controls.Add(listButtons);
        Stack(left, Theme.Label("기능 추가", Theme.Section, Theme.Sub, 24), Theme.Label("분야", Theme.Small, Theme.Sub, 20), catCombo, Theme.Spacer(10),
            Theme.Label("기능", Theme.Small, Theme.Sub, 20), opCombo, Theme.Spacer(8), opHint, addBtn, Theme.Spacer(20), seqTitle, Theme.Spacer(2));

        // ── right: parameters ────────────────────────────────
        var right = new Panel { Dock = DockStyle.Right, Width = SideW, BackColor = Theme.Panel, Padding = new Padding(20, 16, 10, 14) };
        right.Paint += (_, e) => { using var pen = new Pen(Theme.Border); e.Graphics.DrawLine(pen, 0, 0, 0, right.Height); };
        paramHost = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Panel };
        right.Controls.Add(paramHost);
        Stack(right, Theme.Label("파라미터", Theme.Section, Theme.Sub, 24), Theme.Spacer(4));

        // ── center: viewer, film strip, status ───────────────
        var center = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        var topBar = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(18, 0, 14, 0) };
        stageTitle = Theme.Label("", Theme.Bold, Theme.Text, 48);
        stageTitle.Dock = DockStyle.Fill;
        var topButtons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        compareToggle = new CheckBox
        {
            Text = "입력 / 출력 비교", Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat, AutoSize = false, Height = 32,
            Width = 130, TextAlign = ContentAlignment.MiddleCenter, BackColor = Theme.Card, ForeColor = Theme.Text, Cursor = Cursors.Hand,
            Checked = true,
        };
        compareToggle.FlatAppearance.BorderSize = 0;
        compareToggle.FlatAppearance.CheckedBackColor = Theme.Accent;
        compareToggle.FlatAppearance.MouseOverBackColor = Theme.CardHover;
        compareToggle.CheckedChanged += (_, _) => { LayoutViews(); RefreshVisuals(); };
        var fitBtn = Theme.Button("화면 맞춤");
        fitBtn.Height = 32;
        fitBtn.Margin = new Padding(6, 0, 0, 0);
        fitBtn.Click += (_, _) => outView!.Fit();
        topButtons.Controls.AddRange([compareToggle, fitBtn]);
        topBar.Controls.Add(stageTitle);
        topBar.Controls.Add(topButtons);

        viewHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 0, 14, 10) };
        inView = new ImageView { EmptyText = "" };
        outView = new ImageView { EmptyText = "이미지를 열거나 이 창으로 끌어다 놓으세요\n\n지원 형식: JPG, PNG, BMP, TIFF, WebP, GIF, HEIC, JP2, EXR, HDR, PNM 등" };
        inView.Partner = outView;
        outView.Partner = inView;
        viewHost.Controls.AddRange([inView, outView]);
        viewHost.Resize += (_, _) => LayoutViews();
        inView.Hover += p => ShowPixel(p, Math.Max(sel - 1, 0));
        outView.Hover += p => ShowPixel(p, sel);

        strip = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 158, WrapContents = false, AutoScroll = true, BackColor = Theme.Panel,
            Padding = new Padding(14, 8, 14, 4),
        };
        strip.Paint += (_, e) => { using var pen = new Pen(Theme.Border); e.Graphics.DrawLine(pen, 0, 0, strip.Width, 0); };

        var status = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 28, ColumnCount = 3, BackColor = Theme.Panel, Padding = new Padding(14, 0, 14, 0) };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        statusInfo = Theme.Label("", Theme.Small, Theme.Sub, 28);
        statusPixel = Theme.Label("", Theme.Mono, Theme.Sub, 28);
        statusTime = Theme.Label("", Theme.Small, Theme.Sub, 28);
        statusTime.TextAlign = ContentAlignment.MiddleRight;
        foreach (var l in new[] { statusInfo, statusPixel, statusTime }) { l.Dock = DockStyle.Fill; status.Controls.Add(l); }

        center.Controls.Add(viewHost);
        center.Controls.Add(topBar);
        center.Controls.Add(strip);
        center.Controls.Add(status);

        Controls.Add(center);
        Controls.Add(right);
        Controls.Add(left);
        Controls.Add(header);

        // drag & drop anywhere
        foreach (var c in new Control[] { this, viewHost, inView, outView, strip })
        {
            c.AllowDrop = true;
            c.DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy; };
            c.DragDrop += (_, e) => { if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } f) LoadFile(f[0]); };
        }

        catCombo.SelectedIndex = 0;
        if (startupFile != null && File.Exists(startupFile)) LoadFile(startupFile);
        else SetSource(ImageIO.CreateSample(), "샘플 이미지");

        // a first sequence so beginners immediately see how stages chain together
        foreach (var name in new[] { "가우시안 블러", "오츠 자동 이진화", "열림 (Opening)" })
            steps.Add(new Step(Catalog.All.First(o => o.Name == name)));
        RebuildSequence();
        Select(1);
        Request(0);
    }

    /// <summary>Docks controls to the top in visual order (WinForms docks the last-added control outermost).</summary>
    static void Stack(Control parent, params Control[] topToBottom)
    {
        for (int i = topToBottom.Length - 1; i >= 0; i--)
        {
            topToBottom[i].Dock = DockStyle.Top;
            parent.Controls.Add(topToBottom[i]);
        }
    }

    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int on = 1;
        try { DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { } // dark title bar on Windows 10 1809+
        foreach (var c in new Control[] { strip, paramHost, stepList }) Theme.DarkNative(c);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        CheckForUpdateOnStartup();
    }

    // Asks once per launch when GitHub has a newer release, then runs its setup and closes.
    async void CheckForUpdateOnStartup()
    {
        var release = await GitHubUpdater.FindNewerReleaseAsync();
        if (release is null || IsDisposed) return;

        var message = $"새 버전 v{release.Version}을 사용할 수 있습니다.\n현재 버전: {GitHubUpdater.CurrentVersion}\n\n지금 다운로드하고 설치할까요?";
        if (MessageBox.Show(this, message, "업데이트 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;

        var originalTitle = Text;
        UseWaitCursor = true;
        try
        {
            await GitHubUpdater.DownloadAndStartSetupAsync(release, new Progress<int>(percent => Text = $"업데이트 다운로드 중... {percent}%"));
            Close();
        }
        catch (Exception ex)
        {
            Text = originalTitle;
            MessageBox.Show(this, $"업데이트 다운로드에 실패했습니다.\n{ex.Message}", "업데이트 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.O: OpenImage(); return true;
            case Keys.Control | Keys.S: SaveStage(); return true;
            case Keys.Delete when stepList.Focused: DeleteStep(); return true;
            case Keys.Alt | Keys.Up: MoveStep(-1); return true;
            case Keys.Alt | Keys.Down: MoveStep(1); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ── image source ─────────────────────────────────────────

    void OpenImage()
    {
        using var dlg = new OpenFileDialog { Filter = ImageIO.OpenFilter, Title = "이미지 열기" };
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadFile(dlg.FileName);
    }

    void LoadFile(string path)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            SetSource(ImageIO.Load(path), Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"이미지를 열 수 없습니다.\n\n{path}\n\n{ex.Message}", "OpenCV Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { Cursor = Cursors.Default; }
    }

    void SetSource(Mat mat, string name)
    {
        var oldMat = source; var oldBmp = sourceBmp;
        source = mat;
        sourceBmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        sourceName = name;
        outView.Fit();
        RefreshVisuals();
        Discard(oldMat, oldBmp);
        Request(0);
    }

    void SaveStage()
    {
        var m = StageMat(sel);
        if (m == null) return;
        using var dlg = new SaveFileDialog
        {
            Filter = ImageIO.SaveFilter, Title = "현재 단계 결과 저장",
            FileName = $"{Path.GetFileNameWithoutExtension(sourceName)}_{(sel == 0 ? "original" : $"step{sel}")}",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { ImageIO.Save(dlg.FileName, m); }
        catch (Exception ex) { MessageBox.Show(this, $"저장하지 못했습니다.\n\n{ex.Message}", "OpenCV Master", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    Mat? StageMat(int i) => i == 0 ? source : i <= steps.Count ? steps[i - 1].Result : null;
    Bitmap? StageBmp(int i) => i == 0 ? sourceBmp : i <= steps.Count ? steps[i - 1].Bitmap : null;
    string StageName(int i) => i == 0 ? $"원본 · {sourceName}" : $"{i}. {steps[i - 1].Op.Name}";

    // ── sequence editing ─────────────────────────────────────

    void AddStep()
    {
        if (opCombo.SelectedItem is not OpDef op) return;
        int at = Math.Min(sel, steps.Count); // insert right after the selected stage
        steps.Insert(at, new Step(op));
        RebuildSequence();
        Select(at + 1);
        Request(at);
    }

    void MoveStep(int dir)
    {
        int i = sel - 1, j = i + dir;
        if (i < 0 || j < 0 || j >= steps.Count) return;
        (steps[i], steps[j]) = (steps[j], steps[i]);
        RebuildSequence();
        Select(j + 1);
        Request(Math.Min(i, j));
    }

    void ToggleStep()
    {
        if (sel == 0) return;
        steps[sel - 1].Enabled ^= true;
        stepList.Invalidate();
        Request(sel - 1);
    }

    void DeleteStep()
    {
        if (sel == 0) return;
        int i = sel - 1;
        var st = steps[i];
        steps.RemoveAt(i);
        RebuildSequence();
        Select(Math.Min(sel, steps.Count));
        Discard(st.Result, st.Bitmap);
        Request(i);
    }

    void ClearSteps()
    {
        if (steps.Count == 0) return;
        if (MessageBox.Show(this, "처리 시퀀스의 모든 단계를 삭제할까요?", "OpenCV Master", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var old = steps.ToList();
        steps.Clear();
        RebuildSequence();
        Select(0);
        foreach (var st in old) Discard(st.Result, st.Bitmap);
    }

    void RebuildSequence()
    {
        syncing = true;
        stepList.BeginUpdate();
        stepList.Items.Clear();
        foreach (var st in steps) stepList.Items.Add(st);
        stepList.EndUpdate();
        syncing = false;
        seqTitle.Text = $"처리 시퀀스  ·  {steps.Count}단계";

        strip.SuspendLayout();
        foreach (Control c in strip.Controls.Cast<Control>().ToList()) c.Dispose();
        strip.Controls.Clear();
        cards.Clear();
        for (int i = 0; i <= steps.Count; i++)
        {
            if (i > 0) strip.Controls.Add(new Label { Text = "›", Font = Theme.Title, ForeColor = Theme.Sub, Width = 22, Height = 124, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(0, 4, 0, 4) });
            int stage = i;
            var card = new ThumbCard();
            card.Click += (_, _) => Select(stage);
            strip.Controls.Add(card);
            cards.Add(card);
        }
        strip.ResumeLayout();
    }

    void Select(int stage)
    {
        sel = Math.Clamp(stage, 0, steps.Count);
        syncing = true;
        stepList.SelectedIndex = sel - 1;
        syncing = false;
        if (sel - 1 < cards.Count && sel < cards.Count) strip.ScrollControlIntoView(cards[sel]);
        BuildParams();
        LayoutViews();
        RefreshVisuals();
    }

    // ── processing ───────────────────────────────────────────

    /// <summary>Marks stages from <paramref name="from"/> as dirty; work is coalesced so slider drags stay responsive.</summary>
    void Request(int from)
    {
        pendingFrom = Math.Min(pendingFrom, Math.Max(from, 0));
        if (!running) _ = RunLoop();
    }

    async Task RunLoop()
    {
        running = true;
        try
        {
            while (pendingFrom != int.MaxValue && source != null)
            {
                int from = Math.Min(pendingFrom, steps.Count);
                pendingFrom = int.MaxValue;
                if (from >= steps.Count) break;
                var input = from == 0 ? source : steps[from - 1].Result;
                if (input == null) { pendingFrom = 0; continue; }

                var owners = steps.Skip(from).ToArray();
                var snapshot = steps.Select(s => (s.Op, (double[])s.Values.Clone(), s.Enabled)).ToArray();
                var original = source;
                statusTime.Text = "처리 중…";
                var results = await Task.Run(() => Pipeline.Run(snapshot, from, input, original));

                var old = new List<IDisposable?>();
                for (int k = 0; k < results.Count; k++)
                {
                    var r = results[k];
                    int idx = from + k;
                    if (idx >= steps.Count || steps[idx] != owners[k]) { r.Mat.Dispose(); r.Bitmap.Dispose(); continue; } // sequence changed meanwhile
                    var st = steps[idx];
                    old.Add(st.Result); old.Add(st.Bitmap);
                    (st.Result, st.Bitmap, st.Error, st.Info, st.Ms) = (r.Mat, r.Bitmap, r.Error, r.Info, r.Ms);
                }
                RefreshVisuals();
                foreach (var d in old) d?.Dispose();
            }
        }
        finally
        {
            running = false;
            foreach (var d in trash) d.Dispose();
            trash.Clear();
            UpdateTimeStatus();
        }
    }

    /// <summary>Disposes now, or after the background run when it might still be reading these.</summary>
    void Discard(params IDisposable?[] items)
    {
        foreach (var d in items.OfType<IDisposable>())
            if (running) trash.Add(d); else d.Dispose();
    }

    // ── view refresh ─────────────────────────────────────────

    void LayoutViews()
    {
        var r = viewHost.DisplayRectangle;
        bool compare = compareToggle.Checked && sel > 0;
        inView.Visible = compare;
        if (compare)
        {
            int w = (r.Width - 10) / 2;
            inView.Bounds = new Rectangle(r.X, r.Y, w, r.Height);
            outView.Bounds = new Rectangle(r.X + w + 10, r.Y, r.Width - w - 10, r.Height);
        }
        else outView.Bounds = r;
    }

    void RefreshVisuals()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            c.Image = StageBmp(i);
            c.Selected = i == sel;
            if (i == 0) { c.Title = "원본"; c.Sub = source == null ? "" : $"{source.Width}×{source.Height} · {source.Channels()}ch"; c.Faded = false; c.Error = false; }
            else
            {
                var st = steps[i - 1];
                c.Title = $"{i}. {st.Op.Name}";
                c.Sub = st.Error != null ? "오류 - 입력 그대로 전달" : !st.Enabled ? "꺼짐 (건너뜀)" : st.Result == null ? "처리 중…" : st.Info ?? $"{st.Ms:0.0} ms";
                c.Faded = !st.Enabled;
                c.Error = st.Error != null;
            }
            c.Invalidate();
        }

        outView.Image = StageBmp(sel);
        outView.Caption = sel == 0 ? "원본" : $"출력 · {StageName(sel)}";
        inView.Image = sel > 0 ? StageBmp(sel - 1) : null;
        inView.Caption = sel > 0 ? $"입력 · {StageName(sel - 1)}" : "";
        stageTitle.Text = sel == 0 ? StageName(0) : $"{StageName(sel)}   —   {steps[sel - 1].Op.Category}";
        stepList.Invalidate();

        var m = StageMat(sel);
        statusInfo.Text = m == null ? "" : $"{m.Width} × {m.Height}  ·  {m.Channels()} 채널  ·  8bit";
        if (sel > 0)
        {
            var st = steps[sel - 1];
            if (infoLabel != null) { infoLabel.Text = st.Info ?? ""; infoLabel.Visible = st.Info != null; }
            if (errorLabel != null) { errorLabel.Text = st.Error == null ? "" : $"처리 오류: {st.Error}\n파라미터를 조정해 보세요. 이 단계는 입력을 그대로 다음 단계로 전달합니다."; errorLabel.Visible = st.Error != null; }
        }
        UpdateTimeStatus();
    }

    void UpdateTimeStatus()
    {
        if (running) return;
        double total = steps.Where(s => s.Enabled).Sum(s => s.Ms);
        statusTime.Text = steps.Count == 0 ? "" : sel > 0 ? $"이 단계 {steps[sel - 1].Ms:0.0} ms  ·  전체 {total:0.0} ms" : $"전체 {total:0.0} ms";
    }

    void ShowPixel(Point? p, int stage)
    {
        var m = StageMat(stage);
        if (p is not { } pt || m == null || pt.X >= m.Width || pt.Y >= m.Height) { statusPixel.Text = ""; return; }
        string v;
        if (m.Channels() == 1) v = $"값 {m.At<byte>(pt.Y, pt.X),3}";
        else { var c = m.At<Vec3b>(pt.Y, pt.X); v = $"R {c.Item2,3}  G {c.Item1,3}  B {c.Item0,3}"; }
        statusPixel.Text = $"X {pt.X,4}  Y {pt.Y,4}   {v}";
    }

    void DrawStepItem(object? sender, DrawItemEventArgs e)
    {
        // draw off-screen and blit once, so repaints during parameter drags don't flicker
        using var buf = BufferedGraphicsManager.Current.Allocate(e.Graphics, e.Bounds);
        var g = buf.Graphics;
        using (var bg = new SolidBrush(Theme.Panel)) g.FillRectangle(bg, e.Bounds);
        if (e.Index >= 0 && e.Index < steps.Count) DrawStep(g, e.Bounds, e.Index);
        buf.Render(e.Graphics);
    }

    void DrawStep(Graphics g, Rectangle bounds, int index)
    {
        var st = steps[index];
        bool selected = index == sel - 1;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(bounds.X + 1, bounds.Y + 3, bounds.Width - 3, bounds.Height - 6);
        using (var path = Theme.Round(r, 9))
        {
            using (var b = new SolidBrush(selected ? Theme.AccentDim : Theme.Card)) g.FillPath(b, path);
            if (selected) using (var pen = new Pen(Theme.Accent, 1.5f)) g.DrawPath(pen, path);
        }
        var badge = new RectangleF(r.X + 10, r.Y + (r.Height - 26) / 2, 26, 26);
        var badgeColor = st.Error != null ? Theme.Danger : st.Enabled ? Theme.Accent : Theme.Border;
        using (var b = new SolidBrush(badgeColor)) g.FillEllipse(b, badge);
        // center the digit glyphs themselves (font line boxes include padding that pushes them off-center)
        using (var num = new GraphicsPath())
        using (var white = new SolidBrush(Color.White))
        {
            num.AddString((index + 1).ToString(), Theme.Bold.FontFamily, (int)FontStyle.Regular, 13f, PointF.Empty, StringFormat.GenericTypographic);
            var nb = num.GetBounds();
            using var m = new Matrix();
            m.Translate(badge.X + (badge.Width - nb.Width) / 2 - nb.X, badge.Y + (badge.Height - nb.Height) / 2 - nb.Y);
            num.Transform(m);
            g.FillPath(white, num);
        }

        // the off-screen buffer is translated; TextRenderer ignores that unless told to keep it
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsTranslateTransform;
        int tx = (int)r.X + 46, tw = (int)r.Width - 56;
        TextRenderer.DrawText(g, st.Op.Name, Theme.Bold, new Rectangle(tx, (int)r.Y + 5, tw, 20), st.Enabled ? Theme.Text : Theme.Sub, flags);
        string sub = st.Error != null ? "오류 발생" : !st.Enabled ? "꺼짐 · 건너뜀" : st.Result == null ? st.Op.Category : $"{st.Op.Category} · {st.Ms:0.0} ms";
        TextRenderer.DrawText(g, sub, Theme.Small, new Rectangle(tx, (int)r.Y + 24, tw, 18), st.Error != null ? Theme.Danger : Theme.Sub, flags);
    }

    // ── parameter panel ──────────────────────────────────────

    void BuildParams()
    {
        paramHost.SuspendLayout();
        foreach (Control c in paramHost.Controls.Cast<Control>().ToList()) c.Dispose();
        paramHost.Controls.Clear();
        infoLabel = errorLabel = null;

        if (sel == 0)
        {
            AddText("원본 이미지", Theme.Title, Theme.Text);
            AddText(source == null ? "" : $"{sourceName}\n{source.Width} × {source.Height} · {source.Channels()} 채널", Theme.Small, Theme.Sub);
            AddText("사용 방법\n\n1. 왼쪽에서 분야와 기능을 고른 뒤 [시퀀스에 추가]를 누릅니다. 선택한 단계 바로 뒤에 추가됩니다.\n\n" +
                    "2. 시퀀스의 단계를 클릭하면 이 패널에서 파라미터를 조정할 수 있고, 결과가 즉시 다시 계산됩니다.\n\n" +
                    "3. 아래 필름 스트립에서 모든 단계의 중간 결과를 한눈에 비교하세요.\n\n" +
                    "4. 화면에서 휠로 확대, 드래그로 이동, 더블클릭으로 화면 맞춤. 상태 표시줄에 픽셀 값이 표시됩니다.\n\n" +
                    "단축키: Ctrl+O 열기 · Ctrl+S 저장 · Delete 단계 삭제 · Alt+↑↓ 순서 이동", Theme.Body, Theme.Sub);
            paramHost.ResumeLayout();
            return;
        }

        var st = steps[sel - 1];
        AddText(st.Op.Name, Theme.Title, Theme.Text);
        AddText(st.Op.Api, Theme.Mono, Theme.AccentHover);
        AddText(st.Op.Description, Theme.Small, Theme.Sub).Margin = new Padding(0, 2, 0, 14);

        infoLabel = AddBox(Theme.Success, Color.FromArgb(22, 52, 44));
        errorLabel = AddBox(Theme.Danger, Color.FromArgb(60, 28, 32));

        if (st.Op.Params.Length == 0) AddText("이 기능은 조정할 파라미터가 없습니다.", Theme.Small, Theme.Sub);
        for (int i = 0; i < st.Op.Params.Length; i++) paramHost.Controls.Add(ParamRow(st, i));

        if (st.Op.Params.Length > 0)
        {
            var reset = Theme.Button("기본값으로 되돌리기");
            reset.Width = ParamW;
            reset.Margin = new Padding(0, 10, 0, 0);
            reset.Click += (_, _) =>
            {
                for (int i = 0; i < st.Values.Length; i++) st.Values[i] = st.Op.Params[i].Default;
                BuildParams();
                Request(steps.IndexOf(st));
            };
            paramHost.Controls.Add(reset);
        }
        paramHost.ResumeLayout();
        RefreshVisuals();
    }

    Label AddText(string text, Font font, Color color)
    {
        var l = new Label { Text = text, Font = font, ForeColor = color, AutoSize = true, MaximumSize = new Size(ParamW, 0), Margin = new Padding(0, 0, 0, 6) };
        paramHost.Controls.Add(l);
        return l;
    }

    Label AddBox(Color fore, Color back)
    {
        var l = new Label
        {
            AutoSize = true, MaximumSize = new Size(ParamW, 0), MinimumSize = new Size(ParamW, 0), Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 12), ForeColor = fore, BackColor = back, Font = Theme.Small, Visible = false,
        };
        paramHost.Controls.Add(l);
        return l;
    }

    Control ParamRow(Step st, int i)
    {
        var prm = st.Op.Params[i];
        var row = new Panel { Width = ParamW, Margin = new Padding(0, 0, 0, 10), BackColor = Color.Transparent };
        void Commit(double v)
        {
            if (st.Values[i] == v) return;
            st.Values[i] = v;
            Request(steps.IndexOf(st));
        }

        switch (prm.Kind)
        {
            case ParamKind.Bool:
            {
                row.Height = 30;
                var cb = new CheckBox
                {
                    Text = prm.Label, Checked = st.Values[i] != 0, AutoSize = false, Width = ParamW, Height = 28, ForeColor = Theme.Text,
                    FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                };
                cb.FlatAppearance.BorderColor = Theme.Border;
                cb.FlatAppearance.CheckedBackColor = Theme.Accent;
                cb.CheckedChanged += (_, _) => Commit(cb.Checked ? 1 : 0);
                row.Controls.Add(cb);
                break;
            }
            case ParamKind.Choice:
            {
                row.Height = 54;
                var lbl = Theme.Label(prm.Label);
                lbl.SetBounds(0, 0, ParamW, 22);
                var combo = Theme.Combo();
                combo.Items.AddRange(prm.Options);
                combo.SelectedIndex = (int)st.Values[i];
                combo.SetBounds(0, 24, ParamW, 28);
                combo.SelectedIndexChanged += (_, _) => Commit(combo.SelectedIndex);
                row.Controls.AddRange([lbl, combo]);
                break;
            }
            default:
            {
                row.Height = 58;
                var lbl = Theme.Label(prm.Label);
                lbl.SetBounds(0, 2, ParamW - 96, 22);
                var num = new NumericUpDown
                {
                    Minimum = (decimal)prm.Min, Maximum = (decimal)prm.Max, DecimalPlaces = prm.Decimals, Increment = (decimal)prm.Step,
                    Value = (decimal)st.Values[i], BackColor = Theme.Card, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = HorizontalAlignment.Right, Font = Theme.Body,
                };
                num.SetBounds(ParamW - 90, 0, 90, 26);
                var slider = new FlatSlider();
                slider.Setup(prm.Min, prm.Max, prm.Step, st.Values[i]);
                slider.SetBounds(-4, 30, ParamW + 8, 26);
                bool busy = false;
                void Set(double v)
                {
                    if (busy) return;
                    v = prm.Clamp(v);
                    busy = true;
                    slider.Value = v;
                    num.Value = Math.Clamp((decimal)v, num.Minimum, num.Maximum);
                    busy = false;
                    Commit(v);
                }
                slider.ValueChanged += (_, _) => Set(slider.Value);
                num.ValueChanged += (_, _) => Set((double)num.Value);
                new ToolTip().SetToolTip(lbl, $"범위: {Fmt(prm, prm.Min)} ~ {Fmt(prm, prm.Max)}");
                row.Controls.AddRange([lbl, num, slider]);
                break;
            }
        }
        return row;
    }

    static string Fmt(Param p, double v) => v.ToString(p.Decimals == 0 ? "0" : "0." + new string('0', p.Decimals));

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        source?.Dispose();
        sourceBmp?.Dispose();
        foreach (var st in steps) { st.Result?.Dispose(); st.Bitmap?.Dispose(); }
    }
}
