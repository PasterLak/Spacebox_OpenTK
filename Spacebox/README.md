# Spacebox_OpenTK
 
 Engine started!
Scene Loaded: LogoScene
[Shader] Compiled! ID:3 Name: sprite
Shader file changed: Shaders\sprite.glsl - ChangeType: Changed
Unhandled exception. System.Exception: Error occurred whilst compiling Shader(0).


   at Spacebox.Common.Shader.CompileShader(Int32 shader) in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 207
   at Spacebox.Common.Shader.Load() in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 83
   at Spacebox.Common.Shader.ReloadShader() in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 148
   at Spacebox.Common.Shader.OnShaderFileChanged(Object sender, FileSystemEventArgs e) in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 55
   at System.IO.FileSystemWatcher.NotifyFileSystemEventArgs(WatcherChangeTypes changeType, ReadOnlySpan`1 name)
   at System.IO.FileSystemWatcher.ParseEventBufferAndNotifyForEach(ReadOnlySpan`1 buffer)
   at System.IO.FileSystemWatcher.ReadDirectoryChangesCallback(UInt32 errorCode, UInt32 numBytes, AsyncReadState state)
   at System.IO.FileSystemWatcher.<>c.<StartRaisingEvents>b__85_0(UInt32 errorCode, UInt32 numBytes, NativeOverlapped* overlappedPointer)
   at System.Threading.PortableThreadPool.IOCompletionPoller.Callback.Invoke(Event e)
   at System.Threading.ThreadPoolTypedWorkItemQueue`2.System.Threading.IThreadPoolWorkItem.Execute()
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()



Engine started!
Scene Loaded: LogoScene
[Shader] Compiled! ID:3 Name: sprite
Shader file changed: Shaders\sprite.glsl - ChangeType: Changed
vertexShader is 0!
Can not compile shader! Shader ID = 0
vertexShader is 0!
Can not compile shader! Shader ID = 0
Unhandled exception. System.Exception: Error occurred whilst linking Program(0)
   at Spacebox.Common.Shader.LinkProgram(Int32 program) in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 251
   at Spacebox.Common.Shader.Load() in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 123
   at Spacebox.Common.Shader.ReloadShader() in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 171
   at Spacebox.Common.Shader.OnShaderFileChanged(Object sender, FileSystemEventArgs e) in D:\C#\Spacebox_OpenTK\Common\Shader.cs:line 72
   at System.IO.FileSystemWatcher.NotifyFileSystemEventArgs(WatcherChangeTypes changeType, ReadOnlySpan`1 name)
   at System.IO.FileSystemWatcher.ParseEventBufferAndNotifyForEach(ReadOnlySpan`1 buffer)
   at System.IO.FileSystemWatcher.ReadDirectoryChangesCallback(UInt32 errorCode, UInt32 numBytes, AsyncReadState state)
   at System.IO.FileSystemWatcher.<>c.<StartRaisingEvents>b__85_0(UInt32 errorCode, UInt32 numBytes, NativeOverlapped* overlappedPointer)
   at System.Threading.PortableThreadPool.IOCompletionPoller.Callback.Invoke(Event e)
   at System.Threading.ThreadPoolTypedWorkItemQueue`2.System.Threading.IThreadPoolWorkItem.Execute()
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()



