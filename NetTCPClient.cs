using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnityNetwork
{
    public class NetTCPClient
    {
        //发送和接受的超时事件
        public int _sendTimeout = 3;
        public int _recvTimeout = 3;

        //处理消息和逻辑的对象
        private NetworkManager _netMgr = null;

        //与远程服务器链接的socket
        private Socket _socket = null;

        public NetTCPClient()
        {
            _netMgr = NetworkManager.Instance;
        }

        public bool Connect( string address, int remotePort )
        {
            if ( _socket != null && _socket.Connected )
            {
                return true;
            }

            //解析域名
            IPHostEntry hostEntry = Dns.GetHostEntry( address );

            foreach(IPAddress ip in hostEntry.AddressList)
            {
                try
                {
                    //获得远程服务器的地址
                    IPEndPoint ipe = new IPEndPoint( ip, remotePort );

                    //创建socket
                    _socket = new Socket( ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp );

                    //开始连接
                    _socket.BeginConnect( ipe, new System.AsyncCallback( ConnectionCallback ), _socket );

                    break;
                }
                catch(System.Exception e)
                {
                    //连接失败 将消息传入逻辑处理队列
                    PushPacket( (ushort)MessageIdentifiers.ID.CONNECTION_ATTEMPT_FAILED, e.Message );
                }
            }

            return true;
        }

        //向Network Manager的队列传递内部消息
        void PushPacket( ushort msgid, string exception )
        {
            NetPacket packet = new NetPacket();
            packet.SetIDOnly( msgid );
            packet._error = exception;
            packet._peer = _socket;

            _netMgr.AddPacket( packet );
        }


        //向Network Manager的队列传递NetBitStream对象的数据
        void PushPacket2(NetBitStream stream)
        {
            NetPacket packet = new NetPacket();
            stream.BYTES.CopyTo( packet._bytes, 0 );
            packet._peer = stream._socket;

            _netMgr.AddPacket( packet );
        }

        void ConnectionCallback(System.IAsyncResult ar)
        {
            //创建NetBitStream对象
            NetBitStream stream = new NetBitStream();

            //获得服务器socket
            stream._socket = (Socket)ar.AsyncState;

            try
            {
                //与服务器取得连接，如果连接失败，这里将抛出异常
                _socket.EndConnect( ar );

                //设置发送和接受的超时事件
                _socket.SendTimeout = _sendTimeout;
                _socket.ReceiveTimeout = _recvTimeout;

                //向逻辑处理队列发送成功连接的消息
                PushPacket( (ushort)MessageIdentifiers.ID.CONNECTION_REQUEST_ACCEPTED, "" );

                //开始接受从服务器发来的数据
                _socket.BeginReceive( stream.BYTES, 0, NetBitStream.header_length, SocketFlags.None, new System.AsyncCallback( ReceiveHeader ), stream );

            }
            catch(System.Exception e)
            {
                //出现异常，错误处理
                if ( e.GetType() == typeof( SocketException ) )
                {
                    if ( ( (SocketException)e ).SocketErrorCode == SocketError.ConnectionRefused )
                    {
                        PushPacket( (ushort)MessageIdentifiers.ID.CONNECTION_ATTEMPT_FAILED, e.Message );
                    }
                    else
                    {
                        PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, e.Message );
                    }

                }
                Disconnect( 0 );
            }
        }

        public void Disconnect( int timeout )
        {
            if ( _socket.Connected )
            {
                _socket.Shutdown( SocketShutdown.Receive );
                _socket.Close( timeout );
            }
            else
            {
                _socket.Close();
            }
        }

        void ReceiveHeader( System.IAsyncResult ar )
        {
            NetBitStream stream = (NetBitStream)ar.AsyncState;

            try
            {
                int read = _socket.EndReceive( ar );

                //接受0字节，与服务器断开连接
                if ( read < 1 )
                {
                    Disconnect( 0 );
                    PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, "" );
                    return;
                }

                //获得数据体长度
                stream.DecodeHeader();

                //读取数据体
                _socket.BeginReceive( stream.BYTES, NetBitStream.header_length, stream.BodyLength, SocketFlags.None, new System.AsyncCallback( ReceiveBody ), stream );
            }
            catch(System.Exception e)
            {
                PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, e.Message );
                Disconnect( 0 );
            }
        }

        void ReceiveBody(System.IAsyncResult ar)
        {
            NetBitStream stream = (NetBitStream)ar.AsyncState;

            try
            {
                int read = _socket.EndReceive(ar);

                if(read < 1)
                {
                    Disconnect(0);
                    PushPacket((ushort)MessageIdentifiers.ID.CONNCET_LOST,"");
                    return;
                }

                //将收到的数据传入逻辑处理队列
                PushPacket2(stream);

                //下一轮读取
                _socket.BeginReceive(stream.BYTES,0,NetBitStream.header_length,SocketFlags.None,new System.AsyncCallback(ReceiveHeader),stream);
            }
            catch(System.Exception e)
            {
                PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, e.Message );
                Disconnect( 0 );
            }
        }

        public void Send( NetBitStream bts )
        {
            if(!_socket.Connected)
            {
                return;
            }

            //创建NetworkStream
            NetworkStream ns;

            //加锁，避免多线程下出问题
            lock(_socket)
            {
                ns = new NetworkStream( _socket );
            }

            if ( ns.CanWrite )
            {
                try
                {
                    ns.BeginWrite( bts.BYTES, 0, bts.Length, new System.AsyncCallback( SendCallback ), ns );
                }
                catch(System.Exception)
                {
                    PushPacket((ushort)MessageIdentifiers.ID.CONNCET_LOST,"");
                    Disconnect(0);
                }
            }
        }

        //发送回调
        private void SendCallback(System.IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;

            try
            {
                //发送结束
                ns.EndWrite( ar );

                //释放内存
                ns.Flush();
                ns.Close();
            }
            catch(System.Exception)
            {
                PushPacket( (ushort)MessageIdentifiers.ID.CONNCET_LOST, "" );
                Disconnect( 0 );
            }
        }
    }
}
