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
using NeosAnimationToolset;
using RefID = BaseX.RefID;

namespace metagen
{
    class UnifiedPayer : IPlayer
    {
        private DateTime utcNow;
        public Dictionary<RefID, FileStream> output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, BitBinaryReaderX> output_readers = new Dictionary<RefID, BitBinaryReaderX>();
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

        public bool generateAnimation = false;
        public bool generateBvh = false;
        DataManager dataManager;
        MetaGen metagen_comp;
        RecordingTool animationRecorder;
        BvhRecorder bvhRecorder;
        //TODO
        public UnifiedPayer(DataManager dataMan, MetaGen component)
        {
            dataManager = dataMan;
            metagen_comp = component;
            World = component.World;
            bvhRecorder = new BvhRecorder(metagen_comp);
        }
        public void PlayStreams()
        {
            if (!avatars_finished_loading) return;
            //currentWorld.RunSynchronously(() =>
            //{
            try
            {
                foreach (RefID user_id in user_ids)
                {
                    //Decode the streams
                    BinaryReaderX reader = output_readers[user_id];

                    //READ deltaT
                    float deltaT = reader.ReadSingle();
                    int node_index = 0;
                    //foreach (var item in fake_proxies[user_id])
                    foreach (var item in avatar_pose_nodes[user_id])
                    {
                        BodyNode node = item.Item1;
                        var available_streams = avatar_stream_channels[user_id][node];
                        //AvatarObjectSlot comp = item.Item2;
                        AvatarObjectSlot avatarObject = fake_proxies[user_id][node_index].Item2;
                        IAvatarObject comp = item.Item2;
                        Slot slot = comp.Slot;
                        if (node == BodyNode.Root)
                        {
                            slot = avatarObject.Slot;
                        }

                        //UniLog.Log(slot);
                        //READ transform
                        float x, y, z, w;
                        //Scale stream
                        if (available_streams.Item1)
                        {
                            x = reader.ReadSingle();
                            y = reader.ReadSingle();
                            z = reader.ReadSingle();
                            float3 scale = new float3(x, y, z);
                            scale = avatarObject.Slot.Parent.LocalScaleToSpace(scale, slot.Parent);
                            slot.LocalScale = scale;
                            //UniLog.Log(slot.LocalScale.ToString());
                        }
                        //Position stream
                        if (available_streams.Item2)
                        {
                            x = reader.ReadSingle();
                            y = reader.ReadSingle();
                            z = reader.ReadSingle();
                            float3 position = new float3(x, y, z);
                            position = avatarObject.Slot.Parent.LocalPointToSpace(position, slot.Parent);
                            slot.LocalPosition = position;
                            //UniLog.Log(slot.LocalPosition.ToString());
                        }
                        //Rotation stream
                        if (available_streams.Item3)
                        {
                            x = reader.ReadSingle();
                            y = reader.ReadSingle();
                            z = reader.ReadSingle();
                            w = reader.ReadSingle();
                            floatQ rotation = new floatQ(x, y, z, w);
                            rotation = avatarObject.Slot.Parent.LocalRotationToSpace(rotation, slot.Parent);
                            slot.LocalRotation = rotation;
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
                            bool was_succesful = reader.ReadBoolean();
                            x = reader.ReadSingle();
                            y = reader.ReadSingle();
                            z = reader.ReadSingle();
                            w = reader.ReadSingle();
                            //finger_source.UpdateFingerPose(node, new floatQ(x, y, z, w));
                            //UniLog.Log(x);
                            //UniLog.Log(y);
                            //UniLog.Log(z);
                            //UniLog.Log(w);
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
                if (generateAnimation)
                {
                    try
                    {
                        animationRecorder.RecordFrame();
                    } catch (Exception e)
                    {
                        UniLog.Log("Error at animation recording: " + e.Message);
                        UniLog.Log(e.StackTrace);
                    }
                }

                if (generateBvh)
                {
                    try
                    {
                        bvhRecorder.RecordFrame();
                    } catch (Exception e)
                    {
                        UniLog.Log("Error at Bvh recording: " + e.Message);
                        UniLog.Log(e.StackTrace);
                    }
                }
            } catch (Exception e)
            {
                UniLog.Log("OwO: " + e.Message);
                //this.StopPlaying();
                metagen_comp.StopPlaying();
            }
            //});



        }
        public void StartPlaying()
        {
            StartPlaying(0, null);
        }

        public void StartPlaying(int recording_index = 0, Slot avatar_template = null)
        {
            this.recording_index = recording_index;
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
                //if (avatarManager==null) avatarManager = new metagen.AvatarManager();
                avatarManager = new metagen.AvatarManager();
                //string reading_directory = dataManager.LastRecordingForWorld(metagen_comp.World);
                string reading_directory = dataManager.GetRecordingForWorld(metagen_comp.World, this.recording_index);
                if (reading_directory == null) return;

                List<UserMetadata> userMetadatas;
                using (var reader = new StreamReader(Path.Combine(reading_directory, "user_metadata.csv")))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    userMetadatas = csv.GetRecords<UserMetadata>().ToList();
                }
                if (userMetadatas.Where((u, i) => (u.isPublic && u.isRecording)).Count() == 0)
                {
                    UniLog.Log("UwU playing an emtpy (or private) recording");
                    metagen_comp.StopPlaying();
                }
                Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
                foreach (UserMetadata user in userMetadatas)
                {
                    if (!user.isRecording || (!user.isPublic && !metagen_comp.admin_mode)) continue; //at the moment we only allow playing back of public recording, for privacy reasons. In the future, we'll allow private access to the data
                    RefID user_id = RefID.Parse(user.userRefId);
                    UniLog.Log(user_id.ToString());
                    user_ids.Add(user_id);
                    //output_fss[user_id] = new FileStream(Directory.GetFiles(reading_directory, user_id.ToString() + "*_streams.dat")[0], FileMode.Open, FileAccess.Read);
                    output_fss[user_id] = new FileStream(Directory.GetFiles(reading_directory, user_id.ToString() + "_streams.dat")[0], FileMode.Open, FileAccess.Read);
                    BitReaderStream bitstream = new BitReaderStream(output_fss[user_id]);
                    output_readers[user_id] = new BitBinaryReaderX(bitstream);
                    fake_proxies[user_id] = new List<Tuple<BodyNode, AvatarObjectSlot>>();
                    avatar_pose_nodes[user_id] = new List<Tuple<BodyNode, IAvatarObject>>();
                    avatar_stream_channels[user_id] = new Dictionary<BodyNode, Tuple<bool, bool, bool>>();
                    proxy_slots[user_id] = new Dictionary<BodyNode, Slot>();
                    if (avatarManager.avatar_template == null && avatar_template != null)
                    {
                        //metagen_comp.World.RunSynchronously(() =>
                        //{
                        avatarManager.avatar_template = avatar_template;
                        //});
                    }
                    Slot avatar = await avatarManager.GetAvatar();
                    UniLog.Log("AVATAR");
                    UniLog.Log(avatar.ToString());
                    avatars[user_id] = avatar;
                    List<IAvatarObject> components = avatar.GetComponentsInChildren<IAvatarObject>();
                    List<AvatarObjectSlot> root_comps = avatar.GetComponentsInChildren<AvatarObjectSlot>();
                    boness[user_id] = avatar.GetComponentInChildren<Rig>()?.Bones.ToList();

                    //READ absolute time
                    output_readers[user_id].ReadSingle();
                    //READ version identifier
                    int version_number = output_readers[user_id].ReadInt32();
                    float3 relative_avatar_scale = new float3(1f, 1f, 1f);
                    int numBodyNodes = version_number;
                    if (version_number >= 1000)
                    {
                        //READ relative avatar scale
                        relative_avatar_scale.SetComponent(output_readers[user_id].ReadSingle(), 0);
                        relative_avatar_scale.SetComponent(output_readers[user_id].ReadSingle(), 1);
                        relative_avatar_scale.SetComponent(output_readers[user_id].ReadSingle(), 2);
                        //READ number of body nodes
                        numBodyNodes = output_readers[user_id].ReadInt32();
                    }
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
                        if (version_number == 1000)
                        {
                            bodyNodeType = (BodyNode)Enum.Parse(typeof(BodyNode), Enum.GetName(typeof(OldBodyNodes), (OldBodyNodes)nodeInt));
                        }
                        VRIKAvatar avatarIK = avatar.GetComponentInChildren<VRIKAvatar>();
                        avatarIK.IK.Target.Solver.SimulationSpace.Target = avatar;
                        avatarIK.IK.Target.Solver.OffsetSpace.Target = avatar;

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
                                        comp.Slot.LocalScale = comp.Slot.LocalScale * relative_avatar_scale;
                                    }
                                    //if (bodyNodeType == BodyNode.Root)
                                    //{
                                    //    proxy_slots[user_id][bodyNodeType] = avatar;
                                    //} else
                                    //{
                                    //    proxy_slots[user_id][bodyNodeType] = comp.Slot;
                                    //}
                                    //AvatarObjectSlot connected_comp = comp.EquippingSlot;
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
                                    //MethodInfo dynMethod = connected_comp.Slot.GetType().GetMethod("RegisterUserRoot",
                                    //    BindingFlags.NonPublic | BindingFlags.Instance);
                                    //dynMethod.Invoke(connected_comp.Slot, new object[] { metagen_comp.World.LocalUser.LocalUserRoot });
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
                                    //avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(bodyNodeType, comp));
                                    //if (comp.Node != BodyNode.Root)
                                    //{
                                    //    ((AvatarPoseNode)comp).IsTracking.Value = true;
                                    //}
                                    node_found = true;
                                    break;
                                }
                                if (node_found) break;
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
                                //UniLog.Log(nodee.ToString());
                                //fingerSegment.RotationDrive.Target.ReleaseLink(fingerSegment.RotationDrive.Target.DirectLink);
                                finger_slots[user_id][nodee] = fingerSegment.Root.Target;
                                proxy_slots[user_id][nodee] = fingerSegment.Root.Target;
                                finger_compensations[user_id][nodee] = fingerSegment.CoordinateCompensation.Value;
                                //finger_rotations[user_id][nodee] = new RelayRef<IValue<floatQ>>();
                                //finger_rotations[user_id][nodee].TrySet(fingerSegment.Root.Target);
                                //UniLog.Log(fingerSegment.RotationDrive.Target.Parent.ToString());
                                fingerSegment.RotationDrive.Target = (IField<floatQ>)null;
                                //fingerSegment.RotationDrive.ReleaseLink();
                                //fingerSegment.RotationDrive.Target.Value.
                                //fingerSegment.RotationDrive.Target = null;
                            }
                        }
                    }
                    UniLog.Log("got finger rotation vars");
                    //AUDIO PLAY
                    UniLog.Log("Setting up audio!");
                    avatar.GetComponentInChildren<AudioOutput>().Source.Target = null;
                    AvatarAudioOutputManager avatarAudioOutputManager = avatar.GetComponentInChildren<AvatarAudioOutputManager>();
                    AudioOutput audio_output = avatarAudioOutputManager?.AudioOutput.Target;
                    avatarAudioOutputManager?.Slot.RemoveComponent(avatarAudioOutputManager);
                    for (int i = 0; i < 2; i++)
                    {
                        string audio_file;
                        if (i == 0)
                        {
                            if (!play_hearing) continue;
                            string[] files = Directory.GetFiles(reading_directory, user_id.ToString() + "*_hearing.ogg");
                            audio_file = files.Length > 0 ? files[0] : null;
                        } else
                        {
                            if (!play_voice) continue;
                            string[] files = Directory.GetFiles(reading_directory, user_id.ToString() + "*_voice.ogg");
                            audio_file = files.Length > 0 ? files[0] : null;
                        }
                        if (File.Exists(audio_file))
                        {
                            //AudioOutput audio_output = avatar.GetComponentInChildren<AudioOutput>();
                            if (audio_output == null) audio_output = avatar.AttachComponent<AudioOutput>();
                            if (audio_output.Source.Target != null) audio_output = audio_output.Slot.AttachComponent<AudioOutput>();
                            audio_outputs[user_id] = audio_output;
                            VisemeAnalyzer visemeAnalyzer = avatar.GetComponentInChildren<VisemeAnalyzer>();
                            audio_output.Volume.Value = 1f;
                            audio_output.Enabled = true;
                            //audio_outputs[user_id] = audio_output;
                            //AudioX audioData = new AudioX(reading_directory + "/" + user_id.ToString() + "_audio.wav");
                            //AssetRef<AudioClip> audioClip = new AssetRef<AudioClip>();
                            Uri uri = null;
                            //World.RunSynchronously(async () =>
                            //{
                            uri = await this.World.Engine.LocalDB.ImportLocalAssetAsync(audio_file, LocalDB.ImportLocation.Original, (string)null);
                            //});
                            //ToWorld thing = new ToWorld();
                            //var awaiter = thing.GetAwaiter();
                            //awaiter.GetResult();
                            StaticAudioClip audioClip = audio_output.Slot.AttachAudioClip(uri);
                            AudioClipPlayer player = audio_output.Slot.AttachComponent<AudioClipPlayer>();
                            if (visemeAnalyzer != null)
                            {
                                visemeAnalyzer.Source.Target = player;
                            }
                            UniLog.Log("attaching clip to player");
                            player.Clip.Target = (IAssetProvider<AudioClip>)audioClip;
                            UniLog.Log("attaching player to audio output");
                            audio_output.Source.Target = (IAudioSource)player;
                            audio_output.Slot.AttachComponent<AudioMetadata>(true, (Action<AudioMetadata>)null).SetFromCurrentWorld();
                            player.Play();
                        }
                    }
                }
                avatars_finished_loading = true;
                isPlaying = true;
                if (generateAnimation)
                {
                    animationRecorder.StartRecordingAvatars(avatars, audio_outputs);
                }
                if (generateBvh)
                {
                    Guid g = Guid.NewGuid();
                    bvhRecorder.StartRecordingAvatars(avatars, g.ToString());
                }
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
            //foreach(var item in fake_proxies)
            //{
            //    RefID user_id = item.Key;
            //    int i = 0;
            //    foreach(var item2 in item.Value)
            //    {
            //        AvatarObjectSlot proxy_comp = item2.Item2;
            //        BodyNode nodeType = item2.Item1;
            //        IAvatarObject node_comp = avatar_pose_nodes[user_id][i].Item2;
            //        CopyGlobalTransform comp = node_comp.Slot.AttachComponent<CopyGlobalTransform>();
            //        comp.Source.Target = proxy_comp.Slot;
            //        i += 1;
            //    }
            //}
            if (generateAnimation || generateBvh)
            {
                if (generateBvh)
                {
                    bvhRecorder.StopRecording();
                }
                if (generateAnimation)
                {
                    animationRecorder.PreStopRecording();
                    metagen_comp.World.RunSynchronously(() =>
                    {
                        animationRecorder.StopRecording();
                    });
                }
                Task.Run(() =>
                {
                    if (generateAnimation)
                    {
                        animationRecorder.WaitForFinish();
                    }
                    metagen_comp.World.RunSynchronously(() =>
                    {
                        UniLog.Log("Removing avatars");
                        if (generateAnimation)
                        {
                            metagen_comp.Slot.RemoveComponent(animationRecorder);
                        }
                        foreach (var item in avatars)
                        {
                            Slot slot = item.Value;
                            slot.Destroy();
                        }
                        avatars = new Dictionary<RefID, Slot>();
                        finger_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
                        hand_posers = new Dictionary<RefID, Dictionary<Chirality, HandPoser>>();
                        finger_compensations = new Dictionary<RefID, Dictionary<BodyNode, floatQ>>();
                    });
                });
            } else
            {
                UniLog.Log("AVATARS COUNT KEK");
                UniLog.Log(avatars.Count);
                foreach (var item in avatars)
                {
                    Slot slot = item.Value;
                    slot.Destroy();
                }
                avatars = new Dictionary<RefID, Slot>();
                finger_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
                hand_posers = new Dictionary<RefID, Dictionary<Chirality, HandPoser>>();
                finger_compensations = new Dictionary<RefID, Dictionary<BodyNode, floatQ>>();
            }
            output_fss = new Dictionary<RefID, FileStream>();
            output_readers = new Dictionary<RefID, BitBinaryReaderX>();
            fake_proxies = new Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>>();
            avatar_stream_channels = new Dictionary<RefID, Dictionary<BodyNode, Tuple<bool, bool, bool>>>();
            hands_are_tracked = new Dictionary<RefID, bool>();
            proxy_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
            user_ids = new List<RefID>();
            avatarManager.avatar_template = null;
            avatarManager.has_prepared_avatar = false;
            isPlaying = false;
        }
    }
}
