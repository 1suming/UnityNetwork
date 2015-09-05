using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnityNetwork
{
    public class NetTCPServer
    {
        ////最大连接数
        //public int _maxConnections = 5000;

        //发送接收超时
        public int _sendTimeout = 3;
        public int _recvTimeout = 3;

        //服务器Socket
        Socket _listener;

        //端口号
        int _port = 0;

        //网络管理器 处理消息和逻辑
        private NetworkManager _netMgr = null;

        public NetTCPServer()
        {
            _netMgr = NetworkManager.Instance;
        }

        //开始监听
        public bool CreateTcpServer( string ip, int listenPort )
        {
            _port = listenPort;
            _listener = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );

            foreach ( IPAddress address in Dns.GetHostEntry( ip ).AddressList )
            {
                try
                {
                    //获取服务器地址和端口
                    IPAddress hostIP = address;
                    IPEndPoint ipe = new IPEndPoint( address, _port );

                    //将socket与地址与端口绑定
                    _listener.Bind( ipe );

                    //允许客户端连接的数量
                    _listener.Listen( 5 );

                    //等待客户端的连接
                    _listener.BeginAccept( new System.AsyncCallback( ListenTcpClient ), _listener );

                    break;
                }
                catch(System.Exception)
                {
                    return false;
                }
            }
            return true;
        }

        void ListenTcpClient(System.IAsyncResult ar)
        {
            //创建一个NetBitStream对象保存接受到的数据
            NetBitStream stream = new NetBitStream();
            try
            {
                //取得客户端的socket
                Socket client = _listener.EndAccept( ar );
                stream._socket = client;

                //设置发送接受超时
                client.SendTimeout = _sendTimeout;
                client.ReceiveTimeout = _recvTimeout;

                //接受从服务器返回的头信息
                client.BeginReceive( stream.BYTES, 0, NetBitStream.header_length, SocketFlags.None, new System.AsyncCallback( ReceiveHeader ), stream );

                //向逻辑处理队列发送新连接的消息
                PushPacket( (ushort)MessageIdentifiers.ID.NEW_INCOMING_CONNECTION, "", client );
            }
            catch(System.Exception)
            {
                //出现错误
            }

            //继续接受其他连接
            _listener.BeginAccept( new System.AsyncCallback( ListenTcpClient ), _listener );
        }

        //接受数据头
        void ReceiveHeader( System.IAsyncResult ar )
        {
            NetBitStream stream = (NetBitStream)ar.AsyncState;

            try
            {
                int read = stream._socket.EndReceive( ar );

                //服务器断开连接
                if ( read < 1 )
                {
                    PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, "", stream._socket );
                    return;
                }

                //获得消息体长度
                stream.DecodeHeader();

                //开始接受数据体
                stream._socket.BeginReceive( stream.BYTES, NetBitStream.header_length, stream.BodyLength, SocketFlags.None, new System.AsyncCallback( ReceiveBody ), stream );
            }
            catch(System.Exception e)
            {
                PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, e.Message, stream._socket );
            }
        }

        //接受数据体
        void ReceiveBody( System.IAsyncResult ar )
        {
            NetBitStream stream = (NetBitStream)ar.AsyncState;

            try
            {
                int read = stream._socket.EndReceive( ar );

                //用户已下线
                if ( read < 1 )
                {
                    PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, "", stream._socket );
                    return;
                }

                //将收到的数据传入逻辑处理队列
                PushPacket2( stream );

                //下一轮读取
                stream._socket.BeginReceive( stream.BYTES, 0, NetBitStream.header_length, SocketFlags.None, new System.AsyncCallback( ReceiveHeader ), stream );
            }
            catch(System.Exception e)
            {
                PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, e.Message, stream._socket );
            }
        }

        //发送数据
        public void Send( NetBitStream bts, Socket peer )
        {
            NetworkStream ns;
            lock ( peer )
            {
                ns = new NetworkStream( peer );
            }

            if ( ns.CanWrite )
            {
                try
                {
                    ns.BeginWrite( bts.BYTES, 0, bts.Length, new System.AsyncCallback( SendCallback ), ns );
                }
                catch (System.Exception)
                {
                    PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, "", peer );
                }
            }
        }

        //发送回调
        private void SendCallback(System.IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;
            try
            {
                ns.EndWrite( ar );
                ns.Flush();
                ns.Close();
            }
            catch(System.Exception)
            {

            }
        }

        //向Network Manager的队列传递内部消息
        void PushPacket( ushort msgid, string exception, Socket peer )
        {
            NetPacket packet = new NetPacket();
            packet.SetIDOnly( msgid );
            packet._error = exception;
            packet._peer = peer;

            _netMgr.AddPacket( packet );
        }

        //向Network Manager的队列传递数据
        void PushPacket2( NetBitStream stream )
        {
            NetPacket packet = new NetPacket();
            stream.BYTES.CopyTo( packet._bytes, 0 );
            packet._peer = stream._socket;

            _netMgr.AddPacket( packet );
        }
    }
}
