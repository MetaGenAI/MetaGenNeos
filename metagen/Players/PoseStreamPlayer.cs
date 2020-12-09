using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using CodeX;
using System.IO;
using FrooxEngine.CommonAvatar;
using CsvHelper;
using System.Globalization;

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
        //public Dictionary<RefID, FingerPlayerSource> finger_sources = new Dictionary<RefID, FingerPlayerSource>();
        //public Dictionary<RefID, Dictionary<BodyNode, RelayRef<IValue<floatQ>>>> finger_rotations = new Dictionary<RefID, Dictionary<BodyNode, RelayRef<IValue<floatQ>>>>();
        public Dictionary<RefID, Dictionary<BodyNode, Slot>> finger_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
        public Dictionary<RefID, Dictionary<Chirality, HandPoser>> hand_posers = new Dictionary<RefID, Dictionary<Chirality, HandPoser>>();
        public Dictionary<RefID, Dictionary<BodyNode, floatQ>> finger_compensations = new Dictionary<RefID, Dictionary<BodyNode, floatQ>>();
        public Dictionary<RefID, Slot> avatars = new Dictionary<RefID, Slot>();
        public Dictionary<RefID, bool> hands_are_tracked = new Dictionary<RefID, bool>();
        List<RefID> user_ids = new List<RefID>();
        metagen.AvatarManager avatarManager;
        Task avatar_loading_task;
        bool avatars_finished_loading = false;
        World World;
        public bool isPlaying;
        DataManager dataManager;
        MetaGen metagen_comp;
        //TODO
        public PoseStreamPlayer(DataManager dataMan, MetaGen component)
        {
            dataManager = dataMan;
            metagen_comp = component;
            World = component.World;
        }
        public void PlayStreams()
        {
            if (!avatars_finished_loading) return;
            World currentWorld = metagen_comp.World;
            //currentWorld.RunSynchronously(() =>
            //{
                //TODO: Need this function to not depend on users being present! Just gave the info from the recorded data!!
                try
                {
                    foreach (RefID user_id in user_ids)
                    {
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
                                //UniLog.Log(slot.LocalScale.ToString());
                            }
                            //Position stream
                            if (available_streams.Item2)
                            {
                                x = reader.ReadSingle();
                                y = reader.ReadSingle();
                                z = reader.ReadSingle();
                                slot.LocalPosition = new float3(x, y, z);
                                //UniLog.Log(slot.LocalPosition.ToString());
                            }
                            //Rotation stream
                            if (available_streams.Item3)
                            {
                                x = reader.ReadSingle();
                                y = reader.ReadSingle();
                                z = reader.ReadSingle();
                                w = reader.ReadSingle();
                                slot.LocalRotation = new floatQ(x, y, z, w);
                                //UniLog.Log(slot.LocalRotation.ToString());
                            }
                        }
                        //READ finger pose
                        var finger_slot = finger_slots[user_id];
                        if (hands_are_tracked[user_id])
                        {
                        //UniLog.Log("UPDATING HANDS");
                            //FingerPlayerSource finger_source = finger_sources[user_id];
                            float x, y, z, w;
                            //Left Hand
                            HandPoser hand_poser = hand_posers[user_id][Chirality.Left];
                            floatQ lookRot = floatQ.LookRotation(hand_poser.HandForward, hand_poser.HandUp);
                            for (int index = 0; index < FingerPoseStreamManager.FINGER_NODE_COUNT; ++index)
                            {
                                BodyNode node = (BodyNode)(18 + index);
                                //READ whether finger data was obtained
                                bool was_succesful = reader.ReadBoolean();
                                x = reader.ReadSingle();
                                y = reader.ReadSingle();
                                z = reader.ReadSingle();
                                w = reader.ReadSingle();
                                //finger_source.UpdateFingerPose(node, new floatQ(x, y, z, w));
                                UniLog.Log(x);
                                UniLog.Log(y);
                                UniLog.Log(z);
                                UniLog.Log(w);
                                if (finger_slot.ContainsKey(node))
                                {
                                    floatQ rot = new floatQ(x, y, z, w);
                                    rot = lookRot * rot;
                                    Slot root = hand_poser.HandRoot.Target ?? hand_poser.Slot;
                                    rot = finger_slot[node].Parent.SpaceRotationToLocal(rot, root);
                                    rot = rot * finger_compensations[user_id][node];
                                    finger_slot[node].LocalRotation = rot;
                                }
                            }
                            //Right Hand
                            hand_poser = hand_posers[user_id][Chirality.Right];
                            lookRot = floatQ.LookRotation(hand_poser.HandForward, hand_poser.HandUp);
                            for (int index = 0; index < FingerPoseStreamManager.FINGER_NODE_COUNT; ++index)
                            {
                                BodyNode node = (BodyNode)(47 + index);
                                //READ whether finger data was obtained
                                bool was_succesful = reader.ReadBoolean();
                                x = reader.ReadSingle();
                                y = reader.ReadSingle();
                                z = reader.ReadSingle();
                                w = reader.ReadSingle();
                                //finger_source.UpdateFingerPose(node, new floatQ(x, y, z, w));
                                if (finger_slot.ContainsKey(node))
                                {
                                    floatQ rot = new floatQ(x, y, z, w);
                                    rot = lookRot * rot;
                                    Slot root = hand_poser.HandRoot.Target ?? hand_poser.Slot;
                                    rot = finger_slot[node].Parent.SpaceRotationToLocal(rot, root);
                                    rot = rot * finger_compensations[user_id][node];
                                    finger_slot[node].LocalRotation = rot;
                                }
                            }
                        }
                    }
                } catch (Exception e)
                {
                UniLog.Log(e.Message);
                    StopPlaying();
                }
            //});



        }
        public void StartPlaying()
        {

            avatar_loading_task = Task.Run(StartPlayingInternal);
        }
        private async void StartPlayingInternal()
        {
            try
            {
                //Dictionary<RefID, User>.ValueCollection users = metagen_comp.World.AllUsers;
                avatarManager = new metagen.AvatarManager();
                string reading_directory = dataManager.LastRecordingForWorld(metagen_comp.World);
                if (reading_directory == null) return;
                List<UserMetadata> userMetadatas;
                using (var reader = new StreamReader(reading_directory + "/user_metadata.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    userMetadatas = csv.GetRecords<UserMetadata>().ToList();
                }
                foreach (UserMetadata user in userMetadatas)
                {
                    RefID user_id = RefID.Parse(user.userRefId);
                    UniLog.Log(user_id.ToString());
                    user_ids.Add(user_id);
                    output_fss[user_id] = new FileStream(reading_directory + "/" + user_id.ToString() + "_streams.dat", FileMode.Open, FileAccess.Read);
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
                        avatar_stream_channels[user_id][bodyNodeType] = new Tuple<bool, bool, bool>(scale_exists, pos_exists, rot_exists);
                    }
                    //READ whether hands are being tracked
                    hands_are_tracked[user_id] = output_readers[user_id].ReadBoolean();
                    //READ whether metacarpals are being tracked
                    output_readers[user_id].ReadBoolean();
                    //finger_sources[user_id] = avatar.GetComponentInChildren<FingerPlayerSource>(null, true);
                    List<HandPoser> these_hand_posers = avatar.GetComponentsInChildren<HandPoser>(null, excludeDisabled: false, includeLocal: false);
                    UniLog.Log("getting finger rotation vars");
                    finger_slots[user_id] = new Dictionary<BodyNode, Slot>();
                    hand_posers[user_id] = new Dictionary<Chirality, HandPoser>();
                    finger_compensations[user_id] = new Dictionary<BodyNode, floatQ>();
                    foreach (HandPoser hand_poser in these_hand_posers)
                    {
                        UniLog.Log("HI");
                        hand_posers[user_id][hand_poser.Side] = hand_poser;
                        BodyNode side1 = BodyNode.LeftThumb_Metacarpal.GetSide((Chirality)hand_poser.Side);
                        BodyNode side2 = BodyNode.LeftPinky_Tip.GetSide((Chirality)hand_poser.Side);
                        for (BodyNode nodee = side1; nodee <= side2; ++nodee)
                        {
                            int index = nodee - side1;
                            FingerType fingerType = nodee.GetFingerType();
                            FingerSegmentType fingerSegmentType = nodee.GetFingerSegmentType();
                            HandPoser.FingerSegment fingerSegment = hand_poser[fingerType][fingerSegmentType];
                            //UniLog.Log(fingerSegment == null ? "null" : fingerSegment.RotationDrive.IsLinkValid.ToString());
                            //UniLog.Log(fingerSegment == null ? "null" : this.World.CorrespondingWorldId);
                            if (fingerSegment != null && fingerSegment.Root.Target != null)//&& fingerSegment.RotationDrive.IsLinkValid)
                            {
                                UniLog.Log(nodee.ToString());
                                //fingerSegment.RotationDrive.Target.ReleaseLink(fingerSegment.RotationDrive.Target.DirectLink);
                                finger_slots[user_id][nodee] = fingerSegment.Root.Target;
                                finger_compensations[user_id][nodee] = fingerSegment.CoordinateCompensation.Value;
                                //finger_rotations[user_id][nodee] = new RelayRef<IValue<floatQ>>();
                                //finger_rotations[user_id][nodee].TrySet(fingerSegment.Root.Target);
                                //UniLog.Log(fingerSegment.RotationDrive.Target.Parent.ToString());
                                fingerSegment.RotationDrive.Target = (IField<floatQ>) null;
                                //fingerSegment.RotationDrive.ReleaseLink();
                                //fingerSegment.RotationDrive.Target.Value.
                                //fingerSegment.RotationDrive.Target = null;
                            }
                        }
                    }
                    UniLog.Log("got finger rotation vars");
                    UniLog.Log("Setting up audio!");
                    AudioOutput audio_output = avatar.GetComponentInChildren<AudioOutput>();
                    audio_output.Volume.Value = 1f;
                    audio_output.Enabled = true;
                    //audio_outputs[user_id] = audio_output;
                    //AudioX audioData = new AudioX(reading_directory + "/" + user_id.ToString() + "_audio.wav");
                    //AssetRef<AudioClip> audioClip = new AssetRef<AudioClip>();
                    Uri uri = this.World.Engine.LocalDB.ImportLocalAsset(reading_directory + "/" + user_id.ToString() + "_audio.wav", LocalDB.ImportLocation.Original, (string)null);
                    //ToWorld thing = new ToWorld();
                    //var awaiter = thing.GetAwaiter();
                    //awaiter.GetResult();
                    StaticAudioClip audioClip = audio_output.Slot.AttachAudioClip(uri);
                    AudioClipPlayer player = audio_output.Slot.AttachComponent<AudioClipPlayer>();
                    UniLog.Log("attaching clip to player");
                    player.Clip.Target = (IAssetProvider<AudioClip>) audioClip;
                    UniLog.Log("attaching player to audio output");
                    audio_output.Source.Target = (IAudioSource) player;
                    audio_output.Slot.AttachComponent<AudioMetadata>(true, (Action<AudioMetadata>)null).SetFromCurrentWorld();
                    //TODO: refactor this stuff
                    player.Play();
                }
                avatars_finished_loading = true;
                isPlaying = true;
            } catch (Exception e)
            {
                UniLog.Log("TwT: " + e.Message);
            }
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
            hands_are_tracked = new Dictionary<RefID, bool>();
            user_ids = new List<RefID>();
            avatarManager.avatar_template = null;
            foreach (var item in avatars)
            {
                Slot slot = item.Value;
                slot.Destroy();

            }
            avatars = new Dictionary<RefID, Slot>();
            finger_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
            hand_posers = new Dictionary<RefID, Dictionary<Chirality, HandPoser>>();
            finger_compensations = new Dictionary<RefID, Dictionary<BodyNode, floatQ>>();
            isPlaying = false;
        }
    }
}
