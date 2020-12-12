using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using CodeX;
using UnityEngine;
using System.IO;

namespace metagen
{
    class VisionRecorder
    {
        private Dictionary<RefID, FrooxEngine.Camera> cameras = new Dictionary<RefID, FrooxEngine.Camera>();
        private Dictionary<RefID, VideoRecorder> visual_recorders = new Dictionary<RefID, VideoRecorder>();
        private List<string> current_users_ids = new List<string>();
        public int2 camera_resolution;
        public bool isRecording = false;
        public string saving_folder;
        private MetaGen metagen_comp;
        public VisionRecorder(int2 resolution, MetaGen component)
        {
            camera_resolution = resolution;
            metagen_comp = component;
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
                    metagen_comp.StartTask((Func<Task>)(async () =>
                    {
                        Bitmap2D bmp = await cameras[user_id].RenderToBitmap(camera_resolution);
                        visual_recorders[user_id].WriteFrame(bmp.ConvertTo(CodeX.TextureFormat.BGRA32).RawData);
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
            World currentWorld = metagen_comp.World;
            Dictionary<RefID, User>.ValueCollection users = currentWorld.AllUsers;
            currentWorld.RunSynchronously(() =>
            {
                foreach (User user in users)
                {
                    RefID user_id = user.ReferenceID;
                    current_users_ids.Add(user_id.ToString());
                    Slot localSlot = user.Root.HeadSlot.AddLocalSlot("vision recorder camera");
                    FrooxEngine.Camera camera = localSlot.AttachComponent<FrooxEngine.Camera>();
                    camera.GetRenderSettings(camera_resolution);
                    camera.NearClipping.Value = 0.15f;
                    cameras[user_id] = camera;
                    int fps = 30;
                    visual_recorders[user_id] = new VideoRecorder(saving_folder + "/" + user_id.ToString() + "_video.avi", camera_resolution.x, camera_resolution.y, fps, metagen_comp);
                }
                UniLog.Log("Made visual recorder");
                isRecording = true;
            });
        }

        public void StopRecording()
        {
            //World currentWorld = metagen_comp.World;
            //currentWorld.RunSynchronously(() =>
            //{
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
            Task task = Task.Run(() =>
            {
                foreach (string user_id in current_users_ids)
                {
                    File.Move(saving_folder + "/" + user_id.ToString() + "_video.avi", saving_folder + "/" + user_id.ToString() + "_vision.avi");
                }
                current_users_ids = new List<string>();
            });
            task.Wait();
            //});
        }
    }
}
