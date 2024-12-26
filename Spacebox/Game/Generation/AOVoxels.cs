using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game.Generation;

public struct FaceData
{
    public Dictionary<Vector3SByte, Vector3SByte[]> neigbordPositions;
}

public class AOVoxels
{

    private static Dictionary<Face, byte> CachedMasks = new Dictionary<Face, byte>();

    private static Dictionary<Face, FaceData> CachedSidesToCheck = new Dictionary<Face, FaceData>();

    public static readonly Vector3SByte[][] FaceNeighborOffsets = new Vector3SByte[6][]
        {
            // Face.Left
            new Vector3SByte[]
            {
                new Vector3SByte( 0, -1, 0), // Bit 0: Bottom
                new Vector3SByte(0,  -1, -1), // Bit 1: Bottom Left
                new Vector3SByte(0,  0, -1), // Bit 2: Left
                new Vector3SByte( 0,  1, -1), // Bit 3: Upper Left
                new Vector3SByte( 0,  1, 0), // Bit 4: Top
                new Vector3SByte( 0,  1, 1), // Bit 5: Upper Right
                new Vector3SByte( 0, 0, 1), // Bit 6: Right
                new Vector3SByte( 0, -1, 1)  // Bit 7: Bottom Right
            },
            // Face.Right
            new Vector3SByte[]
            {
                new Vector3SByte( 0, -1, 0), // Bit 0: Bottom
                new Vector3SByte(0,  -1, 1), // Bit 1: Bottom Left
                new Vector3SByte(0,  0, 1), // Bit 2: Left
                new Vector3SByte( 0,  1, 1), // Bit 3: Upper Left
                new Vector3SByte( 0,  1, 0), // Bit 4: Top
                new Vector3SByte( 0,  1, -1), // Bit 5: Upper Right
                new Vector3SByte( 0, 0, -1), // Bit 6: Right
                new Vector3SByte( 0, -1, -1)  // Bit 7: Bottom Right
            },
            // Face.Bottom
            new Vector3SByte[]
            {
                new Vector3SByte(0,0,-1), // Bit 0: Bottom
                new Vector3SByte(-1,0,-1), // Bit 1: Bottom Left
                new Vector3SByte(-1, 0,0), // Bit 2: Left
                new Vector3SByte(-1,  0,1), // Bit 3: Upper Left
                new Vector3SByte(0,0,1), // Bit 4: Top
                new Vector3SByte(1,0,1), // Bit 5: Upper Right
                new Vector3SByte(1,0,0), // Bit 6: Right
                new Vector3SByte(1,0,-1)  // Bit 7: Bottom Right
            },
            // Face.Top
            new Vector3SByte[]
            {
                new Vector3SByte(0,0,1),   // Bit 0: Bottom
                new Vector3SByte(-1,  0, 1),   // Bit 1: Bottom Left
                new Vector3SByte(-1,  0,0),   // Bit 2: Left
                new Vector3SByte(-1,0,-1),   // Bit 3: Upper Left
                new Vector3SByte(0,0,-1),  // Bit 4: Top
                new Vector3SByte(1,0,-1),  // Bit 5: Upper Right
                new Vector3SByte(1,0,0),  // Bit 6: Right
                new Vector3SByte(1,0,1)    // Bit 7: Bottom Right
            },
            // Face.Back
            new Vector3SByte[]
            {
               new Vector3SByte( 0, -1, 0), // Bit 0: Bottom
                new Vector3SByte(1,  -1, 0), // Bit 1: Bottom Left
                new Vector3SByte(1,  0, 0), // Bit 2: Left
                new Vector3SByte( 1,  1, 0), // Bit 3: Upper Left
                new Vector3SByte( 0,  1, 0), // Bit 4: Top
                new Vector3SByte( -1,  1, 0), // Bit 5: Upper Right
                new Vector3SByte( -1, 0, 0), // Bit 6: Right
                new Vector3SByte( -1, -1, 0)  // Bit 7: Bottom Right
            },
            // Face.Front
            new Vector3SByte[]
            {
                new Vector3SByte(0,-1,0), // Bit 0: Bottom
                new Vector3SByte(-1, -1, 0),  // Bit 1: Bottom Left
                new Vector3SByte(-1,0,0),  // Bit 2: Left
                new Vector3SByte(-1, 1, 0),   // Bit 3: Upper Left
                new Vector3SByte(0,1,0),   // Bit 4: Top
                new Vector3SByte(1, 1, 0),   // Bit 5: Upper Right
                new Vector3SByte(1,0,0),  // Bit 6: Right
                new Vector3SByte(1, -1, 0)   // Bit 7: Bottom Right
            }
        };

    public static readonly float[] GetLightedPoints = { 1f, 1f, 1f, 1f };


    private static bool isPrecalculated = false;

    public static void Init()
    {
        if (isPrecalculated) return;

        CalculateMasks();
        CalculateSideBlockPositions();

        isPrecalculated = true;

        //PrintAO(0b0011_0110);
    }

    public static void PrintDirections(byte mask)
    {
        var directions = NeighborCombinations.GetDirections(mask);
        Debug.WriteLine($"Mask: {Convert.ToString(mask, 2).PadLeft(8, '0')}");
        Debug.WriteLine("Active Directions:");
        foreach (var direction in directions)
        {
            Debug.WriteLine($"- {direction}");
        }
    }

    public static void PrintAO(byte mask)
    {
        float[] ao = AOShading.GetAO(mask);
        PrintDirections(mask);
        Debug.WriteLine($"Mask: {Convert.ToString(mask, 2).PadLeft(8, '0')}");
        Debug.WriteLine($"AO Values:");
        Debug.WriteLine($"- Lower Left: {ao[0]}");
        Debug.WriteLine($"- Lower Right: {ao[1]}");
        Debug.WriteLine($"- Upper Left: {ao[3]}");
        Debug.WriteLine($"- Upper Right: {ao[2]}");
        Debug.WriteLine("-----------------------------------");
    }

    public static Vector3SByte[] GetNeigbordPositions(Face face, Vector3SByte vertex)
    {
        return CachedSidesToCheck[face].neigbordPositions[vertex];
    }

    private static void CalculateMasks()
    {
        foreach (Face face in Enum.GetValues(typeof(Face)))
        {
            CachedMasks.Add(face, CreateMask(CubeMeshData.GetFaceVertices(face)));
        }
    }

    private static void CalculateSideBlockPositions()
    {
        foreach (Face face in Enum.GetValues(typeof(Face)))
        {
            var vertices = CubeMeshData.GetFaceVertices(face);

            FaceData faceData = new FaceData();
            faceData.neigbordPositions = new Dictionary<Vector3SByte, Vector3SByte[]>();

            foreach (var v in vertices)
            {
                var ver = Vector3SByte.CreateFrom(v);

                var sidePos = ApplyMaskToPosition(Vector3SByte.Zero, ver, CachedMasks[face], face.GetNormal());

                foreach (var g in sidePos)
                {
                    if (g == Vector3SByte.Zero)
                    {
                        Debug.Error("zero side found");
                    }
                }
                faceData.neigbordPositions.Add(ver, sidePos);
            }

            CachedSidesToCheck.Add(face, faceData);
        }
    }

    private static byte CreateMask(Vector3[] vertex)
    {
        byte[] verticesAsByte = new byte[4];

        for (sbyte j = 0; j < vertex.Length; j++)
        {
            verticesAsByte[j] = VectorToBitNumber(Vector3SByte.CreateFrom(vertex[j]));
        }

        return CombineBits(verticesAsByte);
    }

    private static Vector3SByte[] ApplyMaskToPosition(Vector3SByte position, Vector3SByte vertex, byte mask, Vector3SByte normal)
    {
        List<Vector3SByte> result = new List<Vector3SByte>();

        for (byte i = 0; i < 3; i++)
        {
            Vector3SByte currentPosition = position;

            sbyte maskValue = (sbyte)(mask >> 2 - i & 1);

            if (maskValue == 1)
            {
                if (i == 0)
                {
                    currentPosition.X = ApplyShift(currentPosition.X, vertex.X);
                }
                if (i == 1)
                {
                    currentPosition.Y = ApplyShift(currentPosition.Y, vertex.Y);
                }
                if (i == 2)
                {
                    currentPosition.Z = ApplyShift(currentPosition.Z, vertex.Z);
                }
            }
            else
            {
                if (!normal.HasNegativeValues())
                {
                    if (normal.X != 0) currentPosition.X = 0;
                    if (normal.Y != 0) currentPosition.Y = 0;
                    if (normal.Z != 0) currentPosition.Z = 0;
                }
                else
                {

                }
                if (currentPosition != Vector3SByte.Zero)
                {
                    result.Add(currentPosition);
                }

            }

            if (!normal.HasNegativeValues())
            {
                if (normal.X != 0) currentPosition.X = 0;
                if (normal.Y != 0) currentPosition.Y = 0;
                if (normal.Z != 0) currentPosition.Z = 0;
            }

            if (currentPosition != Vector3SByte.Zero)
            {
                result.Add(currentPosition);
            }

        }

        return result.ToArray();
    }

    private static sbyte ApplyShift(sbyte componentValue, sbyte shiftValue)
    {
        if (shiftValue == 0)
        {
            return (sbyte)(componentValue - 1);
        }
        else
        {
            return (sbyte)(componentValue + shiftValue);
        }
    }
    private static byte VectorToBitNumber(Vector3SByte vertex)
    {
        byte bitNumber = (byte)(vertex.X << 2 | vertex.Y << 1 | vertex.Z);
        return bitNumber;
    }
    private static byte CombineBits(byte[] numbers)
    {
        byte result = 0;

        for (byte i = 0; i < numbers.Length; i++)
        {
            result |= numbers[i];
        }

        return result;
    }



}



[Flags]
public enum NeighborDirections : byte
{
    None = 0,
    Bottom = 1 << 0,
    BottomLeft = 1 << 1,
    Left = 1 << 2,
    UpperLeft = 1 << 3,
    Top = 1 << 4,
    UpperRight = 1 << 5,
    Right = 1 << 6,
    BottomRight = 1 << 7
}

public static class NeighborCombinations
{
    public struct Combination
    {
        public byte Mask;
        public NeighborDirections Directions;
    }

    public static readonly Combination[] AllCombinations = new Combination[256];
    private static readonly NeighborDirections[] BitToDirection = new NeighborDirections[8]
    {
        NeighborDirections.Bottom,
        NeighborDirections.BottomLeft,
        NeighborDirections.Left,
        NeighborDirections.UpperLeft,
        NeighborDirections.Top,
        NeighborDirections.UpperRight,
        NeighborDirections.Right,
        NeighborDirections.BottomRight
    };

    static NeighborCombinations()
    {
        for (int i = 0; i < 256; i++)
        {
            AllCombinations[i].Mask = (byte)i;
            AllCombinations[i].Directions = (NeighborDirections)i;
        }
    }

    public static List<NeighborDirections> GetDirections(byte mask)
    {
        var directions = new List<NeighborDirections>();
        byte currentMask = mask;
        byte bitPosition = 0;

        while (currentMask != 0)
        {
            if ((currentMask & 1) != 0)
            {
                directions.Add(BitToDirection[bitPosition]);
            }
            currentMask >>= 1;
            bitPosition++;
        }

        return directions;
    }
}


public static class AOShading
{
    private static readonly float[,] AOValues = new float[256, 4];
    private static readonly int[][] cornerBits = new int[4][]
    {
            new int[] { 0, 1, 2 }, // Lower Left
            new int[] { 0, 6, 7 }, // Lower Right
            new int[] { 4, 5, 6 }, // Upper Right
            new int[] { 2, 3, 4 }  // Upper Left
    };

    static AOShading()
    {
        for (int mask = 0; mask < 256; mask++)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                int count = 0;
                foreach (int bit in cornerBits[corner])
                {
                    if ((mask & 1 << bit) != 0)
                        count++;
                }
                AOValues[mask, corner] = 1f - count / 4f;
            }
        }
    }

    public static float[] GetAO(byte mask)
    {
        return new float[]
        {
                AOValues[mask, 0], // Lower Left
                AOValues[mask, 1], // Lower Right
                AOValues[mask, 2], // Upper Right
                AOValues[mask, 3]  // Upper Left
        };
    }
}

