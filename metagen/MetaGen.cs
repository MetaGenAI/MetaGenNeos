using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using BaseX;
using System.Threading;
using System.IO;
using UnityEngine;
using CodeX;
using metagen;
using System.Runtime.InteropServices;
using FrooxEngine.CommonAvatar;


namespace FrooxEngine.LogiX
{

    [Category("LogiX/AAAA")]
    [NodeName("MetaGen")]
    public class MetaGen : LogixNode
    {
        public bool recording = false;
        private DateTime utcNow;
        private DateTime recordingBeginTime;
        private DataManager dataManager;

        private bool recording_voice = false;
        private VoiceRecorder voiceRecorder;

        public bool recording_vision = false;
        public int2 camera_resolution = new int2(256, 256);
        private VisionRecorder visionRecorder;

        public bool recording_streams = false;
        public bool playing_streams = false;
        private PoseStreamRecorder streamRecorder;
        private PoseStreamPlayer streamPlayer;
        private metagen.AvatarManager avatarManager;
        private float recording_time
        {
            get
            {
                return (float)(DateTime.UtcNow - recordingBeginTime).TotalMilliseconds;
            }
        }

        protected override void OnAttach()
        {
            base.OnAttach();

            //TODO: refactor the audiolistener
            //TODO: make below work in VR mode too

            //This records the audio from an audiolistener. Unfortunately we can only have one audiolistener in an Unity scene:/
            //It starts/stops recording upon pressing the key R.
            //UniLog.Log("Adding Audio Listener");
            //GameObject gameObject = GameObject.Find("AudioListener");
            //UnityNeos.AudioRecorderNeos recorder = gameObject.AddComponent<UnityNeos.AudioRecorderNeos>();

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
            recording_streams = true;
            recording_voice = true;
            recording_vision = true;
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            dataManager = new DataManager();
            dataManager.StartRecordingSession();
            streamRecorder = new PoseStreamRecorder();
            voiceRecorder = new VoiceRecorder(this);
            visionRecorder = new VisionRecorder(camera_resolution, this);
            streamPlayer = new PoseStreamPlayer();
        }
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();

            //Start/Stop recording
            if (this.Input.GetKeyDown(Key.R))
            {
                ToggleRecording();
            }

            //Start a new chunk, if we have been recording for 30 minutes, or start a new section, if a new user has left or joined
            if (recording && (recording_time > 30 * 60 * 1000 || dataManager.ShouldStartNewSection()))
            {
                StopRecording();
                StartRecording();
            }

            //TODO: improve the playback
            //TODO: make playback not fail when file ends (Maybe can save length of stream files in its header, like for wav!)
            //TODO: make playback work on normal sessions (can't use the FingerPlayerSource. Can I create a new stream and feed that to a normal FingerPoseStreamManager?)
            //TODO: make voice playback
            //TODO: add Locomotion to playback
            //TODO: controller stream playback?
            //Start/Stop playing
            if (this.Input.GetKeyDown(Key.P))
            {
                UniLog.Log("Start/Stop playing");
                playing_streams = !playing_streams;
                if (!playing_streams)
                {
                    streamPlayer.StopPlaying();
                }
                else
                {
                    streamPlayer.StartPlaying();
                }
            }

            //TODO: make cameras for vision recording local to not affect the performance of others
            //TODO: record eye and mouth tracking data, haptics, and biometric data via some standard dynamic variables and things?
            //TODO: Save controller streams

            //RECORD ONE FRAME
            //Record one frame of video and streams (audio is handled on the audioRecorder itself via a function tied to the audio system of Neos)
            //We condition on deltaT to be as close to 30fps as possible
            float deltaT = (float)(DateTime.UtcNow - utcNow).TotalMilliseconds;
            if (deltaT > 33.3333)
            {
                if (recording && streamRecorder.isRecording)
                {
                    //UniLog.Log("recording streams");
                    streamRecorder.RecordStreams(deltaT);
                }

                if (recording && visionRecorder.isRecording)
                {
                    //UniLog.Log("recording vision");
                    visionRecorder.RecordVision();
                }
                utcNow = DateTime.UtcNow;
                if (playing_streams)
                {
                    //UniLog.Log("playing streams");
                    streamPlayer.PlayStreams();
                }
            }

        }
        protected override void OnAudioUpdate()
        {
            base.OnAudioUpdate();
            if (recording && voiceRecorder.isRecording)
            {
                //UniLog.Log("recording voice");
                voiceRecorder.RecordAudio();
            }
        }

        public void StartRecording()
        {
            UniLog.Log("Start recording");
            if (!recording)
                dataManager.StartSection();
            recording = true;
            //Set the recordings time to now
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            UniLog.Log(streamRecorder.isRecording.ToString());
            UniLog.Log(recording_streams.ToString());
            if (recording_streams && !streamRecorder.isRecording)
            {
                streamRecorder.saving_folder = dataManager.saving_folder;
                streamRecorder.StartRecording();
                //Record the first frame
                streamRecorder.RecordStreams(0f);
            }

            //AUDIO
            if (recording_voice && !voiceRecorder.isRecording)
            {
                voiceRecorder.saving_folder = dataManager.saving_folder;
                voiceRecorder.StartRecording();
            }

            //VIDEO
            if (recording_vision && !visionRecorder.isRecording)
            {
                visionRecorder.saving_folder = dataManager.saving_folder;
                visionRecorder.StartRecording();
                //Record the first frame
                visionRecorder.RecordVision();
            }
        }
        public void StopRecording()
        {
            UniLog.Log("Stop recording");
            recording = false;

            //STREAMS
            if (streamRecorder.isRecording)
                streamRecorder.StopRecording();

            //AUDIO
            if (voiceRecorder.isRecording)
                voiceRecorder.StopRecording();

            //VIDEO
            if (visionRecorder.isRecording)
                visionRecorder.StopRecording();
        }

        public void ToggleRecording()
        {
            //UniLog.Log("Start/Stop recording");
            if (recording) StopRecording();
            else StartRecording();
        }


    }
}
