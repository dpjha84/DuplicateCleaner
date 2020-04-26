using DuplicateCleaner.UserControls;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DuplicateCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow1 : Window
    {
        public bool paused, terminated, processing;
        public MainWindow1()
        {
            InitializeComponent();
            imgDetail.Source = new BitmapImage(new Uri("..\\..\\images\\Details.png", UriKind.Relative));
            imgSplit.Source = new BitmapImage(new Uri("..\\..\\images\\Split.png", UriKind.Relative));
            dupControl.mainWindow = this;
            dupControl.cleanupWindow = cleanupControl;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 1;
            File.WriteAllText("setting.json", JsonConvert.SerializeObject(SearchInfo.Instance));
            Scan();
        }

        public void Scan()
        {
            Reset();
            //progressBar.IsIndeterminate = true;
            //txtProgress.Text = "Analyzing";
            //dupControl.Scan(button, progressBar, txtProgress, fileCountLabel, timeTakenLabel);
            dupControl.Scan(this);
            //Task.Run(() => StartProcess());
        }

        private void Reset()
        {
            statusLabel.Text = "";
            //fileCountLabel.Text = "";
            spaceLabel.Text = "";
            //timeTakenLabel.Text = "";
            deleteProgressBar.Value = 0;
            txtDeleteProgress.Text = "";
            //progressBar.Value = 0;
        }

        private void btnDetailPane_Click(object sender, RoutedEventArgs e)
        {
            dupControl.DetailPaneClick(btnDetailPane, btnSplitPane);
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            dupControl.BtnStop_Click(this);
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            dupControl.btnDelete_Click(null, null);
        }

        private void btnSplitPane_Click(object sender, RoutedEventArgs e)
        {
            dupControl.SplitPaneClick(btnDetailPane, btnSplitPane);
        }
    }
}