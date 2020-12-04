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

namespace FrooxEngine.LogiX
{
    [Category("LogiX/AAAA")]
    [NodeName("MetaGen")]
    class MetaMetaGen : LogixNode
    {
        private string current_session_id;
        private Dictionary<string, MetaGen> metagens = new Dictionary<string, MetaGen>();
        UnityNeos.AudioRecorderNeos hearingRecorder;
        protected override void OnAttach()
        {
            base.OnAttach();

            ASDF.asdf(this.Engine);
            Job<Slot> awaiter = SlotHelper.TransferToWorld(this.Slot,Userspace.UserspaceWorld).GetAwaiter();
            awaiter.GetResult();
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
        private void StartMetaGen(World world)
        {
            world.RunSynchronously(() =>
            {
                Dictionary<RefID, User>.ValueCollection users = world.AllUsers;
                User recording_hearing_user = null;
                foreach(User user in users)
                {
                    if (user.IsHost)
                    {
                        recording_hearing_user = user;
                        break;
                    }
                }
                hearingRecorder.userID = recording_hearing_user.ReferenceID.ToString();
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
                    metagen.StartRecording();
                }
                metagens[current_session_id] = metagen;
            });
        }
        private void RemoveWorld(World world)
        {
            //current_session_id = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.SessionId;
            metagens.Remove(world.SessionId);
        }
    }
}
