using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MandalaLogics.SurfaceTerminal.Layout;
using MandalaLogics.SurfaceTerminal.Layout.Components;

namespace MandalaLogics.SurfaceTerminal.Parsing
{
    public static class LayoutDeserializer
    {
        private static readonly Regex FirstLineRegex =
            new Regex(@"^\s*layout(?:\s+(?<w>\d+)x(?<h>\d+))?\s*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static SurfaceLayout Read(StreamReader reader)
        {
            return Read(reader, _ => new EmptyPanel());
        }

        public static SurfaceLayout Read(StreamReader reader, Func<string, SurfacePanel> panelResolver)
        {
            if (reader is null) throw new ArgumentNullException(nameof(reader));
            if (panelResolver is null) throw new ArgumentNullException(nameof(panelResolver));

            var firstLine = reader.ReadLine();

            if (firstLine is null)
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid, "Layout file is empty.");

            var firstMatch = FirstLineRegex.Match(firstLine);

            if (!firstMatch.Success)
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    "First line must be 'layout' or 'layout <width>x<height>'.");

            int? width = null;
            int? height = null;

            if (firstMatch.Groups["w"].Success)
            {
                width = int.Parse(firstMatch.Groups["w"].Value);
                height = int.Parse(firstMatch.Groups["h"].Value);
            }

            var parsedLines = ReadContentLines(reader);

            if (parsedLines.Count == 0)
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    "Layout must contain a root node.");

            var layout = new SurfaceLayout(width, height);

            int index = 0;
            ParseNodeInto(layout.RootNode, parsedLines, ref index, parsedLines[0].Indent, panelResolver);

            if (index != parsedLines.Count)
            {
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    $"Unexpected extra content at line {index + 2}.");
            }

            return layout;
        }

        private static List<LayoutDescriptionLine> ReadContentLines(StreamReader reader)
        {
            var lines = new List<LayoutDescriptionLine>();

            while (!reader.EndOfStream)
            {
                var raw = reader.ReadLine();

                if (raw is null)
                    break;

                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                lines.Add(new LayoutDescriptionLine(raw));
            }

            return lines;
        }

        private static void ParseNodeInto(
            SurfaceLayoutNode node,
            IReadOnlyList<LayoutDescriptionLine> lines,
            ref int index,
            int expectedIndent,
            Func<string, SurfacePanel> panelResolver)
        {
            if (index >= lines.Count)
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    "Unexpected end of layout.");

            var line = lines[index];

            if (line.Indent != expectedIndent)
            {
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    $"Unexpected indentation at node index {index}. Expected {expectedIndent}, got {line.Indent}.");
            }

            index++;

            if (line.Type == LayoutDescriptionLineType.Panel)
            {
                node.SetPanel(line.Key, panelResolver(line.Key));
                return;
            }

            ApplySplit(node, line);

            if (index >= lines.Count)
            {
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    "Split node is missing its first child.");
            }

            if (lines[index].Indent <= line.Indent)
            {
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    "Split children must be indented beneath the split line.");
            }

            int childIndent = lines[index].Indent;

            ParseNodeInto(node[1], lines, ref index, childIndent, panelResolver);

            if (index >= lines.Count)
            {
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    "Split node is missing its second child.");
            }

            if (lines[index].Indent != childIndent)
            {
                throw new LayoutException(LayoutExceptionReason.LayoutDescriptionNotValid,
                    "A split node must have exactly two children at the same indentation level.");
            }

            ParseNodeInto(node[2], lines, ref index, childIndent, panelResolver);
        }

        private static void ApplySplit(SurfaceLayoutNode node, LayoutDescriptionLine line)
        {
            if (line.Type != LayoutDescriptionLineType.Split)
                throw new LayoutException(LayoutExceptionReason.ProgrammerException,
                    "ApplySplit called for a non-split line.");

            if (line.SplitType == LayoutLineSplitType.Ratio)
            {
                if (!line.Reverse)
                {
                    node.Split(line.Ratio, line.Direction);
                }
                else
                {
                    node.SplitReverse(line.Ratio, line.Direction);
                }

                return;
            }
            
            if (line.Reverse)
            {
                node.SplitReverse(line.Lines, line.Direction);
            }
            else
            {
                node.Split(line.Lines, line.Direction);
            }
        }
    }
}