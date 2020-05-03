using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DuplicateCleaner.UserControls
{
    public partial class DuplicatesControl : UserControl
    {
        CancellationTokenSource cts;
        TimeSpan timeTaken = TimeSpan.Zero;
        List<string> deleteList = new List<string>();
        private long sizeBytes = 0;
        public long SizeBytes
        {
            get { return sizeBytes; }
            set
            {
                sizeBytes = value;
                if (main != null)
                    main.btnDelete.Content = $"Delete Duplicates ({SizeHelper.Suffix(sizeBytes)})";
            }
        }
        readonly ConcurrentDictionary<string, List<FileInfoWrapper>> dupDataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
        HashSet<string> pics = new HashSet<string>(new[] { ".jpg", ".png", ".jpeg", ".bmp", ".gif", ".tif", ".pcx", ".ico", ".tga" }, StringComparer.OrdinalIgnoreCase);
        HashSet<string> audio = new HashSet<string>(new[] { ".mp3", ".ogg", ".wma", ".wav", ".ape", ".flac", ".m4p", ".m4a", ".aac" }, StringComparer.OrdinalIgnoreCase);
        HashSet<string> video = new HashSet<string>(new[] { ".mov", ".avi", ".mp4", ".mpg", ".wmv", ".flv", ".3pg", ".asf" }, StringComparer.OrdinalIgnoreCase);
        HashSet<string> docs = new HashSet<string>(new[] { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".mdb", ".accdb", ".pdf", ".txt", ".odt", ".ods" }, StringComparer.OrdinalIgnoreCase);
        List<FileInfoWrapper> dupList = new List<FileInfoWrapper>();

        readonly SearchInfo searchInfo;
        bool imagePreview = true;
        public MainWindow1 main { get; set; }
        public CleanupSummary cleanupWindow { get; set; }

        public DuplicatesControl()
        {
            InitializeComponent();
            chkImagePreview.IsChecked = true;
            searchInfo = SearchInfo.Instance;
            dg.ItemsSource = dupList;
            this.Loaded += DuplicatesControl_Loaded;
        }

        private void DuplicatesControl_Loaded(object sender, RoutedEventArgs e)
        {
            ResetGrid();
        }

        void Reset()
        {
            timeTakenLabel.Text = "";
            fileCountLabel.Text = "";
            currentFileLabel.Text = "";
            SizeBytes = 0;
            deleteList.Clear();
            main.statusDeleteLabel.Text = "";
            cmbFileType.SelectedIndex = 0;
            cmbAutoSelect.SelectedIndex = 0;
            ImageControl.Source = null;
        }


        internal void Scan()
        {
            Reset();
            if (main.button.Content.ToString() == "Start Scan")
            {
                main.statusLabel.Text = "Scanning...";
                dupDataDict.Clear();
                dg.ItemsSource = dupDataDict.Values;
                cts = new CancellationTokenSource();
                Task.Run(() => StartProcess(cts.Token));
                main.button.Content = "Stop Scan";
            }
            else
            {
                main.statusLabel.Text = "Stopping...";
                cts.Cancel();
                main.terminated = true;
                main.button.Content = "Start Scan";
            }
        }

        private void StartProcess(CancellationToken token)
        {
            Dispatcher.Invoke(() =>
            {
                main.progressBar.IsIndeterminate = true;
                main.txtProgress.Text = "Analyzing";
            });
            var sw = Stopwatch.StartNew();
            var dataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
            var files = GetFiles(token);
            var fileCount = files.Count();

            int i = 1;
            Dispatcher.Invoke(() =>
            {
                main.progressBar.IsIndeterminate = false;
                main.progressBar.Value = 0;
                main.txtProgress.Text = $"{main.progressBar.Value}%";
            });
            ParallelLoopResult result = Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 4 },
                (file1, state) =>
            {
                if (main.terminated)
                    state.Stop();
                else
                {
                    var hash = HashHelper.GetFileHash(file1);
                    if (hash != null)
                    {
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
                                return v1;
                            });
                    }
                    Dispatcher.Invoke(() =>
                    {
                        main.progressBar.Value = (i++) * 100 / fileCount;
                        main.txtProgress.Text = $"{main.progressBar.Value}%";
                        currentFileLabel.Text = file1;
                        fileCountLabel.Text = dupDataDict.Count + " duplicate(s)";
                    });
                }
            }
            );
            timeTaken = sw.Elapsed.Add(timeTaken);
            Dispatcher.Invoke(() =>
            {
                FlushResult(main.terminated);
            });
        }

        IEnumerable<string> GetFiles(CancellationToken token)
        {
            List<string> extensions = new List<string>();
            if (searchInfo.IncludeImages) extensions.AddRange(pics);
            if (searchInfo.IncludeAudios) extensions.AddRange(audio);
            if (searchInfo.IncludeVideos) extensions.AddRange(video);
            if (searchInfo.IncludeDocuments) extensions.AddRange(docs);

            var files = Enumerable.Empty<string>();
            var result = Extensions.GetOptimizedFolders(searchInfo.ScanLocations);
            foreach (var item in result.UniqueFolders.Where(x => !x.Exclude))
            {
                if (token.IsCancellationRequested) break;
                files = files.Concat(SafeFileEnumerator.EnumerateFiles(item.Name, item.IncludeSubfolders ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly,
                new HashSet<string>(extensions), result.ExcludedInTreeList, searchInfo.MinSize, searchInfo.MaxSize, searchInfo.ModifiedAfter, searchInfo.ModifiedBefore, searchInfo.IncludeHiddenFolders, token));
            }
            return files;
        }

        void FlushResult(bool terminated)
        {
            main.progressBar.Value = 100;
            main.txtProgress.Text = "100%";
            dupList = AttachGroupAndFlattenList(dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length)), true);
            dg.ItemsSource = dupList;
            main.statusLabel.Text = terminated ? "Scan stopped" : "Scan completed";
            fileCountLabel.Text = dupDataDict.Count + " duplicate(s)";
            timeTakenLabel.Text = $"Time: {timeTaken.ToHumanTimeString()}";
            main.button.Content = "Start Scan";
            currentFileLabel.Text = "";
            main.btnDelete.Visibility = Visibility.Visible;
            main.sep.Visibility = Visibility.Visible;
            main.terminated = false;
        }

        List<FileInfoWrapper> AttachGroupAndFlattenList(IEnumerable<List<FileInfoWrapper>> l, bool assist = false)
        {
            int i = 0;
            var list = new List<FileInfoWrapper>();
            foreach (var group in l)
            {
                var sortedGroup = group.OrderBy(x => x.DateCreated).ToList();
                i++;
                for (int j = 0; j < sortedGroup.Count; j++)
                {
                    if (assist && j > 0)
                    {
                        sortedGroup[j].Deleted = true;
                        deleteList.Add(sortedGroup[j].FullName);
                        SizeBytes += sortedGroup[j].Length;
                    }
                    sortedGroup[j].FileType = GetFileType(sortedGroup[j].FullName);
                    sortedGroup[j].Group = i;
                    list.Add(sortedGroup[j]);
                }
            }
            return list;
        }

        private FileType GetFileType(string fullName)
        {
            var extn = Path.GetExtension(fullName);
            if (pics.Contains(extn)) return FileType.Image;
            if (audio.Contains(extn)) return FileType.Audio;
            if (video.Contains(extn)) return FileType.Video;
            if (docs.Contains(extn)) return FileType.Document;
            return FileType.Custom;
        }

        private void HandleFileCheck(FileInfoWrapper file, bool isChecked)
        {
            if (isChecked)
            {
                deleteList.Add(file.FullName);
                SizeBytes += file.Length;
            }
            else
            {
                SizeBytes -= file.Length;
                deleteList.Remove(file.FullName);
            }
        }

        public void chk_Click(object sender, RoutedEventArgs e)
        {
            var file = ((FrameworkElement)sender).DataContext as FileInfoWrapper;
            if (sender != null)
            {
                HandleFileCheck(file, ((ToggleButton)sender).IsChecked == true);
            }
        }

        public void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SizeBytes == 0) return;
            main.gridDeleteProgress.Visibility = Visibility.Visible;
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    main.statusDeleteLabel.Text = "Deleting...";
                    long deleted = 0;
                    var files = new List<DeletedFile>();
                    foreach (var item in deleteList)
                    {
                        try
                        {
                            deleted += new FileInfo(item).Length;
                            if (SizeBytes > 0)
                            {
                                main.deleteProgressBar.Value = (deleted * 100) / SizeBytes;
                                main.txtDeleteProgress.Text = $"{main.deleteProgressBar.Value}%";
                            }
                            files.Add(new DeletedFile
                            {
                                Name = item,
                                ActionTaken = searchInfo.DeleteOption == RecycleOption.SendToRecycleBin ?
                                "Moved to Recycle Bin" : "Deleted Permanently"
                            });
                            FileSystem.DeleteFile(item, UIOption.OnlyErrorDialogs, searchInfo.DeleteOption, UICancelOption.DoNothing);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    main.deleteProgressBar.Value = 100;
                    main.txtDeleteProgress.Text = $"100%";
                    main.statusDeleteLabel.Text = "Delete completed";
                    cleanupWindow.DeletedSize = SizeHelper.Suffix(deleted);
                    cleanupWindow.dgDeleted.ItemsSource = files;
                    (main.tabControl.Items[3] as TabItem).Visibility = Visibility.Visible;
                    main.tabControl.SelectedIndex = 3;
                }
                );
            }
            );
        }

        public void DetailPaneClick(Button btnDetailPane, Button btnSplitPane)
        {
            ResetGrid();
        }

        void ResetGrid()
        {
            if (previewGrid == null) return;
            grid1.ColumnDefinitions.Clear();
            var cd1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
            grid1.ColumnDefinitions.Add(cd1);
            previewGrid.Visibility = Visibility.Collapsed;
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

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var fileInfo = dg.SelectedItem as FileInfoWrapper;
            if (fileInfo != null)
            {
                if (imagePreview && pics.Contains(Path.GetExtension(fileInfo.FullName)))
                {
                    ShowPreviewGrid();
                }
                else
                    ResetGrid();
                try
                {
                    //ImageControl.Source = new BitmapImage(new Uri(fileInfo.FullName));
                    using (var ms = new MemoryStream())
                    {
                        using (var stream = File.OpenRead(fileInfo.FullName))
                        {
                            stream.CopyTo(ms);
                        }
                        ImageControl.Source = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
                catch (Exception)
                {
                }
            }
        }



        void ShowPreviewGrid()
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

        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetFilter(new Predicate<object>(item => ((FileInfoWrapper)item).Name.IndexOf(txt.Text, StringComparison.InvariantCultureIgnoreCase) >= 0));
        }

        private void cmbFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetGrid();
            var fileType = (cmbFileType.SelectedItem as ComboBoxItem).Content.ToString();
            SetFilter(new Predicate<object>(item => fileType == "All" || ((FileInfoWrapper)item).FileType.ToString() == fileType));
        }

        private void cmbAutoSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SizeBytes = 0;
            deleteList.Clear();
            var autoSelect = (cmbAutoSelect.SelectedItem as ComboBoxItem).Content.ToString();
            switch (autoSelect)
            {
                case "Newest Files":
                    AutoSelectNewestFiles();
                    break;
                case "All":
                    dupList.ForEach(x =>
                    {
                        x.Deleted = true;
                        HandleFileCheck(x, true);
                    });
                    break;
                case "None":
                    dupList.ForEach(x => x.Deleted = false);
                    break;
                default:
                    break;
            }
            dg.ItemsSource = dupList;
            dg.Items.Refresh();
        }

        private void AutoSelectNewestFiles()
        {
            if (main == null) return;
            if (dupList.Any())
                dupList[0].Deleted = false;
            for (int i = 1; i < dupList.Count; i++)
            {
                if (dupList[i].Group == dupList[i - 1].Group)
                {
                    dupList[i].Deleted = true;
                    deleteList.Add(dupList[i].FullName);
                    SizeBytes += dupList[i].Length;
                }
                else
                    dupList[i].Deleted = false;
            }
        }

        void SetFilter(Predicate<object> predicate)
        {
            var _itemSourceList = new CollectionViewSource() { Source = dupList };

            // ICollectionView the View/UI part 
            ICollectionView Itemlist = _itemSourceList.View;

            // your Filter
            var yourCostumFilter = predicate; // new Predicate<object>(item => fileType == "All" || ((FileInfoWrapper)item).FileType.ToString() == fileType);

            //now we add our Filter
            Itemlist.Filter = yourCostumFilter;

            dg.ItemsSource = Itemlist;
        }

        private void chkImagePreview_Checked(object sender, RoutedEventArgs e)
        {
            imagePreview = true;
        }

        private void chkImagePreview_Unchecked(object sender, RoutedEventArgs e)
        {
            imagePreview = false;
            ResetGrid();
        }
    }
}
