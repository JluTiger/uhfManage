using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.OleDb;
using System.Globalization;


namespace uhfManage
{
    public partial class rfidMain : Form
    {
        public rfidMain()
        {
            InitializeComponent();

            this.FormClosing += (o, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    DialogResult dr = MessageBox.Show("是否退出系统？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                    if (dr == DialogResult.OK)
                    {
                        stopListen();
                        Application.Exit();
                    }
                    else
                    {
                        e.Cancel = true;
                        return;
                    }

                }
            };
        }

        private void stopListen()
        {
            if (!IsStart)
                return;
            listener.Stop();
            IsStart = false;
        }


        //保存与客户相关的信息列表
        ArrayList friends = new ArrayList();
        //负责监听的套接字
        TcpListener listener;
        //只是是否启动了监听
        bool IsStart = false;
        //对控件进行调用委托类型和委托方法
        //在列表中写字符串
        delegate void AppendDelegate(string str);
        AppendDelegate AppendString;
        //在建立列表时，向下拉列表中添加客户信息
        delegate void AddDelegate(MyFriend frd);
        AddDelegate Addfriend;
        //在断开连接时，从下拉列表中删除客户信息
        delegate void RemoveDelegate(MyFriend frd);
        RemoveDelegate Removefriend;
        //接收的scoket数据
        string infoData = "";
        //定义整型用来记录listbox的行数变化，以便执行定时任务
        int lineNum = 0;
        //在列表中写字符串的委托方法
        private void AppendMethod(string str)
        {
            listBoxStatu.Items.Add(str);
            listBoxStatu.SelectedIndex = listBoxStatu.Items.Count - 1;
            listBoxStatu.ClearSelected();
        }
        //向下拉列表中添加信息的委托方法
        private void AddMethod(MyFriend frd)
        {
            lock (friends)
            {
                friends.Add(frd);
            }
            comboBoxClient.Items.Add(frd.socket.RemoteEndPoint.ToString());
        }

        //从下拉列表中删除信息的委托方法
        private void RemoveMethod(MyFriend frd)
        {
            int i = friends.IndexOf(frd);
            comboBoxClient.Items.RemoveAt(i);
            lock (friends)
            {
                friends.Remove(frd);
            }
            frd.Dispose();
        }


        private void rfidMain_Load(object sender, EventArgs e)
        {
            MaximizeBox = false;
            //实例化委托对象，与委托方法关联
            AppendString = new AppendDelegate(AppendMethod);
            Addfriend = new AddDelegate(AddMethod);
            Removefriend = new RemoveDelegate(RemoveMethod);
            //获取本机IPv4地址
            List<string> listIP = getIP();
            if (listIP.Count == 0)
            {
                this.comboBoxIP.Items.Clear();
                this.comboBoxIP.Text = "未能获取IP！";
            }
            else if (listIP.Count == 1)
            {
                this.comboBoxIP.Items.Add(listIP[0]);
                this.comboBoxIP.SelectedIndex = 0;
            }
            else
            {
                foreach (string str in listIP)
                {
                    this.comboBoxIP.Items.Add(str);
                }
                this.comboBoxIP.Text = "请选择IP！";
            }
            //设置默认端口号
            textBoxServerPort.Text = "9600";
            System.Timers.Timer pTimer = new System.Timers.Timer(1000);//每隔1秒执行一次，没用winfrom自带的
            pTimer.Elapsed += pTimer_Elapsed;//委托，要执行的方法
            pTimer.AutoReset = true;//获取该定时器自动执行
            pTimer.Enabled = true;//这个一定要写，要不然定时器不会执行的
            Control.CheckForIllegalCrossThreadCalls = false;//这个不太懂，有待研究

            startListen();//开始监听
        }


        string serMatcnInfo = "";

        //定时自动执行的方法 
        private void pTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (listBoxStatu.Items.Count != lineNum)//如果行数变化，表示接收到数据，则执行
            {            
                    if (infoData != "")//避免窗体初次启动时，infoData默认值对方法的影响
                    {
                        if (infoData.StartsWith("EPC:"))//接收EPC
                        {
                        string[] EPC = infoData.Split(':');//以逗号作为分隔符，获取传来的数据
                        serMatcnInfo = EPC[1];
                        infoData = "";
                        search(serMatcnInfo);
                        }
                    if (infoData.StartsWith("Info"))//接收手机端填写录入信息
                    {
                        string[] Info = infoData.Split(':');
                        infoData = "";
                        OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
                        OleDbCommand cmd = conn.CreateCommand();
                        //收到的录入信息最后有一个\r\n,需要将其去除
                        Info[4]= Info[4].TrimEnd((char[])"\r\n".ToCharArray());
                        cmd.CommandText = "insert into infoLogin([EPC],[姓名],[年龄],[手机号码]) values('" +Info[1] + "','" + Info[2] + "','" + Info[3] + "','" + Info[4] + "')";
                        conn.Open();
                        OleDbDataReader dr = cmd.ExecuteReader();
                        conn.Close();
                        textBoxEPC.Text = Info[1];
                        textBoxName.Text = Info[2];
                        textBoxAge.Text = Info[3];
                        textBoxPhone.Text = Info[4];
                        setDocInfo();
                        textBoxEPC.Text = "";
                        textBoxName.Text = "";
                        textBoxAge.Text = "";
                        textBoxPhone.Text = "";
                        this.listBoxStatu.Items.Clear();
                        lineNum = 0;
                    }
                    if (infoData.StartsWith("Search"))//亲属端查询用
                    {
                        string[] search = infoData.Split(':');                      
                        infoData = "";
                        searchEPC(search[1]);
                    }

                    } 
            }
        }

        //查询信息用的
        string get_EPC;
        private void searchEPC(string requir)
        {
            OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
            OleDbCommand cmd = conn.CreateCommand();
            string searchIn = requir.TrimEnd((char[])"\r\n".ToCharArray());
            cmd.CommandText = "select * from infoLogin where [手机号码]= '" + searchIn +"'";
            conn.Open();
            OleDbDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            if (dr.HasRows)
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    dt.Columns.Add(dr.GetName(i));
                }
                dt.Rows.Clear();
            }
            while (dr.Read())
            {
                DataRow row = dt.NewRow();
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    row[i] = dr[i];
                }
                dt.Rows.Add(row);
            }
            int ii = dt.Rows.Count;//记录查询的条数
            conn.Close();
            if (ii == 0)
            {
                SendData((MyFriend)friends[comboBoxClient.SelectedIndex], "no\n");
            }
            if (ii != 0)
            {
                get_EPC = dt.Rows[0]["EPC"].ToString();
                searchShow(get_EPC);
            }
        }



        //查询是否存在该EPC
        private void search(string search)
        {
            OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
            OleDbCommand cmd = conn.CreateCommand();
            string searchIn = search.TrimEnd((char[])"\r\n".ToCharArray());
            cmd.CommandText = "select * from infoLogin where [EPC]= '" + searchIn + "'";
            conn.Open();
            OleDbDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            if (dr.HasRows)
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    dt.Columns.Add(dr.GetName(i));
                }
                dt.Rows.Clear();
            }
            while (dr.Read())
            {
                DataRow row = dt.NewRow();
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    row[i] = dr[i];
                }
                dt.Rows.Add(row);
            }
            int ii = dt.Rows.Count;//记录查询的条数
            conn.Close();
            if(ii == 0)
            {
                SendData((MyFriend)friends[comboBoxClient.SelectedIndex], "no\n");
                textBoxEPC.Text = serMatcnInfo;
            }
            if(ii != 0)
            {
                searchShow(serMatcnInfo);
            }
        }

        //发送EPC对应的医嘱信息
        private void searchShow(string searchInfo)
        {
            OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
            OleDbCommand cmd = conn.CreateCommand();
            searchInfo = searchInfo.TrimEnd((char[])"\r\n".ToCharArray());
            //searchInfo = searchInfo.TrimEnd('\0');
            cmd.CommandText = "select * from infoData where [EPC]= '"+searchInfo+"'" ;
            conn.Open();          
            OleDbDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            if (dr.HasRows)
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    dt.Columns.Add(dr.GetName(i));
                }
                dt.Rows.Clear();
            }
            while (dr.Read())
            {
                DataRow row = dt.NewRow();
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    row[i] = dr[i];
                }
                dt.Rows.Add(row);
            }
            int searchCount = dt.Rows.Count;//记录查询的条数
            conn.Close();
            //if (searchCount == 0)
            //{
                //SendData((MyFriend)friends[comboBoxClient.SelectedIndex], "不存在病患就医数据！");    
           // }
            if(searchCount != 0)
            {
                string getEPCInfo = dt.Rows[0]["EPC"].ToString();
                string getNameInfo = dt.Rows[0]["姓名"].ToString();
                string getAgeInfo = dt.Rows[0]["年龄"].ToString();
                string getDocInfo = dt.Rows[0]["医嘱信息"].ToString();
                string messageInfo = getEPCInfo + ";" + getNameInfo + ";" + getAgeInfo + ";" + getDocInfo+"\r\n";
                SendData((MyFriend)friends[comboBoxClient.SelectedIndex], messageInfo);
            }
        }

        private void startListen()
        {
            //服务器已在其中监听，则返回
            if (IsStart)
                return;
            //服务器启动侦听
            IPEndPoint localep = new IPEndPoint(IPAddress.Parse(comboBoxIP.Text), int.Parse(textBoxServerPort.Text));
            listener = new TcpListener(localep);
            listener.Start(10);
            IsStart = true;
            //this.Text = "监听中...";
            //listBoxStatu.Invoke(AppendString, string.Format("服务器已经启动监听！端点为：{0}。", listener.LocalEndpoint.ToString()));//本机的ip和端口号
            //接受连接请求的异步调用
            AsyncCallback callback = new AsyncCallback(AcceptCallBack);
            listener.BeginAcceptSocket(callback, listener);
        }

     
        private void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                //完成异步接收连接请求的异步调用
                //将连接信息添加到列表和下拉列表中
                Socket handle = listener.EndAcceptSocket(ar);
                MyFriend frd = new MyFriend(handle);
                comboBoxClient.Invoke(Addfriend, frd);
                AsyncCallback callback;
                //继续调用异步方法接收连接请求
                if (IsStart)
                {
                    callback = new AsyncCallback(AcceptCallBack);
                    listener.BeginAcceptSocket(callback, listener);
                }

                //开始在连接上进行异步的数据接收
                frd.ClearBuffer();
                callback = new AsyncCallback(ReceiveCallback);
                frd.socket.BeginReceive(frd.Rcvbuffer, 0, frd.Rcvbuffer.Length, SocketFlags.None, callback, frd);

            }
            catch
            {
                //在调用EndAcceptSocket方法时可能引发异常
                //套接字Listener被关闭，则设置为未启动侦听状态
                IsStart = false;

            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {

            MyFriend frd = (MyFriend)ar.AsyncState;
            try
            {
                int i = frd.socket.EndReceive(ar);
                if (i == 0)
                {
                    comboBoxClient.Invoke(Removefriend, frd);
                    return;
                }
                else
                {
                    string data = Encoding.UTF8.GetString(frd.Rcvbuffer, 0, i);
                    infoData = data;//将传来的数据传给infoData
                    lineNum = listBoxStatu.Items.Count;//将行数赋值给lineNum
                    data = string.Format("From[{0}]:{1}", frd.socket.RemoteEndPoint.ToString(), data);
                    comboBoxClient.SelectedItem= (frd.socket.RemoteEndPoint.ToString());//收到信息之后选中发送信息的ip
                    listBoxStatu.Invoke(AppendString, data);
                    frd.ClearBuffer();
                    AsyncCallback callback = new AsyncCallback(ReceiveCallback);
                    frd.socket.BeginReceive(frd.Rcvbuffer, 0, frd.Rcvbuffer.Length, SocketFlags.None, callback, frd);
                }

            }
            catch
            {
                comboBoxClient.Invoke(Removefriend, frd);
            }

        }

        private void SendData(MyFriend frd, string data)
        {
            try
            {
                byte[] msg = Encoding.UTF8.GetBytes(data);
                AsyncCallback callback = new AsyncCallback(SendCallback);
                frd.socket.BeginSend(msg, 0, msg.Length, SocketFlags.None, callback, frd);
                //data = string.Format("To[{0}]:{1}", frd.socket.RemoteEndPoint.ToString(), data);
                //listBoxStatu.Invoke(AppendString, data);
            }
            catch
            {
                comboBoxClient.Invoke(Removefriend, frd);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            MyFriend frd = (MyFriend)ar.AsyncState;
            try
            {
                frd.socket.EndSend(ar);
            }
            catch
            {
                comboBoxClient.Invoke(Removefriend, frd);
            }
        }

      

        //获取本机IPv4地址
        public List<string> getIP()
        {
            List<string> listIP = new List<string>();
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        listIP.Add(IpEntry.AddressList[i].ToString());
                    }
                }
                return listIP;
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取本机IP出错:" + ex.Message);
                listIP.Clear();
                return listIP;
            }
        }




        private void buttonEnter_Click(object sender, EventArgs e)
        {
            if (textBoxEPC.Text.Trim() == "" || textBoxName.Text.Trim() == "" || textBoxAge.Text.Trim() == "" || textBoxPhone.Text.Trim() == "")
            {
                MessageBox.Show("请确保录入信息填写完整！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else {
                OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "insert into infoLogin([EPC],[姓名],[年龄],[手机号码]) values('" + textBoxEPC.Text.Trim() + "','" + textBoxName.Text.Trim() + "','" + textBoxAge.Text.Trim() + "','" + textBoxPhone.Text.Trim()+ "')";
                conn.Open();
                OleDbDataReader dr = cmd.ExecuteReader();
                conn.Close();
                setDocInfo();
                textBoxEPC.Text = "";
                textBoxName.Text = "";
                textBoxAge.Text = "";
                textBoxPhone.Text = "";
                this.listBoxStatu.Items.Clear();
                lineNum = 0;
            }
        }

        //录入信息之后自动在医嘱信息那个表中建立信息
        private void setDocInfo()
        {
            OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "insert into infoData([EPC],[姓名],[年龄],[医嘱信息]) values('" + textBoxEPC.Text.Trim() + "','" + textBoxName.Text.Trim() + "','" + textBoxAge.Text.Trim() + "','暂无')";
            conn.Open();
            OleDbDataReader dr = cmd.ExecuteReader();
            conn.Close();
        }

        private void buttonInfo_Click(object sender, EventArgs e)
        {
            Form f1 = new infoManage();
            f1.ShowDialog();
        }

        private void buttonDoc_Click(object sender, EventArgs e)
        {
            Form f1 = new docWin();
            f1.ShowDialog();
        }
    }
}
