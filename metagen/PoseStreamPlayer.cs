using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using System.IO;
using FrooxEngine.CommonAvatar;

namespace metagen
{
    class PoseStreamPlayer
    {
        private DateTime utcNow;
        public Dictionary<RefID, FileStream> output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, BitBinaryReaderX> output_readers = new Dictionary<RefID, BitBinaryReaderX>();
        public Dictionary<RefID, List<Tuple<BodyNode,IAvatarObject>>> avatar_pose_nodes = new Dictionary<RefID, List<Tuple<BodyNode,IAvatarObject>>>();
        public Dictionary<RefID, Slot> avatars = new Dictionary<RefID, Slot>();
        metagen.AvatarManager avatarManager;
        //TODO
        public PoseStreamPlayer()
        {
            avatarManager = new metagen.AvatarManager();
        }
        public void PlayStreams()
        {
            //TODO: Need this function to not depend on users being present! Just gave the info from the recorded data!!
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                if (!output_readers.ContainsKey(user_id))
                {
                    output_fss[user_id] = new FileStream(user_id.ToString() + "_streams.dat", FileMode.Open, FileAccess.Read);
                    BitReaderStream bitstream = new BitReaderStream(output_fss[user_id]);
                    output_readers[user_id] = new BitBinaryReaderX(bitstream);
                    avatar_pose_nodes[user_id] = new List<Tuple<BodyNode, IAvatarObject>>();
                    Slot avatar = avatarManager.GetAvatar();
                    avatars[user_id] = avatar;
                    List<IAvatarObject> components = avatar.GetComponentsInChildren<IAvatarObject>();

                    //READ absolute time
                    output_readers[user_id].ReadSingle();
                    //READ number of body nodes
                    int numBodyNodes = output_readers[user_id].ReadInt32();
                    for (int i = 0; i < numBodyNodes; i++)
                    {
                        int nodeInt = output_readers[user_id].ReadInt32();
                        BodyNode bodyNodeType = (BodyNode)nodeInt;

                        bool node_found = false;
                        foreach (IAvatarObject comp in components)
                        {
                            if (comp.Node == bodyNodeType)
                            {
                                avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(bodyNodeType, comp));
                                node_found = true;
                            }
                        }
                        if (!node_found) throw new Exception("Node " + bodyNodeType.ToString() + " not found in avatar!");
                    }
                }

                //Decode the streams
                BinaryReaderX reader = output_readers[user_id];

                //READ deltaT
                float deltaT = reader.ReadSingle();
                foreach (var item in avatar_pose_nodes[user_id])
                {
                    BodyNode node = item.Item1;
                    //UniLog.Log(node.ToString());
                    IAvatarObject comp = item.Item2;
                    Slot slot = comp.Slot;
                    //READ transform

                    //Scale stream
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    slot.LocalScale = new float3(x, y, z);
                    //Position stream
                    x = reader.ReadSingle();
                    y = reader.ReadSingle();
                    z = reader.ReadSingle();
                    slot.LocalPosition = new float3(x, y, z);
                    //Rotation stream
                    x = reader.ReadSingle();
                    y = reader.ReadSingle();
                    z = reader.ReadSingle();
                    float w = reader.ReadSingle();
                    slot.LocalRotation = new floatQ(x, y, z, w);
                    //UniLog.Log(x.ToString());
                    //UniLog.Log(y.ToString());
                    //UniLog.Log(z.ToString());
                    //UniLog.Log(w.ToString());
                }
            }

        }
        public void StartPlaying()
        {

        }
        public void StopPlaying()
        {
            foreach (var item in output_fss)
            {
                item.Value.Close();
            }
            output_fss = new Dictionary<RefID, FileStream>();
            output_readers = new Dictionary<RefID, BitBinaryReaderX>();
        }
    }
}
