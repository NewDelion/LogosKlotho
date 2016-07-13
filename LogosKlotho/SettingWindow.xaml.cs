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
    }
}
