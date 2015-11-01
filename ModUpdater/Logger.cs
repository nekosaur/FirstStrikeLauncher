using System;
using System.IO;
using System.Configuration;
using System.Security;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace BF2Utils
{
	public static class Logger
	{
	    private static DirectoryInfo directoryInfo;
	    private static FileStream fileStream;
	    private static StreamWriter streamWriter;
	    private static StackTrace stackTrace;
	    private static MethodBase methodBase;
		
	    private static void Info(Object info)
	    {
	        //Gets folder & file information of the log file
	        string folderName = Directory.GetCurrentDirectory();
	        string fileName = Application.ProductName + "-errors.log";
	        string user = string.Empty;
	
	        directoryInfo = new DirectoryInfo(folderName);
	
	        //Check for existence of logger file
	        if (File.Exists(fileName))
	        {
	            try
	            {
	                fileStream = new FileStream(fileName, FileMode.Append, FileAccess.Write);
	        
	                streamWriter = new StreamWriter(fileStream);
	        
	                string val = DateTime.Now.ToString() + " " + info.ToString();
	
	                streamWriter.WriteLine(val);
	
	            }
	            catch (ConfigurationErrorsException ex)
	            {
	                LogInfo(ex);
	            }
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
	
	                directoryInfo = Directory.CreateDirectory(directoryInfo.FullName);
	
	                fileStream = File.Create(fileName);
	
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


	    public static void LogInfo(Exception ex)
	    {
	        try
	        {
	            stackTrace = new StackTrace(ex, true);
	            methodBase = stackTrace.GetFrame((stackTrace.FrameCount - 1)).GetMethod();
	            String methodName = methodBase.Name;

                Info("Error in method " + methodName + ". Message: " + ex.Message);	
	        }
	        catch (Exception genEx)
	        {
	            Info(genEx.Message);
	            Logger.LogInfo(genEx);
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