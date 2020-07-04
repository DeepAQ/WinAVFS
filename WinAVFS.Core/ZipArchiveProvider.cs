using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WinAVFS.Core
{
    public class ZipArchiveProvider : IArchiveProvider
    {
        private ZipArchive archive;

        public ZipArchiveProvider(string path)
        {
            Console.WriteLine($"Opening archive {path}");
            this.archive = new ZipArchive(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read),
                ZipArchiveMode.Read, false, Encoding.Default);
        }

        public void Dispose()
        {
            this.archive?.Dispose();
        }

        public FSTree ReadFSTree()
        {
            var root = new FSTreeNode(true);
            foreach (var entry in archive.Entries)
            {
                Console.WriteLine($"Loading {entry.FullName} into FS tree");
                var paths = entry.FullName.Split('/');
                var node = root;
                for (var i = 0; i < paths.Length - 1; i++)
                {
                    node = node.GetOrAddChild(true, paths[i]);
                }

                var name = paths[paths.Length - 1];
                if (!string.IsNullOrEmpty(name))
                {
                    node.GetOrAddChild(false, name, entry.Length, entry.CompressedLength, entry);
                }
                else
                {
                    node.Context = entry;
                }
            }

            Console.WriteLine($"Loaded {archive.Entries.Count} entries from archive");
            return new FSTree {Root = root};
        }
    }
}