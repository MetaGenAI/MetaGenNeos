/*
 * Based on the recording tool by Lucas and Jeana (https://github.com/jeanahelver/NeosAnimationToolset)
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

        private Slot hearing_slot;
        private Task bakeAsyncTask;

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
                if (!metagen_comp.record_local_user && user == metagen_comp.World.LocalUser) continue;
                RefID user_id = user.ReferenceID;
                Slot rootSlot = user.Root?.Slot;
                SimpleAvatarProtection protectionComponent = rootSlot?.GetComponentInChildren<SimpleAvatarProtection>();
                if (protectionComponent != null) continue;
                trackedSlots[user_id] = new List<Tuple<BodyNode, TrackedSlot>>();

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
                        if (meshRenderer.Enabled && meshRenderer.Slot.IsActive)
                        {
                            TrackedSkinnedMeshRenderer trackedRenderer = recordedSMR.Add();
                            trackedRenderer.renderer.Target = meshRenderer;
                            trackedRenderer.recordBlendshapes.Value = true;
                            trackedRenderer.recordScales.Value = true;
                        }
                    }

                    foreach(MeshRenderer meshRenderer in rootSlot?.GetComponentInChildren<AvatarRoot>().Slot.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (meshRenderer.Enabled && meshRenderer.Slot.IsActive && !(meshRenderer is SkinnedMeshRenderer))
                        {
                            if (meshRenderer.Slot.GetComponent<InteractionLaser>() != null) continue;
                            if (meshRenderer.Slot.GetComponentInParents<InteractionLaser>() != null) continue;
                            TrackedMeshRenderer trackedRenderer = recordedMR.Add();
                            trackedRenderer.renderer.Target = meshRenderer;
                            trackedRenderer.recordScales.Value = true;
                        }
                    }

                }
            }

            animation = new AnimX(1f);
            recordingUser.Target = LocalUser;
            _startTime.Value = base.Time.WorldTime;
            state.Value = 1;
            foreach (ITrackable it in recordedSMR) { it.OnStart(this); it.OnUpdate(0); }
            foreach (ITrackable it in recordedMR) { it.OnStart(this); it.OnUpdate(0); }
            foreach (ITrackable it in recordedSlots) { it.OnStart(this); it.OnUpdate(0); }
            foreach (ITrackable it in recordedFields) { it.OnStart(this); it.OnUpdate(0); }
            //foreach (ACMngr field in trackedFields) { field.OnStart(this); }
        }
        public void PreStopRecording()
        {
            state.Value = 2;
            bakeAsyncTask = StartTask(async () => { 
                try
                {
                    await this.bakeAsync();
                } catch (Exception e)
                {
                    UniLog.Log("OwO error in bakeAsync: " + e.Message);
                    UniLog.Log(e.StackTrace);
                }
            });
        }
        public void StopRecording()
        {
            StartTask(async () => { 
                try
                {
                    await this.AttachToObject(bakeAsyncTask);
                } catch (Exception e)
                {
                    UniLog.Log("OwO error in AttachToObject: " + e.Message);
                    UniLog.Log(e.StackTrace);
                    state.Value = 4; //error
                }
            });
        }
        
        public void WaitForFinish()
        {
            int MAX_ITERS = 100000;
            Task task = Task.Run(() =>
                {
                    int iter = 0;
                    while (state.Value != 0 && state.Value != 4 && iter < MAX_ITERS) { Thread.Sleep(10); iter += 1; }
                });
            task.Wait();
        }

        public void CreateVisual()
        {
            UniLog.Log("Creating visual");
            //Slot ruut = rootSlot.Target;
            Slot holder = World.RootSlot.AddSlot("Animation");
            holder.LocalRotation = floatQ.Euler(0f, 0f, 0f);
            holder.LocalPosition = new float3(0, 1.5f, 0);
            Slot visual = holder.AddSlot("Main button");

            visual.LocalRotation = floatQ.Euler(0f, 0f, 0f);
            visual.LocalPosition = new float3(0, 0f, 0);

            PBS_Metallic material = visual.AttachComponent<PBS_Metallic>();
            material.AlbedoColor.Value = RandomX.RGB;
            material.EmissiveColor.Value = RandomX.RGB;

            visual.AttachComponent<SphereCollider>().Radius.Value = 0.07f;

            SphereMesh mesh = visual.AttachMesh<SphereMesh>(material);
            mesh.Radius.Value = 0.07f;

            holder.AttachComponent<Grabbable>();
            PhysicalButton button = visual.AttachComponent<PhysicalButton>();
            FrooxEngine.LogiX.Interaction.ButtonEvents touchableEvents = visual.AttachComponent<FrooxEngine.LogiX.Interaction.ButtonEvents>();
            FrooxEngine.LogiX.ReferenceNode<IButton> refNode = visual.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IButton>>();
            refNode.RefTarget.Target = button;
            touchableEvents.Button.Target = refNode;
            touchableEvents.Pressed.Target = animator.Play;

            UniLog.Log("Creating expand/collapse button");
            CreateExpandCollapseButton(holder);
            UniLog.Log("Creating parent/unparent button");
            CreateParentUnparentButton(holder);
            UniLog.Log("Creating on off hearing button");
            CreateActivateDeactivateHearingButton(holder);
            UniLog.Log("Done creating Animation control object");
        }
        private void CreateExpandCollapseButton(Slot holder)
        {
            Slot button_holder = holder.AddSlot("Expand collapse button");

            button_holder.LocalRotation = floatQ.Euler(0f, 0f, 0f);
            button_holder.LocalPosition = new float3(0.1f, 0, 0);

            button_holder.AttachComponent<BoxCollider>().Size.Value = new float3(0.03f, 0.03f, 0.03f);

            PBS_Metallic material = holder.AttachComponent<PBS_Metallic>();
            material.AlbedoColor.Value = new color(215f, 224f, 45f)/255f;
            BoxMesh mesh2 = button_holder.AttachMesh<BoxMesh>(material);
            mesh2.Size.Value = new float3(0.03f, 0.03f, 0.03f);

            PhysicalButton button2 = button_holder.AttachComponent<PhysicalButton>();
            FrooxEngine.LogiX.Interaction.ButtonEvents touchableEvents2 = button_holder.AttachComponent<FrooxEngine.LogiX.Interaction.ButtonEvents>();
            FrooxEngine.LogiX.ReferenceNode<IButton> refNode2 = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IButton>>();
            refNode2.RefTarget.Target = button2;
            touchableEvents2.Button.Target = refNode2;

            FrooxEngine.LogiX.Animation.Tweening.TweenValueNode<float3> tween = button_holder.AttachComponent<FrooxEngine.LogiX.Animation.Tweening.TweenValueNode<float3>>();
            FrooxEngine.LogiX.ReferenceNode<IField<float3>> refNode3 = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IField<float3>>>();
            refNode3.RefTarget.Target = rootSlot.Target.Scale_Field;
            tween.Target.Target = refNode3;
            ValueField<float3> valueField1 = button_holder.AttachComponent<ValueField<float3>>();
            valueField1.Value.Value = new float3(1f, 1f, 1f);
            ValueField<float3> valueField2 = button_holder.AttachComponent<ValueField<float3>>();
            valueField2.Value.Value = new float3(0f, 0f, 0f);
            ValueField<float> tweenDuration = button_holder.AttachComponent<ValueField<float>>();
            tweenDuration.Value.Value = 1.0f;
            tween.From.Target = valueField1.Value;
            tween.To.Target = valueField2.Value;
            tween.Duration.Target = tweenDuration.Value;

            FrooxEngine.LogiX.Animation.Tweening.TweenValueNode<float3> tweenUp = button_holder.AttachComponent<FrooxEngine.LogiX.Animation.Tweening.TweenValueNode<float3>>();
            FrooxEngine.LogiX.ReferenceNode<IField<float3>> refNode3b = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IField<float3>>>();
            refNode3b.RefTarget.Target = rootSlot.Target.Scale_Field;
            tweenUp.Target.Target = refNode3b;
            ValueField<float3> tweenUpFrom = button_holder.AttachComponent<ValueField<float3>>();
            tweenUpFrom.Value.Value = new float3(0f, 0f, 0f);
            ValueField<float3> tweenUpTo = button_holder.AttachComponent<ValueField<float3>>();
            tweenUpTo.Value.Value = new float3(1f, 1f, 1f);
            ValueField<float> tweenUpDuration = button_holder.AttachComponent<ValueField<float>>();
            tweenUpDuration.Value.Value = 1.0f;
            tweenUp.From.Target = tweenUpFrom.Value;
            tweenUp.To.Target = tweenUpTo.Value;
            tweenUp.Duration.Target = tweenUpDuration.Value;

            FrooxEngine.LogiX.WorldModel.SetParent setParent = button_holder.AttachComponent<FrooxEngine.LogiX.WorldModel.SetParent>();
            //FrooxEngine.LogiX.IReferenceNode refNode4 = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<Slot>>();
            FrooxEngine.LogiX.ReferenceNode<Slot> referenceNode = (FrooxEngine.LogiX.ReferenceNode<Slot>) FrooxEngine.LogiX.LogixHelper.GetReferenceNode(rootSlot.Target, Slot.GetType());
            FrooxEngine.LogiX.ReferenceNode<Slot> referenceNode2 = (FrooxEngine.LogiX.ReferenceNode<Slot>) FrooxEngine.LogiX.LogixHelper.GetReferenceNode(holder, Slot.GetType());
            //refNode4.SetRefTarget(rootSlot.Target);
            setParent.Instance.Target = referenceNode;
            setParent.NewParent.Target = referenceNode2;
            referenceNode2.Slot.SetParent(button_holder);
            referenceNode2.ActiveVisual.Destroy();
            referenceNode.Slot.SetParent(button_holder);
            referenceNode.ActiveVisual.Destroy();

            FrooxEngine.LogiX.WorldModel.SetParent setParent2 = button_holder.AttachComponent<FrooxEngine.LogiX.WorldModel.SetParent>();
            //FrooxEngine.LogiX.IReferenceNode refNode4 = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<Slot>>();
            FrooxEngine.LogiX.ReferenceNode<Slot> referenceNodeB = (FrooxEngine.LogiX.ReferenceNode<Slot>) FrooxEngine.LogiX.LogixHelper.GetReferenceNode(rootSlot.Target, Slot.GetType());
            //refNode4.SetRefTarget(rootSlot.Target);
            setParent2.Instance.Target = referenceNodeB;
            referenceNodeB.Slot.SetParent(button_holder);
            referenceNodeB.ActiveVisual.Destroy();
            FrooxEngine.LogiX.WorldModel.RootSlot rootSlotNode = button_holder.AttachComponent<FrooxEngine.LogiX.WorldModel.RootSlot>();
            setParent2.NewParent.Target = rootSlotNode;
            setParent2.OnDone.Target = tweenUp.Tween;

            FrooxEngine.LogiX.ProgramFlow.IfNode ifNode1 = button_holder.AttachComponent<FrooxEngine.LogiX.ProgramFlow.IfNode>();
            touchableEvents2.Pressed.Target = ifNode1.Run;
            ifNode1.True.Target = setParent.DoSetParent;
            ifNode1.False.Target = setParent2.DoSetParent;
            setParent.OnDone.Target = tween.Tween;

            FrooxEngine.LogiX.ProgramFlow.BooleanToggle booleanToggle = button_holder.AttachComponent<FrooxEngine.LogiX.ProgramFlow.BooleanToggle>();
            booleanToggle.State.Value = true;
            ifNode1.Condition.Target = booleanToggle.State;


            tween.OnDone.Target = booleanToggle.Toggle;
            tweenUp.OnDone.Target = booleanToggle.Toggle;

            //FrooxEngine.LogiX.ReferenceNode.PrefixReferenceNode((IWorldElement)field, field.GetType())
            //refNode4.RefTarget.Target = rootSlot.Target;
            //FrooxEngine.LogiX.ReferenceNode<Slot> refNode5 = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<Slot>>();
            //refNode5.RefTarget.Target = holder;
            //setParent.Instance.Target = refNode4;
            //setParent.NewParent.Target = refNode5;

            //tween.OnDone.Target = setParent.DoSetParent;
        }


        private void CreateParentUnparentButton(Slot holder)
        {
            Slot button_holder = holder.AddSlot("Parent unparent button");

            button_holder.LocalRotation = floatQ.Euler(0f, 0f, 0f);
            button_holder.LocalPosition = new float3(0.15f, 0, 0);

            button_holder.AttachComponent<BoxCollider>().Size.Value = new float3(0.03f, 0.03f, 0.03f);

            PBS_Metallic material = holder.AttachComponent<PBS_Metallic>();
            BoxMesh mesh2 = button_holder.AttachMesh<BoxMesh>(material);
            material.AlbedoColor.Value = new color(28f, 214f, 84f)/255f;
            mesh2.Size.Value = new float3(0.03f, 0.03f, 0.03f);

            PhysicalButton button2 = button_holder.AttachComponent<PhysicalButton>();
            FrooxEngine.LogiX.Interaction.ButtonEvents touchableEvents2 = button_holder.AttachComponent<FrooxEngine.LogiX.Interaction.ButtonEvents>();
            FrooxEngine.LogiX.ReferenceNode<IButton> refNode2 = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IButton>>();
            refNode2.RefTarget.Target = button2;
            touchableEvents2.Button.Target = refNode2;

            FrooxEngine.LogiX.WorldModel.SetParent setParent = button_holder.AttachComponent<FrooxEngine.LogiX.WorldModel.SetParent>();
            FrooxEngine.LogiX.ReferenceNode<Slot> referenceNode = (FrooxEngine.LogiX.ReferenceNode<Slot>) FrooxEngine.LogiX.LogixHelper.GetReferenceNode(rootSlot.Target, Slot.GetType());
            FrooxEngine.LogiX.ReferenceNode<Slot> referenceNode2 = (FrooxEngine.LogiX.ReferenceNode<Slot>) FrooxEngine.LogiX.LogixHelper.GetReferenceNode(holder, Slot.GetType());
            setParent.Instance.Target = referenceNode;
            setParent.NewParent.Target = referenceNode2;
            referenceNode2.Slot.SetParent(button_holder);
            referenceNode2.ActiveVisual.Destroy();
            referenceNode.Slot.SetParent(button_holder);
            referenceNode.ActiveVisual.Destroy();

            FrooxEngine.LogiX.WorldModel.SetParent setParent2 = button_holder.AttachComponent<FrooxEngine.LogiX.WorldModel.SetParent>();
            FrooxEngine.LogiX.ReferenceNode<Slot> referenceNodeB = (FrooxEngine.LogiX.ReferenceNode<Slot>) FrooxEngine.LogiX.LogixHelper.GetReferenceNode(rootSlot.Target, Slot.GetType());
            setParent2.Instance.Target = referenceNodeB;
            referenceNodeB.Slot.SetParent(button_holder);
            referenceNodeB.ActiveVisual.Destroy();
            FrooxEngine.LogiX.WorldModel.RootSlot rootSlotNode = button_holder.AttachComponent<FrooxEngine.LogiX.WorldModel.RootSlot>();
            setParent2.NewParent.Target = rootSlotNode;

            FrooxEngine.LogiX.ProgramFlow.IfNode ifNode1 = button_holder.AttachComponent<FrooxEngine.LogiX.ProgramFlow.IfNode>();
            touchableEvents2.Pressed.Target = ifNode1.Run;
            ifNode1.True.Target = setParent.DoSetParent;
            ifNode1.False.Target = setParent2.DoSetParent;

            FrooxEngine.LogiX.ProgramFlow.BooleanToggle booleanToggle = button_holder.AttachComponent<FrooxEngine.LogiX.ProgramFlow.BooleanToggle>();
            booleanToggle.State.Value = true;
            ifNode1.Condition.Target = booleanToggle.State;

            setParent2.OnDone.Target = booleanToggle.Toggle;
            setParent.OnDone.Target = booleanToggle.Toggle;
        }

        private void CreateActivateDeactivateHearingButton(Slot holder)
        {
            Slot button_holder = holder.AddSlot("Hearing on off button");

            button_holder.LocalRotation = floatQ.Euler(0f, 0f, 0f);
            button_holder.LocalPosition = new float3(0.2f, 0, 0);

            button_holder.AttachComponent<BoxCollider>().Size.Value = new float3(0.03f, 0.03f, 0.03f);

            PBS_Metallic material = holder.AttachComponent<PBS_Metallic>();
            BoxMesh mesh2 = button_holder.AttachMesh<BoxMesh>(material);
            material.AlbedoColor.Value = new color(100f, 100f, 100f)/255f;
            mesh2.Size.Value = new float3(0.03f, 0.03f, 0.03f);

            PhysicalButton button2 = button_holder.AttachComponent<PhysicalButton>();
            FrooxEngine.LogiX.Interaction.ButtonEvents touchableEvents2 = button_holder.AttachComponent<FrooxEngine.LogiX.Interaction.ButtonEvents>();
            FrooxEngine.LogiX.ReferenceNode<IButton> refNode2 = button_holder.AttachComponent<FrooxEngine.LogiX.ReferenceNode<IButton>>();
            refNode2.RefTarget.Target = button2;
            touchableEvents2.Button.Target = refNode2;

            FrooxEngine.LogiX.ProgramFlow.BooleanToggle booleanToggle = button_holder.AttachComponent<FrooxEngine.LogiX.ProgramFlow.BooleanToggle>();
            booleanToggle.State.Value = true;

            touchableEvents2.Pressed.Target = booleanToggle.Toggle;

            ValueDriver<bool> hearingDriver = button_holder.AttachComponent<ValueDriver<bool>>();
            hearingDriver.DriveTarget.Target = hearing_slot.GetComponent<AudioOutput>()?.EnabledField;
            hearingDriver.ValueSource.Target = booleanToggle.State;
        }

        public async Task AttachToObject(Task task)
        {
            await task;
            Slot ruut = rootSlot.Target;
            animator = ruut.AttachComponent<Animator>();
            animator.Clip.Target = _result.Target;
            foreach (ITrackable it in recordedSMR) { it.OnReplace(animator); }
            foreach (ITrackable it in recordedMR) { it.OnReplace(animator); }
            foreach (ITrackable it in recordedSlots) { it.OnReplace(animator); }
            foreach (ITrackable it in recordedFields) { it.OnReplace(animator); }

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
            hearing_slot = rootSlot.Target.AddSlot("heard sound");
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

            CreateVisual();
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
            try
            {
                if (state.Value != 1) return;
                User usr = recordingUser.Target;
                if (usr == LocalUser)
                {
                    float t = (float)(base.Time.WorldTime - _startTime);
                    if (t < 0) return;
                    foreach (ITrackable it in recordedSMR) { it.OnUpdate(t); }
                    foreach (ITrackable it in recordedMR) { it.OnUpdate(t); }
                    foreach (ITrackable it in recordedSlots) { it.OnUpdate(t); }
                    foreach (ITrackable it in recordedFields) { it.OnUpdate(t); }
                }
            } catch (Exception e)
            {
                UniLog.Log("OwO error in RecordFrame of AnimationRecorder: " + e.Message);
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
