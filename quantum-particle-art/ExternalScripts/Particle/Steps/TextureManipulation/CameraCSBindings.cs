using Godot;
using UnityEngine;
using UnityEngine.Assertions;

public partial class CameraCSBindings : Node
{
    [Export] private PythonCaller _python;
    [Export] private TextureRect _display;
    private PacketPeerUdp peer;
    private Image _texture;
    private Image _cache;

    public bool TryTakeInstant()
    {
        if (!_texture.IsEmpty())
        {
            Debug.LogWarning("CameraCSBindings: Texture not empty, overwriting, unexpected behaviour");
            return false;
        }

        if (_cache.IsEmpty())
        {
            Debug.LogWarning("CameraCSBindings: No connection available (yet ?), cannot take instant");
            return false;
        }

        _texture.CopyFrom(_cache);
        _display.SetVisible(false);
        _python.Kill();
        return true;
    }

    private const ushort port = 4242;
    private string adress = "127.0.0.1";

    private byte[] _accumulator;
    private int _head;
    private bool _finished => _texture!= null && !_texture.IsEmpty();

    public override void _Ready()
    {
        _display.SetVisible(false);
    }
    public void Start()
    {
        if (peer != null) //Safe in case we inited twice
            return;
        _python.CallPython();
        peer = new();
        var peered = peer.Bind(port, adress);
        if (peered != Error.Ok)
            Debug.LogError("CameraCSBindings: Failed to bind to port " + port + " on adress " + adress + ", error: " +
                           peered);
        else
            Debug.Log("CameraCSBindings: Bound to port " + port + " on adress " + adress);
        //Assert.IsTrue(peer.IsBound() & peer.IsSocketConnected(), "CameraCSBindings: Failed to connect to socker on port " + port);
        _texture = new Image();
        _cache = new Image();
        _head = 0;
        _accumulator = null;
    }

    public override void _Process(double delta)
    {
        if (_finished)
            return;
        //var poll = _server.Poll();
        //Debug.Log("CameraCSBindings: Polling server, result: " + poll);
        //if (_server.IsConnectionAvailable()) {
        if (peer==null || !peer.IsBound())
        {
            //Debug.LogError("Peer not bound");
        }
        else
        {
            while (peer.GetAvailablePacketCount() > 0)
            {
                //Debug.Log("Nb packets in queue : " + peer.GetAvailablePacketCount());
                //peer.Bind(port, adress);
                var data = peer.GetPacket();
                if (data != null && data.Length > 0)
                {
                    int i = 0;
                    if (_accumulator == null)
                    {
                        var length = (data[i++] << 24) | (data[i++] << 16) | (data[i++] << 8) | data[i++];
                        //i=4
                        _accumulator = new byte[length];
                        _head = 0;
                        //Debug.Log("CameraCSBindings: Starting new image of compressed size :" + length);
                    }

                    for (; i < data.Length && _head < _accumulator.Length; i++, _head++)
                    {
                        _accumulator[_head] = data[i];
                    }

                    if (_head >= _accumulator.Length)
                    {
                        if (_cache.IsEmpty())
                            Debug.Log(
                                "CameraCSBindings: Connection available, showing as debug display, ready to take instant");
                        var err = _cache.LoadJpgFromBuffer(_accumulator);
                        _accumulator = null;
                        if (err != Error.Ok)
                            GD.PrintErr("Failed to load image from buffer: " + err);
                        else
                            _display.Texture = ImageTexture.CreateFromImage(_cache);
                        _display.SetVisible(true);
                        break;
                    }
                }
            }
        }

        if (Input.IsKeyPressed(Key.Space))
        {
            TryTakeInstant();
        }
        //}
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            peer?.Close();
            peer = null;
            _texture?.Dispose();
            _texture = null;
            _cache?.Dispose();
            _cache = null;
            Debug.Log("CameraCSBindings: Closing game, resources disposed");
        }

        base._Notification(what);
    }


    public Image Texture => _texture;
}