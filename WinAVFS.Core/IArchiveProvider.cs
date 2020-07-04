using System;
using System.IO;

namespace WinAVFS.Core
{
    public interface IArchiveProvider : IDisposable
    {
        FSTree ReadFSTree();

        void ExtractFile(object context, Stream target);
    }
}