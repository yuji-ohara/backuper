using System.Configuration;
using System.IO;
using System.ServiceProcess;

namespace Backuper
{
    static class Program
    {
        public class BkpArgs {
            public string Path { get; set; } 
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var path = ValidatePath(args);

            if (string.IsNullOrEmpty(path))
                return;

            var bkpService = new BackupService() { Path = path };
            //bkpService.Start();
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                bkpService
            };
            ServiceBase.Run(ServicesToRun);
        }

        private static string ValidatePath(string[] args)
        {
            var path = GetPathFromArgs(args);

            if (string.IsNullOrEmpty(path))
                path = GetPathFromConfig();

            if (Directory.Exists(path))
                return path;

            return string.Empty;
        }

        private static string GetPathFromArgs(string[] args)
        {
            var result = string.Empty;

            var bkpArgs = Args.Configuration.Configure<BkpArgs>().CreateAndBind(args);
            result = bkpArgs.Path;

            return result;
        }

        private static string GetPathFromConfig()
        {
            var result = ConfigurationManager.AppSettings["BackupPath"];

            return result;
        }
    }
}
