using System.Runtime.CompilerServices;
using System.Text;

namespace Spacebox.Common
{
    public class Array2D<T>
    {
        private readonly T[] _data;

        private readonly int Rows;
        private readonly int Columns;

        public Array2D(int rows, int columns)
        {
            _data = new T[rows * columns];
            Rows = rows;
            Columns = columns;
        }

        //private int index = 0;
        public T this[int row, int column]
        {
            get
            {
                 int index = row * Columns + column;
                return _data[index];
            }
            set
            {
                 int index = row * Columns + column;
                _data[index] = value;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndex( int row, int column) => row * Columns + column;

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < Rows; i++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    sb.Append(_data[GetIndex(i, x)]);
                    sb.Append(", ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

    }
}
