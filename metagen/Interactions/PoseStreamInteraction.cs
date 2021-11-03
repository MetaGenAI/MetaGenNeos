using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseX;
using System.IO;

namespace metagen
{
    public class PoseStreamInteraction : IInteraction
    {
        MetaGen metagen_comp;
        public PoseStreamInteraction(MetaGen component)
        {
            metagen_comp = component;
        }
        public void InteractPoseStreams()
        {

            RefID user_id = RefID.Parse("IDC00");
            //Do this for all the necessary bytes for one step
            foreach (Byte b in BitConverter.GetBytes(0.0f))
                metagen_comp.streamPlayer.output_readers[user_id].TargetStream.WriteByte(b);
            metagen_comp.streamPlayer.PlayStreams();
        }
        public void StartInteracting()
        {
            //Acting
            metagen_comp.streamPlayer.PrepareStreamsExternal();
            //Do this for all the necessary bytes for the heading
            RefID user_id = RefID.Parse("IDC00");
            foreach (Byte b in BitConverter.GetBytes(0.0f))
                metagen_comp.streamPlayer.output_readers[user_id].TargetStream.WriteByte(b);
            metagen_comp.streamPlayer.StartPlayingExternal();
        }
        public void StopInteracting()
        {
        }

        //void WaitForFinish();
    }
}
