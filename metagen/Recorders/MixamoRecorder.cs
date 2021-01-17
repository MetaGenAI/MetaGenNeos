/* Deprecated, but keeping just in case.. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using BaseX;
using System.Threading;
using System.IO;
using UnityEngine;
using CodeX;
using metagen;


namespace FrooxEngine.LogiX
{

    [Category("LogiX/AAAA")]
    [NodeName("MetaGenMixamo")]
    public class MixamoRecorder : LogixNode
    {
        //Obsolete class to record the positions of all the bones, even if they are not being tracked
        //public SpinQueue<SyncMessage> messagesToTransmit;
        public Dictionary<User, List<Slot>> joint_slots = new Dictionary<User, List<Slot>>();
        public BinaryWriter writer;
        public System.IO.StreamWriter output_file;
        public bool record_skeleton = false;
        protected override void OnAttach()
        {
            base.OnAttach();
            if (this.LocalUser.UserID == "U-guillefix")
            {
            UniLog.Log("Getting users list");
            Dictionary<RefID, User>.ValueCollection users = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.AllUsers;
            //List<string> left_leg_joint_names = ["Left_upper _leg", "Left_lower_leg", "Left_foot", "Left_foot_end"];
            //List<string> right_leg_joint_names = ["Right_upper_leg", "Right_lower_leg", "Right_foot", "Right_foot_end"];
            //List<string> spine_joint_names = ["Spine", "chest", "Neck"];
            //List<string> left_arm_joint_names = ["Left_shoulder", "Left_upper_arm", "Left_lower_arm", "Left_Hand"];
            //List<string> right_arm_joint_names = ["Right_shoulder", "Right_upper_arm", "Right_lower_arm", "Right_Hand"];
            //List<string> all_joints = ["Hips"];
            UniLog.Log("Creating list of joints");
            List<string> left_leg_joint_names = new List<string> {"LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase" };
            List<string> right_leg_joint_names = new List<string> { "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase" };
            List<string> spine_joint_names = new List<string> { "Spine", "Spine1", "Spine2", "Neck" };
            List<string> left_arm_joint_names = new List<string> { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" };
            List<string> right_arm_joint_names = new List<string> { "RightShoulder", "RightArm", "RightForeArm", "RightHand" };
            List<string> all_joints = new List<string> { "Hips" };
            all_joints.AddRange(right_leg_joint_names);
            all_joints.AddRange(left_leg_joint_names);
            all_joints.AddRange(spine_joint_names);
            all_joints.AddRange(right_arm_joint_names);
            all_joints.AddRange(left_arm_joint_names);
            all_joints = all_joints.Select(x => "mixamorig:" + x).ToList();

            //UniLog.Log("Adding Audio Listener");
            //GameObject gameObject = GameObject.Find("AudioListener");
            //UnityNeos.AudioRecorderNeos recorder = gameObject.AddComponent<UnityNeos.AudioRecorderNeos>();
            //GameObject gameObject2 = new GameObject("HIII");
            //gameObject2.AddComponent<AudioListener>();
            //gameObject2.AddComponent<UnityNeos.AudioRecorderNeos>();
            //recorder.StartWriting("test.wav");

            UniLog.Log("Getting user joint slots");
            foreach (User user in users)
            {
                List<Slot> temp_list = new List<Slot>();
                bool has_compatible_avatar = true;
                Slot root_slot = user.Root.Slot;
                for (int i = 0; i < all_joints.Count; i++)
                {
                    Slot joint_slot = root_slot.FindChild(s => s.Name == all_joints[i]);
                    if (joint_slot == null)
                    {
                        has_compatible_avatar = false;
                        break;
                    } else
                    {
                        temp_list.Add(joint_slot);
                    }
                }
                if (has_compatible_avatar)
                {
                    joint_slots[user] = temp_list.ToList();
                }
            }

            UniLog.Log("Creating output file");
            output_file = new System.IO.StreamWriter("test.txt");
            //FileStream fs = new FileStream("savingFile.dat", FileMode.Create, FileAccess.ReadWrite);
            //writer = new BinaryWriter(fs);
            }
        }

        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();
            if (this.LocalUser.UserID == "U-guillefix")
            {
                UniLog.Log("Update");
                if (record_skeleton)
                {
                    foreach (var item in joint_slots)
                    {
                        User user = item.Key;
                        List<Slot> slots = item.Value;

                        Slot root_slot = slots[0];
                        float3 root_global_position = root_slot.GlobalPosition;
                        floatQ root_global_rotation = root_slot.GlobalRotation;
                        output_file.Write(root_global_position.ToString() + "," + root_global_rotation.ToString());
                        for (int i = 1; i < slots.Count; i++)
                        {
                            Slot slot = slots[i];
                            float3 global_position = slot.GlobalPosition;
                            floatQ global_rotation = slot.GlobalRotation;
                            float3 relative_position = root_slot.GlobalPointToLocal(global_position);
                            floatQ relative_rotation = root_slot.GlobalRotationToLocal(global_rotation);
                            output_file.Write("," + relative_position.ToString() + "," + relative_rotation.ToString());
                        }
                        output_file.Write("\n");
                    }
                }

            }

        }
    static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        return false;
    }

    }
}
