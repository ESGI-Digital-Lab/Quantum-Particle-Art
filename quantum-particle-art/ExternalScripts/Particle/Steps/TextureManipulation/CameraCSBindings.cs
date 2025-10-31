using System.Threading.Tasks;
using Godot;
using UnityEngine;
using UnityEngine.Assertions;

public partial class CameraCSBindings : Node
{
    [Export] private PythonCaller _python;
    [Export] private TextureRect _display;
    private PacketPeerUdp _peer;
    private Image _texture;
    private Image _cache;

    public bool TryTakeInstant()
    {
        Debug.Log("CameraCSBindings: Trying to take instant");
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
    private const ushort ackPort = port + 1;
    private string adress = "127.0.0.1";

    private byte[] _accumulator;
    private int _head;
    private int _nbChunks;
    private bool _finished => _texture != null && !_texture.IsEmpty();

    public void Ack()
    {
        if (_finished || _peer == null || !_peer.IsBound())
            return;
        Debug.Log("CameraCSBindings: Acknowledging packet reception");
        byte[] ack = [1];
        _peer.SetDestAddress(adress, ackPort);
        _peer.PutPacket(ack);
    }

    public override void _Ready()
    {
        _display.SetVisible(_peer != null);
    }

    public void Start()
    {
        if (_peer != null) //Safe in case we inited twice
            return;
        _peer = new();
        int buffSize = _python.totalSize;
        Debug.Log("CameraCSBindings: Binding to port " + port + " on adress " + adress +
                  " with buffer size " + buffSize);
        var peered = _peer.Bind(port, adress, buffSize);
        if (peered == Error.Unavailable)
            Debug.LogError("CameraCSBindings: Failed to bind to port " + port + " on adress " + adress +
                           " it's unavailable, it can happen if a previous run had issues and didn't terminate correctly ,try command \"netstat -ano | findstr 4242\" to find the PID that is already bound to this port (tou can then use \"tasklist /FI \"PID eq 1234\"\" to find more informations about the process), error: " +
                           peered);
        else if (peered != Error.Ok)
            Debug.LogError("CameraCSBindings: Failed to bind to port " + port + " on adress " + adress + ", error: " +
                           peered + _peer);
        else
            Debug.Log("CameraCSBindings: Bound to port " + port + " on adress " + adress);
        //Assert.IsTrue(peer.IsBound() & peer.IsSocketConnected(), "CameraCSBindings: Failed to connect to socker on port " + port);
        _texture = new Image();
        _cache = new Image();
        _head = 0;
        _nbChunks = 0;
        _accumulator = null;
        _display.SetVisible(true);
        Debug.Log("CameraCSBindings: Emptying queue");
        while (_peer.GetAvailablePacketCount() > 0)
            _ = _peer.GetPacket();
        _python.CallPython(Debug.Log);
        Ack();
        try
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    ManualUpdate();
                    await Task.Delay(1);
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError("CameraCSBindings: Exception during manual update task " + e);
        }
    }

    private bool _imageCompleted = false;

    public override void _Process(double delta)
    {
        if (_imageCompleted)
        {
            _imageCompleted = false;
            _display.Texture = ImageTexture.CreateFromImage(_cache);
            _display.SetVisible(true);
        }
    }

    private void ManualUpdate()
    {
        if (_finished)
            return;
        if (_peer == null || !_peer.IsBound())
        {
            Debug.LogError("Peer not bound");
        }
        else
        {
            while (_peer.GetAvailablePacketCount() > 0)
            {
                Debug.Log("Nb packets in queue : " + _peer.GetAvailablePacketCount());
                //peer.Bind(port, adress);
                var data = _peer.GetPacket();
                if (data != null && data.Length > 0)
                {
                    int i = 0;
                    if (_accumulator == null)
                    {
                        var length = (data[i++] << 24) | (data[i++] << 16) | (data[i++] << 8) | data[i++];
                        //i=4
                        _accumulator = new byte[length];
                        _head = 0;
                        _nbChunks = 0;
                        Debug.Log("CameraCSBindings: Starting new image of compressed size :" + length +
                                  "inside of a packet of size " + data.Length);
                        if (i == data.Length)
                        {
                            Ack();
                            continue; //No more data in this packet
                        }
                    }

                    var chunkId = data[i++];
                    _head = chunkId * _python.UsefulSize;
                    for (; i < data.Length && _head < _accumulator.Length; i++, _head++)
                    {
                        _accumulator[_head] = data[i];
                    }

                    Debug.Log("After packet processed, chunk :" + chunkId + "out of " + _nbChunks + "head at :" +
                              _head +
                              "for packet size ; " + data.Length + " and remaining :" +
                              (_accumulator.Length - _head));
                    _nbChunks++;
                    if (_head >= _accumulator.Length)
                    {
                        if (_cache.IsEmpty())
                            Debug.Log(
                                "CameraCSBindings: Connection available, showing as debug display, ready to take instant");
                        byte[] safe = new byte[_accumulator.Length];
                        System.Array.Copy(_accumulator, safe, _accumulator.Length);
                        _accumulator = null;
                        Debug.Log("Accumulated full image of size :" + safe.Length + " after " + _nbChunks +
                                  " chunks, trying to interpret as jpg");
                        try
                        {
                            var err = _cache.LoadJpgFromBuffer(safe);
                            if (err != Error.Ok)
                                GD.PrintErr("Failed to load image from buffer: " + err);
                            else
                                _imageCompleted = true;
                        }
                        catch (System.Exception e)
                        {
                            GD.PrintErr("Failed to load image from buffer: ");
                        }
                        Debug.Log("Remaining packets in queue after full image received : " +
                                  _peer.GetAvailablePacketCount());
                        break;
                    }
                    Ack();
                }
            }
        }

        if (Input.IsKeyPressed(Key.Space))
        {
            Debug.Log("CameraCSBindings: Space pressed, trying to take instant");
            TryTakeInstant();
        }
        //}
    }

    private void UpdateTexture(Error err)
    {
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _peer?.Close();
            _peer = null;
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