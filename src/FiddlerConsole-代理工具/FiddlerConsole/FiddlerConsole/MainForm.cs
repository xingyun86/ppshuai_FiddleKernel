using Fiddler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FiddlerConsole
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        Proxy oProxy;
        public string sHostname = "localhost";
        public int port = 8899;//代理端口 设置个没使用的就行
        public List<Fiddler.Session> oAllSessions = new List<Session>();
        public List<string> keys = new List<string>();
        public bool ifWork = false;
        List<Fiddler.Session> listSession = new List<Fiddler.Session>();

        public string ReadFileInfo(string strFileName)
        {
            FileStream file = null;
            StreamReader reader = null;
            string strData = "";
            try
            {
                file = new FileStream(strFileName, FileMode.Open, FileAccess.Read);
                if (file != null)
                {
                    reader = new StreamReader(file, Encoding.Default);
                    if (reader != null)
                    {
                        strData = reader.ReadToEnd();
                        reader.Close();
                    }
                    file.Close();
                }
            } catch(Exception e)
            {
                throw e;
            }
            return strData;
        }
        public void WriteFileInfo(string strFileName, string strData)
        {
            FileStream file = null;
            StreamWriter writer = null;
            try
            {
                file = new FileStream(strFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                if (file != null)
                {
                    writer = new StreamWriter(file);
                    if (writer != null)
                    {
                        writer.WriteLine(strData);
                        writer.Close();
                    }
                    file.Close();
                }
            } catch(Exception e)
            {
                throw e;
            }
        }
        
        //开始运行抓取
        public void Start()
        {
            oAllSessions = new List<Fiddler.Session>();
            //在请求发出之前做的操作，可以捕获、修改请求内容
            Fiddler.FiddlerApplication.BeforeRequest += delegate (Fiddler.Session oS)
            {
                oS.bBufferResponse = true;
                //检验链接中是否存在关键词，不用可以注释掉
                if (UrlExistKey(oS.fullUrl))
                {
                    Monitor.Enter(oAllSessions);
                    oAllSessions.Add(oS);
                    Monitor.Exit(oAllSessions);
                }
                oS["X-AutoAuth"] = "(default)";
            };
            //响应结果返回，Fiddler接收到之后，浏览器等接收之前，可以捕获、修改响应内容
            Fiddler.FiddlerApplication.BeforeResponse += delegate (Fiddler.Session oS)
            {
                oS.bBufferResponse = true;
                //检验链接中是否存在关键词，不用可以注释掉
                if (UrlExistKey(oS.fullUrl))
                {
                    Monitor.Enter(oAllSessions);
                    oAllSessions.Add(oS);
                    oS.utilDecodeResponse();
                    Write(string.Format("{0}:HTTP {1} for {2}", oS.id, oS.responseCode, oS.fullUrl));
                    Write(oS.GetResponseBodyAsString());
                    Monitor.Exit(oAllSessions);
                }
                oS["X-AutoAuth"] = "(default)";
            };
            FiddlerApplication.SetAppDisplayName("PPSHUAI");
            //第三个参数为True，要求捕获https
            FiddlerApplication.Startup(port, true, true, true);
            oProxy = FiddlerApplication.CreateProxyEndpoint(port, true, sHostname);
            //此处调用makecert.exe设置证书
            if (Fiddler.CertMaker.trustRootCert() == true)
            {
                MessageBox.Show("证书安装成功", "操作提示");
            }
            else
            {
                MessageBox.Show("证书安装出错", "操作提示");
            }
            ifWork = true;
        }
        //检验链接中是否存在关键词，默认False
        private bool UrlExistKey(string url)
        {
            if (keys == null || keys.Count == 0)
                return false;
            for (int i = 0; i < keys.Count; i++)
            {
                if (url.Contains(keys[i]))
                    return true;
            }
            return false;
        }
        //关闭方法
        public void Stop()
        {
            ifWork = false;
            if (null != oProxy) oProxy.Dispose();
            Fiddler.FiddlerApplication.Shutdown();
        }
        //窗体关闭时关闭，。。。。好像不好用
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        //委托输出内容
        public void Write(string str)
        {
            if (textBoxOutput.InvokeRequired)
            {
                while (!textBoxOutput.IsHandleCreated)
                {
                    //解决窗体关闭时出现“访问已释放句柄“的异常
                    if (textBoxOutput.Disposing || textBoxOutput.IsDisposed)
                        return;
                }
                this.Invoke((EventHandler)delegate
                {
                    textBoxOutput.AppendText(str + Environment.NewLine);
                });
            }
            else
            {
                textBoxOutput.AppendText(str + Environment.NewLine);
            }
        }

        //开始按钮
        private void buttonStart_Click(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            string strData = ReadFileInfo("monitor.lst");
            string[] slist = strData.Split('\n');
            foreach (string s in slist)
            {
                if(s.Length > 0)
                {
                    s.Replace('\n', '\0');
                    //用于过滤链接的关键字
                    list.Add(s);
                }
            }
            keys = list;
            Start();
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
        }
        //关闭按钮，手动关闭。比较靠谱，不关闭本机就连不上网了
        private void buttonStop_Click(object sender, EventArgs e)
        {
            Stop();
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
        }

        private void buttonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("指定列表配置文件为monitor.lst\r\n每行定义一个关键词\r\n例如：\r\nbaidu\r\nqq\r\n当前监控列表为baidu和qq,修改后重新启动", "使用说明");
        }
    }
}
