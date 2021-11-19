using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public class ScanProgressingArgs : EventArgs
    {
        public int CurrentProgress { get; set; }
    }

    public class DeleteProgressingArgs : EventArgs
    {
        public double CurrentProgress { get; set; }
    }

    public class ScanCompletedArgs : EventArgs
    {
        public string StatusLabelText { get; set; }

        public long SizeToDelete { get; set; }
    }

    public class DeleteSizeChangedArgs : EventArgs
    {
        public string NewSize { get; set; }
    }

    public class DeleteCompletedArgs : EventArgs
    {
        public string DeleteStatusLabelText { get; set; }

        public string DeletedSize { get; set; }

        public List<DeletedFile> DeletedFiles { get; set; }
    }
}
