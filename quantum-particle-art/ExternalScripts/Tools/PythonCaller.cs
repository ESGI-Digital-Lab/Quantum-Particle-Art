using System;
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

public partial class PythonCaller : Node
{
    [Export, Range(1, 64 * 64 * 64)] private int _readBuffer;
    [Header("Run settings"),
     InfoBox("Overridden when init is called externaly, use them for direct call from editor")]
    [Export] private bool _killOnExit = true;

    [InfoBox("Select the local correct interpretor/venv that has correct package installation")] [Export]
    protected string _pythonInterpreter = "python";


    [Export] [InfoBox("Working directory needs to be set correctly in order for the script to call it's dependecies")]
    protected string folder = "External\\Lenia\\Python";

    [Export] protected string _filename = "LeniaND";
    protected Argument[] _args;
    public bool Responding => _process != null && !_process.HasExited;

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

    public override void _Ready()
    {
        base._Ready();
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        CallPython(null, true, null);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (_killOnExit)
            {
                _process?.Kill(true);
                Stop();
            }
        }

        base._Notification(what);
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

    public void CallPython(Action<string> onOutput = null, bool isAssetRooted = true, Action onEnd = null)
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
            CreateNoWindow = false
        };
        _process = new Process() { StartInfo = startInfo };
        _process.Start();
        //Release if used
        //_process.MaxWorkingSet = new IntPtr(8000000000);
        //_process.MinWorkingSet = new IntPtr(2000000000);
        _process.PriorityClass = ProcessPriorityClass.High;
        _cancel = new CancellationTokenSource();
        Func<bool> endCondition = () => _process != null && _process.HasExited;
        _running1 = ReadOutput(_process.StandardError, _cancel, endCondition, Debug.LogWarning);
        _running2 = ReadOutput(_process.StandardOutput, _cancel, endCondition, onOutput, onEnd);
    }

    public async Task ReadOutput(StreamReader stream, CancellationTokenSource _cancel,
        Func<bool> endCondition = null, Action<string> onOutput = null, Action onEnd = null)
    {
        try
        {
            char[] buffer = new char[_readBuffer];
            while (!(_cancel.IsCancellationRequested || (endCondition != null && endCondition.Invoke())))
            {
                int val = await stream.ReadBlockAsync(buffer, 0, buffer.Length);
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