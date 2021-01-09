/*
 * Based on the recording tool by Jeana and Lucas (https://github.com/jeanahelver/neosPlugins/blob/main/recordingTool/RecordingTool.cs)
 */
using System;
using BaseX;
using FrooxEngine;
using CodeX;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine.UIX;
using FrooxEngine.CommonAvatar;
using System.Threading;
using NeosAnimationToolset;
using metagen;
using System.IO;

namespace NeosAnimationToolset
{
    public enum ResultTypeEnum
    {
        REPLACE_REFERENCES, CREATE_VISUAL, CREATE_NON_PERSISTENT_VISUAL, CREATE_EMPTY_SLOT, CREATE_PARENT_SLOTS, DO_NOTHING, COPY_COMPONENTS
    }
    public partial class RecordingTool : Component, IRecorder
    {
        public readonly SyncRef<User> recordingUser;

        public readonly Sync<int> state;

        public readonly Sync<double> _startTime;

        public AnimX animation;

        public readonly SyncRef<Slot> rootSlot;
        public readonly Sync<bool> replaceRefs;

        //public readonly SyncList<TrackedRig> recordedRigs;
        public readonly SyncList<TrackedSkinnedMeshRenderer> recordedSMR;
        public readonly SyncList<TrackedMeshRenderer> recordedMR;
        public readonly SyncList<TrackedSlot> recordedSlots;
        public readonly SyncList<FieldTracker> recordedFields;

        public readonly SyncRef<StaticAnimationProvider> _result;
        public Animator animator;

        public string saving_folder;
        public MetaGen metagen_comp;
        public DataManager dataManager;
        public Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>> trackedSlots = new Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>>();
        public Dictionary<RefID, TrackedSlot> audioSources = new Dictionary<RefID, TrackedSlot>();
        //public Dictionary<RefID, TrackedRig> trackedRigs = new Dictionary<RefID, TrackedRig>();

        //public int animationTrackIndex = 0;

        //public readonly SyncList<ACMngr> trackedFields;

        protected override void OnAttach()
        {
            base.OnAttach();
            Slot visual = Slot.AddSlot("Visual");

            visual.LocalRotation = floatQ.Euler(90f, 0f, 0f);
            visual.LocalPosition = new float3(0, 0, 0);

            PBS_Metallic material = visual.AttachComponent<PBS_Metallic>();

            visual.AttachComponent<SphereCollider>().Radius.Value = 0.025f;

            ValueMultiplexer<color> vm = visual.AttachComponent<ValueMultiplexer<color>>();
            vm.Target.Target = material.EmissiveColor;
            vm.Values.Add(new color(0, 0.5f, 0, 1));
            vm.Values.Add(new color(0.5f, 0, 0, 1));
            vm.Values.Add(new color(0.5f, 0.5f, 0, 1));
            vm.Values.Add(new color(0, 0, 0.5f, 1));
            vm.Index.DriveFrom<int>(state);

            CylinderMesh mesh = visual.AttachMesh<CylinderMesh>(material);
            mesh.Radius.Value = 0.015f;
            mesh.Height.Value = 0.05f;
        }

        public bool isRecording
        {
            get
            {
                return state.Value == 1;
            }
        }

        public void StartRecording()
        {
            Slot holder = World.RootSlot.AddSlot("holder");
            rootSlot.Target = holder;
            bool record_proxies = false;
            bool record_rigs = false;
            bool record_audio_sources = true;
            bool record_smr = true;
            trackedSlots = new Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>>();
            recordedSlots?.Clear();
            //recordedRigs?.Clear();
            Dictionary<RefID, User>.ValueCollection users = metagen_comp.World.AllUsers;
            foreach (User user in users)
            {
                //if (user == metagen_comp.World.LocalUser) continue;
                RefID user_id = user.ReferenceID;
                trackedSlots[user_id] = new List<Tuple<BodyNode, TrackedSlot>>();
                Slot rootSlot = user.Root?.Slot;

                if (record_audio_sources)
                {
                    AvatarAudioOutputManager comp = user.Root.Slot.GetComponentInChildren<AvatarAudioOutputManager>();
                    AudioOutput audio_output = comp.AudioOutput.Target;
                    Slot containingSlot = audio_output.Slot;
                    TrackedSlot trackedSlot = recordedSlots.Add();
                    trackedSlot.slot.Target = containingSlot;
                    trackedSlot.ResultType.Value = ResultTypeEnum.COPY_COMPONENTS;
                    trackedSlot.position.Value = true;
                    audioSources[user_id] = trackedSlot;
                }

                if (record_proxies)
                {
                    List<AvatarObjectSlot> components = rootSlot?.GetComponentsInChildren<AvatarObjectSlot>();
                    //WRITE the absolute time
                    foreach (AvatarObjectSlot comp in components)
                    {
                        if (comp.IsTracking.Value)
                        {
                            if (comp.Node.Value == BodyNode.LeftController || comp.Node.Value == BodyNode.RightController || comp.Node.Value == BodyNode.NONE) continue;
                            if (comp.Node == BodyNode.Root)
                            {
                                //if the driver is not in the parent, then it is in the slot (which is what happens for the root)
                                TransformStreamDriver driver = comp.Slot.GetComponent<TransformStreamDriver>();
                                TrackedSlot trackedSlot = recordedSlots.Add();
                                trackedSlots[user_id].Add(new Tuple<BodyNode, TrackedSlot>(comp.Node, trackedSlot));
                                trackedSlot.slot.Target = rootSlot;
                                //trackedSlot.rootSlot.Target = rootSlot.Parent;
                                trackedSlot.scale.Value = driver.ScaleStream.Target != null;
                                trackedSlot.position.Value = driver.PositionStream.Target != null;
                                trackedSlot.rotation.Value = driver.RotationStream.Target != null;
                            } else
                            {
                                TransformStreamDriver driver = comp.Slot.Parent.GetComponent<TransformStreamDriver>();
                                if (driver != null)
                                {
                                    TrackedSlot trackedSlot = recordedSlots.Add();
                                    trackedSlots[user_id].Add(new Tuple<BodyNode, TrackedSlot>(comp.Node, trackedSlot));
                                    trackedSlot.slot.Target = comp.Equipped?.Target.Slot;
                                    //trackedSlot.rootSlot.Target = rootSlot;
                                    trackedSlot.scale.Value = driver.ScaleStream.Target != null;
                                    trackedSlot.position.Value = driver.PositionStream.Target != null;
                                    trackedSlot.rotation.Value = driver.RotationStream.Target != null;
                                }
                            }
                        }
                    }
                    List<HandPoser> these_hand_posers = rootSlot?.GetComponentsInChildren<HandPoser>(null, excludeDisabled: false, includeLocal: false);
                    //Fingers
                    foreach (HandPoser hand_poser in these_hand_posers)
                    {
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
                                //UniLog.Log(nodee.ToString());
                                //fingerSegment.RotationDrive.Target = (IField<floatQ>) null;
                                TrackedSlot trackedSlot = recordedSlots.Add();
                                trackedSlots[user_id].Add(new Tuple<BodyNode, TrackedSlot>(nodee, trackedSlot));
                                trackedSlot.slot.Target = fingerSegment.Root.Target;
                                //trackedSlot.rootSlot.Target = rootSlot;
                                trackedSlot.rotation.Value = true;
                                trackedSlot.position.Value = true;
                            }
                        }
                    }
                }
                if (record_rigs)
                {
                    //TrackedRig newRig = recordedRigs.Add();
                    //newRig.rig.Target = rootSlot.GetComponentInChildren<Rig>();
                    //newRig.rootSlot.Target = rootSlot;
                    //newRig.position.Value = true;
                    //newRig.rotation.Value = true;
                    //newRig.scale.Value = true;
                    //trackedRigs[user_id] = newRig;
                }
                if (record_smr)
                {
                    foreach(SkinnedMeshRenderer meshRenderer in rootSlot?.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        TrackedSkinnedMeshRenderer trackedRenderer = recordedSMR.Add();
                        trackedRenderer.renderer.Target = meshRenderer;
                        trackedRenderer.recordBlendshapes.Value = true;
                        trackedRenderer.recordScales.Value = true;
                    }

                }
            }

            animation = new AnimX(1f);
            recordingUser.Target = LocalUser;
            state.Value = 1;
            _startTime.Value = base.Time.WorldTime;
            foreach (ITrackable it in recordedSMR) { it.OnStart(this); it.OnUpdate(0); }
            foreach (ITrackable it in recordedMR) { it.OnStart(this); it.OnUpdate(0); }
            foreach (ITrackable it in recordedSlots) { it.OnStart(this); it.OnUpdate(0); }
            foreach (ITrackable it in recordedFields) { it.OnStart(this); it.OnUpdate(0); }
            //foreach (ACMngr field in trackedFields) { field.OnStart(this); }
        }
        public void StopRecording()
        {
            state.Value = 2;
            Task task = StartTask(bakeAsync);
            //StartTask(async () => { await this.AttachToObject(task); });
        }
        
        public void WaitForFinish()
        {
            Task task = Task.Run(() =>
                {
                    int iter = 0;
                    while (state.Value != 0 && state.Value != 3) { Thread.Sleep(10); iter += 1; }
                });
            task.Wait();
        }

        public void CreateVisual()
        {
            Slot ruut = rootSlot.Target;
            Slot visual = ruut.AddSlot("Visual");

            visual.LocalRotation = floatQ.Euler(90f, 0f, 0f);
            visual.LocalPosition = new float3(0, 0, 0);

            PBS_Metallic material = visual.AttachComponent<PBS_Metallic>();

            visual.AttachComponent<SphereCollider>().Radius.Value = 0.025f;

            //ValueMultiplexer<color> vm = visual.AttachComponent<ValueMultiplexer<color>>();
            //vm.Target.Target = material.EmissiveColor;
            //vm.Values.Add(new color(0, 0.5f, 0, 1));
            //vm.Values.Add(new color(0.5f, 0, 0, 1));
            //vm.Values.Add(new color(0.5f, 0.5f, 0, 1));
            //vm.Values.Add(new color(0, 0, 0.5f, 1));
            //vm.Index.DriveFrom<int>(state);

            SphereMesh mesh = visual.AttachMesh<SphereMesh>(material);
            mesh.Radius.Value = 0.07f;

            visual.AttachComponent<Grabbable>();
            PhysicalButton button = visual.AttachComponent<PhysicalButton>();
            FrooxEngine.LogiX.Interaction.ButtonEvents touchableEvents = visual.AttachComponent<FrooxEngine.LogiX.Interaction.ButtonEvents>();
            FrooxEngine.LogiX.ReferenceNode<IButton> refNode = ruut.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IButton>>();
            refNode.RefTarget.Target = button;
            touchableEvents.Button.Target = refNode;
            touchableEvents.Pressed.Target = animator.Play;
        }

        public void AttachToObjects(Dictionary<RefID,Dictionary<BodyNode,Slot>> slots, Dictionary<RefID,List<Slot>> bones)
        {
            Slot ruut = rootSlot.Target;
            animator = ruut.AttachComponent<Animator>();
            animator.Clip.Target = _result.Target;
            foreach (ITrackable it in recordedSMR) { it.OnReplace(animator); }
            foreach (ITrackable it in recordedMR) { it.OnReplace(animator); }
            foreach (ITrackable it in recordedSlots) { it.OnReplace(animator); }
            foreach (ITrackable it in recordedFields) { it.OnReplace(animator); }

            CreateVisual();
            FrooxEngine.LogiX.Playback.PlaybackReadState playbackState = ruut.AttachComponent<FrooxEngine.LogiX.Playback.PlaybackReadState>();
            FrooxEngine.LogiX.ReferenceNode<IPlayable> refNodeState = ruut.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IPlayable>>();
            refNodeState.RefTarget.Target = animator;
            playbackState.Source.Target = refNodeState;
            //AUDIO PLAY
            UniLog.Log("Attaching audio to exported!");
            string reading_directory = dataManager.GetRecordingForWorld(metagen_comp.World, 0);
            foreach(var item in audioSources)
            {
                RefID user_id = item.Key;
                TrackedSlot trackedSlot = item.Value;
                AudioOutput audio_output = trackedSlot.newSlot.Target.GetComponent<AudioOutput>();
                string[] files2 = Directory.GetFiles(reading_directory, user_id.ToString() + "*_voice.ogg");
                String audio_file2 = files2.Length > 0 ? files2[0] : null;
                if (File.Exists(audio_file2))
                {
                    if (audio_output == null) audio_output = trackedSlot.newSlot.Target.AttachComponent<AudioOutput>();
                    audio_output.Volume.Value = 1f;
                    audio_output.Enabled = true;
                    Uri uri = this.World.Engine.LocalDB.ImportLocalAsset(audio_file2, LocalDB.ImportLocation.Original, (string)null);
                    StaticAudioClip audioClip = audio_output.Slot.AttachAudioClip(uri);
                    AudioClipPlayer player = audio_output.Slot.AttachComponent<AudioClipPlayer>();
                    player.Clip.Target = (IAssetProvider<AudioClip>) audioClip;
                    audio_output.Source.Target = (IAudioSource) player;
                    audio_output.Slot.AttachComponent<AudioMetadata>(true, (Action<AudioMetadata>)null).SetFromCurrentWorld();
                    FrooxEngine.LogiX.Actions.DrivePlaybackNode playbackDrive = ruut.AttachComponent<FrooxEngine.LogiX.Actions.DrivePlaybackNode>();
                    playbackDrive.NormalizedPosition.Target = playbackState.NormalizedPosition;
                    playbackDrive.Play.Target = playbackState.IsPlaying;
                    playbackDrive.Loop.Target = playbackState.Loop;
                    FrooxEngine.LogiX.ReferenceNode<SyncPlayback> refNode = ruut.AttachComponent<FrooxEngine.LogiX.ReferenceNode<SyncPlayback>>();
                    refNode.RefTarget.Target = (SyncPlayback) player.GetSyncMember(4);
                    playbackDrive.DriveTarget.Target = refNode;
                    DriveRef<SyncPlayback> driveRef = (DriveRef<SyncPlayback>)playbackDrive.GetSyncMember(13);
                    driveRef.Target = (SyncPlayback)player.GetSyncMember(4);
                }
            }
            Slot hearing_slot = rootSlot.Target.AddSlot("heard sound");
            AudioOutput audio_output2 = hearing_slot.GetComponent<AudioOutput>();
            string[] files = Directory.GetFiles(reading_directory, "*_hearing.ogg");
            String audio_file = files.Length > 0 ? files[0] : null;
            if (File.Exists(audio_file))
            {
                if (audio_output2 == null) audio_output2 = hearing_slot.AttachComponent<AudioOutput>();
                audio_output2.Volume.Value = 1f;
                audio_output2.Enabled = true;
                Uri uri = this.World.Engine.LocalDB.ImportLocalAsset(audio_file, LocalDB.ImportLocation.Original, (string)null);
                StaticAudioClip audioClip = audio_output2.Slot.AttachAudioClip(uri);
                AudioClipPlayer player = audio_output2.Slot.AttachComponent<AudioClipPlayer>();
                player.Clip.Target = (IAssetProvider<AudioClip>) audioClip;
                audio_output2.Source.Target = (IAudioSource) player;
                audio_output2.Slot.AttachComponent<AudioMetadata>(true, (Action<AudioMetadata>)null).SetFromCurrentWorld();
                FrooxEngine.LogiX.Actions.DrivePlaybackNode playbackDrive = ruut.AttachComponent<FrooxEngine.LogiX.Actions.DrivePlaybackNode>();
                playbackDrive.NormalizedPosition.Target = playbackState.NormalizedPosition;
                playbackDrive.Play.Target = playbackState.IsPlaying;
                playbackDrive.Loop.Target = playbackState.Loop;
                FrooxEngine.LogiX.ReferenceNode<SyncPlayback> refNode = ruut.AttachComponent<FrooxEngine.LogiX.ReferenceNode<SyncPlayback>>();
                refNode.RefTarget.Target = (SyncPlayback) player.GetSyncMember(4);
                playbackDrive.DriveTarget.Target = refNode;
                DriveRef<SyncPlayback> driveRef = (DriveRef<SyncPlayback>)playbackDrive.GetSyncMember(13);
                driveRef.Target = (SyncPlayback)player.GetSyncMember(4);
            }
            foreach (ITrackable it in recordedSMR) { it.Clean(); }
            foreach (ITrackable it in recordedMR) { it.Clean(); }
            foreach (ITrackable it in recordedSlots) { it.Clean(); }
            foreach (ITrackable it in recordedFields) { it.Clean(); }

            trackedSlots = new Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>>();
            audioSources = new Dictionary<RefID, TrackedSlot>();
            //recordedSlots?.Clear();
            //recordedRigs?.Clear();
            state.Value = 0;
        }

        public void RecordFrame()
        {
            //base.OnCommonUpdate();
            if (state.Value != 1) return;
            User usr = recordingUser.Target;
            if (usr == LocalUser)
            {
                float t = (float)(base.Time.WorldTime - _startTime);
                foreach (ITrackable it in recordedSMR) { it.OnUpdate(t); }
                foreach (ITrackable it in recordedMR) { it.OnUpdate(t); }
                foreach (ITrackable it in recordedSlots) { it.OnUpdate(t); }
                foreach (ITrackable it in recordedFields) { it.OnUpdate(t); }
            }
        }

        protected async Task bakeAsync()
        {
            Slot root = rootSlot.Target;
            float t = (float)(base.Time.WorldTime - _startTime);
            animation.GlobalDuration = t;

            foreach (ITrackable it in recordedSMR) { it.OnUpdate(t); it.OnStop(); }
            foreach (ITrackable it in recordedMR) { it.OnUpdate(t); it.OnStop(); }
            foreach (ITrackable it in recordedSlots) { it.OnUpdate(t); it.OnStop(); }
            foreach (ITrackable it in recordedFields) { it.OnUpdate(t); it.OnStop(); }
            await default(ToBackground);

            string tempFilePath = Engine.LocalDB.GetTempFilePath("animx");
            animation.SaveToFile(tempFilePath);
            Uri uri = Engine.LocalDB.ImportLocalAsset(tempFilePath, LocalDB.ImportLocation.Move);

            await default(ToWorld);
            _result.Target = (root ?? Slot).AttachComponent<StaticAnimationProvider>();
            _result.Target.URL.Value = uri;
            state.Value = 3;
        }
    }
}
