using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LogosKlotho
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 1)
            {
                if (System.IO.File.Exists(e.Args[0]))
                {
                    LogosKlotho.MainWindow.file_name = e.Args[0];
                }
                else
                {
                    MessageBox.Show("ファイルが見つかりませんでした。");
                }
            }
        }
    }
}
