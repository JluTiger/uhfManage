using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace uhfManage
{
    class MyFriend
    {
        //用于保存与每个客户相关信息：套接字与接收缓存
        public Socket socket;
        public byte[] Rcvbuffer;
        //实例化方法
        public MyFriend(Socket s)
        {
            socket = s;
        }
        //清空接受缓存，在每一次新的接收之前都要调用该方法
        public void ClearBuffer()
        {
            Rcvbuffer = new byte[1024];
        }
        //
        public void Dispose()
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            finally
            {
                socket = null;
                Rcvbuffer = null;
            }
        }
    }
}
