using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SevenZip;

namespace WinAVFS.Core
{
    public class SevenZipProvider : IArchiveProvider
    {
        private readonly ThreadLocal<SevenZipExtractor> threadSafeArchive;

        public SevenZipProvider(string path)
        {
            Console.WriteLine($"Opening archive {path} with 7z.dll");
            this.threadSafeArchive = new ThreadLocal<SevenZipExtractor>(() => new SevenZipExtractor(path));
        }

        public void Dispose()
        {
            foreach (var archive in this.threadSafeArchive.Values)
            {
                archive.Dispose();
            }
        }

        public FSTree ReadFSTree()
        {
            var root = new FSTreeNode(true);
            var archive = threadSafeArchive.Value;
            long preAllocSize = 0;
            var entryCount = 0;
            foreach (var entry in archive.ArchiveFileData)
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
                    node = node.GetOrAddChild(entry.IsDirectory, paths[paths.Length - 1], (long) entry.Size,
                        (long) entry.Size, entry.Index);
                    if (!node.IsDirectory && node.Buffer == IntPtr.Zero)
                    {
                        node.Buffer = Marshal.AllocHGlobal((IntPtr) node.Length);
                        preAllocSize += node.Length;
                        Console.WriteLine($"TotalPreAllocSize = {preAllocSize}");
                        entryCount++;
                    }
                }
            }

            Console.WriteLine($"Loaded {entryCount} entries from archive");
            return new FSTree {Root = root};
        }

        public void ExtractFileUnmanaged(FSTreeNode node, IntPtr buffer)
        {
            if (!(node.Context is int index))
            {
                throw new ArgumentException();
            }

            unsafe
            {
                using var target = new UnmanagedMemoryStream((byte*) buffer.ToPointer(), node.Length, node.Length,
                    FileAccess.Write);
                threadSafeArchive.Value.ExtractFile(index, target);
            }
        }
    }
}