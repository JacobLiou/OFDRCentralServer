using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ivi.Visa;
using NationalInstruments.Visa;
using System.Windows.Forms;
using System.Threading;

namespace SampleTcpServer
{
    class CN774X
    {
        private MessageBasedSession[] mbSession = new MessageBasedSession[16];

        private void initSessionArray()
        {
            for (int I = 0; I < mbSession.Length; I++)
            {
                mbSession[I] = null;
            }
        }
        public bool openSession(int nIndexN774X, string strAddress)
        {

            using (var rmSession = new ResourceManager())
            {
                try
                {
                    mbSession[nIndexN774X] = (MessageBasedSession)rmSession.Open(strAddress);

                }
                catch (InvalidCastException)
                {
                    MessageBox.Show("Resource selected must be a message-based session");
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.Message);
                }
                finally
                {
                    //Cursor.Current = Cursors.Default;
                }
            }
            return true;

        }

        private void closeSession(int nIndexN774X)
        {
            if (mbSession[nIndexN774X] != null)
            {
                mbSession[nIndexN774X].Dispose();
            }

        }

        public string query(int nIndexN774X, string textToWrite)
        {
            try
            {
                mbSession[nIndexN774X].RawIO.Write(textToWrite);
                return mbSession[nIndexN774X].RawIO.ReadString();
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            finally
            {
                //Cursor.Current = Cursors.Default;
            }
            return "";
        }

        public int write(int nIndexN774X, string textToWrite)
        {
            try
            {
                mbSession[nIndexN774X].RawIO.Write(textToWrite);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                return exp.HResult;
            }
            return 1;
        }

        public string readstring(int nIndexN774X)
        {

            try
            {
                return (mbSession[nIndexN774X].RawIO.ReadString());
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            finally
            {
                //Cursor.Current = Cursors.Default;
            }
            return "";
        }
        public byte[] readbyte(int nIndexN774X)
        {

            try
            {
                return (mbSession[nIndexN774X].RawIO.Read());
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            finally
            {
                //Cursor.Current = Cursors.Default;
            }
            return null;
        }
        //private string ReplaceCommonEscapeSequences(string s)
        //{
        //    return s.Replace("\\n", "\n").Replace("\\r", "\r");
        //}

        //private string InsertCommonEscapeSequences(string s)
        //{
        //    return s.Replace("\n", "\\n").Replace("\r", "\\r");
        //}

        /// <summary>
        /// 向N7745 发"*RST\n"
        /// </summary>
        /// <param name="nIndexN774X"></param>
        public void ResetDevice(int nIndexN774X)
        {
            string strCommand = "*RST\n";
            write(nIndexN774X, strCommand);
            Thread.Sleep(5000);
        }


        public struct tagScanParam
        {
            public UInt32 m_dwSize;
            public double m_dblStartWL,
            m_dblStopWL,
            m_dblStepSize;
            public int m_nSpeed;                //扫描速度   
            public int m_nAvgTime;              //功率计采样平均时间
            public double m_dblTLSPower,
            m_dblPWMPower;
            public UInt32 m_dwNumberOfScan;
            public UInt32 m_dwChannelNumber;
            public UInt32 m_dwChannelCfgHigh,
            m_dwChannelCfgLow;
            public UInt32 m_dwSampleCount;

            // For debug
            public byte m_bHP8164ATLSLowSSE;
        }
        public struct tagPMDeviceConfig
        {
            public byte bPMDevices;//总的PM设备个数，包含非774X的PM设备
            public byte bN774XPMDevices;//真实的N774X的个数
            string strArrayDeviceName;//所有PM设备名称
            string strArrayDriveKeyWords;//所有PM设备关键字
            string strArrayN774XDeviceName;//N774X设备名称
            string strArrayN774XDriveKeyWords;//N774X设备关键字
            public byte[] bDeviceMaxCh; // 最多支持10个PM设备
            public byte[] bN774XDeviceMaxCh;
            public byte[] bPMIndex;//最多支持10个PM设备
            public byte[,] bPM_Slot_Map; // The max scan data quantity is 64, every scan channel parameter includes PMIndex, SlotIndex and Device_CH
        }

        public tagPMDeviceConfig m_pstPMConfig;
        public tagScanParam m_pScanParam;
        private void InitialFromPMConfigCSV()
        {
            m_pstPMConfig.bDeviceMaxCh = new byte[10];
            m_pstPMConfig.bN774XDeviceMaxCh = new byte[10];
            m_pstPMConfig.bPMIndex = new byte[10];
            m_pstPMConfig.bPM_Slot_Map = new byte[70, 3];

        }

        /// <summary>
        /// 用来对N774X模块进行扫描初始化
        /// </summary>
        /// <param name="nIndexN774X"></param>
        /// <param name="lScanParam"></param>
        /// <returns></returns>
        public bool PrepareScan(int nIndexN774X)
        {
            int nAtimTime;
            double dbSetWL;
            string strReadBuffer = "";

            nAtimTime = (int)(((m_pScanParam.m_dblStopWL - m_pScanParam.m_dblStartWL) / (double)m_pScanParam.m_nSpeed / m_pScanParam.m_dwSampleCount) * 1E6 * 0.9);
            dbSetWL = (m_pScanParam.m_dblStopWL + m_pScanParam.m_dblStartWL) / 2;

            //for(int nIdx =1;nIdx <=)
            //mbSession[nIndexN774X].
            if (1 != write(nIndexN774X, string.Format("sens{0}:func: stat logg, stop\n", nIndexN774X)))
            {
                return false;
            }
            if (1 != write(nIndexN774X, string.Format("init{0}:cont 0\n", nIndexN774X)))
            {
                return false;
            }
            if (1 != write(nIndexN774X, string.Format("sens{0}:pow:rang:auto?\n", nIndexN774X)))
            {
                return false;
            }

            strReadBuffer = readstring(nIndexN774X);
            if (strReadBuffer == "")
            {
                return false;
            }

            int nRange = Convert.ToInt32(strReadBuffer);
            if (nRange != 0)
            {
                if (1 != write(nIndexN774X, string.Format("sens{0}:pow:rang:auto 0\n", nIndexN774X)))
                {
                    return false;
                }
            }

            if (1 != write(nIndexN774X, string.Format("trig{0}:inp sme\n", nIndexN774X)))
            {
                return false;
            }

            //设置功率计中心波长
            if (1 != write(nIndexN774X, string.Format("sens{0}:pow:wav {1:0.00}fnm\n", nIndexN774X, dbSetWL)))
            {
                return false;
            }

            //设置当前通道的Range，Range只能是整数
            if (1 != write(nIndexN774X, string.Format("sens{0}:pow:rang {1}DBM\n", nIndexN774X, m_pScanParam.m_dblPWMPower)))
            {
                return false;
            }
            //设置功率计为wm
            if (1 != write(nIndexN774X, string.Format("sens{0}:pow:unit 1\n", nIndexN774X)))
            {
                return false;
            }

            //设置功率计扫描点数及采样时间
            if (1 != write(nIndexN774X, string.Format("sens{0}:func:par:logg {1},{2}us\n", nIndexN774X, m_pScanParam.m_dwSampleCount, m_pScanParam.m_nAvgTime)))
            {
                return false;
            }

            //设置N7744的PASS模式，最后一个不用设置。
            if (1 != write(nIndexN774X, "TRIG:CONF 2\n"))
            {
                return false;
            }

            return true;
        }

        //////////////////////////////////////////////////////////////
        //函数说明：
        //    开始进行trigger Logging
        //    

        ///////////////////////////////////////////////////////////////
        bool StartTriggedLoggin(int nIndexN774X)
        {
            string chSendBuf;
            string strTemp;
            int nGetLength;
            try
            {



                if (1 != write(nIndexN774X, "INIT1:IMM\n"))
                {
                    return false;
                }

                chSendBuf = query(nIndexN774X, "trig1:outp?\n");
                if ("" == chSendBuf)
                {
                    return false;
                }


                if (1 != write(nIndexN774X, "trig1:outp avg\n"))
                {
                    return false;
                }




            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////
        //函数说明：
        //    设置功率计开始扫描

        //////////////////////////////////////////////////////////
        bool SetPMScanBegin(int nIndexN774X)
        {
                
            try
            {

                for (int nIndex = 1; nIndex <= (int)m_pScanParam.m_dwChannelNumber; nIndex++)
                {
                    if (nIndex > 8)
                    {
                        continue;
                    }

                    if (1 != write(nIndexN774X, string.Format("sens%{0}:func:stat logg,star\n", nIndexN774X)))
                    {
                        return false;
                    }


                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////
        //函数说明：
        //    当扫描完成后获得功率计扫描点数据

        /////////////////////////////////////////////////////////////////////
        bool getLambdaScanPMResult(int PWMChannel, double[] pdbPMArry, int PMIndex)
        {
            string chReadBuf;
            int nTempLen, nPMChannel;
            byte[] pbyBuf;//= new byte[20000];
            string strTemp;
            float fPreTemp = 0.0f;
            double dbPMTemp;
            float[] fPowerArray = new float[20000];
            int nGetLength;
            int SlotIndex = 0;


            // N774X channel should be decided by PMConfig.csv document
            nPMChannel = m_pstPMConfig.bPM_Slot_Map[PWMChannel, SlotIndex];


            try
            {
                //查询Log状态
                while (true)
                {


                    chReadBuf = query(PWMChannel, string.Format("sens{0}:func:stat?", PWMChannel));

                    if (chReadBuf.IndexOf("LOGGING_STABILITY,COMPLETE") != -1)
                    {
                        break;
                    }
                }

                //读取扫描结果

                chReadBuf = query(PWMChannel, string.Format("sens{0}:func:res?", PWMChannel));



                nTempLen = chReadBuf[1] - '0';
                //读取数据长度
                //ZeroMemory(chReadBuf, sizeof(chReadBuf));
                byte[] aa = new byte[1024 * 1024];
                aa = readbyte(PMIndex);
                if (chReadBuf == "")
                {
                    return false;
                }

                nTempLen = Convert.ToInt32(chReadBuf);



                pbyBuf = new byte[nTempLen + 100];
                /*
                  ZeroMemory(pbyBuf, sizeof(BYTE) * (nTempLen + 100));
                  switch (PMIndex)
                  {
                      case 0:
                          status = viRead(m_hN774X1, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      case 1:
                          status = viRead(m_hN774X2, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      case 2:
                          status = viRead(m_hN774X3, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      case 3:
                          status = viRead(m_hN774X4, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      case 4:
                          status = viRead(m_hN774X5, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      case 5:
                          status = viRead(m_hN774X6, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      case 6:
                          status = viRead(m_hN774X7, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      case 7:
                          status = viRead(m_hN774X8, (unsigned char *)pbyBuf,nTempLen + 100,&nGetLength);
                          break;
                      default:
                          break;
                  }
                  //	status = viRead(m_hN774X,(unsigned char*)pbyBuf,nTempLen,&nGetLength);
                  if (!DeviceStatusOK(status))
                  {
                      throw "Read N774X fun state error";
                  }
                  pfPowerArray = (float*)pbyBuf;

                  if (m_pScanParam->m_dwSampleCount < (DWORD)nTempLen / 4)
                  {
                      throw "当前扫描波长点数小于功率点数";
                  }
                  //计算功率由mw转换到dBm				
                  for (int nIndex = 0; nIndex < nTempLen / 4; nIndex++)
                  {
                      fPreTemp = *(pfPowerArray + nIndex);
                      if (fPreTemp < 0)
                      {
                          dbPMTemp = -80.0;
                      }
                      else
                      {
                          dbPMTemp = 10 * log10(*(pfPowerArray + nIndex) * 1000);

                          if (dbPMTemp < -80.0)
                          {
                              dbPMTemp = -80.0;
                          }
                      }

                      pdbPMArry[nIndex] = dbPMTemp;

                  }
                 }*/

            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////
        //函数说明：
        //    如果多次扫描，用来改变扫描的Range函数

        /////////////////////////////////////////////////////////////
        private bool ModifyScanRange(int nRange, int PMIndex)
        {

            try
            {
                write(PMIndex, string.Format("sens{0}:pow:rang {1}DBM\n", PMIndex, nRange));
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                return false;
            }
            return true;

        }

    }
}
