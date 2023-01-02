using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DuplicateCleaner
{
    public class SearchInfo
    {
        static string settingFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setting.json");
        //static string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        static object lockObject = new object();

        static SearchInfo()
        {
            var drives = DriveInfo.GetDrives().Select(x => x.Name);
            lock (lockObject)
            {
                try
                {
                    string? cacheData = null;
                    if (File.Exists(settingFile))
                        cacheData = File.ReadAllText(settingFile);

                    //File.WriteAllText(logFile, $"ZZZZZ: {settingFile}");
                    //File.WriteAllText(logFile, $"ZZZZZ: {cacheData}");

                    var temp = JsonConvert.DeserializeObject<SearchInfo>(cacheData);
                    _instance = !string.IsNullOrWhiteSpace(cacheData) && temp.ScanLocations.Any() ? temp : DefaultInstance;
                }
                catch(Exception ex)
                {
                    //File.WriteAllText(logFile, ex.Message);
                    _instance = DefaultInstance;
                }
            }
        }

        static SearchInfo DefaultInstance { get; } = new SearchInfo
        {
            ScanLocations = DriveInfo.GetDrives().Select(x => new Location { Name = x.Name }).ToList()
        };

        public static void UpdateSetting()
        {
            File.WriteAllText(settingFile, JsonConvert.SerializeObject(Instance));
        }

        private static SearchInfo? _instance;
        public static SearchInfo Instance
        {
            get
            {
                lock (lockObject)
                {
                    return _instance;
                }
            }
            set
            {
                lock (lockObject)
                {
                    { _instance = value; }
                }
            }
        }

        public List<Location> ScanLocations { get; set; } = new List<Location>();

        public bool IncludeImages { get; set; } = true;

        public bool IncludeAudios { get; set; }

        public bool IncludeVideos { get; set; }

        public bool IncludeDocuments { get; set; }

        public bool IncludeHiddenFolders { get; set; }

        public bool IncludeSystemFolders { get; set; }

        public long MinSize { get; set; } = 1024 * 1024;

        public long MaxSize { get; set; } = -1;// = new FileSize(0, "KB");

        public DateTime? ModifiedAfter { get; set; }

        public DateTime? ModifiedBefore { get; set; }

        public DeleteOption DeleteOption { get; set; } = DeleteOption.SendToRecycleBin;

        //public bool ShowWelcomePageAtStartup { get; set; } = false;

        public DuplicationMarkingCriteria DupCriteria { get; set; }

        //public AutoDeletionMarking AutoMarking { get; set; }

        public bool CacheHashData { get; set; } = true;

        public IEnumerable<string> CustomFileTypes { get; set; } = new List<string>();
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

        public string RemoveIcon { get; set; } = "..\\images\\Remove.png";

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
