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
using System.Reflection;
using FrooxEngine.FinalIK;
using Grpc.Core;
using NeosAnimationToolset;

namespace metagen
{
    class GrpcPlayer : IPlayer
    {
        private DateTime utcNow;
        public Dictionary<RefID, IAsyncStreamReader<Bones>> output_readers = new Dictionary<RefID, IAsyncStreamReader<Bones>>();
        //public Dictionary<RefID, List<Tuple<BodyNode,IAvatarObject>>> avatar_pose_nodes = new Dictionary<RefID, List<Tuple<BodyNode,IAvatarObject>>>();
        public Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>> fake_proxies = new Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>>();
        public Dictionary<RefID, List<Tuple<BodyNode, IAvatarObject>>> avatar_pose_nodes = new Dictionary<RefID, List<Tuple<BodyNode, IAvatarObject>>>();
        public Dictionary<RefID, Dictionary<BodyNode, Tuple<bool, bool, bool>>> avatar_stream_channels = new Dictionary<RefID, Dictionary<BodyNode, Tuple<bool, bool, bool>>>();
        //public Dictionary<RefID, FingerPlayerSource> finger_sources = new Dictionary<RefID, FingerPlayerSource>();
        //public Dictionary<RefID, Dictionary<BodyNode, RelayRef<IValue<floatQ>>>> finger_rotations = new Dictionary<RefID, Dictionary<BodyNode, RelayRef<IValue<floatQ>>>>();
        public Dictionary<RefID, Dictionary<BodyNode, Slot>> finger_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
        public Dictionary<RefID, Dictionary<Chirality, HandPoser>> hand_posers = new Dictionary<RefID, Dictionary<Chirality, HandPoser>>();
        public Dictionary<RefID, Dictionary<BodyNode, floatQ>> finger_compensations = new Dictionary<RefID, Dictionary<BodyNode, floatQ>>();
        public Dictionary<RefID, Slot> avatars = new Dictionary<RefID, Slot>();
        public Dictionary<RefID, bool> hands_are_tracked = new Dictionary<RefID, bool>();
        public Dictionary<RefID, Dictionary<BodyNode, Slot>> proxy_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
        public Dictionary<RefID, List<Slot>> boness = new Dictionary<RefID, List<Slot>>();
        public int recording_index = 0;
        public bool play_voice = false;
        public bool play_hearing = true;
        public Slot avatar_template = null;
        List<RefID> user_ids = new List<RefID>();
        metagen.AvatarManager avatarManager;
        Task avatar_loading_task;
        bool avatars_finished_loading = false;
        World World;
        public bool isPlaying { get; set; }
        RecordingTool animationRecorder;
        public bool generateAnimation = false;
        DataManager dataManager;
        MetaGen metagen_comp;
        private DataComm.DataCommClient client;
        private Channel channel;

        //TODO
        public GrpcPlayer(DataManager dataMan, MetaGen component)
        {
            dataManager = dataMan;
            metagen_comp = component;
            World = component.World;
        }

        public void PlayStreams()
        {
            //metagen_comp.StartTask(PlayStreamsInternal);
            PlayStreamsInternal();
        }

        public void PlayStreamsInternal()
        {
            if (!avatars_finished_loading) return;
            World currentWorld = metagen_comp.World;
            //currentWorld.RunSynchronously(() =>
            //{
            try
            {
                foreach (RefID user_id in user_ids)
                {
                    //Decode the streams
                    Task readTask = output_readers[user_id].MoveNext();
                    if (Task.WhenAny(readTask, Task.Delay(2000)).Result != readTask) continue;
                    IEnumerator<float> reader = output_readers[user_id].Current.Transforms.GetEnumerator();
                    //UniLog.Log(output_readers[user_id].Current.Transforms);
                    //await default(ToWorld);

                    //READ deltaT
                    //float deltaT = reader.ReadSingle();
                    int node_index = 0;
                    //foreach (var item in fake_proxies[user_id])
                    foreach (var item in avatar_pose_nodes[user_id])
                    {
                        BodyNode node = item.Item1;
                        var available_streams = avatar_stream_channels[user_id][node];
                        //AvatarObjectSlot comp = item.Item2;
                        AvatarObjectSlot avatarObject = fake_proxies[user_id][node_index].Item2;
                        IAvatarObject comp = item.Item2;
                        Slot slot = comp != null ? comp.Slot : null;
                        if (node == BodyNode.Root)
                        {
                            slot = avatarObject.Slot;
                        }

                        //READ transform
                        float x, y, z, w;
                        //Scale stream
                        if (available_streams.Item1)
                        {
                            reader.MoveNext();
                            x = reader.Current;
                            reader.MoveNext();
                            y = reader.Current;
                            reader.MoveNext();
                            z = reader.Current;
                            reader.MoveNext();
                            float3 scale = new float3(x, y, z);
                            if (slot != null)
                            {
                                //scale = avatarObject.Slot.Parent.LocalScaleToSpace(scale, slot.Parent);
                                //slot.LocalScale = scale;
                                //slot.GlobalScale = scale;
                            }
                            //UniLog.Log(slot.LocalScale.ToString());
                        }
                        //Position stream
                        if (available_streams.Item2)
                        {
                            x = reader.Current;
                            reader.MoveNext();
                            y = reader.Current;
                            reader.MoveNext();
                            z = reader.Current;
                            reader.MoveNext();
                            float3 position = new float3(x, y, z);
                            //UniLog.Log(position.x);
                            //UniLog.Log(position.y);
                            //UniLog.Log(position.z);
                            if (slot != null)
                            {
                                //position = avatarObject.Slot.Parent.LocalPointToSpace(position, slot.Parent);
                                //slot.LocalPosition = position;
                                slot.GlobalPosition = position;
                            }
                            //UniLog.Log(slot.LocalPosition.ToString());
                        }
                        //Rotation stream
                        if (available_streams.Item3)
                        {
                            x = reader.Current;
                            reader.MoveNext();
                            y = reader.Current;
                            reader.MoveNext();
                            z = reader.Current;
                            reader.MoveNext();
                            w = reader.Current;
                            floatQ rotation = new floatQ(x, y, z, w);
                            if (slot != null)
                            {
                                //rotation = avatarObject.Slot.Parent.LocalRotationToSpace(rotation, slot.Parent);
                                //rotation = avatarObject.Slot.Parent.GlobalRotationToLocal(rotation);
                                //slot.LocalRotation = rotation;
                                slot.GlobalRotation = rotation;
                            }
                            //UniLog.Log(slot.LocalRotation.ToString());
                        }
                        node_index++;
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
                            //bool was_succesful = reader.ReadBoolean();
                            x = reader.Current;
                            reader.MoveNext();
                            y = reader.Current;
                            reader.MoveNext();
                            z = reader.Current;
                            reader.MoveNext();
                            w = reader.Current;
                            reader.MoveNext();
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
                            //bool was_succesful = reader.ReadBoolean();
                            x = reader.Current;
                            reader.MoveNext();
                            y = reader.Current;
                            reader.MoveNext();
                            z = reader.Current;
                            reader.MoveNext();
                            w = reader.Current;
                            reader.MoveNext();
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
                    //await default(ToBackground);
                }
                if (generateAnimation)
                {
                    animationRecorder.RecordFrame();
                }
            }
            catch (Exception e)
            {
                UniLog.Log("OwO: " + e.Message);
                //this.StopPlaying();
                metagen_comp.StopPlaying();
            }
            //});



        }

        public void StartPlaying()
        {
            StartPlaying(null);
        }

        public void StartPlaying(Slot avatar_template = null)
        {
            avatar_loading_task = Task.Run(StartPlayingInternal);
            this.play_voice = metagen_comp.play_voice;
            this.play_hearing = metagen_comp.play_hearing;
            this.avatar_template = avatar_template;
        }
        private async void StartPlayingInternal()
        {
            try
            {
                if (generateAnimation)
                {
                    metagen_comp.World.RunSynchronously(() =>
                    {
                        animationRecorder = metagen_comp.Slot.AttachComponent<RecordingTool>();
                        animationRecorder.metagen_comp = metagen_comp;
                    });
                }
                //Dictionary<RefID, User>.ValueCollection users = metagen_comp.World.AllUsers;
                avatarManager = new metagen.AvatarManager();
                List<UserMetadata> userMetadatas = new List<UserMetadata>();
                userMetadatas.Add(new UserMetadata { userId = "U-test", bodyNodes = "", devices = "", headDevice = "", isPublic = true, isRecording = true, platform = "", userRefId = "ID2B00" });
                foreach (UserMetadata user in userMetadatas)
                {
                    if (!user.isRecording || !user.isPublic) continue; //at the moment we only allow playing back of public recording, for privacy reasons. In the future, we'll allow private access to the data
                    RefID user_id = RefID.Parse(user.userRefId);
                    UniLog.Log(user_id.ToString());
                    user_ids.Add(user_id);
                    channel = new Channel("127.0.0.1:" + (40052).ToString(), ChannelCredentials.Insecure);
                    client = new DataComm.DataCommClient(channel);

                    output_readers[user_id] = client.GetPose(new Empty()).ResponseStream;
                    fake_proxies[user_id] = new List<Tuple<BodyNode, AvatarObjectSlot>>();
                    avatar_pose_nodes[user_id] = new List<Tuple<BodyNode, IAvatarObject>>();
                    avatar_stream_channels[user_id] = new Dictionary<BodyNode, Tuple<bool, bool, bool>>();
                    proxy_slots[user_id] = new Dictionary<BodyNode, Slot>();
                    if (avatarManager.avatar_template == null && avatar_template != null)
                    {
                        avatarManager.avatar_template = avatar_template;
                    }
                    Slot avatar = await avatarManager.GetAvatar();
                    UniLog.Log("AVATAR");
                    UniLog.Log(avatar.ToString());
                    avatars[user_id] = avatar;
                    List<IAvatarObject> components = avatar.GetComponentsInChildren<IAvatarObject>();
                    List<AvatarObjectSlot> root_comps = avatar.GetComponentsInChildren<AvatarObjectSlot>();
                    boness[user_id] = avatar.GetComponentInChildren<Rig>()?.Bones.ToList();
                    VRIKAvatar avatarIK = avatar.GetComponentInChildren<VRIKAvatar>();

                    //READ absolute time
                    //output_readers[user_id].ReadSingle();
                    //READ number of body nodes
                    int numBodyNodes = 28; //TODO CHECK
                    for (int i = 0; i < numBodyNodes; i++)
                    {
                        //READ body node type
                        //int nodeInt = 
                        //READ if scale stream exists
                        bool scale_exists = true;
                        //READ if position stream exists
                        bool pos_exists = true;
                        //READ if rotation stream exists
                        bool rot_exists = true;
                        BodyNode bodyNodeType = VNetcBodyNodeConverter[i];

                        bool node_found = false;
                        foreach (IAvatarObject comp in components)
                        {
                            foreach (AvatarObjectSlot comp2 in root_comps)
                            {
                                if (comp.Node == bodyNodeType && comp2.Node == bodyNodeType)
                                {
                                    UniLog.Log((comp.Node, scale_exists, pos_exists, rot_exists));
                                    if (bodyNodeType == BodyNode.Root)
                                    {
                                        proxy_slots[user_id][bodyNodeType] = avatar;
                                    }
                                    else
                                    {
                                        proxy_slots[user_id][bodyNodeType] = comp.Slot;
                                    }
                                    comp.Equip(comp2);
                                    if (bodyNodeType != BodyNode.Root)
                                    {
                                        SyncRef<Slot> sourceField = (SyncRef<Slot>)comp.GetType().GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(comp);
                                        sourceField.Target = null;
                                        FieldDrive<float3> posField = (FieldDrive<float3>)comp.GetType().GetField("_position", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(comp);
                                        posField.Target = null;
                                        FieldDrive<floatQ> rotField = (FieldDrive<floatQ>)comp.GetType().GetField("_rotation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(comp);
                                        rotField.Target = null;
                                    }
                                    fake_proxies[user_id].Add(new Tuple<BodyNode, AvatarObjectSlot>(bodyNodeType, comp2));
                                    avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(comp.Node, comp));
                                    comp2.IsTracking.Value = true;
                                    if (bodyNodeType == BodyNode.LeftFoot || bodyNodeType == BodyNode.RightFoot)
                                    {
                                        avatarIK.ForceUseFeetProxies.Value = true;
                                    }
                                    if (bodyNodeType == BodyNode.LeftLowerLeg || bodyNodeType == BodyNode.RightLowerLeg)
                                    {
                                        avatarIK.ForceUseKneeProxies.Value = true;
                                    }
                                    if (bodyNodeType == BodyNode.LeftLowerArm || bodyNodeType == BodyNode.RightLowerArm)
                                    {
                                        avatarIK.ForceUseElbowProxies.Value = true;
                                    }
                                    if (bodyNodeType == BodyNode.Chest)
                                    {
                                        avatarIK.ForceUseChestProxy.Value = true;
                                    }
                                    if (bodyNodeType == BodyNode.Hips)
                                    {
                                        avatarIK.ForceUsePelvisProxy.Value = true;
                                    }
                                    node_found = true;
                                    break;
                                }
                                if (node_found) break;
                            }
                        }
                        //if (!node_found) throw new Exception("Node " + bodyNodeType.ToString() + " not found in avatar!");
                        if (!node_found)
                        {
                            fake_proxies[user_id].Add(new Tuple<BodyNode, AvatarObjectSlot>(bodyNodeType, null));
                            avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(bodyNodeType, null));
                        }
                        avatar_stream_channels[user_id][bodyNodeType] = new Tuple<bool, bool, bool>(scale_exists, pos_exists, rot_exists);
                    }
                    Slot avatarRootSlot = avatar.GetComponentInChildren<AvatarRoot>()?.Slot;
                    if (avatarRootSlot != null)
                    {
                        avatarRootSlot.LocalPosition = new float3(0, 0, 0);
                        avatarRootSlot.LocalRotation = new floatQ(0, 0, 0, 1);
                    }
                    //READ whether hands are being tracked
                    hands_are_tracked[user_id] = false;
                    //READ whether metacarpals are being tracked
                    //output_readers[user_id].ReadBoolean();

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
                            if (fingerSegment != null && fingerSegment.Root.Target != null)//&& fingerSegment.RotationDrive.IsLinkValid)
                            {
                                UniLog.Log(nodee.ToString());
                                finger_slots[user_id][nodee] = fingerSegment.Root.Target;
                                proxy_slots[user_id][nodee] = fingerSegment.Root.Target;
                                finger_compensations[user_id][nodee] = fingerSegment.CoordinateCompensation.Value;
                                fingerSegment.RotationDrive.Target = (IField<floatQ>)null;
                            }
                        }
                    }
                    UniLog.Log("got finger rotation vars");
                    //AUDIO PLAY
                    //UniLog.Log("Setting up audio!");
                    //avatar.GetComponentInChildren<AudioOutput>().Source.Target = null;
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    string audio_file;
                    //    if (i==0)
                    //    {
                    //        if (!play_hearing) continue;
                    //        string[] files = Directory.GetFiles(reading_directory, user_id.ToString() + "*_hearing.ogg");
                    //        audio_file = files.Length > 0 ? files[0] : null;
                    //    } else
                    //    {
                    //        if (!play_voice) continue;
                    //        string[] files = Directory.GetFiles(reading_directory, user_id.ToString() + "*_voice.ogg");
                    //        audio_file = files.Length > 0 ? files[0] : null;
                    //    }
                    //    if (File.Exists(audio_file))
                    //    {
                    //        AudioOutput audio_output = avatar.GetComponentInChildren<AudioOutput>();
                    //        if (audio_output.Source.Target != null) audio_output = audio_output.Slot.AttachComponent<AudioOutput>();
                    //        VisemeAnalyzer visemeAnalyzer = avatar.GetComponentInChildren<VisemeAnalyzer>();
                    //        audio_output.Volume.Value = 1f;
                    //        audio_output.Enabled = true;
                    //        //audio_outputs[user_id] = audio_output;
                    //        //AudioX audioData = new AudioX(reading_directory + "/" + user_id.ToString() + "_audio.wav");
                    //        //AssetRef<AudioClip> audioClip = new AssetRef<AudioClip>();
                    //        Uri uri = this.World.Engine.LocalDB.ImportLocalAsset(audio_file, LocalDB.ImportLocation.Original, (string)null);
                    //        //ToWorld thing = new ToWorld();
                    //        //var awaiter = thing.GetAwaiter();
                    //        //awaiter.GetResult();
                    //        StaticAudioClip audioClip = audio_output.Slot.AttachAudioClip(uri);
                    //        AudioClipPlayer player = audio_output.Slot.AttachComponent<AudioClipPlayer>();
                    //        if (visemeAnalyzer != null)
                    //        {
                    //            visemeAnalyzer.Source.Target = player;
                    //        }
                    //        UniLog.Log("attaching clip to player");
                    //        player.Clip.Target = (IAssetProvider<AudioClip>) audioClip;
                    //        UniLog.Log("attaching player to audio output");
                    //        audio_output.Source.Target = (IAudioSource) player;
                    //        audio_output.Slot.AttachComponent<AudioMetadata>(true, (Action<AudioMetadata>)null).SetFromCurrentWorld();
                    //        player.Play();
                    //    }
                    //}
                }
                avatars_finished_loading = true;
                isPlaying = true;
                if (generateAnimation)
                {
                    animationRecorder.StartRecordingAvatars(avatars, audio_outputs);
                }
            }
            catch (Exception e)
            {
                UniLog.Log("TwT: " + e.Message);
                UniLog.Log(e.StackTrace);
            }
        }
        public void StopPlaying()
        {
            Task.Run(async () => await channel.ShutdownAsync());
            output_readers = new Dictionary<RefID, IAsyncStreamReader<Bones>>();
            fake_proxies = new Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>>();
            avatar_stream_channels = new Dictionary<RefID, Dictionary<BodyNode, Tuple<bool, bool, bool>>>();
            hands_are_tracked = new Dictionary<RefID, bool>();
            proxy_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
            user_ids = new List<RefID>();
            avatarManager.avatar_template = null;
            avatarManager.has_prepared_avatar = false;
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
        public BodyNode[] VNetcBodyNodeConverter = new BodyNode[28] {
            BodyNode.RightUpperArm,
            BodyNode.RightLowerArm,
            BodyNode.RightHand,
            BodyNode.RightThumb_Proximal,
            BodyNode.RightMiddleFinger_Proximal,
            BodyNode.LeftUpperArm,
            BodyNode.LeftLowerArm,
            BodyNode.LeftHand,
            BodyNode.LeftThumb_Proximal,
            BodyNode.LeftMiddleFinger_Proximal,
            BodyNode.NONE,
            BodyNode.LeftEye,
            BodyNode.NONE,
            BodyNode.RightEye,
            BodyNode.NONE,
            BodyNode.RightUpperLeg,
            BodyNode.RightLowerLeg,
            BodyNode.RightFoot,
            BodyNode.RightToes,
            BodyNode.LeftUpperLeg,
            BodyNode.LeftLowerLeg,
            BodyNode.LeftFoot,
            BodyNode.LeftToes,
            BodyNode.Spine,
            BodyNode.Hips,
            BodyNode.Head,
            BodyNode.Neck,
            BodyNode.Spine,
            };
    }
}
