using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DuplicateCleaner
{
    public class DuplicateFinder
    {
        DataGrid dg;
        readonly ConcurrentDictionary<string, List<FileInfoWrapper>> dupDataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
        List<FileInfoWrapper> dupList = new List<FileInfoWrapper>();
        TextBlock timeTakenLabel, fileCountLabel;
        private long? lowestBreakIndex = null;
        MainWindow1 mainWindow;
        public void BtnStop_ClickWithPause(MainWindow1 main)
        {
            if (main.button.Content.ToString() == "Start Scan") return;
            ResetLabels(timeTakenLabel, fileCountLabel);
            main.terminated = true;
            main.processing = false;
            main.paused = false;
            lowestBreakIndex = null;
            FlushResult(true);
        }

        private void ResetLabels(params TextBlock[] labels)
        {
            foreach (var item in labels)
            {
                item.Text = "";
            }
        }

        void FlushResult(bool terminated)
        {
            mainWindow.progressBar.Value = 100;
            mainWindow.txtProgress.Text = "100%";
            dupList = AttachGroupAndFlattenList(dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length)), true);
            dg.ItemsSource = dupList;
            mainWindow.statusLabel.Text = terminated ? "Scan stopped" : "Scan completed";
            fileCountLabel.Text = dupDataDict.Count + " duplicate(s)";
            //timeTakenLabel.Text = $"Time: {timeTaken.ToHumanTimeString()}";
            mainWindow.button.Content = "Start Scan";
            //currentFileLabel.Text = "";
            mainWindow.btnDelete.Visibility = Visibility.Visible;
            mainWindow.sep.Visibility = Visibility.Visible;
            mainWindow.terminated = false;
        }

        List<FileInfoWrapper> AttachGroupAndFlattenList(IEnumerable<List<FileInfoWrapper>> l, bool assist = false)
        {
            var list = new List<FileInfoWrapper>();
            //foreach (var group in l)
            //{
            //    var sortedGroup = group.OrderBy(x => x.DateCreated).ToList();
            //    i++;
            //    for (int j = 0; j < sortedGroup.Count; j++)
            //    {
            //        if (assist && j > 0)
            //        {
            //            sortedGroup[j].Deleted = true;
            //            deleteList.Add(sortedGroup[j].FullName);
            //            SizeBytes += sortedGroup[j].Length;
            //        }
            //        sortedGroup[j].FileType = GetFileType(sortedGroup[j].FullName);
            //        sortedGroup[j].Group = i;
            //        list.Add(sortedGroup[j]);
            //    }
            //}
            return list;
        }

        private void StartProcessWithPause(MainWindow1 main)
        {
            //if (!lowestBreakIndex.HasValue)
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        main.progressBar.IsIndeterminate = true;
            //        main.txtProgress.Text = "Analyzing";
            //    });
            //}
            //var sw = Stopwatch.StartNew();
            //var dataDict = new ConcurrentDictionary<string, List<FileInfoWrapper>>();
            //var files = GetFiles();
            //var fileCount = files.Count();

            //int i = 1, skip = lowestBreakIndex.HasValue ? (int)lowestBreakIndex.Value : 0;
            //Dispatcher.Invoke(() =>
            //{
            //    main.progressBar.IsIndeterminate = false;
            //});
            //ParallelLoopResult result = Parallel.ForEach(files.Skip(skip), new ParallelOptions() { MaxDegreeOfParallelism = -1 },
            //    (file1, state) =>
            //    {
            //        if (main.paused)
            //            state.Break();
            //        else
            //        if (main.terminated)
            //            state.Stop();
            //        else
            //        {
            //            var hash = HashHelper.GetFileHash(file1);
            //            if (hash != null)
            //            {
            //                dataDict.AddOrUpdate(
            //                    hash,
            //                    new List<FileInfoWrapper> { new FileInfoWrapper(file1, hash) },
            //                    (k1, v1) =>
            //                    {
            //                        dataDict[hash].Add(new FileInfoWrapper(file1, hash));
            //                        dupDataDict.AddOrUpdate(hash, dataDict[hash], (k, v) =>
            //                        {
            //                            return v;
            //                        });
            //                        return v1;
            //                    });
            //            }
            //            Dispatcher.Invoke(() =>
            //            {
            //                main.progressBar.Value = (skip + i++) * 100 / fileCount;
            //                main.txtProgress.Text = $"{main.progressBar.Value}%";
            //                currentFileLabel.Text = file1;
            //                fileCountLabel.Text = dupDataDict.Count + " duplicate(s)";
            //            });
            //        }
            //    }
            //);
            //timeTaken = sw.Elapsed.Add(timeTaken);

            //if (main.paused) // Paused
            //{
            //    if (result.LowestBreakIteration.HasValue)
            //    {
            //        if (!lowestBreakIndex.HasValue)
            //            lowestBreakIndex = 0;
            //        lowestBreakIndex += result.LowestBreakIteration;
            //    }
            //}
            //else
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        FlushResult(main.terminated);
            //    });
            //}
            //main.processing = main.paused = main.terminated = false;
        }
    }
}
