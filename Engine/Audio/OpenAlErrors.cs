using OpenTK.Audio.OpenAL;

namespace Engine.Audio;

internal static class OpenAlErrors
{
    public static bool Check(string source, string operation)
    {
        ALError error = AL.GetError();
        if (error == ALError.NoError) return true;
        Debug.Error($"[{source}] OpenAL error during {operation}: {AL.GetErrorString(error)}");
        return false;
    }

    public static void Warn(string source, string after)
    {
        ALError error = AL.GetError();
        if (error != ALError.NoError) Debug.Warning($"[{source}] OpenAL error after {after}: {AL.GetErrorString(error)}");
    }

    public static void Clear(string source, string before)
    {
        ALError error = AL.GetError();
        if (error != ALError.NoError) Debug.Warning($"[{source}] Earlier OpenAL error found before {before}: {AL.GetErrorString(error)}");
    }
}
