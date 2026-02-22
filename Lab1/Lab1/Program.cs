using System.Windows.Forms;

[STAThread]
static void Main()
{
    using var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.png" };
    if (dialog.ShowDialog() == DialogResult.OK)
    {
        ProcessImage(dialog.FileName);
    }
}