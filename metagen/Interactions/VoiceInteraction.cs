using System;
using System.Runtime;
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

namespace metagen.Interactions
{
    public class VoiceInteraction : IInteraction
    {
        public Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
        private Dictionary<RefID, AudioRecorder> audio_recorders = new Dictionary<RefID, AudioRecorder>();
        private List<string> current_users_ids = new List<string>();
        public bool isInteracting = false;
        public bool isRecording = false;
        private MetaGen metagen_comp;
        private float[] buffer;
        public bool audio_sources_ready = false;
        public string saving_folder {
            get {
                return metagen_comp.dataManager.temp_folder;
            }
        }

        public VoiceInteraction(MetaGen component)
        {
            metagen_comp = component;
            this.buffer = new float[metagen_comp.Engine.AudioSystem.BufferSize];
        }

        //Record one chunk from the voice audio of each user
        public void InteractAudio()
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
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = MathX.Clamp(buffer[i], -1, 1);
                        }
                        float max_val = 0;
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            float val_squared = (float) Math.Pow((double)buffer[i], (double)2);
                            if (val_squared > max_val) max_val = val_squared;
                        }
                        if (isRecording)
                        {
                            audio_recorders[user_id].ConvertAndWrite(buffer);
                            if (max_val == 0)
                            {
                                StopWritingFile(user_id);
                            }
                        }
                        else
                        {
                            UniLog.Log(max_val);
                            if (max_val > 0)
                            {
                                StartWritingFile(user_id);
                                audio_recorders[user_id].ConvertAndWrite(buffer);
                            }
                        }
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

        void StartWritingFile(RefID user_id)
        {
            Guid g = Guid.NewGuid();
            audio_recorders[user_id] = new AudioRecorder(saving_folder + "/" + user_id.ToString() + "_voice_tmp_"+g.ToString(), metagen_comp.Engine.AudioSystem.BufferSize, 1, metagen_comp.Engine.AudioSystem.SampleRate, 1);
            audio_recorders[user_id].StartWriting();
            isRecording = true;
        }
        void StopWritingFile(RefID user_id)
        {
            isRecording = false;
            audio_recorders[user_id].WriteHeader();
        }

        public void StartInteracting()
        {
            foreach (var item in metagen_comp.userMetaData)
            {
                User user = item.Key;
                UserMetadata metadata = item.Value;
                if (!(metadata.isRecording || metagen_comp.record_everyone)) continue;
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
                }
            }
            isInteracting = true;
        }
        public void StopInteracting()
        {
            audio_outputs = new Dictionary<RefID, AudioOutput>();
            audio_recorders = new Dictionary<RefID, AudioRecorder>();
            isInteracting = false;
        }
        //public void WaitForFinish()
        //{
        //}
    }
}
