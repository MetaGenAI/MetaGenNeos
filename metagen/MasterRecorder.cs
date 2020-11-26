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
        public bool playing_streams = false;
        public Dictionary<RefID, BitBinaryWriterX> output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, BitBinaryReaderX> output_readers = new Dictionary<RefID, BitBinaryReaderX>();
        public Dictionary<RefID, FileStream> output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
        private Dictionary<RefID, AudioRecorder> audio_recorders = new Dictionary<RefID, AudioRecorder>();
        protected override void OnAttach()
        {
            base.OnAttach();
            if (this.LocalUser.UserID == "U-guillefix")
            {
                Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;

                UniLog.Log("Adding Audio Listener");
                GameObject gameObject = GameObject.Find("AudioListener");
                UnityNeos.AudioRecorderNeos recorder = gameObject.AddComponent<UnityNeos.AudioRecorderNeos>();
                //GameObject gameObject2 = new GameObject("HIII");
                //gameObject2.AddComponent<AudioListener>();
                //gameObject2.AddComponent<UnityNeos.AudioRecorderNeos>();
                //recorder.StartWriting("test.wav");

            }
        }

        protected override void OnAudioUpdate()
        {
            Debug.Log("Audio Update");
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
                        if (audio_outputs[user_id]==null)
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

        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();
            UniLog.Log("Update");
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
                    foreach (var items in audio_recorders)
                    {
                        items.Value.WriteHeader();
                    }
                    audio_outputs = new Dictionary<RefID, AudioOutput>();
                    audio_recorders = new Dictionary<RefID, AudioRecorder>();
                }
            }

            if (this.Input.GetKeyDown(Key.P))
            {
                playing_streams = !playing_streams;
                this.World.Session.isPluginRecordinPlayingBack = playing_streams;
            }

            if (recording_streams)
            {
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
                                UniLog.Log(stream1.Value.ToString());
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

            if (playing_streams)
            {
                Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
                foreach (User user in users)
                {
                    RefID user_id = user.ReferenceID;
                    if (!output_readers.ContainsKey(user_id))
                    {
                        output_fss[user_id] = new FileStream(user_id.ToString() + "_streams.dat", FileMode.Open, FileAccess.Read);
                        BitReaderStream bitstream = new BitReaderStream(output_fss[user_id]);
                        output_readers[user_id] = new BitBinaryReaderX(bitstream);
                    }
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
