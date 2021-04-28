using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;

namespace NeosAnimationToolset
{
    class RecordedValueProcessor<T>
    {
        public static void AttachComponents(Slot s, IField<T> target, FieldTracker fieldTracker)
        {
            ValueCopy<T> comp = s.GetComponent<ValueCopy<T>>();
            if (comp == null) comp = s.AttachComponent<ValueCopy<T>>();
            comp.WriteBack.Value = true;
            ValueField<T> comp2 = s.GetComponent<ValueField<T>>();
            if (comp2 == null) comp2 = s.AttachComponent<ValueField<T>>();
            comp.Source.Target = comp2.Value;
            if (target != null)
                comp.Target.Target = target;
            fieldTracker.field.Target = comp2.Value;
        }
    }
}
