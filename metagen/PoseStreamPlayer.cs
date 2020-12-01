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
        //public Dictionary<RefID, List<Tuple<BodyNode,IAvatarObject>>> avatar_pose_nodes = new Dictionary<RefID, List<Tuple<BodyNode,IAvatarObject>>>();
        public Dictionary<RefID, List<Tuple<BodyNode,AvatarObjectSlot>>> fake_proxies = new Dictionary<RefID, List<Tuple<BodyNode,AvatarObjectSlot>>>();
        public Dictionary<RefID, Dictionary<BodyNode,Tuple<bool,bool,bool>>> avatar_stream_channels = new Dictionary<RefID, Dictionary<BodyNode,Tuple<bool,bool,bool>>>();
        public Dictionary<RefID, Slot> avatars = new Dictionary<RefID, Slot>();
        metagen.AvatarManager avatarManager;
        Task avatar_loading_task;
        bool avatars_finished_loading = false;
        //TODO
        public PoseStreamPlayer()
        {
            avatarManager = new metagen.AvatarManager();
        }
        public void PlayStreams()
        {
            if (!avatars_finished_loading) return;
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            currentWorld.RunSynchronously(() =>
            {
                //TODO: Need this function to not depend on users being present! Just gave the info from the recorded data!!
                Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
                foreach (User user in users)
                {
                    RefID user_id = user.ReferenceID;

                    //Decode the streams
                    BinaryReaderX reader = output_readers[user_id];

                    //READ deltaT
                    float deltaT = reader.ReadSingle();
                    foreach (var item in fake_proxies[user_id])
                    {
                        BodyNode node = item.Item1;
                        var available_streams = avatar_stream_channels[user_id][node];
                        AvatarObjectSlot comp = item.Item2;
                        Slot slot = comp.Slot;

                        //READ transform
                        float x, y, z, w;
                        //Scale stream
                        if (available_streams.Item1)
                        {
                            x = reader.ReadSingle();
                            y = reader.ReadSingle();
                            z = reader.ReadSingle();
                            slot.LocalScale = new float3(x, y, z);
                        }
                        //Position stream
                        if (available_streams.Item2)
                        {
                            x = reader.ReadSingle();
                            y = reader.ReadSingle();
                            z = reader.ReadSingle();
                            slot.LocalPosition = new float3(x, y, z);
                        }
                        //Rotation stream
                        if (available_streams.Item3)
                        {
                            x = reader.ReadSingle();
                            y = reader.ReadSingle();
                            z = reader.ReadSingle();
                            w = reader.ReadSingle();
                            slot.LocalRotation = new floatQ(x, y, z, w);
                        }
                    }
                }

            });



        }
        public void StartPlaying()
        {

            avatar_loading_task = Task.Run(StartPlayingInternal);

        }
        private async void StartPlayingInternal()
        {
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                //if (!output_readers.ContainsKey(user_id))
                //{
                output_fss[user_id] = new FileStream(user_id.ToString() + "_streams.dat", FileMode.Open, FileAccess.Read);
                BitReaderStream bitstream = new BitReaderStream(output_fss[user_id]);
                output_readers[user_id] = new BitBinaryReaderX(bitstream);
                fake_proxies[user_id] = new List<Tuple<BodyNode, AvatarObjectSlot>>();
                avatar_stream_channels[user_id] = new Dictionary<BodyNode, Tuple<bool, bool, bool>>();
                Slot avatar = await avatarManager.GetAvatar();
                UniLog.Log("AVATAR");
                UniLog.Log(avatar.ToString());
                avatars[user_id] = avatar;
                List<IAvatarObject> components = avatar.GetComponentsInChildren<IAvatarObject>();

                //READ absolute time
                output_readers[user_id].ReadSingle();
                //READ number of body nodes
                int numBodyNodes = output_readers[user_id].ReadInt32();
                for (int i = 0; i < numBodyNodes; i++)
                {
                    //READ body node type
                    int nodeInt = output_readers[user_id].ReadInt32();
                    //READ if scale stream exists
                    bool scale_exists = output_readers[user_id].ReadBoolean();
                    //READ if position stream exists
                    bool pos_exists = output_readers[user_id].ReadBoolean();
                    //READ if rotation stream exists
                    bool rot_exists = output_readers[user_id].ReadBoolean();
                    BodyNode bodyNodeType = (BodyNode)nodeInt;

                    bool node_found = false;
                    foreach (IAvatarObject comp in components)
                    {
                        UniLog.Log(comp.Name);
                        if (comp.Node == bodyNodeType)
                        {
                            AvatarObjectSlot connected_comp = comp.EquippingSlot;
                            fake_proxies[user_id].Add(new Tuple<BodyNode, AvatarObjectSlot>(bodyNodeType, connected_comp));
                            //avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(bodyNodeType, comp));
                            node_found = true;
                            break;
                        }
                    }
                    if (!node_found) throw new Exception("Node " + bodyNodeType.ToString() + " not found in avatar!");
                    avatar_stream_channels[user_id][bodyNodeType] = new Tuple<bool, bool, bool>(scale_exists,pos_exists,rot_exists);
                }
                //}
            }
            avatars_finished_loading = true;

        }
        public void StopPlaying()
        {
            foreach (var item in output_fss)
            {
                item.Value.Close();
            }
            output_fss = new Dictionary<RefID, FileStream>();
            output_readers = new Dictionary<RefID, BitBinaryReaderX>();
            fake_proxies = new Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>>();
            avatar_stream_channels = new Dictionary<RefID, Dictionary<BodyNode, Tuple<bool, bool, bool>>>();
        }
    }
}
