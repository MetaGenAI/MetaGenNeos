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


namespace metagen
{
    public class MetaGen : FrooxEngine.Component
    {
        public bool playing = false;
        public OutputState playing_state = OutputState.Stopped;
        public bool recording = false;
        public OutputState recording_state = OutputState.Stopped;
        private DateTime utcNow;
        private DateTime recordingBeginTime;
        private DateTime playingBeginTime;
        private DataManager dataManager;

        public bool recording_hearing = false;
        public User recording_hearing_user;

        public bool recording_voice = false;
        private VoiceRecorder voiceRecorder;

        public bool recording_vision = false;
        public int2 camera_resolution = new int2(256, 256);
        private VisionRecorder visionRecorder;

        public bool recording_streams = false;
        private PoseStreamRecorder streamRecorder;
        private PoseStreamPlayer streamPlayer;
        private metagen.AvatarManager avatarManager;
        public UnityNeos.AudioRecorderNeos hearingRecorder;
        int frame_index = 0;
        float MAX_CHUNK_LEN_MIN = 0.3f;

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
            recording_streams = true;
            recording_voice = true;
            recording_hearing = true;
            recording_vision = true;
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            dataManager = this.Slot.AttachComponent<DataManager>();
            streamRecorder = new PoseStreamRecorder(this);
            voiceRecorder = new VoiceRecorder(this);
            visionRecorder = new VisionRecorder(camera_resolution, this);
            streamPlayer = new PoseStreamPlayer(dataManager, this);
            //StartRecording();
        }
        protected override void OnDispose()
        {
            base.OnDispose();
            StopRecording();
        }
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();
            World currentWorld = this.World;
            //UniLog.Log("HI from " + currentWorld.CorrespondingWorldId);

            //Start/Stop recording
            if (this.Input.GetKeyDown(Key.R))
            {
                ToggleRecording();
            }

            //Start a new chunk, if we have been recording for 30 minutes, or start a new section, if a new user has left or joined
            if (recording && (recording_time > MAX_CHUNK_LEN_MIN * 60 * 1000 || dataManager.ShouldStartNewSection()))
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
                TogglePlaying();
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
                bool streams_ok = (streamRecorder == null ? false : streamRecorder.isRecording) || !recording_streams;
                bool vision_ok = (visionRecorder == null ? false : visionRecorder.isRecording) || !recording_vision;
                bool hearing_ok = (hearingRecorder == null ? false : hearingRecorder.isRecording) || !recording_hearing;
                bool voice_ok = (voiceRecorder == null ? false : (voiceRecorder.isRecording && voiceRecorder.audio_sources_ready)) || !recording_voice;
                //bool all_ready = voice_ok && streams_ok && vision_ok && hearing_ok;
                bool all_ready = hearing_ok;
                if (recording && all_ready && streamRecorder==null? false : streamRecorder.isRecording)
                {
                    //UniLog.Log("recording streams");
                    streamRecorder.RecordStreams(deltaT);
                }

                if (recording && all_ready && visionRecorder==null? false : visionRecorder.isRecording)
                {
                    //UniLog.Log("recording vision");
                    if (frame_index == 30)
                        hearingRecorder.videoStartedRecording = true;
                    visionRecorder.RecordVision();
                }

                if (recording && all_ready && recording_hearing_user != null && hearingRecorder==null? false : hearingRecorder.isRecording)
                {
                    hearingRecorder.UpdatePosition(recording_hearing_user.Root.Slot.GlobalPosition);
                }
                frame_index += 1;

                utcNow = DateTime.UtcNow;
                if (playing)
                {
                    //UniLog.Log("playing streams");
                    streamPlayer.PlayStreams();
                }
            }

        }
        protected override void OnAudioUpdate()
        {
            base.OnAudioUpdate();
            if (recording && voiceRecorder==null? false : voiceRecorder.isRecording)
            {
                //UniLog.Log("recording voice");
                voiceRecorder.RecordAudio();
            }
        }

        public void StartRecording()
        {
            UniLog.Log("Start recording");
            if (!dataManager.have_started_recording_session)
                dataManager.StartRecordingSession();
            if (!recording)
                dataManager.StartSection();
            recording = true;
            recording_state = OutputState.Started;
            frame_index = 0;
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

            //HEARING
            if (recording_hearing && !hearingRecorder.isRecording)
            {
                hearingRecorder.saving_folder = dataManager.saving_folder;
                hearingRecorder.StartRecording();
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
            recording_state = OutputState.Stopped;

            //STREAMS
            if (streamRecorder.isRecording)
                streamRecorder.StopRecording();

            //AUDIO
            if (voiceRecorder.isRecording)
                voiceRecorder.StopRecording();

            //HEARING
            if (hearingRecorder.isRecording)
                hearingRecorder.StopRecording();

            //VIDEO
            if (visionRecorder.isRecording)
                visionRecorder.StopRecording();

            dataManager.StopSection();
        }

        public void ToggleRecording()
        {
            //UniLog.Log("Start/Stop recording");
            if (recording) StopRecording();
            else StartRecording();
        }
        public void StartPlaying()
        {
            UniLog.Log("Start playing");
            playing = true;
            playing_state = OutputState.Started;
            frame_index = 0;
            //Set the recordings time to now
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            if (!streamPlayer.isPlaying)
                streamPlayer.StartPlaying();
        }
        public void StopPlaying()
        {
            UniLog.Log("Stop playing");
            playing = false;
            playing_state = OutputState.Stopped;
            if (streamPlayer.isPlaying)
                streamPlayer.StopPlaying();
        }
        public void TogglePlaying()
        {
            if (playing)
            {
                StopPlaying();
            } else
            {
                StartPlaying();
            }

        }


    }
}
