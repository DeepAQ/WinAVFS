using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using DokanNet;
using DokanNet.Logging;
using FileAccess = DokanNet.FileAccess;

namespace WinAVFS.Core
{
    public class ReadOnlyAVFS : IDokanOperations
    {
        private IArchiveProvider archiveProvider;
        private FSTree fsTree;

        public ReadOnlyAVFS(IArchiveProvider archiveProvider)
        {
            this.archiveProvider = archiveProvider;
        }

        public void Mount(char driveLetter)
        {
            this.fsTree = this.archiveProvider.ReadFSTree();
            this.Mount($"{driveLetter}:", DokanOptions.WriteProtection, new NullLogger());
        }

        public void Unmount(char driveLetter)
        {
            Dokan.Unmount(driveLetter);
            this.fsTree = null;
        }

        #region Dokan filesystem implementation

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode,
            FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            bytesRead = 0;
            return NtStatus.NotImplemented;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset,
            IDokanFileInfo info)
        {
            bytesWritten = 0;
            return NtStatus.AccessDenied;
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            fileInfo = new FileInformation();
            return NtStatus.NotImplemented;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            Console.WriteLine(fileName);
            files = new List<FileInformation>(0);

            var paths = fileName.Split('\\');
            var node = this.fsTree.Root;
            foreach (var path in paths.Where(x => !string.IsNullOrEmpty(x)))
            {
                if (!node.IsDirectory || !node.Children.ContainsKey(path))
                {
                    return NtStatus.ObjectPathNotFound;
                }

                node = node.Children[path];
            }

            if (!node.IsDirectory)
            {
                return NtStatus.NotADirectory;
            }

            files = node.Children.Select(child => new FileInformation
            {
                FileName = child.Value.Name,
                Length = child.Value.Length,
                Attributes = (child.Value.IsDirectory ? FileAttributes.Directory : 0) | FileAttributes.ReadOnly
            }).ToList();
            return NtStatus.Success;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files,
            IDokanFileInfo info)
        {
            files = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
            DateTime? lastWriteTime, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes,
            out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            freeBytesAvailable = 0;
            totalNumberOfBytes = this.fsTree.Root.Length;
            totalNumberOfFreeBytes = 0;
            return NtStatus.Success;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
            out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = "AVFS";
            features = FileSystemFeatures.VolumeIsCompressed | FileSystemFeatures.ReadOnlyVolume;
            fileSystemName = $"WinAVFS {Assembly.GetExecutingAssembly().GetName().Version}";
            maximumComponentLength = 0;
            return NtStatus.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security,
            AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
            IDokanFileInfo info)
        {
            return NtStatus.AccessDenied;
        }

        public NtStatus Mounted(IDokanFileInfo info)
        {
            Console.WriteLine($"Mounted readonly filesystem");
            return NtStatus.Success;
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            Console.WriteLine($"Unmounted readonly filesystem");
            return NtStatus.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = null;
            return NtStatus.Success;
        }

        #endregion
    }
}