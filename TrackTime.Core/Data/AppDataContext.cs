using System;
using System.Collections.Generic;
using System.Text;

using LiteDB;

using TrackTime.Models;

namespace TrackTime.Data
{
    public class AppDataContext : IAppDataContext
    {
        private ILiteCollection<WorkItem>? _workItems;
        private ILiteCollection<TimeEntry>? _timeEntries;
        private ILiteCollection<Customer>? _customers;

        private ConnectionString _connStr;
        private LiteDatabase? _database = null;
        static AppDataContext()
        {
            SetupMappings();
        }

        public AppDataContext(IDbPath dBPath)
        {
            var path = dBPath ?? throw new ArgumentNullException(nameof(dBPath));
            _connStr = new ConnectionString
            {
                Filename = path.Value,
                Upgrade = true,
                Connection = ConnectionType.Direct
            };

        }

        public LiteDatabase Database
        {
            get
            {
                if (_database == null)
                    _database = new LiteDatabase(_connStr);
                return _database;
            }
        }

        public ILiteCollection<Customer> Customers
        {
            get
            {
                if (_customers == null)
                    _customers = Database.GetCollection<Customer>();
                return _customers;
            }
        }

        public ILiteCollection<WorkItem> WorkItems
        {
            get
            {
                if (_workItems == null)
                    _workItems = Database.GetCollection<WorkItem>();
                return _workItems;
            }
        }

        public ILiteCollection<TimeEntry> TimeEntries
        {
            get
            {
                if (_timeEntries == null)
                    _timeEntries = Database.GetCollection<TimeEntry>();
                return _timeEntries;
            }
        }

        private static void SetupMappings()
        {
            var mapper = BsonMapper.Global;
            mapper.EnumAsInteger = true;
            //mapper.Entity<WorkItem>();
            //mapper.Entity<TimeEntry>();
        }

        public void DisconnectDB()
        {
            _database?.Dispose();
            _database = null;
        }
    }


}
