using System;
using System.Collections.Generic;
using BaseX;
using FrooxEngine;

namespace NeosAnimationToolset
{
    public class TrackedSkinnedMeshRenderer : SyncObject, ITrackable
    {
        public readonly SyncRef<SkinnedMeshRenderer> renderer;
        public readonly Sync<bool> recordScales;
        public readonly Sync<bool> recordBlendshapes;
        public RecordingTool _rt;
        public SkinnedMeshRenderer tgt;
        public bool scl;
        public bool bs;

        public List<bool> localSpace;
        public List<CurveFloat3AnimationTrack> positions;
        public List<CurveFloatQAnimationTrack> rotations;
        public List<CurveFloat3AnimationTrack> scales;
        public List<CurveFloatAnimationTrack> blendShapes;


        public void OnStart(RecordingTool rt)
        {
            tgt = renderer.Target;
            localSpace = Pool.BorrowList<bool>();
            positions = Pool.BorrowList<CurveFloat3AnimationTrack>();
            rotations = Pool.BorrowList<CurveFloatQAnimationTrack>();
            scales = Pool.BorrowList<CurveFloat3AnimationTrack>();
            blendShapes = Pool.BorrowList<CurveFloatAnimationTrack>();
            if (tgt == null) return;
            _rt = rt;
            scl = recordScales.Value;
            bs = recordBlendshapes.Value;
            for (int i = 0; i < tgt.Bones.Count; i++)
            {
                bool useLocal = false;
                Slot bone = tgt.Bones[i];
                if(bone!=null)
                for (int j = 0; j < tgt.Bones.Count; j++)
                {
                    if (bone.Parent == tgt.Bones[j]) {
                        useLocal = true;
                        break;
                    }
                }
                localSpace.Add(useLocal);
                positions.Add(rt.animation.AddTrack<CurveFloat3AnimationTrack>());
                rotations.Add(rt.animation.AddTrack<CurveFloatQAnimationTrack>());
                if(scl) scales.Add(rt.animation.AddTrack<CurveFloat3AnimationTrack>());
            }
            if (bs)
                for (int i = 0; i < tgt.BlendShapeWeights.Count; i++)
                {
                    blendShapes.Add(rt.animation.AddTrack<CurveFloatAnimationTrack>());
                }
        }
        public void OnUpdate(float T)
        {
            if (tgt == null) return;
            Slot ruut = _rt.rootSlot.Target;
            for (int i = 0; i < tgt.Bones.Count; i++)
            {
                Slot bone = tgt.Bones[i];
                if (bone == null)
                {
                    positions[i].InsertKeyFrame(new float3(0,0,0), T);
                    rotations[i].InsertKeyFrame(new floatQ(0, 0, 0, 1), T);
                    if (scl) scales[i].InsertKeyFrame(new float3(1, 1, 1), T);
                } else if (localSpace[i]) {
                    positions[i].InsertKeyFrame(bone.LocalPosition, T);
                    rotations[i].InsertKeyFrame(bone.LocalRotation, T);
                    if (scl) scales[i].InsertKeyFrame(bone.LocalScale, T);
                } else {
                    positions[i].InsertKeyFrame(ruut.GlobalPointToLocal(bone.GlobalPosition), T);
                    rotations[i].InsertKeyFrame(ruut.GlobalRotationToLocal(bone.GlobalRotation), T);
                    if (scl) scales[i].InsertKeyFrame(ruut.GlobalScaleToLocal(bone.GlobalScale), T);
                }
            }
            if(bs)
                for (int i = 0; i < tgt.BlendShapeWeights.Count; i++)
                {
                    blendShapes[i].InsertKeyFrame(tgt.BlendShapeWeights[i], T);
                }
        }
        public void OnStop() { }
        public void OnReplace(Animator anim){
            if (tgt == null) return;
            Slot ruut = _rt.rootSlot.Target;
            Slot meshRenderer = ruut.AddSlot("SkinnedMeshRenderer");
            List<Slot> createdBones = new List<Slot>();
            SkinnedMeshRenderer smr = meshRenderer.AttachComponent<SkinnedMeshRenderer>();

            smr.Mesh.Target = tgt.Mesh.Target;
            for (int i = 0; i < tgt.Materials.Count; i++)
                smr.Materials.Add(tgt.Materials[i]);

            for (int i = 0; i < tgt.Bones.Count; i++) {
                Slot created = meshRenderer.AddSlot(tgt.Bones[i]?.Name??"Null Bone");
                createdBones.Add(created);
                smr.Bones.Add(created);
            }

            for (int i = 0; i < tgt.Bones.Count; i++){
                Slot created= createdBones[i];
                if (localSpace[i]) {
                    int index = tgt.Bones.IndexOf(tgt.Bones[i]?.Parent);
                    if(index!=-1)
                        created.Parent = createdBones[index];
                }

                anim.Fields.Add().Target = created.Position_Field;
                anim.Fields.Add().Target = created.Rotation_Field;
                if(scl)anim.Fields.Add().Target = created.Scale_Field;
                else if(localSpace[i]) created.LocalScale = tgt.Bones[i]?.LocalScale??new float3(1,1,1);
                else created.LocalScale = meshRenderer.GlobalScaleToLocal(tgt.Bones[i]?.GlobalScale??new float3(1,1,1));
            }
            if(bs)
                for (int i = 0; i < tgt.BlendShapeWeights.Count; i++)
                {
                    smr.BlendShapeWeights.Add(0);
                    anim.Fields.Add().Target = (IField)smr.BlendShapeWeights.GetElement(i);
                }
        }
        public void Clean(){
            _rt = null;
            tgt = null;
            Pool.Return(ref blendShapes);
            Pool.Return(ref positions);
            Pool.Return(ref rotations);
            Pool.Return(ref scales);
        }
    }
}
