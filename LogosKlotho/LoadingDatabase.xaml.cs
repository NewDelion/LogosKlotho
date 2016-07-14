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
    /// LoadingDatabase.xaml の相互作用ロジック
    /// </summary>
    public partial class LoadingDatabase : Window
    {
        private System.Timers.Timer timer = new System.Timers.Timer();
        private int waiting_counter = 0;
        public LoadingDatabase()
        {
            InitializeComponent();

            Text = "データベースを読み込んでいます";
            DataContext = this;
        }

        private string Text { get; set; }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Text += ".";
            if (++waiting_counter >= 5)
            {
                waiting_counter = 0;
                Text = "データベースを読み込んでいます";
            }
        }

        private bool canClose = false;
        private async void LoadHierarchy()
        {
            await Task.Run(() => { ReadHierarchy("hierarchy", current_owner_id); });
            canClose = true;
            this.Close();
        }

        public MainWindow owner = null;
        private int current_owner_id = -1;
        private void ReadHierarchy(string path, int owner_id)
        {
            foreach (string f in System.IO.Directory.GetFiles(path))
            {
                string class_name = System.IO.Path.GetFileName(f);
                owner.file_list.Add(new MainWindow.File_Class(class_name, owner_id));
                using (System.IO.StreamReader reader = new System.IO.StreamReader(f))
                    while (reader.Peek() != -1)
                        owner.function_list.Add(new MainWindow.Function_Class(owner_id, class_name, reader.ReadLine(), reader.ReadLine()));
            }
            foreach (string d in System.IO.Directory.GetDirectories(path))
            {
                owner.directory_list.Add(new MainWindow.Directory_Class(++current_owner_id, System.IO.Path.GetFileName(d), owner_id));
                ReadHierarchy(d, current_owner_id);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !canClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            owner.directory_list = new List<MainWindow.Directory_Class>();
            owner.file_list = new List<MainWindow.File_Class>();
            owner.function_list = new List<MainWindow.Function_Class>();
            timer.Interval = 800.0;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            LoadHierarchy();
        }
    }
}
