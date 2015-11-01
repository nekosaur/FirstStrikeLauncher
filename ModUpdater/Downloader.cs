using System;
using System.Net;
using System.Threading;
using System.IO;
using Ionic.Zip;

namespace BF2Utils
{
    public class DownloadFailedException : System.Exception
    {
        public DownloadFailedException()
        {
        }
    }

    public class FileCorruptException : System.Exception
    {
        public FileCorruptException()
        {
        }
    }

	public static class Downloader
	{
		private static HttpWebRequest request;
		
		private static int _retryAttemps = 0;
		
		public static bool Get(string fileUrl, string localPath, bool firstAttempt)
		{
		    bool result = false;

			if (firstAttempt)
				_retryAttemps = 2;

            try
            {
                byte[] buffer;
                int bytesRead;
                long fileSize;
                long downloaded = 0;

                buffer = new byte[1024];

                request = (HttpWebRequest)WebRequest.Create(new Uri(fileUrl));
                //request.Timeout = 100000;
                //request.ReadWriteTimeout = 100000;
                //request.ContentType = "binary";
                request.Method = "GET";
                request.Accept = "*/*";
                request.AllowAutoRedirect = true;
                request.AllowWriteStreamBuffering = false;
                request.KeepAlive = true;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    fileSize = response.ContentLength;

                    using (Stream stream = response.GetResponseStream())
                    {
                        using (FileStream fs = new FileStream(localPath, FileMode.Create))
                        {
                            do
                            {
                                bytesRead = stream.Read(buffer, 0, buffer.Length);

                                downloaded += bytesRead;

                                fs.Write(buffer, 0, bytesRead);

                                if (fileSize < 1000)
                                {
                                    Console.Write("[{0}/{1}B] {2}%\r", (int)(downloaded), (int)(fileSize), (int)(downloaded / (0.01 * fileSize)));
                                }
                                else
                                {
                                    Console.Write("[{0}/{1}kB] {2}%\r", (int)(downloaded / 1000), (int)(fileSize / 1000), (int)(downloaded / (0.01 * fileSize)));
                                }
                            } while (bytesRead > 0);
                        }
                    }
                }

                if (new FileInfo(localPath).Length <= 0)
                    throw new FileCorruptException();

                Console.WriteLine("  Download done!                      ");

                result = true;

            }
            catch (FileCorruptException)
            {
                if (_retryAttemps <= 0)
                {
                    Console.WriteLine("Unable to succesfully download file. The update server might be down.");
                }
                else
                {
                    Console.WriteLine("Downloaded file was corrupt. Retrying...");

                    if (File.Exists(localPath))
                        File.Delete(localPath);

                    _retryAttemps--;

                    Get(fileUrl, localPath, false);
                }
            }
            catch (WebException ex)
            {
                if (_retryAttemps <= 0)
                {
                    Console.WriteLine("Unable to download file. The update server might be down.");
                    Logger.LogInfo(ex);
                }
                else
                {
                    Console.WriteLine("Download failed. Retrying...");

                    if (File.Exists(localPath))
                        File.Delete(localPath);

                    _retryAttemps--;

                    Get(fileUrl, localPath, false);
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

		    return result;
		}
	}
}

