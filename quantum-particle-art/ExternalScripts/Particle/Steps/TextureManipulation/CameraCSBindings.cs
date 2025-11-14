using System.Threading;
using System.Threading.Tasks;
using Godot;
using UnityEngine;
using UnityEngine.Assertions;

public partial class CameraCSBindings : Node
{
    [Export] private PythonCaller _python;
    [Export] private TextureRect _display;
    [Export] private bool _takeInstantOnFirstFrame = false;
    private PacketPeerUdp _peer;
    private Image _texture;
    private Image _cache;


    private const ushort port = 4242;
    private const ushort ackPort = port + 1;
    private const string adress = "127.0.0.1";
    private const int initialPacketSize = 4;


    private byte[] _accumulator;
    private int _head;
    private int _nbChunks;
    private object _lock = new();
    private bool _finished;
    private bool _imageCompleted;

    private CancellationTokenSource _cancel;
    public Image Texture => _texture;


    public void SetDisplaySize(Godot.Vector2 size)
    {
        _display.Size = size;
        _display.Position = -size / 2f;
    }

    public void Start()
    {
        if (_peer != null) //Safe in case we inited twice
            return;
        _cancel = new CancellationTokenSource();
        _display.SetVisible(_peer != null);
        _finished = false;
        Assert.IsTrue(_python.ChunkSize != initialPacketSize,
            "Chunk size cannot be equal to initial packet size, we rely on that to differentiate first packet from others, which is required to interpret correctly the data stream");
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
        ClearPackets();
        _python.CallPython(Debug.Log);
        ReInit();
        try
        {
            Task.Run(async () =>
            {
                while (!_cancel.IsCancellationRequested)
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

    private void ClearPackets()
    {
        Debug.Log("CameraCSBindings: Emptying queue");
        while (_peer.GetAvailablePacketCount() > 0)
            _ = _peer.GetPacket();
    }

    public bool TryRestartFeedStreaming()
    {
        lock (_lock)
        {
            if (!_finished)
                return false;
            //Cause of async, we clear first before reiniting
            ClearPackets();
            ReInit();
            _finished = false;
        }

        return true;
    }

    private void ReInit()
    {
        //Empty the texture without reassigning it, because the ref is used and checked elsewhere
        _texture.CopyFrom(new Image());
        _cache = new Image();
        _imageCompleted = false;
        _head = 0;
        _nbChunks = 0;
        _accumulator = null;
        _display.SetVisible(true);
        Ack();
    }

    public override void _Process(double delta)
    {
        if (!_finished)
        {
            if (_imageCompleted)
            {
                lock (_lock)
                {
                    _imageCompleted = false;
                    if (_cache.IsEmpty())
                    {
                        Debug.LogError(
                            "CameraCSBindings; Completed image is empty, something went wrong during reception");
                        return;
                    }

                    _display.Texture = ImageTexture.CreateFromImage(_cache);
                    _display.SetVisible(true);
                    if (_takeInstantOnFirstFrame) //Is first image
                        TryTakeInstant();
                }
            }
        }
    }

    public bool TryTakeInstant()
    {
        Debug.Log("CameraCSBindings: Trying to take instant");
        if (_finished || !_texture.IsEmpty())
        {
            Debug.LogWarning(
                "Instant not taken, _texture wasn't empty => feed wasn't rearmed to take another instant");
            return false;
        }

        lock (_lock)
        {
            if (_cache.IsEmpty())
            {
                Debug.LogWarning("CameraCSBindings: No connection available (yet ?), cannot take instant");
                return false;
            }

            _texture.CopyFrom(_cache);

            _finished = true;
            _display.SetVisible(false);
        }

        //_python.Kill();//We keep it to we can reuse same python server for image processing even if we don't ack it 
        Debug.Log("CameraCSBindings: finished taking instant, flushed _cache into _texture");

        return true;
    }

    private void ManualUpdate()
    {
        lock (_lock)
        {
            if (_finished)
                return;
            if (_peer == null || !_peer.IsBound())
            {
                Debug.LogError(
                    "Peer not bound, probably an already running process which occupied the port and we didn't manage to connect to it, you can kill all python process using powershell command : ");
                Debug.LogError("Get-Process python | Stop-Process -force");
            }
            else
            {
                while (_peer.GetAvailablePacketCount() > 0)
                {
                    //Debug.Log("Nb packets in queue : " + _peer.GetAvailablePacketCount());
                    //peer.Bind(port, adress);
                    var data = _peer.GetPacket();
                    if (data != null && data.Length > 0)
                    {
                        int i = 0;
                        if (_accumulator == null)
                        {
                            if (data.Length !=
                                initialPacketSize) //First packet should be 4 bytes indicating length of image, we'll discard anything else until we have a valid one with size
                            {
                                //Debug.LogError("CameraCSBindings: First packet of a new image should be 4 bytes indicating length, got " + data.Length + " bytes instead, discarding packet, until we get a valid one that specifies length");
                                Ack();
                                continue;
                            }

                            var length = (data[i++] << 24) | (data[i++] << 16) | (data[i++] << 8) | data[i++];
                            //i=4
                            _accumulator = new byte[length];
                            _head = 0;
                            _nbChunks = 0;
                            //Debug.Log("CameraCSBindings: Starting new image of compressed size :" + length + "inside of a packet of size " + data.Length);

                            Ack();
                            continue; //No more data in this packet
                        }

                        var chunkId = data[i++];
                        _head = chunkId * _python.UsefulSize;
                        for (; i < data.Length && _head < _accumulator.Length; i++, _head++)
                        {
                            _accumulator[_head] = data[i];
                        }

                        //Debug.Log("After packet processed, chunk :" + chunkId + "out of " + _nbChunks + "head at :" +
                        //          _head +
                        //          "for packet size ; " + data.Length + " and remaining :" +
                        //          (_accumulator.Length - _head));
                        _nbChunks++;
                        if (_head >= _accumulator.Length)
                        {
                            if (_cache.IsEmpty())
                                Debug.Log(
                                    "CameraCSBindings: Connection correct, showing live flux in temp display, ready to take instants to feed in the pipeline");
                            byte[] safe = new byte[_accumulator.Length];
                            System.Array.Copy(_accumulator, safe, _accumulator.Length);
                            _accumulator = null;
                            //Debug.Log("Accumulated full image of size :" + safe.Length + " after " + _nbChunks + " chunks, trying to interpret as jpg");
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

                            //Debug.Log("Remaining packets in queue after full image received : " + _peer.GetAvailablePacketCount());
                            break;
                        }

                        Ack();
                    }
                }
            }
        }
    }

    public void Ack()
    {
        if (_peer == null || !_peer.IsBound())
            return;
        //Debug.Log("CameraCSBindings: Acknowledging packet reception");
        byte[] ack = [1];
        _peer.SetDestAddress(adress, ackPort);
        _peer.PutPacket(ack);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _cancel.Cancel();
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
}