using BaseX;
using FrooxEngine;
using System;

namespace NeosAnimationToolset
{
    public class FieldTracker : SyncObject, ITrackable
    {

        public readonly SyncRef<IField> source_field;
        public readonly SyncRef<IField> driven_field;
        public Slot holding_slot;
        public CurveAnimationTrack<float> floatTrack;
        public CurveAnimationTrack<float2> float2Track;
        public CurveAnimationTrack<float2x2> float2x2Track;
        public CurveAnimationTrack<float3x3> float3x3Track;
        public CurveAnimationTrack<float4x4> float4x4Track;
        public CurveAnimationTrack<float3> float3Track;
        public CurveAnimationTrack<float4> float4Track;
        public CurveAnimationTrack<int> intTrack;
        public CurveAnimationTrack<int2> int2Track;
        public CurveAnimationTrack<int3> int3Track;
        public CurveAnimationTrack<int4> int4Track;
        public CurveAnimationTrack<bool> boolTrack;
        public CurveAnimationTrack<string> stringTrack;
        public CurveAnimationTrack<color> colorTrack;
        public CurveAnimationTrack<floatQ> floatQTrack;

        public void OnStart(RecordingTool rt) {
            AnimX animx = rt.animation;
            Type type = source_field.Target.ValueType;
            if (type == typeof(float)){ floatTrack = animx.AddTrack<CurveFloatAnimationTrack>(); return; }
            if (type == typeof(float2)){ float2Track = animx.AddTrack<CurveFloat2AnimationTrack>(); return; }
            if (type == typeof(float2x2)){ float2x2Track = animx.AddTrack<CurveFloat2x2AnimationTrack>(); return; }
            if (type == typeof(float3)){ float3Track = animx.AddTrack<CurveFloat3AnimationTrack>(); return; }
            if (type == typeof(float3x3)){ float3x3Track = animx.AddTrack<CurveFloat3x3AnimationTrack>(); return; }
            if (type == typeof(float4)){ float4Track = animx.AddTrack<CurveFloat4AnimationTrack>(); return; }
            if (type == typeof(float4x4)){ float4x4Track = animx.AddTrack<CurveFloat4x4AnimationTrack>(); return; }
            if (type == typeof(int)){ intTrack = animx.AddTrack<CurveIntAnimationTrack>(); return; }
            if (type == typeof(int2)){ int2Track = animx.AddTrack<CurveInt2AnimationTrack>(); return; }
            if (type == typeof(int3)){ int3Track = animx.AddTrack<CurveInt3AnimationTrack>(); return; }
            if (type == typeof(int4)){ int4Track = animx.AddTrack<CurveInt4AnimationTrack>(); return; }
            if (type == typeof(bool)){ boolTrack = animx.AddTrack<CurveBoolAnimationTrack>(); return; }
            if (type == typeof(string)){ stringTrack = animx.AddTrack<CurveStringAnimationTrack>(); return; }
            if (type == typeof(color)){ colorTrack = animx.AddTrack<CurveColorAnimationTrack>(); return; }
            if (type == typeof(floatQ)){ floatQTrack = animx.AddTrack<CurveFloatQAnimationTrack>(); return; }
            source_field.Target = null;

            Slot s = holding_slot;
            FieldTracker fieldTracker = this;
            IField field = source_field;
            if (type == typeof(float)) { fieldTracker.driven_field.Target = RecordedValueProcessor<float>.AttachComponents(s, (IField<float>)field); }
            if (type == typeof(float2)) { fieldTracker.driven_field.Target = RecordedValueProcessor<float2>.AttachComponents(s, (IField<float2>)field); }
            if (type == typeof(float2x2)) { fieldTracker.driven_field.Target = RecordedValueProcessor<float2x2>.AttachComponents(s, (IField<float2x2>)field); }
            if (type == typeof(float3)) { fieldTracker.driven_field.Target = RecordedValueProcessor<float3>.AttachComponents(s, (IField<float3>)field); }
            if (type == typeof(float3x3)) { fieldTracker.driven_field.Target = RecordedValueProcessor<float3x3>.AttachComponents(s, (IField<float3x3>)field); }
            if (type == typeof(float4)) { fieldTracker.driven_field.Target = RecordedValueProcessor<float4>.AttachComponents(s, (IField<float4>)field); }
            if (type == typeof(float4x4)) { fieldTracker.driven_field.Target = RecordedValueProcessor<float4x4>.AttachComponents(s, (IField<float4x4>)field); }
            if (type == typeof(int)) { fieldTracker.driven_field.Target = RecordedValueProcessor<int>.AttachComponents(s, (IField<int>)field); }
            if (type == typeof(int2)) { fieldTracker.driven_field.Target = RecordedValueProcessor<int2>.AttachComponents(s, (IField<int2>)field); }
            if (type == typeof(int3)) { fieldTracker.driven_field.Target = RecordedValueProcessor<int3>.AttachComponents(s, (IField<int3>)field); }
            if (type == typeof(int4)) { fieldTracker.driven_field.Target = RecordedValueProcessor<int4>.AttachComponents(s, (IField<int4>)field); }
            if (type == typeof(bool)) { fieldTracker.driven_field.Target = RecordedValueProcessor<bool>.AttachComponents(s, (IField<bool>)field); }
            if (type == typeof(string)) { fieldTracker.driven_field.Target = RecordedValueProcessor<string>.AttachComponents(s, (IField<string>)field); }
            if (type == typeof(color)) { fieldTracker.driven_field.Target = RecordedValueProcessor<color>.AttachComponents(s, (IField<color>)field); }
            if (type == typeof(floatQ)) { fieldTracker.driven_field.Target = RecordedValueProcessor<floatQ>.AttachComponents(s, (IField<floatQ>)field); }

        }
        public void OnUpdate(float t)
        {
            IField target = source_field.Target;
            Type type = source_field.Target.ValueType;
            if (type == typeof(float)) floatTrack.InsertKeyFrame((float)target.BoxedValue, t);
            if (type == typeof(float2)) float2Track.InsertKeyFrame((float2)target.BoxedValue, t);
            if (type == typeof(float2x2)) float2x2Track.InsertKeyFrame((float2x2)target.BoxedValue, t);
            if (type == typeof(float3)) float3Track.InsertKeyFrame((float3)target.BoxedValue, t);
            if (type == typeof(float3x3)) float3x3Track.InsertKeyFrame((float3x3)target.BoxedValue, t);
            if (type == typeof(float4)) float4Track.InsertKeyFrame((float4)target.BoxedValue, t);
            if (type == typeof(float4x4)) float4x4Track.InsertKeyFrame((float4x4)target.BoxedValue, t);
            if (type == typeof(int)) intTrack.InsertKeyFrame((int)target.BoxedValue, t);
            if (type == typeof(int2)) int2Track.InsertKeyFrame((int2)target.BoxedValue, t);
            if (type == typeof(int3)) int3Track.InsertKeyFrame((int3)target.BoxedValue, t);
            if (type == typeof(int4)) int4Track.InsertKeyFrame((int4)target.BoxedValue, t);
            if (type == typeof(bool)) boolTrack.InsertKeyFrame((bool)target.BoxedValue, t);
            if (type == typeof(string)) stringTrack.InsertKeyFrame((string)target.BoxedValue, t);
            if (type == typeof(color)) colorTrack.InsertKeyFrame((color)target.BoxedValue, t);
            if (type == typeof(floatQ)) floatQTrack.InsertKeyFrame((floatQ)target.BoxedValue, t);
        }
        public void OnReplace(Animator anim) {
            anim.Fields.Add().Target = driven_field.Target;
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
