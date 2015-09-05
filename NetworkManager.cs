using System;
using System.Collections.Generic;
using System.Text;

namespace UnityNetwork
{
    public class NetworkManager
    {
        //NetworkManager静态实例
        protected static NetworkManager _instance = null;

        public static NetworkManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public NetworkManager()
        {
            _instance = this;
        }

        //数据包队列
        private static System.Collections.Queue Packets = new System.Collections.Queue();

        public int PacketSize
        {
            get
            {
                return Packets.Count;
            }
        }

        //数据包入队
        public void AddPacket( NetPacket packet )
        {
            Packets.Enqueue( packet );
        }

        //数据包出队
        public NetPacket GetPacket()
        {
            if ( Packets.Count == 0 )
            {
                return null;
            }

            return (NetPacket)Packets.Dequeue();
        }

        //在这里取出数据包，更新逻辑
        public virtual void Update()
        {
            //暂时什么也不做
            //在实际应用中，将创建用一个类继承NetworkManager，并重写Update处理逻辑
        }
    }
}
