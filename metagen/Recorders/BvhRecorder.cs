using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using FrooxEngine.FinalIK;
using System.IO;
using RefID = BaseX.RefID;

namespace metagen
{
    public class BvhRecorder : IRecorder
    {
        public MetaGen metagen_comp;
        Dictionary<RefID, IKSolverVR.References> boness = new Dictionary<RefID, IKSolverVR.References>();
        Dictionary<RefID, StreamWriter> fileWriters = new Dictionary<RefID, StreamWriter>();
        Dictionary<RefID, string> filenames = new Dictionary<RefID, string>();
        Dictionary<BodyNode, bool> tracking_rotations = new Dictionary<BodyNode, bool>();
        public bool isRecording = false;
        Dictionary<BodyNode, int> boneDepths = new Dictionary<BodyNode, int>(){
            {BodyNode.Hips, 0},
            {BodyNode.Spine, 1},
            {BodyNode.Chest, 2},
            {BodyNode.Neck, 3},
            {BodyNode.Head, 4},
            {BodyNode.LeftShoulder, 3},
            {BodyNode.LeftUpperArm, 4},
            {BodyNode.LeftLowerArm, 5},
            {BodyNode.LeftHand, 6},
            {BodyNode.RightShoulder, 3},
            {BodyNode.RightUpperArm, 4},
            {BodyNode.RightLowerArm, 5},
            {BodyNode.RightHand, 6},
            {BodyNode.LeftUpperLeg, 1},
            {BodyNode.LeftLowerLeg, 2},
            {BodyNode.LeftFoot, 3},
            {BodyNode.LeftToes, 4},
            {BodyNode.RightUpperLeg, 1},
            {BodyNode.RightLowerLeg, 2},
            {BodyNode.RightFoot, 3},
            {BodyNode.RightToes, 4},
        };
        Dictionary<BodyNode, BodyNode> boneParents = new Dictionary<BodyNode, BodyNode>(){
            {BodyNode.Hips, BodyNode.Root},
            {BodyNode.Spine, BodyNode.Hips},
            {BodyNode.Chest, BodyNode.Spine},
            {BodyNode.Neck, BodyNode.Chest},
            {BodyNode.Head, BodyNode.Neck},
            {BodyNode.LeftShoulder, BodyNode.Chest},
            {BodyNode.LeftUpperArm, BodyNode.LeftShoulder},
            {BodyNode.LeftLowerArm, BodyNode.LeftUpperArm},
            {BodyNode.LeftHand, BodyNode.LeftLowerArm},
            {BodyNode.RightShoulder, BodyNode.Chest},
            {BodyNode.RightUpperArm, BodyNode.RightShoulder},
            {BodyNode.RightLowerArm, BodyNode.RightUpperArm},
            {BodyNode.RightHand, BodyNode.RightLowerArm},
            {BodyNode.LeftUpperLeg, BodyNode.Hips},
            {BodyNode.LeftLowerLeg, BodyNode.LeftUpperLeg},
            {BodyNode.LeftFoot, BodyNode.LeftLowerLeg},
            {BodyNode.LeftToes, BodyNode.LeftFoot},
            {BodyNode.RightUpperLeg, BodyNode.Hips},
            {BodyNode.RightLowerLeg, BodyNode.RightUpperLeg},
            {BodyNode.RightFoot, BodyNode.RightLowerLeg},
            {BodyNode.RightToes, BodyNode.RightFoot},
        };
        List<BodyNode> bonesList = new List<BodyNode>()
        {
            BodyNode.Hips,
            BodyNode.Spine,
            BodyNode.Chest,
            BodyNode.Neck,
            BodyNode.Head,
            BodyNode.LeftShoulder,
            BodyNode.LeftUpperArm,
            BodyNode.LeftLowerArm,
            BodyNode.LeftHand,
            BodyNode.RightShoulder,
            BodyNode.RightUpperArm,
            BodyNode.RightLowerArm,
            BodyNode.RightHand,
            BodyNode.LeftUpperLeg,
            BodyNode.LeftLowerLeg,
            BodyNode.LeftFoot,
            BodyNode.LeftToes,
            BodyNode.RightUpperLeg,
            BodyNode.RightLowerLeg,
            BodyNode.RightFoot,
            BodyNode.RightToes
        };
        public BvhRecorder(MetaGen component)
        {
            metagen_comp = component;
        }
        public string saving_folder {
            get {
                return metagen_comp.dataManager.saving_folder;
            }
        }

        public void RecordFrame()
        {
            foreach(User user in metagen_comp.World.AllUsers)
            {
                RefID user_id = user.ReferenceID;
                if (boness.ContainsKey(user_id)) {
                    IKSolverVR.References bones = boness[user_id];
                    BodyNode node = bonesList[0];
                    if (bones[node] != null)
                    {
                        Slot bone = bones[node];
                        float3 pos = bone.GlobalPosition;
                        float3 rot = bone.GlobalRotation.EulerAngles;
                        fileWriters[user_id].Write(string.Format("{0:0.000000}\t{1:0.000000}\t{2:0.000000}", pos.X, pos.Y, pos.Z) + "\t");
                        fileWriters[user_id].Write(string.Format("{0:0.000000}\t{1:0.000000}\t{2:0.000000}", rot.Z, rot.X, rot.Y) + "\t");
                    }
                    for (int i = 1; i < bonesList.Count; i++)
                    {
                        node = bonesList[i];
                        if (bones[node] != null)
                        {
                            Slot bone = bones[node];
                            float3 rot = bone.LocalRotationToSpace(floatQ.Identity, bones[boneParents[node]]).EulerAngles;
                            //float3 rot = bone.LocalRotation.EulerAngles;
                            //float3 pos = bone.LocalPointToSpace(float3.Zero, bones[boneParents[node]]);
                            //float3 rot = floatQ.LookRotation(pos).EulerAngles;
                            if (tracking_rotations[node])
                                fileWriters[user_id].Write(string.Format("{0:0.000000}\t{1:0.000000}\t{2:0.000000}", rot.Z, rot.X, rot.Y) + "\t");
                        }

                    }
                    fileWriters[user_id].Write("\n");
                }
            }

        }

        public void StartRecordingAvatars(Dictionary<RefID,Slot> avatar_roots, string override_filename = null)
        {
            foreach (var item in avatar_roots)
            {
                RefID user_id = item.Key;
                Slot rootSlot = item.Value;
                VRIK comp = rootSlot.GetComponentInChildren<VRIK>();
                if (comp != null)
                {
                    IKSolverVR solver = (IKSolverVR) comp.Solver;
                    boness[user_id] = solver.BoneReferences;
                    string filename = "";
                    if (override_filename != null)
                    {
                       filename = saving_folder + "/" + override_filename + "_mocap.bvh";
                    } else
                    {
                       filename = saving_folder + "/" + user_id.ToString() + "_mocap.bvh";
                    }
                    fileWriters[user_id] =  new System.IO.StreamWriter(filename);
                    filenames[user_id] = filename;
                    BvhHeaderWrite(fileWriters[user_id], boness[user_id]);
                }
            }
            isRecording = true;
        }
        public void StartRecording()
        {
            Dictionary<RefID, Slot> avatar_roots = new Dictionary<RefID, Slot>();
            foreach (var item in metagen_comp.userMetaData)
            {
                User user = item.Key;
                UserMetadata metadata = item.Value;
                UniLog.Log("user " + user.UserName);
                if (!(metadata.isRecording || metagen_comp.record_everyone)) continue;
                RefID user_id = user.ReferenceID;
                Slot rootSlot = user.Root?.Slot;
                avatar_roots[user_id] = rootSlot;
            }
            StartRecordingAvatars(avatar_roots);
        }
        void BvhHeaderWrite(StreamWriter writer, IKSolverVR.References bones)
        {
            writer.WriteLine("HIERARCHY");
            BodyNode node = bonesList[0];
            int depth = boneDepths[node];
            string tabs = new string('\t', depth);
            if (bones[node] != null)
            {
                Slot bone = bones[node];
                writer.WriteLine(tabs + "ROOT\t" + node.ToString());
                writer.WriteLine(tabs + "{");
                writer.WriteLine(tabs + "\t" + "OFFSET\t0.00\t0.00\t0.00");
                writer.WriteLine(tabs + "\t" + "CHANNELS\t6\tXposition\tYposition\tZposition\tZrotation\tXrotation\tYrotation");
                tracking_rotations[node] = true;
            }
            int last_depth = depth;
            for (int i = 1; i < bonesList.Count - 1; i++)
            {
                node = bonesList[i];
                depth = boneDepths[node];
                BodyNode next_node = bonesList[i+1];
                depth = boneDepths[node];
                int next_depth = boneDepths[next_node];
                tabs = new string('\t', depth);
                if (bones[node] != null)
                {
                    int closing_brackets = Math.Max(last_depth + 1 - depth, 0);
                    for (int j = 0; j < closing_brackets; j++)
                    {
                        string lasttabs = new string('\t', last_depth);
                        writer.WriteLine(lasttabs.Substring(0, last_depth - j)+"}");
                    }
                    Slot bone = bones[node];
                    //float3 pos = bone.LocalPosition;
                    float3 pos = bone.LocalPointToSpace(new float3(0f, 0f, 0f), bones[boneParents[node]]);
                    pos = pos*bone.GlobalScale;
                    if (next_depth > depth)
                        writer.WriteLine(tabs + "JOINT\t" + node.ToString());
                    else
                        writer.WriteLine(tabs + "End Site");
                    writer.WriteLine(tabs + "{");
                    writer.WriteLine(tabs + "\t" + "OFFSET\t"+ string.Format("{0:0.000000}\t{1:0.000000}\t{2:0.000000}", pos.X, pos.Y, pos.Z));
                    if (next_depth > depth)
                    {
                        writer.WriteLine(tabs + "\t" + "CHANNELS\t3\tZrotation\tXrotation\tYrotation");
                        tracking_rotations[node] = true;
                    } else
                    {
                        tracking_rotations[node] = false;
                    }
                    last_depth = depth;
                }
            }
                node = bonesList[bonesList.Count-1];
                depth = boneDepths[node];
                depth = boneDepths[node];
                tabs = new string('\t', depth);
                if (bones[node] != null)
                {
                    int closing_brackets = Math.Max(last_depth + 1 - depth, 0);
                    for (int j = 0; j < closing_brackets; j++)
                    {
                        string lasttabs = new string('\t', last_depth);
                        writer.WriteLine(lasttabs.Substring(0, last_depth - j)+"}");
                    }
                    Slot bone = bones[node];
                    //float3 pos = bone.LocalPosition;
                    float3 pos = bone.LocalPointToSpace(new float3(0f, 0f, 0f), bones[boneParents[node]]);
                    pos = pos*bone.GlobalScale;
                    writer.WriteLine(tabs + "End Site");
                    writer.WriteLine(tabs + "{");
                    writer.WriteLine(tabs + "\t" + "OFFSET\t"+ string.Format("{0:0.000000}\t{1:0.000000}\t{2:0.000000}", pos.X, pos.Y, pos.Z));
                    tracking_rotations[node] = false;
                    last_depth = depth;
                }
            for (int j = 0; j < Math.Max(last_depth + 1,0); j++)
            {
                string lasttabs = new string('\t', last_depth);
                writer.WriteLine(lasttabs.Substring(0, last_depth - j)+"}");
            }
            writer.WriteLine("MOTION");
            writer.WriteLine("Frame Time: 0.033333");
        }
        public void StopRecording()
        {
            isRecording = false;
            boness = new Dictionary<RefID, IKSolverVR.References>();
            foreach(var item in fileWriters)
            {
                RefID user_id = item.Key;
                StreamWriter writer = item.Value;
                writer.Close();
                metagen_comp.RunSynchronously(() =>
                {
                    string filename = filenames[user_id];
                    UniLog.Log("Import bvh file");
                    Slot s = metagen_comp.LocalUserSpace.AddSlot(Path.GetFileName(filename), true);
                    metagen_comp.StartGlobalTask((Func<Task>)(async () => await UniversalImporter.ImportRawFile(s, filename)));
                });
            }
            fileWriters = new Dictionary<RefID, StreamWriter>();
            tracking_rotations = new Dictionary<BodyNode, bool>();
            boness = new Dictionary<RefID, IKSolverVR.References>();
            filenames = new Dictionary<RefID, string>();
        }
        public void WaitForFinish()
        {

        }
    }
}
