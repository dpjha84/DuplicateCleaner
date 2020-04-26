using Microsoft.VisualBasic.FileIO;
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
    public partial class DuplicatesControl : UserControl
    {
        List<string> deleteList = new List<string>();
        private long sizeBytes = 0;
        string dirName = "";
        readonly ConcurrentDictionary<string, List<FileInfoWrapper>> dupDataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
        HashSet<string> pics = new HashSet<string> { ".jpg", ".png", ".jpeg", ".bmp", ".gif", ".tif", ".pcx", ".ico", ".tga" };
        List<FileInfoWrapper> dupList = new List<FileInfoWrapper>();
        private long? lowestBreakIndex = null;
        SearchInfo searchInfo;

        public MainWindow1 mainWindow { get; set; }
        public CleanupSummary cleanupWindow { get; set; }

        public DuplicatesControl()
        {
            InitializeComponent();
            searchInfo = SearchInfo.Instance;
            lvFiles.ItemsSource = dupList;
        }

        void Reset()
        {
            sizeBytes = 0;
            mainWindow.spaceLabel.Text = "";
            deleteList.Clear();
            mainWindow.statusDeleteLabel.Text = "";
            ImageControl.Source = null;
        }

        internal void Scan(MainWindow1 main)
        {
            Reset();
            main.statusLabel.Text = "Scanning...";
            if (main.button.Content.ToString() == "Pause Scan")
            {
                main.paused = true;
                main.terminated = false;
                main.processing = false;
                main.statusLabel.Text = "Search paused";
                main.button.Content = "Resume Scan";
            }
            else
            {
                if (main.button.Content.ToString() == "Start Scan")
                {
                    timeTaken = TimeSpan.Zero;
                    dupDataDict.Clear();
                    lvFiles.ItemsSource = dupDataDict.Values;
                }
                main.paused = false;
                main.terminated = false;
                main.processing = true;
                main.button.Content = "Pause Scan";
                Task.Run(() => StartProcess(main));
            }
        }

        TimeSpan timeTaken = TimeSpan.Zero;
        private void StartProcess(MainWindow1 main)
        {
            if (!lowestBreakIndex.HasValue)
            {
                Dispatcher.Invoke(() =>
                {
                    main.progressBar.IsIndeterminate = true;
                    main.txtProgress.Text = "Analyzing";
                });
            }
            var sw = Stopwatch.StartNew();
            var dataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
            var files = GetFiles(dirName);
            var fileCount = files.Count();
            
            int i = 1, skip = lowestBreakIndex.HasValue ? (int)lowestBreakIndex.Value : 0;
            Dispatcher.Invoke(() =>
            {
                main.progressBar.IsIndeterminate = false;
            });
            ParallelLoopResult result = Parallel.ForEach(files.Skip(skip), new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (file1, state) =>
            //foreach(var file1 in files)
            {
                if (main.paused)
                    state.Break();
                else if (main.terminated)
                    state.Stop();
                else
                {
                    var hash = HashHelper.GetFileHash(file1);
                    dataDict.AddOrUpdate(
                        hash,
                        new List<FileInfoWrapper> { new FileInfoWrapper(file1, hash) },
                        (k1, v1) =>
                        {
                            dataDict[hash].Add(new FileInfoWrapper(file1, hash));
                            dupDataDict.AddOrUpdate(hash, dataDict[hash], (k, v) =>
                            {
                                return v;
                            });
                            Dispatcher.Invoke(() =>
                            {
                                lvFiles.ItemsSource = AttachGroupAndFlattenList(dupDataDict.Values);
                            });
                            return v1;
                        }
                        );
                    Dispatcher.Invoke(() =>
                    {
                        main.progressBar.Value = (skip+i++) * 100 / fileCount;
                        main.txtProgress.Text = $"{main.progressBar.Value}%";
                    });
                }
            }
            );
            timeTaken = sw.Elapsed.Add(timeTaken);
            if (main.paused) // Paused
            {
                if(result.LowestBreakIteration.HasValue)
                {
                    if (!lowestBreakIndex.HasValue)
                        lowestBreakIndex = 0;
                    lowestBreakIndex += result.LowestBreakIteration;
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    main.progressBar.Value = 100;
                    main.txtProgress.Text = "100%";
                    lvFiles.ItemsSource = AttachGroupAndFlattenList(dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length)), true);
                    main.statusLabel.Text = main.terminated ? "Scan stopped" : "Scan completed";
                    fileCountLabel.Text = dupDataDict.Count + " record(s).";
                    timeTakenLabel.Text = $"Time: {timeTaken.ToHumanTimeString()}";
                    mainWindow.spaceLabel.Text = $"Total Size: {SizeHelper.Suffix(sizeBytes)}";
                    main.button.Content = "Start Scan";
                });
            }
            main.processing = main.paused = main.terminated = false;
        }

        IEnumerable<string> GetFiles(string dirName)
        {
            var audio = new HashSet<string> { ".mp3", ".ogg", ".wma", ".wav", ".ape", ".flac", ".m4p", ".m4a", ".aac" };
            var video = new HashSet<string> { ".mov", ".avi", ".mp4", ".mpg", ".wmv", ".flv", ".3pg", ".asf" };
            var docs = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".mdb", ".accdb", ".pdf", ".txt", ".odt", ".ods" };
            List<string> extensions = new List<string>();
            if (searchInfo.IncludeImages) extensions.AddRange(pics);
            if (searchInfo.IncludeAudios) extensions.AddRange(audio);
            if (searchInfo.IncludeVideos) extensions.AddRange(video);
            if (searchInfo.IncludeDocuments) extensions.AddRange(docs);

            var files = Enumerable.Empty<string>();
            var result = Extensions.GetOptimizedFolders(searchInfo.ScanLocations);
            foreach (var item in result.UniqueFolders.Where(x => !x.Exclude))
            {
                files = files.Concat(SafeFileEnumerator.EnumerateFiles(item.Name, item.IncludeSubfolders ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly,
                new HashSet<string>(extensions), result.ExcludedInTreeList, searchInfo.MinSize, searchInfo.MaxSize, searchInfo.ModifiedAfter, searchInfo.ModifiedBefore, searchInfo.IncludeHiddenFolders));
            }
            return files;
        }

        private void ResetLabels(params TextBlock[] labels)
        {
            foreach (var item in labels)
            {
                item.Text = "";
            }
        }

        public void BtnStop_Click(MainWindow1 main)
        {
            ResetLabels(timeTakenLabel, fileCountLabel);
            main.terminated = true;
            main.processing = false;
            main.paused = false;
            lowestBreakIndex = null;

            main.progressBar.Value = 100;
            main.txtProgress.Text = "100%";
            //lvFiles.ItemsSource = AttachGroupAndFlattenList(dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length)));
            main.statusLabel.Text = "Scan stopped";
            fileCountLabel.Text = dupDataDict.Count + " record(s).";
            timeTakenLabel.Text = $"Time: {timeTaken.ToHumanTimeString()}";
            main.button.Content = "Start Scan";
        }

        List<FileInfoWrapper> AttachGroupAndFlattenList(IEnumerable<List<FileInfoWrapper>> l, bool assist = false)
        {
            int i = 0;
            //dupList.Clear();
            //dupList = new List<FileInfoWrapper>();
            var list = new List<FileInfoWrapper>();
            foreach (var group in l)
            {
                i++;
                for (int j = 0; j < group.Count; j++)
                {
                    if (assist && j > 0)
                    {
                        group[j].Deleted = true;
                        deleteList.Add(group[j].FullName);
                        sizeBytes += group[j].Length;
                    }
                    group[j].Group = i;
                    list.Add(group[j]);
                }
            }
            return list;
        }

        private void lvFiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var fileInfo = lvFiles.SelectedItem as FileInfoWrapper;
            if (fileInfo != null)
            {
                if (!pics.Contains(System.IO.Path.GetExtension(fileInfo.FullName)))
                {
                    ImageControl.Source = null;
                    return;
                }
                if (previewGrid.Visibility != Visibility.Visible)
                {
                    grid1.ColumnDefinitions.Clear();
                    var cd1 = new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) };
                    var cd2 = new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) };
                    var cd3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
                    grid1.ColumnDefinitions.Add(cd1);
                    grid1.ColumnDefinitions.Add(cd2);
                    grid1.ColumnDefinitions.Add(cd3);
                    previewGrid.Visibility = Visibility.Visible;
                }
                try
                {
                    ImageControl.Source = new BitmapImage(new Uri(fileInfo.FullName));
                }
                catch (Exception)
                {
                }
            }
        }

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
                    var files = new List<DeletedFile>();
                    foreach (var item in deleteList)
                    {
                        try
                        {
                            deleted += new FileInfo(item).Length;
                            if (sizeBytes > 0)
                            {
                                mainWindow.deleteProgressBar.Value = (deleted * 100) / sizeBytes;
                                mainWindow.txtDeleteProgress.Text = $"{mainWindow.deleteProgressBar.Value}%";
                            }
                            files.Add(new DeletedFile { Name = item, ActionTaken = searchInfo.DeleteOption == RecycleOption.SendToRecycleBin ? 
                                "Moved to Recycle Bin" : "Deleted Permanently" });
                            FileSystem.DeleteFile(item, UIOption.OnlyErrorDialogs, searchInfo.DeleteOption);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    mainWindow.deleteProgressBar.Value = 100;
                    mainWindow.txtDeleteProgress.Text = $"100%";
                    mainWindow.statusDeleteLabel.Text = "Delete completed";
                    //var cs = Extensions.FindChild<CleanupSummary>(Application.Current.MainWindow, "cleanupControl");
                    //CleanupSummary cs = uc as CleanupSummary;
                    cleanupWindow.txtSummary.Text = string.Format(cleanupWindow.txtSummary.Text, SizeHelper.Suffix(deleted));
                    cleanupWindow.lvFiles1.ItemsSource = files;
                    mainWindow.tabControl.SelectedIndex = 2;
                }
                );
            }
            );
            
            
        }

        public void btnDetailPane_Click(object sender, RoutedEventArgs e)
        {
            grid1.ColumnDefinitions.Clear();
            var cd1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
            grid1.ColumnDefinitions.Add(cd1);
            previewGrid.Visibility = Visibility.Collapsed;
            //btnDetailPane.BorderThickness = new Thickness(2);
            //btnSplitPane.BorderThickness = new Thickness(0);
        }

        public void DetailPaneClick(Button btnDetailPane, Button btnSplitPane)
        {
            grid1.ColumnDefinitions.Clear();
            var cd1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
            grid1.ColumnDefinitions.Add(cd1);
            previewGrid.Visibility = Visibility.Collapsed;
            btnDetailPane.BorderThickness = new Thickness(2);
            btnSplitPane.BorderThickness = new Thickness(0);
        }

        public void SplitPaneClick(Button btnDetailPane, Button btnSplitPane)
        {
            grid1.ColumnDefinitions.Clear();
            var cd1 = new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) };
            var cd2 = new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) };
            var cd3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
            grid1.ColumnDefinitions.Add(cd1);
            grid1.ColumnDefinitions.Add(cd2);
            grid1.ColumnDefinitions.Add(cd3);
            previewGrid.Visibility = Visibility.Visible;
            btnDetailPane.BorderThickness = new Thickness(0);
            btnSplitPane.BorderThickness = new Thickness(2);
        }

        public void btnSplitPane_Click(object sender, RoutedEventArgs e)
        {
            grid1.ColumnDefinitions.Clear();
            var cd1 = new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) };
            var cd2 = new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) };
            var cd3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
            grid1.ColumnDefinitions.Add(cd1);
            grid1.ColumnDefinitions.Add(cd2);
            grid1.ColumnDefinitions.Add(cd3);
            previewGrid.Visibility = Visibility.Visible;
            //btnDetailPane.BorderThickness = new Thickness(0);
            //btnSplitPane.BorderThickness = new Thickness(2);
        }
    }
}
