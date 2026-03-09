using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SampleTcpServer
{
    //GOUMAX 最多22000 个采样，
    public struct GouMaxlLossArray
    {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22000)]
        public double[] ilArray;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct GouMaxDataParam
    {
        //指示出数组有多少个元素，21501还是其它数值
        public int m_nArrayLength;

        //指示那几个通道参加入扫描置为1，不参加扫描的置为0
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] m_byChannelValide;

        //保存波长数组
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22000)]
        public double[] m_pdblWavelengthArray;
        //保存每个通道的db值， 最大支持64个通道。
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public GouMaxlLossArray[] m_pdblLossArrayP1;

        //保存n774x ref power 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22000)]
        public double[] m_pdbl774xRefArray;

        //保存816x ref power
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22000)]
        public double[] m_pdbl816xRefArray;
    }

    class GouMax_Scan
    {
        ////?OpenGouMaxAllDevice@@YGXXZ
        [DllImport("GouMax_Scan.dll", EntryPoint = "OpenGouMaxAllDevice", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public extern static bool OpenGouMaxAllDevice();

        [DllImport("GouMax_Scan.dll", EntryPoint = "GouMaxDoSingleScan", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public extern static bool GouMaxDoSingleScan(IntPtr atp);



        public GouMaxDataParam m_sttGoumaxData;//= new GouMaxDataParam();
        public IntPtr m_IntGoumaxDataPtr;

        public void GouMax_OpenDevice()
        {
            OpenGouMaxAllDevice();

        }

        public void GouMax_GouMaxDoSingleScan()
        {

            try
            {
                //结构体转指针
                Marshal.StructureToPtr(m_sttGoumaxData, m_IntGoumaxDataPtr, true);

                //执行扫描
                GouMaxDoSingleScan(m_IntGoumaxDataPtr);

                //指针转回结构体
                m_sttGoumaxData = Marshal.PtrToStructure<GouMaxDataParam>(m_IntGoumaxDataPtr);

            }
            catch (Exception e)
            {
                LogHelper.WriteLog(typeof(MainForm), e.Message);
            }
            finally
            {

            }
        }

        public void initStruct()
        {
            int i = 0;

            m_sttGoumaxData.m_pdblWavelengthArray = new double[22000];
            m_sttGoumaxData.m_byChannelValide = new byte[64];
            m_sttGoumaxData.m_pdblWavelengthArray= new double[22000];

            GouMaxlLossArray[] PPP = new GouMaxlLossArray[64];
            for (i = 0; i < 64; i++)
            {
                PPP[i].ilArray = new double[22000];
            }
            m_sttGoumaxData.m_pdblLossArrayP1 = PPP;

            m_sttGoumaxData.m_pdbl774xRefArray = new double[22000];
            m_sttGoumaxData.m_pdbl816xRefArray = new double[22000];

            int nSize = Marshal.SizeOf(m_sttGoumaxData);
            m_IntGoumaxDataPtr = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(m_sttGoumaxData, m_IntGoumaxDataPtr, true);

        }

    }
}
