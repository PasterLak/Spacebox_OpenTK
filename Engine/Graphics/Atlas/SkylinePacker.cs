namespace Engine.Graphics.Atlas;

internal sealed class SkylinePacker
{
    private struct Segment
    {
        public int X;
        public int Y;
        public int Width;

        public Segment(int x, int y, int width)
        {
            X = x;
            Y = y;
            Width = width;
        }
    }

    private readonly List<Segment> _skyline = new List<Segment>();
    private readonly int _width;
    private readonly int _height;

    public SkylinePacker(int width, int height)
    {
        _width = width;
        _height = height;
        _skyline.Add(new Segment(0, 0, width));
    }

    public bool TryPack(int width, int height, out int x, out int y)
    {
        int bestIndex = -1;
        int bestTop = int.MaxValue;
        int bestWidth = int.MaxValue;
        x = 0;
        y = 0;

        for (int i = 0; i < _skyline.Count; i++)
        {
            if (!TryFit(i, width, height, out int fitY)) continue;

            int top = fitY + height;
            int segmentWidth = _skyline[i].Width;
            if (top < bestTop || (top == bestTop && segmentWidth < bestWidth))
            {
                bestIndex = i;
                bestTop = top;
                bestWidth = segmentWidth;
                x = _skyline[i].X;
                y = fitY;
            }
        }

        if (bestIndex < 0) return false;

        Insert(bestIndex, x, y + height, width);
        return true;
    }

    private bool TryFit(int index, int width, int height, out int y)
    {
        y = 0;
        int x = _skyline[index].X;
        if (x + width > _width) return false;

        int remaining = width;
        int i = index;

        while (remaining > 0)
        {
            if (i >= _skyline.Count) return false;

            y = Math.Max(y, _skyline[i].Y);
            if (y + height > _height) return false;

            remaining -= _skyline[i].Width;
            i++;
        }

        return true;
    }

    private void Insert(int index, int x, int y, int width)
    {
        _skyline.Insert(index, new Segment(x, y, width));

        int right = x + width;
        for (int i = index + 1; i < _skyline.Count; i++)
        {
            var segment = _skyline[i];
            if (segment.X >= right) break;

            int overlap = right - segment.X;
            if (overlap >= segment.Width)
            {
                _skyline.RemoveAt(i);
                i--;
                continue;
            }

            _skyline[i] = new Segment(segment.X + overlap, segment.Y, segment.Width - overlap);
            break;
        }

        Merge();
    }

    private void Merge()
    {
        for (int i = 0; i < _skyline.Count - 1; i++)
        {
            if (_skyline[i].Y != _skyline[i + 1].Y) continue;

            _skyline[i] = new Segment(_skyline[i].X, _skyline[i].Y, _skyline[i].Width + _skyline[i + 1].Width);
            _skyline.RemoveAt(i + 1);
            i--;
        }
    }
}
