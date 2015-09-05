using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace UnityNetwork
{
    public class NetPacket
    {
        //byte数组
        public byte[] _bytes;

        //相关的socket
        public Socket _peer = null;

        //包总长
        //protected int _length = 0;

        //错误信息
        public string _error = "";

        //初始化
        public NetPacket()
        {
            _bytes = new byte[NetBitStream.header_length + NetBitStream.max_body_length];
        }

        ////从NetBitStream的byte数组中复制数据
        //public void CopyBytes(NetBitStream stream)
        //{
        //    stream.BYTES.CopyTo( _bytes, 0 );

        //    _length = stream.Length;
        //}

        //设置消息标识符
        public void SetIDOnly(ushort msgid)
        {
            byte[] bs = System.BitConverter.GetBytes( msgid );

            //注意是将消息标识复制到头4个字节之后
            bs.CopyTo( _bytes, NetBitStream.header_msgid );

            //_length = NetBitStream.header_length;
        }

        //取得消息标识符
        public void TOID( out ushort msgid )
        {
            msgid = System.BitConverter.ToUInt16( _bytes, NetBitStream.header_msgid );
        }
    }
}
