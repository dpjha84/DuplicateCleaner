using System.Windows.Controls;

namespace DuplicateCleaner.UserControls
{
    /// <summary>
    /// Interaction logic for DuplicatesControl.xaml
    /// </summary>
    public partial class CleanupSummary : UserControl
    {
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
        }
    }
}
