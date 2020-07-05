using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Zip;
using SevenZipExtractor;

namespace WinAVFS.Core
{
    public class SevenZipProvider : IArchiveProvider
    {
        private readonly ArchiveFile archive;
        private readonly ZipFile zipFile;
        private readonly HashSet<string> zip64Files;

        public SevenZipProvider(string path)
        {
            Console.WriteLine($"Opening archive {path} with 7z.dll");
            this.archive = new ArchiveFile(path);

            var getFormatMethod = typeof(ArchiveFile).GetMethod("GuessFormatFromSignature",
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] {typeof(string), typeof(SevenZipFormat).MakeByRefType()}, null);
            var getFormatParams = new object[] {path, null};

            if (getFormatMethod != null && (bool) getFormatMethod.Invoke(this.archive, getFormatParams) &&
                (SevenZipFormat) getFormatParams[1] == SevenZipFormat.Zip)
            {
                Console.WriteLine($"Archive format is ZIP, applying Zip64 mitigation");
                this.zipFile = new ZipFile(path);
                this.zip64Files = new HashSet<string>();
                foreach (ZipEntry entry in this.zipFile)
                {
                    if (entry.CentralHeaderRequiresZip64)
                    {
                        this.zip64Files.Add(entry.Name.Replace('/', '\\'));
                    }
                }

                this.zipFile.Close();
            }
        }

        public void Dispose()
        {
            this.archive.Dispose();
        }

        public FSTree ReadFSTree()
        {
            var root = new FSTreeNode(true);
            long preAllocSize = 0;
            var entryCount = 0;
            foreach (var entry in this.archive.Entries)
            {
                Console.WriteLine($"Loading {entry.FileName} into FS tree");
                var paths = entry.FileName.Split('/', '\\');
                var node = root;
                for (var i = 0; i < paths.Length - 1; i++)
                {
                    node = node.GetOrAddChild(true, paths[i]);
                }

                if (!string.IsNullOrEmpty(paths[paths.Length - 1]))
                {
                    node = node.GetOrAddChild(entry.IsFolder, paths[paths.Length - 1], (long) entry.Size,
                        (long) entry.Size, entry);
                    if (!node.IsDirectory && node.Buffer == IntPtr.Zero)
                    {
                        node.Buffer = Marshal.AllocHGlobal((IntPtr) node.Length);
                        preAllocSize += node.Length;
                        Console.WriteLine($"PreAllocSize = {preAllocSize}");
                        entryCount++;
                    }
                }

                if (zip64Files.Contains(entry.FileName))
                {
                    Console.WriteLine($"Zip64 mitigation: updating entry of {entry.FileName}");
                    node.Context = entry;
                }
            }

            Console.WriteLine($"Loaded {entryCount} entries from archive");
            return new FSTree {Root = root};
        }

        public void ExtractFileUnmanaged(FSTreeNode node, IntPtr buffer)
        {
            if (!(node.Context is Entry entry))
            {
                throw new ArgumentException();
            }

            unsafe
            {
                using var target = new UnmanagedMemoryStream((byte*) buffer.ToPointer(), node.Length, node.Length,
                    FileAccess.Write);
                entry.Extract(target);
            }
        }
    }
}