using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;

namespace metagen.Interactions
{
    public class FieldInteraction : IInteraction
    {
        public bool isInteracting = false;
        public bool isRecording = false;
        private MetaGen metagen_comp;
        private WebsocketClient client;
        private Dictionary<string,IField> input_fields = new Dictionary<string,IField>();
        private Dictionary<string,IField> output_fields = new Dictionary<string,IField>();
        public FieldInteraction(MetaGen component)
        {
            metagen_comp = component;
        }

        private void OnTextMessageReceived(WebsocketClient client, string msg)
        {
            //UniLog.Log("hi");
            UniLog.Log(msg);
            // UniLog.Log(output_field.Value);
            string name = msg.Split('/')[0];
            string value_str = msg.Substring(name.Length);
            metagen_comp.World.RunSynchronously(() =>
            {
                if (output_fields.ContainsKey(name) && output_fields[name] != null)
                {
                    IField field = output_fields[name];
                    Type type = field.ValueType;
                    if (type == typeof(float)){ float result; if (RobustParser.TryParse(value_str, out result)) ((Sync<float>)field).Value = result;}
                    if (type == typeof(float2)){ float2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<float2>)field).Value = result;}
                    if (type == typeof(float2x2)){ float2x2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<float2x2>)field).Value = result;}
                    if (type == typeof(float3)){ float3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<float3>)field).Value = result;}
                    if (type == typeof(float3x3)){ float3x3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<float3x3>)field).Value = result;}
                    if (type == typeof(float4)){ float4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<float4>)field).Value = result;}
                    if (type == typeof(float4x4)){ float4x4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<float4x4>)field).Value = result;}
                    if (type == typeof(int)){ int result; if (RobustParser.TryParse(value_str, out result)) ((Sync<int>)field).Value = result;}
                    if (type == typeof(int2)){ int2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<int2>)field).Value = result;}
                    if (type == typeof(int3)){ int3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<int3>)field).Value = result;}
                    if (type == typeof(int4)){ int4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<int4>)field).Value = result;}
                    if (type == typeof(bool)){ bool result; if (RobustParser.TryParse(value_str, out result)) ((Sync<bool>)field).Value = result;}
                    if (type == typeof(bool2)){ bool2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<bool2>)field).Value = result;}
                    if (type == typeof(bool3)){ bool3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<bool3>)field).Value = result;}
                    if (type == typeof(bool4)){ bool4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<bool4>)field).Value = result;}
                    if (type == typeof(floatQ)){ floatQ result; if (RobustParser.TryParse(value_str, out result)) ((Sync<floatQ>)field).Value = result;}
                    if (type == typeof(ulong)){ ulong result; if (RobustParser.TryParse(value_str, out result)) ((Sync<ulong>)field).Value = result;}
                    if (type == typeof(ulong2)){ ulong2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<ulong2>)field).Value = result;}
                    if (type == typeof(ulong3)){ ulong3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<ulong3>)field).Value = result;}
                    if (type == typeof(ulong4)){ ulong4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<ulong4>)field).Value = result;}
                    if (type == typeof(long)){ long result; if (RobustParser.TryParse(value_str, out result)) ((Sync<long>)field).Value = result;}
                    if (type == typeof(long2)){ long2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<long2>)field).Value = result;}
                    if (type == typeof(long3)){ long3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<long3>)field).Value = result;}
                    if (type == typeof(long4)){ long4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<long4>)field).Value = result;}
                    if (type == typeof(double)){ double result; if (RobustParser.TryParse(value_str, out result)) ((Sync<double>)field).Value = result;}
                    if (type == typeof(double2)){ double2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<double2>)field).Value = result;}
                    if (type == typeof(double2x2)){ double2x2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<double2x2>)field).Value = result;}
                    if (type == typeof(double3)){ double3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<double3>)field).Value = result;}
                    if (type == typeof(double3x3)){ double3x3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<double3x3>)field).Value = result;}
                    if (type == typeof(double4)){ double4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<double4>)field).Value = result;}
                    if (type == typeof(double4x4)){ double4x4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<double4x4>)field).Value = result;}
                    if (type == typeof(doubleQ)){ doubleQ result; if (RobustParser.TryParse(value_str, out result)) ((Sync<doubleQ>)field).Value = result;}
                    if (type == typeof(uint)){ uint result; if (RobustParser.TryParse(value_str, out result)) ((Sync<uint>)field).Value = result;}
                    if (type == typeof(uint2)){ uint2 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<uint2>)field).Value = result;}
                    if (type == typeof(uint3)){ uint3 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<uint3>)field).Value = result;}
                    if (type == typeof(uint4)){ uint4 result; if (RobustParser.TryParse(value_str, out result)) ((Sync<uint4>)field).Value = result;}
                    if (type == typeof(string)){ string result; if (RobustParser.TryParse(value_str, out result)) ((Sync<string>)field).Value = result;}
                    if (type == typeof(color)){ color result; if (RobustParser.TryParse(value_str, out result)) ((Sync<color>)field).Value = result;}
                    if (type == typeof(short)){ short result; if (RobustParser.TryParse(value_str, out result)) ((Sync<short>)field).Value = result;}
                    if (type == typeof(ushort)){ ushort result; if (RobustParser.TryParse(value_str, out result)) ((Sync<ushort>)field).Value = result;}
                    if (type == typeof(byte)){ byte result; if (RobustParser.TryParse(value_str, out result)) ((Sync<byte>)field).Value = result;}
                    if (type == typeof(sbyte)){ sbyte result; if (RobustParser.TryParse(value_str, out result)) ((Sync<sbyte>)field).Value = result;}
                    if (type == typeof(char)){ char result; if (RobustParser.TryParse(value_str, out result)) ((Sync<char>)field).Value = result;}
                    if (type == typeof(colorX)){ colorX result; if (RobustParser.TryParse(value_str, out result)) ((Sync<colorX>)field).Value = result;}
                    if (type == typeof(TimeSpan)){ TimeSpan result; if (RobustParser.TryParse(value_str, out result)) ((Sync<TimeSpan>)field).Value = result;}
                    if (type == typeof(DateTime)){ DateTime result; if (RobustParser.TryParse(value_str, out result)) ((Sync<DateTime>)field).Value = result;}
                    if (type == typeof(Decimal)){ Decimal result; if (RobustParser.TryParse(value_str, out result)) ((Sync<Decimal>)field).Value = result;}
                }
            });
        }
        private void OnBinaryMessageReceived(WebsocketClient client, byte[] msg)
        {
            //UniLog.Log("ho");
            //UniLog.Log(msg);
        }
        private void OnInputFieldChanged(string name, string type, string value_str)
        {
            metagen_comp.StartTask(async () => await client.Send(name+"/"+type+"/"+value_str));
        }
        public void StartInteracting()
        {
            UniLog.Log("Start Field interaction");
            UniLog.Log("Attaching component");
            client = metagen_comp.interaction_slot.GetComponent<WebsocketClient>();
            if (client == null)
            {
                client = metagen_comp.interaction_slot.AttachComponent<WebsocketClient>();
            }

            UniLog.Log("Attached component");
            client.TextMessageReceived += OnTextMessageReceived;
            client.BinaryMessageReceived += OnBinaryMessageReceived;

            client.HandlingUser.Target = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.LocalUser;
            client.URL.Value = new Uri("ws://localhost:8766");

            //Input fields
            Slot input_field_holders = metagen_comp.input_interaction_fields_slot;
            List<Slot> inputFieldsHolders = input_field_holders?.GetAllChildren();
            if (inputFieldsHolders != null)
            {
                foreach(Slot s in inputFieldsHolders)
                {
                    List<ReferenceField<IField>> referenceSources = s.GetComponentsInChildren<ReferenceField<IField>>();

                foreach(ReferenceField<IField> referenceSource in referenceSources)
                    {
                        IField field = referenceSource.Reference.Target;
                        string name = referenceSource.Slot.Name;
                        Type type = field.ValueType;
                        if (field == null) continue;
                        ;
                        if (type == typeof(float)){ ((Sync<float>)field).OnValueChange += new SyncFieldEvent<float>((SyncField<float> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(float2)){ ((Sync<float2>)field).OnValueChange += new SyncFieldEvent<float2>((SyncField<float2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(float2x2)){ ((Sync<float2x2>)field).OnValueChange += new SyncFieldEvent<float2x2>((SyncField<float2x2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(float3)){ ((Sync<float3>)field).OnValueChange += new SyncFieldEvent<float3>((SyncField<float3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(float3x3)){ ((Sync<float3x3>)field).OnValueChange += new SyncFieldEvent<float3x3>((SyncField<float3x3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(float4)){ ((Sync<float4>)field).OnValueChange += new SyncFieldEvent<float4>((SyncField<float4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(float4x4)){ ((Sync<float4x4>)field).OnValueChange += new SyncFieldEvent<float4x4>((SyncField<float4x4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(int)){ ((Sync<int>)field).OnValueChange += new SyncFieldEvent<int>((SyncField<int> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(int2)){ ((Sync<int2>)field).OnValueChange += new SyncFieldEvent<int2>((SyncField<int2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(int3)){ ((Sync<int3>)field).OnValueChange += new SyncFieldEvent<int3>((SyncField<int3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(int4)){ ((Sync<int4>)field).OnValueChange += new SyncFieldEvent<int4>((SyncField<int4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(bool)){ ((Sync<bool>)field).OnValueChange += new SyncFieldEvent<bool>((SyncField<bool> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(bool2)){ ((Sync<bool2>)field).OnValueChange += new SyncFieldEvent<bool2>((SyncField<bool2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(bool3)){ ((Sync<bool3>)field).OnValueChange += new SyncFieldEvent<bool3>((SyncField<bool3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(bool4)){ ((Sync<bool4>)field).OnValueChange += new SyncFieldEvent<bool4>((SyncField<bool4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(floatQ)){ ((Sync<floatQ>)field).OnValueChange += new SyncFieldEvent<floatQ>((SyncField<floatQ> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(ulong)){ ((Sync<ulong>)field).OnValueChange += new SyncFieldEvent<ulong>((SyncField<ulong> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(ulong2)){ ((Sync<ulong2>)field).OnValueChange += new SyncFieldEvent<ulong2>((SyncField<ulong2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(ulong3)){ ((Sync<ulong3>)field).OnValueChange += new SyncFieldEvent<ulong3>((SyncField<ulong3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(ulong4)){ ((Sync<ulong4>)field).OnValueChange += new SyncFieldEvent<ulong4>((SyncField<ulong4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(long)){ ((Sync<long>)field).OnValueChange += new SyncFieldEvent<long>((SyncField<long> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(long2)){ ((Sync<long2>)field).OnValueChange += new SyncFieldEvent<long2>((SyncField<long2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(long3)){ ((Sync<long3>)field).OnValueChange += new SyncFieldEvent<long3>((SyncField<long3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(long4)){ ((Sync<long4>)field).OnValueChange += new SyncFieldEvent<long4>((SyncField<long4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(double)){ ((Sync<double>)field).OnValueChange += new SyncFieldEvent<double>((SyncField<double> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(double2)){ ((Sync<double2>)field).OnValueChange += new SyncFieldEvent<double2>((SyncField<double2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(double2x2)){ ((Sync<double2x2>)field).OnValueChange += new SyncFieldEvent<double2x2>((SyncField<double2x2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(double3)){ ((Sync<double3>)field).OnValueChange += new SyncFieldEvent<double3>((SyncField<double3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(double3x3)){ ((Sync<double3x3>)field).OnValueChange += new SyncFieldEvent<double3x3>((SyncField<double3x3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(double4)){ ((Sync<double4>)field).OnValueChange += new SyncFieldEvent<double4>((SyncField<double4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(double4x4)){ ((Sync<double4x4>)field).OnValueChange += new SyncFieldEvent<double4x4>((SyncField<double4x4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(doubleQ)){ ((Sync<doubleQ>)field).OnValueChange += new SyncFieldEvent<doubleQ>((SyncField<doubleQ> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(uint)){ ((Sync<uint>)field).OnValueChange += new SyncFieldEvent<uint>((SyncField<uint> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(uint2)){ ((Sync<uint2>)field).OnValueChange += new SyncFieldEvent<uint2>((SyncField<uint2> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(uint3)){ ((Sync<uint3>)field).OnValueChange += new SyncFieldEvent<uint3>((SyncField<uint3> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(uint4)){ ((Sync<uint4>)field).OnValueChange += new SyncFieldEvent<uint4>((SyncField<uint4> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(string)){ ((Sync<string>)field).OnValueChange += new SyncFieldEvent<string>((SyncField<string> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(color)){ ((Sync<color>)field).OnValueChange += new SyncFieldEvent<color>((SyncField<color> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(short)){ ((Sync<short>)field).OnValueChange += new SyncFieldEvent<short>((SyncField<short> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(ushort)){ ((Sync<ushort>)field).OnValueChange += new SyncFieldEvent<ushort>((SyncField<ushort> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(byte)){ ((Sync<byte>)field).OnValueChange += new SyncFieldEvent<byte>((SyncField<byte> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(sbyte)){ ((Sync<sbyte>)field).OnValueChange += new SyncFieldEvent<sbyte>((SyncField<sbyte> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(char)){ ((Sync<char>)field).OnValueChange += new SyncFieldEvent<char>((SyncField<char> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(colorX)){ ((Sync<colorX>)field).OnValueChange += new SyncFieldEvent<colorX>((SyncField<colorX> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(TimeSpan)){ ((Sync<TimeSpan>)field).OnValueChange += new SyncFieldEvent<TimeSpan>((SyncField<TimeSpan> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(DateTime)){ ((Sync<DateTime>)field).OnValueChange += new SyncFieldEvent<DateTime>((SyncField<DateTime> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        if (type == typeof(Decimal)){ ((Sync<Decimal>)field).OnValueChange += new SyncFieldEvent<Decimal>((SyncField<Decimal> f)=> OnInputFieldChanged(name, type.ToString(), f.Value.ToString()));}
                        input_fields[name] = field;
                    }
                }
            }

            //Output fields
            Slot output_field_holders = metagen_comp.output_interaction_fields_slot;
            List<Slot> outputFieldsHolders = output_field_holders?.GetAllChildren();
            if (outputFieldsHolders != null)
            {
                foreach(Slot s in outputFieldsHolders)
                {
                    List<ReferenceField<IField>> referenceSources = s.GetComponentsInChildren<ReferenceField<IField>>();

                foreach(ReferenceField<IField> referenceSource in referenceSources)
                    {
                        IField field = referenceSource.Reference.Target;
                        string name = referenceSource.Slot.Name;
                        output_fields[name] = field;
                    }
                }
            }
            isInteracting = true;
        }
        public void StopInteracting()
        {
            UniLog.Log("Stop Text interaction");
            client.URL.Value = null;
            client.TextMessageReceived -= OnTextMessageReceived;
            client.BinaryMessageReceived -= OnBinaryMessageReceived;
            isInteracting = false;
        }
    }
}
