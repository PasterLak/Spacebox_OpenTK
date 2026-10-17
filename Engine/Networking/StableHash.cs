namespace Engine.Networking;

public static class StableHash
{
    public static uint Compute(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;

        uint hash = offset;
        for (int i = 0; i < value.Length; i++)
        {
            hash ^= value[i];
            hash *= prime;
        }
        return hash;
    }
}
