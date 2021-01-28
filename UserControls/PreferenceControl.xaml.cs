using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DuplicateCleaner.UserControls
{
    public partial class PreferenceControl : UserControl
    {
        readonly SearchInfo searchInfo = SearchInfo.Instance;
        readonly bool loaded = false;

        public PreferenceControl()
        {
            InitializeComponent();

            chkImages.ToolTip = string.Join(", ", FileHelper.Pics);
            chkMusic.ToolTip = string.Join(", ", FileHelper.Audio);
            chkVideo.ToolTip = string.Join(", ", FileHelper.Video);
            chkDocs.ToolTip = string.Join(", ", FileHelper.Docs);
            LoadSetting();

            txtMaxSize.PreviewTextInput += NumberValidationTextBox;
            txtMinSize.PreviewTextInput += NumberValidationTextBox;
            txtMinSize.LostFocus += TxtMinSize_LostFocus;
            txtMaxSize.LostFocus += TxtMaxSize_LostFocus;

            imgAddFolder.Source = new BitmapImage(new Uri("..\\..\\images\\AddFolder2.png", UriKind.Relative));
            loaded = true;
        }

        string FormatText(TextBox text)
        {
            var trimmedText = text.Text.Trim();
            if (trimmedText == "")
                text.Text = "0";
            else if (int.TryParse(trimmedText, out int num) && num < 1024)
                text.Text = num.ToString();
            else
                text.Text = "1023";
            return text.Text;
        }

        private void TxtMinSize_LostFocus(object sender, RoutedEventArgs e)
        {
            searchInfo.MinSize = SizeHelper.GetSizeInBytes(FormatText(sender as TextBox), ((ContentControl)cmbMinSize.SelectedValue).Content as string);
        }

        private void TxtMaxSize_LostFocus(object sender, RoutedEventArgs e)
        {
            searchInfo.MaxSize = SizeHelper.GetSizeInBytes(FormatText(sender as TextBox), ((ContentControl)cmbMaxSize.SelectedValue).Content as string);
        }

        void LoadSetting()
        {
            cmbDupCriteria.SelectedIndex = (int)searchInfo.DupCriteria;
            cmbDeleteOption.SelectedIndex = (int)searchInfo.DeleteOption;
            chkCacheScannedData.IsChecked = searchInfo.CacheHashData;
            btnClearNow.Style = searchInfo.CacheHashData 
                ? FindResource("deleteButton") as Style : FindResource("disabledButton") as Style;

            lvLocations.ItemsSource = searchInfo.ScanLocations;
            lvLocations.Items.Refresh();

            chkImages.IsChecked = searchInfo.IncludeImages;
            chkMusic.IsChecked = searchInfo.IncludeAudios;
            chkVideo.IsChecked = searchInfo.IncludeVideos;
            chkDocs.IsChecked = searchInfo.IncludeDocuments;

            chkHiddenFolder.IsChecked = searchInfo.IncludeHiddenFolders;
            if (searchInfo.MinSize != -1)
            {
                chkMinSize.IsChecked = true;
                txtMinSize.IsEnabled = true;
                cmbMinSize.IsEnabled = true;
                var suffix = SizeHelper.Suffix(searchInfo.MinSize).Split();
                txtMinSize.Text = double.Parse(suffix[0]).ToString();
                cmbMinSize.SelectedIndex = GetIndexByUnit(suffix[1]);
            }
            else
            {
                chkMinSize.IsChecked = false;
                txtMinSize.IsEnabled = false;
                cmbMinSize.IsEnabled = false;
                cmbMinSize.SelectedIndex = 1;
                txtMinSize.Text = "1";
                searchInfo.MinSize = -1;
            }
            if (searchInfo.MaxSize != -1)
            {
                chkMaxSize.IsChecked = true;
                txtMaxSize.IsEnabled = true;
                cmbMaxSize.IsEnabled = true;
                var suffix = SizeHelper.Suffix(searchInfo.MaxSize).Split();
                txtMaxSize.Text = double.Parse(suffix[0]).ToString();
                cmbMaxSize.SelectedIndex = GetIndexByUnit(suffix[1]);
            }
            else
            {
                chkMaxSize.IsChecked = false;
                txtMaxSize.IsEnabled = false;
                cmbMaxSize.IsEnabled = false;
                cmbMaxSize.SelectedIndex = 1;
                txtMaxSize.Text = "100";
                searchInfo.MaxSize = -1;
            }

            if (searchInfo.ModifiedAfter != null)
            {
                var date = searchInfo.ModifiedAfter;
                chkMinModifyDate.IsChecked = true;
                dpMinModifyDate.IsEnabled = true;
                dpMinModifyDate.SelectedDate = date;
            }
            else
            {
                chkMinModifyDate.IsChecked = false;
                dpMinModifyDate.IsEnabled = false;
            }
            if (searchInfo.ModifiedBefore != null)
            {
                var date = searchInfo.ModifiedBefore;
                chkMaxModifyDate.IsChecked = true;
                dpMaxModifyDate.IsEnabled = true;
                dpMaxModifyDate.SelectedDate = date;
            }
            else
            {
                chkMaxModifyDate.IsChecked = false;
                dpMaxModifyDate.IsEnabled = false;
            }
        }

        int GetIndexByUnit(string val)
        {
            if (val == "KB") return 0;
            if (val == "MB") return 1;
            if (val == "GB") return 2;
            return 0;
        }

        private void btnAddLocation_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    searchInfo.ScanLocations.Add(new Location { Name = dialog.SelectedPath });
                    lvLocations.ItemsSource = searchInfo.ScanLocations;
                    lvLocations.Items.Refresh();
                }
            }
        }

        private void btnRemoveLocation_Click(object sender, RoutedEventArgs e)
        {
            var location = ((FrameworkElement)sender).DataContext as Location;
            if (location != null)
            {
                searchInfo.ScanLocations.Remove(location);
                lvLocations.Items.Refresh();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void cmbMaxSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtMaxSize.Text))
                searchInfo.MaxSize = SizeHelper.GetSizeInBytes(txtMaxSize.Text, ((ContentControl)cmbMaxSize.SelectedValue).Content as string);
        }

        private void cmbMinSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtMinSize.Text))
                searchInfo.MinSize = SizeHelper.GetSizeInBytes(txtMinSize.Text, ((ContentControl)cmbMinSize.SelectedValue).Content as string);
        }

        private void chkMinSize_Checked(object sender, RoutedEventArgs e)
        {
            txtMinSize.IsEnabled = true;
            cmbMinSize.IsEnabled = true;
            if (!string.IsNullOrWhiteSpace(txtMinSize.Text))
                searchInfo.MinSize = SizeHelper.GetSizeInBytes(txtMinSize.Text, ((ContentControl)cmbMinSize.SelectedValue).Content as string);
        }

        private void chkMaxSize_Checked(object sender, RoutedEventArgs e)
        {
            txtMaxSize.IsEnabled = true;
            cmbMaxSize.IsEnabled = true;
            if (!string.IsNullOrWhiteSpace(txtMaxSize.Text))
                searchInfo.MaxSize = SizeHelper.GetSizeInBytes(txtMaxSize.Text, ((ContentControl)cmbMaxSize.SelectedValue).Content as string);
        }

        private void chkMinSize_Unchecked(object sender, RoutedEventArgs e)
        {
            txtMinSize.IsEnabled = false;
            cmbMinSize.IsEnabled = false;
            searchInfo.MinSize = -1;
        }

        private void chkMaxSize_Unchecked(object sender, RoutedEventArgs e)
        {
            txtMaxSize.IsEnabled = false;
            cmbMaxSize.IsEnabled = false;
            searchInfo.MaxSize = -1;
        }

        private void chkMinModifyDate_Checked(object sender, RoutedEventArgs e)
        {
            dpMinModifyDate.IsEnabled = true;
            searchInfo.ModifiedAfter = dpMinModifyDate.SelectedDate;
        }

        private void chkMinModifyDate_Unchecked(object sender, RoutedEventArgs e)
        {
            dpMinModifyDate.IsEnabled = false;
            searchInfo.ModifiedAfter = null;
        }

        private void chkMaxModifyDate_Checked(object sender, RoutedEventArgs e)
        {
            dpMaxModifyDate.IsEnabled = true;
            searchInfo.ModifiedBefore = dpMaxModifyDate.SelectedDate;
        }

        private void chkMaxModifyDate_Unchecked(object sender, RoutedEventArgs e)
        {
            dpMaxModifyDate.IsEnabled = false;
            searchInfo.ModifiedBefore = null;
        }

        private void dpMinModifyDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            searchInfo.ModifiedAfter = dpMinModifyDate.SelectedDate;
        }

        private void dpMaxModifyDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            searchInfo.ModifiedBefore = dpMaxModifyDate.SelectedDate;
        }

        private void chkHiddenFolder_Checked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeHiddenFolders = true;
        }

        private void chkHiddenFolder_Unchecked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeHiddenFolders = false;
        }

        private void chkImages_Checked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeImages = true;
            SetBorderColor(sender, System.Windows.Media.Brushes.Green);
        }

        private void chkImages_Unchecked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeImages = false;
            SetBorderColor(sender, System.Windows.Media.Brushes.Gray);
        }

        private void chkMusic_Checked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeAudios = true;
            SetBorderColor(sender, System.Windows.Media.Brushes.Green);
        }

        private void chkMusic_Unchecked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeAudios = false;
            SetBorderColor(sender, System.Windows.Media.Brushes.Gray);
        }

        private void chkVideo_Checked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeVideos = true;
            SetBorderColor(sender, System.Windows.Media.Brushes.Green);
        }

        private void chkVideo_Unchecked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeVideos = false;
            SetBorderColor(sender, System.Windows.Media.Brushes.Gray);
        }

        private void chkDocs_Checked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeDocuments = true;
            SetBorderColor(sender, System.Windows.Media.Brushes.Green);
        }

        private void chkDocs_Unchecked(object sender, RoutedEventArgs e)
        {
            searchInfo.IncludeDocuments = false;
            SetBorderColor(sender, System.Windows.Media.Brushes.Gray);
        }

        void SetBorderColor(object sender, System.Windows.Media.Brush col)
        {
            var textBlock = ((sender as CheckBox)?.Content as Border)?.Child as TextBlock;
            if (textBlock != null)
            {
                if (col == System.Windows.Media.Brushes.Green)
                    textBlock.Foreground = System.Windows.Media.Brushes.White;
                else
                    textBlock.Foreground = System.Windows.Media.Brushes.LightGray;
            }
            if ((sender as CheckBox).Content as Border != null)
                ((sender as CheckBox).Content as Border).Background = col;
        }

        private void rdMoveToRecycleBin_Click(object sender, RoutedEventArgs e)
        {
            searchInfo.DeleteOption = DeleteOption.SendToRecycleBin;
        }

        private void rdDeletePermanent_Click(object sender, RoutedEventArgs e)
        {
            searchInfo.DeleteOption = DeleteOption.PermanentDelete;
        }

        private void lvLocations_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void chkSubfolders_Checked(object sender, RoutedEventArgs e)
        {
            var location = ((FrameworkElement)sender).DataContext as Location;
            location.IncludeSubfolders = true;
            location.Exclude = false;
            lvLocations.Items.Refresh();
            SetBorderColor(sender, System.Windows.Media.Brushes.Green);
        }

        private void chkSubfolders_Unchecked(object sender, RoutedEventArgs e)
        {
            var location = ((FrameworkElement)sender).DataContext as Location;
            location.IncludeSubfolders = false;
            SetBorderColor(sender, System.Windows.Media.Brushes.Gray);
        }

        private void chkExclude_Checked(object sender, RoutedEventArgs e)
        {
            var location = ((FrameworkElement)sender).DataContext as Location;
            location.Exclude = true;
            location.IncludeSubfolders = false;
            lvLocations.Items.Refresh();
            SetBorderColor(sender, System.Windows.Media.Brushes.Green);
        }

        private void chkExclude_Unchecked(object sender, RoutedEventArgs e)
        {
            var location = ((FrameworkElement)sender).DataContext as Location;
            location.Exclude = false;
            SetBorderColor(sender, System.Windows.Media.Brushes.Gray);
        }

        private async void btnClearNow_Click(object sender, RoutedEventArgs e)
        {
            tbCacheCleared.Visibility = Visibility.Visible;
            await HashHelper.ResetCacheAsync();
        }

        private void cmbDeleteOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loaded) return;
            searchInfo.DeleteOption = (DeleteOption)cmbDeleteOption.SelectedIndex;
        }

        private void chkCacheScannedData_Checked(object sender, RoutedEventArgs e)
        {
            searchInfo.CacheHashData = true;
            btnClearNow.Style = FindResource("deleteButton") as Style;
        }

        private async void chkCacheScannedData_Unchecked(object sender, RoutedEventArgs e)
        {
            var task = HashHelper.ResetCacheAsync();
            searchInfo.CacheHashData = false;
            btnClearNow.Style = FindResource("disabledButton") as Style;
            tbCacheCleared.Visibility = Visibility.Collapsed;
            await task;
        }

        private void cmbDupCriteria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loaded) return;
            searchInfo.DupCriteria = (DuplicationMarkingCriteria)cmbDeleteOption.SelectedIndex;
        }

        private void txtCustomFileType_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtCustomFileType_LostFocus(object sender, RoutedEventArgs e)
        {
            var types = txtCustomFileType.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            searchInfo.CustomFileTypes = types ?? Enumerable.Empty<string>();
        }

        //private void cmbDefaultMarking_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (!loaded) return;
        //    searchInfo.AutoMarking = cmbDefaultMarking.SelectedIndex == 0 ? AutoDeletionMarking.NewerFilesInGroup : AutoDeletionMarking.None;
        //}
    }
}
