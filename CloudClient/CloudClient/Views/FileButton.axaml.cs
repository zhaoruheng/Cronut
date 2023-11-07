using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace CloudClient.Views
{
    public partial class FileButton : UserControl
    {
        public FileButton()
        {
            InitializeComponent();
        }

        //以系统默认的方式打开文件
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {

            var fileButtonText = this.FindControl<TextBlock>("FileButtonTextBlock").Text;
            string folderPath = ConfigurationManager.AppSettings["TargetDir"].ToString();
            string filePath = Path.Combine(folderPath, fileButtonText);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
            };

            Process.Start(processStartInfo);
        }
    }
}