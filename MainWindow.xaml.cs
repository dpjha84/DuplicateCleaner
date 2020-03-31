using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;

namespace DuplicateCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, List<FileInfo>> dupDataDict = new Dictionary<string, List<FileInfo>>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            statusLebel.Text = "Scanning...";
            dupDataDict.Clear();
            ic.ItemsSource = dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length));
            Task.Run(() => FindDuplicates());
        }
        private string GetFileHash(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return Encoding.Default.GetString(md5.ComputeHash(stream));
                }
            }
        }
        

        private void FindDuplicates()
        {
            var dataDict = new Dictionary<string, List<FileInfo>>();
            List<string> exclusionList = new List<string>();
            int sz = 0;
            var extList = new HashSet<string>();
            //extList.Add("avi");
            ////extList.Add("mkv");
            //extList.Add("mp4");
            ////extList.Add("jpg");
            //extList.Add("vob");
            // extList.Add("txt");

            var dirName = "";
            Dispatcher.Invoke(() =>
            {
                dirName = textBox2.Text;
            });
            Parallel.ForEach(SafeFileEnumerator.EnumerateFiles(dirName, SearchOption.AllDirectories, extList, exclusionList, sz), file1 =>
            //foreach (var file1 in SafeFileEnumerator.EnumerateFiles(dirName, SearchOption.AllDirectories, extList, exclusionList, sz))
            {
                var hash = GetFileHash(file1);
                if (dataDict.ContainsKey(hash))
                {
                    if (!dupDataDict.ContainsKey(hash))
                        dupDataDict.Add(hash, dataDict[hash]);
                    dupDataDict[hash].Add(new FileInfo(file1));
                    Dispatcher.Invoke(() =>
                    {
                        ic.ItemsSource = dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length));
                    });
                }
                else
                {
                    dataDict.Add(hash, new List<FileInfo> { new FileInfo(file1) });
                }
            }
            );
            Dispatcher.Invoke(() =>
            {
                statusLebel.Text = dupDataDict.Count + " record(s).";
            });
        }

        List<string> deleteList = new List<string>();
        Queue<string> selectedDirList = new Queue<string>();

        private long sizeBytes = 0;

        enum RenderView
        {
            File,
            Folder
        };

        private void Chk_OnClick(object sender, RoutedEventArgs e)
        {
            string file = ((System.Windows.FrameworkElement)sender).DataContext.ToString();
            //if (!File.Exists(file) && view == RenderView.File)
            //{
            //    ((ToggleButton)sender).IsChecked = false;
            //    return;
            //}
            if (sender != null)
                if (((ToggleButton)sender).IsChecked == true)
                {
                    //if (view == RenderView.Folder)
                    //{
                    //    DeleteFromDir(file);
                    //    return;
                    //}
                    var selectedDir = System.IO.Path.GetDirectoryName(file);
                    if (selectedDirList.Count == 0 || selectedDirList.Peek() == selectedDir)
                        selectedDirList.Enqueue(selectedDir);
                    else
                    {
                        selectedDirList.Clear();
                        selectedDirList.Enqueue(selectedDir);
                    }
                    if (selectedDirList.Count == 3)
                    {
                        deleteList.Clear();
                        var dir = selectedDirList.Peek();
                        DeleteFromDir(dir);
                    }
                    else
                    {

                        deleteList.Add(file);
                        sizeBytes += new FileInfo(file).Length;
                    }
                }
                else
                {
                    sizeBytes -= new FileInfo(file).Length;
                    deleteList.Remove(file);
                }
        }

        private void DeleteFromDir(string dir)
        {
            timeTakenLebel.Text = $"Will be deleting all files from : {dir}";
            sizeBytes = 0;
            foreach (var data in dupDataDict.Values)
            {
                foreach (var d in data)
                {
                    if (d.DirectoryName == dir)
                    {
                        sizeBytes += d.Length;
                        deleteList.Add(d.FullName);
                    }
                }
            }
        }

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var file in deleteList)
            {
                File.Delete(file);
            }
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string file = ((System.Windows.FrameworkElement)sender).DataContext.ToString();
            Process ps = new Process();
            var psi = new ProcessStartInfo();
            psi.FileName = @"C:\Windows\System32\rundll32.exe";
            psi.Arguments = $@"""C:\Program Files (x86)\Windows Photo Viewer\PhotoViewer.dll"", ImageView_Fullscreen {file}";
            ps.StartInfo = psi;
            ps.Start();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            string file = ((System.Windows.FrameworkElement)sender).DataContext.ToString();
            if (((Button)sender).Opacity == 1.0)
            {
                ((Button)sender).Opacity = 0.5;
                deleteList.Add(file);
                sizeBytes += new FileInfo(file).Length;
            }
            else
            {
                ((Button)sender).Opacity = 1.0;
                deleteList.Remove(((FrameworkElement)sender).DataContext.ToString());
                sizeBytes -= new FileInfo(file).Length;
            }
        }

        private void FrameworkElement_OnInitialized(object sender, EventArgs e)
        {
            var button = (Button)sender;
            string file = ((System.Windows.FrameworkElement)sender).DataContext.ToString();
            if (System.IO.Path.GetExtension(file) != ".jpg")
            {
                button.Height = 20;
                button.Width = 20;
                button.Template = null;
                button.Content = file;
            }
            else
            {
                button.Height = 100;
                button.Width = 100;
            }
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    textBox2.Text = dialog.SelectedPath;
            }
        }
    }

    public class CustomImagePathConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
                                        System.Globalization.CultureInfo culture)
        {
            return "../Images/" + GetImageName(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                        System.Globalization.CultureInfo culture)
        {
            return "";
        }

        #endregion

        private string GetImageName(string text)
        {
            string name = "";
            name = text.ToLower() + ".png";
            return name;
        }
    }

    public class Site
    {
        public Uri NavigateUri { get; set; }
    }
}