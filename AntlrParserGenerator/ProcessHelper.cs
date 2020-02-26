using System.Diagnostics;
using System.IO;

namespace AntlrParserGenerator
{
    public class ProcessHelper
    {
        public static void ExecuteCommand(string filePath, string command, DataReceivedEventHandler outputEventHandler=null, DataReceivedEventHandler errorEventHandler=null)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";           
            if (outputEventHandler != null)
            {
                p.OutputDataReceived += outputEventHandler;
            }

            if (errorEventHandler != null)
            {
                p.ErrorDataReceived += errorEventHandler;
            }

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            if (outputEventHandler != null)
            {
                p.BeginOutputReadLine();
            }

            if (errorEventHandler != null)
            {
                p.BeginErrorReadLine();
            }

            string folder = Path.GetDirectoryName(filePath);
            p.StandardInput.WriteLine(Path.GetPathRoot(folder).Trim('\\'));
            p.StandardInput.WriteLine($"cd {folder}");
            p.StandardInput.WriteLine(command);

            p.StandardInput.AutoFlush = true;
            p.StandardInput.Flush();
            p.StandardInput.Close();
            p.WaitForExit();
            p.Close();
            p.Dispose();
        }
    }
}
