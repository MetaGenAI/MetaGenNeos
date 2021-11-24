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
        public PoseStreamInteraction poseStreamInteraction;
        public MetaInteraction(MetaGen component)
        {
            metagen_comp = component;
            voiceInteraction = new VoiceInteraction(metagen_comp);
            textInteraction = new TextInteraction(metagen_comp);
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
            if (!poseStreamInteraction.isInteracting)
            {
                poseStreamInteraction.StartInteracting();
            }
            if (!voiceInteraction.isInteracting)
            {
                voiceInteraction.StartInteracting();
            }
            if (!textInteraction.isInteracting)
            {
                textInteraction.StartInteracting();
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
            isInteracting = false;
        }
    }
}
