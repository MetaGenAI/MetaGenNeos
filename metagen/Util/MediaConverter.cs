using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using BaseX;

namespace metagen.Util
{
    class MediaConverter
    {
        static List<Task> ffmpegtasks = new List<Task>();
        static System.Collections.Specialized.StringCollection log = new System.Collections.Specialized.StringCollection();
        public static bool busy = false;
        public static void Run()
        {
            //Console.WriteLine("Hello World!");
            while (true)
            {
                try
                {
                    //UniLog.Log("hi");
                    Task video_task = Task.Run(() => WalkDirectoryTree(new System.IO.DirectoryInfo("./data"), "video"));
                    Task audio_task = Task.Run(() => WalkDirectoryTree(new System.IO.DirectoryInfo("./data"), "audio"));
                    busy = true;
                    video_task.Wait();
                    audio_task.Wait();
                    busy = false;
                } catch (Exception e)
                {
                    UniLog.Log("OwO: " + e.Message);
                    UniLog.Log(e.StackTrace);
                }
                Thread.Sleep(1000);
            }
        }
        static void WalkDirectoryTree(System.IO.DirectoryInfo root, string media_type)
        {
            try
            {
                System.IO.FileInfo[] files = null;
                System.IO.DirectoryInfo[] subDirs = null;

                // First, process all the files directly under this folder
                try
                {
                    //files = root.GetFiles("*_vision.avi");
                    if (media_type == "video")
                    {
                        files = root.GetFiles("*_vision.avi");
                    } else if (media_type == "audio")
                    {
                        
                        FileInfo[] files_audio = root.GetFiles("*_voice.wav");
                        FileInfo[] files_hearing =  root.GetFiles("*_hearing.wav");
                        files = new FileInfo[files_audio.Length + files_hearing.Length];
                        files_audio.CopyTo(files, 0);
                        files_hearing.CopyTo(files, files_audio.Length);
                    } else
                    {
                        UniLog.Log("OwO: Type of media conversion not supported in the Media Converter");
                    }
                }
                // This is thrown if even one of the files requires permissions greater
                // than the application provides.
                catch (UnauthorizedAccessException e)
                {
                    // This code just writes out the message and continues to recurse.
                    // You may decide to do something different here. For example, you
                    // can try to elevate your privileges and access the file again.
                    log.Add(e.Message);
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    UniLog.Log(e.Message);
                }

                if (files != null)
                {
                    foreach (System.IO.FileInfo fi in files)
                    {
                        string ffmpgCmdText = "";
                        UniLog.Log("converting " + fi.FullName);
                        if (media_type == "video")
                        {
                            string new_name = fi.FullName.Substring(0, fi.FullName.Length - 3) + "mp4";
                            ffmpgCmdText = "-hide_banner -loglevel warning -y -i \"" + fi.FullName + "\" \"" + new_name + "\"";
                        } else if (media_type == "audio")
                        {
                            string new_name = fi.FullName.Substring(0, fi.FullName.Length - 3) + "ogg";
                            ffmpgCmdText = "-hide_banner -loglevel warning -y -i \"" + fi.FullName + "\" \"" + new_name + "\"";
                        } else
                        {
                            UniLog.Log("OwO: Type of media conversion not supported in the Media Converter");
                            break;
                        }
                        Process ffmpegProcess = new System.Diagnostics.Process();
                        //processes.Add(ffmpegProcess);
                        ProcessStartInfo processInfo = new ProcessStartInfo();
                        processInfo.FileName = "ffmpeg.exe";
                        processInfo.Arguments = ffmpgCmdText;
                        ffmpegProcess.StartInfo = processInfo;
                        ffmpegProcess.EnableRaisingEvents = true;
                        ffmpegProcess.Exited += (object sender, EventArgs target) => DeleteFile((string) fi.FullName.Clone());
                        ffmpegProcess.Start();
                        ffmpegProcess.WaitForExit();
                        //ffmpegProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs target) => DeleteFile((string) fi.FullName.Clone());
                        //ffmpegProcess.Disposed += (object sender, EventArgs target) => { DeleteFile(fi.FullName); }; 
                    }

                    // Now find all the subdirectories under this directory.
                    subDirs = root.GetDirectories();

                    foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                    {
                        // Resursive call for each subdirectory.
                        WalkDirectoryTree(dirInfo, media_type);
                    }
                }
            } catch (Exception e)
            {
                UniLog.Log("OwO: " + e.Message);
                UniLog.Log(e.StackTrace);
            }
        }
        static private void DeleteFile(string fileName)
        {
            try
            {
                UniLog.Log("DELETING " + fileName);
                File.Delete(fileName);
            } catch (Exception e)
            {
                UniLog.Log("OwO: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        static public void WaitForFinish()
        {
            //TODO: add max iters
            Task task = Task.Run(() =>
            {
                int iter = 0;
                while (busy) { Thread.Sleep(10); iter += 1; }
            });
            task.Wait();
        }

    }
}
