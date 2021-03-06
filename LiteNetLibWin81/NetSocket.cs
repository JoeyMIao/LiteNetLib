#if WINRT && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace LiteNetLib
{
    internal sealed class NetSocket
    {
        private DatagramSocket _datagramSocket;
        private readonly Dictionary<NetEndPoint, DataWriter> _peers = new Dictionary<NetEndPoint, DataWriter>();
        private readonly Queue<IncomingData> _incomingData = new Queue<IncomingData>();
        private readonly AutoResetEvent _receiveWaiter = new AutoResetEvent(false);

        private struct IncomingData
        {
            public NetEndPoint EndPoint;
            public byte[] Data;
        }

        public int ReceiveTimeout = 10;

        //Socket constructor
        public NetSocket(ConnectionAddressType connectionAddressType)
        {

        }
        
        private void OnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var dataReader = args.GetDataReader();
            uint count = dataReader.UnconsumedBufferLength;
            if (count > 0)
            {
                byte[] data = new byte[count];
                dataReader.ReadBytes(data);
                _incomingData.Enqueue(
                    new IncomingData
                    {
                        EndPoint = new NetEndPoint(args.RemoteAddress, args.RemotePort),
                        Data = data
                    });
                _receiveWaiter.Set();
            }
        }

        //Bind socket to port
        public bool Bind(ref NetEndPoint ep)
        {
            _datagramSocket = new DatagramSocket();
            _datagramSocket.Control.DontFragment = true;
            _datagramSocket.MessageReceived += OnMessageReceived;

            try
            {
                if (ep.HostName == null)
                    _datagramSocket.BindServiceNameAsync(ep.PortStr).GetResults();
                else
                    _datagramSocket.BindEndpointAsync(ep.HostName, ep.PortStr).GetResults();

                ep = new NetEndPoint(_datagramSocket.Information.LocalAddress, _datagramSocket.Information.LocalPort);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        //Send to
        public int SendTo(byte[] data, NetEndPoint remoteEndPoint)
        {
            int errorCode = 0;
            return SendTo(data, remoteEndPoint, ref errorCode);
        }

        public int SendTo(byte[] data, NetEndPoint remoteEndPoint, ref int errorCode)
        {
            try
            {
                DataWriter writer;
                if (!_peers.TryGetValue(remoteEndPoint, out writer))
                {
                    var outputStream =
                        _datagramSocket.GetOutputStreamAsync(remoteEndPoint.HostName, remoteEndPoint.PortStr)
                            .AsTask()
                            .Result;
                    writer = new DataWriter(outputStream);
                    _peers.Add(remoteEndPoint, writer);
                }

                writer.WriteBytes(data);
                var res = writer.StoreAsync().AsTask().Result;
                return (int)res;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public int ReceiveFrom(ref byte[] data, ref NetEndPoint remoteEndPoint, ref int errorCode)
        {
            _receiveWaiter.WaitOne(ReceiveTimeout);
            if (_incomingData.Count == 0)
                return 0;
            var incomingData = _incomingData.Dequeue();
            data = incomingData.Data;
            remoteEndPoint = incomingData.EndPoint;
            return data.Length;
        }

        internal void RemovePeer(NetEndPoint ep)
        {
            _peers.Remove(ep);
        }

        //Close socket
        public void Close()
        {
            _datagramSocket.MessageReceived -= OnMessageReceived;
            _datagramSocket.Dispose();
            _datagramSocket = null;

            _receiveWaiter.Reset();
            ClearPeers();
            _incomingData.Clear();
        }

        internal void ClearPeers()
        {
            foreach (var dataWriter in _peers)
            {
                dataWriter.Value.Dispose();
            }
            _peers.Clear();
        }
    }
}
#endif
