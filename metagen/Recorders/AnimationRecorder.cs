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
        public readonly Sync<bool> replaceRefs;

        public readonly SyncList<TrackedRig> recordedRigs;

        public readonly SyncList<TrackedSlot> recordedSlots;

        public readonly SyncRef<StaticAnimationProvider> _result;

        public string saving_folder;
        private MetaGen metagen_comp;

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
            Dictionary<RefID, User>.ValueCollection users = metagen_comp.World.AllUsers;
            foreach (User user in users)
            {
                //if (user == metagen_comp.World.LocalUser) continue;
                RefID user_id = user.ReferenceID;

                List<AvatarObjectSlot> components = user.Root.Slot.GetComponentsInChildren<AvatarObjectSlot>();
                //WRITE the absolute time
                foreach (AvatarObjectSlot comp in components)
                {
                    if (comp.IsTracking.Value)
                    {
                        if (comp.Node.Value == BodyNode.LeftController || comp.Node.Value == BodyNode.RightController || comp.Node.Value == BodyNode.NONE) continue;
                        TransformStreamDriver driver = comp.Slot.Parent.GetComponent<TransformStreamDriver>();
                        TrackedSlot trackedSlot = recordedSlots.Add();
                        trackedSlot.slot.Target = comp.Equipped?.Target.Slot;
                        if (driver != null)
                        {
                            trackedSlot.scale.Value = driver.ScaleStream.Target != null;
                            trackedSlot.position.Value = driver.PositionStream.Target != null;
                            trackedSlot.rotation.Value = driver.RotationStream.Target != null;
                        }
                        else //if the driver is not in the parent, then it is in the slot (which is what happens for the root)
                        {
                            driver = comp.Slot.GetComponent<TransformStreamDriver>();
                            trackedSlot.scale.Value = driver.ScaleStream.Target != null;
                            trackedSlot.position.Value = driver.PositionStream.Target != null;
                            trackedSlot.rotation.Value = driver.RotationStream.Target != null;
                        }
                    }
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
            StartTask(async () => { await this.AttachToObject(task); });
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

        public async Task AttachToObject(Task bake_task)
        {
            await bake_task;
            Animator animator = rootSlot.Target.AttachComponent<Animator>();
            animator.Clip.Target = _result.Target;
            foreach (TrackedRig rig in recordedRigs) { rig.OnReplace(animator); }
            foreach (TrackedSlot slot in recordedSlots) { slot.OnReplace(animator); }
            //foreach (ACMngr field in trackedFields) { field.OnStop(); }
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
            _result.Target = base.Slot.AttachComponent<StaticAnimationProvider>();
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
        void OnReplace(Animator anim);
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
                Slot ruut = rig._rt.Target.rootSlot.Target;
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
            public void OnReplace(Animator anim)
            {
                Slot root = rig._rt.Target.rootSlot.Target;
                Slot s = root.AddSlot(slot.Name);
                if (positionTrack != null) { anim.Fields.Add().Target = s.Position_Field; }
                if (rotationTrack != null) { anim.Fields.Add().Target = s.Rotation_Field; }
                if (scaleTrack != null) { anim.Fields.Add().Target = s.Scale_Field; }
                //World.ReplaceReferenceTargets(slot, s, true);
                World.ForeachWorldElement(delegate (ISyncRef syncRef) {
                    if (syncRef.Target == slot)
                        syncRef.Target = s;
                }, root);
            }
            public void Clean()
            {
                positionTrack = null; rotationTrack = null; scaleTrack = null;
            }
        }
        public void OnReplace(Animator anim)
        {
            foreach (Bonez b in _trackedBones)
            {
                b?.OnReplace(anim);
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
            Slot ruut = _rt.Target.rootSlot.Target;
            positionTrack?.InsertKeyFrame(ruut.GlobalPointToLocal(slot.Target?.GlobalPosition ?? float3.Zero), T);
            rotationTrack?.InsertKeyFrame(ruut.GlobalRotationToLocal(slot.Target?.GlobalRotation ?? floatQ.Identity), T);
            scaleTrack?.InsertKeyFrame(ruut.GlobalVectorToLocal(slot.Target?.GlobalScale ?? float3.Zero), T);
        }
        public void OnStop() { }
        public void OnReplace(Animator anim)
        {
            Slot root = _rt.Target.rootSlot.Target;
            Slot s = root.AddSlot(slot.Name);
            if (positionTrack != null) { anim.Fields.Add().Target = s.Position_Field; }
            if (rotationTrack != null) { anim.Fields.Add().Target = s.Rotation_Field; }
            if (scaleTrack != null) { anim.Fields.Add().Target = s.Scale_Field; }
            //World.ReplaceReferenceTargets(slot, s, true);
            World.ForeachWorldElement(delegate (ISyncRef syncRef) {
                if (syncRef.Target == slot)
                    syncRef.Target = s;
            }, root);
        }
        public void Clean()
        {
            positionTrack = null; rotationTrack = null; scaleTrack = null;
        }
    }
}
