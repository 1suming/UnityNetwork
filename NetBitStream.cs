using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace UnityNetwork
{
    public class NetBitStream
    {
        //*****************************
        //定义消息头和体的长度
        //*****************************
        //头int32 4个字节 头2个字节表示包长度，后2个字节表示MsgId
        public const int header_length = 4;

        public const int header_msgid = 2;

        //体长，最大512字节
        public const int max_body_length = 512;

        //*****************************
        //定义字节长度
        //*****************************
        //byte 1个字节
        public const int BYTE_LEN = 1;

        //int 4个字节
        public const int INT32_LEN = 4;

        //short 2个字节
        public const int SHORT16_LEN = 2;

        //float 4个字节
        public const int FLOAT_LEN = 4;

        //*****************************
        //数据流
        //*****************************
        //byte数组
        private byte[] _bytes = null;
        public byte[] BYTES
        {
            get
            {
                return _bytes;
            }

            set
            {
                _bytes = value;
            }
        }

        //当前数据体长
        private ushort _bodyLength = 0;
        public ushort BodyLength
        {
            get
            {
                return _bodyLength;
            }
        }

        //byte数组总长
        public int Length
        {
            get
            {
                return header_length + _bodyLength;
            }
        }

        //使用数据流的Socket
        public Socket _socket = null;


        //构造函数 初始化
        public NetBitStream()
        {
            _bodyLength = 0;
            _bytes = new byte[header_length + max_body_length];
        }

        //写入消息标识符，参数是一个无符号短整型
        //在Write的过程中，这个函数永远被第一个使用
        public void BeginWrite(uint msgid)
        {
            _bodyLength = 0;
            //this.WriteUShort( msgid );

            this.WriteMsgId( msgid );
        }

        //写入一个Byte
        //public void WriteByte(byte bt)
        //{
        //    //如果长度超过byte数组最大值，退出
        //    if ( _bodyLength + BYTE_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    //将byte写入byte数组
        //    _bytes[header_length + _bodyLength] = bt;

        //    //体长增加
        //    _bodyLength += BYTE_LEN;
        //}

        //写入bool类型
        //public void WriteBool(bool flag)
        //{
        //    if ( _bodyLength + BYTE_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    byte b = (byte)'1';
        //    if(!flag)
        //    {
        //        b = (byte)'0';
        //    }

        //    _bytes[header_length + _bodyLength] = b;

        //    _bodyLength += BYTE_LEN;
        //}

        //写整型
        //public void WriteInt(int number)
        //{
        //    if ( _bodyLength + INT32_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    byte[] bs = System.BitConverter.GetBytes( number );
        //    bs.CopyTo( _bytes, header_length + _bodyLength );

        //    _bodyLength += INT32_LEN;
        //}

        //写无符号整型
        //public void WriteUInt(uint number)
        //{
        //    if ( _bodyLength + INT32_LEN > max_body_length ) 
        //    {
        //        return;
        //    }

        //    byte[] bs = System.BitConverter.GetBytes( number );

        //    bs.CopyTo( _bytes, header_length + _bodyLength );

        //    _bodyLength += INT32_LEN;
        //}

        //写短整型
        //public void WriteShort(short number)
        //{
        //    if ( _bodyLength + SHORT16_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    byte[] bs = System.BitConverter.GetBytes( number );

        //    bs.CopyTo( _bytes, header_length + _bodyLength );

        //    _bodyLength += SHORT16_LEN;
        //}

        //写无符号短整型
        //public void WriteUShort(ushort number)
        //{
        //    if ( _bodyLength + SHORT16_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    byte[] bs = System.BitConverter.GetBytes( number );

        //    bs.CopyTo( _bytes, header_length + _bodyLength );

        //    _bodyLength += SHORT16_LEN;
        //}

        //写浮点数
        //public void WriteFloat(float number)
        //{
        //    if ( _bodyLength + FLOAT_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    byte[] bs = System.BitConverter.GetBytes( number );

        //    bs.CopyTo( _bytes, header_length + _bodyLength );

        //    _bodyLength += FLOAT_LEN;
        //}

        //写字符串
        //因为字符串的长度不固定，所以写入字符串时是发送两条数据
        //第一条是无符号短整型，表示字符串的长度
        //第二条是字符串本身
        //public void WriteString(string str)
        //{
        //    ushort len = (ushort)Encoding.UTF8.GetByteCount( str );
        //    this.WriteUShort( len );

        //    if ( _bodyLength + len > max_body_length )
        //    {
        //        return;
        //    }

        //    Encoding.UTF8.GetBytes( str, 0, str.Length, _bytes, header_length + _bodyLength );

        //    _bodyLength += len;
        //}

        public void WriteBodyBytes(byte[] bs)
        {
            //如果长度超过byte数组最大值，退出
            if ( bs.Length > max_body_length )
            {
                return;
            }

            //将byte写入byte数组
            //_bytes[header_length + _bodyLength] = bt;
            bs.CopyTo( _bytes, header_length );

            //体长增加
            _bodyLength += (ushort)(BYTE_LEN * bs.Length);

        }

        //public byte[] BODY_BYTES
        //{
        //    get
        //    {
        //        byte[] bs = new byte[_bytes.Length - header_length];
        //        _bytes.CopyTo( bs, header_length );
        //        return bs;
        //    }
        //}

        public byte[] ReadBodyBytes()
        {
            byte[] bs = new byte[_bodyLength+1];
            //_bytes.CopyTo( bs, header_length );

            Array.Copy( _bytes, header_length, bs, 0, _bodyLength );
            return bs;
        }


        //开始读取版本1，从packet对象中读取byte数组
        public void BeginRead(NetPacket packet, out uint msgid)
        {
            //复制byte数组
            packet._bytes.CopyTo( this.BYTES, 0 );

            //获得socket
            this._socket = packet._peer;

            //初始化体长为0；
            _bodyLength = 0;

            //读取消息标识符
            this.ReadMsgId( out msgid );
        }

        //开始读取版本2 忽略消息ID
        public void BeginRead2(NetPacket packet)
        {
            packet._bytes.CopyTo( this.BYTES, 0 );

            this._socket = packet._peer;

            _bodyLength = 0;
        }

        private void WriteMsgId( uint msgid )
        {
            byte[] bs = System.BitConverter.GetBytes( msgid );

            bs.CopyTo( _bytes, header_msgid );
        }

        private void ReadMsgId( out uint msgid )
        {
            msgid = System.BitConverter.ToUInt32( _bytes, header_msgid );
        }

        //#region ReadMethod
        //读一个字节
        //public void ReadByte(out byte bt)
        //{
        //    bt = 0;

        //    if ( _bodyLength + BYTE_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    bt = _bytes[header_length + _bodyLength];

        //    _bodyLength += BYTE_LEN;
        //}

        //读bool
        //public void ReadBool( out bool flag )
        //{
        //    flag = false;

        //    if ( _bodyLength + BYTE_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    byte bt = _bytes[header_length + _bodyLength];

        //    if ( bt == (byte)'1' )
        //    {
        //        flag = true;
        //    }
        //    else
        //    {
        //        flag = false;
        //    }

        //    _bodyLength += BYTE_LEN;
        //}

        //读int
        //public void ReadInt( out int number )
        //{
        //    number = 0;

        //    if ( _bodyLength + INT32_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    number = System.BitConverter.ToInt32( _bytes, header_length + _bodyLength );

        //    _bodyLength += INT32_LEN;
        //}

        //读uint
        //public void ReadUInt( out uint number )
        //{
        //    number = 0;

        //    if ( _bodyLength + INT32_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    number = System.BitConverter.ToUInt32( _bytes, header_length + _bodyLength );

        //    _bodyLength += INT32_LEN;
        //}

        //读short
        //public void ReadShort( out short number )
        //{
        //    number = 0;

        //    if ( _bodyLength + SHORT16_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    number = System.BitConverter.ToInt16( _bytes, header_length + _bodyLength );

        //    _bodyLength += SHORT16_LEN;
        //}

        //读ushort
        //public void ReadUShort( out ushort number )
        //{
        //    number = 0;

        //    if ( _bodyLength + SHORT16_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    number = System.BitConverter.ToUInt16( _bytes, header_length + _bodyLength );

        //    _bodyLength += SHORT16_LEN;
        //}

        //读取一个float
        //public void ReadFloat( out float number )
        //{
        //    number = 0;

        //    if ( _bodyLength + FLOAT_LEN > max_body_length )
        //    {
        //        return;
        //    }

        //    number = System.BitConverter.ToSingle( _bytes, header_length + _bodyLength );

        //    _bodyLength += FLOAT_LEN;
        //}

        //读取一个字符串
        //与写入字符串相对应，首先读取一个短整型，它是字符串的长度
        //然后再读取字符串
        //public void ReadString( out string str )
        //{
        //    str = "";

        //    ushort len = 0;
        //    ReadUShort( out len );

        //    if ( _bodyLength + len > max_body_length )
        //    {
        //        return;
        //    }

        //    str = Encoding.UTF8.GetString( _bytes, header_length + _bodyLength, (int)len );

        //    _bodyLength += len;
        //}

        //复制从struct转出的byte数组
        //public bool CopyBytes( byte[] bs )
        //{
        //    if ( bs.Length > _bytes.Length )
        //    {
        //        return false;
        //    }

        //    bs.CopyTo( _bytes, 0 );

        //    _bodyLength = System.BitConverter.ToInt32( _bytes, 0 );

        //    return true;
        //}
        //#endregion

        //获取体长

        public void EncodeHeader()
        {
            byte[] bs = System.BitConverter.GetBytes( _bodyLength );

            bs.CopyTo( _bytes, 0 );
        }

        //计算体长
        public void DecodeHeader()
        {
            _bodyLength = System.BitConverter.ToUInt16( _bytes, 0 );
        }

    }
}
