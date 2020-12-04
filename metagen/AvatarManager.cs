using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Threading.Tasks.Extensions;
using FrooxEngine;
using System.Runtime.CompilerServices;
using BaseX;
using FrooxEngine.CommonAvatar;

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
            //slot = currentWorld.AddSlot("Holder");
            UniLog.Log("Adding holder slot");
            currentWorld.RunSynchronously(() =>
            {
                //this.slot = currentWorld.AddLocalSlot("Holder");
                //TODO: in some worlds the global coordinate of recording vs playback is not correct! (e.g. Victorian appartment by Enverex)
                this.slot = currentWorld.LocalUser.Root.Slot.Parent.AddSlot("Holder");
            });
            UniLog.Log("Added");
            //Slot slot1 = Userspace.UserspaceWorld.AddSlot("Holder");
            //Job<Slot> awaiter = SlotHelper.TransferToWorld(slot1,currentWorld).GetAwaiter();
            //slot = awaiter.GetResult();
        }
        public async Task<Slot> GetAvatar()
        {
            TaskAwaiter<Slot> awaiter;
            if (avatar_template == null)
            {
                //Job<Slot> awaiter = SpawnDefaultAvatar().GetAwaiter();
                //awaiter.Wait();
                //avatar_template = awaiter.GetResult();

                Task<Slot> task = Task.Run(async () =>
                {
                    Slot slot = await SpawnDefaultAvatar();
                    avatar_template = slot;
                    return slot;
                });

                return await task;
            }
                return avatar_template;
            //Job<Slot> awaiter2 = DuplicateAvatarTemplate().GetAwaiter();
            //awaiter2.Wait();
            //return awaiter2.GetResult();
        }
        public Job<Slot> DuplicateAvatarTemplate()
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            Job<Slot> task = new Job<Slot>();
            currentWorld.RunSynchronously(() =>
            {
                Slot slot = avatar_template.Duplicate();
                task.SetResultAndFinish(slot);
            });
            return task;
        }
        public async Task<Slot> SpawnDefaultAvatar()
        {
            return await SpawnAvatar("neosdb:///3992605ec9c401672dd54ff388cce3bd6483313699e4e45642b3abe80941d98b.7zbson");
        }
        public async Task<Slot> SpawnAvatar(String neosdb)
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            //Job<Slot> task = new Job<Slot>();
            TaskCompletionSource<Slot> task = new TaskCompletionSource<Slot>();
            currentWorld.RunSynchronously(() => { 
                Engine engine = FrooxEngine.Engine.Current;
                Uri uri = new Uri(neosdb);
                //await slot.LoadObjectAsync(uri, (Slot)null, (ReferenceTranslator)null);
                ValueTaskAwaiter<string> gatherAwaiter = engine.AssetManager.RequestGather(uri, Priority.Urgent).GetAwaiter();
                string nodeString = gatherAwaiter.GetResult();
                DataTreeDictionary node = DataTreeConverter.Load(nodeString, uri);
                UniLog.Log("slot");
                UniLog.Log(slot.ToString());
                slot.LoadObject(node);
                slot = slot.GetComponent<InventoryItem>((Predicate<InventoryItem>)null, false)?.Unpack((List<Slot>)null) ?? slot;
                List<IAvatarObject> components = slot.GetComponentsInChildren<IAvatarObject>();
                //AvatarRoot root = slot.GetComponentInChildren<AvatarRoot>();
                Slot fake_root = currentWorld.AddSlot("Fake Root");
                //TODO: find a way to do this without a custom component, because this won't work in normal sessions as it is!
                FingerPlayerSource player_source = fake_root.AttachComponent<FingerPlayerSource>();
                List<HandPoser> handPosers = slot.GetComponentsInChildren<HandPoser>();
                foreach (IAvatarObject comp in components)
                {
                    AvatarObjectSlot comp2;
                    if (comp.Node == BodyNode.Root)
                    {
                        comp2 = fake_root.AttachComponent<AvatarObjectSlot>();
                        comp2.Node.Value = comp.Node;
                        comp2.Equipped.Target = comp;
                        comp.Equip(comp2);
                    } else
                    {
                        Slot new_proxy = fake_root.AddSlot(comp.Name);
                        comp2 = new_proxy.AttachComponent<AvatarObjectSlot>();
                        comp2.Node.Value = comp.Node;
                        comp2.Equipped.Target = comp;
                        comp.Equip(comp2);
                    }
                }
                foreach(HandPoser handPoser in handPosers)
                {
                    handPoser.PoseSource.Target = player_source;
                }
                slot.SetParent(fake_root);
                task.SetResult(fake_root);
                //avatars.Add(slot);
                //avatars[avatars.Count - 1].AttachComponent<AvatarPuppeteer>();
            });
            return await task.Task.ConfigureAwait(false);
        }
    }
}
