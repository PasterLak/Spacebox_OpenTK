
using OpenTK.Mathematics;


public static class ImageProcessing
{

    public static Color4[,] MirrorY(Color4[,] original)
    {
        if (original == null)
            throw new ArgumentNullException(nameof(original));

        int height = original.GetLength(0); 
        int width = original.GetLength(1); 

        Color4[,] mirrored = new Color4[height, width];

        for (int y = 0; y < height; y++)
        {
            int mirroredY = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                mirrored[mirroredY, x] = original[y, x];
            }
        }

        return mirrored;
    }

   
    public static void MirrorYInPlace(Color4[,] array)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        int height = array.GetLength(0); 
        int width = array.GetLength(1);  

        for (int y = 0; y < height / 2; y++)
        {
            int mirroredY = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                // Меняем местами текущую строку с зеркальной
                Color4 temp = array[y, x];
                array[y, x] = array[mirroredY, x];
                array[mirroredY, x] = temp;
            }
        }
    }


    public static Color4[,] MirrorX(Color4[,] original)
    {
        if (original == null)
            throw new ArgumentNullException(nameof(original));

        int height = original.GetLength(0); 
        int width = original.GetLength(1);  

        Color4[,] mirrored = new Color4[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int mirroredX = width - 1 - x;
                mirrored[y, mirroredX] = original[y, x];
            }
        }

        return mirrored;
    }

    public static void MirrorXInPlace(Color4[,] array)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        int height = array.GetLength(0); 
        int width = array.GetLength(1); 

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width / 2; x++)
            {
                int mirroredX = width - 1 - x;
             
                Color4 temp = array[y, x];
                array[y, x] = array[y, mirroredX];
                array[y, mirroredX] = temp;
            }
        }
    }
}
