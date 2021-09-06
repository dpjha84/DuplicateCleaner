using System;
using System.Windows;
using System.Windows.Controls;

namespace DuplicateCleaner
{
    public partial class MainWindow : Window
    {
        public bool paused, terminated, processing;
        public MainWindow()
        {
            InitializeComponent();
            if (SearchInfo.Instance.ShowWelcomePageAtStartup)
                tabControl.SelectedIndex = 0;
            else
                WelcomeControl_OnScanCriteriaClicked(null, null);
            welcomeControl.OnScanCriteriaClicked += WelcomeControl_OnScanCriteriaClicked;
            topPanelControl.dupControl = dupControl;
            topPanelControl.OnScanStared += TopPanel_OnScanStared;
            dupControl.Main = this;
            dupControl.CleanupWindow = cleanupControl;
            dupControl.TopPanel = topPanelControl;
            dupControl.OnDeleteCompleted += DupControl_OnDeleteCompleted;
            cleanupControl.dupControl = dupControl;
        }

        private void DupControl_OnDeleteCompleted(object sender, EventArgs e)
        {
            (tabControl.Items[3] as TabItem).Visibility = Visibility.Visible;
            tabControl.SelectedIndex = 3;
        }

        private void TopPanel_OnScanStared(object sender, EventArgs e)
        {
            tabControl.SelectedIndex = 2;
            (tabControl.Items[2] as TabItem).Visibility = Visibility.Visible;
            SearchInfo.UpdateSetting();
        }

        private void WelcomeControl_OnScanCriteriaClicked(object sender, EventArgs e)
        {
            topPanelControl.Visibility = Visibility.Visible;
            (tabControl.Items[1] as TabItem).Visibility = Visibility.Visible;
            tabControl.SelectedIndex = 1;
        }
    }
}