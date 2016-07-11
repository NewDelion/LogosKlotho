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

        public bool ShowStatusBar { get; set; } = true;

        public bool ShowLineNumber { get; set; } = true;

        public bool EnableAutoComplete { get; set; } = true;

        public bool EnableAutoIndent { get; set; } = true;

        public bool EnableWordWrap { get; set; } = false;
    }
}
