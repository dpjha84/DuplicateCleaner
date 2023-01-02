using DuplicateCleaner;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        readonly List<string> deleteList = new List<string>();
        public event EventHandler<EventArgs> OnScanInitiated;
        public event EventHandler<EventArgs> OnFileCountFetched;
        public event EventHandler<ScanProgressingArgs> OnScanProgressing;
        public event EventHandler<DeleteProgressingArgs> OnDeleteProgressing;
        public event EventHandler<ScanCompletedArgs> OnScanCompleted;
        public event EventHandler<DeleteCompletedArgs> OnDeleteCompleted;
        public event EventHandler<EventArgs> OnScanStopping;
        public event EventHandler<EventArgs> OnDeleteInitiated;
        public event EventHandler<DeleteSizeChangedArgs> OnDeleteSizeChanged;
        readonly SearchInfo searchInfo;
        readonly ConcurrentDictionary<string, List<FileInfoWrapper>> dupDataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
        
        private long sizeBytes = 0;
        public long SizeBytes
        {
            get { return sizeBytes; }
            set
            {
                sizeBytes = value;
                OnDeleteSizeChanged?.Invoke(this, new DeleteSizeChangedArgs { NewSize = SizeHelper.Suffix(sizeBytes) });
            }
        }
        List<FileInfoWrapper> dupList = new List<FileInfoWrapper>();

        bool imagePreview = true, attached = false;
        public MainWindow Main { get; set; }
        public TopPanel TopPanel { get; set; }
        public CleanupSummary CleanupWindow { get; set; }
        string view = "grid";
        public DuplicatesControl()
        {
            InitializeComponent();
            chkImagePreview.IsChecked = true;
            searchInfo = SearchInfo.Instance;
            //dg1.AutoGeneratingColumn += Dg_AutoGeneratingColumn;
            SetSource(dupList);
            Loaded += DuplicatesControl_Loaded;
        }

        //void Set()
        //{
        //    if (view == "grid")
        //    {
        //        if (dg2 != null)
        //            dg2.ItemsSource = collection;
        //    }
        //    else
        //    {
        //        if (dg1 != null)
        //            dg1.ItemsSource = collection;
        //    }
        //}
        ItemsControl dg1 = null;
        private void SetSource(IEnumerable<FileInfoWrapper> list)
        {
            if (list == null) return;
            ListCollectionView collection = new ListCollectionView(list.ToList());
            collection.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            
            if (view == "grid1")
            {
                dg.Visibility = Visibility.Collapsed;
                dg2.Visibility = Visibility.Visible;
                dg1 = dg2;
                //if (dg2 != null)
                //    dg2.ItemsSource = collection;
            }
            else
            {
                dg.Visibility = Visibility.Visible;
                //dg2.Visibility = Visibility.Collapsed;
                dg1 = dg;
                //if (dg1 != null)
                //    dg1.ItemsSource = collection;
            }
            if (dg1 != null)
            {
                dg1.ItemsSource = collection;
            }
        }

        private void Dg_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

        private void TopPanel_OnScanStared(object sender, EventArgs e)
        {
            attached = true;
            StartScan();
        }

        private void DuplicatesControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!attached)
            {
                TopPanel.OnScanStared += TopPanel_OnScanStared;
                TopPanel.OnScanStopped += TopPanel_OnScanStopped;
                //TopPanel.OnDeleteStared += TopPanel_OnDeleteStared;
            }
            ResetGrid();
        }

        void Reset()
        {
            timeTakenLabel.Text = "";
            fileCountLabel.Text = "";
            currentFileLabel.Text = "";
            SizeBytes = 0;
            deleteList.Clear();
            //cmbFileType.SelectedIndex = 0;
            //cmbAutoSelect.SelectedIndex = 0;
            ImageControl.Source = null;
            txt.Text = "";
        }

        internal void StartScan()
        {
            Reset();
            if (!searchInfo.ScanLocations.Any())
            {
                searchInfo.ScanLocations = DriveInfo.GetDrives().Select(x => new Location { Name = x.Name }).ToList();
            }
            OnScanInitiated(this, new EventArgs());
            dupDataDict.Clear();
            dupList.Clear();
            dg1.ItemsSource = dupList;
            cts = new CancellationTokenSource();
            Task.Run(() => StartProcess(cts.Token));
        }

        private void StartProcess(CancellationToken token)
        {
            var path = Assembly.GetExecutingAssembly().Location;
            var sw = Stopwatch.StartNew();
            var dataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
            var files = GetFiles(token);
            var fileCount = files.Count();

            int i = 1;
            OnFileCountFetched(this, new EventArgs());
            ParallelLoopResult result = Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 4 },
                (file1, state) =>
                {
                    if (Main.terminated)
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
                            OnScanProgressing(this, new ScanProgressingArgs { CurrentProgress = (i++) * 100 / fileCount });
                            currentFileLabel.Text = file1;
                            fileCountLabel.Text = dupDataDict.Count + " duplicate group(s)";
                        });
                    }
                }
            );
            timeTaken = sw.Elapsed;
            Dispatcher.Invoke(() =>
            {
                FlushResult(Main.terminated);
                if (searchInfo.CacheHashData)
                    HashHelper.CacheHashAsync().ConfigureAwait(false);
            });            
        }

        private IEnumerable<string> GetFiles(CancellationToken token)
        {
            List<string> extensions = new List<string>();
            if (searchInfo.IncludeImages) extensions.AddRange(FileHelper.Pics);
            if (searchInfo.IncludeAudios) extensions.AddRange(FileHelper.Audio);
            if (searchInfo.IncludeVideos) extensions.AddRange(FileHelper.Video);
            if (searchInfo.IncludeDocuments) extensions.AddRange(FileHelper.Docs);
            searchInfo.CustomFileTypes = searchInfo.CustomFileTypes ?? Enumerable.Empty<string>();
            extensions.AddRange(searchInfo.CustomFileTypes);
            extensions = extensions.Distinct().ToList();

            var files = Enumerable.Empty<string>();
            var result = Extensions.GetOptimizedFolders(searchInfo.ScanLocations);
            foreach (var item in result.UniqueFolders.Where(x => x.Include))
            {
                if (token.IsCancellationRequested) break;
                var filter = new FileSearchFilter
                {
                    SearchOption = item.IncludeSubfolders.HasValue && item.IncludeSubfolders.Value ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly,
                    ExtensionList = new HashSet<string>(extensions),
                    ExcludedList = new HashSet<string>(),
                    MinSize = searchInfo.MinSize,
                    MaxSize = searchInfo.MaxSize,
                    ModifyAfter = searchInfo.ModifiedAfter,
                    ModifyBefore = searchInfo.ModifiedBefore,
                    IncludeHiddenFolders = searchInfo.IncludeHiddenFolders,
                    IncludeSystemFolders = searchInfo.IncludeSystemFolders
                };
                files = files.Concat(SafeFileEnumerator.EnumerateFiles(item.Name, filter, token));
            }
            return files;
        }

        private void FlushResult(bool terminated)
        {
            dupList = AttachGroupAndFlattenList(dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length)), true);
            ListCollectionView collection = new ListCollectionView(dupList);
            collection.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            dg1.ItemsSource = collection;
            fileCountLabel.Text = dupDataDict.Count + " duplicate group(s)";
            timeTakenLabel.Text = $"Time: {timeTaken.ToHumanTimeString()}";
            currentFileLabel.Text = "";
            Main.terminated = false;
            OnScanCompleted(this, new ScanCompletedArgs
            {
                StatusLabelText = terminated ? "Scan stopped" : "Scan completed",
                SizeToDelete = SizeBytes
            });
        }

        private List<FileInfoWrapper> AttachGroupAndFlattenList(IEnumerable<List<FileInfoWrapper>> l, bool assist = false)
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
                    sortedGroup[j].FileType = FileHelper.GetFileType(sortedGroup[j].FullName);
                    sortedGroup[j].Group = i;
                    list.Add(sortedGroup[j]);
                }
            }
            return list;
        }

        internal async Task TopPanel_OnDeleteStared(object sender, EventArgs e)
        {
            if (SizeBytes == 0) return;
            await Dispatcher.InvokeAsync(async () =>
            {
                OnDeleteInitiated(this, new EventArgs());
                long deleted = 0;
                var files = new List<DeletedFile>();
                foreach (var item in deleteList)
                {
                    try
                    {
                        deleted += new FileInfo(item).Length;
                        if (SizeBytes > 0)
                        {
                                OnDeleteProgressing(this, new DeleteProgressingArgs { CurrentProgress = (deleted * 100) / SizeBytes });
                        }
                        files.Add(new DeletedFile
                        {
                            Name = item,
                            ActionTaken = searchInfo.DeleteOption == DeleteOption.SendToRecycleBin ?
                            "Moved to Recycle Bin" : "Deleted Permanently"
                        });
                        await FileHelper.DeleteFileAsync(item, searchInfo.DeleteOption);
                        }
                    catch (Exception)
                    {
                    }
                }
                SizeBytes = 0;
                OnDeleteCompleted(this, new DeleteCompletedArgs { DeletedSize = SizeHelper.Suffix(deleted), DeletedFiles = files, DeleteStatusLabelText = "Delete Completed" });
            });
        }

        private void TopPanel_OnScanStopped(object sender, EventArgs e)
        {
            Reset();
            cts.Cancel();
            Main.terminated = true;
            OnScanStopping(this, new EventArgs());
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

        void ResetGrid()
        {
            if (previewGrid == null) return;
            grid1.ColumnDefinitions.Clear();
            var cd1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
            grid1.ColumnDefinitions.Add(cd1);
            previewGrid.Visibility = Visibility.Collapsed;
        }

        void ShowPreviewGrid()
        {
            grid1.ColumnDefinitions.Clear();
            var cd1 = new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) };
            var cd2 = new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) };
            var cd3 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
            grid1.ColumnDefinitions.Add(cd1);
            grid1.ColumnDefinitions.Add(cd2);
            grid1.ColumnDefinitions.Add(cd3);
            previewGrid.Visibility = Visibility.Visible;
        }

        private void AutoSelectNewestFiles()
        {
            if (Main == null) return;
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

        void SetFilter(Func<FileInfoWrapper, bool> predicate)
        {
            if(predicate == null) return;
            var itemSourceList = new CollectionViewSource() { Source = dupList.Where(predicate) };
            ICollectionView Itemlist = itemSourceList.View;
            SetSource(Itemlist.SourceCollection as IEnumerable<FileInfoWrapper>);
        }

        #region Control Event Handlers
        public void chk_Click(object sender, RoutedEventArgs e)
        {
            var file = ((FrameworkElement)sender).DataContext as FileInfoWrapper;
            if (sender != null)
            {
                HandleFileCheck(file, ((ToggleButton)sender).IsChecked == true);
            }
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var rowsCount = (dg1 as DataGrid).SelectedItems.Count;
            if (rowsCount == 0 || rowsCount > 1) return;

            var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            if (fileInfo != null)
            {
                if (imagePreview && FileHelper.Pics.Contains(Path.GetExtension(fileInfo.FullName)))
                {
                    ShowPreviewGrid();
                    try
                    {
                        var image = new BitmapImage();
                        using (var f = File.OpenRead(fileInfo.FullName))
                        {
                            var ms = new MemoryStream();
                            f.CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            image.BeginInit();
                            image.StreamSource = ms;
                            image.EndInit();
                            ImageControl.Source = image;
                        }
                    }
                    catch
                    {
                        ResetGrid();
                    }
                }
                else
                    ResetGrid();
            }
        }

        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetFilter(Filter);
        }

        private void cmbFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetGrid();            
            SetFilter(Filter);
        }

        private Func<FileInfoWrapper, bool> Filter
        {
            get
            {
                var fileType = (cmbFileType.SelectedItem as ComboBoxItem).Content.ToString();
                return item => (item.Name.IndexOf(txt.Text, StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                item.DirectoryName.IndexOf(txt.Text, StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                item.DateModified.ToString().IndexOf(txt.Text, StringComparison.InvariantCultureIgnoreCase) >= 0) &&
                (fileType == "All" || item.FileType.ToString() == fileType);
            }
        }

        private void cmbAutoSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (cmbAutoSelect1 == null) return;
            HandleSelectionChange((cmbAutoSelect.SelectedItem as ComboBoxItem).Content.ToString());
        }

        private void HandleSelectionChange(string autoSelect)
        {
            SizeBytes = 0;
            deleteList.Clear();
            switch (autoSelect)
            {
                case "Newer files in group":
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
            SetSource(dupList);
            dg1.Items.Refresh();
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

        private async void dg_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            //if (fileInfo == null) return;
            //await Windows.System.Launcher.LaunchUriAsync(new Uri(fileInfo.FullName));
        }

        private async void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            //ResetGrid();
            //var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            //await Windows.System.Launcher.LaunchUriAsync(new Uri(fileInfo.FullName));
        }

        private void MenuItem_ExcludeByFolder_Click(object sender, RoutedEventArgs e)
        {
            ResetGrid();
            var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            foreach (var item in dupList)
            {
                if (item.Deleted && item.DirectoryName == fileInfo.DirectoryName)
                {
                    item.Deleted = false;
                    HandleFileCheck(item, false);
                }
            }
            //dg.ItemsSource = dupList;
            SetSource(dupList);
            dg1.Items.Refresh();
        }

        private void MenuItem_ExcludeByFileExtn_Click(object sender, RoutedEventArgs e)
        {
            ResetGrid();
            var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            foreach (var item in dupList)
            {
                if (item.Deleted && Path.GetExtension(item.FullName) == Path.GetExtension(fileInfo.FullName))
                {
                    item.Deleted = false;
                    HandleFileCheck(item, false);
                }
            }
            //dg.ItemsSource = dupList;
            SetSource(dupList);
            dg1.Items.Refresh();
        }

        private void MenuItem_MarkByFolder_Click(object sender, RoutedEventArgs e)
        {
            ResetGrid();
            var fileInfo =(dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            foreach (var item in dupList)
            {
                if (!item.Deleted && item.DirectoryName == fileInfo.DirectoryName)
                {
                    item.Deleted = true;
                    HandleFileCheck(item, true);
                }
            }
            //dg.ItemsSource = dupList;
            SetSource(dupList);
            dg1.Items.Refresh();
        }

        private void MenuItem_MarkByFileExtn_Click(object sender, RoutedEventArgs e)
        {
            ResetGrid();
            var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            foreach (var item in dupList)
            {
                if (!item.Deleted && Path.GetExtension(item.FullName) == Path.GetExtension(fileInfo.FullName))
                {
                    item.Deleted = true;
                    HandleFileCheck(item, true);
                }
            }
            //dg.ItemsSource = dupList;
            SetSource(dupList);
            dg1.Items.Refresh();
        }

        private void MenuItem_MarkByGroup_Click(object sender, RoutedEventArgs e)
        {
            ResetGrid();
            var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            for (int i = 1; i < dupList.Count; i++)
            {
                if (dupList[i].Group == fileInfo.Group)
                {
                    if (!dupList[i].Deleted && dupList[i].Group == dupList[i - 1].Group)
                    {
                        dupList[i].Deleted = true;
                        dupList[i - 1].Deleted = true;
                        HandleFileCheck(dupList[i], true);
                        HandleFileCheck(dupList[i - 1], true);
                    }
                }
                else
                {
                    dupList[i].Deleted = false;
                    HandleFileCheck(dupList[i], false);
                }
            }
            //dg.ItemsSource = dupList;
            SetSource(dupList);
            dg1.Items.Refresh();
        }

        private void MenuItem_ExcludeByGroup_Click(object sender, RoutedEventArgs e)
        {
            ResetGrid();
            var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            for (int i = 1; i < dupList.Count; i++)
            {
                if (dupList[i].Deleted && dupList[i].Group == fileInfo.Group && dupList[i].Group == dupList[i - 1].Group)
                {
                    dupList[i].Deleted = false;
                    dupList[i - 1].Deleted = false;
                    HandleFileCheck(dupList[i], false);
                    if(dupList[i -1].Deleted)
                        HandleFileCheck(dupList[i - 1], false);
                }
            }
            SetSource(dupList);
            dg1.Items.Refresh();
        }

        private void btnResetDeletion_Click(object sender, RoutedEventArgs e)
        {
            HandleSelectionChange("Newer files in group");
        }

        private async void MenuItem_Reveal_Click(object sender, RoutedEventArgs e)
        {
            //ResetGrid();
            //var fileInfo = (dg1 as DataGrid).SelectedItem as FileInfoWrapper;
            //await Windows.System.Launcher.LaunchUriAsync(new Uri(fileInfo.DirectoryName));
        }
        #endregion
    }
}
