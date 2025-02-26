
using OpenTK.Mathematics;

namespace Engine.Utils
{
    public static class RandomHelper
    {
      
        public static Random CreateRandomFromSeed(int worldSeed, Vector3i position)
        {
            int hash = worldSeed;
            hash = HashCombine(hash, position.X);
            hash = HashCombine(hash, position.Y);
            hash = HashCombine(hash, position.Z);
            return new Random(hash);
        }

     
        public static Random CreateRandomFromSeed(int worldSeed, Vector3 position)
        {
            int hash = worldSeed;
            hash = HashCombine(hash, position.X.GetHashCode());
            hash = HashCombine(hash, position.Y.GetHashCode());
            hash = HashCombine(hash, position.Z.GetHashCode());
            return new Random(hash);
        }

        private static int HashCombine(int hash1, int hash2)
        {
            unchecked
            {
                return hash1 * 31 + hash2;
            }
        }
    }
}
