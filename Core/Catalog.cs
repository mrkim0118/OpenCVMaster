using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.XImgProc;

namespace OpenCVMaster.Core;

/// <summary>Every operation the app offers, grouped by category. Each op receives an 8-bit 1/3-channel Mat.</summary>
public static class Catalog
{
    public static readonly List<OpDef> All = [];
    public static IEnumerable<string> Categories => All.Select(o => o.Category).Distinct();

    const string Color = "색상 변환", Blur = "필터 · 블러", Thr = "이진화", Morph = "모폴로지", Edge = "에지 검출",
        Geo = "기하 변환", Hist = "히스토그램", Feat = "특징점 검출", Shape = "윤곽선 · 형태 분석",
        Seg = "영상 분할", Noise = "노이즈 · 복원 · 주파수", Detect = "객체 검출", Mix = "원본과 합성";

    /// <summary>English labels shown in the category / function drop-downs. Names already in English need no entry.</summary>
    static readonly Dictionary<string, string> EnNames = new()
    {
        [Color] = "Color", [Blur] = "Filter / Blur", [Thr] = "Threshold", [Morph] = "Morphology", [Edge] = "Edge Detection",
        [Geo] = "Geometric Transform", [Hist] = "Histogram", [Feat] = "Feature Detection", [Shape] = "Contours / Shape Analysis",
        [Seg] = "Segmentation", [Noise] = "Noise / Restoration / Frequency", [Detect] = "Object Detection", [Mix] = "Compose with Original",

        ["그레이스케일"] = "Grayscale", ["색 공간 변환"] = "Color Space Conversion", ["채널 분리"] = "Channel Split",
        ["색상 반전"] = "Invert Colors", ["HSV 색상 범위 추출"] = "HSV Color Range", ["밝기 / 대비"] = "Brightness / Contrast",
        ["감마 보정"] = "Gamma Correction", ["컬러맵 적용"] = "Apply Colormap", ["그레이월드 화이트밸런스"] = "Gray World White Balance",

        ["평균 블러"] = "Average Blur", ["가우시안 블러"] = "Gaussian Blur", ["미디언 블러"] = "Median Blur",
        ["양방향 필터"] = "Bilateral Filter", ["박스 필터"] = "Box Filter", ["언샤프 마스크 (샤프닝)"] = "Unsharp Mask (Sharpen)",
        ["커널 필터 (Filter2D)"] = "Kernel Filter (Filter2D)", ["가버 필터"] = "Gabor Filter", ["에지 보존 필터"] = "Edge Preserving Filter",
        ["디테일 강화"] = "Detail Enhance", ["스타일화 (수채화)"] = "Stylization", ["연필 스케치"] = "Pencil Sketch",

        ["임계값 이진화"] = "Threshold", ["오츠 자동 이진화"] = "Otsu Threshold", ["트라이앵글 자동 이진화"] = "Triangle Threshold",
        ["적응형 이진화"] = "Adaptive Threshold", ["범위 이진화 (InRange)"] = "Range Threshold (InRange)",

        ["침식 (Erode)"] = "Erode", ["팽창 (Dilate)"] = "Dilate", ["열림 (Opening)"] = "Opening", ["닫힘 (Closing)"] = "Closing",
        ["모폴로지 그래디언트"] = "Morphological Gradient", ["탑햇 (Top Hat)"] = "Top Hat", ["블랙햇 (Black Hat)"] = "Black Hat",
        ["히트 미스 (Hit-or-Miss)"] = "Hit-or-Miss", ["세선화 (Thinning)"] = "Thinning",

        ["캐니 에지"] = "Canny Edge", ["소벨 (Sobel)"] = "Sobel", ["샤르 (Scharr)"] = "Scharr", ["라플라시안"] = "Laplacian",

        ["크기 조절"] = "Resize", ["회전"] = "Rotate", ["반전 (Flip)"] = "Flip", ["90도 단위 회전"] = "Rotate 90°",
        ["평행 이동"] = "Translate", ["자르기 (ROI)"] = "Crop (ROI)", ["원근 변환"] = "Perspective Transform",
        ["영상 피라미드"] = "Image Pyramid", ["극좌표 변환"] = "Polar Warp",

        ["히스토그램 평활화"] = "Histogram Equalization", ["CLAHE (적응형 평활화)"] = "CLAHE",
        ["정규화 (대비 늘리기)"] = "Normalize (Contrast Stretch)", ["히스토그램 그래프"] = "Histogram Plot",

        ["해리스 코너"] = "Harris Corner", ["Shi-Tomasi 코너"] = "Shi-Tomasi Corner", ["MSER 영역"] = "MSER Regions",

        ["윤곽선 검출"] = "Find Contours", ["외접 도형 / 근사"] = "Bounding Shapes / Approximation",
        ["연결 요소 라벨링"] = "Connected Components", ["허프 직선 (확률적)"] = "Hough Lines (Probabilistic)",
        ["허프 직선 (표준)"] = "Hough Lines (Standard)", ["허프 원"] = "Hough Circles", ["거리 변환"] = "Distance Transform",
        ["모멘트 / 중심 계산"] = "Moments / Centroid",

        ["워터셰드 (자동 마커)"] = "Watershed (Auto Markers)", ["그랩컷 (GrabCut)"] = "GrabCut",
        ["K-평균 색상 군집화"] = "K-Means Color Clustering", ["평균 이동 필터"] = "Mean Shift Filter", ["슈퍼픽셀 (SLIC)"] = "Superpixels (SLIC)",

        ["가우시안 노이즈 추가"] = "Add Gaussian Noise", ["소금-후추 노이즈 추가"] = "Add Salt & Pepper Noise",
        ["비지역 평균 디노이즈"] = "Non-Local Means Denoise", ["인페인팅 (밝은 영역 복원)"] = "Inpainting (Bright Areas)",
        ["푸리에 스펙트럼"] = "Fourier Spectrum", ["주파수 필터 (저역/고역)"] = "Frequency Filter (Low / High Pass)",

        ["QR 코드 검출 · 해독"] = "QR Code Detect & Decode", ["HOG 보행자 검출"] = "HOG People Detection",
        ["템플릿 매칭 (중앙 패치)"] = "Template Matching (Center Patch)",

        ["원본과 블렌딩"] = "Blend with Original", ["원본과의 차이"] = "Difference from Original",
        ["마스크로 원본 추출"] = "Extract Original by Mask", ["원본과 비트 연산"] = "Bitwise with Original", ["마스크 오버레이"] = "Mask Overlay",
    };

    public static string English(string name) => EnNames.TryGetValue(name, out var en) ? en : name;

    static void Add(string cat, string name, string api, string desc, Param[] ps, Func<Mat, Args, Mat> run) =>
        All.Add(new OpDef(cat, name, api, desc, ps, run));

    static Param[] P(params Param[] p) => p;

    #region helpers
    static Mat Out(Action<Mat> f) { var d = new Mat(); f(d); return d; }

    static Mat Gray(Mat s)
    {
        var d = new Mat();
        if (s.Channels() == 1) s.CopyTo(d);
        else Cv2.CvtColor(s, d, s.Channels() == 4 ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY);
        return d;
    }

    static Mat Bgr(Mat s)
    {
        var d = new Mat();
        if (s.Channels() == 3) s.CopyTo(d);
        else Cv2.CvtColor(s, d, s.Channels() == 4 ? ColorConversionCodes.BGRA2BGR : ColorConversionCodes.GRAY2BGR);
        return d;
    }

    static bool IsBinary(Mat gray)
    {
        using var t = new Mat();
        Cv2.InRange(gray, new Scalar(1), new Scalar(254), t);
        return Cv2.CountNonZero(t) == 0;
    }

    /// <summary>Binary input for contour-type ops: Otsu is applied automatically when the input is not binary yet.</summary>
    static Mat Binary(Mat s)
    {
        var g = Gray(s);
        if (!IsBinary(g)) Cv2.Threshold(g, g, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        return g;
    }

    /// <summary>Edge input for Hough ops: Canny is applied automatically when the input is not binary yet.</summary>
    static Mat Edges(Mat s)
    {
        var g = Gray(s);
        if (IsBinary(g)) return g;
        var e = new Mat();
        Cv2.Canny(g, e, 50, 150);
        g.Dispose();
        return e;
    }

    /// <summary>Where to draw overlays: on the original image (when the size matches) or on the current input.</summary>
    static Mat Canvas(Mat s, Args p, bool onOriginal) =>
        onOriginal && p.Original.Size() == s.Size() ? Bgr(p.Original) : Bgr(s);

    static Mat SameAsInput(Mat original, Mat s)
    {
        var o = Bgr(original);
        if (o.Size() != s.Size()) Cv2.Resize(o, o, s.Size());
        return o;
    }

    static Mat Kernel(int shape, int size) =>
        Cv2.GetStructuringElement((MorphShapes)shape, new Size(size, size));

    static Mat Lut(Func<int, double> f)
    {
        var m = new Mat(1, 256, MatType.CV_8UC1);
        var a = Enumerable.Range(0, 256).Select(i => (byte)Math.Clamp(Math.Round(f(i)), 0, 255)).ToArray();
        Marshal.Copy(a, 0, m.Data, 256);
        return m;
    }

    static Mat Abs(Mat m) => Out(d => Cv2.ConvertScaleAbs(m, d));

    static Point Pt(Point2f p) => new((int)Math.Round(p.X), (int)Math.Round(p.Y));

    static readonly Scalar[] Palette =
    [
        new(80, 220, 120), new(255, 170, 60), new(60, 200, 255), new(240, 110, 220), new(90, 110, 255),
        new(255, 230, 90), new(160, 255, 200), new(200, 140, 255), new(70, 160, 255), new(255, 255, 255),
    ];
    static Scalar Col(int i) => Palette[i % Palette.Length];

    static readonly string[] Shapes = ["사각형 (Rect)", "십자 (Cross)", "타원 (Ellipse)"];
    static readonly string[] Dir3 = ["X 방향", "Y 방향", "크기 (X+Y)"];

    /// <summary>Swaps quadrants so the zero frequency sits in the center (needs even size).</summary>
    static void Shift(Mat m)
    {
        int cx = m.Cols / 2, cy = m.Rows / 2;
        using var q0 = new Mat(m, new Rect(0, 0, cx, cy));
        using var q1 = new Mat(m, new Rect(cx, 0, cx, cy));
        using var q2 = new Mat(m, new Rect(0, cy, cx, cy));
        using var q3 = new Mat(m, new Rect(cx, cy, cx, cy));
        using var t = new Mat();
        q0.CopyTo(t); q3.CopyTo(q0); t.CopyTo(q3);
        q1.CopyTo(t); q2.CopyTo(q1); t.CopyTo(q2);
    }

    static Mat EvenGrayF(Mat s)
    {
        using var g = Gray(s);
        using var roi = new Mat(g, new Rect(0, 0, g.Cols & ~1, g.Rows & ~1));
        var f = new Mat();
        roi.ConvertTo(f, MatType.CV_32F);
        return f;
    }

    static Mat DrawKeypoints(Mat s, KeyPoint[] kps, bool rich, Args p)
    {
        using var b = Bgr(s);
        p.Info = $"검출된 특징점: {kps.Length}개";
        return Out(d => Cv2.DrawKeypoints(b, kps, d, new Scalar(80, 220, 120),
            rich ? DrawMatchesFlags.DrawRichKeypoints : DrawMatchesFlags.Default));
    }
    #endregion

    static Catalog()
    {
        AddColor();
        AddBlur();
        AddThreshold();
        AddMorphology();
        AddEdge();
        AddGeometry();
        AddHistogram();
        AddFeatures();
        AddShape();
        AddSegmentation();
        AddNoise();
        AddDetection();
        AddMix();
    }

    static void AddColor()
    {
        Add(Color, "그레이스케일", "Cv2.CvtColor(BGR2GRAY)", "컬러 영상을 밝기만 남긴 흑백 영상으로 변환합니다. 대부분의 처리(이진화, 에지, 특징점)의 출발점입니다.",
            P(), (s, p) => Gray(s));

        var codes = new[] { ColorConversionCodes.BGR2HSV, ColorConversionCodes.BGR2HLS, ColorConversionCodes.BGR2Lab, ColorConversionCodes.BGR2Luv, ColorConversionCodes.BGR2YCrCb, ColorConversionCodes.BGR2XYZ, ColorConversionCodes.BGR2YUV };
        Add(Color, "색 공간 변환", "Cv2.CvtColor", "BGR 영상을 다른 색 공간으로 변환합니다. 특정 채널만 골라 보면 각 성분(예: HSV의 H=색상)이 어떻게 분포하는지 확인할 수 있습니다.",
            P(Param.Choice("색 공간", 0, "HSV", "HLS", "Lab", "Luv", "YCrCb", "XYZ", "YUV"),
              Param.Choice("표시 채널", 0, "전체 (3채널)", "채널 1", "채널 2", "채널 3")),
            (s, p) =>
            {
                using var b = Bgr(s);
                var c = Out(d => Cv2.CvtColor(b, d, codes[p.I(0)]));
                if (p.I(1) == 0) return c;
                using (c) return Out(d => Cv2.ExtractChannel(c, d, p.I(1) - 1));
            });

        Add(Color, "채널 분리", "Cv2.Split / Cv2.ExtractChannel", "B, G, R 채널 중 하나만 꺼내 흑백 영상으로 봅니다.",
            P(Param.Choice("채널", 2, "Blue", "Green", "Red")),
            (s, p) => { using var b = Bgr(s); return Out(d => Cv2.ExtractChannel(b, d, p.I(0))); });

        Add(Color, "색상 반전", "Cv2.BitwiseNot", "모든 픽셀 값을 255 - 값으로 뒤집습니다 (네거티브).",
            P(), (s, p) => Out(d => Cv2.BitwiseNot(s, d)));

        Add(Color, "HSV 색상 범위 추출", "Cv2.InRange", "HSV 범위 안에 있는 색만 골라냅니다. H 최소값이 최대값보다 크면 빨간색처럼 0도를 넘나드는 범위로 처리합니다.",
            P(Param.Int("H 최소 (색상)", 0, 179, 35), Param.Int("H 최대", 0, 179, 85),
              Param.Int("S 최소 (채도)", 0, 255, 50), Param.Int("S 최대", 0, 255, 255),
              Param.Int("V 최소 (명도)", 0, 255, 50), Param.Int("V 최대", 0, 255, 255),
              Param.Choice("출력", 1, "마스크", "추출된 색상")),
            (s, p) =>
            {
                using var b = Bgr(s);
                using var hsv = Out(d => Cv2.CvtColor(b, d, ColorConversionCodes.BGR2HSV));
                var mask = new Mat();
                if (p.I(0) <= p.I(1))
                    Cv2.InRange(hsv, new Scalar(p.I(0), p.I(2), p.I(4)), new Scalar(p.I(1), p.I(3), p.I(5)), mask);
                else
                {
                    using var m2 = new Mat();
                    Cv2.InRange(hsv, new Scalar(p.I(0), p.I(2), p.I(4)), new Scalar(179, p.I(3), p.I(5)), mask);
                    Cv2.InRange(hsv, new Scalar(0, p.I(2), p.I(4)), new Scalar(p.I(1), p.I(3), p.I(5)), m2);
                    Cv2.BitwiseOr(mask, m2, mask);
                }
                p.Info = $"선택된 픽셀: {Cv2.CountNonZero(mask) * 100.0 / mask.Total():0.0}%";
                if (p.I(6) == 0) return mask;
                using (mask)
                {
                    var d = new Mat(b.Size(), b.Type(), Scalar.All(0));
                    b.CopyTo(d, mask);
                    return d;
                }
            });

        Add(Color, "밝기 / 대비", "Mat.ConvertTo(alpha, beta)", "출력 = 입력 × 대비(alpha) + 밝기(beta). 255를 넘는 값은 잘립니다(saturate).",
            P(Param.Dbl("대비 (alpha)", 0, 3, 1.2, 0.05), Param.Int("밝기 (beta)", -150, 150, 20)),
            (s, p) => Out(d => s.ConvertTo(d, s.Type(), p.D(0), p.D(1))));

        Add(Color, "감마 보정", "Cv2.LUT", "룩업 테이블로 비선형 밝기 보정을 합니다. 감마 < 1 이면 어두운 영역이 밝아지고, > 1 이면 어두워집니다.",
            P(Param.Dbl("감마", 0.1, 5, 0.6, 0.05)),
            (s, p) => { using var lut = Lut(i => 255 * Math.Pow(i / 255.0, p.D(0))); return Out(d => Cv2.LUT(s, lut, d)); });

        Add(Color, "컬러맵 적용", "Cv2.ApplyColorMap", "흑백 밝기 값을 의사 색상(Jet, Viridis 등)으로 표현합니다. 열화상·깊이 영상 시각화에 쓰입니다.",
            P(Param.Enum("컬러맵", ColormapTypes.Jet)),
            (s, p) => { using var g = Gray(s); return Out(d => Cv2.ApplyColorMap(g, d, p.E<ColormapTypes>(0))); });

        Add(Color, "그레이월드 화이트밸런스", "Cv2.Mean / Cv2.Multiply", "세 채널의 평균이 같아지도록 각 채널을 스케일링해 색 틀어짐을 보정합니다.",
            P(Param.Dbl("강도", 0, 1, 1, 0.05)),
            (s, p) =>
            {
                using var b = Bgr(s);
                var mean = Cv2.Mean(b);
                double avg = (mean.Val0 + mean.Val1 + mean.Val2) / 3, k = p.D(0);
                var gain = new Scalar(1 + k * (avg / Math.Max(mean.Val0, 1) - 1), 1 + k * (avg / Math.Max(mean.Val1, 1) - 1), 1 + k * (avg / Math.Max(mean.Val2, 1) - 1));
                return Out(d => Cv2.Multiply(b, gain, d));
            });
    }

    static void AddBlur()
    {
        Add(Blur, "평균 블러", "Cv2.Blur", "커널 영역의 단순 평균으로 영상을 부드럽게 합니다.",
            P(Param.Int("커널 크기", 1, 51, 5)),
            (s, p) => Out(d => Cv2.Blur(s, d, new Size(p.I(0), p.I(0)))));

        Add(Blur, "가우시안 블러", "Cv2.GaussianBlur", "가우시안 가중치로 블러링합니다. 노이즈 제거와 이진화·에지 검출 전처리에 가장 많이 쓰입니다. Sigma가 0이면 커널 크기에서 자동 계산됩니다.",
            P(Param.Int("커널 크기", 1, 51, 5, odd: true), Param.Dbl("Sigma X", 0, 20, 0, 0.1), Param.Dbl("Sigma Y (0=X와 동일)", 0, 20, 0, 0.1)),
            (s, p) => Out(d => Cv2.GaussianBlur(s, d, new Size(p.I(0), p.I(0)), p.D(1), p.D(2))));

        Add(Blur, "미디언 블러", "Cv2.MedianBlur", "커널 영역의 중간값으로 바꿉니다. 소금-후추 노이즈 제거에 특히 강하고 에지를 잘 보존합니다.",
            P(Param.Int("커널 크기", 1, 31, 5, odd: true)),
            (s, p) => Out(d => Cv2.MedianBlur(s, d, p.I(0))));

        Add(Blur, "양방향 필터", "Cv2.BilateralFilter", "공간 거리와 색 차이를 함께 고려해 에지를 보존하면서 평탄한 영역만 부드럽게 합니다.",
            P(Param.Int("지름 (d)", 1, 25, 9), Param.Dbl("Sigma Color", 1, 250, 75, 1), Param.Dbl("Sigma Space", 1, 250, 75, 1)),
            (s, p) => Out(d => Cv2.BilateralFilter(s, d, p.I(0), p.D(1), p.D(2))));

        Add(Blur, "박스 필터", "Cv2.BoxFilter", "커널 영역의 합(또는 평균)을 계산합니다. 정규화를 끄면 합계가 되어 쉽게 포화됩니다.",
            P(Param.Int("커널 크기", 1, 51, 5), Param.Bool("정규화 (평균)", true)),
            (s, p) => Out(d => Cv2.BoxFilter(s, d, MatType.CV_8U, new Size(p.I(0), p.I(0)), normalize: p.B(1))));

        Add(Blur, "언샤프 마스크 (샤프닝)", "Cv2.GaussianBlur + Cv2.AddWeighted", "원본에서 블러 영상을 빼 경계를 강조합니다. 출력 = 원본 × (1+양) − 블러 × 양",
            P(Param.Dbl("블러 Sigma", 0.5, 10, 2, 0.1), Param.Dbl("강도", 0, 5, 1.5, 0.05)),
            (s, p) =>
            {
                using var bl = Out(d => Cv2.GaussianBlur(s, d, new Size(0, 0), p.D(0)));
                return Out(d => Cv2.AddWeighted(s, 1 + p.D(1), bl, -p.D(1), 0, d));
            });

        var kernels = new (float[] k, double delta)[]
        {
            ([0, -1, 0, -1, 5, -1, 0, -1, 0], 0),
            ([-2, -1, 0, -1, 1, 1, 0, 1, 2], 0),
            ([-1, -1, -1, -1, 8, -1, -1, -1, -1], 0),
            ([1, 0, -1, 1, 0, -1, 1, 0, -1], 128),
            ([1 / 3f, 0, 0, 0, 1 / 3f, 0, 0, 0, 1 / 3f], 0),
        };
        Add(Blur, "커널 필터 (Filter2D)", "Cv2.Filter2D", "사용자 정의 3×3 컨볼루션 커널을 적용합니다. 커널 값에 따라 샤프닝·엠보싱·윤곽선 등 다양한 효과가 납니다.",
            P(Param.Choice("커널", 0, "샤프닝", "엠보싱", "윤곽선 (Outline)", "수직 에지 (Prewitt)", "모션 블러 (대각선)")),
            (s, p) =>
            {
                var (k, delta) = kernels[p.I(0)];
                using var km = new Mat(3, 3, MatType.CV_32FC1);
                Marshal.Copy(k, 0, km.Data, 9);
                return Out(d => Cv2.Filter2D(s, d, MatType.CV_8U, km, null, delta));
            });

        Add(Blur, "가버 필터", "Cv2.GetGaborKernel + Cv2.Filter2D", "특정 방향·주기의 질감에 반응하는 필터입니다. 지문·직물 결함 검사 같은 텍스처 분석에 쓰입니다.",
            P(Param.Int("커널 크기", 3, 61, 21, odd: true), Param.Dbl("Sigma", 0.5, 20, 4, 0.1), Param.Dbl("방향 θ (도)", 0, 180, 0, 1),
              Param.Dbl("파장 λ", 2, 50, 10, 0.5), Param.Dbl("종횡비 γ", 0.1, 1, 0.5, 0.05)),
            (s, p) =>
            {
                using var g = Gray(s);
                using var k = Cv2.GetGaborKernel(new Size(p.I(0), p.I(0)), p.D(1), p.D(2) * Math.PI / 180, p.D(3), p.D(4), 0, MatType.CV_32F);
                using var f = Out(d => Cv2.Filter2D(g, d, MatType.CV_32F, k));
                return Out(d => Cv2.Normalize(f, d, 0, 255, NormTypes.MinMax, MatType.CV_8U));
            });

        Add(Blur, "에지 보존 필터", "Cv2.EdgePreservingFilter", "에지는 유지하고 나머지 영역을 매끈하게 만드는 사진 필터입니다 (photo 모듈).",
            P(Param.Choice("방식", 0, "Recursive", "Normalized Convolution"), Param.Dbl("Sigma S", 1, 200, 60, 1), Param.Dbl("Sigma R", 0.01, 1, 0.4, 0.01)),
            (s, p) => { using var b = Bgr(s); return Out(d => Cv2.EdgePreservingFilter(b, d, p.I(0) == 0 ? EdgePreservingMethods.RecursFilter : EdgePreservingMethods.NormconvFilter, p.F(1), p.F(2))); });

        Add(Blur, "디테일 강화", "Cv2.DetailEnhance", "세부 질감을 강조해 사진을 선명하고 또렷하게 만듭니다.",
            P(Param.Dbl("Sigma S", 1, 200, 10, 1), Param.Dbl("Sigma R", 0.01, 1, 0.15, 0.01)),
            (s, p) => { using var b = Bgr(s); return Out(d => Cv2.DetailEnhance(b, d, p.F(0), p.F(1))); });

        Add(Blur, "스타일화 (수채화)", "Cv2.Stylization", "영상을 수채화 · 카툰 느낌으로 만듭니다.",
            P(Param.Dbl("Sigma S", 1, 200, 60, 1), Param.Dbl("Sigma R", 0.01, 1, 0.45, 0.01)),
            (s, p) => { using var b = Bgr(s); return Out(d => Cv2.Stylization(b, d, p.F(0), p.F(1))); });

        Add(Blur, "연필 스케치", "Cv2.PencilSketch", "연필로 그린 듯한 스케치 효과를 만듭니다.",
            P(Param.Dbl("Sigma S", 1, 200, 60, 1), Param.Dbl("Sigma R", 0.01, 1, 0.07, 0.01), Param.Dbl("음영", 0, 0.1, 0.02, 0.005), Param.Choice("출력", 0, "흑백", "컬러")),
            (s, p) =>
            {
                using var b = Bgr(s);
                var gray = new Mat(); var color = new Mat();
                Cv2.PencilSketch(b, gray, color, p.F(0), p.F(1), p.F(2));
                if (p.I(3) == 0) { color.Dispose(); return gray; }
                gray.Dispose(); return color;
            });
    }

    static void AddThreshold()
    {
        Add(Thr, "임계값 이진화", "Cv2.Threshold", "임계값을 기준으로 픽셀을 나눕니다. Binary: 크면 최대값, 작으면 0 / Trunc: 임계값으로 자름 / ToZero: 작으면 0.",
            P(Param.Int("임계값", 0, 255, 127), Param.Int("최대값", 0, 255, 255),
              Param.Choice("방식", 0, "Binary", "Binary Inverted", "Truncate", "To Zero", "To Zero Inverted")),
            (s, p) => { using var g = Gray(s); return Out(d => Cv2.Threshold(g, d, p.I(0), p.I(1), (ThresholdTypes)p.I(2))); });

        Add(Thr, "오츠 자동 이진화", "Cv2.Threshold(THRESH_OTSU)", "히스토그램을 분석해 두 집단을 가장 잘 나누는 임계값을 자동으로 찾습니다. 계산된 임계값은 결과 정보에 표시됩니다.",
            P(Param.Bool("반전", false)),
            (s, p) =>
            {
                using var g = Gray(s);
                var d = new Mat();
                var t = Cv2.Threshold(g, d, 0, 255, (p.B(0) ? ThresholdTypes.BinaryInv : ThresholdTypes.Binary) | ThresholdTypes.Otsu);
                p.Info = $"오츠 임계값: {t:0}";
                return d;
            });

        Add(Thr, "트라이앵글 자동 이진화", "Cv2.Threshold(THRESH_TRIANGLE)", "히스토그램 봉우리와 끝점을 잇는 직선에서 가장 먼 지점을 임계값으로 씁니다. 한쪽으로 치우친 히스토그램에 유리합니다.",
            P(Param.Bool("반전", false)),
            (s, p) =>
            {
                using var g = Gray(s);
                var d = new Mat();
                var t = Cv2.Threshold(g, d, 0, 255, (p.B(0) ? ThresholdTypes.BinaryInv : ThresholdTypes.Binary) | ThresholdTypes.Triangle);
                p.Info = $"트라이앵글 임계값: {t:0}";
                return d;
            });

        Add(Thr, "적응형 이진화", "Cv2.AdaptiveThreshold", "픽셀마다 주변 영역의 평균(또는 가우시안 가중 평균) − C 를 임계값으로 씁니다. 조명이 고르지 않은 문서·영상에 효과적입니다.",
            P(Param.Choice("방법", 1, "Mean", "Gaussian"), Param.Choice("방식", 0, "Binary", "Binary Inverted"),
              Param.Int("블록 크기", 3, 151, 15, odd: true), Param.Dbl("C (상수)", -30, 30, 4, 0.5)),
            (s, p) =>
            {
                using var g = Gray(s);
                return Out(d => Cv2.AdaptiveThreshold(g, d, 255, p.I(0) == 0 ? AdaptiveThresholdTypes.MeanC : AdaptiveThresholdTypes.GaussianC,
                    p.I(1) == 0 ? ThresholdTypes.Binary : ThresholdTypes.BinaryInv, p.I(2), p.D(3)));
            });

        Add(Thr, "범위 이진화 (InRange)", "Cv2.InRange", "밝기가 [하한, 상한] 범위 안인 픽셀만 255로 만듭니다.",
            P(Param.Int("하한", 0, 255, 80), Param.Int("상한", 0, 255, 200)),
            (s, p) => { using var g = Gray(s); return Out(d => Cv2.InRange(g, new Scalar(p.I(0)), new Scalar(p.I(1)), d)); });
    }

    static void AddMorphology()
    {
        Param[] mp() => P(Param.Choice("커널 모양", 0, Shapes), Param.Int("커널 크기", 1, 41, 5), Param.Int("반복 횟수", 1, 10, 1));
        var ops = new (string name, MorphTypes type, string api, string desc)[]
        {
            ("침식 (Erode)", MorphTypes.Erode, "Cv2.Erode", "커널 영역의 최솟값을 취합니다. 밝은 객체가 줄어들고 작은 흰 노이즈가 사라집니다."),
            ("팽창 (Dilate)", MorphTypes.Dilate, "Cv2.Dilate", "커널 영역의 최댓값을 취합니다. 밝은 객체가 커지고 끊어진 부분·구멍이 메워집니다."),
            ("열림 (Opening)", MorphTypes.Open, "Cv2.MorphologyEx(MORPH_OPEN)", "침식 → 팽창. 객체 크기는 유지하면서 작은 흰 점 노이즈를 제거합니다."),
            ("닫힘 (Closing)", MorphTypes.Close, "Cv2.MorphologyEx(MORPH_CLOSE)", "팽창 → 침식. 객체 내부의 작은 검은 구멍과 틈을 메웁니다."),
            ("모폴로지 그래디언트", MorphTypes.Gradient, "Cv2.MorphologyEx(MORPH_GRADIENT)", "팽창 − 침식. 객체의 외곽선(경계)만 남습니다."),
            ("탑햇 (Top Hat)", MorphTypes.TopHat, "Cv2.MorphologyEx(MORPH_TOPHAT)", "원본 − 열림. 배경보다 밝은 작은 요소를 강조합니다. 불균일 조명 보정에 쓰입니다."),
            ("블랙햇 (Black Hat)", MorphTypes.BlackHat, "Cv2.MorphologyEx(MORPH_BLACKHAT)", "닫힘 − 원본. 배경보다 어두운 작은 요소를 강조합니다."),
        };
        foreach (var (name, type, api, desc) in ops)
            Add(Morph, name, api, desc, mp(), (s, p) =>
            {
                using var k = Kernel(p.I(0), p.I(1));
                return Out(d => Cv2.MorphologyEx(s, d, type, k, iterations: p.I(2)));
            });

        Add(Morph, "히트 미스 (Hit-or-Miss)", "Cv2.MorphologyEx(MORPH_HITMISS)", "커널 모양과 정확히 일치하는 패턴 위치만 찾습니다. 입력이 이진 영상이 아니면 오츠 이진화를 먼저 적용합니다.",
            P(Param.Choice("커널 모양", 1, Shapes), Param.Int("커널 크기", 1, 21, 3)),
            (s, p) =>
            {
                using var b = Binary(s);
                using var k = Kernel(p.I(0), p.I(1));
                return Out(d => Cv2.MorphologyEx(b, d, MorphTypes.HitMiss, k));
            });

        Add(Morph, "세선화 (Thinning)", "CvXImgProc.Thinning", "이진 객체를 1픽셀 두께의 뼈대(skeleton)로 만듭니다 (ximgproc 확장 모듈). 입력이 이진 영상이 아니면 오츠 이진화를 먼저 적용합니다.",
            P(Param.Choice("알고리즘", 0, "Zhang-Suen", "Guo-Hall")),
            (s, p) =>
            {
                using var b = Binary(s);
                return Out(d => CvXImgProc.Thinning(b, d, p.I(0) == 0 ? ThinningTypes.ZHANGSUEN : ThinningTypes.GUOHALL));
            });
    }

    static void AddEdge()
    {
        Add(Edge, "캐니 에지", "Cv2.Canny", "그래디언트 계산 → 비최대 억제 → 이중 임계값 히스테리시스로 가늘고 연결된 에지를 검출합니다. 가장 널리 쓰이는 에지 검출기입니다.",
            P(Param.Dbl("낮은 임계값", 0, 500, 80, 1), Param.Dbl("높은 임계값", 0, 500, 160, 1), Param.Choice("Sobel 커널", 0, "3", "5", "7"), Param.Bool("L2 그래디언트", false)),
            (s, p) => { using var g = Gray(s); return Out(d => Cv2.Canny(g, d, p.D(0), p.D(1), 3 + 2 * p.I(2), p.B(3))); });

        Add(Edge, "소벨 (Sobel)", "Cv2.Sobel", "1차 미분으로 밝기 변화(그래디언트)를 계산합니다. X 방향은 세로 에지, Y 방향은 가로 에지에 반응합니다.",
            P(Param.Choice("방향", 2, Dir3), Param.Choice("커널 크기", 1, "1", "3", "5", "7")),
            (s, p) =>
            {
                using var g = Gray(s);
                int k = new[] { 1, 3, 5, 7 }[p.I(1)];
                using var gx = Out(d => Cv2.Sobel(g, d, MatType.CV_32F, 1, 0, k));
                using var gy = Out(d => Cv2.Sobel(g, d, MatType.CV_32F, 0, 1, k));
                if (p.I(0) == 0) return Abs(gx);
                if (p.I(0) == 1) return Abs(gy);
                using var mag = Out(d => Cv2.Magnitude(gx, gy, d));
                return Abs(mag);
            });

        Add(Edge, "샤르 (Scharr)", "Cv2.Scharr", "3×3 소벨보다 회전 대칭성이 좋은 미분 필터입니다.",
            P(Param.Choice("방향", 2, Dir3)),
            (s, p) =>
            {
                using var g = Gray(s);
                using var gx = Out(d => Cv2.Scharr(g, d, MatType.CV_32F, 1, 0));
                using var gy = Out(d => Cv2.Scharr(g, d, MatType.CV_32F, 0, 1));
                if (p.I(0) == 0) return Abs(gx);
                if (p.I(0) == 1) return Abs(gy);
                using var mag = Out(d => Cv2.Magnitude(gx, gy, d));
                return Out(d => Cv2.Normalize(mag, d, 0, 255, NormTypes.MinMax, MatType.CV_8U));
            });

        Add(Edge, "라플라시안", "Cv2.Laplacian", "2차 미분으로 모든 방향의 에지를 한 번에 검출합니다. 노이즈에 민감하므로 블러 후 사용하는 것이 좋습니다.",
            P(Param.Choice("커널 크기", 1, "1", "3", "5", "7"), Param.Dbl("스케일", 0.1, 5, 1, 0.1)),
            (s, p) =>
            {
                using var g = Gray(s);
                using var l = Out(d => Cv2.Laplacian(g, d, MatType.CV_16S, 1 + 2 * p.I(0), p.D(1)));
                return Abs(l);
            });
    }

    static void AddGeometry()
    {
        var interp = new[] { "Nearest", "Linear", "Cubic", "Area", "Lanczos4" };

        Add(Geo, "크기 조절", "Cv2.Resize", "영상 크기를 바꿉니다. 축소에는 Area, 확대에는 Cubic / Lanczos 보간이 화질이 좋습니다.",
            P(Param.Dbl("가로 배율", 0.05, 4, 0.5, 0.05), Param.Dbl("세로 배율", 0.05, 4, 0.5, 0.05), Param.Choice("보간법", 3, interp)),
            (s, p) => Out(d => Cv2.Resize(s, d, new Size(0, 0), p.D(0), p.D(1), (InterpolationFlags)p.I(2))));

        Add(Geo, "회전", "Cv2.GetRotationMatrix2D + Cv2.WarpAffine", "영상 중심을 기준으로 회전·확대합니다. '캔버스 확장'을 켜면 잘리는 부분 없이 전체를 담습니다.",
            P(Param.Dbl("각도 (도)", -180, 180, 30, 0.5), Param.Dbl("배율", 0.1, 3, 1, 0.05), Param.Bool("캔버스 확장", true), Param.Choice("보간법", 1, interp)),
            (s, p) =>
            {
                var c = new Point2f(s.Cols / 2f, s.Rows / 2f);
                using var m = Cv2.GetRotationMatrix2D(c, p.D(0), p.D(1));
                var size = s.Size();
                if (p.B(2))
                {
                    double cos = Math.Abs(m.At<double>(0, 0)), sin = Math.Abs(m.At<double>(0, 1));
                    int nw = Math.Max(1, (int)(s.Rows * sin + s.Cols * cos)), nh = Math.Max(1, (int)(s.Rows * cos + s.Cols * sin));
                    m.Set(0, 2, m.At<double>(0, 2) + nw / 2.0 - c.X);
                    m.Set(1, 2, m.At<double>(1, 2) + nh / 2.0 - c.Y);
                    size = new Size(nw, nh);
                }
                return Out(d => Cv2.WarpAffine(s, d, m, size, (InterpolationFlags)p.I(3)));
            });

        Add(Geo, "반전 (Flip)", "Cv2.Flip", "영상을 상하 / 좌우 / 상하좌우로 뒤집습니다.",
            P(Param.Choice("방향", 1, "상하", "좌우", "상하좌우")),
            (s, p) => Out(d => Cv2.Flip(s, d, new[] { FlipMode.X, FlipMode.Y, FlipMode.XY }[p.I(0)])));

        Add(Geo, "90도 단위 회전", "Cv2.Rotate", "보간 없이 90° 단위로 정확하게 회전합니다.",
            P(Param.Choice("회전", 0, "시계 90°", "180°", "반시계 90°")),
            (s, p) => Out(d => Cv2.Rotate(s, d, (RotateFlags)p.I(0))));

        Add(Geo, "평행 이동", "Cv2.WarpAffine", "영상을 X, Y 방향으로 이동합니다. 빈 영역은 검은색으로 채워집니다.",
            P(Param.Int("X 이동", -1000, 1000, 80), Param.Int("Y 이동", -1000, 1000, 40)),
            (s, p) =>
            {
                using var m = new Mat(2, 3, MatType.CV_64FC1);
                m.Set(0, 0, 1.0); m.Set(0, 1, 0.0); m.Set(0, 2, (double)p.I(0));
                m.Set(1, 0, 0.0); m.Set(1, 1, 1.0); m.Set(1, 2, (double)p.I(1));
                return Out(d => Cv2.WarpAffine(s, d, m, s.Size()));
            });

        Add(Geo, "자르기 (ROI)", "new Mat(src, Rect)", "관심 영역(Region of Interest)만 잘라냅니다. 값은 영상 크기 대비 퍼센트입니다.",
            P(Param.Dbl("X (%)", 0, 99, 20, 0.5), Param.Dbl("Y (%)", 0, 99, 20, 0.5), Param.Dbl("너비 (%)", 1, 100, 60, 0.5), Param.Dbl("높이 (%)", 1, 100, 60, 0.5)),
            (s, p) =>
            {
                int x = (int)(s.Cols * p.D(0) / 100), y = (int)(s.Rows * p.D(1) / 100);
                int w = Math.Clamp((int)(s.Cols * p.D(2) / 100), 1, s.Cols - x), h = Math.Clamp((int)(s.Rows * p.D(3) / 100), 1, s.Rows - y);
                using var roi = new Mat(s, new Rect(x, y, w, h));
                return roi.Clone();
            });

        Add(Geo, "원근 변환", "Cv2.GetPerspectiveTransform + Cv2.WarpPerspective", "네 꼭짓점을 옮겨 사다리꼴 원근 효과를 만듭니다. 문서 스캔의 기울기 보정에 같은 원리가 쓰입니다.",
            P(Param.Dbl("위쪽 좁히기 (%)", 0, 45, 20, 0.5), Param.Dbl("아래쪽 좁히기 (%)", 0, 45, 0, 0.5)),
            (s, p) =>
            {
                float w = s.Cols, h = s.Rows, t = w * p.F(0) / 100, b = w * p.F(1) / 100;
                using var m = Cv2.GetPerspectiveTransform(
                    [new Point2f(0, 0), new Point2f(w, 0), new Point2f(w, h), new Point2f(0, h)],
                    [new Point2f(t, 0), new Point2f(w - t, 0), new Point2f(w - b, h), new Point2f(b, h)]);
                return Out(d => Cv2.WarpPerspective(s, d, m, s.Size()));
            });

        Add(Geo, "영상 피라미드", "Cv2.PyrDown / Cv2.PyrUp", "가우시안 피라미드로 영상을 1/2(또는 2배)씩 축소·확대합니다.",
            P(Param.Choice("방향", 0, "축소 (PyrDown)", "확대 (PyrUp)"), Param.Int("단계", 1, 3, 1)),
            (s, p) =>
            {
                var cur = s.Clone();
                for (int i = 0; i < p.I(1); i++)
                {
                    var n = new Mat();
                    if (p.I(0) == 0) Cv2.PyrDown(cur, n); else Cv2.PyrUp(cur, n);
                    cur.Dispose(); cur = n;
                }
                return cur;
            });

        Add(Geo, "극좌표 변환", "Cv2.WarpPolar", "영상 중심을 기준으로 극좌표(각도 × 반지름)로 펼칩니다. 원형 물체(병뚜껑·베어링) 검사에 쓰입니다.",
            P(Param.Choice("모드", 0, "Linear", "Log"), Param.Dbl("반지름 (%)", 5, 100, 70, 1)),
            (s, p) =>
            {
                var c = new Point2f(s.Cols / 2f, s.Rows / 2f);
                double r = Math.Min(s.Cols, s.Rows) / 2.0 * p.D(1) / 100 * Math.Sqrt(2);
                return Out(d => Cv2.WarpPolar(s, d, s.Size(), c, r, InterpolationFlags.Linear, p.I(0) == 0 ? WarpPolarMode.Linear : WarpPolarMode.Log));
            });
    }

    static void AddHistogram()
    {
        Add(Hist, "히스토그램 평활화", "Cv2.EqualizeHist", "밝기 분포를 고르게 펴서 대비를 높입니다. 컬러 영상은 밝기(Y) 채널에만 적용합니다.",
            P(Param.Bool("컬러 유지", true)),
            (s, p) =>
            {
                if (!p.B(0) || s.Channels() == 1) { using var g = Gray(s); return Out(d => Cv2.EqualizeHist(g, d)); }
                using var y = Out(d => Cv2.CvtColor(s, d, ColorConversionCodes.BGR2YCrCb));
                var ch = Cv2.Split(y);
                Cv2.EqualizeHist(ch[0], ch[0]);
                Cv2.Merge(ch, y);
                foreach (var c in ch) c.Dispose();
                return Out(d => Cv2.CvtColor(y, d, ColorConversionCodes.YCrCb2BGR));
            });

        Add(Hist, "CLAHE (적응형 평활화)", "Cv2.CreateCLAHE", "영상을 타일로 나눠 지역별로 평활화하고, 대비 제한으로 노이즈 증폭을 막습니다. 의료·저조도 영상에 많이 쓰입니다.",
            P(Param.Dbl("대비 제한", 0.5, 40, 2, 0.5), Param.Int("타일 개수", 1, 32, 8), Param.Bool("컬러 유지", true)),
            (s, p) =>
            {
                using var clahe = Cv2.CreateCLAHE(p.D(0), new Size(p.I(1), p.I(1)));
                if (!p.B(2) || s.Channels() == 1) { using var g = Gray(s); return Out(d => clahe.Apply(g, d)); }
                using var lab = Out(d => Cv2.CvtColor(s, d, ColorConversionCodes.BGR2Lab));
                var ch = Cv2.Split(lab);
                clahe.Apply(ch[0], ch[0]);
                Cv2.Merge(ch, lab);
                foreach (var c in ch) c.Dispose();
                return Out(d => Cv2.CvtColor(lab, d, ColorConversionCodes.Lab2BGR));
            });

        Add(Hist, "정규화 (대비 늘리기)", "Cv2.Normalize(NORM_MINMAX)", "최소~최대 밝기를 지정한 범위로 선형 확장합니다.",
            P(Param.Int("출력 최소", 0, 255, 0), Param.Int("출력 최대", 0, 255, 255)),
            (s, p) => Out(d => Cv2.Normalize(s, d, p.I(0), p.I(1), NormTypes.MinMax)));

        Add(Hist, "히스토그램 그래프", "Cv2.CalcHist", "입력 영상의 채널별 밝기 분포를 그래프로 그립니다. 이진화 임계값을 고를 때 참고하세요.",
            P(Param.Bool("로그 스케일", false)),
            (s, p) =>
            {
                const int W = 768, H = 400, pad = 20;
                var canvas = new Mat(H, W, MatType.CV_8UC3, new Scalar(34, 28, 26));
                for (int i = 0; i <= 4; i++)
                {
                    int x = pad + i * (W - 2 * pad) / 4;
                    Cv2.Line(canvas, new Point(x, pad), new Point(x, H - pad), new Scalar(60, 54, 50), 1);
                    Cv2.PutText(canvas, (i * 64).ToString(), new Point(x + 3, H - 5), HersheyFonts.HersheySimplex, 0.4, new Scalar(150, 150, 150), 1, LineTypes.AntiAlias);
                }
                Scalar[] cols = s.Channels() == 1 ? [new Scalar(230, 230, 230)] : [new Scalar(255, 140, 60), new Scalar(90, 220, 90), new Scalar(80, 80, 255)];
                for (int c = 0; c < cols.Length; c++)
                {
                    using var hist = new Mat();
                    Cv2.CalcHist([s], [c], null, hist, 1, [256], [new Rangef(0, 256)]);
                    if (p.B(0)) { Cv2.Add(hist, Scalar.All(1), hist); Cv2.Log(hist, hist); }
                    Cv2.Normalize(hist, hist, 0, H - 2 * pad, NormTypes.MinMax);
                    var pts = Enumerable.Range(0, 256).Select(i => new Point(pad + i * (W - 2 * pad) / 255, H - pad - (int)hist.At<float>(i))).ToArray();
                    Cv2.Polylines(canvas, [pts], false, cols[c], 2, LineTypes.AntiAlias);
                }
                Cv2.MeanStdDev(s, out var mean, out var sd);
                p.Info = s.Channels() == 1 ? $"평균 {mean.Val0:0.0}, 표준편차 {sd.Val0:0.0}" : $"평균 B {mean.Val0:0} G {mean.Val1:0} R {mean.Val2:0}";
                return canvas;
            });
    }

    static void AddFeatures()
    {
        Add(Feat, "해리스 코너", "Cv2.CornerHarris", "두 방향 모두 밝기 변화가 큰 지점(코너)을 찾습니다. 응답값이 최대값 × 임계 비율 이상인 픽셀을 표시합니다.",
            P(Param.Int("블록 크기", 2, 10, 2), Param.Choice("Sobel 커널", 0, "3", "5", "7"), Param.Dbl("k", 0.01, 0.2, 0.04, 0.005), Param.Dbl("임계 비율 (%)", 0.1, 50, 1, 0.1)),
            (s, p) =>
            {
                using var g = Gray(s);
                using var f = new Mat(); g.ConvertTo(f, MatType.CV_32F);
                using var r = Out(d => Cv2.CornerHarris(f, d, p.I(0), 3 + 2 * p.I(1), p.D(2)));
                Cv2.MinMaxLoc(r, out _, out double max);
                using var mask = new Mat();
                Cv2.Threshold(r, mask, max * p.D(3) / 100, 255, ThresholdTypes.Binary);
                using var m8 = new Mat(); mask.ConvertTo(m8, MatType.CV_8U);
                using var locs = new Mat(); Cv2.FindNonZero(m8, locs);
                var d = Bgr(s);
                if (!locs.Empty() && locs.GetArray(out Point[] pts))
                {
                    foreach (var pt in pts.Take(20000)) Cv2.Circle(d, pt, 3, new Scalar(60, 60, 255), 1, LineTypes.AntiAlias);
                    p.Info = $"코너 픽셀: {pts.Length}개";
                }
                else p.Info = "코너 픽셀: 0개";
                return d;
            });

        Add(Feat, "Shi-Tomasi 코너", "Cv2.GoodFeaturesToTrack", "추적하기 좋은 강한 코너를 지정한 개수만큼 고릅니다. 광류(Optical Flow) 추적의 시작점으로 쓰입니다.",
            P(Param.Int("최대 개수", 1, 2000, 150), Param.Dbl("품질 수준", 0.001, 0.5, 0.01, 0.001), Param.Dbl("최소 거리", 1, 200, 10, 1)),
            (s, p) =>
            {
                using var g = Gray(s);
                var pts = Cv2.GoodFeaturesToTrack(g, p.I(0), p.D(1), p.D(2), null!, 3, false, 0.04);
                var d = Bgr(s);
                foreach (var pt in pts) Cv2.Circle(d, Pt(pt), 5, new Scalar(80, 220, 120), 2, LineTypes.AntiAlias);
                p.Info = $"코너: {pts.Length}개";
                return d;
            });

        Add(Feat, "FAST", "FastFeatureDetector", "원 둘레 16픽셀 비교로 매우 빠르게 코너를 찾습니다. 실시간 처리에 적합합니다.",
            P(Param.Int("임계값", 1, 150, 25), Param.Bool("비최대 억제", true), Param.Bool("크기/방향 표시", false)),
            (s, p) => { using var g = Gray(s); using var det = FastFeatureDetector.Create(p.I(0), p.B(1)); return DrawKeypoints(s, det.Detect(g), p.B(2), p); });

        Add(Feat, "ORB", "ORB.Create", "FAST + 회전 BRIEF 기반의 빠르고 특허 문제 없는 특징점 검출/기술자입니다. 영상 매칭·정합에 널리 쓰입니다.",
            P(Param.Int("최대 개수", 10, 5000, 500), Param.Dbl("스케일 배율", 1.05, 2, 1.2, 0.05), Param.Int("피라미드 단계", 1, 12, 8), Param.Bool("크기/방향 표시", true)),
            (s, p) => { using var g = Gray(s); using var det = ORB.Create(p.I(0), p.F(1), p.I(2)); return DrawKeypoints(s, det.Detect(g), p.B(3), p); });

        Add(Feat, "SIFT", "SIFT.Create", "스케일·회전에 불변인 대표적인 특징점 검출기입니다. 정확도가 높지만 상대적으로 느립니다.",
            P(Param.Int("최대 개수 (0=전체)", 0, 5000, 500), Param.Dbl("대비 임계값", 0.005, 0.2, 0.04, 0.005), Param.Dbl("에지 임계값", 1, 50, 10, 0.5), Param.Bool("크기/방향 표시", true)),
            (s, p) => { using var g = Gray(s); using var det = SIFT.Create(p.I(0), 3, p.D(1), p.D(2)); return DrawKeypoints(s, det.Detect(g), p.B(3), p); });

        Add(Feat, "AKAZE", "AKAZE.Create", "비선형 스케일 공간에서 특징점을 찾아 에지 부근에서도 정확합니다.",
            P(Param.Dbl("임계값", 0.0001, 0.02, 0.001, 0.0001), Param.Bool("크기/방향 표시", true)),
            (s, p) => { using var g = Gray(s); using var det = AKAZE.Create(threshold: p.F(0)); return DrawKeypoints(s, det.Detect(g), p.B(1), p); });

        Add(Feat, "BRISK", "BRISK.Create", "스케일 공간 FAST 검출 + 이진 기술자를 사용하는 빠른 특징점 알고리즘입니다.",
            P(Param.Int("임계값", 5, 150, 30), Param.Int("옥타브", 0, 8, 3), Param.Bool("크기/방향 표시", true)),
            (s, p) => { using var g = Gray(s); using var det = BRISK.Create(p.I(0), p.I(1)); return DrawKeypoints(s, det.Detect(g), p.B(2), p); });

        Add(Feat, "KAZE", "KAZE.Create", "비선형 확산 기반의 스케일 불변 특징점입니다. AKAZE보다 느리지만 정밀합니다.",
            P(Param.Dbl("임계값", 0.0001, 0.02, 0.001, 0.0001), Param.Bool("크기/방향 표시", true)),
            (s, p) => { using var g = Gray(s); using var det = KAZE.Create(threshold: p.F(0)); return DrawKeypoints(s, det.Detect(g), p.B(1), p); });

        Add(Feat, "MSER 영역", "MSER.Create + DetectRegions", "임계값을 바꿔도 모양이 안정적으로 유지되는 영역(글자, 표지판 등)을 찾습니다.",
            P(Param.Int("Delta", 1, 30, 5), Param.Int("최소 면적", 5, 5000, 60), Param.Int("최대 면적", 100, 100000, 14400)),
            (s, p) =>
            {
                using var g = Gray(s);
                using var mser = MSER.Create(p.I(0), p.I(1), p.I(2));
                mser.DetectRegions(g, out Point[][] regions, out _);
                var d = Bgr(s);
                for (int i = 0; i < regions.Length; i++) Cv2.Polylines(d, [Cv2.ConvexHull(regions[i])], true, Col(i), 1, LineTypes.AntiAlias);
                p.Info = $"MSER 영역: {regions.Length}개";
                return d;
            });
    }

    static void AddShape()
    {
        Add(Shape, "윤곽선 검출", "Cv2.FindContours + Cv2.DrawContours", "이진 영상에서 객체의 외곽선을 찾아 그립니다. 입력이 이진 영상이 아니면 오츠 이진화를 먼저 적용합니다.",
            P(Param.Choice("검색 모드", 0, "External (바깥만)", "List (전체)", "CComp (2단계)", "Tree (계층)"), Param.Dbl("최소 면적", 0, 20000, 100, 10),
              Param.Int("선 두께", 1, 10, 2), Param.Bool("원본 위에 그리기", true)),
            (s, p) =>
            {
                using var b = Binary(s);
                Cv2.FindContours(b, out Point[][] cs, out _, (RetrievalModes)p.I(0), ContourApproximationModes.ApproxSimple);
                var kept = cs.Where(c => Cv2.ContourArea(c) >= p.D(1)).ToArray();
                var d = Canvas(s, p, p.B(3));
                for (int i = 0; i < kept.Length; i++) Cv2.DrawContours(d, kept, i, Col(i), p.I(2), LineTypes.AntiAlias);
                p.Info = $"윤곽선: {kept.Length}개 (전체 {cs.Length}개)";
                return d;
            });

        Add(Shape, "외접 도형 / 근사", "BoundingRect, MinAreaRect, MinEnclosingCircle, FitEllipse, ConvexHull, ApproxPolyDP",
            "각 윤곽선을 감싸는 도형을 계산합니다. 다각형 근사는 꼭짓점 수로 삼각형·사각형 등을 구분할 때 쓰입니다.",
            P(Param.Choice("도형", 0, "바운딩 박스", "최소 면적 사각형 (회전)", "최소 외접원", "타원 피팅", "볼록 껍질", "다각형 근사"),
              Param.Dbl("근사 정밀도 ε (둘레 %)", 0.1, 10, 2, 0.1), Param.Dbl("최소 면적", 0, 20000, 200, 10), Param.Bool("원본 위에 그리기", true)),
            (s, p) =>
            {
                using var b = Binary(s);
                Cv2.FindContours(b, out Point[][] cs, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var d = Canvas(s, p, p.B(3));
                int n = 0;
                foreach (var c in cs.Where(c => Cv2.ContourArea(c) >= p.D(2)))
                {
                    var col = Col(n++);
                    switch (p.I(0))
                    {
                        case 0: Cv2.Rectangle(d, Cv2.BoundingRect(c), col, 2, LineTypes.AntiAlias); break;
                        case 1: Cv2.Polylines(d, [Cv2.MinAreaRect(c).Points().Select(Pt).ToArray()], true, col, 2, LineTypes.AntiAlias); break;
                        case 2: Cv2.MinEnclosingCircle(c, out var ctr, out var rad); Cv2.Circle(d, Pt(ctr), (int)rad, col, 2, LineTypes.AntiAlias); break;
                        case 3: if (c.Length >= 5) Cv2.Ellipse(d, Cv2.FitEllipse(c), col, 2, LineTypes.AntiAlias); break;
                        case 4: Cv2.Polylines(d, [Cv2.ConvexHull(c)], true, col, 2, LineTypes.AntiAlias); break;
                        default:
                            var ap = Cv2.ApproxPolyDP(c, p.D(1) / 100 * Cv2.ArcLength(c, true), true);
                            Cv2.Polylines(d, [ap], true, col, 2, LineTypes.AntiAlias);
                            foreach (var v in ap) Cv2.Circle(d, v, 4, col, -1, LineTypes.AntiAlias);
                            Cv2.PutText(d, ap.Length.ToString(), ap[0] + new Point(4, -6), HersheyFonts.HersheySimplex, 0.6, col, 2, LineTypes.AntiAlias);
                            break;
                    }
                }
                p.Info = $"객체: {n}개";
                return d;
            });

        Add(Shape, "연결 요소 라벨링", "Cv2.ConnectedComponentsWithStats", "서로 붙어 있는 픽셀 덩어리마다 번호를 붙이고 면적·위치·중심을 계산합니다. 입력이 이진 영상이 아니면 오츠 이진화를 먼저 적용합니다.",
            P(Param.Choice("연결성", 1, "4-연결", "8-연결"), Param.Int("최소 면적", 0, 20000, 50), Param.Bool("박스·중심 표시", true)),
            (s, p) =>
            {
                using var b = Binary(s);
                using var labels = new Mat(); using var stats = new Mat(); using var cent = new Mat();
                int n = Cv2.ConnectedComponentsWithStats(b, labels, stats, cent, p.I(0) == 0 ? PixelConnectivity.Connectivity4 : PixelConnectivity.Connectivity8);
                var colors = new byte[n * 3];
                int kept = 0;
                for (int i = 1; i < n; i++)
                {
                    if (stats.At<int>(i, (int)ConnectedComponentsTypes.Area) < p.I(1)) continue;
                    kept++;
                    var c = Col(i);
                    colors[i * 3] = (byte)c.Val0; colors[i * 3 + 1] = (byte)c.Val1; colors[i * 3 + 2] = (byte)c.Val2;
                }
                var lab = new int[labels.Rows * labels.Cols];
                Marshal.Copy(labels.Data, lab, 0, lab.Length);
                var px = new byte[lab.Length * 3];
                for (int i = 0; i < lab.Length; i++) { int l = lab[i] * 3; px[i * 3] = colors[l]; px[i * 3 + 1] = colors[l + 1]; px[i * 3 + 2] = colors[l + 2]; }
                var d = new Mat(labels.Rows, labels.Cols, MatType.CV_8UC3);
                Marshal.Copy(px, 0, d.Data, px.Length);
                if (p.B(2))
                    for (int i = 1; i < n; i++)
                    {
                        if (stats.At<int>(i, (int)ConnectedComponentsTypes.Area) < p.I(1)) continue;
                        var r = new Rect(stats.At<int>(i, 0), stats.At<int>(i, 1), stats.At<int>(i, 2), stats.At<int>(i, 3));
                        Cv2.Rectangle(d, r, new Scalar(255, 255, 255), 1);
                        Cv2.Circle(d, new Point((int)cent.At<double>(i, 0), (int)cent.At<double>(i, 1)), 3, new Scalar(0, 0, 255), -1);
                    }
                p.Info = $"연결 요소: {kept}개 (배경 제외, 면적 필터 전 {n - 1}개)";
                return d;
            });

        Add(Shape, "허프 직선 (확률적)", "Cv2.HoughLinesP", "에지 영상에서 직선 선분을 찾습니다. 입력이 에지/이진 영상이 아니면 Canny를 먼저 적용합니다.",
            P(Param.Int("투표 임계값", 1, 300, 60), Param.Dbl("최소 길이", 0, 1000, 50, 1), Param.Dbl("최대 간격", 0, 200, 10, 1), Param.Bool("원본 위에 그리기", true)),
            (s, p) =>
            {
                using var e = Edges(s);
                var lines = Cv2.HoughLinesP(e, 1, Math.PI / 180, p.I(0), p.D(1), p.D(2));
                var d = Canvas(s, p, p.B(3));
                foreach (var l in lines) Cv2.Line(d, l.P1, l.P2, new Scalar(60, 60, 255), 2, LineTypes.AntiAlias);
                p.Info = $"선분: {lines.Length}개";
                return d;
            });

        Add(Shape, "허프 직선 (표준)", "Cv2.HoughLines", "(ρ, θ) 공간 투표로 무한 직선을 찾습니다. 입력이 에지/이진 영상이 아니면 Canny를 먼저 적용합니다.",
            P(Param.Int("투표 임계값", 1, 500, 120), Param.Bool("원본 위에 그리기", true)),
            (s, p) =>
            {
                using var e = Edges(s);
                var lines = Cv2.HoughLines(e, 1, Math.PI / 180, p.I(0));
                var d = Canvas(s, p, p.B(1));
                int L = s.Cols + s.Rows;
                foreach (var l in lines.Take(500))
                {
                    double a = Math.Cos(l.Theta), b = Math.Sin(l.Theta), x0 = a * l.Rho, y0 = b * l.Rho;
                    Cv2.Line(d, new Point((int)(x0 - L * b), (int)(y0 + L * a)), new Point((int)(x0 + L * b), (int)(y0 - L * a)), new Scalar(60, 60, 255), 1, LineTypes.AntiAlias);
                }
                p.Info = $"직선: {lines.Length}개";
                return d;
            });

        Add(Shape, "허프 원", "Cv2.HoughCircles", "그래디언트 기반 허프 변환으로 원을 검출합니다. 동전·구멍·부품 검사에 쓰입니다. 블러된 영상에서 더 안정적입니다.",
            P(Param.Dbl("dp (해상도 비)", 1, 3, 1.2, 0.1), Param.Dbl("원 중심 최소 거리", 1, 500, 30, 1), Param.Dbl("Canny 상한 (param1)", 1, 300, 100, 1),
              Param.Dbl("누적 임계값 (param2)", 1, 200, 30, 1), Param.Int("최소 반지름", 0, 500, 10), Param.Int("최대 반지름 (0=무제한)", 0, 1000, 100), Param.Bool("원본 위에 그리기", true)),
            (s, p) =>
            {
                using var g = Gray(s);
                var circles = Cv2.HoughCircles(g, HoughModes.Gradient, p.D(0), p.D(1), p.D(2), p.D(3), p.I(4), p.I(5));
                var d = Canvas(s, p, p.B(6));
                foreach (var c in circles)
                {
                    Cv2.Circle(d, Pt(c.Center), (int)c.Radius, new Scalar(80, 220, 120), 2, LineTypes.AntiAlias);
                    Cv2.Circle(d, Pt(c.Center), 2, new Scalar(60, 60, 255), -1);
                }
                p.Info = $"원: {circles.Length}개";
                return d;
            });

        Add(Shape, "거리 변환", "Cv2.DistanceTransform", "각 전경 픽셀에서 가장 가까운 배경까지의 거리를 밝기로 나타냅니다. 객체 중심 찾기·워터셰드 전처리에 쓰입니다.",
            P(Param.Choice("거리 종류", 1, "L1 (맨해튼)", "L2 (유클리드)", "C (체스보드)"), Param.Choice("마스크", 1, "3×3", "5×5", "정밀 (Precise)"), Param.Bool("컬러맵 적용", true)),
            (s, p) =>
            {
                using var b = Binary(s);
                var type = new[] { DistanceTypes.L1, DistanceTypes.L2, DistanceTypes.C }[p.I(0)];
                var mask = new[] { DistanceTransformMasks.Mask3, DistanceTransformMasks.Mask5, DistanceTransformMasks.Precise }[p.I(1)];
                using var f = Out(d => Cv2.DistanceTransform(b, d, type, mask));
                Cv2.MinMaxLoc(f, out _, out double max);
                p.Info = $"최대 거리: {max:0.0} px";
                var n = Out(d => Cv2.Normalize(f, d, 0, 255, NormTypes.MinMax, MatType.CV_8U));
                if (!p.B(2)) return n;
                using (n) return Out(d => Cv2.ApplyColorMap(n, d, ColormapTypes.Inferno));
            });

        Add(Shape, "모멘트 / 중심 계산", "Cv2.Moments + Cv2.HuMoments", "각 윤곽선의 면적, 둘레, 무게중심을 계산해 표시합니다.",
            P(Param.Dbl("최소 면적", 0, 20000, 200, 10), Param.Bool("원본 위에 그리기", true)),
            (s, p) =>
            {
                using var b = Binary(s);
                Cv2.FindContours(b, out Point[][] cs, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var d = Canvas(s, p, p.B(1));
                int n = 0;
                foreach (var c in cs)
                {
                    var m = Cv2.Moments(c);
                    if (m.M00 < Math.Max(p.D(0), 1)) continue;
                    var ctr = new Point((int)(m.M10 / m.M00), (int)(m.M01 / m.M00));
                    var col = Col(n++);
                    Cv2.DrawContours(d, [c], 0, col, 2, LineTypes.AntiAlias);
                    Cv2.DrawMarker(d, ctr, new Scalar(255, 255, 255), MarkerTypes.Cross, 14, 2);
                    Cv2.PutText(d, $"A={m.M00:0} P={Cv2.ArcLength(c, true):0}", ctr + new Point(8, -8), HersheyFonts.HersheySimplex, 0.45, col, 1, LineTypes.AntiAlias);
                }
                p.Info = $"객체: {n}개";
                return d;
            });
    }

    static void AddSegmentation()
    {
        Add(Seg, "워터셰드 (자동 마커)", "Cv2.Watershed", "오츠 이진화 → 거리 변환으로 확실한 전경/배경 마커를 만든 뒤, 맞닿은 객체 사이 경계(빨간 선)를 찾습니다. 붙어 있는 동전·세포 분리에 쓰입니다.",
            P(Param.Bool("전경이 어두움 (반전)", false), Param.Dbl("전경 확신 비율", 0.05, 0.95, 0.5, 0.05), Param.Int("열림 반복", 0, 5, 2)),
            (s, p) =>
            {
                using var bgr = Bgr(s); using var g = Gray(s);
                using var th = new Mat();
                Cv2.Threshold(g, th, 0, 255, (p.B(0) ? ThresholdTypes.BinaryInv : ThresholdTypes.Binary) | ThresholdTypes.Otsu);
                using var k = Kernel(0, 3);
                using var opened = Out(d => Cv2.MorphologyEx(th, d, MorphTypes.Open, k, iterations: p.I(2)));
                using var sureBg = Out(d => Cv2.Dilate(opened, d, k, iterations: 3));
                using var dist = Out(d => Cv2.DistanceTransform(opened, d, DistanceTypes.L2, DistanceTransformMasks.Mask5));
                Cv2.MinMaxLoc(dist, out _, out double mx);
                using var fgF = new Mat(); Cv2.Threshold(dist, fgF, p.D(1) * mx, 255, ThresholdTypes.Binary);
                using var sureFg = new Mat(); fgF.ConvertTo(sureFg, MatType.CV_8U);
                using var unknown = Out(d => Cv2.Subtract(sureBg, sureFg, d));
                using var markers = new Mat();
                int n = Cv2.ConnectedComponents(sureFg, markers);
                Cv2.Add(markers, Scalar.All(1), markers);
                markers.SetTo(Scalar.All(0), unknown);
                Cv2.Watershed(bgr, markers);
                using var boundary = new Mat(); Cv2.InRange(markers, new Scalar(-1), new Scalar(-1), boundary);
                using var thick = Out(d => Cv2.Dilate(boundary, d, null));
                var outp = bgr.Clone();
                outp.SetTo(new Scalar(40, 40, 255), thick);
                p.Info = $"분할된 영역: {n - 1}개";
                return outp;
            });

        Add(Seg, "그랩컷 (GrabCut)", "Cv2.GrabCut", "가장자리 여백 바깥을 배경으로 가정하고, 그래프 컷 반복으로 전경 객체만 분리합니다. 큰 영상에서는 수 초 걸릴 수 있습니다.",
            P(Param.Dbl("가장자리 여백 (%)", 1, 45, 8, 0.5), Param.Int("반복 횟수", 1, 10, 3)),
            (s, p) =>
            {
                using var bgr = Bgr(s);
                int mx = Math.Max(1, (int)(bgr.Cols * p.D(0) / 100)), my = Math.Max(1, (int)(bgr.Rows * p.D(0) / 100));
                var rect = new Rect(mx, my, bgr.Cols - 2 * mx, bgr.Rows - 2 * my);
                using var mask = new Mat(); using var bgM = new Mat(); using var fgM = new Mat();
                Cv2.GrabCut(bgr, mask, rect, bgM, fgM, p.I(1), GrabCutModes.InitWithRect);
                using var lut = Lut(i => i == 1 || i == 3 ? 255 : 0);
                using var fg = Out(d => Cv2.LUT(mask, lut, d));
                var outp = new Mat(bgr.Size(), bgr.Type(), Scalar.All(0));
                bgr.CopyTo(outp, fg);
                Cv2.Rectangle(outp, rect, new Scalar(255, 170, 60), 1);
                p.Info = $"전경 비율: {Cv2.CountNonZero(fg) * 100.0 / fg.Total():0.0}%";
                return outp;
            });

        Add(Seg, "K-평균 색상 군집화", "Cv2.Kmeans", "모든 픽셀 색을 K개의 대표 색으로 묶습니다 (색상 양자화 / 영역 분할).",
            P(Param.Int("군집 수 K", 2, 16, 4), Param.Int("시도 횟수", 1, 10, 2)),
            (s, p) =>
            {
                using var bgr = Bgr(s);
                using var flat = bgr.Reshape(1, bgr.Rows * bgr.Cols);
                using var data = new Mat(); flat.ConvertTo(data, MatType.CV_32F);
                using var labels = new Mat(); using var centers = new Mat();
                Cv2.Kmeans(data, p.I(0), labels, new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 10, 1.0), p.I(1), KMeansFlags.PpCenters, centers);
                var lab = new int[labels.Rows];
                Marshal.Copy(labels.Data, lab, 0, lab.Length);
                var cen = new byte[centers.Rows * 3];
                for (int i = 0; i < centers.Rows; i++)
                    for (int j = 0; j < 3; j++) cen[i * 3 + j] = (byte)Math.Clamp(centers.At<float>(i, j), 0, 255);
                var px = new byte[lab.Length * 3];
                for (int i = 0; i < lab.Length; i++) { int c = lab[i] * 3; px[i * 3] = cen[c]; px[i * 3 + 1] = cen[c + 1]; px[i * 3 + 2] = cen[c + 2]; }
                var d = new Mat(bgr.Rows, bgr.Cols, MatType.CV_8UC3);
                Marshal.Copy(px, 0, d.Data, px.Length);
                return d;
            });

        Add(Seg, "평균 이동 필터", "Cv2.PyrMeanShiftFiltering", "비슷한 색·가까운 위치의 픽셀을 같은 색으로 평탄화합니다. 포스터 효과·분할 전처리에 쓰입니다.",
            P(Param.Dbl("공간 반경 (sp)", 1, 60, 15, 1), Param.Dbl("색 반경 (sr)", 1, 120, 30, 1), Param.Int("피라미드 단계", 0, 3, 1)),
            (s, p) => { using var b = Bgr(s); return Out(d => Cv2.PyrMeanShiftFiltering(b, d, p.D(0), p.D(1), p.I(2))); });

        Add(Seg, "슈퍼픽셀 (SLIC)", "CvXImgProc.CreateSuperpixelSLIC", "영상을 색과 위치가 비슷한 작은 조각(슈퍼픽셀)으로 나눕니다 (ximgproc 확장 모듈).",
            P(Param.Choice("알고리즘", 1, "SLIC", "SLICO", "MSLIC"), Param.Int("영역 크기", 5, 100, 25), Param.Dbl("평활도 (ruler)", 1, 50, 10, 0.5), Param.Int("반복 횟수", 1, 20, 10)),
            (s, p) =>
            {
                using var b = Bgr(s);
                using var lab = Out(d => Cv2.CvtColor(b, d, ColorConversionCodes.BGR2Lab));
                using var slic = SuperpixelSLIC.Create(lab, new[] { SLICType.SLIC, SLICType.SLICO, SLICType.MSLIC }[p.I(0)], p.I(1), p.F(2));
                slic.Iterate(p.I(3));
                using var mask = new Mat();
                slic.GetLabelContourMask(mask, true);
                var d = b.Clone();
                d.SetTo(new Scalar(60, 230, 255), mask);
                p.Info = $"슈퍼픽셀: {slic.GetNumberOfSuperpixels()}개";
                return d;
            });
    }

    static void AddNoise()
    {
        Add(Noise, "가우시안 노이즈 추가", "Cv2.Randn", "정규분포 노이즈를 더합니다. 블러·디노이즈 필터의 효과를 시험할 때 사용하세요.",
            P(Param.Dbl("표준편차", 0, 100, 20, 1)),
            (s, p) =>
            {
                using var n = new Mat(s.Size(), MatType.CV_32FC(s.Channels()));
                Cv2.Randn(n, Scalar.All(0), Scalar.All(p.D(0)));
                using var f = new Mat(); s.ConvertTo(f, MatType.CV_32F);
                Cv2.Add(f, n, f);
                return Out(d => f.ConvertTo(d, MatType.CV_8U));
            });

        Add(Noise, "소금-후추 노이즈 추가", "Cv2.Randu", "임의의 픽셀을 흰색(소금)·검은색(후추)으로 바꿉니다. 미디언 블러의 효과를 확인하기 좋습니다.",
            P(Param.Dbl("비율 (%)", 0, 50, 5, 0.5)),
            (s, p) =>
            {
                using var r = new Mat(s.Size(), MatType.CV_8UC1);
                Cv2.Randu(r, Scalar.All(0), Scalar.All(256));
                double t = p.D(0) / 100 * 256 / 2;
                using var pepper = new Mat(); Cv2.Threshold(r, pepper, t - 1, 255, ThresholdTypes.BinaryInv);
                using var salt = new Mat(); Cv2.Threshold(r, salt, 255 - t, 255, ThresholdTypes.Binary);
                var d = s.Clone();
                if (t >= 1) d.SetTo(Scalar.All(0), pepper);
                d.SetTo(Scalar.All(255), salt);
                return d;
            });

        Add(Noise, "비지역 평균 디노이즈", "Cv2.FastNlMeansDenoising(Colored)", "영상 전체에서 비슷한 패치를 찾아 평균하는 고품질 노이즈 제거입니다. 느리지만 디테일 보존이 뛰어납니다.",
            P(Param.Dbl("강도 h", 1, 40, 10, 0.5), Param.Int("템플릿 창", 3, 15, 7, odd: true), Param.Int("탐색 창", 5, 41, 21, odd: true)),
            (s, p) => Out(d =>
            {
                if (s.Channels() == 1) Cv2.FastNlMeansDenoising(s, d, p.F(0), p.I(1), p.I(2));
                else Cv2.FastNlMeansDenoisingColored(s, d, p.F(0), p.F(0), p.I(1), p.I(2));
            }));

        Add(Noise, "인페인팅 (밝은 영역 복원)", "Cv2.Inpaint", "기준 밝기 이상인 픽셀(흰 글씨·스크래치 등)을 마스크로 삼아 주변 정보로 메웁니다.",
            P(Param.Int("마스크 기준 밝기", 0, 255, 235), Param.Int("마스크 팽창", 0, 10, 1), Param.Dbl("복원 반경", 1, 20, 3, 0.5), Param.Choice("알고리즘", 1, "Navier-Stokes", "Telea")),
            (s, p) =>
            {
                using var g = Gray(s);
                using var mask = new Mat(); Cv2.Threshold(g, mask, p.I(0), 255, ThresholdTypes.Binary);
                if (p.I(1) > 0) { using var k = Kernel(2, 3); Cv2.Dilate(mask, mask, k, iterations: p.I(1)); }
                p.Info = $"복원 영역: {Cv2.CountNonZero(mask) * 100.0 / mask.Total():0.0}%";
                return Out(d => Cv2.Inpaint(s, mask, d, p.D(2), p.I(3) == 0 ? InpaintTypes.NS : InpaintTypes.Telea));
            });

        Add(Noise, "푸리에 스펙트럼", "Cv2.Dft", "영상을 주파수 영역으로 변환한 크기 스펙트럼(로그 스케일)을 보여줍니다. 중심이 저주파, 바깥이 고주파입니다.",
            P(Param.Bool("컬러맵 적용", false)),
            (s, p) =>
            {
                using var f = EvenGrayF(s);
                using var cx = Out(d => Cv2.Dft(f, d, DftFlags.ComplexOutput));
                var pl = Cv2.Split(cx);
                using var mag = Out(d => Cv2.Magnitude(pl[0], pl[1], d));
                foreach (var m in pl) m.Dispose();
                Cv2.Add(mag, Scalar.All(1), mag);
                Cv2.Log(mag, mag);
                Shift(mag);
                var n = Out(d => Cv2.Normalize(mag, d, 0, 255, NormTypes.MinMax, MatType.CV_8U));
                if (!p.B(0)) return n;
                using (n) return Out(d => Cv2.ApplyColorMap(n, d, ColormapTypes.Magma));
            });

        Add(Noise, "주파수 필터 (저역/고역)", "Cv2.Dft + Cv2.Idft", "주파수 영역에서 원형 마스크를 곱해 저주파(부드러운 성분) 또는 고주파(에지·노이즈)만 통과시킵니다.",
            P(Param.Choice("종류", 0, "저역 통과 (Low-pass)", "고역 통과 (High-pass)"), Param.Choice("마스크", 1, "이상적 (Ideal)", "가우시안"), Param.Dbl("차단 반경", 1, 300, 30, 1)),
            (s, p) =>
            {
                using var f = EvenGrayF(s);
                int w = f.Cols, h = f.Rows;
                using var cx = Out(d => Cv2.Dft(f, d, DftFlags.ComplexOutput));
                var pl = Cv2.Split(cx);
                double r2 = p.D(2) * p.D(2);
                var mk = new float[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        double d2 = (x - w / 2.0) * (x - w / 2.0) + (y - h / 2.0) * (y - h / 2.0);
                        double v = p.I(1) == 0 ? (d2 <= r2 ? 1 : 0) : Math.Exp(-d2 / (2 * r2));
                        mk[y * w + x] = (float)(p.I(0) == 0 ? v : 1 - v);
                    }
                using var mask = new Mat(h, w, MatType.CV_32FC1);
                Marshal.Copy(mk, 0, mask.Data, mk.Length);
                foreach (var m in pl) { Shift(m); Cv2.Multiply(m, mask, m); Shift(m); }
                Cv2.Merge(pl, cx);
                foreach (var m in pl) m.Dispose();
                using var back = Out(d => Cv2.Idft(cx, d, DftFlags.Scale | DftFlags.RealOutput));
                if (p.I(0) == 0) return Out(d => back.ConvertTo(d, MatType.CV_8U));
                using var a = Out(d => Cv2.ConvertScaleAbs(back, d));
                return Out(d => Cv2.Normalize(a, d, 0, 255, NormTypes.MinMax));
            });
    }

    static void AddDetection()
    {
        Add(Detect, "QR 코드 검출 · 해독", "QRCodeDetector.DetectAndDecode", "영상에서 QR 코드를 찾아 위치를 표시하고 내용을 해독합니다. 해독된 문자열은 결과 정보에 표시됩니다.",
            P(Param.Bool("원본 위에 그리기", true)),
            (s, p) =>
            {
                using var det = new QRCodeDetector();
                using var b = Bgr(s);
                var text = det.DetectAndDecode(b, out Point2f[] pts);
                var d = Canvas(s, p, p.B(0));
                if (pts is { Length: 4 }) Cv2.Polylines(d, [pts.Select(Pt).ToArray()], true, new Scalar(80, 220, 120), 3, LineTypes.AntiAlias);
                p.Info = !string.IsNullOrEmpty(text) ? $"QR 내용: {text}" : pts is { Length: 4 } ? "QR 위치는 찾았지만 해독에 실패했습니다." : "QR 코드를 찾지 못했습니다.";
                return d;
            });

        Add(Detect, "HOG 보행자 검출", "HOGDescriptor.DetectMultiScale", "HOG 특징 + 선형 SVM(내장 모델)으로 서 있는 사람을 찾습니다. 사람 크기가 64×128 이상일 때 잘 동작합니다.",
            P(Param.Dbl("검출 임계값", -1, 3, 0, 0.05), Param.Dbl("스케일 단계", 1.01, 1.5, 1.05, 0.01), Param.Int("그룹 임계값", 0, 10, 2)),
            (s, p) =>
            {
                using var hog = new HOGDescriptor();
                hog.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
                using var b = Bgr(s);
                var found = hog.DetectMultiScale(b, p.D(0), new Size(8, 8), new Size(8, 8), p.D(1), p.I(2));
                var d = b.Clone();
                foreach (var r in found) Cv2.Rectangle(d, r, new Scalar(80, 220, 120), 2);
                p.Info = $"검출된 사람: {found.Length}명";
                return d;
            });

        Add(Detect, "템플릿 매칭 (중앙 패치)", "Cv2.MatchTemplate", "원본 중앙에서 잘라낸 패치를 템플릿으로 삼아 현재 영상에서 가장 비슷한 위치들을 찾습니다.",
            P(Param.Dbl("템플릿 크기 (%)", 5, 50, 15, 1), Param.Choice("방법", 5, "SqDiff", "SqDiff Normed", "CCorr", "CCorr Normed", "CCoeff", "CCoeff Normed"), Param.Dbl("일치 임계값", 0.3, 1, 0.8, 0.01)),
            (s, p) =>
            {
                using var o = SameAsInput(p.Original, s);
                using var src = Bgr(s);
                int tw = Math.Max(4, (int)(o.Cols * p.D(0) / 100)), th = Math.Max(4, (int)(o.Rows * p.D(0) / 100));
                var tr = new Rect((o.Cols - tw) / 2, (o.Rows - th) / 2, tw, th);
                using var tpl = new Mat(o, tr);
                var method = (TemplateMatchModes)p.I(1);
                using var res = Out(d => Cv2.MatchTemplate(src, tpl, d, method));
                using var n = Out(d => Cv2.Normalize(res, d, 0, 1, NormTypes.MinMax));
                if (method is TemplateMatchModes.SqDiff or TemplateMatchModes.SqDiffNormed) Cv2.Subtract(Scalar.All(1), n, n);
                using var hit = new Mat(); Cv2.Threshold(n, hit, p.D(2), 255, ThresholdTypes.Binary);
                using var hit8 = new Mat(); hit.ConvertTo(hit8, MatType.CV_8U);
                Cv2.FindContours(hit8, out Point[][] blobs, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var d = src.Clone();
                foreach (var bl in blobs) { var br = Cv2.BoundingRect(bl); Cv2.Rectangle(d, new Rect(br.X + br.Width / 2, br.Y + br.Height / 2, tw, th), new Scalar(80, 220, 120), 2); }
                Cv2.Rectangle(d, tr, new Scalar(255, 170, 60), 1);
                p.Info = $"일치 위치: {blobs.Length}곳 (주황 = 템플릿 영역)";
                return d;
            });
    }

    static void AddMix()
    {
        Add(Mix, "원본과 블렌딩", "Cv2.AddWeighted", "현재 결과와 원본 영상을 비율대로 섞습니다. 출력 = 결과 × α + 원본 × (1 − α)",
            P(Param.Dbl("결과 비율 α", 0, 1, 0.5, 0.01)),
            (s, p) => { using var o = SameAsInput(p.Original, s); using var b = Bgr(s); return Out(d => Cv2.AddWeighted(b, p.D(0), o, 1 - p.D(0), 0, d)); });

        Add(Mix, "원본과의 차이", "Cv2.Absdiff", "결과와 원본의 픽셀별 절댓값 차이를 보여줍니다. 처리로 무엇이 바뀌었는지 확인할 수 있습니다.",
            P(Param.Dbl("증폭", 1, 20, 3, 0.5)),
            (s, p) => { using var o = SameAsInput(p.Original, s); using var b = Bgr(s); using var df = Out(d => Cv2.Absdiff(b, o, d)); return Out(d => df.ConvertTo(d, MatType.CV_8U, p.D(0))); });

        Add(Mix, "마스크로 원본 추출", "Mat.CopyTo(dst, mask)", "현재 결과(이진 마스크)에서 0이 아닌 영역의 원본 픽셀만 남깁니다. 이진화 → 모폴로지 → 이 단계 순서로 객체를 오려내 보세요.",
            P(Param.Bool("마스크 반전", false)),
            (s, p) =>
            {
                using var o = SameAsInput(p.Original, s);
                using var g = Gray(s);
                using var mask = new Mat(); Cv2.Threshold(g, mask, 0, 255, p.B(0) ? ThresholdTypes.BinaryInv : ThresholdTypes.Binary);
                var d = new Mat(o.Size(), o.Type(), Scalar.All(0));
                o.CopyTo(d, mask);
                return d;
            });

        Add(Mix, "원본과 비트 연산", "Cv2.BitwiseAnd / Or / Xor", "결과와 원본을 비트 단위로 연산합니다.",
            P(Param.Choice("연산", 0, "AND", "OR", "XOR")),
            (s, p) =>
            {
                using var o = SameAsInput(p.Original, s);
                using var b = Bgr(s);
                return Out(d =>
                {
                    if (p.I(0) == 0) Cv2.BitwiseAnd(b, o, d);
                    else if (p.I(0) == 1) Cv2.BitwiseOr(b, o, d);
                    else Cv2.BitwiseXor(b, o, d);
                });
            });

        Add(Mix, "마스크 오버레이", "Cv2.AddWeighted + Mat.SetTo", "현재 결과(이진 마스크) 영역을 원본 위에 반투명 색으로 덮어 검출 결과를 확인합니다.",
            P(Param.Choice("색상", 0, "초록", "빨강", "파랑", "노랑"), Param.Dbl("불투명도", 0, 1, 0.5, 0.01)),
            (s, p) =>
            {
                using var o = SameAsInput(p.Original, s);
                using var g = Gray(s);
                using var mask = new Mat(); Cv2.Threshold(g, mask, 0, 255, ThresholdTypes.Binary);
                using var layer = o.Clone();
                layer.SetTo(new[] { new Scalar(80, 220, 80), new Scalar(60, 60, 255), new Scalar(255, 140, 60), new Scalar(60, 230, 255) }[p.I(0)], mask);
                return Out(d => Cv2.AddWeighted(layer, p.D(1), o, 1 - p.D(1), 0, d));
            });
    }
}
