/*Code by Adrian Werimo for Pathways International*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;


namespace SFTP_Download_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = @"ftp.watersmartsoftware.com";
            string username = @"nweldgate";

            string remoteDirectory = @"/downloads/";
            string localDirectory = @"C:\Rawfiles-WSmartEnrolement\";

            //var GetDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            PrivateKeyFile keyFile = new PrivateKeyFile(localDirectory + @"\nweld_rsa_key.ppk"); //Had to convert the ppk file I downloaded to OpenSSH format for compatibility with SSH.NET
            var keyFiles = new[] { keyFile };

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

            ConnectionInfo conn = new ConnectionInfo(host, 22, username, methods.ToArray());
            using (var sftp = new SftpClient(conn))
            {
                try
                {
                    sftp.Connect();

                    //List all the files in the remote directory
                    var files = sftp.ListDirectory(remoteDirectory);

                    //List only the files which start with the letter N
                    List<SftpFile> fs = new List<SftpFile>();
                    fs.AddRange(files.Where(r => r.Name.StartsWith("n")));

                    //List the last modified dates of all files which start the letter N
                    List<DateTime> filesDate = new List<DateTime>();
                    filesDate.AddRange(fs.Select(q => q.LastAccessTime.Date));

                    //Pick the date of the most recently modified file whose name starts with the letter N
                    DateTime latest = filesDate.Max(p => p);

                    foreach (var file in fs)
                    {
                        string remoteFileName = file.Name;

                        if (file.LastWriteTime.Date == latest)
                        {
                            //Create the local directory if it doesn't exist
                            if (!Directory.Exists(localDirectory))
                                Directory.CreateDirectory(localDirectory);
                            //Download the file to the local directory
                            using (Stream downloadFile = File.OpenWrite(localDirectory + remoteFileName))
                            {
                                sftp.DownloadFile(remoteDirectory + remoteFileName, downloadFile);
                            }
                            //Extract the contents of the downloaded ZIP file to the local directory 
                            ZipFile.ExtractToDirectory(localDirectory + remoteFileName, localDirectory);
                            //Delete the downloaded ZIP file
                            File.Delete(localDirectory + remoteFileName);

                            Console.WriteLine("File has been downloaded and extracted to " + localDirectory);                       
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    //Delete any existing ZIP files on the directory
                    string[] filePaths = Directory.GetFiles(localDirectory, @"*.zip");
                    foreach (var filePath in filePaths)
                    {
                        File.Delete(filePath);
                    }
                }
                finally
                {
                    sftp.Disconnect();
                    Console.WriteLine("Press any key to exit...");
                    //Console.ReadKey();
                }
            }
        }
    }
}
