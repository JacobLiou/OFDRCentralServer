using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq.Expressions;

namespace SampleTcpServer
{

/*
typedef struct tagAutoTestParam
{                                 
	BOOL	m_bPDL;
	int m_nChannelCount;
	int m_nArrayCapacity;
	int  m_nRange[64];
	double	m_pdblWavelengthArray[48000];  
	double	m_pdblLossArrayP1[64][48000]; 
	char  m_chFilefullpath[9][256]; 
} stAutoTestParam, *PAutoTestParam;
// sprintf(m_AutoTestData.m_chFilefullpath[0], "%s","F:\\dongm\\TLS_FASTSCAN_SERVER_interleaver_ok\\BIN\\RowData\\testdata_ref\\MY4_Slot002_1.dat");
*/

    public struct lLossArray
    {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48000)]
        public double[] ilArray;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct DataParam
    {
        [MarshalAsAttribute(UnmanagedType.Bool)]
        public bool m_bPDL;

        public int m_nChannelCount;
        public int m_nArrayCapacity;

        //字符串，SizeConst为字符串的最大长度
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public int[] m_nRange;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48000)]
        public double[] m_pdblWavelengthArray;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public lLossArray[] m_pdblLossArrayP1;

    }
    class SCPI_Scan
    {
        public static int g_nArraySize = 100000;

        public double m_dbl_SCPI_START_WL, m_dbl_SCPI_STOP_WL, m_dbl_SCPI_STEP_WL;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);


        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);


        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetPrivateProfileInt(string section, string key, int nDefault, string filePath);


        [DllImport("ScanDriverDll.dll", EntryPoint = "_AutoTest@4", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        extern static bool AutoTest(IntPtr atp);

        [DllImport("ScanDriverDll.dll", EntryPoint = "OpenDevice", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        //[DllImport("ScanDriverDll.dll")]
        public extern static bool OpenDevice(bool bAlpha);

        [DllImport("ScanDriverDll.dll", EntryPoint = "FreeRawData", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        //[DllImport("ScanDriverDll.dll")]
        public extern static bool FreeRawData();


        [DllImport("ScanDriverDll.dll", EntryPoint = "SetPDLAngles", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        //[DllImport("ScanDriverDll.dll")]
        public extern static bool SetPDLAngles(int nState);

        enum PDLStates
        {
            HorizontalAngle = 1, VerticalAngles, DiagonalAngles, RightHandCircularAngles
        };

        public DataParam m_xxx;//= new DataParam();
        IntPtr m_yyy;

        public List<string> m_list_Config_string;

        public void SCPI_OpenDevice(bool bAlpha)
        {
            OpenDevice(bAlpha);

        }

        public bool SCPI_FreeRawData()
        {
            return FreeRawData();
        }

        public bool SCPI_SETANGLES(int nPDLAngles)
        {
            return SetPDLAngles(nPDLAngles);
        }

        public void initStruct()
        {

            int i = 0;
            
            double dblWVStart, dblWVStop, dblWVStep;

            int nLines = 0, n;
            string strFile = "./CONFIG/PMConfig.csv";
            m_list_Config_string = new List<string>();
            FileStream fs = new FileStream(strFile, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            string readStr = sr.ReadLine();

            while (readStr != null)
            {
                nLines++;
                if (nLines > 6)
                {
                    if(readStr.Trim()!="")
                    {
                        string[] strp = readStr.Split(',');
                        if (strp.Length < 5)
                        {
                            break;
                        }
                        else
                        {
                            m_list_Config_string.Add(strp[4]);

                        }
                    }
                    else
                    {
                        break;
                    }
                }
                readStr = sr.ReadLine();
            }
            sr.Close();
            fs.Close();
            sr = null;
            fs = null;


            //string strPath = "./config/systemconfig.ini";

            //StringBuilder sb = new StringBuilder(32);

            //GetPrivateProfileString("START_WL", "TLS_SCAN", "0", sb, 32, strPath);
            //dblWVStart = Convert.ToDouble(sb.ToString());
            //GetPrivateProfileString("STOP_WL", "TLS_SCAN", "0", sb, 32, strPath);
            //dblWVStop = Convert.ToDouble(sb.ToString());
            //GetPrivateProfileString("STEP", "TLS_SCAN", "0", sb, 32, strPath);
            //dblWVStep = Convert.ToDouble(sb.ToString());
            //string strPath = "./config/systemconfig.ini";

            //StringBuilder sb = new StringBuilder(32);

            //m_Scan_Driver_type = GetPrivateProfileInt("SCAN_ENGINE_TYPE", "SCAN_ENGINE", 1, strPath);
            //m_SCPI_Scan_Has8169 = GetPrivateProfileInt("SCPI_HAS_8169 ", "SCAN_ENGINE", 0, strPath);
            //GetPrivateProfileString("TLS_SCAN", "START_WL", "1310", sb, 32, strPath);
            //m_dbl_SCPI_START_WL = Convert.ToDouble(sb.ToString());
            //GetPrivateProfileString("TLS_SCAN", "STOP_WL", "1410", sb, 32, strPath);
            //m_dbl_SCPI_STOP_WL = Convert.ToDouble(sb.ToString());
            //GetPrivateProfileString("TLS_SCAN", "STEP", "0", sb, 32, strPath);
            //m_dbl_SCPI_STEP_WL = Convert.ToDouble(sb.ToString()) * 1000.000;



            m_xxx.m_pdblWavelengthArray = new double[48000];
            m_xxx.m_nRange = new int[64];
            m_xxx.m_bPDL = false;
            m_xxx.m_nChannelCount = m_list_Config_string.Count;
            m_xxx.m_nArrayCapacity =Convert.ToInt32( (m_dbl_SCPI_STOP_WL - m_dbl_SCPI_START_WL) / m_dbl_SCPI_STEP_WL) +1;

            lLossArray[] PPP = new lLossArray[64];
            for (i = 0; i < 64; i++)
            {
                PPP[i].ilArray = new double[48000];
            }
            m_xxx.m_pdblLossArrayP1 = PPP;

            /*
            lLossArray[] PPP2 = new lLossArray[64];
            for (i = 0; i < 64; i++)
            {
                PPP2[i].ilArray = new double[100000];
            }
            m_xxx.m_pdblLossArrayP2 = PPP2;

            lLossArray[] PPP3 = new lLossArray[64];
            for (i = 0; i < 64; i++)
            {
                PPP3[i].ilArray = new double[100000];
            }
            m_xxx.m_pdblLossArrayP3 = PPP3;

            lLossArray[] PPP4 = new lLossArray[64];
            for (i = 0; i < 64; i++)
            {
                PPP4[i].ilArray = new double[100000];
            }
            m_xxx.m_pdblLossArrayP4 = PPP4;
            */


            int nSize = Marshal.SizeOf(m_xxx);

            //m_xxx = (DataParam)Marshal.PtrToStructure(yyy, typeof(DataParam));
            //Marshal.PtrToStructure(yyy, m_xxx);
            //Marshal.PtrToStructure(yyy, m_xxx);

            m_yyy = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(m_xxx, m_yyy, true);

        }

        public bool SCPI_DoScan()
        {
            //DataParam MSG = new DataParam();
            //
            //IntPtr msgx = Marshal.AllocHGlobal(nSize);
            //Marshal.StructureToPtr(MSG, msgx, true);
            //bool Bre = false;

            bool Bre = false;


            try
            {

                //DataParam m = new DataParam();

                Marshal.StructureToPtr(m_xxx, m_yyy, true);
                Bre = AutoTest(m_yyy);

                // Set this Point to the value of the 
                // Point in unmanaged memory. 
                m_xxx = Marshal.PtrToStructure<DataParam>(m_yyy);
                //m = (DataParam)Marshal.PtrToStructure(m_yyy, typeof(DataParam));

                //Console.WriteLine("The value of new point is " + anotherP.x + " and " + anotherP.y + ".");

            }
            catch(Exception e)
            {
                LogHelper.WriteLog(typeof(MainForm), e.Message);
            }
            finally
            {
                // Free the unmanaged memory.
                //Marshal.FreeHGlobal(m_yyy);
                
            }
            return Bre;
        }

        public void FreeGlobalMemory()
        {
            Marshal.FreeHGlobal(m_yyy);

        }
    }
}
