using System;
using System.Collections.Generic;
using BaseX;
using FrooxEngine;

namespace NeosAnimationToolset
{
    public class TrackedMeshRenderer : SyncObject, ITrackable
    {
        public readonly SyncRef<MeshRenderer> renderer;
        public readonly Sync<bool> recordScales;
        public RecordingTool _rt;
        public MeshRenderer tgt;
        public Slot slot;
        public bool scl;

        public CurveFloat3AnimationTrack position;
        public CurveFloatQAnimationTrack rotation;
        public CurveFloat3AnimationTrack scale;


        public void OnStart(RecordingTool rt)
        {
            tgt = renderer.Target;
            if (tgt == null) return;
            slot = tgt.Slot;
            _rt = rt;
            scl = recordScales.Value;
            position=rt.animation.AddTrack<CurveFloat3AnimationTrack>();
            rotation=rt.animation.AddTrack<CurveFloatQAnimationTrack>();
            if(scl) scale=rt.animation.AddTrack<CurveFloat3AnimationTrack>();
        }
        public void OnUpdate(float T)
        {
            Slot ruut = _rt.rootSlot.Target;
            position.InsertKeyFrame(ruut.GlobalPointToLocal(slot.GlobalPosition), T);
            rotation.InsertKeyFrame(ruut.GlobalRotationToLocal(slot.GlobalRotation), T);
            scale?.InsertKeyFrame(ruut.GlobalScaleToLocal(slot.GlobalScale), T);
        }
        public void OnStop() { }
        public void OnReplace(Animator anim){
            Slot ruut = _rt.rootSlot.Target;
            Slot created = ruut.AddSlot("MeshRenderer");
            MeshRenderer mr = created.AttachComponent<MeshRenderer>();
            mr.Mesh.Target = tgt.Mesh.Target;
            for (int i = 0; i < tgt.Materials.Count; i++)
                mr.Materials.Add(tgt.Materials[i]);
            anim.Fields.Add().Target = created.Position_Field;
            anim.Fields.Add().Target = created.Rotation_Field;
            if(scl)anim.Fields.Add().Target = created.Scale_Field;
            else created.LocalScale = ruut.GlobalScaleToLocal(slot.GlobalScale);
        }
        public void Clean(){
            _rt = null;
            tgt = null;
            slot=null;
            position = null;
            rotation = null;
            scale=null;
        }
    }
}
