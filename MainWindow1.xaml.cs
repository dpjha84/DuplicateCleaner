using System.Windows;
using System.Windows.Controls;

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
            dupControl.main = this;
            dupControl.cleanupWindow = cleanupControl;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 2;
            //File.WriteAllText(".\\setting.json", JsonConvert.SerializeObject(SearchInfo.Instance));
            Scan();
        }

        public void Scan()
        {
            Reset();
            (tabControl.Items[2] as TabItem).Visibility = Visibility.Visible;
            dupControl.Scan();
        }

        private void Reset()
        {
            statusLabel.Text = "";


            gridScanProgress.Visibility = Visibility.Visible;
            gridDeleteProgress.Visibility = Visibility.Collapsed;
            deleteProgressBar.Value = 0;
            txtDeleteProgress.Text = "";
        }

        private void btnRate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnPrivacy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnStartCriteria_Click(object sender, RoutedEventArgs e)
        {
            topPanel.Visibility = Visibility.Visible;
            (tabControl.Items[1] as TabItem).Visibility = Visibility.Visible;
            tabControl.SelectedIndex = 1;

        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            dupControl.btnDelete_Click(null, null);
        }
    }
}