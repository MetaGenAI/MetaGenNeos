using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.ProgramFlow;
using FrooxEngine.LogiX.Actions;
using FrooxEngine.LogiX.WorldModel;

namespace NeosAnimationToolset
{
    class RecordedValueProcessor<T>
    {
        public static IField AttachComponents(Slot s, IField<T> target)
        {
            //ValueCopy<T> comp = s.GetComponent<ValueCopy<T>>();
            //if (comp == null) comp = s.AttachComponent<ValueCopy<T>>();
            //comp.WriteBack.Value = true;
            ValueField<T> comp2 = s.GetComponent<ValueField<T>>();
            if (comp2 == null) comp2 = s.AttachComponent<ValueField<T>>();
            //comp.Source.Target = comp2.Value;
            //if (target != null)
            //    comp.Target.Target = target;
            //Slot ls = s.FindChild((Slot c) => c.Name == "logix");
            //if (ls == null)
            //{
            Slot ls = s.AddSlot("logix");
            s = ls;
            FireOnChange<T> fireOnChange = s.AttachComponent<FireOnChange<T>>();
            HostUser hostUser = s.AttachComponent<HostUser>();
            WriteValueNode<T> writeValueNode = s.AttachComponent<WriteValueNode<T>>();
            ReferenceNode<IValue<T>> referenceNode = s.AttachComponent<ReferenceNode<IValue<T>>>();
            referenceNode.RefTarget.Target = target;
            writeValueNode.Target.Target = referenceNode;
            writeValueNode.Value.Target = comp2.Value;
            fireOnChange.OnlyForUser.Target = hostUser;
            fireOnChange.Value.Target = comp2.Value;
            fireOnChange.Pulse.Target = writeValueNode.Write;
            //}

            //fieldTracker.driven_field.Target = comp2.Value;
            return comp2.Value;
        }
    }
    class RecordedSlotProcessor
    {
        public static void AttachComponents(Slot s, Slot targetSlot, out IField<float3> pos, out IField<floatQ> rot, out IField<float3> scale)
        {
            List<ValueField<float3>> float3_fields = s.GetComponents<ValueField<float3>>();
            ValueField<float3> pos_field, scale_field;
            //if (float3_fields.Count >= 2)
            //{
            //    pos_field = float3_fields[0];
            //    scale_field = float3_fields[1];
            //} else if (float3_fields.Count == 1) {
            //    pos_field = float3_fields[0];
            //    scale_field = s.AttachComponent<ValueField<float3>>();
            //} else
            //{
            pos_field = s.AttachComponent<ValueField<float3>>();
            scale_field = s.AttachComponent<ValueField<float3>>();
            //}
            //ValueField<floatQ> rot_field = s.GetComponent<ValueField<floatQ>>();
            //if (rot_field == null) rot_field = s.AttachComponent<ValueField<floatQ>>();
            ValueField<floatQ> rot_field = s.AttachComponent<ValueField<floatQ>>();
            //Slot ls = s.FindChild((Slot c) => c.Name == "logix");
            //if (ls == null)
            //{
            Slot ls = s.AddSlot("logix");
            s = ls;
            FireOnChange<float3> fireOnChangePos = s.AttachComponent<FireOnChange<float3>>();
            FireOnChange<floatQ> fireOnChangeRot = s.AttachComponent<FireOnChange<floatQ>>();
            FireOnChange<float3> fireOnChangeScale = s.AttachComponent<FireOnChange<float3>>();
            OnePerFrame onePerFrame = s.AttachComponent<OnePerFrame>();
            HostUser hostUser = s.AttachComponent<HostUser>();
            SetGlobalTransform setGlobalTransform = s.AttachComponent<SetGlobalTransform>();
            ReferenceNode<Slot> referenceNode = s.AttachComponent<ReferenceNode<Slot>>();
            referenceNode.RefTarget.Target = targetSlot;
            setGlobalTransform.Instance.Target = referenceNode;
            setGlobalTransform.Position.Target = pos_field.Value;
            setGlobalTransform.Rotation.Target = rot_field.Value;
            setGlobalTransform.Scale.Target = scale_field.Value;
            fireOnChangePos.OnlyForUser.Target = hostUser;
            fireOnChangeRot.OnlyForUser.Target = hostUser;
            fireOnChangeScale.OnlyForUser.Target = hostUser;
            fireOnChangePos.Value.Target = pos_field.Value;
            fireOnChangeRot.Value.Target = rot_field.Value;
            fireOnChangeScale.Value.Target = scale_field.Value;
            fireOnChangePos.Pulse.Target = onePerFrame.Trigger;
            fireOnChangeRot.Pulse.Target = onePerFrame.Trigger;
            fireOnChangeScale.Pulse.Target = onePerFrame.Trigger;
            onePerFrame.Pulse.Target = setGlobalTransform.Set;

            pos = pos_field.Value;
            rot = rot_field.Value;
            scale = scale_field.Value;

        }
    }
}
