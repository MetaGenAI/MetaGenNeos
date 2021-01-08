using BaseX;
using FrooxEngine;
using System;

namespace NeosAnimationToolset
{
    public class FieldTracker : SyncObject, ITrackable
    {

        public readonly SyncRef<IField> field;
        public CurveAnimationTrack<float> floatTrack;
        public CurveAnimationTrack<float2> float2Track;
        public CurveAnimationTrack<float3> float3Track;
        public CurveAnimationTrack<float4> float4Track;
        public CurveAnimationTrack<int> intTrack;
        public CurveAnimationTrack<int2> int2Track;
        public CurveAnimationTrack<int3> int3Track;
        public CurveAnimationTrack<int4> int4Track;
        public CurveAnimationTrack<bool> boolTrack;
        public CurveAnimationTrack<string> stringTrack;
        public CurveAnimationTrack<color> colorTrack;

        public void OnStart(RecordingTool rt) {
            AnimX animx = rt.animation;
            Type type = field.Target.ValueType;
            if (type == typeof(float)){ floatTrack = animx.AddTrack<CurveFloatAnimationTrack>(); return; }
            if (type == typeof(float2)){ float2Track = animx.AddTrack<CurveFloat2AnimationTrack>(); return; }
            if (type == typeof(float3)){ float3Track = animx.AddTrack<CurveFloat3AnimationTrack>(); return; }
            if (type == typeof(float4)){ float4Track = animx.AddTrack<CurveFloat4AnimationTrack>(); return; }
            if (type == typeof(int)){ intTrack = animx.AddTrack<CurveIntAnimationTrack>(); return; }
            if (type == typeof(int2)){ int2Track = animx.AddTrack<CurveInt2AnimationTrack>(); return; }
            if (type == typeof(int3)){ int3Track = animx.AddTrack<CurveInt3AnimationTrack>(); return; }
            if (type == typeof(int4)){ int4Track = animx.AddTrack<CurveInt4AnimationTrack>(); return; }
            if (type == typeof(bool)){ boolTrack = animx.AddTrack<CurveBoolAnimationTrack>(); return; }
            if (type == typeof(string)){ stringTrack = animx.AddTrack<CurveStringAnimationTrack>(); return; }
            if (type == typeof(color)){ colorTrack = animx.AddTrack<CurveColorAnimationTrack>(); return; }
            field.Target = null;
        }
        public void OnUpdate(float t)
        {
            IField target = field.Target;
            Type type = field.Target.ValueType;
            if (type == typeof(float)) floatTrack.InsertKeyFrame((float)target.BoxedValue, t);
            if (type == typeof(float2)) float2Track.InsertKeyFrame((float2)target.BoxedValue, t);
            if (type == typeof(float3)) float3Track.InsertKeyFrame((float3)target.BoxedValue, t);
            if (type == typeof(float4)) float4Track.InsertKeyFrame((float4)target.BoxedValue, t);
            if (type == typeof(int)) intTrack.InsertKeyFrame((int)target.BoxedValue, t);
            if (type == typeof(int2)) int2Track.InsertKeyFrame((int2)target.BoxedValue, t);
            if (type == typeof(int3)) int3Track.InsertKeyFrame((int3)target.BoxedValue, t);
            if (type == typeof(int4)) int4Track.InsertKeyFrame((int4)target.BoxedValue, t);
            if (type == typeof(bool)) boolTrack.InsertKeyFrame((bool)target.BoxedValue, t);
            if (type == typeof(string)) stringTrack.InsertKeyFrame((string)target.BoxedValue, t);
            if (type == typeof(color)) colorTrack.InsertKeyFrame((color)target.BoxedValue, t);
        }
        public void OnReplace(Animator anim) {
            anim.Fields.Add();
        }
        public void OnStop() {}
        public void Clean() {
            floatTrack=null;
            float2Track=null;
            float3Track=null;
            float4Track=null;
            intTrack=null;
            int2Track=null;
            int3Track=null;
            int4Track=null;
            boolTrack=null;
            stringTrack=null;
            colorTrack=null;
        }

    }
}
