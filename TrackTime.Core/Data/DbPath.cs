using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace TrackTime.Data
{
    public class DbPath : IDbPath
    {
        public DbPath()
        {
            //var dir = Path.Combine(ApplicationData.Current.LocalFolder.Path, "TrackTime");
            //Directory.CreateDirectory(dir);
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            Value = Path.Combine(dir, "TrackTimeData.ldb");
        }

        public string Value { get; }
    }
}
