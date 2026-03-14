using System;

namespace MandalaLogics.Threading
{
    public interface IThreadProgress
    {
        public event EventHandler? ProgressUpdated;
        
        public string Text { get; }
        public int Value { get; }
        public int MaxValue { get; }
        public double Fraction { get; }
        public double Percent  { get; }
    }
}