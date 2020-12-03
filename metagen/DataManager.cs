using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using System.IO;

namespace metagen
{
    class DataManager : Component
    {
        private bool have_users_changed = false;
        private string _saving_folder = @"./data";
        private string root_saving_folder = @"./data";
        private string session_saving_folder = "";
        private int section = 0;
        public string saving_folder
        {
            get
            {
                return this._saving_folder;
            }
        }

        public DataManager()
        {
            if (!Directory.Exists(root_saving_folder))
            {
                Directory.CreateDirectory(root_saving_folder);
            }

        }
        public void StartRecordingSession()
        {
            Guid g = Guid.NewGuid();
            session_saving_folder = g.ToString();
            section = 0;
            _saving_folder = root_saving_folder + "/" + session_saving_folder;
            Directory.CreateDirectory(saving_folder);
            have_users_changed = false;
        }

        public void StartSection()
        {
            section += 1;
            _saving_folder = root_saving_folder + "/" + session_saving_folder + "/" + section.ToString();
            Directory.CreateDirectory(saving_folder);
            have_users_changed = false;
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

    }
}
