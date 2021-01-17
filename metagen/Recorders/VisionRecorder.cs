using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using CodeX;
using UnityEngine;
using System.IO;

namespace metagen
{
    class VisionRecorder : IRecorder
    {
        private Dictionary<RefID, FrooxEngine.Camera> cameras = new Dictionary<RefID, FrooxEngine.Camera>();
        private Dictionary<RefID, VideoRecorder> visual_recorders = new Dictionary<RefID, VideoRecorder>();
        private List<string> current_users_ids = new List<string>();
        public int2 camera_resolution;
        public bool isRecording = false;
        private MetaGen metagen_comp;
        public string saving_folder {
            get {
                return metagen_comp.dataManager.saving_folder;
                }
        }
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
                    Task task = metagen_comp.StartTask((Func<Task>)(async () =>
                    {
                        Bitmap2D bmp = await cameras[user_id].RenderToBitmap(camera_resolution);
                        visual_recorders[user_id].WriteFrame(bmp.ConvertTo(CodeX.TextureFormat.BGRA32).RawData);
                    }));
                    //TODO: sync video
                    //task.Wait();
                    //World currentWorld = metagen_comp.World;
                    //FrooxEngine.RenderSettings renderSettings = cameras[user_id].GetRenderSettings(camera_resolution);
                    //byte[] data = currentWorld.Render.Connector.Render(renderSettings).Result;
                    //Bitmap2D bmp = new Bitmap2D(data, renderSettings.size.x, renderSettings.size.y, renderSettings.textureFormat, false, true, (string)null);
                    //visual_recorders[user_id].WriteFrame(bmp.ConvertTo(CodeX.TextureFormat.BGRA32).RawData);
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
            current_users_ids = new List<string>();
            currentWorld.RunSynchronously(() =>
            {
                foreach (var item in metagen_comp.userMetaData)
                {
                User user = item.Key;
                UserMetadata metadata = item.Value;
                if (!metadata.isRecording || (!metagen_comp.record_local_user && user == metagen_comp.World.LocalUser)) continue;
                RefID user_id = user.ReferenceID;
                current_users_ids.Add(user_id.ToString());
                Slot localSlot = user.Root.HeadSlot.AddLocalSlot("vision recorder camera");
                FrooxEngine.Camera camera = localSlot.AttachComponent<FrooxEngine.Camera>();
                camera.GetRenderSettings(camera_resolution);
                camera.NearClipping.Value = 0.15f;
                cameras[user_id] = camera;
                int fps = 30;
                visual_recorders[user_id] = new VideoRecorder(Path.Combine(saving_folder,user_id.ToString() + "_vision_tmp.avi"), camera_resolution.x, camera_resolution.y, fps, metagen_comp);
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
                try
                {
                    item.Value.Slot.RemoveComponent(item.Value);
                } catch (Exception e)
                {
                    UniLog.Log("OwO: " + e.Message);
                    UniLog.Log(e.StackTrace);
                }
            }
            cameras = new Dictionary<RefID, FrooxEngine.Camera>();
            visual_recorders = new Dictionary<RefID, VideoRecorder>();
            isRecording = false;
            Task task1 = Task.Run(() =>
            {
                foreach (string user_id in current_users_ids)
                {
                    UniLog.Log("Moving " + Path.Combine(saving_folder, user_id + "_vision_tmp.avi"));
                    File.Move(Path.Combine(saving_folder,user_id + "_vision_tmp.avi"), Path.Combine(saving_folder,user_id + "_vision.avi"));
                }
            });
            task1.Wait();
            //});
        }
        public void WaitForFinish()
        {
            Task[] tasks = new Task[current_users_ids.Count];
            int MAX_WAIT_ITERS = 100000;
            for (int i = 0; i < current_users_ids.Count; i++)
            {
                string user_id = current_users_ids[i];
                Task task2 = Task.Run(() =>
                {
                    int iter = 0;
                    while (!File.Exists(Path.Combine(saving_folder,user_id + "_vision.mp4")) && iter <= MAX_WAIT_ITERS) { Thread.Sleep(10); iter += 1; }
                });
                tasks[i] = task2;
            }
            Task.WaitAll(tasks);
            current_users_ids = new List<string>();
        }
    }
}
