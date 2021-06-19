using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using System.Globalization;
using BaseX;

namespace metagen
{
    public class DataBase
    {
        private string temp_database_file = @"./data/temp_database.txt";
        private Dictionary<string, MetaGenUser> _userData = new Dictionary<string, MetaGenUser>();
        private bool _should_update = false;
        public bool should_update
        {
            get
            {
                return _should_update;
            }
        }
        //public Dictionary<string, MetaGenUser> userData
        //{
        //    get
        //    {
        //        return this._userData;
        //    }
        //}

        public MetaGenUser GetUserData(string userID)
        {
            if (userID == null) return (MetaGenUser) null;
            if (_userData.ContainsKey(userID))
            {
                return _userData[userID];
            } else
            {
                return MakeNewUser(userID);
            }
        }

        public DataBase()
        {
            SetUpDatabase();
        }

        public void MakeNewFriend(string userID)
        {
            if (_userData.ContainsKey(userID)) return;
            _userData[userID] = new MetaGenUser { default_public = true, default_recording = true, is_banned = false, is_friend = true, total_recorded = 0f, total_recorded_public = 0f, userId = userID };
            _should_update = true;
        }
        public MetaGenUser MakeNewUser(string userID)
        {
            bool isFriend = FrooxEngine.Engine.Current.Cloud.Friends.IsFriend(userID);
            if (_userData.ContainsKey(userID))
            {
                _userData[userID].is_friend = isFriend;
                return _userData[userID];
            }
            //_userData[userID] = new MetaGenUser { default_public = isFriend, default_recording = isFriend, is_banned = false, is_friend = isFriend, total_recorded = 0f, total_recorded_public = 0f, userId = userID };
            _userData[userID] = new MetaGenUser { default_public = true, default_recording = isFriend, is_banned = false, is_friend = isFriend, total_recorded = 0f, total_recorded_public = 0f, userId = userID };
            _should_update = true;
            return _userData[userID];
        }

        public void UpdateRecordedTime(string userID, float time, bool is_public = false)
        {
            UniLog.Log("Updating recorded time " + userID);
            if (_userData.ContainsKey(userID))
            {
                _userData[userID].total_recorded += time;
                if (is_public)
                    _userData[userID].total_recorded_public += time;
            } else
            {
                _userData[userID] = new MetaGenUser { default_public = true, default_recording = true, is_banned = false, is_friend = true, total_recorded = time, total_recorded_public = 0f, userId = userID };
                if (is_public)
                    _userData[userID].total_recorded_public = time;
            }
            _should_update = true;

        }
        public void UpdateIsFriend(string userID, bool is_friend)
        {
            if (_userData.ContainsKey(userID))
            {
                _userData[userID].is_friend = is_friend;
                _should_update = true;
            }
        }
        public void UpdateDefaultPublic(string userID, bool default_public)
        {
            if (_userData.ContainsKey(userID))
            {
                _userData[userID].default_public = default_public;
            } else
            {
                MakeNewFriend(userID);
            }
            _should_update = true;
        }

        private void SetUpDatabase()
        {
            if (!File.Exists(temp_database_file))
            {
                UniLog.Log("Creating database file");
                File.Create(temp_database_file);
            }
            using (var reader = new StreamReader(temp_database_file))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                foreach(MetaGenUser item in csv.GetRecords<MetaGenUser>())
                {
                    _userData[item.userId] = item;
                }
            }
        }

        public void SaveDatabase()
        {
            UniLog.Log("Saving database");
            _should_update = false;
            try {
                List<MetaGenUser> userDatas = new List<MetaGenUser>();
                foreach(var item in _userData)
                {
                    userDatas.Add(item.Value);
                }
                using (var writer = new StreamWriter(temp_database_file))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(userDatas);
                }
            } catch (Exception e)
            {
                UniLog.Log("OwO error in dataBase write: " + e.Message);
                UniLog.Log(e.StackTrace);
            }
        }
    }
}
