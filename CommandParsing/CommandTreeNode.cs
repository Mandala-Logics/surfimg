using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MandalaLogics.Path;

namespace MandalaLogics.CommandParsing
{
    public enum CommandTreeNodeType { Null = 0, Switch, Argument, Command }
    public enum CommandTreeCountType { Null = 0, ZeroOrOne, ZeroOrMore, OneOrMore, FixedRange }

    public sealed class CommandTreeNode
    {
        //STATIC PROPERTIES
        private static readonly Regex lineRegex = new Regex(@"^(?<count>\*|\?|\+|(\d+-\d+|\d\+|\d))?\s*(?<greed>\?|\!+)?(?<type>\^|\$|\%)(?<val>\S+)\s*$", RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        //PUBLIC PROPERTIES
        public CommandTreeNodeType Type {get;}
        public bool IsSwitch => Type == CommandTreeNodeType.Switch;
        public bool IsArgument => Type == CommandTreeNodeType.Argument;
        public bool IsCommand => Type == CommandTreeNodeType.Command;
        public CommandTreeCountType CountType
        {
            get
            {
                if (!Length.HasMax)
                {
                    if (Length.Min == 0) { return CommandTreeCountType.ZeroOrMore; }
                    else { return CommandTreeCountType.OneOrMore; }
                }
                else
                {
                    if (Length.Max == 1 && Length.Min == 0) { return CommandTreeCountType.ZeroOrOne; }
                    else { return CommandTreeCountType.FixedRange; }
                }
            }
        }
        internal CommandRange Length {get;}
        public int Greed {get;}
        public string Line {get;}
        public IReadOnlyList<string> Switches
        {
            get
            {
                if (IsSwitch)
                {
                    return new ReadOnlyListWrapper<string>(switches);
                }
                else { throw new InvalidOperationException($"Cannot get switches for a CommandTreeNode of type {Type}."); }
            }
        }
        public string Argument
        {
            get
            {
                if (IsArgument)
                {
                    return switches[0];
                }
                else { throw new InvalidOperationException($"Cannot get argument for a CommandTreeNode of type {Type}."); }
            }
        }
        public string Command
        {
            get
            {
                if (IsCommand)
                {
                    return switches[0];
                }
                else { throw new InvalidOperationException($"Cannot get command for a CommandTreeNode of type {Type}."); }
            }
        }
        public string SwitchString
        {
            get
            {
                if (IsSwitch)
                {
                    return string.Join('+', switches);
                }
                else { throw new InvalidOperationException($"Cannot get switches for a CommandTreeNode of type {Type}."); }
            }
        }

        //PRIVATE PROPERTIES
        private readonly List<string> switches;

        //CONSTRCUTORS
        public CommandTreeNode(string val, int n)
        {
            Line = val;

            var match = lineRegex.Match(val);            

            if (!match.Success)
            {
                throw new WrongImplimentationException($"Line {n}: Unable to parse line '{val}'");
            }

            var g = match.Groups["count"];
            char c;

            if (g.Success)
            {
                c = g.Value[0];

                Length = c switch
                {
                    '?' => new CommandRange(0, 1),
                    '+' => new CommandRange(1, int.MaxValue),
                    '*' => new CommandRange(0, int.MaxValue),
                    _ => new CommandRange(g.Value, n),
                };
            }
            else
            {
                Length = new CommandRange(1, 1);
            }

            g = match.Groups["greed"];

            if (g.Success)
            {
                if (g.Value == "?")
                {
                    Greed = -1;
                }
                else //equals {n}!
                {
                    Greed = g.Value.Length;
                }
            }
            else
            {
                Greed = 0;
            }

            g = match.Groups["type"];

            c = g.Value[0];

            switch (c)
            {
                case '$':
                    Type = CommandTreeNodeType.Argument;
                    break;
                case '%':
                    Type = CommandTreeNodeType.Switch;
                    if (Length.Min > 1 || Length.Max > 1) { throw new WrongImplimentationException($"Line {n}: switches (%) may only be optional (?) or required (1), no other counts are valid."); }
                    break;
                case '^':
                    Type = CommandTreeNodeType.Command;
                    if (Greed != 0) { throw new WrongImplimentationException($"Line {n}: greed modifiers are not valid for commands."); }
                    break;
                default:
                    throw new Exception();
            }

            if (Type == CommandTreeNodeType.Switch)
            {   
                switches = new List<string>(match.Groups["val"].Value.Split('+'));
            }
            else
            {
                switches = new List<string>() { match.Groups["val"].Value };
            }            
        }

        //PUBLIC METHODS
        public ParsedCommand Parse(IEnumerable<CommandArgument> args, ObjectTreeNode<CommandTreeNode> node)
        {
            // Materialize once; makes control-flow sane and avoids re-enumeration surprises.
            var allArgs = (args ?? Enumerable.Empty<CommandArgument>()).ToList();

            // 1) Split into: this command’s args/nodes + optional nested command parse.
            var (myArgs, myNodes, nested) = SplitNestedCommand(allArgs, node);

            var retArgs = new Dictionary<string, IEnumerable<string>>();
            var retSwitches = new List<ParsedSwitch>();

            // 2) Parse leading positional arguments (those before the first switch node in the grammar).
            int index = 0;

            var leadingNodes = myNodes.TakeWhile(n => n.Value.IsArgument).ToList();
            if (leadingNodes.Count > 0 || (myNodes.Count > 0 && !myNodes[0].Value.IsSwitch))
            {
                index += ParsePositionalArguments(
                    myArgs,
                    index,
                    leadingNodes,
                    retArgs,
                    onTooFew: (missing, lastToken) =>
                        lastToken is object
                            ? $"Expected more arguments after '{lastToken}', at least {missing} more, see help."
                            : $"Expected arguments after '{node.Value.Command}', at least {missing}, see help.",
                    onUnexpectedLeading: () =>
                        $"Unexpected leading arguments: {string.Join(' ', myArgs.TakeWhile(a => a.IsArgument))}"
                );
            }

            // 3) Parse switches (and their argument payloads) until we hit a non-switch token.
            var switchNodes = myNodes.Where(n => n.Value.IsSwitch).ToList();
            while (index < myArgs.Count && myArgs[index].IsSwitch)
            {
                index = ParseSingleSwitch(myArgs, index, switchNodes, retSwitches);
            }

            // 4) If there are remaining tokens, they must be trailing positional args (end-of-line arguments).
            if (index < myArgs.Count)
            {
                var trailingNodes = myNodes
                    .AsEnumerable()
                    .Reverse()
                    .TakeWhile(n => n.Value.IsArgument)
                    .Reverse()
                    .ToList();

                if (trailingNodes.Count == 0)
                    throw new CommandParsingException("Hit unexpected argument: " + myArgs[index]);

                index += ParsePositionalArguments(
                    myArgs,
                    index,
                    trailingNodes,
                    retArgs,
                    onTooFew: (missing, _) =>
                        myArgs.Count > 0
                            ? $"Expected more arguments after '{node.Value.Command}', at least {missing} more, see help."
                            : $"Expected arguments at end of command line, at least {missing}, see help.",
                    onUnexpectedLeading: null
                );
            }

            // 5) If anything is still unconsumed, it’s unexpected.
            if (index < myArgs.Count)
                throw new CommandParsingException("Unexpected argument(s): " + string.Join(' ', myArgs.Skip(index)));

            var parsedCommand = new ParsedCommand(node.Value.Command, retArgs, retSwitches, nested);

            // Preserve your final min-count check (counts args + switches + nested-command-as-1).
            var totalCount = retArgs.Count + (nested is null ? 0 : 1) + retSwitches.Count;
            if (node.Value.Length.Min > totalCount)
                throw new CommandParsingException("Not enough commands or arguments following " + node.Value.Command);

            return parsedCommand;
        }

        private static (List<CommandArgument> myArgs, List<ObjectTreeNode<CommandTreeNode>> myNodes, ParsedCommand? nested)
            SplitNestedCommand(List<CommandArgument> args, ObjectTreeNode<CommandTreeNode> node)
        {
            ParsedCommand? nested = null;

            // Find first arg that matches any command child.
            int nestedIndex = -1;
            string? nestedToken = null;

            for (int i = 0; i < args.Count; i++)
            {
                var a = args[i];
                if (!a.IsArgument) continue;

                var cmdChild = node.FirstOrDefault(n => n.Value.IsCommand && n.Value.Command.Equals(a.Argument));
                if (cmdChild is object)
                {
                    nestedIndex = i;
                    nestedToken = a.Argument;
                    nested = cmdChild.Value.Parse(args.Skip(i + 1), cmdChild);
                    break;
                }
            }

            if (nestedIndex >= 0)
            {
                // Args before nested command token belong to this node.
                var myArgs = args.Take(nestedIndex).ToList();

                // Only nodes before that nested command node apply here.
                var myNodes = node.Children
                    .TakeWhile(n => !(n.Value.IsCommand && n.Value.Command.Equals(nestedToken)))
                    .ToList();

                // Matches your original: if there are effectively no applicable nodes, return just nested.
                if (myNodes.Count == 0 && myArgs.Count == 0)
                    return (myArgs, myNodes, nested);

                return (myArgs, myNodes, nested);
            }

            return (args, node.Children.ToList(), null);
        }

        private int ParsePositionalArguments(
            List<CommandArgument> args,
            int startIndex,
            List<ObjectTreeNode<CommandTreeNode>> nodes,
            Dictionary<string, IEnumerable<string>> outArgs,
            Func<int, CommandArgument?, string> onTooFew,
            Func<string>? onUnexpectedLeading
        )
        {
            // Count contiguous argument-tokens from startIndex.
            int argSpan = 0;
            while (startIndex + argSpan < args.Count && args[startIndex + argSpan].IsArgument)
                argSpan++;

            int minCount = TotalMinCount(nodes);
            if (minCount > argSpan)
            {
                CommandArgument? last = argSpan > 0 ? args[startIndex + argSpan - 1] : (CommandArgument?)null;
                throw new CommandParsingException(onTooFew(minCount - argSpan, last));
            }

            if (argSpan == 0)
            {
                // Still need to enforce required nodes.
                foreach (var n in nodes)
                {
                    if (n.Value.Length.Min > 0)
                        throw new CommandParsingException($"The required argument '{n.Value.Argument}' was not provided, see help.");
                }
                return 0;
            }

            if (nodes.Count == 0)
            {
                if (onUnexpectedLeading is object)
                    throw new CommandParsingException(onUnexpectedLeading());
                return 0;
            }

            var ranges = DistributeArgs(argSpan, nodes);
            int consumedByRanges = ranges.Sum(r => r.Length);

            if (consumedByRanges < argSpan)
            {
                var extra = args.Skip(startIndex + consumedByRanges).Take(argSpan - consumedByRanges);
                throw new CommandParsingException("Unexpected argument(s): " + string.Join(' ', extra));
            }

            int idx = startIndex;

            for (int r = 0; r < ranges.Count; r++)
            {
                var n = nodes[r];
                var take = ranges[r].Length;

                var list = new List<string>(take);
                for (int i = 0; i < take; i++)
                    list.Add(args[idx++].Argument);

                if (n.Value.Length.Min > list.Count)
                    throw new CommandParsingException($"Not enough parameters given for the argument '{n.Value.Argument}', see help.");

                if (list.Count > 0)
                    outArgs[n.Value.Argument] = list;
            }

            // Validate remaining required nodes beyond allocated ranges.
            for (int r = ranges.Count; r < nodes.Count; r++)
            {
                if (nodes[r].Value.Length.Min > 0)
                    throw new CommandParsingException($"The required argument '{nodes[r].Value.Argument}' was not provided, see help.");
            }

            return idx - startIndex;
        }

        private int ParseSingleSwitch(
            List<CommandArgument> args,
            int switchIndex,
            List<ObjectTreeNode<CommandTreeNode>> switchNodes,
            List<ParsedSwitch> outSwitches
        )
        {
            var swToken = args[switchIndex];
            var swNode = switchNodes.FirstOrDefault(n => n.Value.Switches.Contains(swToken.Switch));

            if (swNode is null)
                throw new CommandParsingException($"Unknown switch: '{swToken}'");

            int index = switchIndex + 1;

            int maxCount = TotalMaxCount(swNode.Children);
            int minCount = TotalMinCount(swNode.Children);

            // Switch args are: the following arguments until next switch token.
            int span = 0;
            while (index + span < args.Count && args[index + span].IsArgument)
                span++;

            // If no more switches exist later and switch has finite max, cap it (matches your original intent).
            bool anyLaterSwitch = args.Skip(index).Any(a => a.IsSwitch);
            if (maxCount < int.MaxValue && !anyLaterSwitch)
                span = Math.Min(span, maxCount);

            if (minCount > span)
                throw new CommandParsingException($"Expected at least {minCount} arguments after switch '{swToken}'.");

            if (span == 0)
            {
                outSwitches.Add(new ParsedSwitch(swNode.Value.switches[0]));
                return index;
            }

            if (!swNode.HasChildren)
                throw new CommandParsingException(
                    $"Unexpected argument(s) following switch '{swToken.Switch}': {string.Join(' ', args.Skip(index).Take(span))}"
                );

            var ranges = DistributeArgs(span, swNode.Children);
            int consumed = ranges.Sum(r => r.Length);

            if (consumed < span)
            {
                var extra = args.Skip(index + consumed).Take(span - consumed);
                throw new CommandParsingException(
                    $"Unexpected argument(s) following switch '{swToken.Switch}': {string.Join(' ', extra)}"
                );
            }

            var tmp = new Dictionary<string, IEnumerable<string>>();

            int idx = index;
            for (int r = 0; r < ranges.Count; r++)
            {
                var child = swNode.Children.ElementAt(r);
                if (!child.Value.IsArgument)
                    throw new WrongImplimentationException($"Only arguments may be the children of switches: '{swToken.Switch}'.");

                var take = ranges[r].Length;
                var list = new List<string>(take);

                for (int i = 0; i < take; i++)
                    list.Add(args[idx++].Argument);

                if (child.Value.Length.Min > list.Count)
                    throw new CommandParsingException($"There are not enough arguments given follow the switch '{swToken.Switch}', see help.");

                if (list.Count > 0)
                    tmp[child.Value.switches[0]] = list;
            }

            // Validate remaining required child-args.
            foreach (var child in swNode.Children.Skip(ranges.Count))
            {
                if (!child.Value.IsArgument)
                    throw new WrongImplimentationException($"Only arguments may be the children of switches: '{swToken.Switch}'.");
                if (child.Value.Length.Min > 0)
                    throw new CommandParsingException($"There are not enough arguments given follow the switch '{swToken.Switch}', see help.");
            }

            outSwitches.Add(new ParsedSwitch(swNode.Value.switches[0], tmp));
            return idx;
        }

        internal static List<ParsedRange> DistributeArgs(int count, IEnumerable<ObjectTreeNode<CommandTreeNode>> nodes)
        {
            int a, b, c;
            bool changed;
            List<ParsedRange> ranges = new List<ParsedRange>();

            if (!nodes.Any()) { return ranges; }

            b = 0;

            foreach (var node in nodes)
            {
                ranges.Add(new ParsedRange(b, c = node.Value.Length.Min, node.Value.Length.Max, node.Value.Greed));

                b += c;
            }

            do
            {
                changed = false;

                for (a = 0; a < ranges.Count - 1; a++)
                {
                    if (!ranges[a].AtMax)
                    {
                        if (ranges[a].End < ranges[a + 1].Start)
                        {
                            ranges[a].Streach(1, count);
                            changed = true;
                        }
                        else if (ranges[a].End == ranges[a + 1].Start && ranges[a].Greed >= ranges[a + 1].Greed)
                        {
                            for (b = ranges.Count - 1; b > a; b--)
                            {
                                if (!ranges[b].Shove(1, count)) { break; }
                                else { changed = true; }
                            }

                            if (changed) { ranges[a].Streach(1, count); }
                        }
                        else if (ranges[a].Length > 0 && ranges[a].End == ranges[a + 1].Start && ranges[a].Greed == -1 && ranges[a + 1].Greed > -1)
                        {
                            if (ranges[a].Streach(-1, count))
                            {
                                changed = true;
                                ranges[a + 1].Shove(-1, count);
                                ranges[a + 1].Streach(1, count);
                            }
                        }
                        else
                        {
                            if (ranges[a].Streach(1, count)) { changed = true; }
                        }
                    }
                }

                if (!ranges[^1].AtMax)
                {
                    if (ranges[^1].Streach(1, count)) { changed = true; }
                }

            } while (changed);

            return ranges;
        }
        public override string ToString()
        {
            var sb = new StringBuilder();

            switch (CountType)
            {
                case CommandTreeCountType.ZeroOrOne:
                    sb.Append('?');
                    break;
                case CommandTreeCountType.ZeroOrMore:
                    sb.Append('*');
                    break;
                case CommandTreeCountType.OneOrMore:
                    sb.Append('+');
                    break;
                case CommandTreeCountType.FixedRange:
                    if (Length.Min == Length.Max) { sb.Append(Length.Min); }
                    else { sb.Append(string.Join('-', Length.Min, Length.Max)); }
                    break;
                default:
                    throw new Exception();
            }

            switch (Greed)
            {
                case -1:
                    sb.Append('?');
                    break;
                case 0:
                    break;
                default:
                    sb.Append(new string('!', Greed));
                    break;
            }

            switch (Type)
            {
                case CommandTreeNodeType.Switch:
                    sb.Append('%');
                    break;
                case CommandTreeNodeType.Argument:
                    sb.Append('$');
                    break;
                case CommandTreeNodeType.Command:
                    sb.Append('^');
                    break;
                default:
                    throw new Exception();
            }

            sb.Append(string.Join('+', switches));

            return sb.ToString();
        }

        //STATIC METHODS        
        private static int TotalMinCount(IEnumerable<ObjectTreeNode<CommandTreeNode>> ls)
        {
            int ret = 0;

            foreach (var x in ls) { ret += x.Value.Length.Min; }

            return ret;
        }

        private static int TotalParsedLength(IEnumerable<ParsedRange> ls)
        {
            int ret = 0;

            foreach (var x in ls) { ret += x.Length; }

            return ret;
        }

        private static int TotalMaxCount(IEnumerable<ObjectTreeNode<CommandTreeNode>> ls)
        {
            int ret = 0;

            foreach (var x in ls)
            { 
                if (x.Value.Length.HasMax)
                {
                    ret += x.Value.Length.Min; 
                }
                else
                {
                    return int.MaxValue;
                }                
            }

            return ret;
        }
    }
}