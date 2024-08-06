﻿using OpenTK.Audio.OpenAL;

namespace Spacebox.Common;
public class AudioManager : IDisposable
{
    public ALDevice Device { get; private set; }
    public ALContext Context { get; private set; }

    public AudioManager()
    {
        Device = ALC.OpenDevice(null);
        if (Device == ALDevice.Null)
        {
            throw new InvalidOperationException("Failed to open the default audio device.");
        }

        Context = ALC.CreateContext(Device, (int[])null);
        if (Context == ALContext.Null)
        {
            ALC.CloseDevice(Device);
            throw new InvalidOperationException("Failed to create an audio context.");
        }

        ALC.MakeContextCurrent(Context);
        CheckALError("making context current");
    }

    public void Dispose()
    {
        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(Context);
        ALC.CloseDevice(Device);
    }

    private void CheckALError(string operation)
    {
        ALError error = AL.GetError();
        if (error != ALError.NoError)
        {
            throw new InvalidOperationException($"OpenAL error during {operation}: {AL.GetErrorString(error)}");
        }
    }
}
