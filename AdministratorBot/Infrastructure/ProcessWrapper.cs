using System.Diagnostics;

namespace AdministratorBot.Infrastructure
{
    public interface IProcessWrapper
    {
        Process StartProcess(ProcessStartInfo processStartInfo);
        Process GetProcessById(int processId);
        Process[] GetProcesses();
        void StopProcess(int processId);
        void StopProcess(Process process);
    }
    public class ProcessWrapper : IProcessWrapper
    {
        public Process StartProcess(ProcessStartInfo processStartInfo)
        {
            return Process.Start(processStartInfo);
        }

        public Process GetProcessById(int processId)
        {
            return Process.GetProcessById(processId);
        }

        public Process[] GetProcesses()
        {
            return Process.GetProcesses();
        }

        public void StopProcess(int processId)
        {
            var process = GetProcessById(processId);
            StopProcess(process);
        }

        public void StopProcess(Process process)
        {
            process.Kill();
            process.WaitForExit();
            process.Dispose();
        }
    }
}
