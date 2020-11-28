using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using System.IO;
using FrooxEngine.CommonAvatar;
using Stream = FrooxEngine.Stream;

namespace metagen
{
    class PoseStreamRecorder
    {
        private DateTime utcNow;
        public Dictionary<RefID, BitBinaryWriterX> output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, FileStream> output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, List<Tuple<BodyNode,TransformStreamDriver>>> avatar_object_slots = new Dictionary<RefID, List<Tuple<BodyNode,TransformStreamDriver>>>();
        //TODO
        public void RecordStreams(float deltaT)
        {
            //TODO: Need to save info about what each of the streams are!!
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                if (!output_writers.ContainsKey(user_id))
                {
                    output_fss[user_id] = new FileStream(user_id.ToString() + "_streams.dat", FileMode.Create, FileAccess.ReadWrite);
                    BitWriterStream bitstream = new BitWriterStream(output_fss[user_id]);
                    output_writers[user_id] = new BitBinaryWriterX(bitstream);
                    avatar_object_slots[user_id] = new List<Tuple<BodyNode, TransformStreamDriver>>();
                    List<AvatarObjectSlot> components = user.Root.Slot.GetComponentsInChildren<AvatarObjectSlot>();
                    //WRITE the absolute time
                    output_writers[user_id].Write((float)DateTimeOffset.Now.ToUnixTimeMilliseconds()); //absolute time
                    //WRITE the number of body nodes
                    int numValidNodes = 0;
                    foreach(AvatarObjectSlot comp in components)
                    {
                        if (comp.Node.Value == BodyNode.LeftController || comp.Node.Value == BodyNode.RightController || comp.Node.Value == BodyNode.NONE) continue;
                        TransformStreamDriver driver = comp.Slot.Parent.GetComponent<TransformStreamDriver>();
                        if (driver != null)
                        {
                            avatar_object_slots[user_id].Add(new Tuple<BodyNode, TransformStreamDriver>(comp.Node.Value, driver));
                        } else //if the driver is not in the parent, then it is in the slot (which is what happens for the root)
                        {
                            driver = comp.Slot.GetComponent<TransformStreamDriver>();
                            avatar_object_slots[user_id].Add(new Tuple<BodyNode, TransformStreamDriver>(comp.Node.Value, driver));
                        }
                        numValidNodes += 1;
                    }
                    output_writers[user_id].Write(numValidNodes); //int
                    foreach(var item in avatar_object_slots[user_id])
                    {
                        output_writers[user_id].Write((int)item.Item1);
                    }
                    deltaT = 0f; //for the initial step written to the file, the deltaT is 0
                } 

                //Encode the streams
                //TODO: add rest of streams (in particular controller streams and root stream (if not included here?)
                BinaryWriterX writer = output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float
                foreach(var item in avatar_object_slots[user_id])
                {
                    BodyNode node = item.Item1;
                    TransformStreamDriver driver = item.Item2;
                    //WRITE the transform

                    //scale stream;
                    if (driver.ScaleStream.Target != null)
                    {
                        float3 scale = driver.TargetScale;
                        writer.Write((float) (0.004*scale.x));
                        writer.Write((float) (0.004*scale.y));
                        writer.Write((float) (0.004*scale.z));
                    } else
                    {
                        writer.Write((float) 1.0f);
                        writer.Write((float) 1.0f);
                        writer.Write((float) 1.0f);
                    }
                    //position stream;
                    float3 position = driver.TargetPosition;
                    if (node == BodyNode.Root)
                    {
                        writer.Write(position.x);
                        writer.Write(position.y);
                        writer.Write(position.z);
                    } else
                    {
                        writer.Write((float) (position.x/0.004));
                        writer.Write((float) (position.y/0.004));
                        writer.Write((float) (position.z/0.004));
                    }
                    //rotation stream;
                    floatQ rotation = driver.TargetRotation;
                    writer.Write(rotation.x);
                    writer.Write(rotation.y);
                    writer.Write(rotation.z);
                    writer.Write(rotation.w);
                    //UniLog.Log(rotation.ToString());
                }
            }

        }
        public void StartRecording()
        {

        }
        public void StopRecording()
        {
            foreach (var item in output_writers)
            {
                item.Value.Flush();
            }
            foreach (var item in output_fss)
            {
                item.Value.Close();
            }
            output_writers = new Dictionary<RefID, BitBinaryWriterX>();
            output_fss = new Dictionary<RefID, FileStream>();
        }
    }
}
