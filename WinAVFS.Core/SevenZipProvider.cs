using System;
using System.IO;
using SevenZipExtractor;

namespace WinAVFS.Core
{
    public class SevenZipProvider : IArchiveProvider
    {
        private readonly ArchiveFile archive;

        public SevenZipProvider(string path)
        {
            Console.WriteLine($"Opening archive {path} with 7z.dll");
            this.archive = new ArchiveFile(path);
        }

        public void Dispose()
        {
            this.archive.Dispose();
        }

        public FSTree ReadFSTree()
        {
            var root = new FSTreeNode(true);
            foreach (var entry in this.archive.Entries)
            {
                Console.WriteLine($"Loading {entry.FileName} into FS tree");
                var paths = entry.FileName.Split('/', '\\');
                var node = root;
                for (var i = 0; i < paths.Length - 1; i++)
                {
                    node = node.GetOrAddChild(true, paths[i]);
                }

                var name = paths[paths.Length - 1];
                if (!string.IsNullOrEmpty(name))
                {
                    node = node.GetOrAddChild(false, name, (long) entry.Size, (long) entry.PackedSize, entry);
                }

                node.Context = entry;
            }

            Console.WriteLine($"Loaded {archive.Entries.Count} entries from archive");
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