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
        Slot holder_slot;
        //List<Slot> avatars = new List<Slot>();
        public Slot avatar_template;
        public bool has_prepared_avatar = false;

        public AvatarManager()
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            //slot = currentWorld.AddSlot("Holder");
            UniLog.Log("Adding holder slot");
            currentWorld.RunSynchronously(() =>
            {
                //this.slot = currentWorld.AddLocalSlot("Holder");
                //TODO: in some worlds the global coordinate of recording vs playback is not correct! (e.g. Victorian appartment by Enverex)
                this.holder_slot = currentWorld.LocalUser.Root.Slot.Parent.AddSlot("Holder");
            });
            UniLog.Log("Added");
            //Slot slot1 = Userspace.UserspaceWorld.AddSlot("Holder");
            //Job<Slot> awaiter = SlotHelper.TransferToWorld(slot1,currentWorld).GetAwaiter();
            //slot = awaiter.GetResult();
        }
        public async Task<Slot> GetAvatar()
        {
            //TaskAwaiter<Slot> awaiter;
            Task<Slot> task;
            if (avatar_template == null)
            {
                //Job<Slot> awaiter = SpawnDefaultAvatar().GetAwaiter();
                //awaiter.Wait();
                //avatar_template = awaiter.GetResult();

                task = Task.Run(async () =>
                {
                    Slot slot = await SpawnDefaultAvatar();
                    avatar_template = slot;
                    has_prepared_avatar = true;
                    return slot;
                });

                return await task;
            }
            return await DuplicateAvatarTemplate();
            //Job<Slot> awaiter2 = DuplicateAvatarTemplate().GetAwaiter();
            //awaiter2.Wait();
            //return awaiter2.GetResult();
        }
        public async Task<Slot> DuplicateAvatarTemplate()
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            TaskCompletionSource<Slot> task = new TaskCompletionSource<Slot>();
            currentWorld.RunSynchronously(async () => { 
                if (!has_prepared_avatar)
                {
                    avatar_template = PrepareAvatar(avatar_template.Duplicate());
                    has_prepared_avatar = true;
                    task.SetResult(avatar_template);
                } else
                {
                    Slot slot = avatar_template.Duplicate();
                    //    task.SetResultAndFinish(slot);
                    task.SetResult(slot);
                }
            });
            return await task.Task.ConfigureAwait(false);
        }
        public async Task<Slot> SpawnDefaultAvatar()
        {
            return await SpawnAvatar("neosdb:///3992605ec9c401672dd54ff388cce3bd6483313699e4e45642b3abe80941d98b.7zbson");
            //return await SpawnAvatar("neosdb:///f70e161112d7398522d32f043ef500b6d4340b9dd27c3d02824f4639c8da7386.7zbson");
        }
        public async Task<Slot> SpawnAvatar(String neosdb)
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            //Job<Slot> task = new Job<Slot>();
            TaskCompletionSource<Slot> task = new TaskCompletionSource<Slot>();
            currentWorld.RunSynchronously(async () => { 
                Engine engine = FrooxEngine.Engine.Current;
                Uri uri = new Uri(neosdb);
                //await slot.LoadObjectAsync(uri, (Slot)null, (ReferenceTranslator)null);
                //ValueTaskAwaiter<string> gatherAwaiter = engine.AssetManager.RequestGather(uri, Priority.Urgent).GetAwaiter();
                string nodeString = await engine.AssetManager.RequestGather(uri, Priority.Urgent);
                //string nodeString = gatherAwaiter.GetResult();
                DataTreeDictionary node = DataTreeConverter.Load(nodeString, uri);
                UniLog.Log("slot");
                UniLog.Log(holder_slot.ToString());
                holder_slot.LoadObject(node);
                Slot slot = holder_slot.GetComponent<InventoryItem>((Predicate<InventoryItem>)null, false)?.Unpack((List<Slot>)null) ?? holder_slot;
                Slot fake_root = PrepareAvatar(slot);
                task.SetResult(fake_root);
                //avatars.Add(slot);
                //avatars[avatars.Count - 1].AttachComponent<AvatarPuppeteer>();
            });
            return await task.Task.ConfigureAwait(false);
        }
        public Slot PrepareAvatar(Slot slot)
        {
            //Slot fake_root = null;
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            //TaskCompletionSource<Slot> task = new TaskCompletionSource<Slot>();
            //currentWorld.RunSynchronously(() =>
            //{
            float3 originalScale = slot.LocalScale;
            slot.SetParent(currentWorld.LocalUser.Root.Slot.Parent);
            List<IAvatarObject> components = slot.GetComponentsInChildren<IAvatarObject>();
            //AvatarRoot root = slot.GetComponentInChildren<AvatarRoot>();
            Slot fake_root = currentWorld.AddSlot("Fake Root");
            Slot hidden_slot = fake_root.AddLocalSlot("hand poser local slot");
            //TODO: find a way to do this without a custom component, because this won't work in normal sessions as it is!
            FingerPlayerSource player_source = hidden_slot.AttachComponent<FingerPlayerSource>();
            //FingerPlayerSource player_source = fake_root.AttachComponent<FingerPlayerSource>();
            List<HandPoser> handPosers = slot.GetComponentsInChildren<HandPoser>();
            float3 avatarScale = originalScale;
            foreach (IAvatarObject comp in components)
            {
                AvatarObjectSlot comp2;
                if (comp.Node == BodyNode.Root)
                {
                    avatarScale = ((AvatarRoot)comp).Scale;
                    comp2 = fake_root.AttachComponent<AvatarObjectSlot>();
                    comp2.Node.Value = comp.Node;
                    comp2.Equipped.Target = comp;
                    comp.Equip(comp2);
                }
                else
                {
                    Slot new_proxy = fake_root.AddSlot(comp.Name);
                    comp2 = new_proxy.AttachComponent<AvatarObjectSlot>();
                    comp2.Node.Value = comp.Node;
                    comp2.Equipped.Target = comp;
                    comp.Equip(comp2);
                }
            }
            //foreach(HandPoser handPoser in handPosers)
            //{
            //handPoser.PoseSource.Target = player_source;

            //Slot new_hidden_slot = hidden_slot.AddLocalSlot("hand local slot");
            //new_hidden_slot.Parent = handPoser.Slot.Parent;
            //HandPoser new_hand_poser = new_hidden_slot.AttachComponent<HandPoser>();
            //new_hand_poser.Side.Value = handPoser.Side.Value;
            //new_hand_poser.HandRoot.Target = handPoser.Slot;
            //BipedRig rig = handPoser.FindCompatibleRig();
            //handPoser.Slot.RemoveComponent(handPoser);
            //new_hand_poser.AssignFingers(rig);

            //BodyNode side1 = BodyNode.LeftThumb_Metacarpal.GetSide((Chirality)new_hand_poser.Side);
            //BodyNode side2 = BodyNode.LeftPinky_Tip.GetSide((Chirality)new_hand_poser.Side);
            //for (BodyNode nodee = side1; nodee <= side2; ++nodee)
            //{
            //    int index = nodee - side1;
            //    FingerType fingerType = nodee.GetFingerType();
            //    FingerSegmentType fingerSegmentType = nodee.GetFingerSegmentType();
            //    HandPoser.FingerSegment fingerSegment = new_hand_poser[fingerType][fingerSegmentType];
            //    if (fingerSegment != null && fingerSegment.RotationDrive.IsLinkValid)
            //    {
            //        fingerSegment.RotationDrive.Target.ReleaseLink(fingerSegment.RotationDrive.Target.DirectLink);
            //    }
            //}
            //HandPoser new_hand_poser = new_hidden_slot.CopyComponent<HandPoser>(handPoser);
            //new_hand_poser.HandRoot.Target = handPoser.Slot;
            //new_hand_poser.CopyProperties(handPoser);
            //new_hand_poser.CopyValues(handPoser);
            //new_hand_poser.Side.Value = handPoser.Side.Value;
            //handPoser.Enabled = false;

            //new_hand_poser.PoseSource.Target = player_source;
            //}
            //foreach (HandPoser handPoser in handPosers)
            //{
            //    handPoser.Slot.RemoveComponent(handPoser);
            //}
            slot.SetParent(fake_root);
            slot.LocalScale = originalScale;
                return fake_root;
            //    task.SetResult(fake_root);
            //});
            //return await task.Task.ConfigureAwait(false);
        }
    }
}
