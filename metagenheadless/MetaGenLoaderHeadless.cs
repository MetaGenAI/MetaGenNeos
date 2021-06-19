/*
 * This is just a node that i keep spawend in my local world so that it loads the actual MetaMetaGen component
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;

namespace FrooxEngine.LogiX
{
    [Category("LogiX/AAAA")]
    [NodeName("MetaGenLoader")]
    class MetaGenLoader : LogixNode
    {
        protected override void OnStart()
        {
            base.OnStart();
            Slot slot = this.World.AddSlot("meta gen");
            slot.AttachComponent<MetaMetaGen>();
        }

    }
}
