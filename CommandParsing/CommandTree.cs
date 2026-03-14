using System;
using System.IO;
using MandalaLogics.Path;

namespace MandalaLogics.CommandParsing
{
    public sealed class CommandTree
    {
        private static readonly Func<string, int, CommandTreeNode> parseDeleagte = new Func<string, int, CommandTreeNode>((val, line) => new CommandTreeNode(val, line));

        private readonly ObjectTreeNode<CommandTreeNode> root;
        
        public CommandTree(StreamReader reader)
        {
            root = ObjectTreeNode<CommandTreeNode>.ReadTree(reader, parseDeleagte);
        }
        
        public ParsedCommand ParseCommandLine(CommandLine line)
        {
            if ((root?.Count ?? 0) == 0) { throw new InvalidOperationException("Command tree is not definied."); }

            return root.Value.Parse(line, root);
        }
    }
}