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
using RefID = BaseX.RefID;

namespace metagen
{
    public class PoseStreamRecorder : UserBinaryDataRecorder, IRecorder
    {
        public Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>> avatar_stream_drivers = new Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>>();
        public Dictionary<RefID, List<Tuple<BodyNode, TrackedDevicePositioner>>> tracked_device_positioners = new Dictionary<RefID, List<Tuple<BodyNode, TrackedDevicePositioner>>>();
        public Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>> avatar_object_slots = new Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>>();
        public Dictionary<RefID, FingerPoseStreamManager> finger_stream_drivers = new Dictionary<RefID, FingerPoseStreamManager>();
        public List<RefID> current_users = new List<RefID>();
        public bool isRecording = false;
        public bool external_control = false;

        public PoseStreamRecorder(MetaGen component) : base(component)
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
                foreach (var item in avatar_object_slots[user_id])
                {
                    BodyNode node = item.Item1;
                    //UniLog.Log(node);
                    //TransformStreamDriver driver = item.Item2;
                    TransformStreamDriver driver = avatar_stream_drivers[user_id][node_index].Item2;
                    IAvatarObject avatarObject = item.Item2.Equipped?.Target;
                    Slot slot = null;
                    if (node == BodyNode.Root)
                    {
                        slot = driver.Slot;
                    } else if (avatarObject != null)
                    {
                        slot = avatarObject.Slot;
                    } else
                    {
                        slot = tracked_device_positioners[user_id][node_index].Item2.BodyNodeRoot.Target;
                    }

                    //WRITE the transform

                    //scale stream;
                    if (driver.ScaleStream.Target != null)
                    {
                        float3 scale = slot.LocalScale;
                        scale = slot.Parent.LocalScaleToSpace(scale,driver.Slot.Parent);
                        if (node == BodyNode.Root)
                        {
                            //scale = driver.TargetScale;
                            scale = slot.Parent.LocalScaleToGlobal(scale);
                        }
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
                        {
                            //position = driver.TargetPosition;
                            position = slot.Parent.LocalPointToGlobal(position);
                        }
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
                        {
                            //rotation = driver.TargetRotation;
                            rotation = slot.Parent.LocalRotationToGlobal(rotation);
                        }
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
            this.source_type = Source.FILE;
            StartRecordingInternal();
        }
        public void StartRecordingExternal()
        {
            external_control = true;
            this.source_type = Source.STREAM;
            StartRecordingInternal();
        }

        public void StartRecordingInternal()
        {
            RegisterUserStreams("streams");
            foreach (var userItem in metagen_comp.userMetaData)
            {
                User user = userItem.Key;
                UserMetadata metadata = userItem.Value;
                if (!metadata.isRecording || (metagen_comp.LocalUser == user && !metagen_comp.record_local_user)) continue;
                RefID user_id = user.ReferenceID;
                avatar_stream_drivers[user_id] = new List<Tuple<BodyNode, TransformStreamDriver>>();
                List<AvatarObjectSlot> components = user.Root.Slot.GetComponentsInChildren<AvatarObjectSlot>();
                finger_stream_drivers[user_id] = user.Root.Slot.GetComponent<FingerPoseStreamManager>();
                avatar_object_slots[user_id] = new List<Tuple<BodyNode, AvatarObjectSlot>>();
                tracked_device_positioners[user_id] = new List<Tuple<BodyNode, TrackedDevicePositioner>>();
                //WRITE the absolute time
                output_writers[user_id].Write((float)DateTimeOffset.Now.ToUnixTimeMilliseconds()); //absolute time
                int numValidNodes = 0;
                foreach (AvatarObjectSlot comp in components)
                {
                    if (comp.IsTracking.Value)
                    {
                        if (comp.Node.Value == BodyNode.NONE) continue;
                        //if (comp.Node.Value == BodyNode.LeftController || comp.Node.Value == BodyNode.RightController || comp.Node.Value == BodyNode.NONE) continue;
                        //if (comp.Node.Value == BodyNode.LeftHand || comp.Node.Value == BodyNode.RightHand)
                        //{
                        //    TrackedDevicePositioner positioner = comp.Slot.Parent.GetComponent<TrackedDevicePositioner>();
                        //    UniLog.Log(positioner.TrackedDevice.BodyNodePositionOffset);
                        //    UniLog.Log(positioner.TrackedDevice.BodyNodeRotationOffset);
                        //}
                        //avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(comp.Node, comp.Equipped?.Target));
                        avatar_object_slots[user_id].Add(new Tuple<BodyNode, AvatarObjectSlot>(comp.Node, comp));
                        TransformStreamDriver driver = comp.Slot.Parent.GetComponent<TransformStreamDriver>();
                        TrackedDevicePositioner positioner = comp.Slot.Parent.GetComponent<TrackedDevicePositioner>();
                        tracked_device_positioners[user_id].Add(new Tuple<BodyNode, TrackedDevicePositioner>(comp.Node.Value, positioner));
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
            if (!external_control) isRecording = true;
        }

        public void StopRecording()
        {
            UnregisterUserStreams();
            avatar_stream_drivers = new Dictionary<RefID, List<Tuple<BodyNode, TransformStreamDriver>>>();
            avatar_object_slots = new Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>>();
            tracked_device_positioners = new Dictionary<RefID, List<Tuple<BodyNode, TrackedDevicePositioner>>>();
            current_users = new List<RefID>();
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }
    }
}
