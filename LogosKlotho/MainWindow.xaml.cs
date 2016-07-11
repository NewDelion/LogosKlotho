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
using System.Windows.Navigation;
using System.Windows.Shapes;

using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Document;
using System.Text.RegularExpressions;
using System.Drawing;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.ComponentModel;

namespace LogosKlotho
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private CompletionWindow completionWindow;
        private List<Directory_Class> directory_list = null;
        private List<File_Class> file_list = null;
        private List<Function_Class> function_list = null;

        private List<string> ord_word_list = null;
        private List<string> ord_function_list = null;
        private List<string> ord_def_function_list = null;


        private FindReplace.FindReplaceMgr FRManager = null;

        private Status status = null;


        private bool EnableAutoComplete = true;
        private bool EnableAutoIndent = true;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            using (var reader = new XmlTextReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("LogosKlotho.PHP-Mode.xshd")))
                textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
            textEditor.TextArea.KeyDown += textEditor_TextArea_KeyDown;
            textEditor.TextArea.Caret.PositionChanged += textEditor_TextArea_Caret_PositionChanged;

            status = new Status();
            text_line.DataContext = status;
            text_column.DataContext = status;

            textEditor.Focus();

            LoadWordListSetting();
            LoadUseHierarchy();
            LoadSetting();

            //ICSharpCode.AvalonEdit.Search.SearchPanel.Install(textEditor);

            FRManager = new FindReplace.FindReplaceMgr();
            FRManager.CurrentEditor = new FindReplace.TextEditorAdapter(textEditor);
            FRManager.ShowSearchIn = false;
            FRManager.OwnerWindow = this;
            CommandBindings.Add(FRManager.FindBinding);
            CommandBindings.Add(FRManager.ReplaceBinding);
            CommandBindings.Add(FRManager.FindNextBinding);

            if (file_name != "")
            {
                textEditor.Load(file_name);
                this.Title = System.IO.Path.GetFileName(file_name + " - LogosKlotho");
            }

            textEditor.FontFamily = new System.Windows.Media.FontFamily(Properties.Settings.Default.font_name);
            textEditor.FontSize = Properties.Settings.Default.font_size / 72.0 * 96.0;
            textEditor.FontStyle = Properties.Settings.Default.font_style == "normal" ?
                FontStyles.Normal : FontStyles.Italic;
            textEditor.FontWeight = Properties.Settings.Default.font_weight == "normal" ?
                FontWeights.Normal : FontWeights.Bold;



            
            
        }

        private void LoadWordListSetting()
        {
            if (!System.IO.Directory.Exists("setting"))
                System.IO.Directory.CreateDirectory("setting");

            if (!System.IO.File.Exists("setting\\ord_word_list"))
                System.IO.File.WriteAllText("setting\\ord_word_list", Properties.Resources.ord_word_list);
            if (!System.IO.File.Exists("setting\\ord_function_list"))
                System.IO.File.WriteAllText("setting\\ord_function_list", Properties.Resources.ord_function_list);
            if (!System.IO.File.Exists("setting\\ord_def_function_list"))
                System.IO.File.WriteAllText("setting\\ord_def_function_list", Properties.Resources.ord_def_function_list);


            ord_word_list = System.IO.File.ReadAllLines("setting\\ord_word_list").Where(d => d != "").ToList();
            ord_function_list = System.IO.File.ReadAllLines("setting\\ord_function_list").Where(d => d != "").ToList();
            ord_def_function_list = System.IO.File.ReadAllLines("setting\\ord_def_function_list").Where(d => d != "").ToList();
        }

        private void LoadSetting()
        {
            if (!Properties.Settings.Default.show_statusbar)
            {
                statusBar.Visibility = Visibility.Collapsed;
                textEditor.Margin = new Thickness(textEditor.Margin.Left, textEditor.Margin.Top, textEditor.Margin.Right, 0.0);
            }
            if (!Properties.Settings.Default.show_linenumber)
            {
                textEditor.ShowLineNumbers = false;
            }
            if (!Properties.Settings.Default.enable_autocomplete)
            {
                EnableAutoComplete = false;
            }
            if (!Properties.Settings.Default.enable_autoindent)
            {
                EnableAutoIndent = false;
            }
            if (Properties.Settings.Default.enable_wordwrap)
            {
                textEditor.WordWrap = true;
            }
        }

        

        private void GetWordList(IList<ICompletionData> data, string type = "all")
        {
            List<ICompletionData> result = new List<ICompletionData>();

            if (type == "word" || type == "all")
            {
                try
                {
                    MatchCollection mc = Regex.Matches(textEditor.Text, "use ([a-zA-Z0-9]+\\\\)*([a-zA-Z0-9]+)( as ([a-zA-Z0-9]+))?;");
                    foreach (Match m in mc)
                        result.Add(new WordCompletionData(m.Groups[4].Value == "" ? m.Groups[2].Value : m.Groups[4].Value, ToWPFBitmap(Properties.Resources.file)));
                    if (textEditor.SelectionStart >= 2)
                    {
                        mc = Regex.Matches(textEditor.Text.Substring(0, textEditor.SelectionStart - 2), @"\$(?<var>[a-zA-Z0-9_]+)");
                        foreach (Match m in mc)
                            result.Add(new WordCompletionData(m.Groups["var"].Value, "$" + m.Groups["var"].Value));
                    }
                }
                catch (Exception ex)
                {
                    Clipboard.SetText(ex.Message);
                    MessageBox.Show("エラーが発生しました。\nエラーメッセージをクリップボードにコピーしました。\n" + ex.Message);
                }

                foreach (string w in ord_word_list)
                    if (!result.ToList().Exists(d => d.Text.Equals(w)))
                        result.Add(new WordCompletionData(w));
            }
            if (type == "function" || type == "all")
            {
                try
                {
                    MatchCollection mc = Regex.Matches(textEditor.Text, "use ([a-zA-Z0-9]+\\\\)*([a-zA-Z0-9]+)( as ([a-zA-Z0-9]+))?;");
                    foreach (Match m in mc)
                    {
                        int owner_id = GetOwnerId(m.Groups[0].Value.Substring(4, m.Groups[0].Value.LastIndexOf('\\') - 3));
                        string class_name = m.Groups[2].Value;
                        foreach (Function_Class f in function_list.Where(d => d.owner_id == owner_id && d.owner_class.Equals(class_name)))
                            result.Add(new WordCompletionData(f.name, f.description));
                    }
                    mc = Regex.Matches(textEditor.Text, @"(?<description>function (?<name>[a-zA-Z0-9_]+) *\(.*\))");
                    foreach (Match m in mc)
                        result.Add(new WordCompletionData(m.Groups["name"].Value));
                }
                catch (Exception ex)
                {
                    Clipboard.SetText(ex.Message);
                    MessageBox.Show("エラーが発生しました。\nエラーメッセージをクリップボードにコピーしました。\n" + ex.Message);
                }

                foreach (string w in ord_function_list)
                    if (!result.ToList().Exists(d => d.Text.Equals(w)))
                        result.Add(new WordCompletionData(w));
            }
            if (type == "def_function" || type == "all")
            {
                try
                {
                    MatchCollection mc = Regex.Matches(textEditor.Text, "use ([a-zA-Z0-9]+\\\\)*([a-zA-Z0-9]+)( as ([a-zA-Z0-9]+))?;");
                    foreach (Match m in mc)
                    {
                        int owner_id = GetOwnerId(m.Groups[0].Value.Substring(4, m.Groups[0].Value.LastIndexOf('\\') - 3));
                        string class_name = m.Groups[2].Value;
                        foreach (Function_Class f in function_list.Where(d => d.owner_id == owner_id && d.owner_class.Equals(class_name)))
                            result.Add(new WordCompletionData(f.name, f.description));
                    }
                    mc = Regex.Matches(textEditor.Text, @"(?<description>function (?<name>[a-zA-Z0-9_]+) *\(.*\))");
                    foreach (Match m in mc)
                        result.Add(new WordCompletionData(m.Groups["name"].Value));
                }
                catch (Exception ex)
                {
                    Clipboard.SetText(ex.Message);
                    MessageBox.Show("エラーが発生しました。\nエラーメッセージをクリップボードにコピーしました。\n" + ex.Message);
                }

                foreach (string w in ord_function_list)
                    if (!result.ToList().Exists(d => d.Text.Equals(w)))
                        result.Add(new WordCompletionData(w));
            }

            result.Sort((a, b) => string.Compare(a.Text, b.Text));
            foreach (ICompletionData icd in result)
                if (!data.ToList().Exists(d => d.Text.Equals(icd.Text) && d.Description.Equals(icd.Description)))
                    data.Add(icd);
        }

        private void showCompletionWindow(string WordList = "all", bool AutoSelect = false, IList<ICompletionData> list = null)
        {
            completionWindow = new CompletionWindow(textEditor.TextArea);
            completionWindow.StartOffset = GetStartOffset(completionWindow.StartOffset);
            completionWindow.EndOffset = GetEndOffset(completionWindow.EndOffset);
            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            if (list == null)
                GetWordList(data, WordList);
            else
                foreach (ICompletionData compData in list)
                    data.Add(compData);
            if (AutoSelect)
            {
                string input = textEditor.Text.Substring(completionWindow.StartOffset, completionWindow.EndOffset - completionWindow.StartOffset);
                if (input != "")
                {
                    if (textEditor.SelectionStart != 0 && textEditor.SelectionStart != completionWindow.StartOffset)
                    {
                        textEditor.SelectionStart = textEditor.SelectionStart - 1;
                        textEditor.SelectionStart = textEditor.SelectionStart + 1;
                    }
                    if (completionWindow.CompletionList.ListBox.Items.Count == 0)
                    {
                        completionWindow.Close();
                        completionWindow = null;
                    }
                    else
                    {
                        completionWindow.Show();
                        completionWindow.CompletionList.SelectionChanged += completionWindow_CompletionList_SelectionChanged;
                        completionWindow.Closed += delegate { completionWindow = null; };
                    }
                }
                else
                {
                    completionWindow.Show();
                    completionWindow.CompletionList.SelectionChanged += completionWindow_CompletionList_SelectionChanged;
                    completionWindow.Closed += delegate { completionWindow = null; };
                }
            }
            else
            {
                completionWindow.Show();
                completionWindow.CompletionList.SelectionChanged += completionWindow_CompletionList_SelectionChanged;
                completionWindow.Closed += delegate { completionWindow = null; };
            }
        }

        private int GetStartOffset(int offset)
        {
            int result = offset;

            while (result > 0)
            {
                if (textEditor.Text.Length > 1 && !char.IsLetterOrDigit(textEditor.Text[result - 1]))
                    break;
                result--;
            }

            return result;
        }

        private int GetEndOffset(int offset)
        {
            int result = offset;

            while (result < textEditor.Text.Length)
            {
                if (!char.IsLetterOrDigit(textEditor.Text[result]))
                    break;
                result++;
            }

            return result;
        }

        private string GetCurrentWord(int offset)
        {
            int start = GetStartOffset(offset);
            int end = GetEndOffset(offset);
            return textEditor.Text.Substring(start, end - start);
        }

        private string GetLineText(int offset)
        {
            int start = offset;
            while (start > 0)
            {
                if (textEditor.Text.Length > 1 && textEditor.Text[start - 1] == '\n')
                    break;
                start--;
            }

            int end = offset;
            while (textEditor.Text.Length > end)
            {
                if (textEditor.Text[end] == '\r' || textEditor.Text[end] == '\n')
                    break;
                end++;
            }

            return textEditor.Text.Substring(start, end - start);
        }

        private int GetLineStart(int offset)
        {
            int start = offset;
            while (start > 0)
            {
                if (textEditor.Text.Length > 1 && textEditor.Text[start - 1] == '\n')
                    break;
                start--;
            }
            return start;
        }

        private string GetLineToOffset(int offset)
        {
            int start = GetLineStart(offset);
            return textEditor.Text.Substring(start, offset - start);
        }

        
        /// <summary>
        /// -2 => 候補なし, -1 => pocketmine
        /// </summary>
        private int GetOwnerId(string path)
        {
            if (path == "")
                return -1;
            int owner_id = -1;
            string[] path_array = path.Split('\\');
            for (int i = 0; i < path_array.Length; i++)
            {
                if (path_array[i] == "")
                    break;
                List<Directory_Class> ds = GetDirectoriesFromOwnerId(owner_id);
                if (ds == null)
                    return -2;
                var dir = ds.Find(d => d.name.Equals(path_array[i]));
                if (dir == null)
                    return -2;
                owner_id = dir.id;
            }

            return owner_id;
        }

        private void LoadUseHierarchy()
        {
            if (!System.IO.Directory.Exists("hierarchy"))
            {
                DownloadingDatabase dd = new DownloadingDatabase();
                dd.ShowDialog();
                if (dd.Fail)
                {
                    this.Close();
                    return;
                }
            }

            directory_list = new List<Directory_Class>();
            file_list = new List<File_Class>();
            function_list = new List<Function_Class>();
            ReadHierarchy("hierarchy", current_owner_id);
            return;

            //directory_list = new List<Directory_Class>();
            //file_list = new List<File_Class>();
            //string[] file_lines = Properties.Resources.file_list.Split('\n');
            //for (int i = 0; i < file_lines.Length; i += 2)
            //    file_list.Add(new File_Class(file_lines[i], int.Parse(file_lines[i + 1])));
            //string[] directory_lines = Properties.Resources.directory_list.Split('\n');
            //for (int i = 0; i < directory_lines.Length; i += 3)
            //    directory_list.Add(new Directory_Class(int.Parse(directory_lines[i]), directory_lines[i + 1], int.Parse(directory_lines[i + 2])));
        }

        private int current_owner_id = -1;
        private void ReadHierarchy(string path, int owner_id)
        {
            foreach (string f in System.IO.Directory.GetFiles(path))
            {
                string class_name = System.IO.Path.GetFileName(f);
                file_list.Add(new File_Class(class_name, owner_id));
                using (System.IO.StreamReader reader = new System.IO.StreamReader(f))
                    while (reader.Peek() != -1)
                        function_list.Add(new Function_Class(owner_id, class_name, reader.ReadLine(), reader.ReadLine()));
            }
            foreach (string d in System.IO.Directory.GetDirectories(path))
            {
                directory_list.Add(new Directory_Class(++current_owner_id, System.IO.Path.GetFileName(d), owner_id));
                ReadHierarchy(d, current_owner_id);
            }
        }

        private List<Directory_Class> GetDirectoriesFromOwnerId(int owner_id)
        {
            var result = this.directory_list.Where(d => d.owner_id == owner_id);
            return result == null ? null : result.ToList();
        }

        private List<File_Class> GetFilesFromOwnerId(int owner_id)
        {
            var result = this.file_list.Where(d => d.owner_id == owner_id);
            return result == null ? null : result.ToList();
        }

        private void textEditor_TextArea_KeyDown(object sender, KeyEventArgs e)
        {
            if (EnableAutoComplete && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.Space && completionWindow == null)
            {
                showCompletionWindow("all", true);
                e.Handled = true;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.M)
            {
                //Console.WriteLine();
                //FRManager.ShowAsFind();
                
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.J)
            {
                FRManager.FindNext();
                e.Handled = true;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.K)
            {
                FRManager.FindPrevious();
                e.Handled = true;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.O)
            {
                Open();
                e.Handled = true;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None && e.Key == Key.S)
            {
                SaveAs();
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.S)
            {
                Save();
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.OemPlus)
            {
                textEditor.FontSize += 1.0;
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None && e.Key == Key.OemMinus)
            {
                textEditor.FontSize -= 1.0;
                e.Handled = true;
            }
        }

        private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (EnableAutoComplete && e.Text == ">" && textEditor.Text.Length > 1 && textEditor.Text[textEditor.SelectionStart - 2] == '-')
            {
                showCompletionWindow("function");
            }
            if (EnableAutoComplete && completionWindow == null && char.IsLetterOrDigit(e.Text[0]) && GetCurrentWord(textEditor.SelectionStart).Length == 1)
            {
                int start = GetStartOffset(textEditor.SelectionStart);
                if (start > 2 && textEditor.Text[start - 1] == '>' && textEditor.Text[start - 2] == '-')
                    showCompletionWindow("function", true);
                else if (start > 9 && textEditor.Text.Substring(start - 9, 9).Equals("function "))
                    showCompletionWindow("def_function", true);
                else if (GetLineText(start).TrimStart().IndexOf("use ") == 0)
                {
                    int use_start = textEditor.Text.LastIndexOf("use ", textEditor.SelectionStart);
                    string use_path = textEditor.Text.Substring(use_start + 4, textEditor.SelectionStart - use_start - 5);
                    int owner_id = GetOwnerId(use_path);
                    List<ICompletionData> wordList = new List<ICompletionData>();
                    var ds = GetDirectoriesFromOwnerId(owner_id);
                    if (ds != null)
                        foreach (var d in ds)
                            wordList.Add(new WordCompletionData(d.name, ToWPFBitmap(Properties.Resources.folder)));
                    var fs = GetFilesFromOwnerId(owner_id);
                    if (fs != null)
                        foreach (var f in fs)
                            wordList.Add(new WordCompletionData(f.name, ToWPFBitmap(Properties.Resources.file)));
                    if (use_path == "")
                    {
                        if (textEditor.Text[textEditor.SelectionStart - 1] == 'p')
                            showCompletionWindow("use_", true, wordList);
                    }
                    else if (wordList.Count > 0)
                        showCompletionWindow("use_", true, wordList);
                    //use_path == "" => textEditor.Text[textEditor.SelectionStart - 1] == 'p'
                }
                else if (!textEditor.TextArea.Selection.IsMultiline)
                    showCompletionWindow("word", true);
            }
            else if (EnableAutoIndent && e.Text == "\n")
            {
                bool check = true;
                bool newLineOnce = false;
                int index = textEditor.SelectionStart - 1;
                while (0 <= --index && check)
                {
                    if (textEditor.Text[index] == ';' || textEditor.Text[index] == '}')
                        check = false;
                    else if (textEditor.Text[index] == '\n' && !newLineOnce)
                        newLineOnce = true;
                    else if (textEditor.Text[index] == '\n' && newLineOnce)
                        check = false;
                    else if (textEditor.Text[index] == '\r' || textEditor.Text[index] == '\t' || textEditor.Text[index] == ' ')
                        continue;
                    else if (index >= 4 && textEditor.Text.Substring(index - 4, 5).Equals("<?php"))
                        check = false;
                    else
                        break;
                }

                if (index > 0 && check)
                {
                    string line = GetLineText(textEditor.SelectionStart);
                    int start = GetLineStart(textEditor.SelectionStart);
                    textEditor.Document.Insert(start, "\t");
                    //DocumentLine dline=textEditor.Document.GetLineByOffset()
                }
            }
            else if (EnableAutoIndent && e.Text == "}")
            {
                int index = textEditor.SelectionStart - 1;
                bool ok = true;
                while (0 <= --index && ok)
                {
                    if (textEditor.Text[index] == '\r' || textEditor.Text[index] == '\n')
                        break;
                    else if (textEditor.Text[index] == ' ' || textEditor.Text[index] == '\t')
                        continue;
                    else
                        ok = false;
                }
                if (index > 0 && ok)
                {
                    string indent = GetLineToOffset(textEditor.SelectionStart);
                    int space = textEditor.Text.LastIndexOf("    ", textEditor.SelectionStart - 1);
                    int tab = textEditor.Text.LastIndexOf("\t", textEditor.SelectionStart - 1);
                    if (space >= 0)
                        textEditor.Document.Remove(space, 4);
                    else if (tab >= 0)
                        textEditor.Document.Remove(tab, 1);
                }
            }
        }

        private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    //completionWindow.CompletionList.RequestInsertion(e);
                    completionWindow.Close();
                    completionWindow = null;
                }
            }
        }
        
        private void completionWindow_CompletionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (completionWindow.CompletionList.ListBox.Items.Count == 0)
            {
                completionWindow.Close();
                completionWindow = null;
            }
        }

        private void textEditor_TextArea_Caret_PositionChanged(object sender, EventArgs e)
        {
            status.Line = textEditor.TextArea.Caret.Line;
            status.Column = textEditor.TextArea.Caret.Column;
        }

        public class WordCompletionData : ICompletionData
        {
            public System.Windows.Media.ImageSource Image { get; private set; }
            public string Text { get; private set; }

            // 以下のプロパティはリスト項目としてテキスト以外を設定する場合に使うことになる
            public object Content { get { return this.Text; } }
            public object Description { get; private set; }

            public double Priority { get { return 0; } }

            public WordCompletionData(string text, ImageSource imageSource = null)
            {
                this.Text = text;
                this.Description = "Description for " + this.Text;
                this.Image = imageSource;
            }

            public WordCompletionData(string text, string description)
            {
                this.Text = text;
                this.Description = description;
            }

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, this.Text);
            }
        }

        public class Directory_Class
        {
            public int id;
            public string name;
            public int owner_id;

            public Directory_Class(int id, string name, int owner)
            {
                this.id = id;
                this.name = name;
                this.owner_id = owner;
            }
        }

        public class File_Class
        {
            public string name;
            public int owner_id;

            public File_Class(string name, int owner)
            {
                this.name = name;
                this.owner_id = owner;
            }
        }

        public class Function_Class
        {
            public int owner_id;
            public string owner_class;
            public string name;
            public string description;

            public Function_Class(int owner, string owner_class, string name, string description)
            {
                this.owner_id = owner;
                this.owner_class = owner_class;
                this.name = name;
                this.description = description;
            }
        }

        public class Status : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private int line = 1;
            private int column = 1;

            public int Line
            {
                get { return line; }
                set { line = value; NotifiyPropertyChanged("Line"); }
            }

            public int Column
            {
                get { return column; }
                set { column = value; NotifiyPropertyChanged("Column"); }
            }

            //イベントが実装されてれば実行する。
            private void NotifiyPropertyChanged(String propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToWPFBitmap(Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();

            BitmapSource source;
            try
            {
                source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
            return source;
        }

        private void Menu_Search(object sender, RoutedEventArgs e)
        {
            FRManager.ShowAsFind();
        }

        private void Menu_SearchNext(object sender, RoutedEventArgs e)
        {
            FRManager.FindNext();
        }

        private void Menu_SearchPrev(object sender, RoutedEventArgs e)
        {
            FRManager.FindPrevious();
        }

        private void Menu_Replace(object sender, RoutedEventArgs e)
        {
            FRManager.ShowAsReplace();
        }

        public static string file_name = "";
        private void Menu_Open(object sender, RoutedEventArgs e)
        {
            Open();
        }

        private void Menu_Save(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Menu_SaveAs(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private void Menu_New(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private void Menu_Browse_Database(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("hierarchy");
        }

        private void Menu_Browse_Setting(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("setting");
        }

        private void Menu_Font(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FontDialog fontDialog1 = new System.Windows.Forms.FontDialog();
            fontDialog1.ShowColor = true;
            fontDialog1.AllowVerticalFonts = false;
            fontDialog1.AllowVectorFonts = true;
            fontDialog1.FontMustExist = true;

            fontDialog1.Font = new Font(textEditor.FontFamily.Source, (float)(textEditor.FontSize / 96.0 * 72.0));

            if (fontDialog1.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                textEditor.FontFamily = new System.Windows.Media.FontFamily(fontDialog1.Font.FontFamily.Name);
                textEditor.FontSize = fontDialog1.Font.SizeInPoints / 72.0 * 96.0;
                textEditor.FontStyle = (fontDialog1.Font.Style & System.Drawing.FontStyle.Italic) == 0 ?
                    FontStyles.Normal : FontStyles.Italic;
                textEditor.FontWeight = (fontDialog1.Font.Style & System.Drawing.FontStyle.Bold) == 0 ?
                    FontWeights.Normal : FontWeights.Bold;

                Properties.Settings.Default.font_name = fontDialog1.Font.FontFamily.Name;
                Properties.Settings.Default.font_size = fontDialog1.Font.SizeInPoints;
                Properties.Settings.Default.font_style = textEditor.FontStyle == FontStyles.Normal ? "normal" : "italic";
                Properties.Settings.Default.font_weight = textEditor.FontWeight == FontWeights.Normal ? "normal" : "bold";
                Properties.Settings.Default.Save();
            }
        }

        private void Menu_Setting(object sender, RoutedEventArgs e)
        {
            SettingWindow setting = new SettingWindow();
            setting.ShowStatusBar = Properties.Settings.Default.show_statusbar;
            setting.ShowLineNumber = Properties.Settings.Default.show_linenumber;
            setting.EnableAutoComplete = Properties.Settings.Default.enable_autocomplete;
            setting.EnableAutoIndent = Properties.Settings.Default.enable_autoindent;
            setting.EnableWordWrap = Properties.Settings.Default.enable_wordwrap;
            setting.ShowDialog();
            bool change = false;
            if(Properties.Settings.Default.show_statusbar != setting.ShowStatusBar)
            {
                statusBar.Visibility = setting.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;
                textEditor.Margin = new Thickness(textEditor.Margin.Left, textEditor.Margin.Top, textEditor.Margin.Right, setting.ShowStatusBar ? statusBar.Height : 0.0);
                Properties.Settings.Default.show_statusbar = setting.ShowStatusBar;
                change = true;
            }
            if (Properties.Settings.Default.show_linenumber != setting.ShowLineNumber)
            {
                textEditor.ShowLineNumbers = setting.ShowLineNumber;
                Properties.Settings.Default.show_linenumber = setting.ShowLineNumber;
                change = true;
            }
            if (Properties.Settings.Default.enable_autocomplete != setting.EnableAutoComplete)
            {
                EnableAutoComplete = setting.EnableAutoComplete;
                Properties.Settings.Default.enable_autocomplete = setting.EnableAutoComplete;
                change = true;
            }
            if (Properties.Settings.Default.enable_autoindent != setting.EnableAutoIndent)
            {
                EnableAutoIndent = setting.EnableAutoIndent;
                Properties.Settings.Default.enable_autoindent = setting.EnableAutoIndent;
                change = true;
            }
            if (Properties.Settings.Default.enable_wordwrap != setting.EnableWordWrap)
            {
                textEditor.WordWrap = setting.EnableWordWrap;
                Properties.Settings.Default.enable_wordwrap = setting.EnableWordWrap;
                change = true;
            }
            if (change)
                Properties.Settings.Default.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (textEditor.IsModified)
            {
                MessageBoxResult result = MessageBox.Show("内容が変更されています。変更を保存しますか？", "確認ダイアログ", MessageBoxButton.YesNoCancel);
                if(result == MessageBoxResult.Yes)
                {
                    Save();
                }
                else if(result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void Open()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "ファイルを開く";
            dialog.Filter = "全てのファイル(*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                if (file_name != "")
                    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location, dialog.FileName);
                else
                {
                    textEditor.Load(dialog.FileName);
                    file_name = dialog.FileName;
                    this.Title = System.IO.Path.GetFileName(file_name + " - LogosKlotho");
                }
            }
        }

        private void Save()
        {
            if (file_name == "")
            {
                var dialog = new SaveFileDialog();
                dialog.Title = "ファイルを保存";
                dialog.Filter = "PHPファイル(*.php)|*.php|全てのファイル(*.*)|*.*";
                if (dialog.ShowDialog() == true)
                {
                    file_name = dialog.FileName;
                    this.Title = System.IO.Path.GetFileName(file_name + " - LogosKlotho");
                }
                else
                    return;
            }
            textEditor.Save(file_name);
        }

        private void SaveAs()
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "ファイルを保存";
            dialog.Filter = "PHPファイル(*.php)|*.php|全てのファイル(*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                file_name = dialog.FileName;
                textEditor.Save(file_name);
                this.Title = System.IO.Path.GetFileName(file_name + " - LogosKlotho");
            }
        }

        
    }

    public class StatusLineConvert : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "Line: " + ((int)value).ToString();
        }
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int a = 0;
            int.TryParse(((string)value).Substring(0, ((string)value).Length - 1), out a);
            return a;
        }
    }

    public class StatusColumnConvert : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "Column: " + ((int)value).ToString();
        }
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int a = 0;
            int.TryParse(((string)value).Substring(0, ((string)value).Length - 1), out a);
            return a;
        }
    }
}
