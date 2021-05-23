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
    class PoseStreamRecorder : IRecorder
    {
        public Dictionary<RefID, BitBinaryWriterX> output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, FileStream> output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>> avatar_stream_drivers = new Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>>();
        public Dictionary<RefID, List<Tuple<BodyNode, IAvatarObject>>> avatar_pose_nodes = new Dictionary<RefID, List<Tuple<BodyNode, IAvatarObject>>>();
        public Dictionary<RefID, FingerPoseStreamManager> finger_stream_drivers = new Dictionary<RefID, FingerPoseStreamManager>();
        public List<RefID> current_users = new List<RefID>();
        public bool isRecording = false;
        private MetaGen metagen_comp;
        public string saving_folder {
            get {
                return metagen_comp.dataManager.saving_folder;
                }
        }
        public PoseStreamRecorder(MetaGen component)
        {
            metagen_comp = component;
        }
        public void RecordStreams(float deltaT)
        {
            foreach (RefID user_id in current_users)
            {
                //Encode the streams
                BinaryWriterX writer = output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float
                int node_index = 0;
                //foreach (var item in avatar_stream_drivers[user_id])
                foreach (var item in avatar_pose_nodes[user_id])
                {
                    BodyNode node = item.Item1;
                    //UniLog.Log(node);
                    //TransformStreamDriver driver = item.Item2;
                    TransformStreamDriver driver = avatar_stream_drivers[user_id][node_index].Item2;
                    IAvatarObject avatarObject = item.Item2;
                    Slot slot = null;
                    if (node == BodyNode.Root)
                    {
                        slot = driver.Slot;
                    } else
                    {
                        slot = avatarObject.Slot;
                    }

                    //WRITE the transform

                    //scale stream;
                    if (driver.ScaleStream.Target != null)
                    {
                        float3 scale = slot.LocalScale;
                        scale = slot.Parent.LocalScaleToSpace(scale,driver.Slot.Parent);
                        if (node == BodyNode.Root)
                            scale = driver.TargetScale;
                        writer.Write((float)(scale.x));
                        writer.Write((float)(scale.y));
                        writer.Write((float)(scale.z));
                    }
                    //position stream;
                    if (driver.PositionStream.Target != null)
                    {
                        float3 position = slot.LocalPosition;
                        position = slot.Parent.LocalPointToSpace(position,driver.Slot.Parent);
                        if (node == BodyNode.Root)
                        if (node == BodyNode.Root)
                            position = driver.TargetPosition;
                        writer.Write(position.x);
                        writer.Write(position.y);
                        writer.Write(position.z);
                    }
                    //rotation stream;
                    if (driver.RotationStream.Target != null)
                    {
                        floatQ rotation = slot.LocalRotation;
                        rotation = slot.Parent.LocalRotationToSpace(rotation,driver.Slot.Parent);
                        if (node == BodyNode.Root)
                            rotation = driver.TargetRotation;
                        writer.Write(rotation.x);
                        writer.Write(rotation.y);
                        writer.Write(rotation.z);
                        writer.Write(rotation.w);
                    }
                    node_index++;
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
            foreach (var userItem in metagen_comp.userMetaData)
            {
                User user = userItem.Key;
                UserMetadata metadata = userItem.Value;
                if (!(metadata.isRecording || metagen_comp.record_everyone)) continue;
                RefID user_id = user.ReferenceID;
                output_fss[user_id] = new FileStream(saving_folder + "/" + user_id.ToString() + "_streams.dat", FileMode.Create, FileAccess.ReadWrite);

                BitWriterStream bitstream = new BitWriterStream(output_fss[user_id]);
                output_writers[user_id] = new BitBinaryWriterX(bitstream);
                avatar_stream_drivers[user_id] = new List<Tuple<BodyNode, TransformStreamDriver>>();
                List<AvatarObjectSlot> components = user.Root.Slot.GetComponentsInChildren<AvatarObjectSlot>();
                finger_stream_drivers[user_id] = user.Root.Slot.GetComponent<FingerPoseStreamManager>();
                avatar_pose_nodes[user_id] = new List<Tuple<BodyNode, IAvatarObject>>();
                //WRITE the absolute time
                output_writers[user_id].Write((float)DateTimeOffset.Now.ToUnixTimeMilliseconds()); //absolute time
                int numValidNodes = 0;
                foreach (AvatarObjectSlot comp in components)
                {
                    if (comp.IsTracking.Value)
                    {
                        if (comp.Node.Value == BodyNode.LeftController || comp.Node.Value == BodyNode.RightController || comp.Node.Value == BodyNode.NONE) continue;
                        avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(comp.Node, comp.Equipped?.Target));
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
                }
                float3 avatar_root_scale = user.Root.Slot.GetComponentInChildren<AvatarRoot>().Scale.Value;
                float3 relative_avatar_scale = user.Root.Slot.GetComponentInChildren<AvatarRoot>().Slot.LocalScale / avatar_root_scale;
                //WRITE version identifier
                output_writers[user_id].Write(1001); //int
                //WRITE relative avatar scale
                output_writers[user_id].Write(relative_avatar_scale.x); //float
                output_writers[user_id].Write(relative_avatar_scale.y); //float
                output_writers[user_id].Write(relative_avatar_scale.z); //float
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
            avatar_pose_nodes = new Dictionary<RefID, List<Tuple<BodyNode, IAvatarObject>>>();
            current_users = new List<RefID>();
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }
    }
}
