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
        public CurveAnimationTrack<char> charTrack;
        public CurveAnimationTrack<color> colorTrack;
        public CurveAnimationTrack<floatQ> floatQTrack;
        public CurveAnimationTrack<long> longTrack;
        public CurveAnimationTrack<ulong> ulongTrack;
        public CurveAnimationTrack<byte> byteTrack;
        public CurveAnimationTrack<double> doubleTrack;
        public CurveAnimationTrack<double2> double2Track;
        public CurveAnimationTrack<double2x2> double2x2Track;
        public CurveAnimationTrack<double3> double3Track;
        public CurveAnimationTrack<double3x3> double3x3Track;
        public CurveAnimationTrack<double4> double4Track;
        public CurveAnimationTrack<double4x4> double4x4Track;
        public CurveAnimationTrack<doubleQ> doubleQTrack;
        public CurveAnimationTrack<short> shortTrack;
        public CurveAnimationTrack<ushort> ushortTrack;
        public CurveAnimationTrack<uint> uintTrack;

        public void OnStart(RecordingTool rt) {
            AnimX animx = rt.animation;
            Type type = source_field.Target.ValueType;
            if (type == typeof(float)){ floatTrack = animx.AddTrack<CurveFloatAnimationTrack>(); }
            if (type == typeof(float2)){ float2Track = animx.AddTrack<CurveFloat2AnimationTrack>();  }
            if (type == typeof(float2x2)){ float2x2Track = animx.AddTrack<CurveFloat2x2AnimationTrack>();  }
            if (type == typeof(float3)){ float3Track = animx.AddTrack<CurveFloat3AnimationTrack>();  }
            if (type == typeof(float3x3)){ float3x3Track = animx.AddTrack<CurveFloat3x3AnimationTrack>();  }
            if (type == typeof(float4)){ float4Track = animx.AddTrack<CurveFloat4AnimationTrack>();  }
            if (type == typeof(float4x4)){ float4x4Track = animx.AddTrack<CurveFloat4x4AnimationTrack>();  }
            if (type == typeof(int)){ intTrack = animx.AddTrack<CurveIntAnimationTrack>();  }
            if (type == typeof(int2)){ int2Track = animx.AddTrack<CurveInt2AnimationTrack>();  }
            if (type == typeof(int3)){ int3Track = animx.AddTrack<CurveInt3AnimationTrack>();  }
            if (type == typeof(int4)){ int4Track = animx.AddTrack<CurveInt4AnimationTrack>();  }
            if (type == typeof(bool)){ boolTrack = animx.AddTrack<CurveBoolAnimationTrack>();  }
            if (type == typeof(string)){ stringTrack = animx.AddTrack<CurveStringAnimationTrack>(); }
            if (type == typeof(char)){ charTrack = animx.AddTrack<CurveCharAnimationTrack>(); }
            if (type == typeof(color)){ colorTrack = animx.AddTrack<CurveColorAnimationTrack>();  }
            if (type == typeof(floatQ)){ floatQTrack = animx.AddTrack<CurveFloatQAnimationTrack>(); }
            if (type == typeof(long)){ longTrack = animx.AddTrack<CurveLongAnimationTrack>(); }
            if (type == typeof(ulong)){ ulongTrack = animx.AddTrack<CurveUlongAnimationTrack>(); }
            if (type == typeof(byte)){ byteTrack = animx.AddTrack<CurveByteAnimationTrack>(); }
            if (type == typeof(double)){ doubleTrack = animx.AddTrack<CurveDoubleAnimationTrack>(); }
            if (type == typeof(double2)){ double2Track = animx.AddTrack<CurveDouble2AnimationTrack>(); }
            if (type == typeof(double2x2)){ double2x2Track = animx.AddTrack<CurveDouble2x2AnimationTrack>(); }
            if (type == typeof(double3)){ double3Track = animx.AddTrack<CurveDouble3AnimationTrack>(); }
            if (type == typeof(double3x3)){ double3x3Track = animx.AddTrack<CurveDouble3x3AnimationTrack>(); }
            if (type == typeof(double4)){ double4Track = animx.AddTrack<CurveDouble4AnimationTrack>(); }
            if (type == typeof(double4x4)){ double4x4Track = animx.AddTrack<CurveDouble4x4AnimationTrack>(); }
            if (type == typeof(doubleQ)){ doubleQTrack = animx.AddTrack<CurveDoubleQAnimationTrack>(); }
            if (type == typeof(short)){ shortTrack = animx.AddTrack<CurveShortAnimationTrack>(); }
            if (type == typeof(ushort)){ ushortTrack = animx.AddTrack<CurveUshortAnimationTrack>(); }
            if (type == typeof(uint)){ uintTrack = animx.AddTrack<CurveUintAnimationTrack>(); }

            Slot s = holding_slot;
            FieldTracker fieldTracker = this;
            IField field = source_field.Target;
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
            if (type == typeof(char)) { fieldTracker.driven_field.Target = RecordedValueProcessor<char>.AttachComponents(s, (IField<char>)field); }
            if (type == typeof(color)) { fieldTracker.driven_field.Target = RecordedValueProcessor<color>.AttachComponents(s, (IField<color>)field); }
            if (type == typeof(floatQ)) { fieldTracker.driven_field.Target = RecordedValueProcessor<floatQ>.AttachComponents(s, (IField<floatQ>)field); }
            if (type == typeof(long)) { fieldTracker.driven_field.Target = RecordedValueProcessor<long>.AttachComponents(s, (IField<long>)field); }
            if (type == typeof(ulong)) { fieldTracker.driven_field.Target = RecordedValueProcessor<ulong>.AttachComponents(s, (IField<ulong>)field); }
            if (type == typeof(byte)) { fieldTracker.driven_field.Target = RecordedValueProcessor<byte>.AttachComponents(s, (IField<byte>)field); }
            if (type == typeof(double)) { fieldTracker.driven_field.Target = RecordedValueProcessor<double>.AttachComponents(s, (IField<double>)field); }
            if (type == typeof(double2)) { fieldTracker.driven_field.Target = RecordedValueProcessor<double2>.AttachComponents(s, (IField<double2>)field); }
            if (type == typeof(double2x2)) { fieldTracker.driven_field.Target = RecordedValueProcessor<double2x2>.AttachComponents(s, (IField<double2x2>)field); }
            if (type == typeof(double3)) { fieldTracker.driven_field.Target = RecordedValueProcessor<double3>.AttachComponents(s, (IField<double3>)field); }
            if (type == typeof(double3x3)) { fieldTracker.driven_field.Target = RecordedValueProcessor<double3x3>.AttachComponents(s, (IField<double3x3>)field); }
            if (type == typeof(double4)) { fieldTracker.driven_field.Target = RecordedValueProcessor<double4>.AttachComponents(s, (IField<double4>)field); }
            if (type == typeof(double4x4)) { fieldTracker.driven_field.Target = RecordedValueProcessor<double4x4>.AttachComponents(s, (IField<double4x4>)field); }
            if (type == typeof(doubleQ)) { fieldTracker.driven_field.Target = RecordedValueProcessor<doubleQ>.AttachComponents(s, (IField<doubleQ>)field); }
            if (type == typeof(short)) { fieldTracker.driven_field.Target = RecordedValueProcessor<short>.AttachComponents(s, (IField<short>)field); }
            if (type == typeof(ushort)) { fieldTracker.driven_field.Target = RecordedValueProcessor<ushort>.AttachComponents(s, (IField<ushort>)field); }
            if (type == typeof(uint)) { fieldTracker.driven_field.Target = RecordedValueProcessor<uint>.AttachComponents(s, (IField<uint>)field); }
            //source_field.Target = null;

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
            if (type == typeof(char)) charTrack.InsertKeyFrame((char)target.BoxedValue, t);
            if (type == typeof(color)) colorTrack.InsertKeyFrame((color)target.BoxedValue, t);
            if (type == typeof(floatQ)) floatQTrack.InsertKeyFrame((floatQ)target.BoxedValue, t);
            if (type == typeof(long)) longTrack.InsertKeyFrame((long)target.BoxedValue, t);
            if (type == typeof(ulong)) ulongTrack.InsertKeyFrame((ulong)target.BoxedValue, t);
            if (type == typeof(byte)) ulongTrack.InsertKeyFrame((byte)target.BoxedValue, t);
            if (type == typeof(double)) doubleTrack.InsertKeyFrame((double)target.BoxedValue, t);
            if (type == typeof(double2)) double2Track.InsertKeyFrame((double2)target.BoxedValue, t);
            if (type == typeof(double2x2)) double2x2Track.InsertKeyFrame((double2x2)target.BoxedValue, t);
            if (type == typeof(double3)) double3Track.InsertKeyFrame((double3)target.BoxedValue, t);
            if (type == typeof(double3x3)) double3x3Track.InsertKeyFrame((double3x3)target.BoxedValue, t);
            if (type == typeof(double4)) double4Track.InsertKeyFrame((double4)target.BoxedValue, t);
            if (type == typeof(double4x4)) double4x4Track.InsertKeyFrame((double4x4)target.BoxedValue, t);
            if (type == typeof(doubleQ)) doubleQTrack.InsertKeyFrame((doubleQ)target.BoxedValue, t);
            if (type == typeof(short)) shortTrack.InsertKeyFrame((short)target.BoxedValue, t);
            if (type == typeof(ushort)) ushortTrack.InsertKeyFrame((ushort)target.BoxedValue, t);
            if (type == typeof(uint)) uintTrack.InsertKeyFrame((uint)target.BoxedValue, t);
        }
        public void OnReplace(Animator anim) {
            anim.Fields.Add().Target = driven_field.Target;
        }
        public void OnStop() {}
        public void Clean() {
            floatTrack=null;
            float2Track=null;
            float2x2Track=null;
            float3Track=null;
            float3x3Track=null;
            float4Track=null;
            float4x4Track=null;
            intTrack=null;
            int2Track=null;
            int3Track=null;
            int4Track=null;
            boolTrack=null;
            stringTrack=null;
            charTrack=null;
            colorTrack=null;
            floatQTrack=null;
            longTrack=null;
            ulongTrack=null;
            byteTrack=null;
            doubleTrack=null;
            double2Track=null;
            double2x2Track=null;
            double3Track=null;
            double3x3Track=null;
            double4Track=null;
            double4x4Track=null;
            doubleQTrack=null;
            shortTrack=null;
        }

    }
}
