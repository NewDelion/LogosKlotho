using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogosKlotho
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        public bool ShowStatusBar { get; set; }

        public bool ShowLineNumber { get; set; }

        public bool EnableAutoComplete { get; set; }

        public bool EnableAutoIndent { get; set; }

        public bool EnableWordWrap { get; set; }

        public bool ShowNewLine { get; set; }
        public string NewLine { get; set; }

        public bool ShowTab { get; set; }
        public string Tab { get; set; }

        public bool ShowSpace { get; set; }
        public string Space { get; set; }

        public bool ShowSpaceJpn { get; set; }
        public string SpaceJpn { get; set; }

        public void Button_Export(object sender, RoutedEventArgs e)
        {
            Export();
        }

        public void Button_Import(object sender, RoutedEventArgs e)
        {
            Import();
        }

        private void Import()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "ファイルを開く";
            dialog.Filter = "全てのファイル(*.*)|*.*";
            dialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory() + "\\setting";
            if (dialog.ShowDialog() == true)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(dialog.FileName))
                {
                    ShowStatusBar = reader.ReadLine().Equals("1");
                    ShowLineNumber = reader.ReadLine().Equals("1");
                    EnableAutoComplete = reader.ReadLine().Equals("1");
                    EnableAutoIndent = reader.ReadLine().Equals("1");
                    EnableWordWrap = reader.ReadLine().Equals("1");
                    ShowNewLine = reader.ReadLine().Equals("1");
                    NewLine = reader.ReadLine();
                    ShowTab = reader.ReadLine().Equals("1");
                    Tab = reader.ReadLine();
                    ShowSpace = reader.ReadLine().Equals("1");
                    Space = reader.ReadLine();
                    ShowSpaceJpn = reader.ReadLine().Equals("1");
                    SpaceJpn = reader.ReadLine();
                }
            }
        }

        private void Export()
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "ファイルを保存";
            dialog.Filter = "設定ファイル(*.conf)|*.conf|全てのファイル(*.*)|*.*";
            dialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory() + "\\setting";
            if (dialog.ShowDialog() == true)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(dialog.FileName))
                {
                    write(writer, ShowStatusBar);
                    write(writer, ShowLineNumber);
                    write(writer, EnableAutoComplete);
                    write(writer, EnableAutoIndent);
                    write(writer, EnableWordWrap);
                    write(writer, ShowNewLine);
                    write(writer, NewLine);
                    write(writer, ShowTab);
                    write(writer, Tab);
                    write(writer, ShowSpace);
                    write(writer, Space);
                    write(writer, ShowSpaceJpn);
                    write(writer, SpaceJpn);
                }
            }
        }

        private void write(System.IO.StreamWriter writer, bool flg, bool writeline = true)
        {
            if (writeline)
                writer.WriteLine(flg ? "1" : "0");
            else
                writer.Write(flg ? "1" : "0");
        }
        private void write(System.IO.StreamWriter writer, string str, bool writeline = true)
        {
            if (writeline)
                writer.WriteLine(str);
            else
                writer.Write(str);
        }

        
    }
}
