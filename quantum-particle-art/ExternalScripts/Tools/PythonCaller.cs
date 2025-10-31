using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using NaughtyAttributes;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Mathf = Godot.Mathf;

public partial class PythonCaller : Node, IDisposable
{
    [ExportGroup("Python params")] [Export]
    private int _cameraID = 0;

    [Export] private int _fps = 30;

    [Export] private int _chunksPerFrame = 15;
    [Export] private int _chunkSize = 65000;
    [Export(PropertyHint.Link)] private Vector2I _resolution = new(1920, 1080);
    public int totalSize => _resolution.X * _resolution.Y * 4 * 2 + 16;
    [Export] private bool _display = false;

    [ExportGroup("Caller params")] [Export, Range(1, 64 * 64 * 64)]
    private int _readBuffer;

    [Export] private bool _killOnExit = true;
    [Export] private bool _showTerminal = false;


    [InfoBox("Select the local correct interpretor/venv that has correct package installation")] [Export]
    protected string _pythonInterpreter = "python";


    [Export] [InfoBox("Working directory needs to be set correctly in order for the script to call it's dependecies")]
    protected string folder = "External\\Lenia\\Python";

    [Export] protected string _filename = "LeniaND";
    protected Argument[] _args;
    public bool Responding => _process != null && !_process.HasExited;

    public int ChunkSize => _chunkSize;

    [Serializable]
    public struct Argument
    {
        private string id;
        private string value;

        public Argument(string id, string value)
        {
            this.id = id.Trim();
            this.value = value;
        }

        override public string ToString()
        {
            return (id.Length > 1 ? "--" : "-") + id + " " + value;
        }

        public static implicit operator Argument((string arg, object value) av)
        {
            return new Argument(av.arg, av.value.ToString());
        }
    }

    [ReadOnly] private Process _process;
    private Task _running1;
    private Task _running2;
    private CancellationTokenSource _cancel = new();

    ~PythonCaller()
    {
        Kill();
    }

    public void CallPython(Action<string> onOutput = null)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        var l = new List<Argument>();
        l.Add(("f", _fps.ToString()));
        l.Add(("r", _resolution.X.ToString() + " " + _resolution.Y.ToString()));
        l.Add(("i", _cameraID.ToString()));
        l.Add(("c", _chunksPerFrame.ToString()));
        l.Add(("s", _chunkSize.ToString()));
        if (_display)
            l.Add(("d", ""));
        _args = l.ToArray();
        CallPython(onOutput, true, null);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (_killOnExit)
            {
                Kill();
                Stop();
            }
        }

        base._Notification(what);
    }

    public void Kill()
    {
        _process?.Kill(true);
        _process = null;
    }

    //[Button]
    public void Stop()
    {
        _cancel.Cancel();
    }

    [Button]
    private void CallPythonEditor()
    {
        CallPython(Debug.Log);
    }

    public void CallPython(Action<string> onOutput, bool isAssetRooted, Action onEnd)
    {
        _args ??= [];
        //Stop();
        string arguments = _filename + ".py" + " " + string.Join(' ', _args);
        string workingDirectory;
        if (isAssetRooted)
        {
            var res = ProjectSettings.GlobalizePath("res://").Trim('/');
            var splitted = res.Split('/');
            var root = string.Join('\\', splitted, 0, splitted.Length - 1);
            workingDirectory = root + '\\' + splitted[^1] + '\\' + folder;
            _pythonInterpreter = root + "\\" + _pythonInterpreter;
        }
        else
            workingDirectory = folder;

        //Debug.Log($" Executing python script {arguments} in {workingDirectory}");
        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonInterpreter,
            WorkingDirectory = workingDirectory,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = !_showTerminal
        };
        _process = new Process() { StartInfo = startInfo };
        _cancel = new CancellationTokenSource();
        Func<bool> endCondition = () => _process != null && _process.HasExited;
        _process.Start();
        _process.PriorityClass = ProcessPriorityClass.High;
        _running1 = ReadOutput(_process.StandardError, _cancel, endCondition,
            s => Debug.LogWarning("From stderror: " + s), onEnd);
        _running2 = ReadOutput(_process.StandardOutput, _cancel, endCondition, onOutput, onEnd);
    }

    public async Task ReadOutput(StreamReader stream, CancellationTokenSource _cancel,
        Func<bool> endCondition = null, Action<string> onOutput = null, Action onEnd = null)
    {
        try
        {
            _readBuffer = Mathf.Max(_readBuffer, 1);
            char[] buffer = new char[_readBuffer];
            while (!(_cancel.IsCancellationRequested || (endCondition != null && endCondition.Invoke())))
            {
                int val = await stream.ReadAsync(buffer, 0, buffer.Length);
                //Debug.Log($"One {_readBuffer / 1024} KBytes Block read from reader");
                if (val > 0)
                {
                    onOutput?.Invoke(new string(buffer, 0, val));
                }

                //await Task.Delay(1);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            //throw e;
        }

        var end = stream.ReadToEnd();
        if (!string.IsNullOrEmpty(end))
            onOutput?.Invoke(end);
        onEnd?.Invoke();
        stream.Dispose();
    }
}