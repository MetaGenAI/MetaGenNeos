using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using System.IO;
using CsvHelper;
using System.Globalization;
using BaseX;

namespace metagen
{
    public class DataManager : Component
    {
        private bool have_users_changed = false;
        private string _saving_folder = @"./data";
        //public readonly string last_saving_folder;
        private string root_saving_folder = @"./data";
        private string session_saving_folder = "";
        private int section = 0;
        public bool have_started_recording_session = false;
        public MetaGen metagen_comp;
        public string saving_folder
        {
            get
            {
                return this._saving_folder;
            }
        }
        public string last_saving_folder
        {
            get; private set;
        }
        public string reading_folder
        {
            get
            {
                return last_saving_folder;
            }
        }

        public DataManager()
        {
            if (!Directory.Exists(root_saving_folder))
            {
                Directory.CreateDirectory(root_saving_folder);
            }

        }
        public string scapeWorldID(string worldID)
        {
            return worldID.Replace(@":", @"_").Replace(@"-", @"_");
        }
        public void StartRecordingSession()
        {
            Guid g = Guid.NewGuid();
            World currentWorld = this.World;
            UniLog.Log(currentWorld.CorrespondingWorldId);
            UniLog.Log(currentWorld.SessionId);
            string world_id = currentWorld.CorrespondingWorldId ?? "new_world";
            string escaped_world_id = scapeWorldID(world_id);
            if (!Directory.Exists(Path.Combine(root_saving_folder,escaped_world_id)))
            {
                Directory.CreateDirectory(Path.Combine(root_saving_folder,escaped_world_id));
            }
            session_saving_folder = Path.Combine(escaped_world_id,currentWorld.SessionId+"_"+g.ToString());
            section = 0;
            _saving_folder = Path.Combine(root_saving_folder,session_saving_folder);
            Directory.CreateDirectory(saving_folder);
            have_started_recording_session = true;
            have_users_changed = false;
        }

        public void StartSection()
        {
            section += 1;
            _saving_folder = Path.Combine(root_saving_folder,session_saving_folder,section.ToString());
            Directory.CreateDirectory(saving_folder);
            have_users_changed = false;
            WriteUserMetadata();
        }
        public void StopSection()
        {
            last_saving_folder = _saving_folder;
        }

        public override void OnUserLeft(User user)
        {
            base.OnUserLeft(user);
            have_users_changed = true;
        }

        public override void OnUserJoined(User user)
        {
            base.OnUserJoined(user);
            have_users_changed = true;
        }

        public bool ShouldStartNewSection()
        {
            //we should restart recording if users have left or joined
            bool result = have_users_changed;
            //we reset the indicator of whether a user has left or joined
            return result;
        }
        private void WriteUserMetadata()
        {
            Dictionary<RefID, User>.ValueCollection users = this.World.AllUsers;
            List<UserMetadata> user_metadatas = new List<UserMetadata>();
            foreach(User user in users)
            {
                if (!metagen_comp.record_local_user && user == metagen_comp.World.LocalUser) continue;
                user_metadatas.Add(new UserMetadata
                {
                    userRefId = user.ReferenceID.ToString(),
                    userId = user.UserID,
                    headDevice = user.HeadDevice.ToString(),
                    platform = user.Platform.ToString(),
                    bodyNodes = String.Join(",",user.BodyNodes.Select(n => n.ToString())),
                    devices = String.Join(",", user.Devices.Where<SyncVar>((Func<SyncVar, bool>)(i => i.IsDictionary)).Select(d => d["Type"].GetValue<string>(true))),
                });
            }
            using (var writer = new StreamWriter(Path.Combine(saving_folder,"user_metadata.csv")))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(user_metadatas);
            }
        }
        public List<string> CurrentWorldRecordings
        {
            get
            {
                World world = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
                return GetRecordingsForWorld(world);
            }
        }
        public string GetRecordingForCurrentWorld(int index)
        {
            World world = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            return GetRecordingForWorld(world, index);
        }
        public int NumberRecordingsForCurrentWorld()
        {
            World world = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            return NumberRecordingsForWorld(world);
        }
        public int NumberRecordingsForWorld(World world)
        {
            return GetRecordingsForWorld(world).Count;
        }
        public string GetRecordingForWorld(World world, int index)
        {
            //index starts from the last (0) to the first
            List<string> recordings = GetRecordingsForWorld(world);
            if (recordings.Count > 0)
            {
                return recordings[recordings.Count - 1 - index];
            } else
            {
                return null;
            }
        }
        public string LastRecordingForWorld(World world)
        {
            List<string> recordings = GetRecordingsForWorld(world);
            if (recordings.Count > 0)
            {
                return recordings[recordings.Count - 1];
            } else
            {
                return null;
            }
        }
        public List<string> GetRecordingsForWorld(World world)
        {
            string world_id = world.CorrespondingWorldId ?? "new_world";
            string path = Path.Combine(root_saving_folder,scapeWorldID(world_id));
            if (!Directory.Exists(path)) return null;
            var di = new DirectoryInfo(path);
            List<string> session_folders = di.EnumerateDirectories()
                              .OrderBy(d => d.CreationTime)
                              .Select(d=>d.FullName)
                              .ToList();
            List<string> subfolders = new List<string>();
            foreach(string subfolder in session_folders)
            {
                di = new DirectoryInfo(subfolder);
                subfolders.AddRange(di.EnumerateDirectories()
                                  .OrderBy(d => d.CreationTime)
                                  .Select(d => d.FullName)
                                  .ToList());
            }
            return subfolders;
        }

    }
}
