using System;
using System.Text.RegularExpressions;
using MandalaLogics.SurfaceTerminal.Layout;

namespace MandalaLogics.SurfaceTerminal.Parsing
{
    public enum LayoutDescriptionLineType
    {
        Panel,
        Split
    }

    public enum LayoutLineSplitType
    {
        Ratio,
        Int
    }

    internal sealed class LayoutDescriptionLine
    {
        private static readonly Regex PanelLineRegex =
            new Regex(@"^(?<indent>\s*)panel\s+(?<key>\S+)\s*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex SplitLineRegex =
            new Regex(@"^(?<indent>\s*)split\s+(?<dir>h|v)\s+(?<amount>-?\d+(?:%)?)\s*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public LayoutDescriptionLineType Type { get; }

        public int Indent { get; }
        public string Key => _key ?? throw ProgrammerError();
        public int Lines => _lines ?? throw ProgrammerError();
        public double Ratio => _ratio ?? throw ProgrammerError();
        public LayoutSplitDirection Direction => _direction ?? throw ProgrammerError();
        public LayoutLineSplitType SplitType => _splitType ?? throw ProgrammerError();
        public bool Reverse { get; }

        private readonly string? _key;
        private readonly int? _lines;
        private readonly double? _ratio;
        private readonly LayoutSplitDirection? _direction;
        private readonly LayoutLineSplitType? _splitType;

        public LayoutDescriptionLine(string line)
        {
            if (line is null) throw new ArgumentNullException(nameof(line));

            var m = PanelLineRegex.Match(line);

            if (m.Success)
            {
                Type = LayoutDescriptionLineType.Panel;
                Indent = m.Groups["indent"].Value.Length;
                _key = m.Groups["key"].Value;
                return;
            }

            m = SplitLineRegex.Match(line);

            if (m.Success)
            {
                Type = LayoutDescriptionLineType.Split;
                Indent = m.Groups["indent"].Value.Length;

                _direction = m.Groups["dir"].Value == "h"
                    ? LayoutSplitDirection.Horizonal
                    : LayoutSplitDirection.Vertical;

                var amountText = m.Groups["amount"].Value;
                Reverse = amountText.StartsWith("-", StringComparison.Ordinal);

                if (amountText.EndsWith("%", StringComparison.Ordinal))
                {
                    _splitType = LayoutLineSplitType.Ratio;

                    var numeric = amountText.TrimEnd('%');

                    if (!double.TryParse(numeric, out var percent))
                    {
                        throw new LayoutException(
                            LayoutExceptionReason.LayoutDescriptionNotValid,
                            $"Line not valid: {line}");
                    }

                    percent = Math.Abs(percent);

                    if (percent <= 0d || percent >= 100d)
                    {
                        throw new LayoutException(
                            LayoutExceptionReason.LayoutDescriptionNotValid,
                            $"Percentage must be between 0 and 100 (exclusive): {line}");
                    }

                    _ratio = percent / 100d;
                }
                else
                {
                    _splitType = LayoutLineSplitType.Int;

                    if (!int.TryParse(amountText, out var lines))
                    {
                        throw new LayoutException(
                            LayoutExceptionReason.LayoutDescriptionNotValid,
                            $"Line not valid: {line}");
                    }

                    if (lines == 0)
                    {
                        throw new LayoutException(
                            LayoutExceptionReason.LayoutDescriptionNotValid,
                            $"Split amount cannot be zero: {line}");
                    }

                    _lines = Math.Abs(lines);
                }

                return;
            }

            throw new LayoutException(
                LayoutExceptionReason.LayoutDescriptionNotValid,
                $"Line not valid: {line}");
        }

        private static Exception ProgrammerError()
        {
            return new LayoutException(
                LayoutExceptionReason.ProgrammerException,
                "LayoutDescriptionLine property accessed for the wrong line type.");
        }
    }
}