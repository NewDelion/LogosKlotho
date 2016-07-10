using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogosKlotho
{
    /// <summary>
    /// DownloadingDatabase.xaml の相互作用ロジック
    /// </summary>
    public partial class DownloadingDatabase : Window, INotifyPropertyChanged
    {
        string url = "https://github.com/NewDelion/LogosKlotho/raw/master/hierarchy.zip";
        System.Net.WebClient client = null;
        private int progress_value = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Fail = false;

        public int ProgressValue
        {
            get
            {
                return this.progress_value;
            }
            set
            {
                this.progress_value = value;
                PropertyChanged(this, new PropertyChangedEventArgs(""));
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        const int GWL_STYLE = -16;
        const int WS_SYSMENU = 0x80000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(handle, GWL_STYLE);
            style = style & (~WS_SYSMENU);
            SetWindowLong(handle, GWL_STYLE, style);
        }

        public DownloadingDatabase()
        {
            InitializeComponent();

            client = new System.Net.WebClient();
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            client.DownloadFileCompleted += client_DownloadFileCompleted;
            progress1.Maximum = 110;
            progress1.Minimum = 0;

            this.DataContext = this;

            client.DownloadFileAsync(new Uri(url), "hierarchy.zip");
        }

        private async void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            ProgressValue = 100;
            try
            {
                if (!System.IO.File.Exists("hierarchy.zip"))
                {
                    Fail = true;
                    MessageBox.Show("データベースファイルが見つかりませんでした。");
                }
                else if (new System.IO.FileInfo("hierarchy.zip").Length == 0)
                {
                    Fail = true;
                    MessageBox.Show("データベースがダウンロードできませんでした。ネットワーク接続を確認してください。");
                }
            }
            catch (Exception ex)
            {
                Fail = true;
                MessageBox.Show(ex.Message);
            }
            if (!Fail)
            {
                status.Text = "データベースファイルの解凍中...";
                await Task.Run(() =>
                {
                    try
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory("hierarchy.zip", "hierarchy");
                    }
                    catch (Exception exc)
                    {
                        Fail = true;
                        MessageBox.Show(exc.Message);
                        if (System.IO.Directory.Exists("hierarchy"))
                            System.IO.Directory.Delete("hierarchy");
                    }
                    finally
                    {
                        if (System.IO.File.Exists("hierarchy.zip"))
                            System.IO.File.Delete("hierarchy.zip");
                    }
                });
                ProgressValue = 110;
            }
            canClose = true;
            this.Close();
        }

        private void client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            ProgressValue = e.ProgressPercentage;
        }

        private bool canClose = false;
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !canClose;
        }
    }
}
