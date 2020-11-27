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


namespace FrooxEngine.LogiX
{

    [Category("LogiX/AAAA")]
    [NodeName("MetaGen")]
    public class UwU : LogixNode
    {

        //public SpinQueue<SyncMessage> messagesToTransmit;
        public Dictionary<User, List<Slot>> joint_slots = new Dictionary<User, List<Slot>>();
        public BinaryWriter writer;
        public bool recording_streams = false;
        public bool recording_audio = false;
        public bool recording_video = false;
        public bool playing_streams = false;
        public Dictionary<RefID, BitBinaryWriterX> output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, BitBinaryReaderX> output_readers = new Dictionary<RefID, BitBinaryReaderX>();
        public Dictionary<RefID, FileStream> output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
        private Dictionary<RefID, AudioRecorder> audio_recorders = new Dictionary<RefID, AudioRecorder>();
        private Dictionary<RefID, Camera> cameras = new Dictionary<RefID, Camera>();
        private Dictionary<RefID, VisualRecorder> visual_recorders = new Dictionary<RefID, VisualRecorder>();
        private DateTime utcNow;

        public int2 camera_resolution = new int2(256, 256);
        protected override void OnAttach()
        {
            base.OnAttach();
            //This records the audio from an audiolistener. Unfortunately we can only have one audiolistener in an Unity scene:/
            UniLog.Log("Adding Audio Listener");
            GameObject gameObject = GameObject.Find("AudioListener");
            UnityNeos.AudioRecorderNeos recorder = gameObject.AddComponent<UnityNeos.AudioRecorderNeos>();
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
                    foreach (var item in output_writers)
                    {
                        item.Value.Flush();
                    }
                    foreach (var item in output_fss)
                    {
                        item.Value.Close();
                    }
                    output_writers = new Dictionary<RefID, BitBinaryWriterX>();
                    output_fss = new Dictionary<RefID, FileStream>();
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
                recording_video = !recording_video;
                if (!recording_video)
                {
                    foreach (var item in visual_recorders)
                    {
                        item.Value.Close();
                        //TODO: check recording several videos. It doesn't seem to be working
                    }
                    foreach (var item in cameras)
                    {
                        item.Value.Slot.RemoveComponent(item.Value);
                    }
                    cameras = new Dictionary<RefID, Camera>();
                    visual_recorders = new Dictionary<RefID, VisualRecorder>();
                } 
            }

            //Start/Stop playing (not implemented yet)
            if (this.Input.GetKeyDown(Key.P))
            {
                playing_streams = !playing_streams;
                this.World.Session.isPluginRecordinPlayingBack = playing_streams;
            }

            if (recording_streams)
            {
                RecordStreams();
            }

            if (recording_video)
            {
                float delta = (float)(DateTime.UtcNow - utcNow).TotalSeconds;
                if (delta>0.03333)
                {
                    RecordVideo();
                    utcNow = DateTime.UtcNow;
                }
            }

            if (playing_streams)
            {
                //Doesn't currently work
                //PlayStreams();
            }
        }

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
                        //audio_outputs[user_id].Source.Target.Read<MonoSample>(SampleHelper.AsMonoBuffer(buffer, 0, -1));
                        audio_outputs[user_id].Source.Target.Read(buffer.AsMonoBuffer());
                        audio_recorders[user_id].ConvertAndWrite(buffer);
                    }
                }
            }
        }

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
                    //byte[] frame = base.World.Render.Connector.Render(cameras[user_id].GetRenderSettings(new int2(84, 84))).Result;
                    this.StartTask((Func<Task>)(async () =>
                    {
                        Bitmap2D bmp = await cameras[user_id].RenderToBitmap(camera_resolution);
                        visual_recorders[user_id].WriteFrame(bmp.ConvertTo(CodeX.TextureFormat.RGBA32).RawData);
                    }));
                    //byte[] frame = new byte[256*256];
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


        //One step of recording streams
        private void RecordStreams()
        {
            //TODO: Need to save info about what each of the streams are!!
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                if (!output_writers.ContainsKey(user_id))
                {
                    output_fss[user_id] = new FileStream(user_id.ToString() + "_streams.dat", FileMode.Create, FileAccess.ReadWrite);
                    BitWriterStream bitstream = new BitWriterStream(output_fss[user_id]);
                    output_writers[user_id] = new BitBinaryWriterX(bitstream);
                }

                //Encode the streams
                //Currently only encoding the streams of type float3 and floatQ
                //TODO: move this to their own functions and allow for other types of streams
                var orderedPairs = user.Streams.OrderBy(s => (ulong)s.ReferenceID);
                foreach (Stream stream in orderedPairs)
                {
                    if (IsSubclassOfRawGeneric(typeof(ValueStreamBase<>), stream.GetType()))
                    {
                        //RefID stream_id = stream.ReferenceID;
                        //output_writers[user_id].Write((ulong)stream_id);
                        if (stream is ValueStream<float3>) {
                            ValueStream<float3> stream1 = (ValueStream<float3>)stream;
                            output_writers[user_id].Write((byte)0);
                            output_writers[user_id].Write(stream1.Value.x);
                            output_writers[user_id].Write(stream1.Value.y);
                            output_writers[user_id].Write(stream1.Value.z);
                            //UniLog.Log(stream1.Value.ToString());
                        }
                        if (stream is ValueStream<floatQ>) {
                            ValueStream<floatQ> stream1 = (ValueStream<floatQ>)stream;
                            output_writers[user_id].Write((byte)1);
                            output_writers[user_id].Write(stream1.Value.x);
                            output_writers[user_id].Write(stream1.Value.y);
                            output_writers[user_id].Write(stream1.Value.z);
                            output_writers[user_id].Write(stream1.Value.w);
                        }
                        //stream.Encode(output_writers[user_id]);
                    }
                }
            }

        }
        //Play the streams, for one step
        private void PlayStreams()
        {
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                //Check if a user doesn't have their streams being currently recorded
                //TODO: check if a user has left and then finalize those streams
                RefID user_id = user.ReferenceID;
                if (!output_readers.ContainsKey(user_id))
                {
                    output_fss[user_id] = new FileStream(user_id.ToString() + "_streams.dat", FileMode.Open, FileAccess.Read);
                    BitReaderStream bitstream = new BitReaderStream(output_fss[user_id]);
                    output_readers[user_id] = new BitBinaryReaderX(bitstream);
                }
                //Decode the streams
                //Currently only recording the streams of type float3 and floatQ
                //TODO: this part doesn't currently work and relies on a modified client that allows writting to Value. So probably not gonna use this part and I'll probably delete it soon.
                //TODO: instead may need something like Common Avatar Creator but where we plug in our owns treams!
                var orderedPairs = user.Streams.OrderBy(s => (ulong)s.ReferenceID);
                foreach (Stream stream in orderedPairs)
                {
                    if (IsSubclassOfRawGeneric(typeof(ValueStreamBase<>), stream.GetType()))
                    {
                        //RefID stream_id = stream.ReferenceID;
                        //output_writers[user_id].Write((ulong)stream_id);
                        //StreamMessage fake_message = new StreamMessage(this.World.SyncTick+1, this.World.SyncTick+1, this.World.Session.HostConnection, false);
                        //stream.Decode(output_readers[user_id], fake_message);
                        if (stream is ValueStream<float3>) {
                            ValueStream<float3> stream1 = (ValueStream<float3>)stream;
                            output_readers[user_id].ReadByte();
                            float x = output_readers[user_id].ReadSingle();
                            float y = output_readers[user_id].ReadSingle();
                            float z = output_readers[user_id].ReadSingle();
                            stream1.Value = new float3(x, y, z);
                            UniLog.Log(stream1.Value.ToString());
                        }
                        if (stream is ValueStream<floatQ>) {
                            ValueStream<floatQ> stream1 = (ValueStream<floatQ>)stream;
                            output_readers[user_id].ReadByte();
                            float x = output_readers[user_id].ReadSingle();
                            float y = output_readers[user_id].ReadSingle();
                            float z = output_readers[user_id].ReadSingle();
                            float w = output_readers[user_id].ReadSingle();
                            stream1.Value = new floatQ(x, y, z, w);
                        }
                    }
                }
            }
        }
    static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        return false;
    }
    }
}
