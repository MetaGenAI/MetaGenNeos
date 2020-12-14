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
    class BotLogic : Component
    {
        private volatile OutputState _playingState = OutputState.Stopped;
        private volatile OutputState _recordingState = OutputState.Stopped;
        private StreamStatus _obsStatus;
        protected readonly SyncTime _recordingStarted;
        private MetaGenBotPanelUI panelUI;
        public bool IsPlaying
        {
            get
            {
                return this._playingState != OutputState.Stopped;
            }
        }

        public bool IsRecording
        {
            get
            {
                return this._recordingState != OutputState.Stopped;
            }
        }
        protected override void OnAttach()
        {
            base.OnAttach();
            panelUI = this.Slot.AttachComponent<MetaGenBotPanelUI>();
        }
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();
            StreamStatus obsStatus1 = this._obsStatus;
            int num1 = obsStatus1 != null ? obsStatus1.TotalStreamTime : 0;
            //string localized1 = this.GetLocalized("CameraCOntrol.OBS.Live", (string)null, (Dictionary<string, object>)null);
            string localized1 = "idle";
            if (_recordingState == OutputState.Started)
            {
                num1 = (int)this._recordingStarted.CurrentTime;
                localized1 = this.GetLocalized("CameraCOntrol.OBS.Recording", (string)null, (Dictionary<string, object>)null);
            }
            int num2 = num1 / 3600;
            int num3 = num1 / 60 % 60;
            int num4 = num1 % 60;
            this.panelUI._recordingTime.Target.Content.Value = string.Format("{0}: {1:00}:{2:00}:{3:00}", (object)localized1, (object)num2, (object)num3, (object)num4);
            this.UpdateButton((Button)this.panelUI._playButton, this._playingState, "Streaming");
            this.UpdateButton((Button)this.panelUI._recordButton, this._recordingState, "Recording");
        }

        private void UpdateButton(Button button, OutputState state, string label)
        {
            color color1 = new color();
            string key = (string)null;
            switch (state)
            {
                case OutputState.Starting:
                    color1 = color.Yellow;
                    key = "CameraControl.OBS." + label + ".Starting";
                    break;
                case OutputState.Started:
                    color1 = color.Green;
                    key = "CameraControl.OBS." + label + ".Stop";
                    break;
                case OutputState.Stopping:
                    color1 = color.Orange;
                    key = "CameraControl.OBS." + label + ".Stopping";
                    break;
                case OutputState.Stopped:
                    color1 = color.Red;
                    key = "CameraControl.OBS." + label + ".Start";
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
    }
}
