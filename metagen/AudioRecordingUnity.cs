using System;
using System.Collections.Generic;
using System.Text;
using System.IO; // for FileStream
using System; // for BitConverter and Byte Type
using UnityEngine;
using metagen;


namespace UnityNeos
{

    public class AudioRecorderNeos : MonoBehaviour {
        private int bufferSize;
        private int numBuffers;
        private int outputRate = 44100;
        private String fileName = "recTest.wav";
        private int headerSize = 44; //default for uncompressed wav
        private bool recOutput;
        AudioRecorder recorder;
     
        void Awake()
        {
            outputRate = AudioSettings.outputSampleRate;
        }

        void Start()
        {
            AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
        }

        void Update()
        {
            if (Input.GetKeyDown("r"))
            {
                print("rec");
                if (recOutput == false)
                {
                    Guid g = Guid.NewGuid();
                    recorder = new AudioRecorder(g.ToString(), bufferSize, numBuffers, outputRate);
                    recorder.StartWriting();
                    recOutput = true;
                }
                else
                {
                    recOutput = false;
                    recorder.WriteHeader();
                    print("rec stop");
                }
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (recOutput)
            {
                recorder.ConvertAndWrite(data); //audio data is interlaced
            }
        }

    }
}
