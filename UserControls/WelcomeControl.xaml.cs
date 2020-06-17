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

namespace DuplicateCleaner.UserControls
{
    /// <summary>
    /// Interaction logic for WelcomeControl.xaml
    /// </summary>
    public partial class WelcomeControl : UserControl
    {
        public delegate void ScanCriteriaClickedDelegate(object sender, EventArgs e);
        public event ScanCriteriaClickedDelegate OnScanCriteriaClicked;
        public WelcomeControl()
        {
            InitializeComponent();
            chkShowWelcomePage.IsChecked = SearchInfo.Instance.ShowWelcomePageAtStartup;
        }

        private void btnStartCriteria_Click(object sender, RoutedEventArgs e)
        {
            OnScanCriteriaClicked(this, new EventArgs());
        }

        private void chkShowWelcomePage_Checked(object sender, RoutedEventArgs e)
        {
            SearchInfo.Instance.ShowWelcomePageAtStartup = true;
            SearchInfo.UpdateSetting();
        }

        private void chkShowWelcomePage_Unchecked(object sender, RoutedEventArgs e)
        {
            SearchInfo.Instance.ShowWelcomePageAtStartup = false;
            SearchInfo.UpdateSetting();
        }
    }
}
