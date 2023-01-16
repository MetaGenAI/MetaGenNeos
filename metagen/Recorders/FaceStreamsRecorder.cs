using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using System.IO;
using FrooxEngine.CommonAvatar;
using Stream = FrooxEngine.Stream;
using RefID = BaseX.RefID;

namespace metagen
{
    public class EyeStreamRecorder : UserBinaryDataRecorder, IRecorder
    {
        public Dictionary<RefID, EyeTrackingStreamManager> eye_streams = new Dictionary<RefID, EyeTrackingStreamManager>();
        public List<RefID> current_eye_tracking_users = new List<RefID>();
        public bool isRecording = false;
        public EyeStreamRecorder(MetaGen component) : base(component)
        {
            metagen_comp = component;
        }

        void WriteStreams(EyeTrackingStreamManager.EyeStreams eyeStreams, BinaryWriterX writer)
        {
            writer.Write(eyeStreams.IsTracking.Target.Value); //bool
            writer.Write(eyeStreams.Position.Target.Value.x); //float
            writer.Write(eyeStreams.Position.Target.Value.y); //float
            writer.Write(eyeStreams.Position.Target.Value.z); //float
            writer.Write(eyeStreams.Direction.Target.Value.x); //float
            writer.Write(eyeStreams.Direction.Target.Value.y); //float
            writer.Write(eyeStreams.Direction.Target.Value.z); //float
            writer.Write(eyeStreams.Openness.Target.Value); //float
            writer.Write(eyeStreams.Widen.Target.Value); //float
            writer.Write(eyeStreams.Squeeze.Target.Value); //float
            writer.Write(eyeStreams.Frown.Target.Value); //float
            writer.Write(eyeStreams.PupilDiameter.Target.Value); //float
        }
        public void RecordStreams(float deltaT)
        {
            foreach (RefID user_id in current_eye_tracking_users)
            {
                //Encode the streams
                BinaryWriterX writer = output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float

                EyeTrackingStreamManager eyeStreams = eye_streams[user_id];
                //WRITE left eye streams
                WriteStreams(eyeStreams.LeftEyeStreams, writer);
                //WRITE right eye streams
                WriteStreams(eyeStreams.RightEyeStreams, writer);

            }
        }
        public void StartRecording()
        {
            source_type = Source.FILE;
            foreach (var userItem in metagen_comp.userMetaData)
            {
                User user = userItem.Key;
                UserMetadata metadata = userItem.Value;
                //if (!metadata.isRecording || (metagen_comp.LocalUser == user && !metagen_comp.record_local_user)) continue;
                if (!metadata.isRecording) continue;
                RefID user_id = user.ReferenceID;
                bool has_eye_tracking = user.Devices.Where<SyncVar>((Func<SyncVar, bool>)(i => i.IsDictionary)).Any<SyncVar>((Func<SyncVar, bool>)(i => i["Type"].GetValue<string>(true) == "Eye Tracking"));

                if (has_eye_tracking) {
                    RegisterUserStream(user_id, "eye_streams");
                    eye_streams[user_id] = user.Root.Slot.GetComponent<EyeTrackingStreamManager>();
                    current_eye_tracking_users.Add(user_id);
                }

            }
            isRecording = true;
        }
        public void StopRecording()
        {
            UnregisterUserStreams();
            current_eye_tracking_users = new List<RefID>();
            eye_streams = new Dictionary<RefID, EyeTrackingStreamManager>();
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }
    }
    public class MouthStreamRecorder : UserBinaryDataRecorder, IRecorder
    {
        public Dictionary<RefID, MouthTrackingStreamManager> mouth_streams = new Dictionary<RefID, MouthTrackingStreamManager>();
        public List<RefID> current_mouth_tracking_users = new List<RefID>();
        public bool isRecording = false;
        public MouthStreamRecorder(MetaGen component) : base(component)
        {
            metagen_comp = component;
        }

        void WriteStreams(MouthTrackingStreamManager mouthStreams, BinaryWriterX writer)
        {
            writer.Write(mouthStreams.IsTracking.Target.Value); //bool
            writer.Write(mouthStreams.Jaw.Target.Value.x); //float
            writer.Write(mouthStreams.Jaw.Target.Value.y); //float
            writer.Write(mouthStreams.Jaw.Target.Value.z); //float
            writer.Write(mouthStreams.JawOpen.Target.Value); //float
            writer.Write(mouthStreams.Tongue.Target.Value.x); //float
            writer.Write(mouthStreams.Tongue.Target.Value.y); //float
            writer.Write(mouthStreams.Tongue.Target.Value.z); //float
            writer.Write(mouthStreams.TongueRoll.Target.Value); //float
            writer.Write(mouthStreams.LipUpperLeftRaise.Target.Value); //float
            writer.Write(mouthStreams.LipUpperRightRaise.Target.Value); //float
            writer.Write(mouthStreams.LipLowerLeftaise.Target.Value); //float
            writer.Write(mouthStreams.LipLowerRightRaise.Target.Value); //float
            writer.Write(mouthStreams.LipUpperHorizontal.Target.Value); //float
            writer.Write(mouthStreams.LipLowerHorizontal.Target.Value); //float
            writer.Write(mouthStreams.MouthLeftSmileFrown.Target.Value); //float
            writer.Write(mouthStreams.MouthRightSmileFrown.Target.Value); //float
            writer.Write(mouthStreams.MouthPout.Target.Value); //float
            writer.Write(mouthStreams.LipTopOverturn.Target.Value); //float
            writer.Write(mouthStreams.LipBottomOverturn.Target.Value); //float
            writer.Write(mouthStreams.LipTopOverUnder.Target.Value); //float
            writer.Write(mouthStreams.LipBottomOverUnder.Target.Value); //float
            writer.Write(mouthStreams.CheekLeftPuffSuck.Target.Value); //float
            writer.Write(mouthStreams.CheekRightPuffSuck.Target.Value); //float
        }
        public void RecordStreams(float deltaT)
        {
            foreach (RefID user_id in current_mouth_tracking_users)
            {
                //Encode the streams
                BinaryWriterX writer = output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float

                MouthTrackingStreamManager mouthStreams = mouth_streams[user_id];
                //WRITE mouth streams
                WriteStreams(mouthStreams, writer);
            }
        }
        public void StartRecording()
        {
            source_type = Source.FILE;
            foreach (var userItem in metagen_comp.userMetaData)
            {
                User user = userItem.Key;
                UserMetadata metadata = userItem.Value;
                if (!metadata.isRecording || (metagen_comp.LocalUser == user && !metagen_comp.record_local_user)) continue;
                RefID user_id = user.ReferenceID;
                bool has_mouth_tracking = user.Devices.Where<SyncVar>((Func<SyncVar, bool>)(i => i.IsDictionary)).Any<SyncVar>((Func<SyncVar, bool>)(i => i["Type"].GetValue<string>(true) == "Lip Tracking"));

                if (has_mouth_tracking) {
                    RegisterUserStream(user_id, "mouth_streams");
                    mouth_streams[user_id] = user.Root.Slot.GetComponent<MouthTrackingStreamManager>();
                    current_mouth_tracking_users.Add(user_id);
                }
            }
            isRecording = true;
        }
        public void StopRecording()
        {
            UnregisterUserStreams();
            current_mouth_tracking_users = new List<RefID>();
            mouth_streams = new Dictionary<RefID, MouthTrackingStreamManager>();
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }
    }
}
