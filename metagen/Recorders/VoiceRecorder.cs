using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using BaseX;
using CodeX;
using FrooxEngine.LogiX;
using System.IO;
using RefID = BaseX.RefID;
//using UnityNeos;
//using UnityEngine;

namespace metagen
{
    public class VoiceRecorder : IRecorder
    {
        public Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
        private Dictionary<RefID, AudioRecorder> audio_recorders = new Dictionary<RefID, AudioRecorder>();
        private List<string> current_users_ids = new List<string>();
        public bool isRecording = false;
        private MetaGen metagen_comp;
        private float[] buffer;
        public bool audio_sources_ready = false;
        public string saving_folder;
        //public string saving_folder {
        //    get {
        //        return metagen_comp.dataManager.saving_folder;
        //    }
        //}

        public VoiceRecorder(MetaGen component)
        {
            metagen_comp = component;
            this.buffer = new float[metagen_comp.Engine.AudioSystem.BufferSize];
        }

        //Record one chunk from the voice audio of each user
        public void RecordAudio()
        {
            //Debug.Log("Recording audio");
            foreach (var item in audio_outputs)
            {
                AudioOutput audio_output = item.Value;
                RefID user_id = item.Key;
                if (audio_output != null)
                {
                    buffer.EnsureSize<float>(metagen_comp.Engine.AudioSystem.BufferSize, false);
                    if (audio_output.Source.Target != null)
                    {
                        //AudioSystemConnector.InformOfDSPTime(AudioSettings.dspTime);
                        FrooxEngine.Engine.Current.AudioRead();
                        audio_sources_ready = true;
                        AudioStream<MonoSample> stream = (AudioStream<MonoSample>)audio_output.Source.Target;
                        stream.Read<MonoSample>(buffer.AsMonoBuffer());
                        //if (buffer.Length > stream.MissedSamples)
                        //{
                        //    buffer = buffer.Take(buffer.Length - stream.MissedSamples).ToArray();
                        //buffer[0] = 0f;
                        //buffer[1] = 0f;
                        //buffer[2] = 0f;
                        //buffer[3] = 0f;
                        //Console.WriteLine("[{0}]", string.Join(", ", buffer));
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = MathX.Clamp(buffer[i], -1, 1);
                        }
                        //UniLog.Log(buffer.Length);
                        audio_recorders[user_id].ConvertAndWrite(buffer);
                            //Task.Run(() => { audio_recorders[user_id].ConvertAndWrite(buffer); });
                            //metagen_comp.StartTask(async () => { audio_recorders[user_id].ConvertAndWrite(buffer); });
                        //}
                    } else
                    {
                        UniLog.Log("Audio Output Source target was null! (hmm should we restart it?). Did it happen coz a user left (in which case we shouldn't restart it), or something else?");
                        //UniLog.Log("Restarting audio recording coz audio output source target was null!");
                        //StopRecording();
                        //StartRecording();
                    }
                }
            }
        }

        public void StartRecording()
        {
            current_users_ids = new List<string>();
            saving_folder = metagen_comp.dataManager.saving_folder;
            foreach (var item in metagen_comp.userMetaData)
            {
                User user = item.Key;
                UserMetadata metadata = item.Value;
                //if (!metadata.isRecording || (metagen_comp.LocalUser == user && !metagen_comp.record_local_user)) continue;
                if (!metadata.isRecording) continue;
                RefID user_id = user.ReferenceID;
                current_users_ids.Add(user_id.ToString());
                AvatarAudioOutputManager comp = user.Root.Slot.GetComponentInChildren<AvatarAudioOutputManager>();
                AudioOutput audio_output = comp.AudioOutput.Target;
                audio_outputs[user_id] = audio_output;
                if (audio_outputs[user_id] == null)
                {
                    UniLog.Log("OwO: Audio output for user " + user_id.ToString() + " is null!");
                }
                else
                {
                    UniLog.Log("Sample rate");
                    UniLog.Log(metagen_comp.Engine.AudioSystem.Connector.SampleRate.ToString());
                    audio_recorders[user_id] = new AudioRecorder(saving_folder + "/" + user_id.ToString() + "_voice_tmp", metagen_comp.Engine.AudioSystem.BufferSize, 1, metagen_comp.Engine.AudioSystem.SampleRate, 1);
                    audio_recorders[user_id].StartWriting();
                }
            }
            isRecording = true;
        }
        public void StopRecording()
        {
            isRecording = false;
            foreach (var item in audio_recorders)
            {
                item.Value.WriteHeader();
            }
            string saving_folder_inner = saving_folder;
            var current_users_ids_inner = current_users_ids;
            Task task1 = Task.Run(() =>
            {
                foreach (string user_id in current_users_ids_inner)
                {
                    UniLog.Log("Moving " + Path.Combine(saving_folder_inner, user_id + "_voice_tmp.wav"));
                    //File.Move(saving_folder_inner + "/" + user_id + "_voice_tmp.wav", saving_folder_inner + "/" + user_id + "_voice.wav");
                    File.Move(Path.Combine(saving_folder_inner,user_id + "_voice_tmp.wav"), Path.Combine(saving_folder_inner,user_id + "_voice.wav"));
                }
                //current_users_ids = new List<string>();
            });
            task1.Wait();

            audio_outputs = new Dictionary<RefID, AudioOutput>();
            audio_recorders = new Dictionary<RefID, AudioRecorder>();
        }
        public void WaitForFinish()
        {
            Task[] tasks = new Task[current_users_ids.Count];
            int MAX_WAIT_ITERS = 100000;
            for (int i = 0; i < current_users_ids.Count; i++)
            {
                string user_id = current_users_ids[i];
                Task task2 = Task.Run(() =>
                {
                    int iter = 0;
                    while (File.Exists(saving_folder + "/" + user_id + "_voice.wav") && !File.Exists(saving_folder + "/" + user_id + "_voice.ogg") && iter <= MAX_WAIT_ITERS) { Thread.Sleep(10); iter += 1; }
                });
                tasks[i] = task2;
            }
            Task.WaitAll(tasks);
        }
    }
}
