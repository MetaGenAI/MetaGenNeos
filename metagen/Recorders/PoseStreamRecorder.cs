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
        public Dictionary<RefID, BitBinaryWriterX> output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, FileStream> output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>> avatar_stream_drivers = new Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>>();
        public Dictionary<RefID, FingerPoseStreamManager> finger_stream_drivers = new Dictionary<RefID, FingerPoseStreamManager>();
        public List<RefID> current_users = new List<RefID>();
        public bool isRecording = false;
        public string saving_folder;
        public void RecordStreams(float deltaT)
        {
            foreach (RefID user_id in current_users)
            {
                //Encode the streams
                BinaryWriterX writer = output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float
                foreach (var item in avatar_stream_drivers[user_id])
                {
                    BodyNode node = item.Item1;
                    TransformStreamDriver driver = item.Item2;

                    //WRITE the transform

                    //scale stream;
                    if (driver.ScaleStream.Target != null)
                    {
                        float3 scale = driver.TargetScale;
                        writer.Write((float)(scale.x));
                        writer.Write((float)(scale.y));
                        writer.Write((float)(scale.z));
                    }
                    //position stream;
                    if (driver.PositionStream.Target != null)
                    {
                        float3 position = driver.TargetPosition;
                        writer.Write(position.x);
                        writer.Write(position.y);
                        writer.Write(position.z);
                    }
                    //rotation stream;
                    if (driver.RotationStream.Target != null)
                    {
                        floatQ rotation = driver.TargetRotation;
                        writer.Write(rotation.x);
                        writer.Write(rotation.y);
                        writer.Write(rotation.z);
                        writer.Write(rotation.w);
                    }
                }
                //WRITE finger pose
                if (finger_stream_drivers[user_id] != null)
                {
                    //Left Hand
                    for (int index = 0; index < FingerPoseStreamManager.FINGER_NODE_COUNT; ++index)
                    {
                        BodyNode node = (BodyNode)(18 + index);
                        float3 position;
                        floatQ rotation;
                        bool was_succesful = finger_stream_drivers[user_id].TryGetFingerData(node, out position, out rotation);
                        //WRITE whether finger data was obtained
                        writer.Write(was_succesful);
                        writer.Write(rotation.x);
                        writer.Write(rotation.y);
                        writer.Write(rotation.z);
                        writer.Write(rotation.w);
                    }
                    //Right Hand
                    for (int index = 0; index < FingerPoseStreamManager.FINGER_NODE_COUNT; ++index)
                    {
                        BodyNode node = (BodyNode)(47 + index);
                        float3 position;
                        floatQ rotation;
                        bool was_succesful = finger_stream_drivers[user_id].TryGetFingerData(node, out position, out rotation);
                        //WRITE whether finger data was obtained
                        writer.Write(was_succesful);
                        writer.Write(rotation.x);
                        writer.Write(rotation.y);
                        writer.Write(rotation.z);
                        writer.Write(rotation.w);
                    }

                }
            }

        }
        public void StartRecording()
        {
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                output_fss[user_id] = new FileStream(saving_folder + "/" + user_id.ToString() + "_streams.dat", FileMode.Create, FileAccess.ReadWrite);

                BitWriterStream bitstream = new BitWriterStream(output_fss[user_id]);
                output_writers[user_id] = new BitBinaryWriterX(bitstream);
                avatar_stream_drivers[user_id] = new List<Tuple<BodyNode, TransformStreamDriver>>();
                List<AvatarObjectSlot> components = user.Root.Slot.GetComponentsInChildren<AvatarObjectSlot>();
                finger_stream_drivers[user_id] = user.Root.Slot.GetComponent<FingerPoseStreamManager>();
                //WRITE the absolute time
                output_writers[user_id].Write((float)DateTimeOffset.Now.ToUnixTimeMilliseconds()); //absolute time
                int numValidNodes = 0;
                foreach (AvatarObjectSlot comp in components)
                {
                    if (comp.Node.Value == BodyNode.LeftController || comp.Node.Value == BodyNode.RightController || comp.Node.Value == BodyNode.NONE) continue;
                    TransformStreamDriver driver = comp.Slot.Parent.GetComponent<TransformStreamDriver>();
                    if (driver != null)
                    {
                        avatar_stream_drivers[user_id].Add(new Tuple<BodyNode, TransformStreamDriver>(comp.Node.Value, driver));
                    }
                    else //if the driver is not in the parent, then it is in the slot (which is what happens for the root)
                    {
                        driver = comp.Slot.GetComponent<TransformStreamDriver>();
                        avatar_stream_drivers[user_id].Add(new Tuple<BodyNode, TransformStreamDriver>(comp.Node.Value, driver));
                    }
                    numValidNodes += 1;
                }
                //WRITE the number of body nodes
                output_writers[user_id].Write(numValidNodes); //int
                foreach (var item in avatar_stream_drivers[user_id])
                {
                    TransformStreamDriver driver = item.Item2;
                    //WRITE the the body node types
                    output_writers[user_id].Write((int)item.Item1);
                    //WRITE whether scaleStream is set
                    output_writers[user_id].Write(driver.ScaleStream.Target != null);
                    //WRITE whether positionStream is set
                    output_writers[user_id].Write(driver.PositionStream.Target != null);
                    //WRITE whether rotationStream is set
                    output_writers[user_id].Write(driver.RotationStream.Target != null);
                }
                //WRITE whether hands are being tracked
                output_writers[user_id].Write(finger_stream_drivers[user_id] != null);
                //WRITE whether metacarpals are tracked (just used as metadata)
                if (finger_stream_drivers[user_id] != null)
                {
                    output_writers[user_id].Write(finger_stream_drivers[user_id].TracksMetacarpals);
                }
                else
                {
                    output_writers[user_id].Write(false);
                }
                current_users.Add(user_id);
            }
            isRecording = true;
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
            avatar_stream_drivers = new Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>>();
            current_users = new List<RefID>();
            isRecording = false;
        }
    }
}
