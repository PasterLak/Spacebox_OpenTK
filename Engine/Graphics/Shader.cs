using Engine.Graphics;
using Engine.Utils;
using Engine.Graphics.Rhi;
using OpenTK.Mathematics;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine
{
    public class Shader : IResource
    {
        public int Handle { get; private set; }
        public ProgramHandle Program => new ProgramHandle(Handle);
        public int GetHandle() { return Handle; }
        private Dictionary<string, int> _uniformLocations;
        private const string ShaderFormat = ".glsl";
        private const string VertexShaderKey = "--Vert";
        private const string FragmentShaderKey = "--Frag";
        private const string GeometryShaderKey = "--Geom";

        private FileSystemWatcher _watcher;
        private readonly string _shaderPath;

        private bool _isProcessingChange = false;
        private bool _isReloadingShader = false;

        private int _previousHandle;
        private Dictionary<string, int> _previousUniformLocations;

        private ShaderHotReloader _hotReloader;
        public bool IsDisposed { get; set; }
        public Shader()
        {

        }
        public Shader(string shaderPath)
        {
            _shaderPath = shaderPath;
            ResourceScopes.Track(this);
            if (Application.IsHeadless) return;

            var r = LoadFromPath(_shaderPath);
            Load(ref r.vertexSrc, ref r.fragmentSrc, ref r.geometrySrc);
            if (HotReloadEnabled) _hotReloader = ShaderHotReloader.TryCreate(_shaderPath, OnShaderFileChanged);
        }

        public static bool HotReloadEnabled { get; set; } = true;

        public Shader(string[] code)
        {
            _shaderPath = "";
            ResourceScopes.Track(this);
            if (Application.IsHeadless) return;

            var r = LoadFromCode(code);
            Load(ref r.vertexSrc, ref r.fragmentSrc, ref r.geometrySrc); 
        }

        public string GetResourceInfo()
        {
            return $"Shader: {Path.GetFileName(_shaderPath)} (Handle: {Handle})";
        }

        private void OnShaderFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_isProcessingChange)
                return;
            _isProcessingChange = true;
            _hotReloader?.Disable();

            Task.Run(async () =>
            {
                await Task.Delay(100);
                while (true)
                {
                    try
                    {
                        using (var stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            break;
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        _isProcessingChange = false;
                        _hotReloader?.Enable();
                        return;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        _isProcessingChange = false;
                        return;
                    }
                    catch (IOException) { await Task.Delay(100); }
                }
                EngineWindow._mainThreadActions.Enqueue(() =>
                {
                    ReloadShader();
                    _isProcessingChange = false;
                    _hotReloader?.Enable();
                });
            });
        }

        private static void BindUniformBlocks(ProgramHandle program)
        {
            var device = Gpu.Device;
            int count = device.GetUniformBlockCount(program);

            for (int i = 0; i < count; i++)
            {
                string name = device.GetUniformBlockName(program, i);
                int binding = BindingAllocator.GetOrAssign(name);
                device.SetUniformBlockBinding(program, i, binding);
            }
        }

        private (string vertexSrc, string fragmentSrc, string geometrySrc) LoadFromPath(string path)
        {
            if (!File.Exists(_shaderPath + ShaderFormat))
                throw new ArgumentException("Invalid shader path: " + _shaderPath);

            return ShaderParser.ParseShadersFromPath(_shaderPath);

        }

        private (string vertexSrc, string fragmentSrc, string geometrySrc) LoadFromCode(string[] code)
        {
            return ShaderParser.ParseShadersFromLines(code);

        }

        private void Load(ref string vertexSrc, ref string fragmentSrc, ref string geometrySrc)
        {
            var device = Gpu.Device;
            var program = device.CreateProgram(new ShaderSources(vertexSrc, fragmentSrc, geometrySrc));
            BindUniformBlocks(program);
            int newHandle = program.Id;

            var newUniforms = CacheUniformLocations(program);
            if (Handle != 0)
            {
                device.DeleteProgram(Program);
            }
            _previousHandle = Handle;
            _previousUniformLocations = _uniformLocations;
            Handle = newHandle;
            _uniformLocations = newUniforms;
            GPUDebug.LabelProgram(Handle, _shaderPath);
            Debug.Log($"[Shader][{Handle}] Compiled! - {Path.GetFileName(_shaderPath)}", Color4.BlueViolet);
        }

        public void ReloadShader()
        {
            if (String.IsNullOrEmpty(_shaderPath)) return;

            Debug.Log("[Shader] Reloading shader...");
            _isReloadingShader = true;
            Gpu.Device.InvalidateProgram();
            try {
                var r = LoadFromPath(_shaderPath);
                Load(ref r.vertexSrc, ref r.fragmentSrc,ref r.geometrySrc); Debug.Log("[Shader] Shader reloaded successfully."); }
            catch (Exception ex)
            {
                Debug.Error("[Shader] Shader reload failed: " + ex.Message);
                Debug.Log("[Shader] Using previous working shader.");
                if (Handle != _previousHandle)
                {
                    if (Handle != 0) Gpu.Device.DeleteProgram(Program);
                    Handle = _previousHandle;
                    _uniformLocations = _previousUniformLocations;
                }
            }
            finally { _isReloadingShader = false; }
        }

        private static Dictionary<string, int> CacheUniformLocations(ProgramHandle program)
        {
            var locations = new Dictionary<string, int>();
            Gpu.Device.GetActiveUniforms(program, locations);
            return locations;
        }

        public void Use()
        {
            if (_isReloadingShader || Handle == 0) return;

            Gpu.Device.UseProgram(Program);
        }

        public int GetAttribLocation(string attribName) => Handle == 0 ? -1 : Gpu.Device.GetAttributeLocation(Program, attribName);

        public void SetBool(string name, bool data)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, data ? 1 : 0);
        }
        public void SetInt(string name, int data)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, data);
        }
        public void SetFloat(string name, float data)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, data);
        }
        public void SetMatrix4(string name, Matrix4 data, bool transpose = true)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, in data, transpose);
        }
        public void SetMatrix3(string name, Matrix3 data, bool transpose = true)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, in data, transpose);
        }
        public void SetVector2(string name, Vector2 data)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, data);
        }
        public void SetVector3(string name, Vector3 data)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, data);
        }
        public void SetVector3Array(string name, Vector3[] values)
        {
            if (values == null || values.Length == 0) return;
            string key = name + "[0]";
            if (!TryGetUniformLocation(key, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, (ReadOnlySpan<Vector3>)values);
        }

        private bool TryGetUniformLocation(string name, out int location)
        {
            if (_isReloadingShader || Handle == 0)
            {
                location = -1;
                return false;
            }
            return _uniformLocations.TryGetValue(name, out location);
        }
        public void SetVector4(string name, Vector4 data)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, data);
        }
        public void SetVector4(string name, Color4 data)
        {
            if (!TryGetUniformLocation(name, out int location))
                return;
            Use();
            Gpu.Device.SetUniform(location, data);
        }
        public void Dispose()
        {
            if (IsDisposed || Resources.BlocksDispose(this)) return;

            ResourceScopes.Forget(this);

            if (Handle != 0)
                Gpu.Device.DeleteProgram(Program);
            _watcher?.Dispose();
            _hotReloader?.Dispose();
            _hotReloader = null;

            IsDisposed = true;
        }

        public IResource Load(string path)
        {
            return new Shader(path);
        }

    }
  
    public static class ShaderParser
    {

        public static string RemoveComments(string src)
        {
            // 1) remove /* ... */ 
            string noBlock = Regex.Replace(
                src,
                @"/\*.*?\*/",
                "",
                RegexOptions.Singleline
            );
            // 2) remove // 
            string noLine = Regex.Replace(
                noBlock,
                @"//.*?$",
                "",
                RegexOptions.Multiline
            );
            return noLine;
        }

        public static (string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource)
                ParseShadersFromPath(string filePath)
        {
            var lines = File.ReadAllLines(filePath + ".glsl");
            return ParseShadersFromLines(lines, filePath);
        }


        public static (string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource)
                ParseShadersFromLines(string[] lines)
        {
            return ParseShadersFromLines(lines, "Shaders");
        }

        private static (string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource)
                ParseShadersFromLines(string[] lines, string filePath)
        {

            bool inVert = false, inFrag = false, inGeom = false;
            var v = new StringBuilder();
            var f = new StringBuilder();
            var g = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Contains("--Vert")) { inVert = true; inFrag = inGeom = false; continue; }
                if (line.Contains("--Frag")) { inFrag = true; inVert = inGeom = false; continue; }
                if (line.Contains("--Geom")) { inGeom = true; inVert = inFrag = false; continue; }

                if (inVert) v.AppendLine(line);
                if (inFrag) f.AppendLine(line);
                if (inGeom) g.AppendLine(line);
            }

            string dir = Path.GetDirectoryName(filePath)!;

            string vertSrc = ExpandIncludes(v.ToString(), dir);
            string fragSrc = ExpandIncludes(f.ToString(), dir);
            string geomSrc = ExpandIncludes(g.ToString(), dir);


            vertSrc = RemoveComments(vertSrc);
            fragSrc = RemoveComments(fragSrc);
            geomSrc = RemoveComments(geomSrc);

            if (string.IsNullOrWhiteSpace(vertSrc) || string.IsNullOrWhiteSpace(fragSrc))
                throw new Exception("Both vertex and fragment shader sources must be provided.");

            return (vertSrc, fragSrc, geomSrc);
        }

        static string ExpandIncludes(string src, string dir, HashSet<string>? visited = null)
        {
            visited ??= new HashSet<string>();
            var sb = new StringBuilder();
            int lineNo = 1;
            bool globalsInjected = false;

            foreach (var raw in src.Split('\n'))
            {
                var t = raw.TrimStart();


                if (t.StartsWith("#include"))
                {
                    int q1 = t.IndexOf('"') + 1;
                    int q2 = t.LastIndexOf('"');
                    if (q1 <= 0 || q2 <= q1) continue;

                    string incName = t[q1..q2];
                    string incPath = Path.Combine(dir, incName);

                    if (!visited.Add(incPath)) continue;
                    if (!File.Exists(incPath))
                        throw new FileNotFoundException($"Include not found: {incPath}");

                    string included = File.ReadAllText(incPath);
                    sb.AppendLine(ExpandIncludes(included, Path.GetDirectoryName(incPath)!, visited));
                    sb.AppendLine($"#line {++lineNo}");
                }
                else if (t.StartsWith("#version"))
                {

                    if (!globalsInjected && t.StartsWith("#version"))
                    {
                        sb.AppendLine(raw);
                        sb.AppendLine(GlobalUniforms.GenerateCode());
                        globalsInjected = true;
                    }
                    lineNo++;

                }

                else
                {
                    sb.AppendLine(raw);
                    lineNo++;
                }
            }
            return sb.ToString();
        }

    }


}
