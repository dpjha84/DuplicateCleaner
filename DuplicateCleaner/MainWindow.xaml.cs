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

            topPanelControl.Visibility = Visibility.Visible;
            (tabControl.Items[0] as TabItem).Visibility = Visibility.Visible;
            tabControl.SelectedIndex = 0;

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
            (tabControl.Items[2] as TabItem).Visibility = Visibility.Visible;
            tabControl.SelectedIndex = 2;
        }

        private void TopPanel_OnScanStared(object sender, EventArgs e)
        {
            tabControl.SelectedIndex = 1;
            (tabControl.Items[1] as TabItem).Visibility = Visibility.Visible;
        }

        private void WelcomeControl_OnScanCriteriaClicked(object sender, EventArgs e)
        {
            topPanelControl.Visibility = Visibility.Visible;
            (tabControl.Items[0] as TabItem).Visibility = Visibility.Visible;
            tabControl.SelectedIndex = 0;
        }
    }
}