using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DuplicateCleaner.UserControls
{
    /// <summary>
    /// Interaction logic for DuplicatesControl.xaml
    /// </summary>
    public partial class CleanupSummary : UserControl
    {
        List<string> deleteList = new List<string>();
        private long sizeBytes = 0;
        string dirName = "";
        readonly ConcurrentDictionary<string, List<FileInfoWrapper>> dupDataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
        HashSet<string> pics = new HashSet<string> { ".jpg", ".png", ".jpeg", ".bmp", ".gif", ".tif", ".pcx", ".ico", ".tga" };
        List<DeletedFile> dupList = new List<DeletedFile>();
        private long? lowestBreakIndex = null;
        SearchInfo searchInfo;

        public MainWindow1 mainWindow { get; set; }

        public CleanupSummary()
        {
            InitializeComponent();
            searchInfo = SearchInfo.Instance;
            lvFiles1.ItemsSource = dupList;
        }

        //private void lvFiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        //{
        //    var fileInfo = lvFiles1.SelectedItem as FileInfoWrapper;
        //    if (fileInfo != null)
        //    {
        //        if (!pics.Contains(System.IO.Path.GetExtension(fileInfo.FullName)))
        //        {
        //            ImageControl.Source = null;
        //            return;
        //        }
        //        if (previewGrid.Visibility != Visibility.Visible)
        //        {
        //            grid1.ColumnDefinitions.Clear();
        //            var cd1 = new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) };
        //            var cd2 = new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) };
        //            var cd3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
        //            grid1.ColumnDefinitions.Add(cd1);
        //            grid1.ColumnDefinitions.Add(cd2);
        //            grid1.ColumnDefinitions.Add(cd3);
        //            previewGrid.Visibility = Visibility.Visible;
        //        }
        //        BitmapImage bitmap = new BitmapImage();
        //        bitmap.BeginInit();
        //        bitmap.UriSource = new Uri(fileInfo.FullName);
        //        bitmap.EndInit();
        //        ImageControl.Source = bitmap;
        //    }
        //}

        private void chk_Click(object sender, RoutedEventArgs e)
        {
            var file = ((FrameworkElement)sender).DataContext as FileInfoWrapper;
            if (sender != null)
            {
                if (((ToggleButton)sender).IsChecked == true)
                {
                    deleteList.Add(file.FullName);
                    sizeBytes += file.Length;
                }
                else
                {
                    sizeBytes -= file.Length;
                    deleteList.Remove(file.FullName);
                }
                mainWindow.spaceLabel.Text = $"Total Size: {SizeHelper.Suffix(sizeBytes)}";
            }
        }

        public void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    mainWindow.statusDeleteLabel.Text = "Deleting...";
                    long deleted = 0;
                    foreach (var item in deleteList)
                    {
                        try
                        {
                            deleted += new FileInfo(item).Length;
                            mainWindow.deleteProgressBar.Value = (deleted * 100) / sizeBytes;
                            mainWindow.txtDeleteProgress.Text = $"{mainWindow.deleteProgressBar.Value}%";
                            File.Delete(item);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    mainWindow.statusDeleteLabel.Text = "Delete completed";
                    mainWindow.tabControl.SelectedIndex = 0;
                }
                );
            }
            );
            
            
        }

        //public void btnDetailPane_Click(object sender, RoutedEventArgs e)
        //{
        //    grid1.ColumnDefinitions.Clear();
        //    var cd1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
        //    grid1.ColumnDefinitions.Add(cd1);
        //    previewGrid.Visibility = Visibility.Collapsed;
        //    //btnDetailPane.BorderThickness = new Thickness(2);
        //    //btnSplitPane.BorderThickness = new Thickness(0);
        //}

        //public void DetailPaneClick(Button btnDetailPane, Button btnSplitPane)
        //{
        //    grid1.ColumnDefinitions.Clear();
        //    var cd1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
        //    grid1.ColumnDefinitions.Add(cd1);
        //    previewGrid.Visibility = Visibility.Collapsed;
        //    btnDetailPane.BorderThickness = new Thickness(2);
        //    btnSplitPane.BorderThickness = new Thickness(0);
        //}

        //public void SplitPaneClick(Button btnDetailPane, Button btnSplitPane)
        //{
        //    grid1.ColumnDefinitions.Clear();
        //    var cd1 = new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) };
        //    var cd2 = new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) };
        //    var cd3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
        //    grid1.ColumnDefinitions.Add(cd1);
        //    grid1.ColumnDefinitions.Add(cd2);
        //    grid1.ColumnDefinitions.Add(cd3);
        //    previewGrid.Visibility = Visibility.Visible;
        //    btnDetailPane.BorderThickness = new Thickness(0);
        //    btnSplitPane.BorderThickness = new Thickness(2);
        //}

        //public void btnSplitPane_Click(object sender, RoutedEventArgs e)
        //{
        //    grid1.ColumnDefinitions.Clear();
        //    var cd1 = new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) };
        //    var cd2 = new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) };
        //    var cd3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
        //    grid1.ColumnDefinitions.Add(cd1);
        //    grid1.ColumnDefinitions.Add(cd2);
        //    grid1.ColumnDefinitions.Add(cd3);
        //    previewGrid.Visibility = Visibility.Visible;
        //    //btnDetailPane.BorderThickness = new Thickness(0);
        //    //btnSplitPane.BorderThickness = new Thickness(2);
        //}
    }
}
