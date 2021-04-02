using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public class SearchInfo
    {
        const string settingFile = "setting.json";

        static SearchInfo()
        {
            try
            {
                var cacheData = FileHelper.Read(settingFile).Result;
                Instance = string.IsNullOrWhiteSpace(cacheData) ? DefaultInstance : JsonConvert.DeserializeObject<SearchInfo>(cacheData);
            }
            catch (Exception ex)
            {
                Instance = DefaultInstance;
            }
        }

        static SearchInfo DefaultInstance { get; } = new SearchInfo
        {
            ScanLocations = new List<Location>
                {
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
                }
        };

        public static void UpdateSetting()
        {
            FileHelper.Write(settingFile, JsonConvert.SerializeObject(Instance)).ConfigureAwait(false);
        }

        public static SearchInfo Instance { get; private set; }

        public List<Location> ScanLocations { get; set; }

        public bool IncludeImages { get; set; } = true;

        public bool IncludeAudios { get; set; }

        public bool IncludeVideos { get; set; }

        public bool IncludeDocuments { get; set; }

        public bool IncludeHiddenFolders { get; set; }

        public long MinSize { get; set; } = 1024 * 1024;

        public long MaxSize { get; set; } = -1;// = new FileSize(0, "KB");

        public DateTime? ModifiedAfter { get; set; }

        public DateTime? ModifiedBefore { get; set; }

        public DeleteOption DeleteOption { get; set; } = DeleteOption.SendToRecycleBin;

        public bool ShowWelcomePageAtStartup { get; set; } = true;

        public DuplicationMarkingCriteria DupCriteria { get; set; }

        //public AutoDeletionMarking AutoMarking { get; set; }

        public bool CacheHashData { get; set; } = true;

        public IEnumerable<string> CustomFileTypes { get; set; } = Enumerable.Empty<string>();
    }

    public enum DuplicationMarkingCriteria { FileContent, FileName }
    //public enum AutoDeletionMarking { NewerFilesInGroup, None }

    public class Location
    {
        public string Name { get; set; }

        public bool? IncludeSubfolders { get; set; } = true;

        public bool Include { get; set; } = true;

        //public bool Include => !Exclude;

        public bool ExcludedInTree { get; set; }

        public string RemoveIcon { get; set; } = "..\\..\\images\\Remove.png";

        string icon;
        public string Icon
        {
            get
            {
                icon = new DirectoryInfo(Name).Root.FullName == Name ? "..\\..\\images\\drive.png" : "..\\..\\images\\folder2.png";
                return icon;
            }
            set
            {
                icon = value;
            }
        }
    }

    public enum DeleteOption { SendToRecycleBin, PermanentDelete };

    public class FileSize
    {
        int _val;
        public int Val 
        { 
            get { return _val; }
            set
            {
                var sz = GetSize(value, Unit);
                if (sz <= 0) throw new ArgumentException("Max File Size exceeded");
                _val = value;
            }
        }

        string _unit;
        public string Unit
        {
            get { return _unit; }
            set
            {
                var sz = GetSize(Val, value);
                if (sz <= 0) throw new ArgumentException("Max File Size exceeded");
                _unit = value;
            }
        }

        public FileSize(long bytes)
        {

        }

        public FileSize(int val, string unit)
        {
            _val = val;
            _unit = unit;
            //if (unit == "GB" && val > 5) throw new ArgumentException("Max File Size exceeded");
            //Val = val;
            //Unit = unit;
        }

        private long GetSize(int val, string unit)
        {
            switch (unit)
            {
                case "KB":
                    return val * 1024;
                case "MB":
                    return val * 1024 * 1024;
                case "GB":
                    return val * 1024 * 1024 * 1024;
            }
            return 0;
        }

        long _size = 0;

        public void SetZero()
        {
            _size = 0;
        }

        public long Size
        {
            get
            {
                _size = GetSize(Val, Unit);
                return _size;
            }
        }
    }
}
