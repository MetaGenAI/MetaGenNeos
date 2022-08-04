using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseX;

namespace metagen.Interactions
{
    public class MetaInteraction : IInteraction
    {
        public bool isInteracting = false;
        public bool isRecording = false;
        private MetaGen metagen_comp;
        public VoiceInteraction voiceInteraction;
        public TextInteraction textInteraction;
        public FieldInteraction fieldInteraction;
        public PoseStreamInteraction poseStreamInteraction;
        public MetaInteraction(MetaGen component)
        {
            metagen_comp = component;
            voiceInteraction = new VoiceInteraction(metagen_comp);
            textInteraction = new TextInteraction(metagen_comp);
            fieldInteraction = new FieldInteraction(metagen_comp);
            poseStreamInteraction = new PoseStreamInteraction(metagen_comp);
        }

        public void InteractionStep(float deltaT)
        {
            //UniLog.Log("InteractionStep");
            if (poseStreamInteraction.isInteracting)
            {
                poseStreamInteraction.InteractPoseStreams(deltaT);
            }
        }

        public void StartInteracting()
        {
            try {
                if (!poseStreamInteraction.isInteracting)
                {
                    poseStreamInteraction.StartInteracting();
                }
            } catch (Exception e) {
                   UniLog.Log("OwO error in bakeAsync: " + e.Message);
                   UniLog.Log(e.StackTrace);
            }
            try {
                if (!voiceInteraction.isInteracting)
                {
                    voiceInteraction.StartInteracting();
                }
            } catch (Exception e) {
                   UniLog.Log("OwO error in bakeAsync: " + e.Message);
                   UniLog.Log(e.StackTrace);
            }
            try {
                if (!textInteraction.isInteracting)
                {
                    textInteraction.StartInteracting();
                }
            } catch (Exception e) {
                    UniLog.Log("OwO error in bakeAsync: " + e.Message);
                    UniLog.Log(e.StackTrace);
            }
            try {
                if (!fieldInteraction.isInteracting)
                {
                    fieldInteraction.StartInteracting();
                }
            } catch (Exception e) {
                    UniLog.Log("OwO error in bakeAsync: " + e.Message);
                    UniLog.Log(e.StackTrace);
            }
            isInteracting = true;
        }
        public void StopInteracting()
        {
            if (poseStreamInteraction.isInteracting)
            {
                poseStreamInteraction.StopInteracting();
            }
            if (voiceInteraction.isInteracting)
            {
                voiceInteraction.StopInteracting();
            }
            if (textInteraction.isInteracting)
            {
                textInteraction.StopInteracting();
            }
            if (fieldInteraction.isInteracting)
            {
                fieldInteraction.StopInteracting();
            }
            isInteracting = false;
        }
    }
}
