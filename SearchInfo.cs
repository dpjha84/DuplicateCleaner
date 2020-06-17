using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            catch (Exception)
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

        public long MinSize { get; set; }

        public long MaxSize { get; set; }

        public DateTime? ModifiedAfter { get; set; }

        public DateTime? ModifiedBefore { get; set; }

        public RecycleOption DeleteOption { get; set; } = RecycleOption.SendToRecycleBin;

        public bool ShowWelcomePageAtStartup { get; set; } = true;
    }

    public class Location
    {
        public string Name { get; set; }

        public bool IncludeSubfolders { get; set; } = true;

        public bool Exclude { get; set; }

        public bool ExcludedInTree { get; set; }

        public string RemoveIcon { get; set; } = "..\\..\\images\\Remove.png";
    }
}
