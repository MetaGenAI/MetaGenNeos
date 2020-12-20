using System;
using System.Collections.Generic;
using System.Text;
using System.IO; // for FileStream
using System; // for BitConverter and Byte Type
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

        public void UpdatePosition(float3 global_position)
        {
            this.gameObject.transform.position = global_position.ToUnity();
        }

        public void StartRecording()
        {
            //Guid g = Guid.NewGuid();
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
