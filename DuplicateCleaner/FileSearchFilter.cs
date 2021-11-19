using System;
using System.Collections.Generic;
using System.IO;

namespace DuplicateCleaner
{
    public struct FileSearchFilter
    {
        public SearchOption SearchOption { get; set; }
        
        public HashSet<string> ExtensionList { get; set; }

        public HashSet<string> ExcludedList { get; set; }

        public long MinSize { get; set; }

        public long MaxSize { get; set; }

        public DateTime? ModifyAfter { get; set; }

        public DateTime? ModifyBefore { get; set; }

        public bool IncludeHiddenFolders { get; set; }

        public bool IncludeSystemFolders { get; set; }
    }
}
