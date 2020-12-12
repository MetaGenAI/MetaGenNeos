using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading; 

namespace VideoConverter
{
    class Program
    {
        static List<Task> ffmpegtasks = new List<Task>();
        static System.Collections.Specialized.StringCollection log = new System.Collections.Specialized.StringCollection();
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            while (true)
            {
                try
                {
                    Task task = Task.Run(() => WalkDirectoryTree(new System.IO.DirectoryInfo("./data")));
                    task.Wait();
                } catch (Exception e)
                {
                    Console.WriteLine("OwO: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                Thread.Sleep(1000);
            }
        }
        static void WalkDirectoryTree(System.IO.DirectoryInfo root)
        {
            try
            {
                System.IO.FileInfo[] files = null;
                System.IO.DirectoryInfo[] subDirs = null;

                // First, process all the files directly under this folder
                try
                {
                    //files = root.GetFiles("*_vision.avi");
                    files = root.GetFiles("*_vision.avi");
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
                    Console.WriteLine(e.Message);
                }

                if (files != null)
                {
                    foreach (System.IO.FileInfo fi in files)
                    {
                        // In this example, we only access the existing FileInfo object. If we
                        // want to open, delete or modify the file, then
                        // a try-catch block is required here to handle the case
                        // where the file has been deleted since the call to TraverseTree().
                        Console.WriteLine("converting " + fi.FullName);
                        string new_name = fi.FullName.Substring(0, fi.FullName.Length - 10) + "vision.mp4";
                        string ffmpgCmdText;
                        ffmpgCmdText = "-hide_banner -loglevel warning -y -i \"" + fi.FullName + "\" \"" + new_name + "\"";
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
                        WalkDirectoryTree(dirInfo);
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine("OwO: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        static private void DeleteFile(string fileName)
        {
            try
            {
                Console.WriteLine("DELETING " + fileName);
                File.Delete(fileName);
            } catch (Exception e)
            {
                Console.WriteLine("OwO: " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
