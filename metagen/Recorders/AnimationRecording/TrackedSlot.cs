using FrooxEngine;
using BaseX;

namespace NeosAnimationToolset
{
    public class TrackedSlot : SyncObject, ITrackable
    {
        public class SlotListReference : SyncObject
        {
            public readonly SyncRef<SyncRefList<Slot>> list;
            public readonly Sync<int> index;
        }
        public readonly SyncRef<Slot> slot;
        public readonly Sync<bool> position;
        public readonly Sync<bool> rotation;
        public readonly Sync<bool> scale;
        public readonly SyncRefList<SyncRef<Slot>> references;
        public readonly SyncList<SlotListReference> listReferences;
        public readonly Sync<ResultTypeEnum> ResultType;
        public RecordingTool _rt;
        public bool addedByRig = false;

        public CurveFloat3AnimationTrack positionTrack;
        public CurveFloatQAnimationTrack rotationTrack;
        public CurveFloat3AnimationTrack scaleTrack;

        public void OnStart(RecordingTool rt)
        {
            _rt = rt;
            if (position.Value) positionTrack = rt.animation.AddTrack<CurveFloat3AnimationTrack>();
            if (rotation.Value) rotationTrack = rt.animation.AddTrack<CurveFloatQAnimationTrack>();
            if (scale.Value) scaleTrack = rt.animation.AddTrack<CurveFloat3AnimationTrack>();
        }
        public void OnUpdate(float T)
        {
            Slot ruut = _rt.rootSlot.Target;
            positionTrack?.InsertKeyFrame(ruut.GlobalPointToLocal(slot.Target?.GlobalPosition ?? float3.Zero), T);
            rotationTrack?.InsertKeyFrame(ruut.GlobalRotationToLocal(slot.Target?.GlobalRotation ?? floatQ.Identity), T);
            scaleTrack?.InsertKeyFrame(ruut.GlobalScaleToLocal(slot.Target?.GlobalScale ?? float3.Zero), T);
        }
        public void OnStop() { }
        public void OnReplace(Animator anim)
        {
            Slot root = _rt.rootSlot.Target;
            ResultTypeEnum rte = ResultType.Value;
            if (rte == ResultTypeEnum.DO_NOTHING)
            {
                if (positionTrack != null) { anim.Fields.Add(); }
                if (rotationTrack != null) { anim.Fields.Add(); }
                if (scaleTrack != null) { anim.Fields.Add(); }
                return;
            }

            Slot s = root.AddSlot((rte == ResultTypeEnum.CREATE_VISUAL || rte == ResultTypeEnum.CREATE_NON_PERSISTENT_VISUAL) ? "Visual" : "Empty Object", rte != ResultTypeEnum.CREATE_NON_PERSISTENT_VISUAL);
            if (positionTrack != null) { anim.Fields.Add().Target = s.Position_Field; }
            if (rotationTrack != null) { anim.Fields.Add().Target = s.Rotation_Field; }
            if (scaleTrack != null) { anim.Fields.Add().Target = s.Scale_Field; }
            if (rte == ResultTypeEnum.CREATE_VISUAL || rte == ResultTypeEnum.CREATE_NON_PERSISTENT_VISUAL)
            {
                CrossMesh mesh = root.GetComponentOrAttach<CrossMesh>();
                mesh.Size.Value = 0.05f;
                mesh.BarRatio.Value = 0.05f;
                PBS_Metallic mat = root.GetComponentOrAttach<PBS_Metallic>();
                mat.EmissiveColor.Value = new color(0.5f, 0.5f, 0.5f);
                MeshRenderer meshRenderer = s.AttachComponent<MeshRenderer>();
                meshRenderer.Mesh.Target = mesh;
                meshRenderer.Materials.Add(mat);
            }
            else if (rte == ResultTypeEnum.CREATE_PARENT_SLOTS)
            {
                Slot old = slot.Target;
                old.SetParent(s, false);
                if (positionTrack != null) { old.LocalPosition = new float3(0, 0, 0); }
                if (rotationTrack != null) { old.LocalRotation = floatQ.Identity; }
                if (scaleTrack != null) { old.LocalScale = new float3(1, 1, 1); }
            }
            else if (rte == ResultTypeEnum.REPLACE_REFERENCES)
            {
                foreach (SyncRef<Slot> it in references) { it.Target = s; }
                foreach (SlotListReference it in listReferences) { it.list.Target[it.index.Value] = s; }
            }
        }
        public void Clean()
        {
            positionTrack = null; rotationTrack = null; scaleTrack = null;
        }
    }
}
