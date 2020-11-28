using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Threading.Tasks.Extensions;
using FrooxEngine;
using System.Runtime.CompilerServices;
using BaseX;

namespace metagen
{
    class AvatarManager
    {
        Slot slot;
        //List<Slot> avatars = new List<Slot>();
        Slot avatar_template;

        public AvatarManager()
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            slot = currentWorld.AddSlot("Holder");
            avatar_template = SpawnDefaultAvatar();
        }
        public Slot GetAvatar()
        {
            return avatar_template.Duplicate();
        }
        public Slot SpawnDefaultAvatar()
        {
            return SpawnAvatar("neosdb:///3992605ec9c401672dd54ff388cce3bd6483313699e4e45642b3abe80941d98b.7zbson");
        }
        public Slot SpawnAvatar(String neosdb)
        {
            Engine engine = FrooxEngine.Engine.Current;
            Uri uri = new Uri(neosdb);
            //await slot.LoadObjectAsync(uri, (Slot)null, (ReferenceTranslator)null);
            ValueTaskAwaiter<string> gatherAwaiter = engine.AssetManager.RequestGather(uri, Priority.Urgent).GetAwaiter();
            string nodeString = gatherAwaiter.GetResult();
            DataTreeDictionary node = DataTreeConverter.Load(nodeString, uri);
            slot.LoadObject(node);
            slot = slot.GetComponent<InventoryItem>((Predicate<InventoryItem>)null, false)?.Unpack((List<Slot>)null) ?? slot;
            //avatars.Add(slot);
            //avatars[avatars.Count - 1].AttachComponent<AvatarPuppeteer>();
            return slot;
        }
    }
}
