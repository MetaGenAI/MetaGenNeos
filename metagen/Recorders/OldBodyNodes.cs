using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metagen
{
    public enum OldBodyNodes {
    NONE = 0,
    Root = 1,
    LeftController = 2,
    RightController = 3,
    Hips = 4,
    Spine = 5,
    Chest = 6,
    UpperChest = 7,
    Neck = 8,
    Head = 9,
    Jaw = 10, // 0x0000000A
    LeftEye = 11, // 0x0000000B
    RightEye = 12, // 0x0000000C
    LeftShoulder = 13, // 0x0000000D
    LeftUpperArm = 14, // 0x0000000E
    LeftLowerArm = 15, // 0x0000000F
    LeftHand = 16, // 0x00000010
    LeftPalm = 17, // 0x00000011
    LEFT_FINGER_START = 18, // 0x00000012
    LeftThumb_Metacarpal = 18, // 0x00000012
    LeftThumb_Proximal = 19, // 0x00000013
    LeftThumb_Distal = 20, // 0x00000014
    LeftThumb_Tip = 21, // 0x00000015
    LeftIndexFinger_Metacarpal = 22, // 0x00000016
    LeftIndexFinger_Proximal = 23, // 0x00000017
    LeftIndexFinger_Intermediate = 24, // 0x00000018
    LeftIndexFinger_Distal = 25, // 0x00000019
    LeftIndexFinger_Tip = 26, // 0x0000001A
    LeftMiddleFinger_Metacarpal = 27, // 0x0000001B
    LeftMiddleFinger_Proximal = 28, // 0x0000001C
    LeftMiddleFinger_Intermediate = 29, // 0x0000001D
    LeftMiddleFinger_Distal = 30, // 0x0000001E
    LeftMiddleFinger_Tip = 31, // 0x0000001F
    LeftRingFinger_Metacarpal = 32, // 0x00000020
    LeftRingFinger_Proximal = 33, // 0x00000021
    LeftRingFinger_Intermediate = 34, // 0x00000022
    LeftRingFinger_Distal = 35, // 0x00000023
    LeftRingFinger_Tip = 36, // 0x00000024
    LeftPinky_Metacarpal = 37, // 0x00000025
    LeftPinky_Proximal = 38, // 0x00000026
    LeftPinky_Intermediate = 39, // 0x00000027
    LeftPinky_Distal = 40, // 0x00000028
    LEFT_FINGER_END = 41, // 0x00000029
    LeftPinky_Tip = 41, // 0x00000029
    RightShoulder = 42, // 0x0000002A
    RightUpperArm = 43, // 0x0000002B
    RightLowerArm = 44, // 0x0000002C
    RightHand = 45, // 0x0000002D
    RightPalm = 46, // 0x0000002E
    RIGHT_FINGER_START = 47, // 0x0000002F
    RightThumb_Metacarpal = 47, // 0x0000002F
    RightThumb_Proximal = 48, // 0x00000030
    RightThumb_Distal = 49, // 0x00000031
    RightThumb_Tip = 50, // 0x00000032
    RightIndexFinger_Metacarpal = 51, // 0x00000033
    RightIndexFinger_Proximal = 52, // 0x00000034
    RightIndexFinger_Intermediate = 53, // 0x00000035
    RightIndexFinger_Distal = 54, // 0x00000036
    RightIndexFinger_Tip = 55, // 0x00000037
    RightMiddleFinger_Metacarpal = 56, // 0x00000038
    RightMiddleFinger_Proximal = 57, // 0x00000039
    RightMiddleFinger_Intermediate = 58, // 0x0000003A
    RightMiddleFinger_Distal = 59, // 0x0000003B
    RightMiddleFinger_Tip = 60, // 0x0000003C
    RightRingFinger_Metacarpal = 61, // 0x0000003D
    RightRingFinger_Proximal = 62, // 0x0000003E
    RightRingFinger_Intermediate = 63, // 0x0000003F
    RightRingFinger_Distal = 64, // 0x00000040
    RightRingFinger_Tip = 65, // 0x00000041
    RightPinky_Metacarpal = 66, // 0x00000042
    RightPinky_Proximal = 67, // 0x00000043
    RightPinky_Intermediate = 68, // 0x00000044
    RightPinky_Distal = 69, // 0x00000045
    RIGHT_FINGER_END = 70, // 0x00000046
    RightPinky_Tip = 70, // 0x00000046
    LeftUpperLeg = 71, // 0x00000047
    LeftLowerLeg = 72, // 0x00000048
    LeftFoot = 73, // 0x00000049
    LeftToes = 74, // 0x0000004A
    RightUpperLeg = 75, // 0x0000004B
    RightLowerLeg = 76, // 0x0000004C
    RightFoot = 77, // 0x0000004D
    RightToes = 78, // 0x0000004E
    END = 79, // 0x0000004F
        }
}
