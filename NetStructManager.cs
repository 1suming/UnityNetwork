using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityNetwork
{
    public class NetStructManager
    {
        //数据头，每个struct的第一个属性都应当是一个int类型，用来保存数据体长
        public const int HeaderSize = 4;

        //将结构体转为byte数组
        public static byte[] getBytes(object structObj)
        {
            //获得结构体大小
            int size = Marshal.SizeOf( structObj );

            //保存结构的比特数组
            byte[] bytes = new byte[size];

            //将结构体复制到内存空间
            System.IntPtr ptr = Marshal.AllocHGlobal( size );

            //将结构存入内存
            Marshal.StructureToPtr( structObj, ptr, true );

            //将内存复制到比特数组中
            Marshal.Copy( ptr, bytes, 0, size );

            //释放内存
            Marshal.FreeHGlobal( ptr );

            //将体长写入数据头中
            EncoderHeader( ref bytes );

            return bytes;


        }

        //将byte数组转为结构体
        public static object fromBytes( byte[] bytes, System.Type type )
        {
            //结构的大小
            int size = Marshal.SizeOf( type );
            if ( size > bytes.Length )
            {
                //返回空
                return null;
            }

            //分配内存
            System.IntPtr ptr = Marshal.AllocHGlobal( size );

            //将比特数组复制到内存中
            Marshal.Copy( bytes, 0, ptr, size );

            //从内存中创建结构
            object obj = Marshal.PtrToStructure( ptr, type );

            //释放内存
            Marshal.FreeHGlobal( ptr );

            return obj;
        }

        //每个struct的第一个属性都应当是一个int类型
        //我们要确保头4个字节保存了数据的体长
        public static void EncoderHeader( ref byte[] bytes )
        {
            //数据体长
            int length = bytes.Length - HeaderSize;

            byte[] bs = System.BitConverter.GetBytes( length );

            //写入byte数组的头4个字节
            bs.CopyTo( bytes, 0 );
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential,CharSet=CharSet.Ansi,Pack=1)]
    public struct TestStruct
    {
        //一个int值，用来保存数据体长度
        public int header;

        //消息标识符
        public ushort msgid;

        //其他数据...
        public int n;
        public float m;

        //必须定义字符串类型的最大长度
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 128 )]
        public string str;
    }
}
