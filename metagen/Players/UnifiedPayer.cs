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
    public class UnifiedPayer : IPlayer
    {
        private DateTime utcNow;
        public Dictionary<RefID, FileStream> pose_streams_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, FileStream> controller_streams_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, BitBinaryReaderX> pose_streams_readers = new Dictionary<RefID, BitBinaryReaderX>();
        public Dictionary<RefID, BitBinaryReaderX> controller_streams_readers = new Dictionary<RefID, BitBinaryReaderX>();
        public Dictionary<RefID, BinaryWriter> output_writers = new Dictionary<RefID, BinaryWriter>();
        //Controller Streams
        public Dictionary<RefID, CommonTool> commonToolLefts = new Dictionary<RefID, CommonTool>();
        public Dictionary<RefID, CommonTool> commonToolRights = new Dictionary<RefID, CommonTool>();
        public Dictionary<RefID, CommonToolInputs> commonToolInputsLefts = new Dictionary<RefID, CommonToolInputs>();
        public Dictionary<RefID, CommonToolInputs> commonToolInputsRights = new Dictionary<RefID, CommonToolInputs>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> primaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> secondaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> laserActiveStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>> showLaserToOthersStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
        public Dictionary<RefID, Tuple<ValueStream<float3>, ValueStream<float3>>> laserTargetStreams = new Dictionary<RefID, Tuple<ValueStream<float3>, ValueStream<float3>>>();
        public Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>> grabDistanceStreams = new Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>>();

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
        //public bool play_controllers = true;
        public bool play_controllers = false;
        public Slot avatar_template = null;
        List<RefID> user_ids = new List<RefID>();
        metagen.AvatarManager avatarManager;
        Task avatar_loading_task;
        bool avatars_finished_loading = false;
        World World;
        public bool isPlaying { get; set; }
        public bool external_control = false;
        public Source source_type;
        private Action<CommonTool> onInputUpdate;

        public bool generateAnimation = false;
        public bool generateBvh = false;
        DataManager dataManager;
        MetaGen metagen_comp;
        RecordingTool animationRecorder;
        BvhRecorder bvhRecorder;
        public string reading_directory
        {
            get
            {
                return this.recording_index == -1 ? null : dataManager.GetRecordingForWorld(metagen_comp.World, this.recording_index);
            }
        }
        public User LocalUser
        {
            get
            {
                return this.metagen_comp.World.LocalUser;
            }
        }
        public UnifiedPayer(DataManager dataMan, MetaGen component)
        {
            dataManager = dataMan;
            metagen_comp = component;
            World = component.World;
            bvhRecorder = new BvhRecorder(metagen_comp);
        }
        private void PlayPoseStreams()
        {
            foreach (var item1 in pose_streams_readers)
            {
                RefID user_id = item1.Key;
                //UniLog.Log("LELELELELE play pose streams: "+user_id.ToString());
                //Decode the streams
                BinaryReaderX reader = pose_streams_readers[user_id];

                //READ deltaT
                float deltaT = reader.ReadSingle();
                UniLog.Log(deltaT);
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
                        if (node == BodyNode.Root)
                        {
                            scale = slot.Parent.GlobalScaleToLocal(scale);
                        }
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
                        if (node == BodyNode.Root)
                        {
                            position = slot.Parent.GlobalPointToLocal(position);
                        }
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
                        if (node == BodyNode.Root)
                        {
                            rotation = slot.Parent.GlobalRotationToLocal(rotation);
                        }
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
        }

        private void PlayControllerStreams()
        {
            foreach(var item in controller_streams_readers)
            {
                RefID user_id = item.Key;
                //UniLog.Log("LOLOLOLOL play controller streams: "+user_id.ToString());
                BinaryReaderX reader = item.Value;
                CommonToolInputs commonToolInputsLeft = commonToolInputsLefts[user_id];
                CommonToolInputs commonToolInputsRight = commonToolInputsRights[user_id];
                Tuple<ValueStream<bool>,ValueStream<bool>> primaryBlockedStreamBoth = primaryBlockedStreams[user_id];
                Tuple<ValueStream<bool>,ValueStream<bool>> secondaryBlockedStreamBoth = secondaryBlockedStreams[user_id];
                Tuple<ValueStream<bool>,ValueStream<bool>> laserActiveStreamBoth = laserActiveStreams[user_id];
                Tuple<ValueStream<bool>,ValueStream<bool>> showLaserToOthersStreamBoth = showLaserToOthersStreams[user_id];
                Tuple<ValueStream<float3>,ValueStream<float3>> laserTargetStreamBoth = laserTargetStreams[user_id];
                Tuple<ValueStream<float>,ValueStream<float>> grabDistanceStreamBoth = grabDistanceStreams[user_id];
                //READ deltaT
                float deltaT = reader.ReadSingle();

                //READ primaryStreams
                bool primaryLeft = reader.ReadBoolean(); //Left
                bool primaryRight = reader.ReadBoolean(); //Right
                commonToolInputsLeft.Interact.Value.UpdateState(primaryLeft);
                commonToolInputsRight.Interact.Value.UpdateState(primaryRight);
                
                //READ secondaryStreams
                bool secondaryLeft = reader.ReadBoolean(); //Left
                bool secondaryRight = reader.ReadBoolean(); //Right
                commonToolInputsLeft.Secondary.Value.UpdateState(secondaryLeft);
                commonToolInputsRight.Secondary.Value.UpdateState(secondaryRight);

                //READ grabStreams
                bool grabLeft = reader.ReadBoolean(); //Left
                bool grabRight = reader.ReadBoolean(); //Right
                //UniLog.Log(grabLeft.ToString());
                //UniLog.Log(grabRight.ToString());
                commonToolInputsLeft.Grab.Value.UpdateState(grabLeft);
                commonToolInputsRight.Grab.Value.UpdateState(grabRight);

                //READ menuStreams
                bool menuLeft = reader.ReadBoolean(); //Left
                bool menuRight = reader.ReadBoolean(); //Right
                commonToolInputsLeft.Menu.Value.UpdateState(menuLeft);
                commonToolInputsRight.Menu.Value.UpdateState(menuRight);

                //READ strengthStreams
                float strengthLeft = reader.ReadSingle(); //Left
                float strengthRight = reader.ReadSingle(); //Right
                commonToolInputsLeft.Strength.Value.UpdateValue(strengthLeft, deltaT);
                commonToolInputsRight.Strength.Value.UpdateValue(strengthRight, deltaT);

                //READ axisStreams
                float2 axisLeft = reader.Read2D_Single(); //Left
                float2 axisRight = reader.Read2D_Single(); //Right
                commonToolInputsLeft.Axis.Value.UpdateValue(axisLeft, deltaT);
                commonToolInputsRight.Axis.Value.UpdateValue(axisRight, deltaT);

                //commonToolInputsLeft.Update(deltaT);
                //commonToolInputsRight.Update(deltaT);
                CommonTool commonToolLeft = commonToolLefts[user_id];
                CommonTool commonToolRight = commonToolRights[user_id];
                //var onInputUpdateInfo = commonToolLeft.GetType().GetMethod("OnInputUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                //var startGrab = commonToolLeft.GetType().GetMethod("StartGrab", BindingFlags.NonPublic | BindingFlags.Instance);
                if (commonToolLeft.IsStarted)
                {
                    //UniLog.Log((commonToolLeft.Inputs == commonToolInputsLeft).ToString());
                    UniLog.Log("uwu");
                    //UniLog.Log(commonToolLeft.Inputs.Grab.Pressed);
                    //if (commonToolLeft.Inputs.Grab.Pressed)
                    //    startGrab.Invoke(commonToolLeft, null);
                    onInputUpdate(commonToolLeft);
                    //onInputUpdateInfo.Invoke(commonToolLeft, null);
                }
                if (commonToolRight.IsStarted)
                {
                    //UniLog.Log((commonToolRight.Inputs == commonToolInputsRight).ToString());
                    UniLog.Log("owo");
                    UniLog.Log(commonToolRight.Inputs.Grab.Pressed);
                    //if (commonToolRight.Inputs.Grab.Pressed)
                    //    startGrab.Invoke(commonToolRight, null);
                    onInputUpdate(commonToolRight);
                    //onInputUpdateInfo.Invoke(commonToolRight, null);
                }

                //READ primaryBlockedStreams
                bool primaryBlockedLeft = reader.ReadBoolean(); //Left
                bool primaryBlockedRight = reader.ReadBoolean(); //Right
                primaryBlockedStreamBoth.Item1.Value = primaryBlockedLeft;
                primaryBlockedStreamBoth.Item2.Value = primaryBlockedLeft;

                //READ secondaryBlockedStreams
                bool secondaryBlockedLeft = reader.ReadBoolean(); //Left
                bool secondaryBlockedRight = reader.ReadBoolean(); //Right
                secondaryBlockedStreamBoth.Item1.Value = secondaryBlockedLeft;
                secondaryBlockedStreamBoth.Item2.Value = secondaryBlockedLeft;

                //WRITE laserActiveStreams
                bool laserActiveLeft = reader.ReadBoolean(); //Left
                bool laserActiveRight = reader.ReadBoolean(); //Right
                laserActiveStreamBoth.Item1.Value = laserActiveLeft;
                laserActiveStreamBoth.Item2.Value = laserActiveLeft;

                //READ showLaserToOthersStreams
                bool showLaserToOthersLeft = reader.ReadBoolean(); //Left
                bool showLaserToOthersRight = reader.ReadBoolean(); //Right
                showLaserToOthersStreamBoth.Item1.Value = showLaserToOthersLeft;
                showLaserToOthersStreamBoth.Item2.Value = showLaserToOthersLeft;

                //READ laserTargetStreams
                float3 laserTargetLeft = reader.Read3D_Single(); //Left
                float3 laserTargetRight = reader.Read3D_Single(); //Right
                laserTargetStreamBoth.Item1.Value = laserTargetLeft;
                laserTargetStreamBoth.Item2.Value = laserTargetLeft;

                //READ grabDistanceStreams
                float grabDistanceLeft = reader.ReadSingle(); //Left
                float grabDistanceRight = reader.ReadSingle(); //Right
                grabDistanceStreamBoth.Item1.Value = grabDistanceLeft;
                grabDistanceStreamBoth.Item2.Value = grabDistanceLeft;
            }
        }

        public void PlayStreams()
        {
            if (!avatars_finished_loading) return;
            //currentWorld.RunSynchronously(() =>
            //{
            try
            {
                PlayPoseStreams();
                PlayControllerStreams();
                if (generateAnimation)
                {
                    try
                    {
                        animationRecorder.RecordFrame();
                    }
                    catch (Exception e)
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
                    }
                    catch (Exception e)
                    {
                        UniLog.Log("Error at Bvh recording: " + e.Message);
                        UniLog.Log(e.StackTrace);
                    }
                }
            }
            catch (Exception e)
            {
                UniLog.Log("OwO: " + e.Message);
                UniLog.Log(e.StackTrace);
                if (e.InnerException != null)
                {
                    UniLog.Log(e.InnerException.Message);
                    UniLog.Log(e.InnerException.StackTrace);
                }
                //this.StopPlaying();
                if (!external_control)
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
            SimpleAvatarProtection protectionComponent = avatar_template?.GetComponentInChildren<SimpleAvatarProtection>();
            if (protectionComponent != null && !metagen_comp.admin_mode) return;
            this.recording_index = recording_index;
            this.play_voice = metagen_comp.play_voice;
            this.play_hearing = metagen_comp.play_hearing;
            this.avatar_template = avatar_template;
            this.source_type = Source.FILE;
            this.external_control = false;
            avatar_loading_task = Task.Run(StartPlayingInternal);
        }
        public void StartPlayingExternal(int num_meta_datas = 1, Slot avatar_template = null)
        {
            SimpleAvatarProtection protectionComponent = avatar_template?.GetComponentInChildren<SimpleAvatarProtection>();
            if (protectionComponent != null && !metagen_comp.admin_mode) return;
            this.recording_index = -1;
            this.play_voice = false;
            this.play_hearing = false;
            this.avatar_template = avatar_template;
            this.source_type = Source.STREAM;
            List<UserMetadata> userMetadatas = PrepareMetadatas(num_meta_datas);
            this.external_control = true;
            Task.Run(() => StartPlayingInternal(userMetadatas));
        }
        private List<UserMetadata> PrepareMetadatas(int num_meta_datas=1)
        {
            List<UserMetadata> userMetadatas = new List<UserMetadata>();
            if (this.source_type == Source.FILE)
            {
                if (reading_directory == null) return null;

                using (var reader = new StreamReader(Path.Combine(reading_directory, "user_metadata.csv")))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    userMetadatas = csv.GetRecords<UserMetadata>().ToList();
                }
                if (userMetadatas.Where((u, i) => ((u.isPublic || metagen_comp.admin_mode) && u.isRecording)).Count() == 0)
                {
                    UniLog.Log("UwU playing an emtpy (or private) recording");
                    metagen_comp.StopPlaying();
                }
            } else if (this.source_type == Source.STREAM) {
                userMetadatas = new List<UserMetadata>();
                for (int i = 0; i < num_meta_datas; i++)
                {
                    UserMetadata userMetadata = new UserMetadata();
                    userMetadata.userRefId = "ID2C0"+i.ToString();
                    userMetadata.userId = "null";
                    userMetadata.isPublic = true;
                    userMetadata.isRecording = true;
                    userMetadatas.Add(userMetadata);
                }
            }
            return userMetadatas;
        }
        private async void StartPlayingInternal()
        {
            List<UserMetadata> userMetadatas = PrepareMetadatas();
            PrepareStreams(userMetadatas);
            await Task.Run(()=>StartPlayingInternal(userMetadatas));
        }
        public void PrepareStreamsExternal(int num_meta_datas=1)
        {
            this.source_type = Source.STREAM;
            List<UserMetadata> userMetadatas = PrepareMetadatas(num_meta_datas);
            PrepareStreams(userMetadatas);
        }
        private void PrepareStreams(List<UserMetadata> userMetadatas)
        {
            foreach (UserMetadata user in userMetadatas)
            {
                if (!user.isRecording || (!user.isPublic && !metagen_comp.admin_mode)) continue; //at the moment we only allow playing back of public recording, for privacy reasons. In the future, we'll allow private access to the data
                RefID user_id = RefID.Parse(user.userRefId);
                UniLog.Log(user_id.ToString());
                user_ids.Add(user_id);
                BitReaderStream bitstream = null;
                if (this.source_type == Source.FILE)
                {
                    pose_streams_fss[user_id] = new FileStream(Directory.GetFiles(reading_directory, user_id.ToString() + "_streams.dat")[0], FileMode.Open, FileAccess.Read);
                    bitstream = new BitReaderStream(pose_streams_fss[user_id]);
                }
                else if (this.source_type == Source.STREAM)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    output_writers[user_id] = new BinaryWriter(memoryStream);
                    bitstream = new BitReaderStream(memoryStream);
                }
                pose_streams_readers[user_id] = new BitBinaryReaderX(bitstream);
            }
        }
        private async void StartPlayingInternal(List<UserMetadata> userMetadatas)
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
                if (userMetadatas == null) return;
                Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
                foreach (UserMetadata user in userMetadatas)
                {
                    if (!user.isRecording || (!user.isPublic && !metagen_comp.admin_mode)) continue; //at the moment we only allow playing back of public recording, for privacy reasons. In the future, we'll allow private access to the data
                    RefID user_id = RefID.Parse(user.userRefId);
                    UniLog.Log(user_id.ToString());
                    user_ids.Add(user_id);
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
                    UniLog.Log(pose_streams_readers[user_id].ReadSingle());
                    //READ version identifier
                    int version_number = pose_streams_readers[user_id].ReadInt32();
                    float3 relative_avatar_scale = new float3(1f, 1f, 1f);
                    int numBodyNodes = version_number;
                    UniLog.Log("version_number");
                    UniLog.Log(version_number);
                    if (version_number >= 1000)
                    {
                        //READ relative avatar scale
                        relative_avatar_scale = relative_avatar_scale.SetComponent(pose_streams_readers[user_id].ReadSingle(), 0);
                        UniLog.Log(relative_avatar_scale.X);
                        relative_avatar_scale = relative_avatar_scale.SetComponent(pose_streams_readers[user_id].ReadSingle(), 1);
                        UniLog.Log(relative_avatar_scale.Y);
                        relative_avatar_scale = relative_avatar_scale.SetComponent(pose_streams_readers[user_id].ReadSingle(), 2);
                        UniLog.Log(relative_avatar_scale.Z);
                        //READ number of body nodes
                        numBodyNodes = pose_streams_readers[user_id].ReadInt32();
                        UniLog.Log(numBodyNodes);
                    }
                    Slot left_hand_slot = null;
                    Slot right_hand_slot = null;
                    Slot left_controller_slot = null;
                    Slot right_controller_slot = null;

                    for (int i = 0; i < numBodyNodes; i++)
                    {
                        //READ body node type
                        int nodeInt = pose_streams_readers[user_id].ReadInt32();
                        UniLog.Log(nodeInt);
                        //READ if scale stream exists
                        bool scale_exists = pose_streams_readers[user_id].ReadBoolean();
                        UniLog.Log(scale_exists);
                        //READ if position stream exists
                        bool pos_exists = pose_streams_readers[user_id].ReadBoolean();
                        UniLog.Log(pos_exists);
                        //READ if rotation stream exists
                        bool rot_exists = pose_streams_readers[user_id].ReadBoolean();
                        UniLog.Log(rot_exists);
                        BodyNode bodyNodeType = (BodyNode)nodeInt;
                        if (version_number < 1000)
                        {
                            bodyNodeType = (BodyNode)Enum.Parse(typeof(BodyNode), Enum.GetName(typeof(OldBodyNodes), (OldBodyNodes)nodeInt));
                        }
                        VRIKAvatar avatarIK = avatar.GetComponentInChildren<VRIKAvatar>();
                        avatarIK.IK.Target.Solver.SimulationSpace.Target = avatar;
                        avatarIK.IK.Target.Solver.OffsetSpace.Target = avatar;

                        bool node_found = false;
                        IAvatarObject avatarObject = null;
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
                                    avatarObject = comp;
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
                        if (!node_found)
                        {
                            Slot fake_proxy = avatar.AddSlot(bodyNodeType.ToString());
                            Slot avatar_pose_node_slot = avatar.AddSlot(bodyNodeType.ToString()+"-pose_node");
                            AvatarObjectSlot avatarObjectSlot = fake_proxy.AttachComponent<AvatarObjectSlot>();
                            AvatarPoseNode avatarPoseNode = avatar_pose_node_slot.AttachComponent<AvatarPoseNode>();
                            avatarObject = (IAvatarObject)avatarPoseNode;
                            avatarPoseNode.Node.Value = bodyNodeType;
                            //avatarPoseNode.IsTracking.Value = true;
                            avatarObjectSlot.Node.Value = avatarPoseNode.Node;
                            avatarObjectSlot.Equipped.ForceLink(avatarPoseNode);
                            avatarObjectSlot.IsTracking.Value = false;
                            fake_proxies[user_id].Add(new Tuple<BodyNode, AvatarObjectSlot>(bodyNodeType, avatarObjectSlot));
                            avatar_pose_nodes[user_id].Add(new Tuple<BodyNode, IAvatarObject>(avatarPoseNode.Node, avatarPoseNode));
                            //throw new Exception("Node " + bodyNodeType.ToString() + " not found in avatar!");
                        }
                        if (bodyNodeType == BodyNode.LeftHand)
                        {
                            if (avatarObject != null)
                                left_hand_slot = avatarObject.Slot;
                        }
                        else if (bodyNodeType == BodyNode.RightHand)
                        {
                            if (avatarObject != null)
                                right_hand_slot = avatarObject.Slot;
                        }
                        else if (bodyNodeType == BodyNode.LeftController)
                        {
                            if (avatarObject != null)
                                left_controller_slot = avatarObject.Slot;
                        }
                        else if (bodyNodeType == BodyNode.RightController)
                        {
                            if (avatarObject != null)
                                right_controller_slot = avatarObject.Slot;
                        }
                        avatar_stream_channels[user_id][bodyNodeType] = new Tuple<bool, bool, bool>(scale_exists, pos_exists, rot_exists);
                    }

                    //HAND TRACKING
                    //READ whether hands are being tracked
                    hands_are_tracked[user_id] = pose_streams_readers[user_id].ReadBoolean();
                    UniLog.Log(hands_are_tracked[user_id]);
                    //READ whether metacarpals are being tracked
                    UniLog.Log(pose_streams_readers[user_id].ReadBoolean());
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
                        }
                        else
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

                    //CONTROLLER STREAMS
                    UniLog.Log("Setting up controller streams!");
                    metagen_comp.World.RunSynchronously(() =>
                    {
                        avatar.SetParent(LocalUser.Root.Slot);
                    });
                    if (play_controllers)
                    {
                        string[] files = Directory.GetFiles(reading_directory, user_id.ToString() + "*_controller_streams.dat");
                        string controller_streams_file = files.Length > 0 ? files[0] : null;
                        if (controller_streams_file != null)
                        {
                            //Add CommonTool
                            CommonToolStreamDriver commonToolStreamDriverLeft = avatar.AttachComponent<CommonToolStreamDriver>();
                            commonToolStreamDriverLeft.Side.Value = Chirality.Left;
                            ValueStream<bool> primaryBlockedStreamLeft = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<bool> secondaryBlockedStreamLeft = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<bool> laserActiveStreamLeft = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<bool> showLaserToOthersStreamLeft = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<float3> laserTargetStreamLeft = LocalUser.AddStream<ValueStream<float3>>();
                            ValueStream<float> grabDistanceStreamLeft = LocalUser.AddStream<ValueStream<float>>();
                            commonToolStreamDriverLeft.PrimaryBlockedStream.Target = primaryBlockedStreamLeft;
                            commonToolStreamDriverLeft.SecondaryBlockedStream.Target = secondaryBlockedStreamLeft;
                            commonToolStreamDriverLeft.LaserActiveStream.Target = laserActiveStreamLeft;
                            commonToolStreamDriverLeft.ShowLaserToOthersStream.Target = showLaserToOthersStreamLeft;
                            commonToolStreamDriverLeft.LaserTargetStream.Target = laserTargetStreamLeft;
                            commonToolStreamDriverLeft.GrabDistanceStream.Target = grabDistanceStreamLeft;
                            CommonToolStreamDriver commonToolStreamDriverRight = avatar.AttachComponent<CommonToolStreamDriver>();
                            commonToolStreamDriverRight.Side.Value = Chirality.Right;
                            ValueStream<bool> primaryBlockedStreamRight = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<bool> secondaryBlockedStreamRight = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<bool> laserActiveStreamRight = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<bool> showLaserToOthersStreamRight = LocalUser.AddStream<ValueStream<bool>>();
                            ValueStream<float3> laserTargetStreamRight = LocalUser.AddStream<ValueStream<float3>>();
                            ValueStream<float> grabDistanceStreamRight = LocalUser.AddStream<ValueStream<float>>();
                            commonToolStreamDriverRight.PrimaryBlockedStream.Target = primaryBlockedStreamRight;
                            commonToolStreamDriverRight.SecondaryBlockedStream.Target = secondaryBlockedStreamRight;
                            commonToolStreamDriverRight.LaserActiveStream.Target = laserActiveStreamRight;
                            commonToolStreamDriverRight.ShowLaserToOthersStream.Target = showLaserToOthersStreamRight;
                            commonToolStreamDriverRight.LaserTargetStream.Target = laserTargetStreamRight;
                            commonToolStreamDriverRight.GrabDistanceStream.Target = grabDistanceStreamRight;
                            primaryBlockedStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(primaryBlockedStreamLeft, primaryBlockedStreamRight);
                            secondaryBlockedStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(secondaryBlockedStreamLeft, secondaryBlockedStreamRight);
                            laserActiveStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(laserActiveStreamLeft, laserActiveStreamRight);
                            showLaserToOthersStreams[user_id] = new Tuple<ValueStream<bool>, ValueStream<bool>>(showLaserToOthersStreamLeft, showLaserToOthersStreamRight);
                            laserTargetStreams[user_id] = new Tuple<ValueStream<float3>, ValueStream<float3>>(laserTargetStreamLeft, laserTargetStreamRight);
                            grabDistanceStreams[user_id] = new Tuple<ValueStream<float>, ValueStream<float>>(grabDistanceStreamLeft, grabDistanceStreamRight);

                            //var headDeviceProp = typeof(SystemInfoConnector).GetProperty("HeadDevice");
                            //headDeviceProp = headDeviceProp.DeclaringType.GetProperty("HeadDevice");
                            //headDeviceProp.SetValue(metagen_comp.Engine.SystemInfo, HeadOutputDevice.SteamVR, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                            //metagen_comp.InputInterface.VR_Active = true;

                            CommonTool commonToolLeft = left_controller_slot.AddSlot("Common Tool").AttachComponent<CommonTool>(false);
                            //ContextMenu contextMenuLeft = left_hand_slot.AddSlot("Context Menu", true).AttachComponent<ContextMenu>(true, (Action<ContextMenu>)null);
                            //contextMenuLeft.Owner.Target = LocalUser;
                            commonToolLeft.Side.Value = Chirality.Left;
                            commonToolLeft.InitializeTool(commonToolStreamDriverLeft);
                            Grabber grabberLeft = commonToolLeft.Grabber;
                            PropertyInfo linkingkey_prop = typeof(Grabber).GetProperty("LinkingKey");
                            linkingkey_prop = linkingkey_prop.DeclaringType.GetProperty("LinkingKey");
                            linkingkey_prop.SetValue(grabberLeft, Grabber.LEFT_HAND_KEY, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                            //commonToolLeft.ContextMenu.Target = contextMenuLeft;
                            AvatarObjectSlot objectSlotLeft = left_hand_slot.GetComponent<AvatarPoseNode>().EquippingSlot;
                            Slot proxy_slot_left = left_hand_slot.GetComponent<AvatarObjectComponentProxy>()?.Target.Target;
                            metagen_comp.World.RunSynchronously(() => {
                                foreach (Slot slot in new List<Slot> { left_hand_slot, proxy_slot_left }) {
                                    AvatarObjectSlot.ForeachObjectComponent(slot, (Action<IAvatarObjectComponent>)(c =>
                                    {
                                        //UniLog.Log(c);
                                        try
                                        {
                                            if (c is AvatarToolAnchor anchor)
                                            {
                                                if (anchor != null)
                                                {
                                                        switch (anchor.AnchorPoint.Value)
                                                    {
                                                        case AvatarToolAnchor.Point.Tooltip:
                                                            UniLog.Log(anchor.Slot);
                                                            UniLog.Log(anchor.AnchorPoint.Value);
                                                            commonToolLeft.SetTooltipAnchor(anchor.Slot);
                                                            break;
                                                        case AvatarToolAnchor.Point.GrabArea:
                                                            UniLog.Log(anchor.Slot);
                                                            UniLog.Log(anchor.AnchorPoint.Value);
                                                            commonToolLeft.SetGrabberAnchor(anchor.Slot);
                                                            break;
                                                        case AvatarToolAnchor.Point.Toolshelf:
                                                            UniLog.Log(anchor.Slot);
                                                            UniLog.Log(anchor.AnchorPoint.Value);
                                                            commonToolLeft.SetToolshelfAnchor(anchor.Slot);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                                //objectSlot.Debug.Error((object)(string.Format("Exception in OnEquip on {0}\n", (object)c) + DebugManager.PreprocessException<Exception>(ex)?.ToString()), true);
                                                UniLog.Log((object)(string.Format("Exception in OnEquip on {0}\n", (object)c) + DebugManager.PreprocessException<Exception>(ex)?.ToString()), true);
                                        }
                                    }));
                                }
                            });
                            CommonTool commonToolRight = right_controller_slot.AddSlot("Common Tool").AttachComponent<CommonTool>(false);
                            //ContextMenu contextMenuRight = right_hand_slot.AddSlot("Context Menu", true).AttachComponent<ContextMenu>(true, (Action<ContextMenu>)null);
                            //contextMenuRight.Owner.Target = LocalUser;
                            commonToolRight.Side.Value = Chirality.Right;
                            commonToolRight.InitializeTool(commonToolStreamDriverRight);
                            Grabber grabberRight = commonToolRight.Grabber;
                            linkingkey_prop.SetValue(grabberRight, Grabber.RIGHT_HAND_KEY, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                            //commonToolRight.ContextMenu.Target = contextMenuRight;
                            AvatarObjectSlot objectSlotRight = right_hand_slot.GetComponent<AvatarPoseNode>().EquippingSlot;
                            Slot proxy_slot_right = right_hand_slot.GetComponent<AvatarObjectComponentProxy>()?.Target.Target;
                            metagen_comp.World.RunSynchronously(() =>
                            {
                                foreach (Slot slot in new List<Slot> { right_hand_slot, proxy_slot_right })
                                {
                                    AvatarObjectSlot.ForeachObjectComponent(slot, (Action<IAvatarObjectComponent>)(c =>
                                    {
                                        //UniLog.Log(c);
                                        try
                                        {
                                            if (c is AvatarToolAnchor anchor)
                                            {
                                                //c.OnEquip(objectSlotLeft);
                                                if (anchor != null)
                                                {
                                                    switch (anchor.AnchorPoint.Value)
                                                    {
                                                        case AvatarToolAnchor.Point.Tooltip:
                                                            UniLog.Log(anchor.Slot);
                                                            UniLog.Log(anchor.AnchorPoint.Value);
                                                            commonToolRight.SetTooltipAnchor(anchor.Slot);
                                                            break;
                                                        case AvatarToolAnchor.Point.GrabArea:
                                                            UniLog.Log(anchor.Slot);
                                                            UniLog.Log(anchor.AnchorPoint.Value);
                                                            commonToolRight.SetGrabberAnchor(anchor.Slot);
                                                            break;
                                                        case AvatarToolAnchor.Point.Toolshelf:
                                                            UniLog.Log(anchor.Slot);
                                                            UniLog.Log(anchor.AnchorPoint.Value);
                                                            commonToolRight.SetToolshelfAnchor(anchor.Slot);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            //objectSlot.Debug.Error((object)(string.Format("Exception in OnEquip on {0}\n", (object)c) + DebugManager.PreprocessException<Exception>(ex)?.ToString()), true);
                                            UniLog.Log((object)(string.Format("Exception in OnEquip on {0}\n", (object)c) + DebugManager.PreprocessException<Exception>(ex)?.ToString()), true);
                                        }
                                    }));
                                }
                            });
                            CommonToolInputs commonToolInputsLeft = new CommonToolInputs(Chirality.Left);
                            CommonToolInputs commonToolInputsRight = new CommonToolInputs(Chirality.Right);

                            var prop = commonToolLeft.GetType().GetField("_inputs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            //if (onInputUpdate == null)
                            onInputUpdate = (Action<CommonTool>) Delegate.CreateDelegate(typeof(Action<CommonTool>), commonToolLeft.GetType().GetMethod("OnInputUpdate", BindingFlags.NonPublic | BindingFlags.Instance));
                            //var onInputEvaluate = commonToolLeft.GetType().GetMethod("OnInputEvaluate", BindingFlags.NonPublic | BindingFlags.Instance);
                            //prop.SetValue(commonToolLeft, commonToolInputsLeft);
                            //commonToolInputsLeft.RegisterManager(metagen_comp.Input, commonToolLeft, new Action(()=>onInputUpdate.Invoke(commonToolLeft,null)), new Action(()=>onInputEvaluate.Invoke(commonToolLeft, null)));
                            //prop.SetValue(commonToolRight, commonToolInputsRight);
                            //commonToolInputsRight.RegisterManager(metagen_comp.Input, commonToolRight, new Action(()=>onInputUpdate.Invoke(commonToolRight, null)), new Action(()=>onInputEvaluate.Invoke(commonToolRight, null)));
                            commonToolLeft.Changed += new Action<IChangeable>(target =>
                            {
                                if (((CommonTool)target).Inputs != commonToolInputsLeft)
                                {
                                    UniLog.Log("updating fields of Common Tool Left " + target.ToString());
                                    prop.SetValue(target, commonToolInputsLeft);
                                    //commonToolInputsLeft.RegisterManager(metagen_comp.Input, target, new Action(()=>onInputUpdate.Invoke(target,null)), new Action(()=>onInputEvaluate.Invoke(target, null)));
                                }
                            });
                            commonToolRight.Changed += new Action<IChangeable>(target =>
                            {
                                if (((CommonTool)target).Inputs != commonToolInputsRight)
                                {
                                    UniLog.Log("updating fields of Common Tool Right " + target.ToString());
                                    prop.SetValue(target, commonToolInputsRight);
                                    //commonToolInputsRight.RegisterManager(metagen_comp.Input, target, new Action(()=>onInputUpdate.Invoke(target, null)), new Action(()=>onInputEvaluate.Invoke(target, null)));
                                }
                            });

                            commonToolInputsLefts[user_id] = commonToolInputsLeft;
                            commonToolInputsRights[user_id] = commonToolInputsRight;
                            commonToolLefts[user_id] = commonToolLeft;
                            commonToolRights[user_id] = commonToolRight;


                            //commonToolInputsLeft.Interact.Value.UpdateState(true);

                            controller_streams_fss[user_id] = new FileStream(controller_streams_file, FileMode.Open, FileAccess.Read);
                            BitReaderStream bitstream2 = new BitReaderStream(controller_streams_fss[user_id]);
                            controller_streams_readers[user_id] = new BitBinaryReaderX(bitstream2);
                        }
                    }
                }
                World currentWorld = metagen_comp.World;
                int currentTotalUpdates = currentWorld.TotalUpdates;
                metagen_comp.StartTask(async () =>
                {
                    await Task.Run(() =>
                    {
                        bool all_commontools_loaded = false;
                        while (!all_commontools_loaded & currentWorld.TotalUpdates <= currentTotalUpdates + 60)
                        {
                            all_commontools_loaded = true;
                            foreach (var item in commonToolLefts)
                            {
                                CommonTool ct = item.Value;
                                all_commontools_loaded &= ct.IsStarted;
                            }
                            foreach (var item in commonToolRights)
                            {
                                CommonTool ct = item.Value;
                                all_commontools_loaded &= ct.IsStarted;
                            }
                        }

                        foreach (var item in avatars)
                        {
                            Slot avatar = item.Value;
                            metagen_comp.World.RunSynchronously(() => {
                                avatar.SetParent(metagen_comp.World.RootSlot);
                            });
                        }

                        avatars_finished_loading = true;
                        if (!external_control) isPlaying = true;
                        if (generateAnimation)
                        {
                            animationRecorder.StartRecordingAvatars(avatars, audio_outputs);
                        }
                        if (generateBvh)
                        {
                            Guid g = Guid.NewGuid();
                            bvhRecorder.StartRecordingAvatars(avatars, g.ToString());
                        }
                    });
                });
            }
            catch (Exception e)
            {
                UniLog.Log("TwT: " + e.Message);
            }
        }
        public void StopPlaying()
        {
            foreach (var item in pose_streams_fss)
            {
                item.Value.Close();
            }
            foreach (var item in controller_streams_fss)
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
            }
            else
            {
                UniLog.Log("AVATARS COUNT KEK");
                UniLog.Log(avatars.Count);
                foreach (var item in avatars)
                {
                    Slot slot = item.Value;
                    UniLog.Log("Removing avatar " + slot.ToString());
                    slot.Destroy();
                }
                avatars = new Dictionary<RefID, Slot>();
                finger_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
                hand_posers = new Dictionary<RefID, Dictionary<Chirality, HandPoser>>();
                finger_compensations = new Dictionary<RefID, Dictionary<BodyNode, floatQ>>();
            }
            foreach (var item in primaryBlockedStreams) {
                LocalUser.RemoveStream(item.Value.Item1);
                LocalUser.RemoveStream(item.Value.Item2);
            }
            primaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            foreach (var item in secondaryBlockedStreams) {
                LocalUser.RemoveStream(item.Value.Item1);
                LocalUser.RemoveStream(item.Value.Item2);
            }
            secondaryBlockedStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            foreach (var item in laserActiveStreams) {
                LocalUser.RemoveStream(item.Value.Item1);
                LocalUser.RemoveStream(item.Value.Item2);
            }
            laserActiveStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            foreach (var item in showLaserToOthersStreams) {
                LocalUser.RemoveStream(item.Value.Item1);
                LocalUser.RemoveStream(item.Value.Item2);
            }
            showLaserToOthersStreams = new Dictionary<RefID, Tuple<ValueStream<bool>, ValueStream<bool>>>();
            foreach (var item in laserTargetStreams) {
                LocalUser.RemoveStream(item.Value.Item1);
                LocalUser.RemoveStream(item.Value.Item2);
            }
            laserTargetStreams = new Dictionary<RefID, Tuple<ValueStream<float3>, ValueStream<float3>>>();
            foreach (var item in grabDistanceStreams) {
                LocalUser.RemoveStream(item.Value.Item1);
                LocalUser.RemoveStream(item.Value.Item2);
            }
            grabDistanceStreams = new Dictionary<RefID, Tuple<ValueStream<float>, ValueStream<float>>>();
            controller_streams_fss = new Dictionary<RefID, FileStream>();
            controller_streams_readers = new Dictionary<RefID, BitBinaryReaderX>();
            commonToolInputsLefts = new Dictionary<RefID, CommonToolInputs>();
            commonToolInputsRights = new Dictionary<RefID, CommonToolInputs>();
            pose_streams_fss = new Dictionary<RefID, FileStream>();
            pose_streams_readers = new Dictionary<RefID, BitBinaryReaderX>();
            fake_proxies = new Dictionary<RefID, List<Tuple<BodyNode, AvatarObjectSlot>>>();
            avatar_stream_channels = new Dictionary<RefID, Dictionary<BodyNode, Tuple<bool, bool, bool>>>();
            hands_are_tracked = new Dictionary<RefID, bool>();
            proxy_slots = new Dictionary<RefID, Dictionary<BodyNode, Slot>>();
            user_ids = new List<RefID>();
            avatarManager.avatar_template = null;
            avatarManager.has_prepared_avatar = false;
            isPlaying = false;
        }
        public enum Source
        {
            FILE = 0,
            STREAM = 1,
        };
    }
}
