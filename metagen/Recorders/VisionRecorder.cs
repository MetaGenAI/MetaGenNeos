using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using CodeX;
using UnityEngine;

namespace metagen
{
    class VisionRecorder
    {
        private Dictionary<RefID, FrooxEngine.Camera> cameras = new Dictionary<RefID, FrooxEngine.Camera>();
        private Dictionary<RefID, VideoRecorder> visual_recorders = new Dictionary<RefID, VideoRecorder>();
        public int2 camera_resolution;
        public bool isRecording = false;
        public string saving_folder;
        private FrooxEngine.LogiX.MetaGen comp;
        public VisionRecorder(int2 resolution, FrooxEngine.LogiX.MetaGen component)
        {
            camera_resolution = resolution;
            comp = component;
        }

        //Record one frame of the head camera for each user
        public void RecordVision()
        {
            foreach (var item in visual_recorders)
            {
                RefID user_id = item.Key;
                VideoRecorder videoRecorder = item.Value;
                if (videoRecorder != null && cameras[user_id] != null)
                {
                    comp.StartTask((Func<Task>)(async () =>
                    {
                        Bitmap2D bmp = await cameras[user_id].RenderToBitmap(camera_resolution);
                        visual_recorders[user_id].WriteFrame(bmp.ConvertTo(CodeX.TextureFormat.RGBA32).RawData);
                    }));
                }
                else
                { //something was null:P
                    bool vis_rec_null = false;
                    bool camera_null = false;
                    if (visual_recorders[user_id] == null) vis_rec_null = true;
                    if (cameras[user_id] == null) camera_null = true;
                    UniLog.Log("OwO. These things were null: " + (camera_null ? "Camera" : "") + (vis_rec_null ? "Visual recorder" : ""));
                }
            }
        }
        public void StartRecording()
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            Dictionary<RefID, User>.ValueCollection users = currentWorld.AllUsers;
            currentWorld.RunSynchronously(() =>
            {
                foreach (User user in users)
                {
                    RefID user_id = user.ReferenceID;
                    FrooxEngine.Camera camera = user.Root.HeadSlot.AttachComponent<FrooxEngine.Camera>();
                    camera.GetRenderSettings(camera_resolution);
                    cameras[user_id] = camera;
                    visual_recorders[user_id] = new VideoRecorder(saving_folder + "/" + user_id.ToString() + "_video.avi", camera_resolution.x, camera_resolution.y, 30);
                }
                UniLog.Log("Made visual recorder");
                isRecording = true;
            });
        }

        public void StopRecording()
        {
            World currentWorld = FrooxEngine.Engine.Current.WorldManager.FocusedWorld;
            currentWorld.RunSynchronously(() =>
            {
                foreach (var item in visual_recorders)
                {
                    item.Value.Close();
                    //TODO: check recording several videos consequtively. It doesn't seem to be working
                }
                foreach (var item in cameras)
                {
                    item.Value.Slot.RemoveComponent(item.Value);
                }
                cameras = new Dictionary<RefID, FrooxEngine.Camera>();
                visual_recorders = new Dictionary<RefID, VideoRecorder>();
                isRecording = false;
            });
        }
    }
}
