using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace ArcToCore.Core.HandballBoard.VideoCutter
{
    public static class Program
    {
        /// <summary>
        /// Defines the checkVideoList.
        /// </summary>
        private static List<string> checkVideoList = new List<string>();

        /// <summary>
        /// The DisplayMenu.
        /// </summary>
        static public void DisplayMenu()
        {
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("ArcToCore Video Cutter Version 1.3");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// The AddTribleQuotes.
        /// </summary>
        /// <param name="value">The value<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string AddTribleQuotes(this string value)
        {
            return "\"\"" + value + "\"\"";
        }

        /// <summary>
        /// The AddDoubleQuotes.
        /// </summary>
        /// <param name="value">The value<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string AddDoubleQuotes(this string value)
        {
            return "\"" + value + "\"";
        }

        /// <summary>
        /// The Main.
        /// </summary>
        private static void Main()
        {
            GenerateVideo();
            int count = 10;
            bool run = true;
            while (run)
            {
                if (count == 0)
                {
                    run = false;
                }

                if (!IsVideoGeneratingComplete())
                {
                    GenerateVideo();
                }
                else
                {
                    run = false;
                }


                count--;
            }
        }

        /// <summary>
        /// The IsVideoGeneratingComplete.
        /// </summary>
        /// <returns>The <see cref="bool"/>.</returns>
        private static bool IsVideoGeneratingComplete()
        {
            var alreadyGeneratedFiles = Directory.GetFiles(Environment.CurrentDirectory + @"\VideoOutput\", "*.mp4*").ToList();

            if (checkVideoList.Count != alreadyGeneratedFiles.Count)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// The GenerateVideo.
        /// </summary>
        private static void GenerateVideo()
        {
            List<string> pausedFilesList = new List<string>();
            int videoFormat = 1;

            string[] mp4File = Directory.GetFiles(Environment.CurrentDirectory + @"\VideoFile\", "*.mp4");

            if (string.IsNullOrEmpty(mp4File[0]))
            {
                Console.WriteLine("VideoFile folder contains no mp4 files.");
                return;
            }

            string[] filePaths = Directory.GetFiles(Environment.CurrentDirectory + @"\VideoTrackInput\", "*.json");
            ProcessStartInfo startInfo = new ProcessStartInfo();

            var alreadyGeneratedFiles = Directory.GetFiles(Environment.CurrentDirectory + @"\VideoOutput\", "*.*").ToList();

            string line;
            var path = Environment.CurrentDirectory + @"\";

            foreach (var fileItem in filePaths)
            {
                StreamReader file = new StreamReader(fileItem);
                line = file.ReadToEnd();

                var pathFmpeg = path + "ffmpeg.exe";

                var tracks = line.Split("|");

                foreach (var trackSegment in tracks)
                {
                    if (string.IsNullOrEmpty(trackSegment))
                    {
                        continue;
                    }

                    if (trackSegment.Equals(" "))
                    {
                        continue;
                    }

                    string filename = trackSegment.Replace(@"\r", "").Replace(@"\n", "").Trim().Split(' ').GetValue(0).ToString().Trim();

                    FileInfo fi = new FileInfo(filename);

                    checkVideoList.Add(fi.Name);

                    if (alreadyGeneratedFiles.Any(x => new FileInfo(x).Name.Split('.').GetValue(0).ToString().Equals(fi.Name.Split('.').GetValue(0).ToString())))
                    {
                        //Console.WriteLine(fi.Name + " already exsists. Try delete the file.");
                        continue;
                    }

                    string split1 = trackSegment.Split(' ').GetValue(1).ToString();

                    string split2 = trackSegment.Split(' ').GetValue(3).ToString();

                    if (split1.Equals(split2))
                    {
                        Console.WriteLine("Split time for:" + split1 + " is equal to:" + split2);
                        continue;
                    }

                    string oldFile = string.Empty;

                    if (videoFormat.Equals(1))
                    {
                        oldFile = path + "VideoOutput\\" + filename + ".mp4";
                    }

                    if (File.Exists(oldFile))
                    {
                        File.Delete(oldFile);
                    }
                    Thread.Sleep(10000);

                    if (videoFormat.Equals(1))
                    {
                        startInfo.Arguments = " -i " + AddDoubleQuotes(mp4File[0]) + " -ss " + split1 + " -to " + split2 + " -c:v libx265 -crf 28 -c:a aac -b:a 128k " + AddDoubleQuotes(path + "VideoOutput\\" + filename + ".mp4");
                    }

                    startInfo.CreateNoWindow = false;
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = pathFmpeg;
                    startInfo.RedirectStandardOutput = true;

                    Console.WriteLine(string.Format(
                        "Executing \"{0}\" with arguments \"{1}\".\r\n",
                        startInfo.FileName,
                        startInfo.Arguments));

                    try
                    {
                        Console.Write("Prepare to generate video tracks....");
                        using (var progress = new ProgressBar())
                        {
                            for (int i = 0; i <= 100; i++)
                            {
                                progress.Report((double)i / 100);
                                Thread.Sleep(100);
                            }
                        }
                        Console.WriteLine("Ready.");

                        using (Process process = Process.Start(startInfo))
                        {
                            while (!process.StandardOutput.EndOfStream)
                            {
                                string linex = process.StandardOutput.ReadLine();
                                Console.WriteLine(linex);
                            }

                            process.WaitForExit();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Equals("Access is denied"))
                        {
                            pausedFilesList.Add(startInfo.Arguments);
                        }
                    }
                }

                if (pausedFilesList.Count > 0)
                {
                    try
                    {
                        foreach (var item in pausedFilesList)
                        {
                            startInfo.Arguments = item;
                            using (Process process = Process.Start(startInfo))
                            {
                                while (!process.StandardOutput.EndOfStream)
                                {
                                    string linex = process.StandardOutput.ReadLine();
                                    Console.WriteLine(linex);
                                }

                                process.WaitForExit();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                file.Close();
            }
        }
    }
}