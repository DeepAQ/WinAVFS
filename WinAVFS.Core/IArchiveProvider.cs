using System;

namespace WinAVFS.Core
{
    public interface IArchiveProvider : IDisposable
    {
        FSTree ReadFSTree();
    }
}