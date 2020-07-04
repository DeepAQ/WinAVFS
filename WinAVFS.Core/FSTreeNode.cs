using System.Collections.Generic;

namespace WinAVFS.Core
{
    public class FSTreeNode
    {
        public FSTreeNode Parent { get; internal set; }

        public string Name { get; internal set; } = "";

        public string FullName { get; internal set; } = "";

        public long Length { get; internal set; } = 0;

        public long CompressedLength { get; internal set; } = 0;

        public object Context { get; internal set; }

        public Dictionary<string, FSTreeNode> Children { get; internal set; }

        public bool IsDirectory => this.Children != null;

        public FSTreeNode() : this(false)
        {
        }

        public FSTreeNode(bool isDirectory)
        {
            if (isDirectory)
            {
                this.Children = new Dictionary<string, FSTreeNode>();
            }
        }

        public FSTreeNode GetOrAddChild(bool isDirectory, string name, long length = 0, long compressedLength = 0,
            object context = null)
        {
            if (this.Children == null)
            {
                return null;
            }

            if (this.Children.ContainsKey(name))
            {
                return this.Children[name];
            }

            var child = new FSTreeNode(isDirectory)
            {
                Parent = this,
                Name = name,
                FullName = $"{this.FullName}{name}{(isDirectory ? "/" : "")}",
                Length = length,
                CompressedLength = compressedLength,
                Context = context,
            };
            this.Children[name] = child;

            if (!isDirectory)
            {
                var parent = this;
                while (parent != null)
                {
                    parent.Length += length;
                    parent.CompressedLength += compressedLength;
                    parent = parent.Parent;
                }
            }

            return child;
        }
    }
}