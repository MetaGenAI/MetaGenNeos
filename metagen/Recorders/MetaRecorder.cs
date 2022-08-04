using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeosAnimationToolset;
using BaseX;
using FrooxEngine;
//TODO: make this work with IRecorders automatically or something

namespace metagen
{
    public class MetaRecorder : IRecorder
    {
        public VoiceRecorder voiceRecorder;
        public VisionRecorder visionRecorder;
        public int2 camera_resolution = new int2(64,64);
        public PoseStreamRecorder streamRecorder;
        public EyeStreamRecorder eyeRecorder;
        public MouthStreamRecorder mouthRecorder;
        //public InputDeviceStreamRecorder inputDeviceRecorder;
        public ControllerStreamRecorder controllerRecorder;
        public RecordingTool animationRecorder;
        public BvhRecorder bvhRecorder;
        private MetaGen metagen_comp;
        public bool isRecording;

        public MetaRecorder(MetaGen component)
        {
            metagen_comp = component;
            streamRecorder = new PoseStreamRecorder(metagen_comp);
            eyeRecorder = new EyeStreamRecorder(metagen_comp);
            mouthRecorder = new MouthStreamRecorder(metagen_comp);
            controllerRecorder = new ControllerStreamRecorder(metagen_comp);
            voiceRecorder = new VoiceRecorder(metagen_comp);
            bvhRecorder = new BvhRecorder(metagen_comp);
            visionRecorder = new VisionRecorder(camera_resolution, metagen_comp);
            animationRecorder = metagen_comp.Slot.AttachComponent<RecordingTool>();
            animationRecorder.metagen_comp = metagen_comp;
        }

        public void RecordFrame(float deltaT)
        {
            bool streams_ok = (streamRecorder == null ? false : streamRecorder.isRecording) || !metagen_comp.recording_streams;
            bool eye_streams_ok = (eyeRecorder == null ? false : eyeRecorder.isRecording) || !metagen_comp.recording_faces;
            bool mouth_streams_ok = (mouthRecorder == null ? false : mouthRecorder.isRecording) || !metagen_comp.recording_faces;
            bool controller_streams_ok = (controllerRecorder == null ? false : controllerRecorder.isRecording) || !metagen_comp.recording_controllers;
            bool vision_ok = (visionRecorder == null ? false : visionRecorder.isRecording) || !metagen_comp.recording_vision;
            bool hearing_ok = (metagen_comp.hearingRecorder == null ? false : metagen_comp.hearingRecorder.isRecording) || !metagen_comp.recording_hearing;
            bool voice_ok = (voiceRecorder == null ? false : (voiceRecorder.isRecording && voiceRecorder.audio_sources_ready)) || !metagen_comp.recording_voice;
            bool all_ready = voice_ok && streams_ok && controller_streams_ok && eye_streams_ok && mouth_streams_ok && vision_ok && hearing_ok;
            //bool all_ready = hearing_ok;
            if (all_ready && streamRecorder==null? false : streamRecorder.isRecording)
            {
                UniLog.Log("recording pose streams");
                streamRecorder.RecordStreams(deltaT);
            }

            if (all_ready && controllerRecorder==null? false : controllerRecorder.isRecording)
            {
                UniLog.Log("recording controller streams");
                controllerRecorder.RecordStreams(deltaT);
            }

            if (all_ready && eyeRecorder==null? false : eyeRecorder.isRecording)
            {
                //UniLog.Log("recording streams");
                eyeRecorder.RecordStreams(deltaT);
            }

            if (all_ready && mouthRecorder==null? false : mouthRecorder.isRecording)
            {
                //UniLog.Log("recording streams");
                mouthRecorder.RecordStreams(deltaT);
            }

            if (all_ready && visionRecorder==null? false : visionRecorder.isRecording)
            {
                //UniLog.Log("recording vision");
                //if (frame_index == 30)
                //    hearingRecorder.videoStartedRecording = true;
                visionRecorder.RecordVision();
            }

            if (all_ready && animationRecorder==null? false : animationRecorder.isRecording)
            {
                animationRecorder.RecordFrame();
            }

            if (all_ready && bvhRecorder==null? false : bvhRecorder.isRecording)
            {
                bvhRecorder.RecordFrame();
            }

            //if (recording && all_ready && recording_hearing_user != null && hearingRecorder==null? false : hearingRecorder.isRecording)
            //{
            //}
        }
        public void StartRecording()
        {
            //STREAMS
            if (metagen_comp.recording_streams && !streamRecorder.isRecording)
            {
                streamRecorder.StartRecording();
                //Record the first frame
                //streamRecorder.RecordStreams(0f);
            }

            //CONTROLLER STREAMS
            if (metagen_comp.recording_controllers && !controllerRecorder.isRecording)
            {
                controllerRecorder.StartRecording();
                //Record the first frame
                //controllerRecorder.RecordStreams(0f);
            }

            //EYE STREAMS
            if (metagen_comp.recording_faces && !eyeRecorder.isRecording)
            {
                eyeRecorder.StartRecording();
                //Record the first frame
                //eyeRecorder.RecordStreams(0f);
            }

            //MOUTH STREAMS
            if (metagen_comp.recording_faces && !mouthRecorder.isRecording)
            {
                mouthRecorder.StartRecording();
                //Record the first frame
                //mouthRecorder.RecordStreams(0f);
            }

            //ANIMATION
            if (metagen_comp.recording_animation && !animationRecorder.isRecording)
            {
                animationRecorder = metagen_comp.Slot.AttachComponent<RecordingTool>();
                animationRecorder.metagen_comp = metagen_comp;
                animationRecorder.StartRecording();
                //Record the first frame
                //animationRecorder.RecordFrame();
            }

            //BVH
            if (metagen_comp.recording_bvh && !bvhRecorder.isRecording)
            {
                bvhRecorder.StartRecording();
            }

            //AUDIO
            if (metagen_comp.recording_voice && !voiceRecorder.isRecording)
            {
                voiceRecorder.StartRecording();
            }

            //HEARING
            if (metagen_comp.recording_hearing && !metagen_comp.hearingRecorder.isRecording)
            {
                metagen_comp.hearingRecorder.StartRecording();
            }

            //VIDEO
            if (metagen_comp.recording_vision && !visionRecorder.isRecording)
            {
                visionRecorder.StartRecording();
                //Record the first frame
                //visionRecorder.RecordVision();
            }

            isRecording = true;
        }
        public void StopRecording()
        {
            bool wait_streams = false;
            bool wait_controller_streams = false;
            bool wait_eye_streams = false;
            bool wait_mouth_streams = false;
            bool wait_voices = false;
            bool wait_hearing = false;
            bool wait_vision = false;
            bool wait_anim = false;

            //STREAMS
            if (streamRecorder.isRecording)
            {
                streamRecorder.StopRecording();
                wait_streams = true;
            }

            //CONTROLLER STREAMS
            if (controllerRecorder.isRecording)
            {
                controllerRecorder.StopRecording();
                wait_controller_streams = true;
            }

            //EYE STREAMS
            if (eyeRecorder.isRecording)
            {
                eyeRecorder.StopRecording();
                wait_eye_streams = true;
            }

            //MOUTH STREAMS
            if (mouthRecorder.isRecording)
            {
                mouthRecorder.StopRecording();
                wait_mouth_streams = true;
            }

            //VOICES
            if (voiceRecorder.isRecording)
            {
                voiceRecorder.StopRecording();
                wait_voices = true;
            }

            //HEARING
            if (metagen_comp.hearingRecorder != null && metagen_comp.hearingRecorder.isRecording)
            {
                metagen_comp.hearingRecorder.StopRecording();
                wait_hearing = true;
            }

            //VISION
            if (visionRecorder.isRecording)
            {
                visionRecorder.StopRecording();
                wait_vision = true;
            }

            //BVH
            if (bvhRecorder.isRecording)
            {
                bvhRecorder.StopRecording();
            }

            try
            {
                if (animationRecorder.isRecording)
                {
                    animationRecorder.PreStopRecording();
                    wait_anim = true;
                }
            } catch (Exception e)
            {
                UniLog.Log(">w< animation stopping failed");
            }


            Task task = Task.Run(() =>
            {
                try
                {
                    //STREAMS
                    if (wait_streams)
                    {
                        streamRecorder.WaitForFinish();
                        wait_streams = false;
                    }

                    //CONTROLLER STREAMS
                    if (wait_controller_streams)
                    {
                        controllerRecorder.WaitForFinish();
                        wait_controller_streams = false;
                    }

                    //EYE STREAMS
                    if (wait_eye_streams)
                    {
                        eyeRecorder.WaitForFinish();
                        wait_eye_streams = false;
                    }

                    //MOUTH STREAMS
                    if (wait_mouth_streams)
                    {
                        mouthRecorder.WaitForFinish();
                        wait_mouth_streams = false;
                    }

                    //VOICES
                    if (wait_voices)
                    {
                        UniLog.Log("Waiting voices");
                        voiceRecorder.WaitForFinish();
                        wait_voices = false;
                        UniLog.Log("Waited voices");
                    }

                    //HEARING
                    if (wait_hearing)
                    {
                        metagen_comp.hearingRecorder.WaitForFinish();
                        wait_hearing = false;
                    }

                    //VISION
                    if (wait_vision)
                    {
                        visionRecorder.WaitForFinish();
                        wait_vision = false;
                    }

                    metagen.Util.MediaConverter.WaitForFinish();

                    //ANIMATION
                    if (wait_anim)
                    {
                        animationRecorder.StopRecording();
                        animationRecorder.WaitForFinish();
                        metagen_comp.World.RunSynchronously(() =>
                        {
                            metagen_comp.Slot.RemoveComponent(animationRecorder);
                        });
                        wait_anim = false;
                    }
                } catch (Exception e)
                {
                    UniLog.Log("OwO error in waiting task when stopped recording: " + e.Message);
                    UniLog.Log(e.StackTrace);
                } finally
                {
                    UniLog.Log("FINISHED STOPPING RECORDING");
                    metagen_comp.recording_state = OutputState.Stopped;
                    metagen_comp.dataManager.StopSection();
                }
            });
            //task.ContinueWith((Task t) =>
            //{
            //    UniLog.Log("FINISHED STOPPING RECORDING");
            //    this.recording_state = OutputState.Stopped;
            //    dataManager.StopSection();
            //});
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }
    }
}
