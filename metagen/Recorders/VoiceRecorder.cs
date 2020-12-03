using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using BaseX;
using CodeX;
using FrooxEngine.LogiX;

namespace metagen
{
    class VoiceRecorder
    {
        public Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
        private Dictionary<RefID, AudioRecorder> audio_recorders = new Dictionary<RefID, AudioRecorder>();
        public bool isRecording = false;
        public string saving_folder;
        private MetaGen comp;

        public VoiceRecorder(MetaGen component)
        {
            comp = component;
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
                    float[] buffer = new float[comp.Engine.AudioSystem.BufferSize];
                    buffer.EnsureSize<float>(comp.Engine.AudioSystem.BufferSize, false);
                    audio_output.Source.Target.Read(buffer.AsMonoBuffer());
                    audio_recorders[user_id].ConvertAndWrite(buffer);
                }
            }
        }

        public void StartRecording()
        {
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                if (!audio_outputs.ContainsKey(user_id))
                {
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
                        UniLog.Log(comp.Engine.AudioSystem.Connector.SampleRate.ToString());
                        audio_recorders[user_id] = new AudioRecorder(saving_folder + "/" + user_id.ToString() + "_audio", comp.Engine.AudioSystem.BufferSize, 1, comp.Engine.AudioSystem.SampleRate, 1);
                        audio_recorders[user_id].StartWriting();
                    }
                }
            }
            isRecording = true;
        }
        public void StopRecording()
        {
            foreach (var item in audio_recorders)
            {
                item.Value.WriteHeader();
            }
            audio_outputs = new Dictionary<RefID, AudioOutput>();
            audio_recorders = new Dictionary<RefID, AudioRecorder>();
            isRecording = false;
        }
    }
}
