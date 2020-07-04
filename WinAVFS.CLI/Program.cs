using System;
using WinAVFS.Core;

namespace WinAVFS.CLI
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var fs = new ReadOnlyAVFS();
            fs.Mount('Z');
            Console.ReadLine();
            fs.Unmount('Z');
        }
    }
}