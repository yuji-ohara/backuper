using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace Backuper
{
    public partial class BackupService : ServiceBase
    {
        public string Path { get; set; }
        private string BackupFolderPath { get; set; }
        private Backuper Backuper { get; set; }
        private FileSystemWatcher fsWatcher { get; set; }

        public BackupService()
        {
            InitializeComponent();
            InitLogs();
            var currentFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            BackupFolderPath = System.IO.Path.Combine(currentFolder, "Backups");
            Backuper = new Backuper();
        }

        private void InitLogs()
        {
            AutoLog = false;
            eventLog1 = new System.Diagnostics.EventLog();
            eventLog1.Source = ServiceName;
            eventLog1.Log = "Application";

            eventLog1.BeginInit();
            if (!System.Diagnostics.EventLog.SourceExists(eventLog1.Source))
            {
                System.Diagnostics.EventLog.CreateEventSource(eventLog1.Source, eventLog1.Log);
            }
            eventLog1.EndInit();
        }

        public void Start()
        {
            OnStart(new string[0]);
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Backuper started", System.Diagnostics.EventLogEntryType.Information);

            CheckBackupFolder();
            CreateBackup();

            fsWatcher = new FileSystemWatcher();
            fsWatcher.Path = Path;
            fsWatcher.Changed += FsWatcher_Changed;
            fsWatcher.Created += FsWatcher_Changed;
            fsWatcher.Deleted += FsWatcher_Changed;
            fsWatcher.Renamed += FsWatcher_Changed;
            fsWatcher.EnableRaisingEvents = true;
        }

        private void FsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            eventLog1.WriteEntry("Creating Backup", System.Diagnostics.EventLogEntryType.Information);
            CheckBackupFolder();
            CreateBackup();
        }

        private void CreateBackup()
        {
            var backupFileName = GetBackupFileName();
            var destinationFilePath = System.IO.Path.Combine(BackupFolderPath, backupFileName);
            try
            {
                Backuper.CreateZipFolder(Path, destinationFilePath);
            }
            catch(Exception ex)
            {
                eventLog1.WriteEntry($"Backuper.CreateZipFolder-{ex.Message}", System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void CheckBackupFolder()
        {
            if (!Directory.Exists(BackupFolderPath))
                Directory.CreateDirectory(BackupFolderPath);
            else
            {
                var dirInfo = new DirectoryInfo(BackupFolderPath);
                var dirFiles = dirInfo.GetFiles();
                DeleteOldBackups(dirFiles);
            }
        }

        private void DeleteOldBackups(FileInfo[] dirFiles)
        {
            var regex = new Regex(@"^bkp-(\d){4}-(\d){2}-(\d{2})-(\d{2})h(\d{2})-(\w*\.?)*.zip");
            foreach(var fileInfo in dirFiles)
            {
                if (regex.Match(fileInfo.Name).Success)
                    TryDeleteFile(fileInfo);
            }
        }

        private void TryDeleteFile(FileInfo fileInfo)
        {
            try
            {
                File.Delete(fileInfo.FullName);
            }
            catch(Exception ex)
            {
                eventLog1.WriteEntry($"Backuper.TryDeleteFile-{ex.Message}", System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private string GetBackupFileName()
        {
            var dirName = new DirectoryInfo(Path).Name;
            var now = DateTime.Now;
            var bkpFileName = $"bkp-{now:yyyy-MM-dd}-{now:hh}h{now:mm}-{dirName}.zip";
            return bkpFileName;
        }

        protected override void OnStop()
        {
            fsWatcher.Dispose();
        }
    }
}
