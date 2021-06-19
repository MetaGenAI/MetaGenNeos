using System;
using System.Collections.Generic;
using System.Text;
using System.IO; // for FileStream
//using System; // for BitConverter and Byte Type
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using metagen;
using BaseX;


namespace UnityNeos
{

    public class AudioRecorderNeos : MonoBehaviour {
        private int bufferSize;
        private int numBuffers;
        private int outputRate = 44100;
        //private String fileName = "recTest.wav";
        public string saving_folder;
        public string userID;
        private int headerSize = 44; //default for uncompressed wav
        public bool isRecording = false;
        public bool videoStartedRecording = false;
        AudioRecorder recorder;
        public MetaGen metagen_comp;
        public FrooxEngine.Slot earSlot;
     
        void Awake()
        {
            outputRate = AudioSettings.outputSampleRate;
        }

        void Start()
        {
            AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
        }

        //void Update()
        //{
        //    //if (Input.GetKeyDown("r"))
        //    //{
        //    //    print("rec");
        //    //    if (recOutput == false)
        //    //    {
        //    //        Guid g = Guid.NewGuid();
        //    //        recorder = new AudioRecorder(g.ToString(), bufferSize, numBuffers, outputRate);
        //    //        recorder.StartWriting();
        //    //        recOutput = true;
        //    //    }
        //    //    else
        //    //    {
        //    //        recOutput = false;
        //    //        recorder.WriteHeader();
        //    //        print("rec stop");
        //    //    }
        //    //}
        //}

        //public void UpdateTransform(float3 global_position, floatQ global_rotation)
        //{
        //    //this.gameObject.transform.position = global_position.ToUnity();
        //    //this.gameObject.transform.rotation = global_rotation.ToUnity();
        //    earSlot.GlobalPosition = global_position;
        //    earSlot.GlobalRotation = global_rotation;
        //}

        public void StartRecording()
        {
            //Guid g = Guid.NewGuid();
            saving_folder = metagen_comp.dataManager.saving_folder;
            recorder = new AudioRecorder(Path.Combine(saving_folder,userID+"_hearing_tmp"), bufferSize, numBuffers, outputRate);
            recorder.StartWriting();
            isRecording = true;
            print("hearing rec start");
        }
        public void StopRecording()
        {
            isRecording = false;
            videoStartedRecording = false;
            recorder.WriteHeader();
            //Task task = Task.Run(() =>
            //{
            File.Move(Path.Combine(saving_folder,userID + "_hearing_tmp.wav"), Path.Combine(saving_folder,userID + "_hearing.wav"));
            //});
            //task.Wait();

            print("hearing rec stop");
        }
        public void WaitForFinish()
        {
            List<string> current_users_ids = new List<string> { userID };
            Task[] tasks = new Task[current_users_ids.Count];
            int MAX_WAIT_ITERS = 1000000;
            for (int i = 0; i < current_users_ids.Count; i++)
            {
                string user_id = current_users_ids[i];
                Task task2 = Task.Run(() =>
                {
                    int iter = 0;
                    while (!File.Exists(saving_folder + "/" + user_id + "_hearing.ogg") && iter <= MAX_WAIT_ITERS) { Thread.Sleep(10); iter += 1; }
                });
                tasks[i] = task2;
            }
            Task.WaitAll(tasks);
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            //UniLog.Log("KEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEK");
            if (isRecording) //&& videoStartedRecording)
            {
                recorder.ConvertAndWrite(data); //audio data is interlaced
            }
        }

    }
}
