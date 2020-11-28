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
    public class UwU : LogixNode
    {

        public bool recording_audio = false;
        public Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
        private Dictionary<RefID, AudioRecorder> audio_recorders = new Dictionary<RefID, AudioRecorder>();

        public bool recording_video = false;
        private Dictionary<RefID, Camera> cameras = new Dictionary<RefID, Camera>();
        private Dictionary<RefID, VisualRecorder> visual_recorders = new Dictionary<RefID, VisualRecorder>();
        public int2 camera_resolution = new int2(256, 256);
        private DateTime utcNow;

        public bool recording_streams = false;
        public bool playing_streams = false;
        private PoseStreamRecorder streamRecorder;
        private PoseStreamPlayer streamPlayer;
        //private metagen.AvatarManager avatarManager;

        protected override void OnAttach()
        {
            base.OnAttach();
            //This records the audio from an audiolistener. Unfortunately we can only have one audiolistener in an Unity scene:/
            //It starts/stops recording upon pressing the key R.
            //TODO: make below work in VR mode too
            //UniLog.Log("Adding Audio Listener");
            //GameObject gameObject = GameObject.Find("AudioListener");
            //UnityNeos.AudioRecorderNeos recorder = gameObject.AddComponent<UnityNeos.AudioRecorderNeos>();

            //avatarManager = new metagen.AvatarManager();
            //Recorder and Player for the pose data of the users
            streamRecorder = new PoseStreamRecorder();
            streamPlayer = new PoseStreamPlayer();

            //System.IO.File.WriteAllText(@"C:\Users\Public\TestFolder\WriteText.txt", this.Engine.CompatibilityHash);
            string compatibilityHash = "zXTWhGf44euiFzsWH5HFCQ==";
            var t = typeof(Engine);
            t.GetProperty("CompatibilityHash").SetValue(this.Engine, compatibilityHash, null);
            //Job<Slot> awaiter = this.Slot.TransferToWorld(Userspace.UserspaceWorld).GetAwaiter();
            //awaiter.GetResult();
        }
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();
            //Start/Stop recording
            if (this.Input.GetKeyDown(Key.R))
            {
                recording_streams = !recording_streams;
                if (!recording_streams)
                {
                    streamRecorder.StopRecording();
                }
                recording_audio = !recording_audio;
                if (!recording_audio)
                {
                    foreach (var item in audio_recorders)
                    {
                        item.Value.WriteHeader();
                    }
                    audio_outputs = new Dictionary<RefID, AudioOutput>();
                    audio_recorders = new Dictionary<RefID, AudioRecorder>();
                }
                //recording_video = !recording_video;
                if (!recording_video)
                {
                    foreach (var item in visual_recorders)
                    {
                        item.Value.Close();
                        //TODO: check recording several videos consequtively. It doesn't seem to be working
                    }
                    foreach (var item in cameras)
                    {
                        item.Value.Slot.RemoveComponent(item.Value);
                    }
                    cameras = new Dictionary<RefID, Camera>();
                    visual_recorders = new Dictionary<RefID, VisualRecorder>();
                } 
            }

            ////Spawn avatar test
            //if (this.Input.GetKeyDown(Key.Y))
            //{
            //    avatarManager.SpawnAvatar("neosdb:///3992605ec9c401672dd54ff388cce3bd6483313699e4e45642b3abe80941d98b.7zbson");
            //    //this.StartTask(async () =>
            //    //{
            //    //    //await avatarManager.SpawnAvatar("neosdb:///3992605ec9c401672dd54ff388cce3bd6483313699e4e45642b3abe80941d98b.7zbson");
            //    //});
            //}

            //Start/Stop playing
            if (this.Input.GetKeyDown(Key.P))
            {
                playing_streams = !playing_streams;
                if (!playing_streams)
                {
                    streamPlayer.StopPlaying();
                }
            }

            //Record one frame of video and streams
            //We condition on deltaT to be as close to 30fps as possible
            float deltaT = (float)(DateTime.UtcNow - utcNow).TotalMilliseconds;
            if (deltaT>33.3333)
            {
                if (recording_streams)
                {
                    streamRecorder.RecordStreams(deltaT);
                }

                if (recording_video)
                {
                        RecordVideo();
                }
                utcNow = DateTime.UtcNow;
                if (playing_streams)
                {
                    streamPlayer.PlayStreams();
                }
            }

        }

        //TODO OVERALL: Add the correct behaviour for when users leave

        //Record one chunk from the voice audio of each user
        protected override void OnAudioUpdate()
        {
            //Debug.Log("Audio Update");
            if (recording_audio)
            {
                Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
                foreach (User user in users)
                {
                    RefID user_id = user.ReferenceID;
                    if (!audio_outputs.ContainsKey(user_id))
                    {
                        CommonAvatar.AvatarAudioOutputManager comp = user.Root.Slot.GetComponentInChildren<CommonAvatar.AvatarAudioOutputManager>();
                        AudioOutput audio_output = comp.AudioOutput.Target;
                        audio_outputs[user_id] = audio_output;
                        if (audio_outputs[user_id] == null)
                        {
                            UniLog.Log("OwO: Audio output for user " + user_id.ToString() + " is null!");
                        } else
                        {
                            UniLog.Log("Sample rate");
                            UniLog.Log(this.Engine.AudioSystem.Connector.SampleRate.ToString());
                            audio_recorders[user_id] = new AudioRecorder(user_id.ToString() + "_audio", this.Engine.AudioSystem.BufferSize, 1, this.Engine.AudioSystem.SampleRate, 1);
                            audio_recorders[user_id].StartWriting();
                        }
                    }
                    if (audio_outputs[user_id] != null)
                    {
                        float[] buffer = new float[this.Engine.AudioSystem.BufferSize];
                        buffer.EnsureSize<float>(this.Engine.AudioSystem.BufferSize, false);
                        audio_outputs[user_id].Source.Target.Read(buffer.AsMonoBuffer());
                        audio_recorders[user_id].ConvertAndWrite(buffer);
                    }
                }
            }
        }

        //Record one frame of the head camera for each user
        private void RecordVideo()
        {
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                if (!visual_recorders.ContainsKey(user_id))
                {
                    Camera camera = user.Root.HeadSlot.AttachComponent<Camera>();
                    camera.GetRenderSettings(camera_resolution);
                    cameras[user_id] = camera;
                    visual_recorders[user_id] = new VisualRecorder(user_id.ToString()+"_video.avi", camera_resolution.x, camera_resolution.y, 30);
                    UniLog.Log("Made visual recorder");
                }
                if (visual_recorders[user_id] != null && cameras[user_id] != null)
                {
                    this.StartTask((Func<Task>)(async () =>
                    {
                        Bitmap2D bmp = await cameras[user_id].RenderToBitmap(camera_resolution);
                        visual_recorders[user_id].WriteFrame(bmp.ConvertTo(CodeX.TextureFormat.RGBA32).RawData);
                    }));
                }
                else
                { //something was null:P
                    bool vis_rec_null = false;
                    bool camera_null = false;
                    if (visual_recorders[user_id] == null) vis_rec_null = true;
                    if (cameras[user_id] == null) camera_null = true;
                    UniLog.Log("OwO. These things were null: " + (camera_null ? "Camera" : "") + (vis_rec_null ? "Visual recorder" : ""));
                }
            }

        }
    }
}
