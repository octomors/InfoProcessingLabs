using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Forms;
using System.Drawing;

class Program
{
    [STAThread]
    static void Main()
    {
        using var src = GetImageFromFile() ?? GetImageFromClipboard();
        if (src == null) return;

        using var dst = ProcessImage(src);

        int holes = CountHoles(dst);
        Console.WriteLine($"Holes: {holes}");

        ShowImage(dst);

        Cv2.WaitKey(0);
        Cv2.DestroyAllWindows();
    }

    static Mat? GetImageFromClipboard()
    {
        if (!Clipboard.ContainsImage()) return null;
        using var bmp = Clipboard.GetImage() as Bitmap;
        if (bmp == null) return null;

        var mat = BitmapConverter.ToMat(bmp);
        Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY);
        return mat;
    }

    static Mat? GetImageFromFile()
    {
        var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.bmp" };
        if (dlg.ShowDialog() != DialogResult.OK) return null;
        return Cv2.ImRead(dlg.FileName, ImreadModes.Grayscale);
    }

    // Только бинаризация
    static Mat ProcessImage(Mat src)
    {
        // Узнаём порог Оцу
        using var tmp = new Mat();
        double otsuT = Cv2.Threshold(src, tmp, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        // "Подкрутка": +20% к порогу Оцу (можно поменять на +N)
        double t = Math.Clamp(otsuT * 1.2, 0.0, 255.0);

        // Финальная бинаризация уже фиксированным t
        Mat dst = new Mat();
        Cv2.Threshold(src, dst, t, 255, ThresholdTypes.Binary);

        return dst; // 0/255, один канал
    }

    /// <summary>
    /// Считает количество отверстий (белые отверстия в черной детали) по бинарному изображению.
    /// Вход: бинарное 0/255.
    /// Ожидается: деталь = черная (0), отверстия = белые (255), фон = белый (255).
    /// </summary>
    static int CountHoles(Mat binary)
    {
        if (binary.Empty()) return 0;

        // Приведём к маске "деталь белая", чтобы применять формулу H = C - χ
        // Тут деталь чёрная => инвертируем: деталь=255, фон/отверстия=0
        using var partMask = new Mat();
        Cv2.BitwiseNot(binary, partMask);

        // Добавим чёрную рамку, чтобы фон точно был "снаружи" (уменьшает артефакты на границе)
        Cv2.CopyMakeBorder(partMask, partMask, 1, 1, 1, 1, BorderTypes.Constant, Scalar.Black);

        int components = CountWhiteComponents4(partMask);
        int chi = EulerCharacteristic4Connected(partMask);

        int holes = components - chi;
        if (holes < 0) holes = 0;
        return holes;
    }

    static int CountWhiteComponents4(Mat mask)
    {
        using var labels = new Mat();
        int n = Cv2.ConnectedComponents(mask, labels, PixelConnectivity.Connectivity4, MatType.CV_32S);
        return n - 1; // минус фон
    }

    static int EulerCharacteristic4Connected(Mat mask)
    {
        if (mask.Type() != MatType.CV_8UC1)
            throw new ArgumentException("mask must be CV_8UC1 (binary 0/255).");

        int interior = 0;
        int exterior = 0;

        for (int y = 0; y < mask.Rows - 1; y++)
        {
            for (int x = 0; x < mask.Cols - 1; x++)
            {
                int sum = 0;

                if (mask.At<byte>(y, x) != 0) sum++;
                if (mask.At<byte>(y, x + 1) != 0) sum++;
                if (mask.At<byte>(y + 1, x) != 0) sum++;
                if (mask.At<byte>(y + 1, x + 1) != 0) sum++;

                if (sum == 3) interior++;
                if (sum == 1) exterior++;
            }
        }

        return (exterior - interior) / 4;
    }

    static void ShowImage(Mat img)
    {
        Cv2.ImShow("Result", img);
    }
}