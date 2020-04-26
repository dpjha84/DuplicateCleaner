using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DuplicateCleaner
{
    public class SearchInfo
    {
        static readonly SearchInfo _instance;

        static SearchInfo()
        {
            try
            {
                _instance = JsonConvert.DeserializeObject<SearchInfo>(File.ReadAllText("setting.json"));
            }
            catch (Exception ex)
            {
                _instance = new SearchInfo();
                _instance.ScanLocations = new List<Location>
                {
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
                    new Location { Name = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
                    new Location { Name = "E:\\" },
                    new Location { Name = "E:\\Test\\Sub" },
                };
            }            
        }

        public static SearchInfo Instance => _instance;

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
