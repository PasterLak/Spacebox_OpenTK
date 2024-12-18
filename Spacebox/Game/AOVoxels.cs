using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game;

public struct FaceData
{
    public Dictionary<Vector3SByte, Vector3SByte[]> neigbordPositions;
}

public class AOVoxels
{

    private static Dictionary<Face, byte> Masks = new Dictionary<Face, byte>();
    
    private static Dictionary<Face, FaceData> SideBlockPositions = new Dictionary<Face, FaceData>();
    
    private static bool isPrecalculated = false;
    
    public static void Init()
    {
        if(isPrecalculated) return;
        
        CalculateMasks();
        CalculateSideBlockPositions();

        isPrecalculated = true;
    }

    public static Vector3SByte[] GetNeigbordPositions(Face face, Vector3SByte vertex)
    {
        return SideBlockPositions[face].neigbordPositions[vertex];
    }

    private static void CalculateMasks()
    {
        foreach (Face face in Enum.GetValues(typeof(Face)))
        {
            Masks.Add(face,CreateMask(CubeMeshData.GetFaceVertices(face)));
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

                var sidePos = ApplyMaskToPosition(Vector3SByte.Zero, ver, Masks[face], face.GetNormal());

                foreach (var g in sidePos)
                {
                    if (g == Vector3SByte.Zero)
                    {
                        Debug.Error("zero side found");
                    }  
                }
                faceData.neigbordPositions.Add(ver, sidePos);
            }
            
            SideBlockPositions.Add(face, faceData);
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

            sbyte maskValue = (sbyte)((mask >> (2 - i)) & 1);

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
        byte bitNumber = (byte)((vertex.X << 2) | (vertex.Y << 1) | vertex.Z);
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