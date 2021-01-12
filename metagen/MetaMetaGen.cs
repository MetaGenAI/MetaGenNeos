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
        protected override void OnAttach()
        {
            base.OnAttach();

            ASDF.asdf(this.Engine);
            Job<Slot> awaiter = SlotHelper.TransferToWorld(this.Slot, Userspace.UserspaceWorld).GetAwaiter();
            awaiter.GetResult();
            this.StartTask(()=>Task.Run(metagen.Util.MediaConverter.Run));
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
            hearingRecorder = gameObject.AddComponent<UnityNeos.AudioRecorderNeos>();

            //dataManager = new DataManager();
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            current_session_id = currentWorld.SessionId;
            //AddWorld(currentWorld);
            FrooxEngine.Engine.Current.WorldManager.WorldRemoved += RemoveWorld;
            //FrooxEngine.Engine.Current.WorldManager.WorldAdded += AddWorld;
            FrooxEngine.Engine.Current.WorldManager.WorldFocused += FocusedWorld;
            FrooxEngine.Engine.Current.Cloud.Messages.OnMessageReceived += ProcessMessage;
            FrooxEngine.Engine.Current.Cloud.Friends.FriendRequestCountChanged += ProcessFriendRequest;
        }
        
        private void ProcessFriendRequest(int count)
        {
            Friend friend = FrooxEngine.Engine.Current.Cloud.Friends.FindFriend(f => f.FriendStatus == FriendStatus.Requested);
            Engine.Cloud.Friends.AddFriend(friend);
        }

        private void ProcessMessage(CloudX.Shared.Message msg)
        {
            this.RunSynchronously(() =>
            {
                string content = msg.Content;
                
                switch (msg.MessageType)
                {
                    case CloudX.Shared.MessageType.Text:
                        processCommand(content);
                        break;
                    case CloudX.Shared.MessageType.SessionInvite:
                        processInvite(msg);
                        break;
                    default:
                        break;
                }
            });
        }
        private void processInvite(Message msg)
        {
            SessionInfo sessionInfo = msg.ExtractContent<SessionInfo>();
            WorldManager worldManager = FrooxEngine.Engine.Current.WorldManager;
            List<Uri> sessions = sessionInfo.GetSessionURLs();
            if (current_metagen == null ? true : !current_metagen.recording)
            {
                World world = worldManager.JoinSession(sessions);
                StartTask(async () => await Userspace.FocusWhenReady(world));
            }
        }
        private void processCommand(string msg)
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            currentWorld.RunSynchronously(() =>
            {
                List<string> msgarr = msg.Split(' ').ToList();
                string command = msgarr[0];
                string argument = msgarr.Count > 1 ? msgarr[1] : "";
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
                        User user = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.FindUser(u => u.Name == argument);
                        SetHearingUser(user.UserID);
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
                }

            });

        }

        protected override void OnCommonUpdate()
        {
            if (current_metagen == null ? false : current_metagen.recording)
                FrooxEngine.Engine.Current.Cloud.Status.OnlineStatus = OnlineStatus.Busy;
            else
                FrooxEngine.Engine.Current.Cloud.Status.OnlineStatus = OnlineStatus.Online;
        }
        private void FocusedWorld(World world)
        {
            foreach(var item in metagens)
            {
                MetaGen metagen = item.Value;
                metagen.StopRecording();
                metagen.StopPlaying();
            }
            current_session_id = world.SessionId;
            AddWorld(world);
        }
        private void ResetCurrentMetaGen()
        {
            World world = current_metagen.World;
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
        private void SetHearingUser(string userid)
        {
                hearingRecorder.userID = userid;
        }
        private void StartMetaGen(World world)
        {
            world.RunSynchronously(() =>
            {
                Dictionary<RefID, User>.ValueCollection users = world.AllUsers;
                User recording_hearing_user = world.LocalUser;
                //foreach (User user in users)
                //{
                //    if (user.IsHost)
                //    {
                //        recording_hearing_user = user;
                //        break;
                //    }
                //}
                SetHearingUser(recording_hearing_user.ReferenceID.ToString());
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
            });
        }
        private void RemoveWorld(World world)
        {
            //current_session_id = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.SessionId;
            if (metagens.ContainsKey(world.SessionId))
                metagens.Remove(world.SessionId);
        }
    }
}
