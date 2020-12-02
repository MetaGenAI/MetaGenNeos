using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;

namespace metagen
{
    class FingerPlayerSource : Component, IFingerPoseSource
    {

        public bool tracksMetacarpals = false;
        Dictionary<BodyNode, floatQ> finger_rotations = new Dictionary<BodyNode, floatQ>();

        public FingerPlayerSource()
        {
        }

        public void UpdateFingerPose(BodyNode node, floatQ rotation)
        {
            finger_rotations[node] = rotation;
        }

        bool IFingerPoseSource.TracksMetacarpals
        {
            get
            {
                return this.tracksMetacarpals;
            }
        }

        public bool AreFingersTracking(Chirality chirality)
        {
            switch (chirality)
            {
                case Chirality.Left:
                    return true;
                case Chirality.Right:
                    return true;
                default:
                    return false;
            }
        }
        public void GetFingerData(BodyNode node, out float3 position, out floatQ rotation)
        {
            position = float3.Zero;
            if (finger_rotations.ContainsKey(node))
            {
                rotation = finger_rotations[node];
            } else
            {
                rotation = floatQ.Identity;
            }
        }
        public bool TryGetFingerData(BodyNode node, out float3 position, out floatQ rotation)
        {
            GetFingerData(node, out position, out rotation);
            return true;
        }
        public void GetFingerData(List<BodyNode> nodes, List<float3> positions, List<floatQ> rotations)
        {
            foreach (BodyNode node in nodes)
            {
                floatQ rotation;
                this.GetFingerData(node, out float3 _, out rotation);
                rotations.Add(rotation);
            }
        }
        public void GetFingerData(Chirality chirality, List<float3> positions, List<floatQ> rotations)
        {
            if (chirality == Chirality.Left)
            {
                for (int index = 0; index < FingerPoseStreamManager.FINGER_NODE_COUNT; ++index)
                {
                    BodyNode node = (BodyNode)(18 + index);
                    floatQ rotation;
                    this.GetFingerData(node, out float3 _, out rotation);
                    rotations.Add(rotation);
                }
            } else
            {
                for (int index = 0; index < FingerPoseStreamManager.FINGER_NODE_COUNT; ++index)
                {
                    BodyNode node = (BodyNode)(47 + index);
                    floatQ rotation;
                    this.GetFingerData(node, out float3 _, out rotation);
                    rotations.Add(rotation);
                }
            }
        }
    }
}
