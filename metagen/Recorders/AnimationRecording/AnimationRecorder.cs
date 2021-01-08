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

namespace metagen
{
    public partial class AnimationRecorder : Component, IRecorder
    {
        public readonly SyncRef<User> recordingUser;

        public readonly Sync<int> state;

        public readonly Sync<double> _startTime;

        public AnimX animation;

        public readonly SyncRef<Slot> rootSlot;
        public readonly SyncRef<Slot> componentHoldingSlot;
        public readonly Sync<bool> replaceRefs;

        public readonly SyncList<TrackedRig> recordedRigs;

        public readonly SyncList<TrackedSlot> recordedSlots;

        public readonly SyncRef<StaticAnimationProvider> _result;

        public string saving_folder;
        public MetaGen metagen_comp;
        public Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>> trackedSlots = new Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>>();
        public Dictionary<RefID, TrackedRig> trackedRigs = new Dictionary<RefID, TrackedRig>();

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
            bool record_proxies = false;
            bool record_rigs = true;
            trackedSlots = new Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>>();
            recordedSlots?.Clear();
            recordedRigs?.Clear();
            Dictionary<RefID, User>.ValueCollection users = metagen_comp.World.AllUsers;
            foreach (User user in users)
            {
                //if (user == metagen_comp.World.LocalUser) continue;
                RefID user_id = user.ReferenceID;
                trackedSlots[user_id] = new List<Tuple<BodyNode, TrackedSlot>>();
                Slot rootSlot = user.Root?.Slot;

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
                                trackedSlot.rootSlot.Target = rootSlot.Parent;
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
                                    trackedSlot.rootSlot.Target = rootSlot;
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
                                trackedSlot.rootSlot.Target = rootSlot;
                                trackedSlot.rotation.Value = true;
                                trackedSlot.position.Value = true;
                            }
                        }
                    }
                }
                if (record_rigs)
                {
                    TrackedRig newRig = recordedRigs.Add();
                    newRig.rig.Target = rootSlot.GetComponentInChildren<Rig>();
                    newRig.rootSlot.Target = rootSlot;
                    newRig.position.Value = true;
                    newRig.rotation.Value = true;
                    newRig.scale.Value = true;
                    trackedRigs[user_id] = newRig;
                }
            }

            animation = new AnimX(1f);
            recordingUser.Target = LocalUser;
            state.Value = 1;
            _startTime.Value = base.Time.WorldTime;
            foreach (TrackedRig rig in recordedRigs) { rig.OnStart(this); }
            foreach (TrackedSlot slot in recordedSlots) { slot.OnStart(this); }
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

        public void AttachToObjects(Dictionary<RefID,Dictionary<BodyNode,Slot>> slots, Dictionary<RefID,List<Slot>> bones)
        {
            //await bake_task;
            Animator animator = componentHoldingSlot.Target.AttachComponent<Animator>();
            animator.Clip.Target = _result.Target;

            //Dictionary<RefID, User>.ValueCollection users = metagen_comp.World.AllUsers;
            //foreach (User user in users)
            //{
            //    FrooxEngine.CommonAvatar.AvatarManager avatarManager = user.Root.Slot.GetComponentInChildren<FrooxEngine.CommonAvatar.AvatarManager>();
                //Slot avatarRoot = user.Root.Slot.GetComponentInChildren<AvatarRoot>().Slot;
                //Slot newAvatarRoot = avatarRoot.Duplicate();
                //newAvatarRoot.SetParent(metagen_comp.World.RootSlot);
                //avatarManager.Equip(newAvatarRoot);
            //}
            //foreach (TrackedRig rig in recordedRigs) { rig.OnReplace(animator); }
            //foreach (TrackedSlot slot in recordedSlots) { slot.OnReplace(animator); }
            foreach (var item in trackedSlots)
            {
                RefID user_id = item.Key;
                Slot root = null;
                foreach(var item2 in item.Value)
                {
                    BodyNode node = item2.Item1;
                    if (node == BodyNode.Root) root = slots[user_id][node];
                }
                foreach(var item2 in item.Value)
                {
                    BodyNode node = item2.Item1;
                    TrackedSlot trackedSlot = item2.Item2;
                    if (slots[user_id].ContainsKey(node))
                        trackedSlot.OnReplace(root,slots[user_id][node], animator, node != BodyNode.Root);
                    else
                        trackedSlot.OnReplace(root,null, animator, node != BodyNode.Root);
                }
            }
            foreach (var item in trackedRigs)
            {
                RefID user_id = item.Key;
                Slot root = slots[user_id][BodyNode.Root];
                TrackedRig rig = item.Value;
                //Rig avatarRig = bones[user_id][0].GetComponentInParents<Rig>();
                //avatarRig.Slot.RemoveComponent(avatarRig);
                //FrooxEngine.FinalIK.VRIK avatarIK = bones[user_id][0].GetComponentInParents<FrooxEngine.FinalIK.VRIK>();
                //avatarIK.Slot.RemoveComponent(avatarIK);
                List<SkinnedMeshRenderer> meshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
                rig.OnReplace(root, meshRenderers, animator);
            }

            //foreach (ACMngr field in trackedFields) { field.OnStop(); }
            trackedSlots = new Dictionary<RefID, List<Tuple<BodyNode, TrackedSlot>>>();
            recordedSlots?.Clear();
            recordedRigs?.Clear();
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
                foreach (TrackedRig rig in recordedRigs) { rig.OnUpdate(t); }
                foreach (TrackedSlot slot in recordedSlots) { slot.OnUpdate(t); }
                //foreach (ACMngr field in trackedFields) { field.OnUpdate(t); }
            }
        }

        protected async Task bakeAsync()
        {
            float t = (float)(base.Time.WorldTime - _startTime);
            animation.GlobalDuration = t;

            foreach (TrackedRig rig in recordedRigs) { rig.OnStop(); }
            foreach (TrackedSlot slot in recordedSlots) { slot.OnStop(); }
            //foreach (ACMngr field in trackedFields) { field.OnStop(); }
            await default(ToBackground);

            string tempFilePath = Engine.LocalDB.GetTempFilePath("animx");
            animation.SaveToFile(tempFilePath);
            Uri uri = Engine.LocalDB.ImportLocalAsset(tempFilePath, LocalDB.ImportLocation.Move);

            await default(ToWorld);
            _result.Target = componentHoldingSlot.Target.AttachComponent<StaticAnimationProvider>();
            _result.Target.URL.Value = uri;
            if (replaceRefs.Value)
                state.Value = 3;
            else
                state.Value = 0;
        }
    }
    public interface Trackable
    {
        void OnStart(AnimationRecorder rt);
        void OnUpdate(float T);
        void OnStop();
        //void OnReplace(Slot s, Animator anim);
        void Clean();
    }
    public class TrackedRig : SyncObject, Trackable
    {
        public readonly SyncRef<Rig> rig;
        public readonly Sync<bool> position;
        public readonly Sync<bool> rotation;
        public readonly Sync<bool> scale;
        public readonly SyncRef<AnimationRecorder> _rt;
        public readonly SyncRefList<Bonez> _trackedBones;
        public readonly SyncRef<Slot> rootSlot;
        //public Bonez[] bonezs;


        public void OnStart(AnimationRecorder rt)
        {
            if (rig.Target == null) return;
            _rt.Target = rt;
            bool pos = position.Value;
            bool rot = rotation.Value;
            bool scl = scale.Value;
            //bonezs = new Bonez[rig.Target.Bones.Count];
            foreach (Slot bone in rig.Target.Bones)
            {
                Bonez b = new Bonez();
                World.ReferenceController.LocalAllocationBlockBegin();
                b.Initialize(World, _trackedBones);
                World.ReferenceController.LocalAllocationBlockEnd();
                _trackedBones.Add(b);
                b.OnStart(this, bone, pos, rot, scl);
                b.rootSlot.Target = rootSlot.Target;
            }
        }
        public void OnUpdate(float T)
        {
            foreach (Bonez b in _trackedBones)
            {
                b?.OnUpdate(T);
            }
        }
        public void OnStop() { }
        public class Bonez : SyncObject, ICustomInspector
        {
            public readonly SyncRef<Slot> slot;
            public TrackedRig rig;
            public CurveFloat3AnimationTrack positionTrack;
            public CurveFloatQAnimationTrack rotationTrack;
            public CurveFloat3AnimationTrack scaleTrack;
            public readonly SyncRef<Slot> rootSlot;
            public BodyNode bodyNode;

            public void OnStart(TrackedRig r, Slot sloot, bool position, bool rotation, bool scale)
            {
                rig = r;
                slot.Target = sloot;
                if (position) positionTrack = r._rt.Target.animation.AddTrack<CurveFloat3AnimationTrack>();
                if (rotation) rotationTrack = r._rt.Target.animation.AddTrack<CurveFloatQAnimationTrack>();
                if (scale) scaleTrack = r._rt.Target.animation.AddTrack<CurveFloat3AnimationTrack>();
            }
            public void OnUpdate(float T)
            {
                //Slot ruut = rig._rt.Target.rootSlot.Target;
                Slot ruut = rootSlot.Target;
                positionTrack?.InsertKeyFrame(ruut.GlobalPointToLocal(slot.Target?.GlobalPosition ?? float3.Zero), T);
                rotationTrack?.InsertKeyFrame(ruut.GlobalRotationToLocal(slot.Target?.GlobalRotation ?? floatQ.Identity), T);
                scaleTrack?.InsertKeyFrame(ruut.GlobalVectorToLocal(slot.Target?.GlobalScale ?? float3.Zero), T);
            }
            public void OnStop()
            {

            }
            public void BuildInspectorUI(UIBuilder ui)
            {
                ui.PushStyle();
                ui.Style.MinHeight = 24f;
                ui.Panel();
                ui.Text("<Tracked slot>");
                ui.NestOut();
                ui.PopStyle();
            }
            public void OnReplace(Slot root, List<SkinnedMeshRenderer> meshRenderers, int index, Animator anim, bool add_proxy=false)
            {
                //Slot root = _rt.Target.rootSlot.Target;
                //Slot root = rootSlot.Target;
                Slot s = null;
                bool created_proxy = false;
                foreach(SkinnedMeshRenderer meshRenderer in meshRenderers)
                {
                    SyncRefList<Slot> bones = meshRenderer.Bones;
                    if (index < bones.Count)
                    {
                        Slot s1 = bones[index];
                        if (add_proxy && s1 != null)
                        {
                            if (!created_proxy)
                            {
                                s = root.AddSlot(s1.Name);
                                created_proxy = true;
                                //CopyGlobalTransform comp = s1.AttachComponent<CopyGlobalTransform>();
                                //comp.Source.Target = s;
                            }
                            bones[index] = s;
                        } else
                        {
                            s = s1;
                        }
                    }
                }
                //Slot s = slot.Target;
                if (positionTrack != null) { anim.Fields.Add().Target = s?.Position_Field; }
                if (rotationTrack != null) { anim.Fields.Add().Target = s?.Rotation_Field; }
                if (scaleTrack != null) { anim.Fields.Add().Target = s?.Scale_Field; }
                //World.ReplaceReferenceTargets(slot, s, true);
                //World.ForeachWorldElement(delegate (ISyncRef syncRef) {
                //    if (syncRef.Target == slot)
                //        syncRef.Target = s;
                //}, root);
            }
            public void Clean()
            {
                positionTrack = null; rotationTrack = null; scaleTrack = null;
            }
        }
        public void OnReplace(Slot root, List<SkinnedMeshRenderer> meshRenderers, Animator anim)
        {
            int i = 0;
            foreach (Bonez b in _trackedBones)
            {
                b?.OnReplace(root, meshRenderers, i, anim,true);
                i += 1;
            }
        }
        public void Clean()
        {
            foreach (Bonez b in _trackedBones) { b.Clean(); }
        }
    }
    public class TrackedSlot : SyncObject, Trackable
    {
        public readonly SyncRef<Slot> slot;
        public readonly Sync<bool> position;
        public readonly Sync<bool> rotation;
        public readonly Sync<bool> scale;
        public readonly SyncRef<AnimationRecorder> _rt;
        public readonly SyncRef<Slot> rootSlot;

        public CurveFloat3AnimationTrack positionTrack;
        public CurveFloatQAnimationTrack rotationTrack;
        public CurveFloat3AnimationTrack scaleTrack;

        public void OnStart(AnimationRecorder rt)
        {
            _rt.Target = rt;
            if (position.Value) positionTrack = rt.animation.AddTrack<CurveFloat3AnimationTrack>();
            if (rotation.Value) rotationTrack = rt.animation.AddTrack<CurveFloatQAnimationTrack>();
            if (scale.Value) scaleTrack = rt.animation.AddTrack<CurveFloat3AnimationTrack>();
        }
        public void OnUpdate(float T)
        {
            //Slot ruut = _rt.Target.rootSlot.Target;
            Slot ruut = rootSlot.Target;
            positionTrack?.InsertKeyFrame(ruut.GlobalPointToLocal(slot.Target?.GlobalPosition ?? float3.Zero), T);
            rotationTrack?.InsertKeyFrame(ruut.GlobalRotationToLocal(slot.Target?.GlobalRotation ?? floatQ.Identity), T);
            scaleTrack?.InsertKeyFrame(ruut.GlobalVectorToLocal(slot.Target?.GlobalScale ?? float3.Zero), T);
            //positionTrack?.InsertKeyFrame(slot.Target?.LocalPosition ?? float3.Zero, T);
            //rotationTrack?.InsertKeyFrame(slot.Target?.LocalRotation ?? floatQ.Identity, T);
            //scaleTrack?.InsertKeyFrame(slot.Target?.LocalScale ?? float3.One, T);
        }
        public void OnStop() { }
        public void OnReplace(Slot root, Slot s1, Animator anim, bool add_proxy=false)
        {
            //Slot root = _rt.Target.rootSlot.Target;
            //Slot root = rootSlot.Target;
            Slot s = null;
            if (add_proxy && s1 != null)
            {
                s = root.AddSlot(s1.Name);
                CopyGlobalTransform comp = s1.AttachComponent<CopyGlobalTransform>();
                comp.Source.Target = s;
            } else
            {
                s = s1;
            }
            //Slot s = slot.Target;
            if (positionTrack != null) { anim.Fields.Add().Target = s?.Position_Field; }
            if (rotationTrack != null) { anim.Fields.Add().Target = s?.Rotation_Field; }
            if (scaleTrack != null) { anim.Fields.Add().Target = s?.Scale_Field; }
            //World.ReplaceReferenceTargets(slot, s, true);
            //World.ForeachWorldElement(delegate (ISyncRef syncRef) {
            //    if (syncRef.Target == slot)
            //        syncRef.Target = s;
            //}, root);
        }
        public void Clean()
        {
            positionTrack = null; rotationTrack = null; scaleTrack = null;
        }
    }
}
