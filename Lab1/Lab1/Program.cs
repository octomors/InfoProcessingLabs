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

    static Mat ProcessImage(Mat src)
    {
        Mat dst = new Mat();
        Cv2.Threshold(src, dst, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        return dst;
    }

    static void ShowImage(Mat img)
    {
        Cv2.ImShow("Result", img);
    }
}