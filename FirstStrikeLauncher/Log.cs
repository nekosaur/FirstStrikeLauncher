using System;
using System.IO;
using System.Configuration;
using System.Security;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace FirstStrikeLauncher
{
    public static class Log
    {
        private static DirectoryInfo directoryInfo;
        private static FileStream fileStream;
        private static StreamWriter streamWriter;
        private static StackTrace stackTrace;
        private static MethodBase methodBase;
        private static string _folderName;

        private static void Info(Object info)
        {

            //Gets folder & file information of the log file

            //string folderName = Directory.GetCurrentDirectory();
            string folderName = _folderName;
            //string folderName = Path.Get
            //ConfigurationManager.AppSettings["LogFolder"].ToString();

            string fileName = Application.ProductName + "-errors.log";
            //ConfigurationManager.AppSettings["FileName"].ToString();
            string user = string.Empty;

            string fullPath = Path.Combine(folderName, fileName);

            //directoryInfo = new DirectoryInfo(folderName);

            //Check for existence of logger file
            if (File.Exists(fullPath))
            {
                try
                {
                    fileStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write);

                    streamWriter = new StreamWriter(fileStream);

                    string val = DateTime.Now.ToString() + " " + info.ToString();

                    streamWriter.WriteLine(val);

                }
                //catch (ConfigurationErrorsException ex)
                //{
                 //   LogInfo(ex);
                //}
                catch (DirectoryNotFoundException ex)
                {
                    LogInfo(ex);
                }
                catch (FileNotFoundException ex)
                {
                    LogInfo(ex);
                }
                catch (PathTooLongException ex)
                {
                    LogInfo(ex);
                }
                catch (ArgumentException ex)
                {
                    LogInfo(ex);
                }
                catch (SecurityException ex)
                {
                    LogInfo(ex);
                }
                catch (Exception Ex)
                {
                    LogInfo(Ex);
                }
                finally
                {
                    Dispose();
                }
            }
            else
            {

                //If file doesn't exist create one
                try
                {

                    //directoryInfo = Directory.CreateDirectory(directoryInfo.FullName);

                    fileStream = File.Create(fullPath);

                    streamWriter = new StreamWriter(fileStream);

                    String val1 = DateTime.Now.ToString() + " " + info.ToString();

                    streamWriter.WriteLine(val1);

                    streamWriter.Close();

                    fileStream.Close();

                }
                catch (FileNotFoundException fileEx)
                {
                    LogInfo(fileEx);
                }
                catch (DirectoryNotFoundException dirEx)
                {
                    LogInfo(dirEx);
                }
                catch (Exception ex)
                {
                    LogInfo(ex);
                }
                finally
                {
                    Dispose();
                }

            }
        }

        public static void SetOutputFolder(string folder)
        {
            _folderName = folder;

            return;
        }

        public static void LogInfo(Exception ex)
        {
            try
            {

                //Writes error information to the log file including name of the file, line number & error message description
                //Console.WriteLine("Start");
                stackTrace = new StackTrace(ex, true);
                //Console.WriteLine("Frames: {0}", stackTrace.FrameCount);
                //Console.WriteLine("Frame 0: {0}", stackTrace.GetFrame(0).GetFileName());
                //string fileNames = stackTrace.GetFrame((stackTrace.FrameCount - 1)).GetFileName();
                //Console.WriteLine(fileNames);
                //fileNames = fileNames.Substring(fileNames.LastIndexOf(Application.ProductName));
                //Console.WriteLine("Rawr");
                //Int32 lineNumber = stackTrace.GetFrame((stackTrace.FrameCount - 1)).GetFileLineNumber();
                //Console.WriteLine(lineNumber);
                methodBase = stackTrace.GetFrame((stackTrace.FrameCount - 1)).GetMethod();    //These two lines are respnsible to find out name of the method

                String methodName = methodBase.Name;

                Info("Error in method " + methodName + ". Message: " + ex.Message);
                //Info("Error in " + fileNames + ". Method name is " + methodName + ", at line number " + lineNumber.ToString() + ". Error Message: " + ex.Message);

            }
            catch (Exception genEx)
            {
                Info(genEx.Message);
                Log.LogInfo(genEx);
            }
            finally
            {
                Dispose();
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                //Write general message to the log file
                Info("Message-----" + message);
            }
            catch (Exception genEx)
            {
                Info(genEx.Message);
            }

        }

        private static void Dispose()
        {
            if (directoryInfo != null)
                directoryInfo = null;

            if (streamWriter != null)
            {
                streamWriter.Close();
                streamWriter.Dispose();
                streamWriter = null;
            }
            if (fileStream != null)
            {
                fileStream.Dispose();
                fileStream = null;
            }
            if (stackTrace != null)
                stackTrace = null;
            if (methodBase != null)
                methodBase = null;
        }
    }
}