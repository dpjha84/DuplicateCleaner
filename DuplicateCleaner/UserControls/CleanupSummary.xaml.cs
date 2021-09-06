using System.Windows.Controls;

namespace DuplicateCleaner.UserControls
{
    public partial class BaseControl : UserControl
    {

    }
    /// <summary>
    /// Interaction logic for DuplicatesControl.xaml
    /// </summary>
    public partial class CleanupSummary : BaseControl
    {
        public DuplicatesControl dupControl { get; set; }
        string _size;
        public string DeletedSize
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                txtSummary.Text = $"Hurray! You were able to clean up {_size} space.";
            }
        }
        public CleanupSummary()
        {
            InitializeComponent();
            Loaded += CleanupSummary_Loaded;
        }
        bool attached = false;
        private void CleanupSummary_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if(!attached)
                dupControl.OnDeleteCompleted += DupControl_OnDeleteCompleted;
        }

        private void DupControl_OnDeleteCompleted(object sender, DeleteCompletedArgs e)
        {
            attached = true;
            DeletedSize = e.DeletedSize;
            dgDeleted.ItemsSource = e.DeletedFiles;
        }
    }
}
