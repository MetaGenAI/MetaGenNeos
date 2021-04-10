/*
 * This is the main entry point, that orchestrates the whole system
 * It keeps track of creating the MetaGen components, when entering a new world, and creating the Bot components (BotLogic and BotUI)
 * It also keeps tracks of commands from users (via messages, invitations, etc)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using metagen;
using System.Threading;
using BaseX;
using UnityEngine;
using CloudX;
using CloudX.Shared;

namespace FrooxEngine.LogiX
{
    [Category("LogiX/AAAA")]
    [NodeName("MetaGen")]
    //TODO: PlayerManager and RecorderManager to abstract multi-recorders and multi-players in MetaGen
    //TODO: create interface for players and recorders
    class MetaMetaGen : LogixNode
    {
        private string current_session_id;
        private Dictionary<string, MetaGen> metagens = new Dictionary<string, MetaGen>();
        MetaGen current_metagen = null;
        UnityNeos.AudioRecorderNeos hearingRecorder;
        DataBase dataBase;
        User recording_hearing_user;
        World currentWorld;
        bool default_record_local_user = false;
        bool is_in_VR_mode = false;
        bool auto_set_status = false;
        bool record_everyone = false;
        Slot earsSlot;
        protected override void OnAttach()
        {
            base.OnAttach();

            ASDF.asdf(this.Engine);
            Job<Slot> awaiter = SlotHelper.TransferToWorld(this.Slot, Userspace.UserspaceWorld).GetAwaiter();
            awaiter.GetResult();
            //Start the co-routine which checks for newly generated media files, and converts them
            this.StartTask(() => Task.Run(metagen.Util.MediaConverter.Run));
        }

        protected override void OnPaste()
        {
            base.OnPaste();
            UniLog.Log("Transferred to userspace");
            //Remember that onPasting this component is reinitialized
            //so that changes made in the previous OnAttach won't be saved!

            //This records the audio from an audiolistener. Unfortunately we can only have one audiolistener in an Unity scene:/
            //It starts/stops recording upon pressing the key R.
            //TODO: make below work in VR mode too
            //TODO: sync between audios and videos is not right!!
            UniLog.Log("Adding Audio Listener");
            GameObject gameObject = GameObject.Find("AudioListener");
            //default_record_local_user = true;
            if (!(LocalUser.HeadDevice == HeadOutputDevice.Screen)) //must be on VR mode
            {
                UniLog.Log("VR mode!");
                //gameObject = GameObject.Find("Camera (ears)");
                default_record_local_user = true;
                is_in_VR_mode = true;
            }

            //GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            //GameObject gameObject = rootObjects[0];
            //for (int i = 0; i < rootObjects.Length; i++)
            //{
            //    AudioListener audioListener = rootObjects[i].GetComponentInChildren<UnityEngine.AudioListener>();
            //    if (audioListener != null) {
            //        gameObject = audioListener.gameObject;
            //        break;
            //    }
            //}
            hearingRecorder = gameObject.AddComponent<UnityNeos.AudioRecorderNeos>();

            //dataManager = new DataManager();
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            current_session_id = currentWorld.SessionId;
            //AddWorld(currentWorld);
            FrooxEngine.Engine.Current.WorldManager.WorldRemoved += RemoveWorld;
            //FrooxEngine.Engine.Current.WorldManager.WorldAdded += AddWorld;
            FrooxEngine.Engine.Current.WorldManager.WorldFocused += FocusedWorld;
            if (!is_in_VR_mode)
                FrooxEngine.Engine.Current.Cloud.Messages.OnMessageReceived += ProcessMessage;
            //FrooxEngine.Engine.Current.Cloud.Friends.FriendRequestCountChanged += ProcessFriendRequest;
            FrooxEngine.Engine.Current.Cloud.Friends.FriendAdded += ProcessFriendRequest;
            FrooxEngine.Engine.Current.Cloud.Friends.FriendRequestCountChanged += ProcessFriendRequest2;
            FrooxEngine.Engine.Current.Cloud.Friends.FriendRemoved += ProcessFriendRemoved;
            dataBase = new DataBase();
        }

        private void ProcessFriendRemoved(Friend friend)
        {
            UniLog.Log("removing friend " + friend.FriendUserId);
            dataBase.UpdateIsFriend(friend.FriendUserId, false);
        }
        private void ProcessFriendRequest(Friend friend)
        {
            Engine.Cloud.Friends.AddFriend(friend);
            dataBase.MakeNewFriend(friend.FriendUserId);
            Task.Run(() =>
            {
                int max_iter = 10000;
                int iter = 0;
                while (!Engine.Cloud.Friends.IsFriend(friend.FriendUserId) && iter < max_iter) { }
                {
                    //SendMessage(friend.FriendUserId, "Hi. Welcome!");
                    //SendMessage(friend.FriendUserId, "Friends of the bot will by default record data with a CC0 \"Public Domain\" license.");
                    //SendMessage(friend.FriendUserId, "To change this default to be that your data is kept private,");
                    //SendMessage(friend.FriendUserId, "please send a message to the bot with the text \"default no public\".");
                    //SendMessage(friend.FriendUserId, "This default can always be overriden for a particular recording from the bot UI.");
                    //SendMessage(friend.FriendUserId, "You can also change the default back to public by sending a message with \"default public\"");
                }
            });
            Task.Run(() =>
            {
                while (!Engine.Cloud.Friends.IsFriend(friend.FriendUserId)) { }
            });
        }

        private void ProcessFriendRequest2(int count)
        {
            Task.Run(() =>
            {
                int max_iter = 10000;
                int iter = 0;
                Friend friend = FrooxEngine.Engine.Current.Cloud.Friends.FindFriend(f => f.FriendStatus == FriendStatus.Requested);
                while (friend == null && iter < max_iter)
                {
                    friend = FrooxEngine.Engine.Current.Cloud.Friends.FindFriend(f => f.FriendStatus == FriendStatus.Requested);
                    iter += 1;
                    Thread.Sleep(10);
                }
                Engine.Cloud.Friends.AddFriend(friend);
            });
        }

        private void ProcessMessage(CloudX.Shared.Message msg)
        {
            this.RunSynchronously(() =>
            {
                switch (msg.MessageType)
                {
                    case CloudX.Shared.MessageType.Text:
                        processCommand(msg);
                        break;
                    case CloudX.Shared.MessageType.SessionInvite:
                            processInvite(msg);
                        break;
                    default:
                        break;
                }
                MessageManager.UserMessages userMessages = this.Engine.Cloud.Messages.GetUserMessages(msg.SenderId);
                userMessages.MarkAllRead();
            });
        }
        private void processInvite(Message msg)
        {
            //if (msg.SenderId == "U-guillefix") return;
            //string userName = this.Engine.Cloud.Friends.FindFriend(f => f.FriendUserId == msg.SenderId).FriendUsername;
            //if (userName == "badhaloninja" || userName == "marsmaantje" || userName == "oXoMaStErSoXo") return;
            SessionInfo sessionInfo = msg.ExtractContent<SessionInfo>();
            WorldManager worldManager = FrooxEngine.Engine.Current.WorldManager;
            List<Uri> sessions = sessionInfo.GetSessionURLs();
            if (current_metagen == null ? true : !current_metagen.recording)
            {
                World world = worldManager.JoinSession(sessions);
                StartTask(async () => await Userspace.FocusWhenReady(world));
            }
            else
            {
                MessageManager.UserMessages userMessages = this.Engine.Cloud.Messages.GetUserMessages(msg.SenderId);
                //CloudX.Shared.Message textMessage = userMessages.CreateTextMessage("Busy recording somewhere else. Try again in a bit!");
                userMessages.SendTextMessage("Sorry. Busy recording somewhere else. Try again in a bit!");
            }

        }

        private void SendMessage(string userID, string message)
        {
            MessageManager.UserMessages userMessages = this.Engine.Cloud.Messages.GetUserMessages(userID);
            userMessages.SendTextMessage(message);
        }

        private void processCommand(Message msg)
        {
            string msg_text = msg.Content;
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            currentWorld.RunSynchronously(() =>
            {
                List<string> msgarr = msg_text.Split(' ').ToList();
                string command = msgarr[0];
                string argument = msgarr.Count > 1 ? string.Join(" ", msgarr.Skip(1)) : "";
            switch (command)
            {
                case "hearing":
                    current_metagen.recording_hearing = argument == "y";
                    break;
                case "vision":
                    current_metagen.recording_vision = argument == "y";
                    break;
                case "voice":
                    current_metagen.recording_voice = argument == "y";
                    break;
                case "movement":
                    current_metagen.recording_streams = argument == "y";
                    break;
                case "hu": //hearing user
                    User user = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.FindUser(u => u.UserName == argument);
                    if (user != null) SetHearingUser(user);
                    break;
                case "rec": //start recording
                    current_metagen.StartRecording();
                    break;
                case "stoprec": //stop recording
                    current_metagen.StopRecording();
                    break;
                case "play": //start recording
                    current_metagen.StartPlaying();
                    break;
                case "stop": //stop playing
                    current_metagen.StopPlaying();
                    break;
                case "reset": //reset current metagen (e.g. if it crashed)
                    ResetCurrentMetaGen();
                    break;
                case "default":
                    if (argument == "no public")
                        {
                            dataBase.UpdateDefaultPublic(msg.SenderId, false);
                            SendMessage(msg.SenderId,"Default updated to private");
                        }
                    else
                        {
                            dataBase.UpdateDefaultPublic(msg.SenderId, true);
                            SendMessage(msg.SenderId,"Default updated to public");
                        }
                    break;
                case "info":
                    MetaGenUser user_data = dataBase.GetUserData(msg.SenderId);
                    bool default_public = user_data.default_public;
                    float total_recorded_time = (int)user_data.total_recorded;
                    float total_recorded_time_public = (int)user_data.total_recorded_public;
                    string message = System.String.Format("Default license: " + (default_public ? "public" : "private") + "\n" + "Total recorded time: {0:0}h{1:0}m{2:0}s" + "\n" + "Total public recorded time:  {3:0}h{4:0}m{5:0}s",
                        System.Math.Floor(System.Math.Floor(total_recorded_time / 60f) / 60f), System.Math.Floor(total_recorded_time / 60f) % 60, total_recorded_time % 60, 
                        System.Math.Floor(System.Math.Floor(total_recorded_time_public / 60f) / 60f), System.Math.Floor(total_recorded_time_public / 60f) % 60, total_recorded_time_public % 60);
                    SendMessage(msg.SenderId,message);
                    break;
                }

            });

        }

        protected override void OnCommonUpdate()
        {
            if (!is_in_VR_mode)
            {
                if (auto_set_status)
                {
                    if (current_metagen == null ? false : current_metagen.recording)
                    {
                        if (FrooxEngine.Engine.Current.Cloud.Status.OnlineStatus != OnlineStatus.Busy)
                            FrooxEngine.Engine.Current.Cloud.Status.OnlineStatus = OnlineStatus.Busy;
                    }
                    else
                    {
                        if (FrooxEngine.Engine.Current.Cloud.Status.OnlineStatus != OnlineStatus.Online)
                            FrooxEngine.Engine.Current.Cloud.Status.OnlineStatus = OnlineStatus.Online;
                    }
                }

                if (this.Engine.WorldManager.FocusedWorld?.LocalUser?.ActiveVoiceMode != VoiceMode.Mute)
                {
                    this.InputInterface.IsMuted = true;
                    this.Engine.WorldManager.FocusedWorld.LocalUser.VoiceMode = VoiceMode.Mute;
                }
            }

            if (dataBase != null && dataBase.should_update)
            {
                Task.Run(() => dataBase.SaveDatabase());
            }
        }
        private void FocusedWorld(World world)
        {
            if (currentWorld != null && !is_in_VR_mode)
            {
                try
                {
                    if (currentWorld.IsAuthority) Userspace.LeaveSession(currentWorld);
                    else Userspace.EndSession(currentWorld);
                } catch (Exception e)
                {
                    UniLog.Log("Exception when leaving/ending session: " + e.Message);
                }

            }
            bool is_there_metagen_in_world = false;
            foreach(var item in metagens)
            {
                try
                {
                    MetaGen metagen = item.Value;
                    metagen.StopPlaying();
                    metagen.StopRecording();
                    //metagen.Destroy();
                    if (metagen.World == world) is_there_metagen_in_world = true;
                } catch (Exception e)
                {
                    UniLog.Log("owo Exception when stopping the metagens when focusing a new world");
                    UniLog.Log("This could be because someone invited the bot while it was playing");
                }
            }
            current_session_id = world.SessionId;
            currentWorld = world;
            if (!is_there_metagen_in_world) AddWorld(world);
        }
        private void ResetCurrentMetaGen()
        {
            World world = current_metagen.World;
            current_metagen.botComponent.Slot.Destroy();
            current_metagen.Destroy();
            current_session_id = world.SessionId;
            AddWorld(world);
        }
        private void AddWorld(World world)
        {
            //current_session_id = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.SessionId;
            if (world.State == World.WorldState.Running)
            {
                StartMetaGen(world);
            } else
            {
                world.WorldRunning += StartMetaGen;
            }
        }
        private void SetHearingUser(User user)
        {
            recording_hearing_user = user;
            hearingRecorder.userID = user.ReferenceID.ToString();
            if (current_metagen != null) current_metagen.recording_hearing_user = user;
        }
        public void MaybeResetHearingUserOnLeft(User left_user)
        {
            if (left_user.UserID == recording_hearing_user.UserID)
            {
                ResetHearingUser();
            }
        }
        public void MaybeResetHearingUserOnJoin(User joined_user)
        {
            World world = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            UniLog.Log("hi "+recording_hearing_user.UserID);
            if (recording_hearing_user.UserID == world.LocalUser.UserID)
            {
            UniLog.Log("h0");
                ResetHearingUser();
            }

        }
        private void ResetHearingUser()
        {
            World world = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            UniLog.Log("Reset hearing user for world " + world.CorrespondingWorldId);
            Dictionary<RefID, User>.ValueCollection users = world.AllUsers;
            recording_hearing_user = world.LocalUser;
            if (!is_in_VR_mode || !default_record_local_user)
            {
                foreach (User user in users)
                {
                    if (user.UserID != null)
                    {
                        MetaGenUser user_data = dataBase.GetUserData(user.UserID);
                        if (user != world.LocalUser && user_data.default_public && user_data.default_recording)
                        {
                            recording_hearing_user = user;
                            break;
                        }
                    }
                }
            }
            SetHearingUser(recording_hearing_user);
        }
        private void StartMetaGen(World world)
        {
            world.RunSynchronously(() =>
            {
                Slot metagen_slot = world.RootSlot.Find("5013598197metagen local slot");
                if (metagen_slot == null)
                {
                    metagen_slot = world.AddLocalSlot("5013598197metagen local slot");
                }
                MetaGen metagen = metagen_slot.GetComponent<MetaGen>();
                if (metagen == null)
                {
                    Action<MetaGen> beforeAttach = (MetaGen comp) =>
                       {
                           comp.hearingRecorder = hearingRecorder;
                           comp.recording_hearing_user = recording_hearing_user;
                       };
                    metagen = metagen_slot.AttachComponent<MetaGen>(true, beforeAttach);
                } else
                {
                    metagen.hearingRecorder = hearingRecorder;
                    metagen.recording_hearing_user = recording_hearing_user;
                    //metagen.StartRecording();
                }
                metagens[current_session_id] = metagen;
                current_metagen = metagen;
                hearingRecorder.metagen_comp = metagen;


                //attach BotLogic
                Slot botLogicSlot = world.LocalUser.Root.Slot.AddLocalSlot("botlogic local slot");
                BotLogic logicComp = botLogicSlot.AttachComponent<BotLogic>();
                logicComp.mg = current_metagen;
                current_metagen.botComponent = logicComp;
                current_metagen.dataBase = dataBase;
                ResetHearingUser();
                current_metagen.record_local_user = default_record_local_user;
                current_metagen.Initialize();
                current_metagen.OnUserLeftCallback += MaybeResetHearingUserOnLeft;
                current_metagen.OnUserJoinedCallback += MaybeResetHearingUserOnJoin;
                earsSlot = world.AddSlot("AudioListener");
                world.LocalUser.LocalUserRoot.OverrideEars.Target = earsSlot;
                hearingRecorder.earSlot = earsSlot;
                current_metagen.is_loaded = true;
            });
        }
        private void RemoveWorld(World world)
        {
            UniLog.Log("remove world " + world.RawName);
            //current_session_id = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.SessionId;
            if (world.SessionId != null && metagens.ContainsKey(world.SessionId))
                metagens.Remove(world.SessionId);
        }
    }
}
