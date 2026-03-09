using System.Windows.Forms;
using System.Text;
using TcpServer;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using AgServerFSILLib;
using AgServerILLib;
using AgServerILPDLLib;
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;

namespace SampleTcpServer
{

    public partial class MainForm : Form
    {
        public static MainForm Ins = null;
        public static double dbl_dummy_wvstart;
        public static double dbl_dummy_wvstop;
        public static double dbl_dummy_wvstep;
        public static string str_absolutePath;


        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);


        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);


        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetPrivateProfileInt(string section, string key, int nDefault, string filePath);




        ServerManager mManager = new ServerManager();

        AgServerFSILLib.EngineMgr myMgr;// = new EngineMgr();
        AgServerFSILLib.Engine myEng;

        AgServerILLib.EngineMgr myMgrSlow;// = new EngineMgr();
        AgServerILLib.Engine myEngSlow;

        AgServerILPDLLib.EngineMgr myMgrPDL;
        AgServerILPDLLib.Engine myEngPDL;

        bool IsStoped;
        List<clStructList> m_listScanList;
        List<ProtocolMapping> m_listProtocolMapping;

        List<string> m_listMF;// = new List<string>();
        List<string> m_listN77XXSN;//= new List<string>();

        List<clInstrumentChannelsMapping> m_listChannelMapping;

        //GOUMAX 扫描插值使用
        public List<double> m_list_GouMax_Interpolation;



        public readonly short Queue_Waiting_Scan = 101;
        public readonly short Queue_Doing_Scan = 201;
        public readonly short Queue_Completed_Scan = 301;
        public readonly short Monitor_Error = 4001;
        public readonly short WriteFile_Timeout_Error = 501;

        private static int m_nProtocolIndex = 0;
        private bool m_bExistAllofPrgress;

        /// <summary>
        /// SCPI_SCAN CLASSS
        /// </summary>
        /// 
        SCPI_Scan m_SCPI_Scan;
        GouMax_Scan m_GouMax_Scan;
        int m_n_SCPI_PDL_state;
        int m_Scan_Driver_type;
        int m_n_GAS_CELL_CHANNEL;
        int m_SCPI_Scan_Has8169;
        int m_Dummy_Environment_Sleep_Time;
        double m_dbl_SCPI_START_WL;
        double m_dbl_SCPI_STOP_WL;
        double m_dbl_SCPI_STEP_WL;
        string m_strServerSerialNumber;

        //在调用写文件时，拼出绝对路劲字串，为返回客户端使用。
        //string m_strAbsolutepath = "";
        string m_strABS_PATH = "";

        /// <summary>
        /// 监控扫描请求，完成请求线程
        /// </summary>
        private BackgroundWorker scanRequestBK;

        /// <summary>
        /// 监控扫描过程同步mutex
        /// </summary>
        private const string scanRequestMutexName = "scanRequstMutex";
        private static Mutex scanRequestMutex = null;

        /// <summary>
        /// write result files Background
        /// </summary>
        private BackgroundWorker writeFilesRequestBK;

        private const string writeFilesRequestMutexName = "writeFilesRequstMutex";
        private static Mutex writeFilesRequestMutex = null;

        private const string TLSLamdaZeroingMutexName = "TLSLamdaZeroingMutex";
        private static Mutex TLSLamdaZeroingMutex = null;
        private bool m_bTLSLamdaZeroing = false;


        private AgServerFSILLib.IOMRGraph graph;
        private AgServerILLib.IOMRGraph graphSlow;
        private AgServerILPDLLib.IOMRGraph graphPDL;
        private AgServerILPDLLib.IOMRGraph graphPDL_PDL;

        private List<double> m_listReference;

        struct SystemTime
        {
            [MarshalAs(UnmanagedType.U2)]
            internal ushort year; // 年
            [MarshalAs(UnmanagedType.U2)]
            internal ushort month; // 月
            [MarshalAs(UnmanagedType.U2)]
            internal ushort dayOfWeek; // 星期
            [MarshalAs(UnmanagedType.U2)]
            internal ushort day; // 日
            [MarshalAs(UnmanagedType.U2)]
            internal ushort hour; // 时
            [MarshalAs(UnmanagedType.U2)]
            internal ushort minute; // 分
            [MarshalAs(UnmanagedType.U2)]
            internal ushort second; // 秒
            [MarshalAs(UnmanagedType.U2)]
            internal ushort milliseconds; // 毫秒
        }

        struct ProtocolMapping
        {
            public string strFSTPStationKey;
            public string strComputerName;
            public string strMappingAddress;
            public string strEndpoint;
        }

        struct Gas_Cell_Calibrate
        {
            public double dblLeftWV;
            public double dblRightWV;
            public double dblCW;
        }

        private List<Gas_Cell_Calibrate> m_listWVCalibrate;
        private double m_nWVCalibrateOffset;
        private int  m_nWVCalibrateEnable;


        [DllImport("Kernel32.dll")]
        static extern bool GetLocalTime(ref SystemTime sysTime);

        [DllImport("Kernel32.dll")]
        static extern bool SetLocalTime(ref SystemTime lpSystemTime);

        [DllImport("Kernel32.dll")]
        static extern void GetSystemTime(ref SystemTime lpSystemTime);

        [DllImport("Kernel32.dll")]
        static extern bool SetSystemTime(ref SystemTime lpSystemTime);



        public MainForm()
        {
            m_bExistAllofPrgress = false;
            Ins = this;
            InitializeComponent();

        }

        protected override void OnLoad(EventArgs e)
        {
            //CheckForIllegalCrossThreadCalls = false;
            bool b1 = Convert.ToBoolean("true");

            //SetLocalTimeByStr("20180809121212");
            string strPath = "./config/systemconfig.ini";

            StringBuilder sb = new StringBuilder(128);

            m_Scan_Driver_type = GetPrivateProfileInt("SCAN_ENGINE_TYPE", "SCAN_ENGINE", 1, strPath);
            m_n_GAS_CELL_CHANNEL = GetPrivateProfileInt("SCAN_ENGINE_TYPE", "GAS_CELL_CHANNEL", 1, strPath);

            m_SCPI_Scan_Has8169 = GetPrivateProfileInt("SCAN_ENGINE_TYPE ", "SCPI_HAS_8169", 0, strPath);
            m_Dummy_Environment_Sleep_Time = GetPrivateProfileInt("SCAN_ENGINE_TYPE ", "Dummy_Environment_Sleep_Time", 100, strPath);


            GetPrivateProfileString("TLS_SCAN","START_WL" , "1310",sb,128, strPath);
            m_dbl_SCPI_START_WL = Convert.ToDouble(sb.ToString());
            GetPrivateProfileString("TLS_SCAN","STOP_WL" , "1410", sb, 128, strPath);
            m_dbl_SCPI_STOP_WL =Convert.ToDouble(sb.ToString());
            GetPrivateProfileString("TLS_SCAN", "STEP", "0", sb, 128, strPath);
            m_dbl_SCPI_STEP_WL = Convert.ToDouble(sb.ToString())*1000.000;

            GetPrivateProfileString("TLS_SCAN","DUMMY_START_WL" , "1310",sb,128, strPath);
            dbl_dummy_wvstart = Convert.ToDouble(sb.ToString());
            GetPrivateProfileString("TLS_SCAN", "DUMMY_STOP_WL", "1410", sb, 128, strPath);
            dbl_dummy_wvstop = Convert.ToDouble(sb.ToString());
            GetPrivateProfileString("TLS_SCAN", "DUMMY_STEP_WL", "5", sb, 128, strPath);
            dbl_dummy_wvstep = Convert.ToDouble(sb.ToString())*1000.000;



            GetPrivateProfileString("SCAN_ENGINE_TYPE ", "SERVER_SN", "1st.", sb, 128, strPath);
            m_strServerSerialNumber = sb.ToString();
            GetPrivateProfileString("SCAN_ENGINE_TYPE ", "ABSOLUTE_PATH", "127.0.0.1", sb, 128, strPath);
            m_strABS_PATH = sb.ToString();

            GetPrivateProfileString("SCAN_ENGINE_TYPE ", "GAS_CELL_OFFSET", "1E-11", sb, 128, strPath);
            m_nWVCalibrateOffset = Convert.ToDouble( sb.ToString());

            GetPrivateProfileString("SCAN_ENGINE_TYPE ", "GAS_CELL_ENABLE", "1", sb, 128, strPath);
            m_nWVCalibrateEnable = Convert.ToInt32(sb.ToString());

            m_listWVCalibrate = new List<Gas_Cell_Calibrate>();
            m_listWVCalibrate.Clear();
            LoadWVCalibateList();

            m_listProtocolMapping = new List<ProtocolMapping>();
            m_listProtocolMapping.Clear();
            LoadProtocolMapping();

            this.Text += "    " + m_strServerSerialNumber;

            if (m_Scan_Driver_type == 1)
            {
                InitEngineFS();
            }
            else if (m_Scan_Driver_type == 2)
            {
                m_SCPI_Scan = new SCPI_Scan();
                m_SCPI_Scan.m_dbl_SCPI_START_WL = m_dbl_SCPI_START_WL;
                m_SCPI_Scan.m_dbl_SCPI_STEP_WL = m_dbl_SCPI_STEP_WL / 1000.000;
                m_SCPI_Scan.m_dbl_SCPI_STOP_WL = m_dbl_SCPI_STOP_WL;
                m_SCPI_Scan.initStruct();
                if (m_SCPI_Scan_Has8169 == 0)
                {
                    m_SCPI_Scan.SCPI_OpenDevice(false);
                }
                else if (m_SCPI_Scan_Has8169 == 1)
                {
                    m_SCPI_Scan.SCPI_OpenDevice(true);
                }
            }
            else if (m_Scan_Driver_type == 3)
            {
                InitEngineSlowIL();
            }
            else if (m_Scan_Driver_type == 4)
            {
                InitEnginePDL();
            }
            else if (m_Scan_Driver_type == 5)
            {
                strPath = "./config/GoumaxConfig.ini";
                GetPrivateProfileString("GENERAL", "START_WV", "1250", sb, 128, strPath);
                m_dbl_SCPI_START_WL = Convert.ToDouble(sb.ToString());
                GetPrivateProfileString("GENERAL", "STOP_WV", "1650", sb, 128, strPath);
                m_dbl_SCPI_STOP_WL = Convert.ToDouble(sb.ToString());
                GetPrivateProfileString("GENERAL", "STEP_SIZE", "20", sb, 128, strPath);
                m_dbl_SCPI_STEP_WL = Convert.ToDouble(sb.ToString()) ;


                m_GouMax_Scan = new GouMax_Scan();
                m_GouMax_Scan.initStruct();
                m_GouMax_Scan.GouMax_OpenDevice();
            }
            else if(m_Scan_Driver_type == 44)
            {
                //dummy environment,do nothing;
            }

            m_listScanList = new List<clStructList>();
            m_listChannelMapping = new List<clInstrumentChannelsMapping>();


            m_listReference = new List<double>();
            m_listReference.Clear();

            m_listMF = new List<string>();
            m_listN77XXSN = new List<string>();


            m_list_GouMax_Interpolation = new List<double>();
            m_list_GouMax_Interpolation.Clear();

            btnStopServer.Enabled = false;


            scanRequestMutex = new Mutex(false, scanRequestMutexName + m_Scan_Driver_type.ToString());
            scanRequestBK = new BackgroundWorker();
            scanRequestBK.DoWork += scanRequestBK_DoWork;
            scanRequestBK.WorkerSupportsCancellation = true;
            scanRequestBK.WorkerReportsProgress = true;
            scanRequestBK.RunWorkerAsync();



            writeFilesRequestMutex = new Mutex(false, writeFilesRequestMutexName + m_Scan_Driver_type.ToString());
            writeFilesRequestBK = new BackgroundWorker();
            writeFilesRequestBK.DoWork += writeFilesRequestBK_DoWork;
            writeFilesRequestBK.ProgressChanged += writeFilesRequestBK_Progress;
            writeFilesRequestBK.WorkerSupportsCancellation = true;
            writeFilesRequestBK.WorkerReportsProgress = true;
            //scanRequestBK.RunWorkerAsync();

            TLSLamdaZeroingMutex = new Mutex(false, TLSLamdaZeroingMutexName + m_Scan_Driver_type.ToString());


            if (m_Scan_Driver_type == 1)
            {
                SaveXMLChannelList(myEng.ChannelSetup);

                //开机先做一次REFERENCE;
                double DBLPWMSensitivity = myEng.PWMSensitivity;

                myEng.ChannelSetup = XMLChannelSetup(myEng.ChannelSetup);
                myEng.PWMSensitivity = 0;

                StartupDoReference();
            }
            else if (m_Scan_Driver_type == 3)
            {
                SaveXMLChannelListSlow(myEngSlow.ChannelSetup);


                //开机先做一次REFERENCE;
                double DBLPWMSensitivity = myEngSlow.PWMSensitivity;

                //myEngSlow.ChannelSetup = XMLChannelSetup(myEngSlow.ChannelSetup);
                myEngSlow.PWMSensitivity = 0;

                StartupDoReferenceSlow();
            }
            else if (m_Scan_Driver_type == 4)
            {
                //这个应该没有用，先屏蔽。后续确认再处理
                SaveXMLChannelListPDL(myEngPDL.ChannelSetup);


                //开机先做一次REFERENCE;

                double DBLPWMSensitivity = myEngPDL.PWMSensitivity;

                //myEngSlow.ChannelSetup = XMLChannelSetup(myEngSlow.ChannelSetup);
                //myEngPDL.PWMSensitivity = 0;

                //StartupDoReferencePDL();
            }
            else if (m_Scan_Driver_type == 5)
            {
                //goumax 开机先做一次REFERENCE;
                StartupDoReferenceGOUMAX();
            }
            else if (m_Scan_Driver_type == 44)
            {
                //dummy environment ,do nothing;

                //StartupDoReferencePDL();
            }

            LoadChannelMapping();
            //LoadProtocolMapping();

            IsStoped = false;

            base.OnLoad(e);
        }



        //private void scanRequestBK_Progress(object sender, ProgressChangedEventArgs e)
        //{
        //    if (scanRequestMutex.WaitOne())
        //    {
        //        int curIndex = 0;
        //        while (m_listScanList.Count > curIndex)
        //        {
        //            clStructList nj = m_listScanList[curIndex];
        //            if (nj.shStatus == 201)
        //            {
        //                ListViewItem lvi = ListView1.Items[curIndex];
        //                if (lvi.SubItems[9].Text == nj.strGUID)
        //                {
        //                    FeedBackMSG();
        //                    lock (ListView1)
        //                    {
        //                        ListView1.Items.RemoveAt(lvi.Index);
        //                    }

        //                    lock (m_listScanList)
        //                    {
        //                        m_listScanList.Remove(nj);
        //                    }

        //                    break;
        //                }
        //                else
        //                {
        //                    //如果删除行，则序号不需要加1
        //                    curIndex++;
        //                }
        //            }
        //        }
        //        scanRequestMutex.ReleaseMutex();
        //    }
        //}

        //private void scanRequestBK_Progress(object sender, ProgressChangedEventArgs e)
        //{
        //    if (scanRequestMutex.WaitOne())
        //    {
        //        int curIndex = 0;


        //        for (int i = m_listScanList.Count - 1; i >= 0; i--)
        //        {
        //            clStructList nj = m_listScanList[i];
        //            if (nj.shStatus == 201)
        //            {


        //                FeedBackMSG(nj);
        //                lock (ListView1)
        //                {
        //                    ListView1.Items.RemoveAt(i);
        //                }

        //                lock (m_listScanList)
        //                {
        //                    m_listScanList.RemoveAt(i);
        //                }
        //            }
        //        }

        //        scanRequestMutex.ReleaseMutex();

        //    }
        //}
        private void scanRequestBK_DoWork(object sender, DoWorkEventArgs e)
        {
            //int nCountItem = 0;
            bool bDoTest;
            int i = 0;

            while (true)
            {
                if (m_bExistAllofPrgress)
                    break;

                Thread.Sleep(100);
                bDoTest = false;
                if(scanRequestMutex.WaitOne())
                {
                    //LogHelper.WriteLog(typeof(MainForm), "scanRequestBK_DoWork() scanRequestMutex.WaitOne()");
                    //nCountItem = m_listScanList.Count;
                    string strsplice = "";
                    string strsplicePWMChannel="";


                    for ( i =0 ; i <m_listScanList.Count ; i++)
                    {
                        clStructList nj = m_listScanList[i];
                        if (nj.shStatus == Queue_Waiting_Scan)
                        {
                            //Debug.WriteLine("check 101  ={0}", System.DateTime.Now.Ticks );
                            //string[] split = nj.strIndexOfPWMChannels.Split(',');
                            strsplice += ";" + nj.strIndexOfPWMChannels;
                            strsplicePWMChannel += ";" + nj.nSplitPWMChannels.ToString(); ;

                            nj.shStatus = Queue_Doing_Scan;
                            bDoTest = true;
                        }

                    }
                    if (m_Scan_Driver_type == 5)
                    {
                        for (i = 0; i < m_GouMax_Scan.m_sttGoumaxData.m_byChannelValide.Length; i++)
                        {
                            m_GouMax_Scan.m_sttGoumaxData.m_byChannelValide[i] = 0;
                        }

                        
                        string[] split = strsplicePWMChannel.Split(';');
                        HashSet<string> hsChannels = new HashSet<string>();
                        foreach (string a in split)
                        {
                            if (a != "")
                            {
                                hsChannels.Add(a);
                                int nCh = Convert.ToByte(a);
                                m_GouMax_Scan.m_sttGoumaxData.m_byChannelValide[nCh - 1] = 1;
                            }

                        }
                    }
                    //LogHelper.WriteLog(typeof(MainForm), "scanRequestBK_DoWork() scanRequestMutex.ReleaseMutex()");
                }

                scanRequestMutex.ReleaseMutex();

                if (bDoTest)
                {
                    long lnow= System.DateTime.Now.Ticks;

                    start();
                }

                if (IsStoped)
                    return;
            }
        }


        private void writeFilesRequestBK_Progress(object sender, ProgressChangedEventArgs e)
        {
            if (scanRequestMutex.WaitOne())
            {
                LogHelper.WriteLog(typeof(MainForm), "writeFilesRequestBK_Progress() scanRequestMutex.WaitOne()");


                HashSet<string> hash = new HashSet<string>();

                /*
                string strProtocol = myEng.ProtocolText;
                int nProMax = myEng.ProtocolMax;
                int nProMin = myEng.ProtocolMin;
                */
                string strP1 = "";
                if (m_Scan_Driver_type == 1)
                {
                    strP1= myEng.GetProtocolTextAt(m_nProtocolIndex - 1, m_nProtocolIndex);
                }
                else if (m_Scan_Driver_type == 3)
                {
                    strP1 = myEngSlow.GetProtocolTextAt(m_nProtocolIndex - 1, m_nProtocolIndex);
                }
                else if (m_Scan_Driver_type == 4)
                {
                    strP1 = myEngPDL.GetProtocolTextAt(m_nProtocolIndex - 1, m_nProtocolIndex);
                }
                else
                {
                    strP1 = "Measurement successful";
                }

                for (int i = m_listScanList.Count - 1; i >= 0; i--)
                {
                    clStructList nj = m_listScanList[i];
                    if (nj.shStatus == Queue_Completed_Scan)
                    {
                        switch(m_Scan_Driver_type)
                        {
                            case 1:case 2:case 3:case 5:
                                if (strP1.IndexOf("Measurement successful") == -1)
                                {
                                    FeedBackErrorMSG(strP1);
                                    LogHelper.WriteLog(typeof(MainForm), string.Format("writeFilesRequestBK_Progress()  feedback {0}", strP1));

                                }
                                else if (e.ProgressPercentage == 0)
                                {
                                    FeedBackMSG(nj, hash);
                                }
                                else if (e.ProgressPercentage == Monitor_Error)
                                {
                                    FeedBackErrorMSG("mornitor 变化超过1, 请检查光路错误.");
                                }
                                else if (e.ProgressPercentage == WriteFile_Timeout_Error)
                                {
                                    //scanRequestBK.CancelAsync();

                                    FeedBackErrorMSG("写文件超时错误.");
                                    LogHelper.WriteLog(typeof(MainForm), "写文件超时错误.");
                                    //myEng.DeActivate();
                                    //myEng.Activate();
                                    
                                    //scanRequestMutex.ReleaseMutex();

                                    //scanRequestBK.RunWorkerAsync();
                                    //return;

                                }
                                break;
                            case 4:
                                if (strP1.IndexOf("Evaluation successful") == -1)
                                {
                                    FeedBackErrorMSG(strP1);
                                    LogHelper.WriteLog(typeof(MainForm), string.Format("writeFilesRequestBK_Progress()  feedback {0}", strP1));

                                }
                                else if (e.ProgressPercentage == 0)
                                {
                                    FeedBackMSG(nj, hash);
                                }
                                else if (e.ProgressPercentage == Monitor_Error)
                                {
                                    FeedBackErrorMSG("mornitor 变化超过1, 请检查光路错误.");
                                }
                                break;

                            case 44:
                                FeedBackMSG(nj, hash);
                                break;
                            default:
                                //FeedBackErrorMSG("writeFilesRequestBK_Progress() 未知的错误.");
                                LogHelper.WriteLog(typeof(MainForm), "writeFilesRequestBK_Progress() 未知的错误.");
                                break;
                        }


                        //lock (ListView1)
                        {
                            //ListView1.Invoke(new InvokeDelegate (ListView_Remove_Item) );
                            //ListView1.Invoke(ListView_Remove_Item, i);
                            //ListView1.Items.RemoveAt(i);
                            ListView_Remove_Item(nj.strSocketID,nj.strGUID);
                        }

                        lock (m_listScanList)
                        {
                            m_listScanList.RemoveAt(i);
                        }
                    }
                }

                scanRequestMutex.ReleaseMutex();
                LogHelper.WriteLog(typeof(MainForm), "writeFilesRequestBK_Progress()  scanRequestMutex.ReleaseMutex()");
            }
        }


        //事件回调

        delegate void asynShowDataStatus(int nStatus);
        public void ShowDataStatus(int nStatus)
        {
            if (InvokeRequired)
            {
                Invoke(new asynShowDataStatus(delegate (int _Count)
                {
                    textBox1.Text =  _Count.ToString();
                }), nStatus);
            }
            else
            {
                textBox1.Text =  nStatus.ToString();
            }
        }
        private void DoRefernce()
        {
            Int64[] m = new Int64[48];
            m[0] = 1;
            m[1] = 1;
            m[2] = 1;
            m[3] = 1;
            m[4] = 1;
            m[5] = 1;
            Int32[] n = new Int32[48];
            for(int i=0;i<48;i++)
            {
                n[i] =1;
            }
            string strconfig = myEng.Configuration;
            myEng.StartCustomReference(n);
            int nPwmCount =myEng.PWMCount;
            myEng.CopyReferenceChannel(0, n);
            myEng.SaveReference("./reference/FSTPRef.omr", 1);
            //myEng.CopyReferenceChannel(m);
        }
        private void writeFilesRequestBK_DoWork(object sender, DoWorkEventArgs e)
        {

            long lnow = System.DateTime.Now.Ticks;

            if (writeFilesRequestMutex.WaitOne())
            {

                LogHelper.WriteLog(typeof(MainForm), "write file start !");
                int error = 0;

                if (m_Scan_Driver_type==1)
                {
                    Array y = graph.YData;
                    

                    int noChannels = graph.noChannels;
                    lnow = System.DateTime.Now.Ticks;
                    double dbStart = myEng.WavelengthStart;
                    double dbStep = myEng.WavelengthStep * 1e-3;
                    double dbStop = myEng.WavelengthStop;
                    
                    error = WriteFile(y, noChannels, dbStart, dbStep, dbStop);
                }
                else if(m_Scan_Driver_type == 2)
                {
                    error = WriteFile(m_SCPI_Scan.m_xxx,m_dbl_SCPI_START_WL,m_dbl_SCPI_STEP_WL/1000.000,m_dbl_SCPI_STOP_WL,m_n_SCPI_PDL_state);
                }
                else if (m_Scan_Driver_type == 3)
                {
                    Array y = graphSlow.YData;


                    int noChannels = graphSlow.noChannels;
                    lnow = System.DateTime.Now.Ticks;
                    double dbStart = myEngSlow.WavelengthStart*1e9;
                    double dbStep = myEngSlow.WavelengthStep * 1e9;
                    double dbStop = myEngSlow.WavelengthStop * 1e9;
                    error = WriteFile(y, noChannels, dbStart, dbStep, dbStop);
                }
                else if (m_Scan_Driver_type == 4)
                {
                    Array y = graphPDL.YData;


                    int noChannels = graphPDL.noChannels;
                    lnow = System.DateTime.Now.Ticks;
                    double dbStart = graphPDL.xStart * 1e9;
                    double dbStep = graphPDL.xStep * 1e9;
                    
                    double dbStop = graphPDL.xStop * 1e9;
                    
                    error = WriteFilePDL(y, noChannels, dbStart, dbStep, dbStop);
                }
                else if (m_Scan_Driver_type == 5)
                {
                    error = WriteGouMaxFile(m_GouMax_Scan.m_sttGoumaxData, m_dbl_SCPI_START_WL, 0.001, m_dbl_SCPI_STOP_WL);
                }
                else if (m_Scan_Driver_type == 44)
                {


                    int noChannels= 8;
                    lnow = System.DateTime.Now.Ticks;
                    double dbStart = dbl_dummy_wvstart;
                    double dbStep = dbl_dummy_wvstep*0.001;
                    double dbStop = dbl_dummy_wvstop;
                    error = 1;
                    error = WriteFileDummy( noChannels, dbStart, dbStep, dbStop);
                }

                LogHelper.WriteLog(typeof(MainForm), string.Format("WriteFile time = {0}", (System.DateTime.Now.Ticks - lnow) / 10000));
                if (error == 1)
                {
                    writeFilesRequestBK.ReportProgress(0);
                }
                else
                {
                    writeFilesRequestBK.ReportProgress(error);
                }
                
                writeFilesRequestMutex.ReleaseMutex();
            }
            
        }

        void ProcessScanList()
        {
            int nCountItem = 0;
            bool bDoTest;
            while (true)
            {
                Thread.Sleep(100);
                bDoTest = false;
                lock (m_listScanList)
                {
                    //nCountItem = m_listScanList.Count;
                    foreach(clStructList nj in m_listScanList)
                    {
                        if(nj.shStatus== Queue_Waiting_Scan)
                        {
                            nj.shStatus = Queue_Doing_Scan;
                            bDoTest = true;
                        }

                    }
                }

                if (bDoTest)
                {
                    //start();
                }

                //nCountItem = ListView1.Items.Count-1;
                HashSet<string> hash = new HashSet<string>();
                foreach (clStructList nj in m_listScanList)
                {
                    if (nj.shStatus == Queue_Doing_Scan)
                    {
                        foreach (ListViewItem lvi in ListView1.Items)
                        {
                            if(lvi.SubItems[8].Text == nj.strGUID)
                            {
                                FeedBackMSG(nj,hash);
                                lock(ListView1)
                                {
                                    ListView1.Items.RemoveAt(lvi.Index);
                                }
                                
                                lock(m_listScanList)
                                {
                                    m_listScanList.Remove(nj);
                                }
                                
                                break;
                            }
                        }
                    }

                }

                if (IsStoped)
                    return;
            }
        }

        public static bool SetLocalTimeByStr(string timestr)
        {
            bool flag = false;
            SystemTime sysTime = new SystemTime();

            string SysTime = timestr.Trim();   //此步骤多余，为方便程序而用直接用timestr即可
            sysTime.year = Convert.ToUInt16(SysTime.Substring(0, 4));
            sysTime.month = Convert.ToUInt16(SysTime.Substring(4, 2));
            sysTime.day = Convert.ToUInt16(SysTime.Substring(6, 2));
            sysTime.hour = Convert.ToUInt16(SysTime.Substring(8, 2));
            sysTime.minute = Convert.ToUInt16(SysTime.Substring(10, 2));
            sysTime.second = Convert.ToUInt16(SysTime.Substring(12, 2));
            //注意：
            //结构体的wDayOfWeek属性一般不用赋值，函数会自动计算，写了如果不对应反而会出错
            //wMiliseconds属性默认值为一，可以赋值
            try
            {
                flag = SetLocalTime(ref sysTime);
            }
            //由于不是C#本身的函数，很多异常无法捕获
            //函数执行成功则返回true，函数执行失败返回false
            //经常不返回异常，不提示错误，但是函数返回false，给查找错误带来了一定的困难
            catch (Exception ex1)
            {
                Console.WriteLine("SetLocalTime函数执行异常" + ex1.Message);
            }

            return flag;
        }


        //启动服务器 
        private void btnStartServer_Click(object sender, System.EventArgs e)
        {
            ListView.Items.Clear();
            string strPath = "./config/systemconfig.ini";
            StringBuilder sb = new StringBuilder(32);
            GetPrivateProfileString("SCAN_ENGINE_TYPE", "SERVER_PORT", "9988",sb,32, strPath);
            ServerPort.Text = sb.ToString();
            int port;
            if (int.TryParse(ServerPort.Text, out port))
                mManager.Init(port);
            else
                mManager.Init();

            mManager.Start();
            btnStartServer.Enabled = false;
            btnStopServer.Enabled = true;
            ServerState.Image = Properties.Resources.Normal;
        }

        //停止服务器
        private void btnStopServer_Click(object sender, System.EventArgs e)
        {
            mManager.Stop();
            btnStartServer.Enabled = true;
            btnStopServer.Enabled = false;
            ServerState.Image = Properties.Resources.Close;
        }



        //事件回调
        delegate void AsynUpdateCount(int pCount);
        public void UpdateCount(int pCount)
        {
            if (InvokeRequired)
            {
                Invoke(new AsynUpdateCount(delegate (int _Count)
                {
                    ConnectCount.Text = "当前链接数 :" + _Count;
                }), pCount);
            }
            else
            {
                ConnectCount.Text = "当前链接数 :" + pCount;
            }
        }

        delegate void ListViewRemoveItemHandler(string  strSocketID,string strGUID);
        void ListView_Remove_Item(string strSocketID, string strGUID)
        {
            if(InvokeRequired)
            {
                Invoke(new ListViewRemoveItemHandler(delegate (string _strSocketID, string _strGUID)
                {
                    for(int ii=ListView1.Items.Count-1;ii>=0;ii--)
                    {
                        ListViewItem item = ListView1.Items[ii];
                        if((item.SubItems[0].Text == _strSocketID) &&(item.SubItems[1].Text == _strGUID))
                        {
                            ListView1.Items.RemoveAt(ii);
                        }
                    }
                    
                }),   strSocketID,  strGUID);
            }
            else
            {
                for (int ii = ListView1.Items.Count - 1; ii >= 0; ii--)
                {
                    ListViewItem item = ListView1.Items[ii];
                    if ((item.SubItems[0].Text == strSocketID) && (item.SubItems[1].Text == strGUID))
                    {
                        ListView1.Items.RemoveAt(ii);
                    }
                }
            }
            
        }


        delegate void ListViewAddItemHandler(ListViewItem item);
        void ListView_Add_Item(ListViewItem item)
        {
            if (InvokeRequired)
            {
                Invoke(new ListViewAddItemHandler(delegate (ListViewItem _item)
                {
                    ListView1.Items.Add(_item);
                }), item);
            }
            else
            {
                ListView1.Items.Add(item);
            }

        }


        delegate void AsynOnConnected(TCPClientSession pSession);
        public void OnConnected(TCPClientSession pSession)
        {
            if (InvokeRequired)
            {
                Invoke(new AsynOnConnected(delegate (TCPClientSession _Session)
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = _Session.ClientID.ToString();
                    item.SubItems.Add("正常");
                    item.SubItems.Add("0");
                    item.SubItems.Add("0");
                    item.SubItems.Add("0");
                    item.SubItems.Add("0");
                    ListView.Items.Add(item);
                    _Session.m_ListItem = item;
                }), pSession);
            }
            else
            {
                ListViewItem item = new ListViewItem();
                item.Text = pSession.ClientID.ToString();
                item.SubItems.Add("正常");
                item.SubItems.Add("0");
                item.SubItems.Add("0");
                item.SubItems.Add("0");
                item.SubItems.Add("0");
                ListView.Items.Add(item);
                pSession.m_ListItem = item;
            }
        }

        delegate void AsynOnDisconnected(TCPClientSession pSession);
        public void OnDisconnected(TCPClientSession pSession)
        {
            if (InvokeRequired)
            {
                Invoke(new AsynOnDisconnected(delegate (TCPClientSession _Session)
                {
                    foreach (ListViewItem item in ListView.Items)
                    {
                        if (item.SubItems[0].Text == _Session.ClientID.ToString())
                        {
                            item.SubItems[1].Text = "断开";
                        }
                    }
                }), pSession);
            }
            else
            {
                foreach (ListViewItem item in ListView.Items)
                {
                    if (item.SubItems[0].Text == pSession.ClientID.ToString())
                    {
                        item.SubItems[1].Text = "断开";
                    }
                }
            }
        }

        delegate void AsynUpdateSendInfo(ListViewItem pItem, int pCount, long pSzie);
        public void UpdateSendInfo(ListViewItem pItem, int pCount, long pSzie)
        {
            if (InvokeRequired)
            {
                Invoke(new AsynUpdateSendInfo(delegate (ListViewItem _Item, int _Count, long _Szie)
                {

                    _Item.SubItems[2].Text = _Count + "";
                    _Item.SubItems[3].Text = _Szie + "";
                }), pItem, pCount, pSzie);
            }
            else
            {
                pItem.SubItems[2].Text = pCount + "";
                pItem.SubItems[3].Text = pSzie + "";
            }
        }


        delegate void AsynUpdateReceivedData(byte[] pData, long pSzie, EndPoint remoteEndPoint);
        public void UpdateReceivedData( byte[] pData, long pSzie, EndPoint remoteEndPoint)
        {
            string str1 = Encoding.Default.GetString(pData);
            if (InvokeRequired)
            {
                Invoke(new AsynUpdateReceivedData(delegate (byte[] _Count, long _Szie, EndPoint _EndPoint)
                {

                    JObject jo;

                    if (CHK_LOG.Checked == true)
                    {
                        richTextBox1.Text = richTextBox1.Text + str1 + System.Environment.NewLine;
                    }
                    LogHelper.WriteLog(typeof(MainForm), "RECEIVED:" + str1);
                    try
                    {
                        jo = (JObject)JsonConvert.DeserializeObject(str1);
                    }
                    catch(System.Exception ex)
                    {

                        string strReturn = "JSON PARSE ERROR!";
                        foreach (var item1 in mManager.m_AllClients)
                        {

                            if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                            {

                                CResultFeedBack AA = new CResultFeedBack();
                                AA.SocketGUID = "None";
                                AA.IndexOfPWMChannels = "None";
                                AA.Result = "0";
                                AA.ErrorDesc = "JSON PARSE ERROR!";
                                AA.SocketID = "None";
                                AA.StartWaveLength = "None";
                                AA.StopWaveLength = "None";
                                AA.ScanStep = "None";


                                string strJSON = JsonConvert.SerializeObject(AA);
                                mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                            }
                        }
                        LogHelper.WriteLog(typeof(MainForm), "JSON PARSE ERROR:" + str1); 
                        return;
                    }


                    // 收到HEX 9 0 0 0， (JObject)JsonConvert.DeserializeObject(str1), 不会被TRY CATCH
                    if(jo==null)
                    {
                        string strReturn = "JSON PARSE ERROR!";
                        foreach (var item1 in mManager.m_AllClients)
                        {

                            if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                            {

                                CResultFeedBack AA = new CResultFeedBack();
                                AA.SocketGUID = "None";
                                AA.IndexOfPWMChannels = "None";
                                AA.Result = "0";
                                AA.ErrorDesc = "JSON PARSE ERROR!";
                                AA.SocketID = "None";
                                AA.StartWaveLength = "None";
                                AA.StopWaveLength = "None";
                                AA.ScanStep = "None";


                                string strJSON = JsonConvert.SerializeObject(AA);
                                mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                            }
                        }
                        LogHelper.WriteLog(typeof(MainForm), "JSON PARSE ERROR:" + str1);
                        return;
                    }

                    if (jo.Count!=11)
                    {
                        string strValue = jo.Last.ToString();
                        if(strValue.IndexOf("CheckStatus")>-1)
                        {

                            CCheckStatusFeedBack AA = new CCheckStatusFeedBack();
                            AA.SocketGUID = jo["SocketGUID"].ToString();
                            AA.SocketID = jo["SocketID"].ToString();

                            if (Chk_ScanListStatus_Doing_Scan())
                            {
                                AA.CheckStatus = "1";

                            }
                            else
                            {
                                AA.CheckStatus = "0";
                            }

                            foreach (var item1 in mManager.m_AllClients)
                            {

                                if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                                {

                                    string strJSON = JsonConvert.SerializeObject(AA);
                                    mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                                }
                            }
                            LogHelper.WriteLog(typeof(MainForm), string.Format("Check scan status is = {0}", AA.CheckStatus));
                            return;
                        }
           
                    }


                    //{"TaskType":"Register","FSTPStationKey":"C:\Users\wjhuang08\MyDocument\FSTP","ComputerName":"ZHUHL5CG7381MKN","SocketGUID":"105311"}
                    if (jo.Count==4)
                    {
                        if (jo["TaskType"].ToString() == "Register")
                        {
                            string strkey = "", strcmpname = "", strEndpoint = "";
                            strkey = jo["FSTPStationKey"].ToString();
                            strcmpname = jo["ComputerName"].ToString();
                            strEndpoint = remoteEndPoint.ToString();
                            bool bFinded = false;
                            for (int nItem = 0; nItem < m_listProtocolMapping.Count; nItem++)
                            {
                                if (m_listProtocolMapping[nItem].strFSTPStationKey == strkey && m_listProtocolMapping[nItem].strComputerName == strcmpname)
                                {
                                    bFinded = true;
                                    var CItem = m_listProtocolMapping[nItem];
                                    CItem.strEndpoint = remoteEndPoint.ToString();
                                    //var cItem = m_listProtocolMapping.Where();

                                    m_listProtocolMapping[nItem] = CItem;
                                    break;
                                }
                            }

                            foreach (var item1 in mManager.m_AllClients)
                            {

                                if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                                {
                                    CRegisterFeedBack AA = new CRegisterFeedBack();
                                    AA.TaskType = "Register";
                                    AA.SocketGUID = jo["SocketGUID"].ToString();
                                    if (bFinded)
                                    {
                                        AA.Result = "1";
                                    }
                                    else
                                    {
                                        AA.Result = "0";
                                    }
                                    string strJSON = JsonConvert.SerializeObject(AA);
                                    mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                                    return;
                                }
                            }
                        }
                    }


                    //时要求扫描的数据，先配对 jo["IndexOfPWMChannels"].ToString();
                    //jo["IndexOfPWMChannels"] = "";
                    //foreach (var RegItem in m_listProtocolMapping)
                    //{
                    //    if(RegItem.strEndpoint == remoteEndPoint.ToString())
                    //    {
                    //        jo["IndexOfPWMChannels"] = RegItem.strMappingAddress;
                    //        break;
                    //    }
                    //}


                    if (jo["StartWaveLength"].ToString()=="" || jo["StopWaveLength"].ToString() == "" || jo["ScanStep"].ToString() == "" )
                    {
                        foreach (var item1 in mManager.m_AllClients)
                        {

                            if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                            {

                                CResultFeedBack AA = new CResultFeedBack();
                                AA.SocketGUID = jo["SocketGUID"].ToString();
                                AA.IndexOfPWMChannels = jo["IndexOfPWMChannels"].ToString();
                                AA.Result = "0";
                                AA.ErrorDesc = "request StartWaveLength ,StopWaveLength ,step can't be null, please check wavelength setting!";// "StartWaveLength error or StopWaveLength error, please check wavelength!";
                                AA.SocketID = jo["SocketID"].ToString();
                                AA.StartWaveLength = jo["StartWaveLength"].ToString();
                                AA.StopWaveLength = jo["StopWaveLength"].ToString();
                                AA.ScanStep = jo["ScanStep"].ToString();


                                string strJSON = JsonConvert.SerializeObject(AA);
                                mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                            }
                        }
                        LogHelper.WriteLog(typeof(MainForm), "request StartWaveLength, StopWaveLength, step can't be null, please check wavelength setting!");
                        return;
                    }

                    //CHECK START WAVELENGTH AND  STOP  WAVELENGTH
                    double dbStart_1 =Convert.ToDouble(jo["StartWaveLength"].ToString());
                    double dbStop_1 = Convert.ToDouble(jo["StopWaveLength"].ToString());
                    double dbStep_1 = 0;
                    double dbStepMod = 0;
                    double dbCmprStart=0, dbCmprStop=0,dbCmprStep=0;
                    string strStep = jo["ScanStep"].ToString();

                    if(strStep.IndexOf("pm")>=0)
                    {
                        dbStep_1 =Convert.ToDouble(strStep.Substring(0,strStep.IndexOf("pm")));
                    }
                    else if (strStep.IndexOf("nm") >= 0)
                    {
                        dbStep_1 = Convert.ToDouble(strStep.Substring(0,strStep.IndexOf("nm")))*1000;
                    }
                    else
                    {
                        dbStep_1 = Convert.ToDouble(strStep) ;
                    }


                    //m_dbl_SCPI_START_WL
                    //m_dbl_SCPI_STOP_WL
                    if (m_Scan_Driver_type==1)
                    {
                        dbStepMod = dbStep_1 % myEng.WavelengthStep;
                        dbCmprStart = myEng.WavelengthStart;
                        dbCmprStop = myEng.WavelengthStop;
                        dbCmprStep = myEng.WavelengthStep;
                    }
                    else if (m_Scan_Driver_type == 2)
                    {
                        dbStepMod = dbStep_1 % m_dbl_SCPI_STEP_WL;
                        dbCmprStart = m_dbl_SCPI_START_WL;
                        dbCmprStop = m_dbl_SCPI_STOP_WL;
                        dbCmprStep =m_dbl_SCPI_STEP_WL;
                    }
                    else if (m_Scan_Driver_type == 3)
                    {
                        dbStepMod = dbStep_1 % myEng.WavelengthStep;
                        dbCmprStart = myEngSlow.WavelengthStart;
                        dbCmprStop = myEngSlow.WavelengthStop;
                        dbCmprStep = myEngSlow.WavelengthStep;
                    }
                    else if (m_Scan_Driver_type == 4)
                    {
                        
                        double sweep = myEngPDL.SweepRate;

                        //Sets or gets the sweep rate in nm / s.Sweep rate v in nm / s and step size s in pm are related by
                        //0.3 x v[nm / s] = s[pm](e.g. 20 nm / s yields 6 pm step size)
                        dbCmprStep = myEngPDL.SweepRate * 0.3;
                        dbCmprStep = 1;
                        dbStepMod = dbStep_1 % dbCmprStep;
                        dbCmprStart = myEngPDL.WavelengthStart;
                        dbCmprStop = myEngPDL.WavelengthStop;

                    }
                    else if (m_Scan_Driver_type == 5)
                    {
                        dbStepMod = 0;
                        dbCmprStart = m_dbl_SCPI_START_WL;
                        dbCmprStop = m_dbl_SCPI_STOP_WL;
                        dbCmprStep = 10;
                    }

                    else if (m_Scan_Driver_type == 44)
                    {
                        dbCmprStep = dbl_dummy_wvstep;
                        dbStepMod = dbStep_1 % dbCmprStep;
                        dbCmprStart = dbl_dummy_wvstart;
                        dbCmprStop = dbl_dummy_wvstop;
                    }
                    else
                    {
                        dbCmprStep = dbl_dummy_wvstep;
                        dbStepMod = dbStep_1 % dbCmprStep;
                        dbCmprStart = dbl_dummy_wvstart;
                        dbCmprStop = dbl_dummy_wvstop;
                    }

                    if ((dbStart_1< dbCmprStart)  || (dbStop_1 > dbCmprStop)  ||(dbStepMod != 0) ||( dbStep_1 == 0.0))
                    {
                        foreach (var item1 in mManager.m_AllClients)
                        {

                            if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                            {

                                CResultFeedBack AA = new CResultFeedBack();
                                AA.SocketGUID = jo["SocketGUID"].ToString();
                                AA.IndexOfPWMChannels = jo["IndexOfPWMChannels"].ToString();
                                AA.Result = "0";
                                AA.ErrorDesc = string.Format("TLS StartWaveLength is {0} nm,StopWaveLength is {1} nm,Step is {2} pm;request StartWaveLength is {3} nm,StopWaveLength is {4} nm,step is {5} pm, please check wavelength setting!", dbCmprStart, dbCmprStop, dbCmprStep, jo["StartWaveLength"].ToString(), jo["StopWaveLength"].ToString(), jo["ScanStep"].ToString());// "StartWaveLength error or StopWaveLength error, please check wavelength!";
                                AA.SocketID = jo["SocketID"].ToString();
                                AA.StartWaveLength = jo["StartWaveLength"].ToString();
                                AA.StopWaveLength = jo["StopWaveLength"].ToString();
                                AA.ScanStep = dbStep_1.ToString();


                                string strJSON = JsonConvert.SerializeObject(AA);
                                mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                            }
                        }
                        LogHelper.WriteLog(typeof(MainForm),string.Format("TLS StartWaveLength is {0} nm,StopWaveLength is {1} nm,Step is {2} pm;request StartWaveLength is {3} nm,StopWaveLength is {4} nm,step is {5} pm, please check wavelength setting!", dbCmprStart, dbCmprStop, dbCmprStep, jo["StartWaveLength"].ToString(), jo["StopWaveLength"].ToString(),jo["ScanStep"].ToString()));
                        return;
                    }



                    //check channel exist;
                    if (m_Scan_Driver_type != 44)
                    {
                        if(jo["IndexOfPWMChannels"].ToString().IndexOf('_') > -1)
                        {
                            if (!ChkChannelExist(jo["IndexOfPWMChannels"].ToString()))
                            {
                                foreach (var item1 in mManager.m_AllClients)
                                {

                                    if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                                    {

                                        CResultFeedBack AA = new CResultFeedBack();
                                        AA.SocketGUID = jo["SocketGUID"].ToString();
                                        AA.IndexOfPWMChannels = jo["IndexOfPWMChannels"].ToString();
                                        AA.Result = "0";
                                        AA.ErrorDesc = "N774x sn error or Slot number error";
                                        AA.SocketID = jo["SocketID"].ToString();
                                        AA.StartWaveLength = jo["StartWaveLength"].ToString();
                                        AA.StopWaveLength = jo["StopWaveLength"].ToString();
                                        AA.ScanStep = jo["ScanStep"].ToString();


                                        string strJSON = JsonConvert.SerializeObject(AA);
                                        mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                                    }
                                }
                                LogHelper.WriteLog(typeof(MainForm), "N774x sn error or Slot number error");
                                return;
                            }
                        }

                    }





                    //check channel count, pwmpower count , decrement count 三条数量是不是一致;
                    if (m_Scan_Driver_type != 44)
                    {
                        string[] split = jo["IndexOfPWMChannels"].ToString().Split(';');//class1.strIndexOfPWMChannels.Split(',');
                        string[] splitPWMPPower = jo["PWMPower"].ToString().Split(';');//class1.strIndexOfPWMChannels.Split(',');
                        string[] splitDecrement = jo["Decrement"].ToString().Split(';');//class1.strIndexOfPWMChannels.Split

                        bool bQuantityError = false;

                        if(jo["NumberOfScan"].ToString()=="1")
                        {
                            if ((split.Length != splitPWMPPower.Length) || (split.Length != splitDecrement.Length) || (splitDecrement.Length != splitPWMPPower.Length))
                            {
                                bQuantityError = true;
                            }
                        }
                        else if(jo["NumberOfScan"].ToString() == "0")
                        {
                            if (split.Length != splitPWMPPower.Length)
                            {
                                bQuantityError = true;
                            }
                        }

                        if(bQuantityError)
                        {
                            foreach (var item1 in mManager.m_AllClients)
                            {

                                if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                                {

                                    CResultFeedBack AA = new CResultFeedBack();
                                    AA.SocketGUID = jo["SocketGUID"].ToString();
                                    AA.IndexOfPWMChannels = jo["IndexOfPWMChannels"].ToString();
                                    AA.Result = "0";
                                    AA.ErrorDesc = "IndexOfPWMChannels, PWMPower and Decrement's quantity error";
                                    AA.SocketID = jo["SocketID"].ToString();
                                    AA.StartWaveLength = jo["StartWaveLength"].ToString();
                                    AA.StopWaveLength = jo["StopWaveLength"].ToString();
                                    AA.ScanStep = jo["ScanStep"].ToString();


                                    string strJSON = JsonConvert.SerializeObject(AA);
                                    mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                                }
                            }
                            LogHelper.WriteLog(typeof(MainForm), "IndexOfPWMChannels, PWMPower and Decrement's quantity error");
                            return;
                        }


                    }


                    //JObject o = (JObject)ja[1];
                    ListViewItem item = new ListViewItem();
                    

                    item.Text = jo["SocketID"].ToString();
                    item.SubItems.Add(jo["SocketGUID"].ToString());
                    item.SubItems.Add(jo["StartWaveLength"].ToString());
                    item.SubItems.Add(jo["StopWaveLength"].ToString());
                    item.SubItems.Add(dbStep_1.ToString());
                    item.SubItems.Add(jo["TLSPower"].ToString());
                    item.SubItems.Add(jo["PWMPower"].ToString());
                    item.SubItems.Add(jo["DoPDL"].ToString());
                    item.SubItems.Add(jo["NumberOfScan"].ToString());
                    item.SubItems.Add(jo["IndexOfPWMChannels"].ToString());
                    item.SubItems.Add(remoteEndPoint.ToString());
                    item.SubItems.Add(jo["Decrement"].ToString() );


                    lock(m_listScanList)
                    {

                        string[] split = jo["IndexOfPWMChannels"].ToString().Split(';');//class1.strIndexOfPWMChannels.Split(',');
                        string[] splitPWMPPower = jo["PWMPower"].ToString().Split(';');//class1.strIndexOfPWMChannels.Split(',');
                        string[] splitDecrement = jo["Decrement"].ToString().Split(';');//class1.strIndexOfPWMChannels.Split(',');
                        string strAbsolutePathOfIL="", strAbsolutePathOfPDL="";
                        string strDoPDL = jo["DoPDL"].ToString();

                        foreach (string memberofsplitChannels in split)
                        {
                            if (memberofsplitChannels.IndexOf('_') > -1)
                            {
                                //2是SCPI
                                if (m_Scan_Driver_type == 2)
                                {
                                    if(strDoPDL=="1")
                                    {
                                        strAbsolutePathOfIL += string.Format("{0}{1}_1_copy.dat;", m_strABS_PATH, memberofsplitChannels);
                                        strAbsolutePathOfIL += string.Format("{0}{1}_2_PDL_copy.dat;", m_strABS_PATH, memberofsplitChannels);
                                        //strAbsolutePathOfIL += string.Format("{0}{1}_3.dat;", m_strABS_PATH, memberofsplitChannels);
                                        //strAbsolutePathOfIL += string.Format("{0}{1}_4.dat;", m_strABS_PATH, memberofsplitChannels);
                                        //strAbsolutePathOfPDL += string.Format("{0}{1}_PDL.dat;", m_strABS_PATH, memberofsplitChannels);
                                    }
                                    else
                                    {
                                        strAbsolutePathOfIL += string.Format("{0}{1}.dat;", m_strABS_PATH, memberofsplitChannels);
                                        strAbsolutePathOfPDL = "";
                                    }

                                }
                                //4是n7786
                                else if (m_Scan_Driver_type == 4)
                                {
                                    strAbsolutePathOfIL += string.Format("{0}{1}.dat;", m_strABS_PATH, memberofsplitChannels);
                                    strAbsolutePathOfPDL += string.Format("{0}{1}_PDL.dat;", m_strABS_PATH, memberofsplitChannels);


                                }
                                //其他
                                else
                                {
                                    strAbsolutePathOfIL += string.Format("{0}{1}.dat;", m_strABS_PATH, memberofsplitChannels);
                                    strAbsolutePathOfPDL = "";
                                }
                            }
                            else
                            {
                                int indexChannel = Convert.ToInt32(memberofsplitChannels)-1;
                                //2是SCPI
                                if (m_Scan_Driver_type == 2)
                                {
                                    if(strDoPDL=="1")
                                    {
                                        strAbsolutePathOfIL += string.Format("{0}{1}_1_copy.dat;", m_strABS_PATH,m_SCPI_Scan.m_list_Config_string[indexChannel]);
                                        strAbsolutePathOfIL += string.Format("{0}{1}_2_PDL_copy.dat;", m_strABS_PATH, m_SCPI_Scan.m_list_Config_string[indexChannel]);
                                        //strAbsolutePathOfIL += string.Format("{0}{1}_3.dat;", m_strABS_PATH, m_SCPI_Scan.m_list_Config_string[indexChannel]);
                                        //strAbsolutePathOfIL += string.Format("{0}{1}_4.dat;", m_strABS_PATH, m_SCPI_Scan.m_list_Config_string[indexChannel]);
                                        //strAbsolutePathOfPDL += string.Format("{0}{1}_PDL.dat;", m_strABS_PATH, m_SCPI_Scan.m_list_Config_string[indexChannel]); ;
                                    }
                                    else
                                    {
                                        strAbsolutePathOfIL += string.Format("{0}{1}.dat;", m_strABS_PATH, m_SCPI_Scan.m_list_Config_string[indexChannel]);
                                        strAbsolutePathOfPDL = "";
                                    }

                                }
                                //4是n7786
                                else if(m_Scan_Driver_type == 4)
                                {
                                    string[] strMF = m_listMF[indexChannel].Split('.');
                                    int mfIndex = Convert.ToInt32(strMF[0].Substring(2));
                                    strAbsolutePathOfIL += string.Format("{0}{1}_{2}.dat;", m_strABS_PATH, strMF[3], strMF[1]);
                                    strAbsolutePathOfPDL += string.Format("{0}{1}_{2}_PDL.dat;", m_strABS_PATH, strMF[3], strMF[1]);
                                }
                                //其他
                                else
                                {
                                    string[] strMF = m_listMF[indexChannel].Split('.');
                                    int mfIndex = Convert.ToInt32(strMF[0].Substring(2));
                                    strAbsolutePathOfIL += string.Format("{0}{1}_{2}.dat;", m_strABS_PATH, strMF[3], strMF[1]);
                                    strAbsolutePathOfPDL = "";
                                }
                            }
                        }

                            //HashSet<int> hsChannels = new HashSet<int>();
                        int nLoop = 0;
                        foreach (string memberofsplit in split)
                        {
                            if (memberofsplit != "")
                            {
                                //hsChannels.Add(Convert.ToInt32(a));
                                clStructList class1 = new clStructList();
                                class1.strSocketID = jo["SocketID"].ToString();
                                class1.strStartWaveLength = jo["StartWaveLength"].ToString();
                                class1.strStopWaveLength = jo["StopWaveLength"].ToString();
                                class1.strScanStep = dbStep_1.ToString();
                                class1.strTLSPower = jo["TLSPower"].ToString();
                                class1.strPWMPower = jo["PWMPower"].ToString();
                                if (jo["NumberOfScan"].ToString() == "0")
                                {
                                    class1.strNumberOfScan = "0";
                                }
                                else if (jo["NumberOfScan"].ToString() == "1")
                                {
                                    class1.strNumberOfScan = "1";
                                }
                                else
                                {
                                    class1.strNumberOfScan = "0";
                                }
                                class1.nNumberofExecutions = 0;

                                class1.strIndexOfPWMChannels = jo["IndexOfPWMChannels"].ToString();
                                class1.strRemoteEndPoint = remoteEndPoint.ToString();
                                class1.shStatus = Queue_Waiting_Scan;
                                class1.strDoPDL = jo["DoPDL"].ToString();
                                class1.strGUID = jo["SocketGUID"].ToString();
                                string[] splitChannel = { "" };
                                if(memberofsplit.IndexOf('_')>-1)
                                {
                                    splitChannel= memberofsplit.Split('_');
                                    class1.nSplitPWMChannels = CheckChannelMapping(splitChannel[0], splitChannel[1]);
                                }
                                else
                                {
                                    int indexChannel = Convert.ToInt32(memberofsplit);
                                    class1.nSplitPWMChannels = indexChannel;
                                }

                                class1.strSplitPWMPower = splitPWMPPower[nLoop];
                                if(jo["NumberOfScan"].ToString()=="1")
                                {
                                    if (splitDecrement[nLoop].IndexOf("-") >= 0)
                                    {
                                        class1.strSplitDecrementRange = splitDecrement[nLoop];

                                    }
                                    else
                                    {
                                        //class1.strSplitDecrementRange = "-" + splitDecrement[nLoop];
                                        class1.strSplitDecrementRange = "-20" ;

                                    }
                                }

                                class1.strAbsolutePathForIL = strAbsolutePathOfIL;
                                class1.strAbsolutePathForPDL = strAbsolutePathOfPDL;

                                m_listScanList.Add(class1);
                                nLoop++;
                            }

                        }
                        
                    }
                    LogHelper.WriteLog(typeof(MainForm), string.Format("scan request {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10},{11} ",
                        jo["SocketGUID"].ToString(),
                        jo["SocketID"].ToString(),
                        jo["StartWaveLength"].ToString(),
                        jo["StopWaveLength"].ToString(),
                        jo["ScanStep"].ToString(),
                        jo["TLSPower"].ToString(),
                        jo["PWMPower"].ToString(),
                        jo["NumberOfScan"].ToString(),
                        jo["IndexOfPWMChannels"].ToString(),
                        jo["DoPDL"].ToString(),
                        jo["Decrement"].ToString(),
                        remoteEndPoint.ToString()));

                    //JsonTextReader reader = new JsonTextReader(new StringReader(str1));
                    //item.SubItems.Add(ja["startWaveLength"].ToString());

                    //while (reader.Read())
                    //{
                    //    if (reader.Value != null)
                    //    {
                    //        Debug.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                    //        if (nRead == 0)
                    //        {
                    //            item.Text = reader.Value.ToString();
                    //        }
                    //        else
                    //        {
                    //            item.SubItems.Add(reader.Value.ToString());
                    //        }
                    //    }
                    //    else
                    //    {
                    //        Debug.WriteLine("Token: {0}", reader.TokenType);
                    //    }
                    //    nRead++;
                    //}
                    //lock (ListView1)
                    {
                        ListView_Add_Item(item);
                    }
                    if (TLSLamdaZeroingMutex.WaitOne())
                    {
                        if (m_bTLSLamdaZeroing)
                        {
                            foreach (var item1 in mManager.m_AllClients)
                            {

                                if (item1.Value.TcpClient.Client.RemoteEndPoint == remoteEndPoint)
                                {

                                    CResultFeedBack AA = new CResultFeedBack();

                                    AA.SocketGUID = jo["SocketGUID"].ToString();
                                    AA.SocketID = jo["SocketID"].ToString();
                                    AA.IndexOfPWMChannels = jo["IndexOfPWMChannels"].ToString();
                                    AA.Result = "0";
                                    AA.ErrorDesc = "TLS Lambda zeroing 正在执行，请等待...";
                                    AA.StartWaveLength = jo["StartWaveLength"].ToString();
                                    AA.StopWaveLength = jo["StopWaveLength"].ToString();
                                    AA.ScanStep = jo["ScanStep"].ToString();


                                    string strJSON = JsonConvert.SerializeObject(AA);
                                    mManager.m_Server.Send(item1.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                                }
                            }
                        }
                        TLSLamdaZeroingMutex.ReleaseMutex();
                    }
                }), pData, pSzie,remoteEndPoint);
            }
            else
            {
                if (CHK_LOG.Checked == true )
                {

                    richTextBox1.Text = richTextBox1.Text + str1 + System.Environment.NewLine;
                }

            }
        }

        delegate void AsynUpdateReceInfo(ListViewItem pItem, int pCount, long pSzie);
        public void UpdateReceInfo(ListViewItem pItem, int pCount, long pSzie)
        {
            if (InvokeRequired)
            {
                Invoke(new AsynUpdateReceInfo(delegate (ListViewItem _Item, int _Count, long _Szie)
                {
                    _Item.SubItems[4].Text = _Count + "";
                    _Item.SubItems[5].Text = _Szie + "";
                }), pItem, pCount, pSzie);
            }
            else
            {
                pItem.SubItems[4].Text = pCount + "";
                pItem.SubItems[5].Text = pSzie + "";
            }
        }

        private long lastSendBytes = 0;
        private long lastReceBytes = 0;
        private void FlowTimer_Tick(object sender, System.EventArgs e)
        {
            PreSecSendBytes.Text = AsyncTCPServer.s_AllSendBytes - lastSendBytes + @"B/s";
            PreSecReceBytes.Text = AsyncTCPServer.s_AllReceBytes - lastReceBytes + @"B/s";
            lastSendBytes = AsyncTCPServer.s_AllSendBytes;
            lastReceBytes = AsyncTCPServer.s_AllReceBytes;
        }


        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            CResultFeedBack AA = new CResultFeedBack();
            AA.SocketGUID = System.Guid.NewGuid().ToString();
            AA.IndexOfPWMChannels = "3,5,7";
            AA.Result = "1";
            AA.ErrorDesc = "None";
            AA.StartWaveLength = "None";


            string strJSON = JsonConvert.SerializeObject(AA);
            //string strSend = @"yrtrrerr";
            foreach(var item in mManager.m_AllClients)
            {
                TCPClientSession A = item.Value;
                //A.TcpClient.Client.RemoteEndPoint.ToString()  = 
                mManager.m_Server.Send(item.Value, System.Text.Encoding.Default.GetBytes(strJSON));
            }
        }

        private void FeedBackMSG(clStructList nj , HashSet<string> hash)
        {
            CResultFeedBack AA = new CResultFeedBack();
            AA.SocketID = nj.strSocketID;
            AA.SocketGUID = nj.strGUID;
            AA.IndexOfPWMChannels = nj.strIndexOfPWMChannels;
            AA.Result = "1";
            AA.ErrorDesc = "None";
            AA.AbsoluePath = nj.strAbsolutePathForIL;
            if(m_Scan_Driver_type==1)
            {
                AA.StartWaveLength = myEng.WavelengthStart.ToString();
                AA.StopWaveLength = myEng.WavelengthStop.ToString();
                AA.ScanStep = myEng.WavelengthStep.ToString();
            }
            else if (m_Scan_Driver_type == 2)
            {

                AA.StartWaveLength = m_dbl_SCPI_START_WL.ToString();
                AA.StopWaveLength = m_dbl_SCPI_STOP_WL.ToString();
                AA.ScanStep = m_dbl_SCPI_STEP_WL.ToString(); 
            }
            else if(m_Scan_Driver_type == 3)
            {
                AA.StartWaveLength = myEngSlow.WavelengthStart.ToString();
                AA.StopWaveLength = myEngSlow.WavelengthStop.ToString();
                AA.ScanStep = myEngSlow.WavelengthStep.ToString();
            }
            else if (m_Scan_Driver_type == 4)
            {
                AA.StartWaveLength = myEngPDL.WavelengthStart.ToString();
                AA.StopWaveLength = myEngPDL.WavelengthStop.ToString();
                double sweep = myEngPDL.SweepRate;

                // Sets or gets the sweep rate in nm / s.Sweep rate v in nm / s and step size s in pm are related by
                //0.3 x v[nm / s] = s[pm](e.g. 20 nm / s yields 6 pm step size)
                AA.ScanStep =Convert.ToString( myEngPDL.SweepRate*0.3);

                AA.AbsoluePath = nj.strAbsolutePathForIL + nj.strAbsolutePathForPDL;

            }
            else if (m_Scan_Driver_type == 5)
            {

                AA.StartWaveLength = m_dbl_SCPI_START_WL.ToString();
                AA.StopWaveLength = m_dbl_SCPI_STOP_WL.ToString();
                AA.ScanStep = m_dbl_SCPI_STEP_WL.ToString();
            }
            else if (m_Scan_Driver_type == 44)
            {
                AA.StartWaveLength = dbl_dummy_wvstart.ToString();
                AA.StopWaveLength = dbl_dummy_wvstop.ToString();
                AA.ScanStep = dbl_dummy_wvstep.ToString();
            }

            //LogHelper.WriteLog(typeof(MainForm), string.Format("scan feedback {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7},  ",
            //                    AA.SocketID ,
            //                    AA.SocketGUID ,
            //                    AA.IndexOfPWMChannels,
            //                    AA.Result ,
            //                    AA.ErrorDesc ,
            //                    AA.StartWaveLength ,
            //                    AA.StopWaveLength ,
            //                    AA.ScanStep ));

            string strJSON = JsonConvert.SerializeObject(AA);

            


            //HashSet<string> hash = new HashSet<string>();
            //string strSend = @"yrtrrerr";
            foreach (var item in mManager.m_AllClients)
            {

                if (item.Value.TcpClient.Client.RemoteEndPoint.ToString() == nj.strRemoteEndPoint)
                {

                    if (hash.Add(nj.strRemoteEndPoint))
                    {
                        LogHelper.WriteLog(typeof(MainForm), string.Format("scan feedback {0}", strJSON));
                        mManager.m_Server.Send(item.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                    }
                    
                }
            }
        }

        private void FeedBackErrorMSG(string strError)
        {

            HashSet<string> hash = new HashSet<string>();
            for (int i = m_listScanList.Count - 1; i >= 0; i--)
            {
                clStructList nj = m_listScanList[i];

                //if (nj.shStatus == Queue_Doing_Scan)
                {
 
                    CResultFeedBack AA = new CResultFeedBack();
                    AA.SocketID = nj.strSocketID;
                    AA.SocketGUID = nj.strGUID;
                    AA.IndexOfPWMChannels = nj.strIndexOfPWMChannels;
                    AA.Result = "0";
                    AA.ErrorDesc = strError;
                    AA.AbsoluePath = nj.strAbsolutePathForIL;

                    if (m_Scan_Driver_type == 1)
                    {
                        AA.StartWaveLength = myEng.WavelengthStart.ToString();
                        AA.StopWaveLength = myEng.WavelengthStop.ToString();
                        AA.ScanStep = myEng.WavelengthStep.ToString();
                    }
                    else if (m_Scan_Driver_type == 2)
                    {

                        AA.StartWaveLength = m_dbl_SCPI_START_WL.ToString();
                        AA.StopWaveLength = m_dbl_SCPI_STOP_WL.ToString();
                        AA.ScanStep = m_dbl_SCPI_STEP_WL.ToString();
                    }
                    else if (m_Scan_Driver_type == 3 )
                    {
                        AA.StartWaveLength = myEngSlow.WavelengthStart.ToString();
                        AA.StopWaveLength = myEngSlow.WavelengthStop.ToString();
                        AA.ScanStep = myEngSlow.WavelengthStep.ToString();
                    }
                    else if (m_Scan_Driver_type == 4)
                    {
                        AA.StartWaveLength = myEngPDL.WavelengthStart.ToString();
                        AA.StopWaveLength = myEngPDL.WavelengthStop.ToString();
                        double sweep = myEngPDL.SweepRate;

                        // Sets or gets the sweep rate in nm / s.Sweep rate v in nm / s and step size s in pm are related by
                        //0.3 x v[nm / s] = s[pm](e.g. 20 nm / s yields 6 pm step size)
                        AA.ScanStep = Convert.ToString(myEngPDL.SweepRate * 0.3);
                    }
                    else if (m_Scan_Driver_type == 44)
                    {
                        AA.StartWaveLength = dbl_dummy_wvstart.ToString();
                        AA.StopWaveLength = dbl_dummy_wvstop.ToString();
                        AA.ScanStep = dbl_dummy_wvstep.ToString();
                    }
                    string strJSON = JsonConvert.SerializeObject(AA);
                    //string strSend = @"yrtrrerr";
                    foreach (var item in mManager.m_AllClients)
                    {

                        if (item.Value.TcpClient.Client.RemoteEndPoint.ToString() == nj.strRemoteEndPoint)
                        {
                            if(hash.Add(nj.strRemoteEndPoint))
                            {
                                mManager.m_Server.Send(item.Value, System.Text.Encoding.Default.GetBytes(strJSON));
                            }
                            
                        }
                    }
                }
            }
        }


        private void btnSingleScan_Click(object sender, EventArgs e)
        {
            start();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";

        }


        private void ReadPortMappingConfig()
        {

            FileStream fs = new FileStream("./config/PortMapping.txt", FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string s;


            s = sr.ReadLine();
            while (s != null)
            {
                //MSGBox.Text += s + "\n";
                s = sr.ReadLine();
            }
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            //Thread.Sleep(100);

            btnStartServer_Click(sender, e);

        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_bExistAllofPrgress = true;
            if(m_Scan_Driver_type==1)
            {
                if(myEng.Active==1)
                {
                    myEng.DeActivate();
                }
                mManager.Stop();
            }
            else if (m_Scan_Driver_type == 2)
            {
                m_SCPI_Scan.FreeGlobalMemory();
                SCPI_FreeRawData();
            }
            else if(m_Scan_Driver_type==3)
            {
                if(myEngSlow.Active==1)
                {
                    myEngSlow.DeActivate();
                }
                mManager.Stop();
            }
            else if (m_Scan_Driver_type == 5)
            {

            }

            mManager.Stop();

        }


        private int CheckChannelMapping(string strSN, string strSlot)
        {

            for(int i=0;i< m_listChannelMapping.Count;i++)
            {
                clInstrumentChannelsMapping nj = m_listChannelMapping[i];
                if (nj.strN774XSN == strSN && strSlot == nj.strSlotNo)
                {
                    return nj.nChannelSN;
                }

            }

            return 0;
        }

        private void LoadChannelMapping()
        {
            m_listChannelMapping.Clear();
            if (m_Scan_Driver_type == 1 || m_Scan_Driver_type == 3 || m_Scan_Driver_type == 4 || m_Scan_Driver_type == 5 || m_Scan_Driver_type == 44)
            {
                FileStream fs = new FileStream("./config/PortMappingauto.txt", FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                sr.ReadLine();
                string strcontent = sr.ReadLine();
                int nloop = 1;
                m_listN77XXSN.Clear();
                m_listMF.Clear();
                while (null != strcontent)
                {
                    m_listMF.Add(strcontent);

                    string[] split = strcontent.Split('.');
                    clInstrumentChannelsMapping nj = new clInstrumentChannelsMapping();
                    nj.nChannelSN = nloop;
                    nj.strMFNo = split[0];
                    nj.strSlotNo = split[1];
                    nj.strChannelNo = split[2];
                    nj.strN774XSN = split[3];
                    m_listN77XXSN.Add(split[3]);

                    m_listChannelMapping.Add(nj);

                    nloop++;
                    strcontent = sr.ReadLine();
                }
                sr.Close();
                fs.Close();
            }
            else 
            {

                int nLines = 0, n;
                string strFile = "./CONFIG/PMConfig.csv";
                List<string> list_Config_string = new List<string>();
                FileStream fs = new FileStream(strFile, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                string readStr = sr.ReadLine();

                while (readStr != null)
                {
                    nLines++;
                    if (nLines > 6)
                    {

                        string[] split = readStr.Split(',');
                        if(split.Length <5)
                        {
                            readStr = sr.ReadLine();
                            break;
                        }
                        string[] sp1 = split[4].Split('_');
                        if(sp1.Length<2)
                        {
                            readStr = sr.ReadLine();
                            break;
                        }
                        clInstrumentChannelsMapping nj = new clInstrumentChannelsMapping();
                        nj.nChannelSN =Convert.ToInt32(split[0]);
                        nj.strMFNo = split[1];
                        nj.strSlotNo = sp1[1];
                        nj.strChannelNo = "Channel000";
                        nj.strN774XSN = sp1[0];

                        m_listChannelMapping.Add(nj);
                    }
                    readStr = sr.ReadLine();
                }
                sr.Close();
                fs.Close();
                sr = null;
                fs = null;
            }


        }


        private void LoadWVCalibateList()
        {
            //List<ProtocolMapping> m_listProtocolMapping;
            m_listWVCalibrate.Clear();
            try
            {
                FileStream fs = new FileStream("./config/Gas_Cell_CW_LIST.txt", FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                string strcontent = sr.ReadLine();
                while ((strcontent = sr.ReadLine()) !=null)
                {
                    string[] split = strcontent.Split(',');
                    Gas_Cell_Calibrate nj = new Gas_Cell_Calibrate();

                    nj.dblLeftWV = Convert.ToDouble(split[0]);
                    nj.dblRightWV = Convert.ToDouble(split[1]);
                    nj.dblCW = Convert.ToDouble(split[2]);
                    m_listWVCalibrate.Add(nj);
                    
                }

                sr.Close();
                fs.Close();
                sr = null;
                fs = null;
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                //Console.WriteLine("The file could not be read:");
                //Console.WriteLine(e.Message);
                MessageBox.Show("The file could not be read:" + e.Message);
            }

        }

        private void LoadProtocolMapping()
        {
            //List<ProtocolMapping> m_listProtocolMapping;
            m_listProtocolMapping.Clear();
      
            FileStream fs = new FileStream("./config/ProtocolMapping.txt", FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            sr.ReadLine();
            string strcontent = sr.ReadLine();
            int nloop = 1;
            while (null != strcontent)
            {
                string[] split = strcontent.Split(',');
                ProtocolMapping nj = new ProtocolMapping();
                
                nj.strFSTPStationKey = split[0];
                nj.strComputerName = split[1];
                nj.strMappingAddress = split[2];
                nj.strEndpoint = "";

                m_listProtocolMapping.Add(nj);

                nloop++;
                strcontent = sr.ReadLine();
            }
            sr.Close();
            fs.Close();
            sr = null;
            fs = null;
            


        }

        bool SCPI_FreeRawData()
        {
            return m_SCPI_Scan.SCPI_FreeRawData();
        }

        bool SCPI_ChannelSetup()
        {
            for (int jj = 0; jj <48000; jj++)
            {
                
                m_SCPI_Scan.m_xxx.m_pdblWavelengthArray[jj] = 0;
            }
            for(int ii =0;ii<64;ii++)
            {
                m_SCPI_Scan.m_xxx.m_nRange[ii] = 0;
                for (int jj = 0; jj < 48000; jj++)
                {
                    m_SCPI_Scan.m_xxx.m_pdblLossArrayP1[ii].ilArray[jj] = 0;
                }
            }



            for (int jj =0;jj<m_listChannelMapping.Count;jj++)
            {
                clInstrumentChannelsMapping nj = m_listChannelMapping[jj];

                if (scanRequestMutex.WaitOne())
                {
                    for (int I = 0; I < m_listScanList.Count; I++)
                    {
                        clStructList Mn = m_listScanList[I];
                        if (Mn.strNumberOfScan == "0")//扫描单档
                        {
                            if ((nj.nChannelSN == Mn.nSplitPWMChannels) && (Mn.shStatus == Queue_Doing_Scan))
                            {
                                m_SCPI_Scan.m_xxx.m_nRange[nj.nChannelSN-1] = Convert.ToInt32(Convert.ToDouble(Mn.strSplitPWMPower));
                            }
                        }
                        else //扫描两档
                        {
                            if(Mn.strDoPDL == "1")//有 PDL 扫描4次，两档
                            {
                                if ((nj.nChannelSN == Mn.nSplitPWMChannels) &&   (Mn.shStatus == Queue_Doing_Scan))
                                {
                                    if (Mn.nNumberofExecutions <= 4)
                                    {
                                        m_SCPI_Scan.m_xxx.m_nRange[nj.nChannelSN-1] = Convert.ToInt32(Convert.ToDouble(Mn.strSplitPWMPower));
                                    }
                                    else
                                    {
                                        m_SCPI_Scan.m_xxx.m_nRange[nj.nChannelSN-1] = Convert.ToInt32(Convert.ToDouble(Mn.strSplitDecrementRange   ));
                                    }
                                }
                            }
                            else  //没有PDL 扫描1次，两档
                            {
                                if ((nj.nChannelSN == Mn.nSplitPWMChannels) && (Mn.shStatus == Queue_Doing_Scan))
                                {
                                    if(Mn.nNumberofExecutions <1 )
                                    {
                                        m_SCPI_Scan.m_xxx.m_nRange[nj.nChannelSN-1] = Convert.ToInt32(Convert.ToDouble(Mn.strSplitPWMPower));
                                    }
                                    else
                                    {
                                        m_SCPI_Scan.m_xxx.m_nRange[nj.nChannelSN-1] = Convert.ToInt32(Convert.ToDouble(Mn.strSplitDecrementRange));
                                    }
                                }
                            }

                        }


                    }
                    scanRequestMutex.ReleaseMutex();
                }
            }
            return true;
        }


        string XMLChannelSetup(string strXML)
        {
            string strReturn;
            XmlDataDocument xdoc = new XmlDataDocument();
            xdoc.LoadXml(strXML);
            XmlNode root = xdoc.DocumentElement;
            XmlNodeList a = root.ChildNodes;

            string str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", "MF000", "Slot001", "Channel000", "OverrideRange");
            XmlNode dd = xdoc.SelectSingleNode(str);
            dd.Attributes["Value"].Value = "0";

            for (int jj = 0; jj < m_listChannelMapping.Count; jj++)
            {
                clInstrumentChannelsMapping nj = m_listChannelMapping[jj];
                str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "Active");
                dd = xdoc.SelectSingleNode(str);

                //原有的方式，现将 所有通道  Active 置为 0， 然后根据扫描需要再 置 1，现在全改为 1
                //dd.Attributes["Value"].Value = "0";
                dd.Attributes["Value"].Value = "1";
                str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "OverrideRange");
                dd = xdoc.SelectSingleNode(str);
                dd.Attributes["Value"].Value = "0";
                if (scanRequestMutex.WaitOne())
                {
                    for (int I = 0; I < m_listScanList.Count; I++)
                    {
                        clStructList Mn = m_listScanList[I];

                        if ((nj.nChannelSN == Mn.nSplitPWMChannels) && (Mn.shStatus == Queue_Doing_Scan))
                        {
                            str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "Active");
                            dd = xdoc.SelectSingleNode(str);
                            dd.Attributes["Value"].Value = "1";
                            str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "OverrideRange");
                            dd = xdoc.SelectSingleNode(str);
                            dd.Attributes["Value"].Value = "1";
                            str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "Range");
                            dd = xdoc.SelectSingleNode(str);
                            dd.Attributes["Value"].Value = Mn.strSplitPWMPower;
                        }

                        if ((nj.nChannelSN == Mn.nSplitPWMChannels) && (Mn.strNumberOfScan == "1") && (Mn.nNumberofExecutions > 0) && (Mn.shStatus == Queue_Doing_Scan))
                        {
                            str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "OverrideRange");
                            dd = xdoc.SelectSingleNode(str);
                            dd.Attributes["Value"].Value = "1";
                            str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "Range");
                            dd = xdoc.SelectSingleNode(str);
                            dd.Attributes["Value"].Value = Mn.strSplitDecrementRange;
                        }
                        //else if ((nj.nChannelSN == Mn.nSplitPWMChannels) && (Mn.strNumberOfScan == "0") && (Mn.shStatus == Queue_Doing_Scan))
                        //{
                        //    str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "OverrideRange");
                        //    dd = xdoc.SelectSingleNode(str);
                        //    dd.Attributes["Value"].Value = "1";
                        //    str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Property[@Name='{2}']/Item[@Name='{3}']", nj.strMFNo, nj.strSlotNo, nj.strChannelNo, "Range");
                        //    dd = xdoc.SelectSingleNode(str);
                        //    dd.Attributes["Value"].Value = Mn.strSplitPWMPower;
                        //}

                    }
                    scanRequestMutex.ReleaseMutex();
                }


            }


            XmlNodeList list = xdoc.SelectNodes("Property");

            if (!Directory.Exists("./xmlsetup/"))
            {
                Directory.CreateDirectory("./xmlsetup/");
            }

            xdoc.Save("./xmlsetup/channelsetup.xml");

            return strReturn = xdoc.InnerXml;
        }

        string XMLChannelPDLSetup(string strXML)
        {
            string strReturn;
            XmlDataDocument xdoc = new XmlDataDocument();
            xdoc.LoadXml(strXML);
            XmlNode root = xdoc.DocumentElement;
            XmlNodeList a = root.ChildNodes;

            xdoc.Save("./xmlsetup/channelsetup.xml");

            return strReturn = xdoc.InnerXml;
        }


        string XMLChannelSLOWSetup(string strXML)
        {
            string strReturn;
            XmlDataDocument xdoc = new XmlDataDocument();
            xdoc.LoadXml(strXML);
            XmlNode root = xdoc.DocumentElement;
            XmlNodeList a = root.ChildNodes;

            xdoc.Save("./xmlsetup/channelsetup.xml");

            return strReturn = xdoc.InnerXml;
        }


        void SaveXMLChannelList(string strXML)
        {
            string strReturn;
            XmlDataDocument xdoc = new XmlDataDocument();
            xdoc.LoadXml(strXML);
            XmlNode root = xdoc.DocumentElement;
            XmlNodeList a = root.ChildNodes;

            //string str = string.Format("Property[@Name='']/Property[@Name='{0}']", "Channels");
            string str = "Property[@Name='']/Property[@Name='Channels']";
            XmlNode dd = xdoc.SelectSingleNode(str);
            XmlNodeList xmlchildren = dd.ChildNodes;
            string strAttribute;
            int nCount = xmlchildren.Count;


            m_listMF.Clear();
            HashSet<string> hash = new HashSet<string>();

            for (int i = 0; i < xmlchildren.Count; i++)
            {
                dd = xmlchildren[i];
                strAttribute = dd.FirstChild.Attributes["Value"].Value;
                m_listMF.Add(strAttribute.Trim());
                string[] strsplit = strAttribute.Split('.');
                hash.Add(strsplit[0]);

            }

            foreach (string s in hash)
            {
                str = string.Format("Property[@Name='']/Property[@Name='{0}']/Item[@Name='SN']", s);
                dd=xdoc.SelectSingleNode(str);
                strAttribute = dd.Attributes["Value"].Value;
                m_listN77XXSN.Add(strAttribute.Trim());
            }

            int nPWMCount = 0;

            if (m_Scan_Driver_type == 1)
            {
                nPWMCount = myEng.PWMCount;
            }
            else if (m_Scan_Driver_type == 3)
            {
                //nPWMCount = myEngSlow.OutputPorts;
            }

            FileStream fs = new FileStream("./config/PortMappingAuto.txt",FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine("N774X机器号, N774X slot号, N774X channel号, N774X SN");
            int iloop = 0;
            for(int i=0;i< nPWMCount;i++)
            {
                int nPWMChannels = myEng.GetPWMChannelCount(i);
                for (int m = 0; m < nPWMChannels; m++)
                {
                    sw.WriteLine(string.Format("{0}.{1}", m_listMF[iloop], m_listN77XXSN[i].Trim()));
                    iloop++;
                }

            }

            sw.Flush();
            sw.Close();
            fs.Close();


        }


        void SaveXMLChannelListSlow(string strXML)
        {
            string strReturn;
            XmlDataDocument xdoc = new XmlDataDocument();
            xdoc.LoadXml(strXML);
            XmlNode root = xdoc.DocumentElement;
            XmlNodeList a = root.ChildNodes;

            string str = string.Format("Property[@Name='']/Property[@Name='{0}']", "Channels");
            XmlNode dd = xdoc.SelectSingleNode(str);
            XmlNodeList xmlchildren = dd.ChildNodes;
            string strAttribute;
            int nCount = xmlchildren.Count;


            m_listMF.Clear();
            HashSet<string> hash = new HashSet<string>();

            for (int i = 0; i < xmlchildren.Count; i++)
            {
                dd = xmlchildren[i];
                strAttribute = dd.FirstChild.Attributes["Value"].Value;
                m_listMF.Add(strAttribute.Trim());         //strAttribute : MF000.Slot001.Channel000
                //string[] strsplit = strAttribute.Split('.');
                hash.Add(strAttribute);

            }

            foreach (string s in hash)
            {
                string[] strsplit = s.Split('.');
                str = string.Format("Property[@Name='']/Property[@Name='{0}']/Property[@Name='{1}']/Item[@Name='SN']", strsplit[0], strsplit[1]);
                dd=xdoc.SelectSingleNode(str);
                strAttribute = dd.Attributes["Value"].Value;
                m_listN77XXSN.Add(strAttribute.Trim());     //strAttribute: MY48103023
            }



            FileStream fs = new FileStream("./config/PortMappingAuto.txt",FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine("N774X机器号, N774X slot号, N774X channel号, N774X SN");
            int iloop = 0;
            for(int i=0;i< nCount; i++)
            {
                    sw.WriteLine(string.Format("{0}.{1}", m_listMF[i], m_listN77XXSN[i].Trim()));
                    iloop++;

            }

            sw.Flush();
            sw.Close();
            fs.Close();


        }

        void SaveXMLChannelListPDL(string strXML)
        {
            string strReturn;
            XmlDataDocument xdoc = new XmlDataDocument();
            xdoc.LoadXml(strXML);
            XmlNode root = xdoc.DocumentElement;
            XmlNodeList a = root.ChildNodes;

            string str = string.Format("Property[@Name='']/Property[@Name='{0}']", "Channels");
            XmlNode dd = xdoc.SelectSingleNode(str);
            XmlNodeList xmlchildren = dd.ChildNodes;
            string strAttribute;
            int nCount = xmlchildren.Count;


            m_listMF.Clear();
            HashSet<string> hash = new HashSet<string>();

            for (int i = 0; i < xmlchildren.Count; i++)
            {
                dd = xmlchildren[i];
                strAttribute = dd.FirstChild.Attributes["Value"].Value;
                m_listMF.Add(strAttribute.Trim());
                //string[] strsplit = strAttribute.Split('.');
                hash.Add(strAttribute);

            }

/*  保留格式，不要强制对齐
<Property Name="">
- <Property Name="MF000">
    <Item Name="ModelCode" Type="String" Value="N7745A" /> 
    <Item Name="SN" Type="String" Value="MY48103023" /> 
*/


            foreach (string s in hash)
            {
                string[] strsplit = s.Split('.');
                str = string.Format("Property[@Name='']/Property[@Name='{0}']/Item[@Name='SN']", strsplit[0]);
                dd = xdoc.SelectSingleNode(str);
                strAttribute = dd.Attributes["Value"].Value;
                if (!m_listN77XXSN.Exists(t=>t==strAttribute.Trim()))
                {
                    m_listN77XXSN.Add(strAttribute.Trim());
                }
               
            }



            FileStream fs = new FileStream("./config/PortMappingAuto.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine("N774X机器号, N774X slot号, N774X channel号, N774X SN");
            int iloop = 0;
            for (int i = 0; i < nCount; i++)
            {
                string[] sp = m_listMF[i].Split('.');
                int nid = Convert.ToInt32(sp[0].Substring(2));
                sw.WriteLine(string.Format("{0}.{1}", m_listMF[i], m_listN77XXSN[nid].Trim()));
                iloop++;

            }

            sw.Flush();
            sw.Close();
            fs.Close();


        }


        private bool  ChkChannelExist(string rqtString )
        {
            string[] split = rqtString.Split(';');//class1.strIndexOfPWMChannels.Split(',');
            List<bool> listbExist = new List<bool>();
            int nLoop = 0;
            bool rtnBool = true;
            string strSplit = "";

            foreach (string a in split)
            {
                strSplit = a.ToUpper();
                listbExist.Add(false);
                //
                foreach(clInstrumentChannelsMapping b  in m_listChannelMapping)
                {
                    string strCompare = b.strN774XSN + "_" + b.strSlotNo;
                    strCompare = strCompare.ToUpper();

                    //LogHelper.WriteLog(typeof(MainForm), string.Format("compare string var1={0},var2={1}", strCompare, strSplit));
                    if (strCompare == strSplit)
                    {
                        listbExist[nLoop] = true;
                        break;
                    }
                }
                nLoop++;
            }

            foreach(bool f in listbExist)
            {
                rtnBool = rtnBool && f;
            }

            return rtnBool;
        }


        private bool Chk_ScanListStatus_Doing_Scan()
        {

            for (int i = 0; i < m_listScanList.Count; i++)
            {
                clStructList nj = m_listScanList[i];
                if (nj.shStatus == Queue_Doing_Scan)
                {

                    return true;
                }

            }

            return false;
        }

    }
}