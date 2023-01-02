using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace DuplicateCleaner.UserControls
{
    /// <summary>
    /// Interaction logic for TopPanel.xaml
    /// </summary>
    public partial class TopPanel : UserControl
    {
        public event EventHandler<EventArgs> OnScanStared;
        public event EventHandler<EventArgs> OnScanStopped;
        public event EventHandler<EventArgs> OnDeleteStared;
        public DuplicatesControl dupControl { get; set; }
        //public PreferenceControl prefControl { get; set; }
        public TopPanel()
        {
            InitializeComponent();
            this.Loaded += TopPanel_Loaded;
        }

        private void TopPanel_Loaded(object sender, RoutedEventArgs e)
        {
            dupControl.OnScanInitiated += DupControl_OnScanInitiated;
            dupControl.OnFileCountFetched += DupControl_OnFileCountFetched;
            dupControl.OnScanProgressing += DupControl_OnScanProgressing;
            dupControl.OnScanCompleted += DupControl_OnScanCompleted;
            dupControl.OnScanStopping += DupControl_OnScanStopping;
            dupControl.OnDeleteInitiated += DupControl_OnDeleteInitiated;
            dupControl.OnDeleteProgressing += DupControl_OnDeleteProgressing;
            dupControl.OnDeleteCompleted += DupControl_OnDeleteCompleted;
            dupControl.OnDeleteSizeChanged += DupControl_OnDeleteSizeChanged;
        }

        private void DupControl_OnDeleteSizeChanged(object sender, DeleteSizeChangedArgs e)
        {
            btnDelete.Content = $"Delete Duplicates ({e.NewSize})";
        }

        private void DupControl_OnDeleteCompleted(object sender, DeleteCompletedArgs e)
        {
            deleteProgressBar.Value = 100;
            txtDeleteProgress.Text = e.DeleteStatusLabelText;
            //statusDeleteLabel.Text = "Delete completed";
        }

        private void DupControl_OnDeleteProgressing(object sender, DeleteProgressingArgs e)
        {
            deleteProgressBar.Value = e.CurrentProgress;
            txtDeleteProgress.Text = $"{deleteProgressBar.Value}%";
        }

        private void DupControl_OnDeleteInitiated(object sender, EventArgs e)
        {
            txtDeleteProgress.Text = "Deleting...";
        }

        private void DupControl_OnScanStopping(object sender, EventArgs e)
        {
            txtProgress.Text = "Stopping...";
            txtProgress.Text = "Stopping...";
            button.IsEnabled = false;
            button.Content = "Start Scan";
        }

        private void DupControl_OnScanCompleted(object sender, ScanCompletedArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = 100;
                //txtProgress.Text = "100%";
                progressBar.IsIndeterminate = false;
                txtProgress.Text = e.StatusLabelText;

                progressBar.Value = 100;
                //txtProgress.Text = "100%";
                progressBar.IsIndeterminate = false;
                txtProgress.Text = e.StatusLabelText;


                button.IsEnabled = true;
                button.Content = "Start Scan";
                btnDelete.Visibility = Visibility.Visible;
                sep.Visibility = Visibility.Visible;
                if (e.SizeToDelete == 0)
                {
                    btnDelete.Visibility = Visibility.Hidden;
                    sep.Visibility = Visibility.Hidden;
                }
                else
                {
                    btnDelete.Visibility = Visibility.Visible;
                    sep.Visibility = Visibility.Visible;
                }

            });
        }

        private void DupControl_OnScanProgressing(object sender, ScanProgressingArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.IsIndeterminate = false;
                progressBar.Value = e.CurrentProgress;
                txtProgress.Text = $"{progressBar.Value}%";
            });
        }

        private void DupControl_OnFileCountFetched(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.IsIndeterminate = false;
                progressBar.Value = 0;
                txtProgress.Text = $"{progressBar.Value}%";
            });
        }

        private void DupControl_OnScanInitiated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                //if (!SearchInfo.Instance.ScanLocations.Any())
                //{
                //    SearchInfo.Instance.ScanLocations.Add(new Location { Name = @"C:\" });
                //    prefControl.lvLocations.ItemsSource = SearchInfo.Instance.ScanLocations;
                //    prefControl.lvLocations.Items.Refresh();
                //}

                //statusLabel.Text = "Scanning...";
                button.Content = "Stop Scan";
                progressBar.IsIndeterminate = true;
                txtProgress.Text = "Scanning...";

                progressBar.IsIndeterminate = true;
                txtProgress.Text = "Scanning...";
            });
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            txtProgress.Text = "";
            txtProgress.Text = "";
            //statusDeleteLabel.Text = "";
            gridScanProgress.Visibility = Visibility.Visible;
            //gridScanProgress1.Visibility = Visibility.Visible;
            gridDeleteProgress.Visibility = Visibility.Hidden;
            sep1.Visibility = Visibility.Hidden;
            deleteProgressBar.Value = 0;
            txtDeleteProgress.Text = "";
            btnDelete.Visibility = Visibility.Hidden;
            sep.Visibility = Visibility.Hidden;
            if (button.Content.ToString() == "Start Scan")
                OnScanStared(this, new EventArgs());
            else
                OnScanStopped(this, new EventArgs());
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            ModalWindow modalWindow = new ModalWindow();
            modalWindow.ShowDialog();
            //string valueFromModalTextBox = ModalWindow.delete;
            //var result = MessageBox.Show("Files will be deleted", "Deletion Summary", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ModalWindow.delete)
            {
                gridDeleteProgress.Visibility = Visibility.Visible;
                sep1.Visibility = Visibility.Visible;
                //OnDeleteStared(this, new EventArgs());
                await dupControl.TopPanel_OnDeleteStared(null, null).ConfigureAwait(false);
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            const string subject = "Feedback or help for Duplicate Remover Pro";
            Process.Start(new ProcessStartInfo (new Uri($"mailto:dpjha84@gmail.com?subject={subject}").ToString()) { UseShellExecute = true});
            e.Handled = true;
        }
    }
}
