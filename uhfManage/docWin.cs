using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace uhfManage
{
    public partial class docWin : Form
    {
        public docWin()
        {
            InitializeComponent();
        }

        private void docWin_Load(object sender, EventArgs e)
        {
            dataGridviewShow();
        }

        private void dataGridviewShow()//连接数据库，显示datagridview
        {
            OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
            OleDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = "select * from infoData";
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
            cmd.Dispose();
            conn.Close();
            dataGridView1.DataSource = dt;
            AutoSizeColumn(dataGridView1);
        }
        /// <summary>
        /// 使DataGridView的列自适应宽度
        /// </summary>
        /// <param name="dgViewFiles"></param>
        private void AutoSizeColumn(DataGridView dgViewFiles)
        {
            int width = 0;
            //使列自使用宽度
            //对于DataGridView的每一个列都调整
            for (int i = 0; i < dgViewFiles.Columns.Count; i++)
            {
                //将每一列都调整为自动适应模式
                dgViewFiles.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.AllCells);
                //记录整个DataGridView的宽度
                width += dgViewFiles.Columns[i].Width;
            }
            //判断调整后的宽度与原来设定的宽度的关系，如果是调整后的宽度大于原来设定的宽度，
            //则将DataGridView的列自动调整模式设置为显示的列即可，
            //如果是小于原来设定的宽度，将模式改为填充。
            if (width > dgViewFiles.Size.Width)
            {
                dgViewFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            }
            else
            {
                dgViewFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            //冻结某列 从左开始 0，1，2
            //dgViewFiles.Columns[1].Frozen = true;
        }

        private void upDocBtn_Click(object sender, EventArgs e)
        {
            if(textBoxDoc.Text.Trim() == "")
            {
                MessageBox.Show("请输入医嘱信息！", "提示信息：", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                updataDoc();
            }        
        }

        private void updataDoc()
        {
            Int32 selectedRowCount = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Selected);
            OleDbConnection conn = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source=rfidInfo.mdb"); //Jet OLEDB:Database Password=
            OleDbCommand cmd = conn.CreateCommand();
            conn.Open();
            if (selectedRowCount < 1)
            {
                conn.Close();//关闭数据库连接
                MessageBox.Show("更新失败！请选中要更新的用户信息。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                DialogResult dr = MessageBox.Show("确定要更新这条记录中的医嘱信息吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
                    {
                        if (dataGridView1.Rows[i].Selected)
                        {
                            string sqlDel = "Update [infoData] set [医嘱信息]='" + textBoxDoc.Text.Trim() + "' Where EPC='" + dataGridView1.Rows[i].Cells[0].Value + "'";
                            OleDbCommand com = new OleDbCommand(sqlDel, conn);
                            com.ExecuteNonQuery();
                            conn.Close();//关闭数据库连接
                            textBoxDoc.Text = "";
                            dataGridviewShow();//刷新一下窗体上的预览
                        }
                    }
                }

            }
   
        }
        
    }
}
