using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.UIX;
using BaseX;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace metagen
{
    public class BotLogic : Component
    {
        protected readonly SyncTime _recordingStarted;
        private MetaGenBotPanelUI panelUI;
        public metagen.MetaGen mg;
        bool just_created_panel;
        public bool IsPlaying
        {
            get
            {
                return mg.playing_state != OutputState.Stopped;
            }
        }

        public bool IsRecording
        {
            get
            {
                return mg.recording_state != OutputState.Stopped;
            }
        }
        protected override void OnAttach()
        {
            base.OnAttach();
            panelUI = this.Slot.AttachComponent<MetaGenBotPanelUI>();

            //UI Control Logic (the events from the UI control the MetaGen functionality which implements the recording/playback logic
            panelUI.ToggleRecording += () =>
            {
                if (mg.recording)
                {
                    mg.StopRecording();
                } else
                {
                    mg.recording_animation = panelUI._animationsCheckbox.Target.State.Value;
                    mg.recording_vision = panelUI._videoCheckbox.Target.State.Value;
                    mg.StartRecording();
                }
            };
            panelUI.TogglePlaying += () =>
            {
                if (mg.playing)
                {
                    mg.StopPlaying();
                } else
                {
                    int recording_index = Int32.Parse(panelUI._recordIndexField.Target.Text.Content.Value);
                    mg.play_hearing = panelUI._hearingCheckbox.Target.State.Value;
                    mg.play_voice = panelUI._voicesCheckbox.Target.State.Value;
                    Slot avatar = panelUI._avatarRefField.Target.Reference.Target;
                    mg.StartPlaying(recording_index,avatar);
                }
            };

            panelUI.SwapUI += () =>
            {
                panelUI.LinkUISlot();
            };


            just_created_panel = true;
        }
        public void AddOverride(User user)
        {
            //This is called by MetaGen when there is a change to the number of users
            panelUI?.publicDomainOverride.SetOverride(user, mg.users[user].default_public);
            panelUI?.recordUserOverride.SetOverride(user, mg.users[user].is_friend);
        }
        public void RemoveOverride(User user)
        {
            //This is called by MetaGen when there is a change to the number of users
            panelUI?.publicDomainOverride.RemoveOverride(user);
            panelUI?.recordUserOverride.RemoveOverride(user);
        }
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();

            //Thing to run when the panel has finished attaching (I think theres a callback thing I could use instead but well)
            if (mg.is_loaded && just_created_panel && panelUI?.publicDomainOverride != null && panelUI?.recordUserOverride != null)
            {
                UniLog.Log("AAAAAAAAAAAAAAAAAA");
                UniLog.Log(mg.userMetaData.Count);
                //mg.metaDataManager.GetUserMetaData();
                foreach (var item in mg.userMetaData)
                {
                    User user = item.Key;
                    UserMetadata data = item.Value;
                    panelUI?.publicDomainOverride.SetOverride(user, data.isPublic);
                }
                foreach (var item in mg.userMetaData)
                {
                    User user = item.Key;
                    UserMetadata data = item.Value;
                    panelUI?.recordUserOverride.SetOverride(user, data.isRecording);
                }
                panelUI.recordUserOverride.Changed += (IChangeable a) =>
                {
                    SyncBag<ValueUserOverride<bool>.Override> overrides = (SyncBag<ValueUserOverride<bool>.Override>) panelUI.recordUserOverride.GetSyncMember(7);
                    foreach (var item in overrides)
                    {
                        ValueUserOverride<bool>.Override ov = item.Value;
                        //Perhaps make an API function for this
                        mg.metaDataManager.userMetaData[ov.User.User.Target].isRecording = ov.Value.Value;
                    }

                };

                panelUI.publicDomainOverride.Changed += (IChangeable a) =>
                {
                    SyncBag<ValueUserOverride<bool>.Override> overrides = (SyncBag<ValueUserOverride<bool>.Override>) panelUI.publicDomainOverride.GetSyncMember(7);
                    foreach (var item in overrides)
                    {
                        ValueUserOverride<bool>.Override ov = item.Value;
                        //Perhaps make an API function for this
                        mg.metaDataManager.userMetaData[ov.User.User.Target].isPublic = ov.Value.Value;
                    }
                };
                just_created_panel = false;
            }

            //Update panel UI
            //string localized1 = this.GetLocalized("CameraCOntrol.OBS.Live", (string)null, (Dictionary<string, object>)null);
            string localized1 = "idle";
            if (mg.recording_state == OutputState.Started)
            {
                localized1 = this.GetLocalized("CameraCOntrol.OBS.Recording", (string)null, (Dictionary<string, object>)null);
            } else if (mg.playing_state == OutputState.Started)
            {
                localized1 = "PLAY";
            }
            int num1 = (mg.recording || mg.playing) ? (int)mg.recording_time / 1000 : 0;
            int num2 = num1 / 3600;
            int num3 = num1 / 60 % 60;
            int num4 = num1 % 60;
            if (panelUI != null)
            {
                this.panelUI._recordingTime.Target.Content.Value = string.Format("{0}: {1:00}:{2:00}:{3:00}", (object)localized1, (object)num2, (object)num3, (object)num4);
                this.UpdateButton((Button)this.panelUI._playButton, mg.playing_state, "Playing");
                this.UpdateButton((Button)this.panelUI._recordButton, mg.recording_state, "Recording");
            }
        }

        private void UpdateButton(Button button, OutputState state, string label)
        {
            color color1 = new color();
            string key = (string)null;
            //TODO: add localization for Playing strings/MetaGen in general
            switch (state)
            {
                case OutputState.Starting:
                    color1 = color.Yellow;
                    key = label=="Recording"?"CameraControl.OBS." + label + ".Starting" : "Play Started";
                    break;
                case OutputState.Started:
                    color1 = color.Green;
                    key = label=="Recording"?"CameraControl.OBS." + label + ".Stop" : "Stop Play";
                    break;
                case OutputState.Stopping:
                    color1 = color.Orange;
                    key = label=="Recording"?"CameraControl.OBS." + label + ".Stopping" : "Stopping Play";
                    break;
                case OutputState.Stopped:
                    color1 = color.Red;
                    key = label=="Recording"?"CameraControl.OBS." + label + ".Start" : "Start Play";
                    break;
            }
            Button button1 = button;
            ref color local1 = ref color1;
            color white = color.White;
            ref color local2 = ref white;
            color color2 = MathX.Lerp(in local1, in local2, 0.5f);
            ref color local3 = ref color2;
            button1.SetColors(in local3);
            button.LabelText = this.GetLocalized(key, (string)null, (Dictionary<string, object>)null);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            panelUI.Destroy();
        }

    }
}
    public enum OutputState
    {
        Starting,
        Started,
        Stopping,
        Stopped,
    }
    public class StreamStatus
    {
        [JsonProperty(PropertyName = "streaming")]
        public bool Streaming { internal set; get; }

        [JsonProperty(PropertyName = "recording")]
        public bool Recording { internal set; get; }

        [JsonProperty(PropertyName = "bytes-per-sec")]
        public int BytesPerSec { internal set; get; }

        [JsonProperty(PropertyName = "kbits-per-sec")]
        public int KbitsPerSec { internal set; get; }

        [JsonProperty(PropertyName = "strain")]
        public float Strain { internal set; get; }

        [JsonProperty(PropertyName = "total-stream-time")]
        public int TotalStreamTime { internal set; get; }

        [JsonProperty(PropertyName = "num-total-frames")]
        public int TotalFrames { internal set; get; }

        [JsonProperty(PropertyName = "num-dropped-frames")]
        public int DroppedFrames { internal set; get; }

        [JsonProperty(PropertyName = "fps")]
        public float FPS { internal set; get; }

        [JsonProperty(PropertyName = "cpu-usage")]
        public double CPU { internal set; get; }

        [JsonProperty(PropertyName = "output-skipped-frames")]
        public int SkippedFrames { internal set; get; }

        [JsonProperty(PropertyName = "render-missed-frames")]
        public int RenderMissedFrames { internal set; get; }

        [JsonProperty(PropertyName = "stream-timecode")]
        public string StreamTime { internal set; get; }

        [JsonProperty(PropertyName = "replay-buffer-active")]
        public bool ReplayBufferActive { internal set; get; }

        public StreamStatus(JObject data)
        {
            JsonConvert.PopulateObject(data.ToString(), (object)this);
        }
    }
