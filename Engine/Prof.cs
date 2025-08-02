using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Engine
{
    public static class Prof
    {
#if PROFILING
        public readonly struct Token
        {
            internal readonly int Id;
            internal Token(int id) { Id = id; }
            public bool IsValid => Id >= 0;
        }

        public enum Kind : byte { Timer = 0, Counter = 1, Gauge = 2 }

        public readonly struct Scope : IDisposable
        {
            readonly int _id;
            readonly long _start;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Scope(int id)
            {
                _id = id;
                _start = Stopwatch.GetTimestamp();
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (_id < 0) return;
                long dt = Stopwatch.GetTimestamp() - _start;
                Interlocked.Increment(ref _count[_id]);
                Interlocked.Add(ref _totalTicks[_id], dt);
                long curr;
                while (dt > (curr = Volatile.Read(ref _maxTicks[_id])) &&
                       Interlocked.CompareExchange(ref _maxTicks[_id], dt, curr) != curr) { }
            }
        }

        const int Capacity = 2048;

        static readonly string[] _name = new string[Capacity];
        static readonly Kind[] _kind = new Kind[Capacity];

        static long[] _count = new long[Capacity];
        static long[] _totalTicks = new long[Capacity];
        static long[] _maxTicks = new long[Capacity];

        static long[] _counterVal = new long[Capacity];
        static long[] _gaugeVal = new long[Capacity];

        static int _nextId;
        static readonly double _invFreqMs = 1000.0 / Stopwatch.Frequency;
        static readonly StringBuilder _sb = new StringBuilder(8 * 1024);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int AllocSlot(string name, Kind kind)
        {
            int id = Interlocked.Increment(ref _nextId) - 1;
            if ((uint)id >= Capacity) return -1;
            _name[id] = name;
            _kind[id] = kind;
            return id;
        }

        public static Token RegisterTimer(string name)
        {
            int id = AllocSlot(name, Kind.Timer);
            return new Token(id);
        }

        public static Token RegisterCounter(string name)
        {
            int id = AllocSlot(name, Kind.Counter);
            return new Token(id);
        }

        public static Token RegisterGauge(string name)
        {
            int id = AllocSlot(name, Kind.Gauge);
            return new Token(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scope Time(Token token)
        {
            return new Scope(token.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Count(Token token, long delta = 1)
        {
            if (token.Id < 0) return;
            Interlocked.Add(ref _counterVal[token.Id], delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Gauge(Token token, long value)
        {
            if (token.Id < 0) return;
            Volatile.Write(ref _gaugeVal[token.Id], value);
        }

        public static void ResetFrame()
        {
            int n = Math.Min(_nextId, Capacity);
            for (int i = 0; i < n; i++)
            {
                if (_kind[i] == Kind.Timer)
                {
                    _count[i] = 0;
                    _totalTicks[i] = 0;
                    _maxTicks[i] = 0;
                }
                else if (_kind[i] == Kind.Counter)
                {
                    _counterVal[i] = 0;
                }
            }
        }

        public static bool TryGetTimer(Token token, out long calls, out double msAvg, out double msMax)
        {
            if (!token.IsValid) { calls = 0; msAvg = 0; msMax = 0; return false; }
            long c = Volatile.Read(ref _count[token.Id]);
            long total = Volatile.Read(ref _totalTicks[token.Id]);
            long max = Volatile.Read(ref _maxTicks[token.Id]);
            calls = c;
            msAvg = c > 0 ? (total * _invFreqMs) / c : 0.0;
            msMax = max * _invFreqMs;
            return true;
        }

        public static bool TryGetCounter(Token token, out long value)
        {
            if (!token.IsValid) { value = 0; return false; }
            value = Volatile.Read(ref _counterVal[token.Id]);
            return true;
        }

        public static bool TryGetGauge(Token token, out long value)
        {
            if (!token.IsValid) { value = 0; return false; }
            value = Volatile.Read(ref _gaugeVal[token.Id]);
            return true;
        }

        public static void DumpToConsole(bool sortTimersByAvgDesc = true)
        {
            int n = Math.Min(_nextId, Capacity);
            if (n <= 0) return;

            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;

            Array.Sort(order, (a, b) =>
            {
                byte ka = (byte)_kind[a];
                byte kb = (byte)_kind[b];
                int pa = ka == (byte)Kind.Timer ? 0 : ka == (byte)Kind.Counter ? 1 : 2;
                int pb = kb == (byte)Kind.Timer ? 0 : kb == (byte)Kind.Counter ? 1 : 2;
                int p = pa.CompareTo(pb);
                if (p != 0) return p;
                if (_kind[a] == Kind.Timer && _kind[b] == Kind.Timer && sortTimersByAvgDesc)
                {
                    double avga = _count[a] > 0 ? (_totalTicks[a] * _invFreqMs) / _count[a] : 0.0;
                    double avgb = _count[b] > 0 ? (_totalTicks[b] * _invFreqMs) / _count[b] : 0.0;
                    int c = -avga.CompareTo(avgb);
                    if (c != 0) return c;
                }
                return string.CompareOrdinal(_name[a], _name[b]);
            });

            int nameW = 4, callsW = 5, cntW = 3, gaugeW = 5;
            for (int k = 0; k < n; k++)
            {
                int i = order[k];
                if (_name[i] == null) continue;
                if (_name[i].Length > nameW) nameW = _name[i].Length;
                if (_kind[i] == Kind.Timer)
                {
                    long c = Volatile.Read(ref _count[i]);
                    int w = c.ToString().Length;
                    if (w > callsW) callsW = w;
                }
                else if (_kind[i] == Kind.Counter)
                {
                    long v = Volatile.Read(ref _counterVal[i]);
                    int w = v.ToString().Length;
                    if (w > cntW) cntW = w;
                }
                else
                {
                    long v = Volatile.Read(ref _gaugeVal[i]);
                    int w = v.ToString().Length;
                    if (w > gaugeW) gaugeW = w;
                }
            }

            const int msW = 9;
            _sb.Clear();
            _sb.Append("=== PROF ===").AppendLine();
            _sb.Append('|').Append("Name".PadRight(nameW)).Append(" | ")
               .Append("calls".PadLeft(callsW)).Append(" | ")
               .Append("avg ms".PadLeft(msW)).Append(" | ")
               .Append("max ms".PadLeft(msW)).Append(" | ")
               .Append("cnt".PadLeft(cntW)).Append(" | ")
               .Append("gauge".PadLeft(gaugeW)).Append(" |").AppendLine();

            int lineLen = 3 + nameW + callsW + msW + msW + cntW + gaugeW + 15;
            _sb.Append(new string('-', lineLen)).AppendLine();

            for (int k = 0; k < n; k++)
            {
                int i = order[k];
                if (_name[i] == null) continue;

                if (_kind[i] == Kind.Timer)
                {
                    long c = Volatile.Read(ref _count[i]);
                    if (c == 0) continue;
                    double avg = (_totalTicks[i] * _invFreqMs) / c;
                    double mx = _maxTicks[i] * _invFreqMs;
                    _sb.Append('|').Append(_name[i].PadRight(nameW)).Append(" | ")
                       .Append(c.ToString().PadLeft(callsW)).Append(" | ")
                       .Append(avg.ToString("F3").PadLeft(msW)).Append(" | ")
                       .Append(mx.ToString("F3").PadLeft(msW)).Append(" | ")
                       .Append(new string(' ', cntW)).Append(" | ")
                       .Append(new string(' ', gaugeW)).Append(" |").AppendLine();
                }
                else if (_kind[i] == Kind.Counter)
                {
                    long v = Volatile.Read(ref _counterVal[i]);
                    if (v == 0) continue;
                    _sb.Append('|').Append(_name[i].PadRight(nameW)).Append(" | ")
                       .Append(new string(' ', callsW)).Append(" | ")
                       .Append(new string(' ', msW)).Append(" | ")
                       .Append(new string(' ', msW)).Append(" | ")
                       .Append(v.ToString().PadLeft(cntW)).Append(" | ")
                       .Append(new string(' ', gaugeW)).Append(" |").AppendLine();
                }
                else
                {
                    long v = Volatile.Read(ref _gaugeVal[i]);
                    _sb.Append('|').Append(_name[i].PadRight(nameW)).Append(" | ")
                       .Append(new string(' ', callsW)).Append(" | ")
                       .Append(new string(' ', msW)).Append(" | ")
                       .Append(new string(' ', msW)).Append(" | ")
                       .Append(new string(' ', cntW)).Append(" | ")
                       .Append(v.ToString().PadLeft(gaugeW)).Append(" |").AppendLine();
                }
            }
            Debug.WriteLine(_sb.ToString());
        }

        [Conditional("PROFILING")]
        public static void Assert(bool cond, string message = null)
        {
            if (!cond) Debug.Error(message ?? "assert");
        }
#else
        public readonly struct Token { internal readonly int Id; internal Token(int id) { Id = -1; } public bool IsValid => false; }
        public enum Kind : byte { Timer = 0, Counter = 1, Gauge = 2 }
        public readonly struct Scope : IDisposable { public void Dispose() { } }
        public static Token RegisterTimer(string name) => new Token(-1);
        public static Token RegisterCounter(string name) => new Token(-1);
        public static Token RegisterGauge(string name) => new Token(-1);
        public static Scope Time(Token token) => new Scope();
        public static void Count(Token token, long delta = 1) { }
        public static void Gauge(Token token, long value) { }
        public static void ResetFrame() { }
        public static bool TryGetTimer(Token token, out long calls, out double msAvg, out double msMax) { calls = 0; msAvg = 0; msMax = 0; return false; }
        public static bool TryGetCounter(Token token, out long value) { value = 0; return false; }
        public static bool TryGetGauge(Token token, out long value) { value = 0; return false; }
        public static void DumpToConsole(bool sortTimersByAvgDesc = true) { }
        public static void Assert(bool cond, string message = null) { }
#endif
    }
}
