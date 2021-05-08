using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using System.Globalization;
using BaseX;
using FrooxEngine;

namespace metagen
{
    public class MetaDataManager
    {
        private MetaGen metagen_comp;
        //Metadata refers to the per-recording data about the user
        public Dictionary<User, UserMetadata> userMetaData = new Dictionary<User, UserMetadata>();
        //the user data refers to the data about the user gotten from the database
        public Dictionary<User, MetaGenUser> users = new Dictionary<User, MetaGenUser>();
        private DataBase dataBase
        {
            get
            {
                return metagen_comp.dataBase;
            }
        }
        public MetaDataManager(MetaGen component)
        {
            metagen_comp = component;
        }

        public void GetUsers()
        {
            users = new Dictionary<User, MetaGenUser>();
            foreach (User user in metagen_comp.World.AllUsers)
            {
                //if (!metagen_comp.record_local_user && user == metagen_comp.World.LocalUser) continue;
                users[user] = dataBase.GetUserData(user.UserID);
            }
        }
        public void AddUserMetaData(User user)
        {
            UserMetadata thisUserMetadata = new UserMetadata
            {
                userRefId = user.ReferenceID.ToString(),
                userId = user.UserID,
                isPublic = users[user].default_public,
                isRecording = users[user].default_recording,
                headDevice = user.HeadDevice.ToString(),
                platform = user.Platform.ToString(),
                bodyNodes = String.Join(",", user.BodyNodes.Select(n => n.ToString())),
                devices = String.Join(",", user.Devices.Where<SyncVar>((Func<SyncVar, bool>)(i => i.IsDictionary)).Select(d => d["Type"].GetValue<string>(true))),
            };
            userMetaData[user] = thisUserMetadata;
        }
        public void RemoveUserMetaData(User user)
        {
            userMetaData.Remove(user);
        }
        public void GetUserMetaData()
        {
            GetUsers();
            userMetaData = new Dictionary<User, UserMetadata>();
            foreach(var item in users)
            {
                User user = item.Key;
                bool isRecording = users[user].default_recording;
                bool isPublic = users[user].default_public;
                //if (!metagen_comp.record_local_user && user == metagen_comp.World.LocalUser)
                //{
                //    isRecording = false;
                //    isPublic = true;
                //}
                //else if (user == metagen_comp.World.LocalUser)
                //{
                //    isRecording = true;
                //    isPublic = true;
                //}
                isRecording = GetUserRecording(user);
                UniLog.Log("Is user "+user.UserID+" recording");
                UniLog.Log(isRecording);
                UserMetadata thisUserMetadata = new UserMetadata
                {
                    userRefId = user.ReferenceID.ToString(),
                    userId = user.UserID,
                    isPublic = isPublic,
                    isRecording = isRecording,
                    headDevice = user.HeadDevice.ToString(),
                    platform = user.Platform.ToString(),
                    bodyNodes = String.Join(",", user.BodyNodes.Select(n => n.ToString())),
                    devices = String.Join(",", user.Devices.Where<SyncVar>((Func<SyncVar, bool>)(i => i.IsDictionary)).Select(d => d["Type"].GetValue<string>(true))),
                };
                userMetaData[user] = thisUserMetadata;
            }
        }
        public bool GetUserRecording(User user)
        {
            string user_id = user.UserID;
            bool value = false;
            if (metagen_comp.users_config_space.TryReadValue<bool>(user_id.Substring(2).Replace("-"," "), out value))
            {
                return value;
            } else
            {
                return false;
            }
        }

        public void UpdateUserPublic(User user, bool isPublic)
        {
            if (!userMetaData.ContainsKey(user))
            {
                AddUserMetaData(user);
            }
            userMetaData[user].isPublic = isPublic;
        }
        public void UpdateUserRecording(User user, bool isRecording)
        {
            if (!userMetaData.ContainsKey(user))
            {
                AddUserMetaData(user);
            }
            userMetaData[user].isRecording = isRecording;
        }
        public void WriteUserMetaData()
        {
            List<UserMetadata> user_metadatas = new List<UserMetadata>();
            foreach(var item in userMetaData)
            {
                user_metadatas.Add(item.Value);
            }
            using (var writer = new StreamWriter(Path.Combine(metagen_comp.dataManager.saving_folder,"user_metadata.csv")))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(user_metadatas);
            }
        }
    }
}
