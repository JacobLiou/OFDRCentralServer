using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TcpServer;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using AgServerFSILLib;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SampleTcpServer
{
    public partial class MainForm : Form
    {

        int InitEngineFS()
        {
            int nAct = 0;
            int nCountTime = 0;
            myMgr = new AgServerFSILLib.EngineMgr();
            myEng = myMgr.NewEngine();
            // C:\Users\Public\Documents\Photonic Application Suite\AgEngineFSIL.agconfig

            myEng.LoadConfiguration("C:\\Users\\Public\\Documents\\Photonic Application Suite\\AgEngineFSIL.agconfig");
            myEng.Activate();
            //MessageBox.Show("AAA");
            while (nAct == 0)
            {
                Thread.Sleep(1000);
                nAct = myEng.Active;

                if (nCountTime > 30)
                {
                    if (myEng.UserInputPrompt != null)
                    {
                        MessageBox.Show(myEng.UserInputPrompt, "error");
                        return 0;
                    }
                    MessageBox.Show(myEng.UserInputPrompt + myEng.UserInputChoice, "error");
                    break;
                }
                nCountTime++;
            }

            //myEng.LoadReference("FSILRef.omr");
            int Supportstwoway = myEng.SupportsTwoWay;
            int twoway = myEng.UseTwoway;
            myEng.UseTwoway = 0;
            twoway = myEng.UseTwoway;
            return 1;
        }



        void ReadGasCellConfig()
        {


        }
        int InitEngineSlowIL()
        {
            int nAct = 0;
            int nCountTime = 0;
            myMgrSlow = new AgServerILLib.EngineMgr();
            myEngSlow = myMgrSlow.NewEngine();
            // C:\Users\Public\Documents\Photonic Application Suite\AgEngineFSIL.agconfig

            //myEng.LoadConfiguration("C:\\Users\\Public\\Documents\\Photonic Application Suite\\AgEngineFSIL.agconfig");
            myEngSlow.Activate();
            while (nAct == 0)
            {
                Thread.Sleep(100);
                nAct = myEngSlow.Active;

                if (nCountTime > 300)
                {
                    if (myEngSlow.UserInputPrompt != null)
                    {
                        MessageBox.Show(myEngSlow.UserInputPrompt, "error");
                        return 0;
                    }
                }
                nCountTime++;
            }


            return 1;
        }

        int InitEnginePDL()
        {
            int nAct = 0;
            int nCountTime = 0;
            int nbusy = 0;
            myMgrPDL = new AgServerILPDLLib.EngineMgr();
            myEngPDL = myMgrPDL.NewEngine();
            // C:\Users\Public\Documents\Photonic Application Suite\AgEngineFSIL.agconfig

            myEngPDL.LoadConfiguration("C:\\Users\\Public\\Documents\\Photonic Application Suite\\PreActiveILPDL.agconfig");
            myEngPDL.Activate();
            while (nAct == 0)
            {
                Thread.Sleep(100);
                nAct = myEngPDL.Active;

                if (nCountTime > 300)
                {
                    if (myEngPDL.UserInputPrompt != null)
                    {
                        MessageBox.Show(myEngPDL.UserInputPrompt, "error");
                        return 0;
                    }
                }
                nCountTime++;
            }


            return 1;
        }


        void PDLEngineDoReference()
        {
            myEngPDL.StartCustomReferenceSinglePort(0);
            int nbusy = myEngPDL.Busy;
            string straa = myEngPDL.UserInputPrompt;
            while (nbusy == 1)
            {
                Thread.Sleep(1000);
                nbusy = myEngPDL.Busy;
                straa = myEngPDL.UserInputPrompt;
            }
            int[] ay_copy = { 1, 2, 3, 4, 5, 6, 7 };
            myEngPDL.CopyReferenceChannel(0, ay_copy);
        }

        public object Deserialize(Type type, string strxml)
        {

            using (StringReader sr = new StringReader(strxml))
            {
                XmlSerializer xmldes = new XmlSerializer(type);
                return xmldes.Deserialize(sr);
            }
        }
        void start()
        {

            double dbStart = 0;
            double dbStop = 0;
            double dbStep = 0;

            string xmlSetup = "";
            //设置每个通道的功率RANGE
            switch (m_Scan_Driver_type)
            {
                case 1: //FSIL ENGINE
                    {
                        dbStart = myEng.WavelengthStart;
                        dbStop = myEng.WavelengthStop;
                        dbStep = myEng.WavelengthStep * 0.001;
                        int nNumOfScan = myEng.NumberOfScans;
                        xmlSetup = myEng.ChannelSetup;
                        myEng.ChannelSetup = XMLChannelSetup(myEng.ChannelSetup);
                    }
                    break;
                case 2: //SCPI 
                    {
                        dbStart = m_dbl_SCPI_START_WL;
                        dbStop = m_dbl_SCPI_STOP_WL;
                        dbStep = m_dbl_SCPI_STEP_WL;
                        SCPI_ChannelSetup();
                    }
                    break;
                case 3: //IL ENGINE
                    {
                        dbStart = myEngSlow.WavelengthStart;
                        dbStop = myEngSlow.WavelengthStop;
                        dbStep = myEngSlow.WavelengthStep * 0.001;
                        int nNumOfScan = myEngSlow.NumberOfScans;
                        xmlSetup = myEngSlow.ChannelSetup;
                    }
                    break;
                case 4: //PDL ENGINE
                    {
                        dbStart = myEngPDL.WavelengthStart;
                        dbStop = myEngPDL.WavelengthStop;
                        //double sweep = myEngPDL.SweepRate;

                        // Sets or gets the sweep rate in nm / s.Sweep rate v in nm / s and step size s in pm are related by
                        //0.3 x v[nm / s] = s[pm](e.g. 20 nm / s yields 6 pm step size)
                        dbStep = myEngPDL.SweepRate * 0.3;
                        //int nNumOfScan = myEngPDL.NumberOfScans;
                        xmlSetup = myEngPDL.ChannelSetup;
                    }
                    break;
                case 5: //goumax  ENGINE
                    {

                        //for (int i = 0; i < m_GouMax_Scan.m_sttGoumaxData.m_byChannelValide.Length; i++)
                        //{
                        //m_GouMax_Scan.m_sttGoumaxData.m_byChannelValide[i] = 0;
                        //}

                        dbStart = m_dbl_SCPI_START_WL;
                        dbStop = m_dbl_SCPI_STOP_WL;
                        dbStep = m_dbl_SCPI_STEP_WL;
                    }
                    break;
                case 44:
                    //dummy environment,do nothing
                    dbStart = dbl_dummy_wvstart;
                    dbStop = dbl_dummy_wvstop;
                    dbStep = dbl_dummy_wvstep;


                    break;
            }


            long lnow = System.DateTime.Now.Ticks;
            int nSec = Convert.ToInt32((dbStop - dbStart) / dbStep) + 1;
            string straa = "";
            //启动测试
            switch (m_Scan_Driver_type)
            {
                case 1:
                    LogHelper.WriteLog(typeof(MainForm), "FS IL Engine StartMeasurement()");
                    straa = myEng.UserInputPrompt;
                    myEng.StartMeasurement();
                    break;
                case 2:
                    //SCPI Scan will Block util scan completed!!!
                    LogHelper.WriteLog(typeof(MainForm), "SCPI  StartMeasurement()");
                    switch (m_n_SCPI_PDL_state)
                    {
                        case 1:
                            m_n_SCPI_PDL_state = 2;
                            break;
                        case 2:
                            m_n_SCPI_PDL_state = 3;
                            break;
                        case 3:
                            m_n_SCPI_PDL_state = 4;
                            break;
                        case 4:
                            m_n_SCPI_PDL_state = 1;
                            break;
                        default:
                            m_n_SCPI_PDL_state = 1;
                            break;
                    }

                    m_SCPI_Scan.SCPI_SETANGLES(m_n_SCPI_PDL_state);


                    if (!m_SCPI_Scan.SCPI_DoScan())
                    {
                        LogHelper.WriteLog(typeof(MainForm), "m_SCPI_Scan.SCPI_DoScan() execution error,exit function start()");
                        FeedBackErrorMSG("m_SCPI_Scan.SCPI_DoScan() execution error,exit function start().");
                    }
                    LogHelper.WriteLog(typeof(MainForm), "SCPI  completeMeasurement()");
                    break;
                case 3:
                    LogHelper.WriteLog(typeof(MainForm), "slow IL Engine StartMeasurement()");
                    straa = myEngSlow.UserInputPrompt;
                    myEngSlow.StartMeasurement();
                    break;
                case 4:
                    LogHelper.WriteLog(typeof(MainForm), "PDL Engine StartMeasurement()");
                    straa = myEngPDL.UserInputPrompt;
                    myEngPDL.StartMeasurement();
                    break;
                case 5:
                    LogHelper.WriteLog(typeof(MainForm), "Goumax Engine StartMeasurement()");
                    m_GouMax_Scan.GouMax_GouMaxDoSingleScan();
                    break;
                case 44:
                    //dummy environment,do nothing
                    break;

            }

            if (m_Scan_Driver_type == 1 || m_Scan_Driver_type == 3 || m_Scan_Driver_type == 4 || m_Scan_Driver_type == 44)
            {
                int nbusy = 1, nbusyOld=0;
                int ntime = 1;
                string strError = "";
                int nSleep = 0;
                string strMsgcmp = "";

                while (nbusy == 1)
                {
                    Thread.Sleep(100);
                    string strA = "";

                    if (m_Scan_Driver_type == 1)
                    {
                        nbusy = myEng.Busy;
                        strA = myEng.UserInputPrompt;

                    }
                    else if (m_Scan_Driver_type == 3)
                    {
                        nbusy = myEngSlow.Busy;
                        strA = myEngSlow.UserInputPrompt;

                    }
                    else if (m_Scan_Driver_type == 4)
                    {
                        nbusy = myEngPDL.Busy;
                        strA = myEngPDL.UserInputPrompt;

                    }
                    else if (m_Scan_Driver_type == 44)
                    {
                        //dummy environment
                        nSleep++;
                        if (nSleep > m_Dummy_Environment_Sleep_Time)
                        {
                            break;
                        }

                    }


                    ShowDataStatus(nbusy);
                    //Debug.WriteLine("busy ={0},times = {1},UserInputPrompt = {2}", nbusy, ntime,strA);
                    if (strA != null)
                    {
                        //Debug.WriteLine("user prompt string is {0}, input waiting  is {1}", myEng.UserInputPrompt, myEng.UserInputWaiting);


                        if (strA.IndexOf("TLS Lambda zeroing") > -1)
                        {


                            if (m_Scan_Driver_type == 1)
                            {
                                myEng.UserInputResponse(6);
                                myEng.UserInputWaiting = 0;

                            }
                            else if (m_Scan_Driver_type == 3)
                            {
                                myEngSlow.UserInputResponse(6);
                                myEngSlow.UserInputWaiting = 0;

                            }
                            else if (m_Scan_Driver_type == 4)
                            {
                                myEngPDL.UserInputResponse(6);
                                myEngPDL.UserInputWaiting = 0;

                            }
                            if (strMsgcmp != strA)
                            {
                                LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                                strMsgcmp = strA;
                            }

                            //FeedBackErrorMSG("TLS Lambda zeroing is performing, please waiting... ");
                            TLSLamdaZeroingMutex.WaitOne();
                            m_bTLSLamdaZeroing = true;
                            TLSLamdaZeroingMutex.ReleaseMutex();
                            FeedBackErrorMSG("TLS Lambda zeroing 正在执行，请等待...");

                        }
                        else if (strA.IndexOf("Overrange or high reference loss detected.") > -1)
                        {


                            if (m_Scan_Driver_type == 1)
                            {
                                myEng.UserInputResponse(1);
                                myEng.UserInputWaiting = 0;

                            }
                            else if (m_Scan_Driver_type == 3)
                            {
                                myEngSlow.UserInputResponse(1);
                                myEngSlow.UserInputWaiting = 0;

                            }
                            else if (m_Scan_Driver_type == 4)
                            {
                                myEngPDL.UserInputResponse(1);
                                myEngPDL.UserInputWaiting = 0;

                            }
                            if (strMsgcmp != strA)
                            {
                                LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                                strMsgcmp = strA;
                            }
                            FeedBackErrorMSG("Overrange or high reference loss detected.");

                        }
                        else if (strA.IndexOf("Evaluation yielded no data.\nCheck connections, run zeroing or try\nselecting a smaller resolution setting\nor a larger wavelength range and make\nsure IL deembedding settings are correct.") > -1)
                        {
                            //PDLEngineDoReference();
                            if (strMsgcmp != strA)
                            {
                                LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                                strMsgcmp = strA;
                            }
                            FeedBackErrorMSG("Evaluation yielded no data.\nCheck connections, run zeroing or try\nselecting a smaller resolution setting\nor a larger wavelength range and make\nsure IL deembedding settings are correct.");
                        }


                        /*Warning: TLS module out of specification:
                        You should power cycle the instrument or run
                        a realign operation from the
                        TLS front panel menu before continuing.
                        Press OK to continue or Cancel to stop measurement.*/
                        else if (strA.IndexOf("Warning: TLS module out of specification:") > -1)
                        {
                            if (m_Scan_Driver_type == 1)
                            {
                                //Engine.UserInputChoice.
                                myEng.UserInputResponse(1);
                                myEng.UserInputWaiting = 0;

                            }
                            else if (m_Scan_Driver_type == 3)
                            {
                                myEngSlow.UserInputResponse(1);
                                myEngSlow.UserInputWaiting = 0;

                            }
                            else if (m_Scan_Driver_type == 4)
                            {
                                myEngPDL.UserInputResponse(1);
                                myEngPDL.UserInputWaiting = 0;

                            }
                            if (strMsgcmp != strA)
                            {
                                LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                                strMsgcmp = strA;
                            }
                            FeedBackErrorMSG(strA);

                        }
                        else
                        {
                            if (strMsgcmp != strA)
                            {
                                LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                                strMsgcmp = strA;
                            }
                            FeedBackErrorMSG(strA);
                        }

                    }
                    ntime++;
                }

            }


            LogHelper.WriteLog(typeof(MainForm), "FS IL get MeasurementResult");

            AgServerFSILLib.IOMRFile result = null;
            AgServerILLib.IOMRFile resultSlow = null;
            AgServerILPDLLib.IOMRFile resultPDL = null;

            if (m_Scan_Driver_type == 1)
            {
                result = myEng.MeasurementResult;
            }
            else if (m_Scan_Driver_type == 3)
            {
                resultSlow = myEngSlow.MeasurementResult;

            }
            else if (m_Scan_Driver_type == 4)
            {
                resultPDL = myEngPDL.MeasurementResult;

            }
            else if (m_Scan_Driver_type == 44)
            {
                //dummy environment ,do nothing;

            }


            LogHelper.WriteLog(typeof(MainForm), string.Format("scan time = {0}", (System.DateTime.Now.Ticks - lnow) / 10000));

            //Array a1 = result.GraphNames;
            if (m_Scan_Driver_type == 1)
            {
                m_nProtocolIndex = myEng.ProtocolMax;
            }
            else if (m_Scan_Driver_type == 3)
            {
                m_nProtocolIndex = myEngSlow.ProtocolMax;
            }
            else if (m_Scan_Driver_type == 4)
            {
                m_nProtocolIndex = myEngPDL.ProtocolMax;
            }

            if (scanRequestMutex.WaitOne())
            {
                LogHelper.WriteLog(typeof(MainForm), "start() entered scanRequestMutex.WaitOne()");

                for (int i = 0; i < m_listScanList.Count; i++)
                {
                    clStructList nj = m_listScanList[i];
                    if (nj.shStatus == Queue_Doing_Scan)
                    {
                        //Debug.WriteLine("check 101  ={0}", System.DateTime.Now.Ticks );

                        //string[] split = nj.strIndexOfPWMChannels.Split(',');
                        nj.nNumberofExecutions++;

                        //如果是8169PDL扫描，
                        if (m_Scan_Driver_type == 2)
                        {
                            if (nj.strDoPDL == "1" && nj.strNumberOfScan == "1")
                            {
                                ///如果 实际扫描次数大于3，才设置状态为 扫描完成，否则就保持状态为  Queue_Doing_Scan
                                if (nj.nNumberofExecutions > 7)
                                {
                                    nj.shStatus = Queue_Completed_Scan;
                                }
                                else
                                {///否则重新加入扫描队列
                                    nj.shStatus = Queue_Waiting_Scan;
                                }
                            }
                            else if (nj.strDoPDL == "1")
                            {
                                ///如果 实际扫描次数大于3，才设置状态为 扫描完成，否则就保持状态为  Queue_Doing_Scan
                                if (nj.nNumberofExecutions > 3)
                                {
                                    nj.shStatus = Queue_Completed_Scan;
                                }
                                else
                                {///否则重新加入扫描队列
                                    nj.shStatus = Queue_Waiting_Scan;
                                }
                            }
                            else
                            {
                                if (nj.nNumberofExecutions > 1)
                                {
                                    nj.shStatus = Queue_Completed_Scan;
                                }
                                else
                                {///否则重新加入扫描队列
                                    nj.shStatus = Queue_Waiting_Scan;
                                }
                            }

                        }
                        else if (m_Scan_Driver_type == 5)
                        {
                            //GOUMAX 是自动扫描两档，不用判断nj.strNumberOfScan  是否扫描两档
                            nj.shStatus = Queue_Completed_Scan;
                        }
                        else
                        {
                            //如果是 要求扫描次数是 1，立即设置状态为 扫描完成
                            if (nj.strNumberOfScan == "0")
                            {
                                nj.shStatus = Queue_Completed_Scan;
                            }
                            else
                            {

                                ///如果 实际扫描次数大于1，才设置状态为 扫描完成，否则就保持状态为  Queue_Doing_Scan
                                if (nj.nNumberofExecutions > 1)
                                {
                                    nj.shStatus = Queue_Completed_Scan;
                                }
                                else
                                {///否则重新加入扫描队列
                                    nj.shStatus = Queue_Waiting_Scan;
                                }

                            }
                        }


                    }

                }

                scanRequestMutex.ReleaseMutex();
                LogHelper.WriteLog(typeof(MainForm), "start() scanRequestMutex.ReleaseMutex()");
            }
            TLSLamdaZeroingMutex.WaitOne();
            m_bTLSLamdaZeroing = false;
            TLSLamdaZeroingMutex.ReleaseMutex();
            LogHelper.WriteLog(typeof(MainForm), "11111start() writeFilesRequestMutex.WaitOne()");
            if (writeFilesRequestMutex.WaitOne())
            {
                LogHelper.WriteLog(typeof(MainForm), "start() writeFilesRequestMutex.WaitOne()");
                if (m_Scan_Driver_type == 1)
                {
                    graph = result.Graph["RXTXAvgIL"];
                    if (m_nWVCalibrateEnable == 1)
                    {
                        if (FSILCalibrateGasCellWV(graph) < 1)
                        {
                            FeedBackErrorMSG("Gas cell calibrate wavelength error,test already exit!");
                        }
                    }

                }
                else if (m_Scan_Driver_type == 3)
                {
                    graphSlow = resultSlow.Graph["RXTXAvgIL"];
                }
                else if (m_Scan_Driver_type == 4)
                {
                    graphPDL = resultPDL.Graph["RXTXAvgIL"];
                    graphPDL_PDL = resultPDL.Graph["TLS0_RXTXPDL"];
                }
                else if (m_Scan_Driver_type == 44)
                {
                    //dummy environment ,do nothing;
                }

                writeFilesRequestMutex.ReleaseMutex();
                LogHelper.WriteLog(typeof(MainForm), "start() writeFilesRequestMutex.ReleaseMutex()");
            }

            writeFilesRequestBK.RunWorkerAsync();

        }
        /// <summary>
        /// 开机做归零，为MONITOR 做 Reference.
        /// </summary>
        void StartupDoReference()
        {

            double dbStart = myEng.WavelengthStart;
            double dbStop = myEng.WavelengthStop;
            double dbStep = myEng.WavelengthStep * 0.001;


            int PWMAutoRanging = myEng.PWMAutoRanging;
            int PWMAutoRangingRef = myEng.PWMAutoRangingRef;
            double PWMAvgTime = myEng.PWMAvgTime;
            int PWMAvgTimeAuto = myEng.PWMAvgTimeAuto;
            int PWMCount = myEng.PWMCount;
            double PWMMinimumRange = myEng.PWMMinimumRange;
            double PWMSensitivity = myEng.PWMSensitivity;
            double PWMSensitivityRef = myEng.PWMSensitivityRef;
            double RangeDecrement = myEng.RangeDecrement;
            double RangeDecrementRef = myEng.RangeDecrementRef;


            int nNumOfScan = myEng.NumberOfScans;
            //myEng.KeepRawData = 1;
            string xmlSetup = myEng.ChannelSetup;
            //myEng.ChannelSetup = XMLChannelSetup(myEng.ChannelSetup);


            long lnow = System.DateTime.Now.Ticks;
            int nSec = Convert.ToInt32((dbStop - dbStart) / dbStep) + 1;
            string straa = myEng.UserInputPrompt;
            LogHelper.WriteLog(typeof(MainForm), "FS IL StartDoReference()");


            string strProtocol = myEng.ProtocolText;
            int nProMax = myEng.ProtocolMax;
            int nProMin = myEng.ProtocolMin;
            string strP1 = myEng.GetProtocolTextAt(nProMax - 1, nProMax);

            myEng.StartMeasurement();
            int nbusy = 1;

            while (nbusy == 1)
            {

                nbusy = myEng.Busy;
                string strA = myEng.UserInputPrompt;
                //Debug.WriteLine("busy ={0},times = {1},UserInputPrompt = {2}", nbusy, ntime,strA);
                if (strA != null)
                {
                    //Debug.WriteLine("user prompt string is {0}, input waiting  is {1}", myEng.UserInputPrompt, myEng.UserInputWaiting);
                    string strB = myEng.UserInputChoice;
                    if (strA.IndexOf("TLS Lambda zeroing") > -1)
                    {
                        myEng.UserInputResponse(6);
                        myEng.UserInputWaiting = 0;
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                        //FeedBackErrorMSG("TLS Lambda zeroing is performing, please waiting... ");
                        TLSLamdaZeroingMutex.WaitOne();
                        m_bTLSLamdaZeroing = true;
                        TLSLamdaZeroingMutex.ReleaseMutex();
                        FeedBackErrorMSG("TLS Lambda zeroing 正在执行，请等待...");

                    }
                    else if (strA.IndexOf("Overrange or high reference loss detected.") > -1)
                    {
                        myEng.UserInputResponse(1);
                        myEng.UserInputWaiting = 0;
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                        //FeedBackErrorMSG("Overrange or high reference loss detected.");

                    }
                    else
                    {
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                    }

                }
            }

            m_bTLSLamdaZeroing = false;

            LogHelper.WriteLog(typeof(MainForm), "FS IL get Reference Result");
            IOMRFile result = myEng.MeasurementResult;
            LogHelper.WriteLog(typeof(MainForm), string.Format("Reference time = {0}", (System.DateTime.Now.Ticks - lnow) / 10000));

            graph = result.Graph["RXTXAvgIL"];
            Array y = graph.YData;
            int noChannels = graph.noChannels;
            WriteReferenceFile(y, noChannels, dbStart, dbStep, dbStop);
        }


        void StartupDoReferenceSlow()
        {

            double dbStart = myEngSlow.WavelengthStart;
            double dbStop = myEngSlow.WavelengthStop;
            double dbStep = myEngSlow.WavelengthStep * 0.001;




            //int PWMAutoRanging = myEngSlow.PWMAutoRanging;
            //int PWMAutoRangingRef = myEngSlow.PWMAutoRangingRef;
            //double PWMAvgTime = myEngSlow.PWMAvgTime;
            //int PWMAvgTimeAuto = myEngSlow.PWMAvgTimeAuto;
            //int PWMCount = myEngSlow.PWMCount;
            //double PWMMinimumRange = myEngSlow.PWMMinimumRange;
            //double PWMSensitivity = myEngSlow.PWMSensitivity;
            //double PWMSensitivityRef = myEngSlow.PWMSensitivityRef;
            //double RangeDecrement = myEngSlow.RangeDecrement;
            //double RangeDecrementRef = myEngSlow.RangeDecrementRef;



            int nNumOfScan = myEngSlow.NumberOfScans;
            //myEngSlow.KeepRawData = 1;
            string xmlSetup = myEngSlow.ChannelSetup;
            //myEngSlow.ChannelSetup = XMLChannelSetup(myEngSlow.ChannelSetup);
            XMLChannelSLOWSetup(myEngSlow.ChannelSetup);

            long lnow = System.DateTime.Now.Ticks;
            int nSec = Convert.ToInt32((dbStop - dbStart) / dbStep) + 1;
            string straa = myEngSlow.UserInputPrompt;
            LogHelper.WriteLog(typeof(MainForm), "IL StartDoReference()");


            string strProtocol = myEngSlow.ProtocolText;
            int nProMax = myEngSlow.ProtocolMax;
            int nProMin = myEngSlow.ProtocolMin;
            string strP1 = myEngSlow.GetProtocolTextAt(nProMax - 1, nProMax);

            myEngSlow.StartMeasurement();
            int nbusy = 1;

            while (nbusy == 1)
            {

                nbusy = myEngSlow.Busy;
                string strA = myEngSlow.UserInputPrompt;
                //Debug.WriteLine("busy ={0},times = {1},UserInputPrompt = {2}", nbusy, ntime,strA);
                if (strA != null)
                {
                    //Debug.WriteLine("user prompt string is {0}, input waiting  is {1}", myEngSlow.UserInputPrompt, myEngSlow.UserInputWaiting);
                    string strB = myEngSlow.UserInputChoice;
                    if (strA.IndexOf("TLS Lambda zeroing") > -1)
                    {
                        myEngSlow.UserInputResponse(6);
                        myEngSlow.UserInputWaiting = 0;
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                        //FeedBackErrorMSG("TLS Lambda zeroing is performing, please waiting... ");
                        TLSLamdaZeroingMutex.WaitOne();
                        m_bTLSLamdaZeroing = true;
                        TLSLamdaZeroingMutex.ReleaseMutex();
                        FeedBackErrorMSG("TLS Lambda zeroing 正在执行，请等待...");

                    }
                    else if (strA.IndexOf("Overrange or high reference loss detected.") > -1)
                    {
                        myEngSlow.UserInputResponse(1);
                        myEngSlow.UserInputWaiting = 0;
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                        //FeedBackErrorMSG("Overrange or high reference loss detected.");

                    }
                    else
                    {
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                    }

                }
            }


            LogHelper.WriteLog(typeof(MainForm), "IL get Reference Result");
            AgServerILLib.IOMRFile result = myEngSlow.MeasurementResult;
            LogHelper.WriteLog(typeof(MainForm), string.Format("Reference time = {0}", (System.DateTime.Now.Ticks - lnow) / 10000));

            graphSlow = result.Graph["RXTXAvgIL"];
            Array y = graphSlow.YData;
            int noChannels = graphSlow.noChannels;
            WriteReferenceFile(y, noChannels, dbStart, dbStep, dbStop);
        }
        void StartupDoReferencePDL()
        {

            double dbStart = myEngPDL.WavelengthStart;
            double dbStop = myEngPDL.WavelengthStop;
            double dbStep = myEngPDL.SweepRate * 0.3 * 0.001;




            //int PWMAutoRanging = myEngPDL.PWMAutoRanging;
            //int PWMAutoRangingRef = myEngPDL.PWMAutoRangingRef;
            //double PWMAvgTime = myEngPDL.PWMAvgTime;
            //int PWMAvgTimeAuto = myEngPDL.PWMAvgTimeAuto;
            //int PWMCount = myEngPDL.PWMCount;
            //double PWMMinimumRange = myEngPDL.PWMMinimumRange;
            //double PWMSensitivity = myEngPDL.PWMSensitivity;
            //double PWMSensitivityRef = myEngPDL.PWMSensitivityRef;
            //double RangeDecrement = myEngPDL.RangeDecrement;
            //double RangeDecrementRef = myEngPDL.RangeDecrementRef;



            int nNumOfScan = 0;
            //myEngPDL.KeepRawData = 1;
            string xmlSetup = myEngPDL.ChannelSetup;
            XMLChannelPDLSetup(myEngPDL.ChannelSetup);


            long lnow = System.DateTime.Now.Ticks;
            int nSec = Convert.ToInt32((dbStop - dbStart) / dbStep) + 1;
            string straa = myEngPDL.UserInputPrompt;
            LogHelper.WriteLog(typeof(MainForm), "PDL IL StartDoReference()");


            string strProtocol = myEngPDL.ProtocolText;
            int nProMax = myEngPDL.ProtocolMax;
            int nProMin = myEngPDL.ProtocolMin;
            string strP1 = myEngPDL.GetProtocolTextAt(nProMax - 1, nProMax);

            myEngPDL.StartMeasurement();
            int nbusy = 1;

            while (nbusy == 1)
            {
                Thread.Sleep(1000);

                nbusy = myEngPDL.Busy;
                string strA = myEngPDL.UserInputPrompt;
                //Debug.WriteLine("busy ={0},times = {1},UserInputPrompt = {2}", nbusy, ntime,strA);
                if (strA != null)
                {
                    //Debug.WriteLine("user prompt string is {0}, input waiting  is {1}", myEngPDL.UserInputPrompt, myEngPDL.UserInputWaiting);
                    string strB = myEngPDL.UserInputChoice;
                    if (strA.IndexOf("TLS Lambda zeroing") > -1)
                    {
                        myEngPDL.UserInputResponse(6);
                        myEngPDL.UserInputWaiting = 0;
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                        //FeedBackErrorMSG("TLS Lambda zeroing is performing, please waiting... ");
                        TLSLamdaZeroingMutex.WaitOne();
                        m_bTLSLamdaZeroing = true;
                        TLSLamdaZeroingMutex.ReleaseMutex();
                        FeedBackErrorMSG("TLS Lambda zeroing 正在执行，请等待...");

                    }
                    else if (strA.IndexOf("Overrange or high reference loss detected.") > -1)
                    {
                        myEngPDL.UserInputResponse(1);
                        myEngPDL.UserInputWaiting = 0;
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                        //FeedBackErrorMSG("Overrange or high reference loss detected.");

                    }
                    else
                    {
                        LogHelper.WriteLog(typeof(MainForm), string.Format("Eng.UserInputPrompt = {0}", strA));
                    }

                }
            }


            LogHelper.WriteLog(typeof(MainForm), "IL get Reference Result");
            AgServerILPDLLib.IOMRFile result = myEngPDL.MeasurementResult;
            LogHelper.WriteLog(typeof(MainForm), string.Format("Reference time = {0}", (System.DateTime.Now.Ticks - lnow) / 10000));

            graphPDL = result.Graph["RXTXAvgIL"];
            Array y = graphPDL.YData;
            int noChannels = graphPDL.noChannels;
            double dblxStart = graphPDL.xStart * 1e9;
            double dblxStop = graphPDL.xStop * 1e9;
            double dblxStep = graphPDL.xStep * 1e9;

            WriteReferencePDLFile(y, noChannels, dblxStart, dblxStep, dblxStop);
        }

        void StartupDoReferenceGOUMAX()
        {
            m_GouMax_Scan.GouMax_GouMaxDoSingleScan();
        }

        /// <summary>
        /// write data to Reference file
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        void WriteReferenceFile(Array a, int noChannels, double wvstart, double wvstep, double wvstop)
        {
            int n = 0;
            int xLen = a.Length;
            int num = xLen / noChannels;

            if (!Directory.Exists("./reference/"))
            {
                Directory.CreateDirectory("./reference/");
            }


            //for (n = 1; n <= noChannels; n++)
            n = 1;
            {
                string strFile;
                string[] strMF = m_listMF[n - 1].Split('.');
                int mfIndex = Convert.ToInt32(strMF[0].Substring(2));
                //strFile= string.Format("./testdata/test_fs_{0}.dat", n);
                strFile = "./reference/reference.csv";

                FileStream fs = new FileStream(strFile, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                //StreamWriter sw = new StreamWriter(fs);

                double dbl = 0.0f;
                double dblWV = 0.0f;

                List<double> listdbl = new List<double>();
                int m = 0;



                for (m = 0; m < num; m++)
                {
                    dblWV = (wvstart + wvstep * m);
                    dbl = Convert.ToDouble(a.GetValue(m));
                    if (double.IsNaN(dbl))
                    {
                        dbl = -80.0f;
                    }
                    m_listReference.Add(dbl);
                    sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                }

                sw.Flush();
                sw.Close();
                fs.Close();
            }

        }




        /// <summary>
        /// write data to Reference file
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        void WriteReferencePDLFile(Array a, int noChannels, double wvstart, double wvstep, double wvstop)
        {
            int n = 0;
            int xLen = a.Length;
            int num = xLen / noChannels;

            if (!Directory.Exists("./reference/"))
            {
                Directory.CreateDirectory("./reference/");
            }


            //for (n = 1; n <= noChannels; n++)
            n = 1;
            {
                string strFile;
                strFile = "./reference/reference.csv";

                FileStream fs = new FileStream(strFile, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);


                double dbl = 0.0f;
                double dblWV = 0.0f;

                List<double> listdbl = new List<double>();
                int m = 0;

                for (m = 0; m < num; m++)
                {
                    dblWV = (wvstart + wvstep * m);
                    dbl = Convert.ToDouble(a.GetValue(m));
                    if (double.IsNaN(dbl))
                    {
                        dbl = -80.0f;
                    }
                    m_listReference.Add(dbl);

                    sw.WriteLine(string.Format("{0:0.000},{1:0.00000000}", dblWV, dbl));
                    if (m < (num - 1))
                    {
                        int nInnerValue = 0;
                        int nInnerStep = Convert.ToInt32(wvstep * 1000);
                        double dblInner1 = 0, dblInner2 = 0, dblDiff = 0;

                        dblInner1 = Convert.ToDouble(a.GetValue(m));
                        dblInner2 = Convert.ToDouble(a.GetValue(m + 1));
                        dblDiff = (dblInner2 - dblInner1) / nInnerStep;


                        for (nInnerValue = 1; nInnerValue < nInnerStep; nInnerValue++)
                        {
                            dblWV = (wvstart + wvstep * m) + nInnerValue / 1000.000;
                            dbl = dblInner1 + dblInner1 * dblDiff * nInnerValue;
                            m_listReference.Add(dbl);
                            sw.WriteLine(string.Format("{0:0.000},{1:0.00000000}", dblWV, dbl));
                        }

                    }
                }

                sw.Flush();
                sw.Close();
                fs.Close();
            }

        }

        private static void EndWriteCallback(IAsyncResult asr)
        {
            using (Stream str = (Stream)asr.AsyncState)
            {
                str.EndWrite(asr);
                //Console.WriteLine("异步写入结束");
            }
        }


        /// <summary>
        /// check wv with gas cell
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        int FSILCalibrateGasCellWV(IOMRGraph graph)
        {
            //double[] anyVariableWV = (double[])graph.XData;
            double[] anyVariableIL = (double[])graph.YData;
            int nlen = graph.YData.Length / graph.noChannels;
            int nSourceIndex = nlen * (m_n_GAS_CELL_CHANNEL - 1);
            double[] tempIL = new double[nlen];
            List<double> listWV = new List<double>();

            double dblwv = graph.xStart;
            double dblwvstep = graph.xStep;
            double dblwvstop = graph.xStop + 1e-16;
            for (dblwv = graph.xStart; dblwv <= dblwvstop; dblwv += dblwvstep)
            {
                listWV.Add(dblwv);
            }
            //copy IL  从需要的INDEX 开始
            Array.Copy(anyVariableIL, nSourceIndex, tempIL, 0, nlen);
            //List<double> list1 = new List<double>(anyVariableWV);
            List<double> list2 = new List<double>(tempIL);
            double dblReturn = 0;
            foreach (Gas_Cell_Calibrate nj in m_listWVCalibrate)
            {
                dblReturn = SearchMaxIL_index(listWV, list2, nj.dblLeftWV, nj.dblRightWV, nj.dblCW);
                if (dblReturn > m_nWVCalibrateOffset)
                {
                    //Console.WriteLine(string.Format("ITU {0} has error", nj.dblCW));
                    LogHelper.WriteLog(typeof(MainForm), string.Format("Gas cell ITU {0} has error", nj.dblCW));

                    return 0;
                }

            }

            return 1;


        }
        double SearchMaxIL_index(List<double> listWV, List<double> listIL, double dblStart, double dblStop, double dblITU)
        {
            int nindex1 = -1, nindex2 = -1, nindex3 = -1;
            double dblfind = 0;

            nindex1 = listWV.FindIndex(t => (t >= dblStart && t <= dblStart - 1e-12));
            nindex2 = listWV.FindIndex(t => (t >= dblStop && t <= dblStop + 1e-12));

            //i没有找到符合的波长，直接返回错误
            if ((nindex1 <= 0) || (nindex2 <= 0))
            {
                return m_nWVCalibrateOffset + 1;
            }

            List<double> local_1_List = listIL.GetRange(nindex1, nindex2 - nindex1 + 1);
            nindex3 = local_1_List.FindIndex(t => t == local_1_List.Max());

            dblfind = listWV[nindex1 + nindex3];
            return (Math.Abs(dblITU - dblfind));
        }



        /// <summary>
        /// write data to file
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        int WriteFile(Array a, int noChannels, double wvstart, double wvstep, double wvstop)
        {
            int n = 0;
            int xLen = a.Length;
            int num = xLen / noChannels;
            int m = 0;
            long lnow = System.DateTime.Now.Ticks;
            long timeDiff = 0;


            LogHelper.WriteLog(typeof(MainForm), string.Format("WriteFile FS IL Array len:{0}, noChannels:{1}", xLen, noChannels));

            if (!Directory.Exists("./monitor/"))
            {
                Directory.CreateDirectory("./monitor/");
            }

            List<double> listRefDif = new List<double>();
            FileStream fsMonitor = new FileStream("./monitor/Monitor.dat", FileMode.Create);
            BinaryWriter bsMonitor = new BinaryWriter(fsMonitor);
            for (m = 0; m < num; m++)
            {
                listRefDif.Add(m_listReference[m] - Convert.ToDouble(a.GetValue(m)));
                bsMonitor.Write(BitConverter.GetBytes(listRefDif[m]));

                timeDiff = (System.DateTime.Now.Ticks - lnow) / 10000;
                if (timeDiff > 2000)
                {
                    return WriteFile_Timeout_Error;
                }
            }
            bsMonitor.Flush();
            bsMonitor.Close();
            fsMonitor.Close();

            LogHelper.WriteLog(typeof(MainForm), "WriteFile FS IL Monitor for loop completed!");
            double dbmax = Math.Abs(listRefDif.Max());
            double dbmin = Math.Abs(listRefDif.Min());

            //if((dbmax >0.5)  || (dbmin>0.5))
            //{
            //    LogHelper.WriteLog(typeof(MainForm), "mornitor 变化超过1, WriteFile 函数退出，并向客户端报告错误");
            //    return Monitor_Error;
            //}

            for (n = 1; n <= noChannels; n++)
            {
                string strFile;
                string[] strMF = m_listMF[n - 1].Split('.');
                int mfIndex = Convert.ToInt32(strMF[0].Substring(2));
                //strFile= string.Format("./testdata/test_fs_{0}.dat", n);
                strFile = string.Format("./testdata/{0}_{1}.dat", m_listN77XXSN[n-1], strMF[1]);
                //
                clStructList nj = null;
                bool b2ndScan = false;

                for (int i = 0; i < m_listScanList.Count; i++)
                {
                    nj = m_listScanList[i];

                    timeDiff = (System.DateTime.Now.Ticks - lnow) / 10000;
                    if (timeDiff > 2000)
                    {
                        return WriteFile_Timeout_Error;
                    }
                    if (nj.nSplitPWMChannels == n)
                    {


                        if (nj.strNumberOfScan == "1")
                        {
                            if (nj.nNumberofExecutions == 1)
                            {
                                strFile = string.Format("./testdata/{0}_{1}_a.dat", m_listN77XXSN[n-1], strMF[1]);
                            }
                            else if (nj.nNumberofExecutions == 2)
                            {
                                b2ndScan = true;
                                strFile = string.Format("./testdata/{0}_{1}_b.dat", m_listN77XXSN[n-1], strMF[1]);
                            }
                        }
                        else
                        {
                        }
                        break;
                    }
                    else
                    {
                        nj = null;
                    }
                }

                if (!Directory.Exists("./testdata/"))
                {
                    Directory.CreateDirectory("./testdata/");
                }

                FileStream fs = new FileStream(strFile, FileMode.Create);

                BinaryWriter bw = new BinaryWriter(fs);
                //StreamWriter sw = new StreamWriter(fs);

                double dbl = 0.0f;
                double dblWV = 0.0f;
                int nStartIndex = 0;
                int nStopIdex = 0;
                int nSplitPoint = 0;
                List<double> listdbl = new List<double>();
                double dbScanStep; //= Convert.ToDouble(nj.strScanStep)*.001;
                m = 0;
                if (nj != null)
                {
                    dbScanStep = Convert.ToDouble(nj.strScanStep) * .001;
                    nStartIndex = (int)((Convert.ToDouble(nj.strStartWaveLength) - wvstart) / wvstep + 0.5);
                    nStopIdex = (int)((Convert.ToDouble(nj.strStopWaveLength) - wvstart) / wvstep + 0.5);
                    nSplitPoint = (int)(dbScanStep / wvstep + 0.5);

                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        dblWV = (wvstart + wvstep * m);
                        //dbl = Convert.ToDouble(a.GetValue(m + (n - 1) * num)) * -1;
                        //if (double.IsNaN(dbl))
                        //{
                        //    dbl = -90;
                        //}
                        //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                        bw.Write(BitConverter.GetBytes(dblWV));
                    }
                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        dbl = (Convert.ToDouble(a.GetValue(m + (n - 1) * num)) + listRefDif[m]) * -1;
                        if (double.IsNaN(dbl))
                        {
                            dbl = -85.0f;
                        }
                        //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                        bw.Write(BitConverter.GetBytes(dbl));
                    }



                    //如果是第二次扫描，需要从第一个文件读出数据
                    if (b2ndScan)
                    {

                        strFile = string.Format("./testdata/{0}_{1}_a.dat", m_listN77XXSN[n-1], strMF[1]);
                        FileStream fsreader = new FileStream(strFile, FileMode.Open);
                        BinaryReader sr = new BinaryReader(fsreader);

                        try
                        {
                            while (true)
                            {
                                listdbl.Add(sr.ReadDouble());
                                timeDiff = (System.DateTime.Now.Ticks - lnow) / 10000;
                                if (timeDiff > 2000)
                                {
                                    return WriteFile_Timeout_Error;
                                }
                            }

                        }
                        catch (EndOfStreamException ex)
                        {
                            sr.Close();
                            fsreader.Close();
                            //LogHelper.WriteLog(typeof(MainForm), string.Format("end of file {0}", ex.Message));
                        }

                        int nPowerIndex = listdbl.Count / 2;
                        string strFileMerge = string.Format("./testdata/{0}_{1}.dat", m_listN77XXSN[n-1], strMF[1]);
                        FileStream fsMerge = new FileStream(strFileMerge, FileMode.Create);
                        BinaryWriter srMerge = new BinaryWriter(fsMerge);
                        for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                        {
                            dblWV = (wvstart + wvstep * m);

                            srMerge.Write(BitConverter.GetBytes(dblWV));
                        }


                        double dbDecrement = Convert.ToDouble(nj.strSplitDecrementRange);
                        double dblPWMrange = Convert.ToDouble(nj.strSplitPWMPower) - 40.0;

                        for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                        {
                            dbl = (Convert.ToDouble(a.GetValue(m + (n - 1) * num)) + listRefDif[m]) * -1;
                            //第一次测的值大于 第二档位设置档位值，就用第一次的值

                            if (listdbl[nPowerIndex] > dblPWMrange)
                            {
                                //if (double.IsNaN(dbl) && (listdbl[nPowerIndex] != -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                //else if ((!double.IsNaN(dbl)) && (listdbl[nPowerIndex] != -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                //else if ((double.IsNaN(dbl)) && (listdbl[nPowerIndex] == -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                dbl = listdbl[nPowerIndex];
                            }
                            //否则用第二次测的值。
                            else
                            {
                                if (double.IsNaN(dbl))
                                {
                                    dbl = -85f;
                                }

                            }

                            nPowerIndex++;

                            //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                            srMerge.Write(BitConverter.GetBytes(dbl));
                        }
                        srMerge.Flush();
                        srMerge.Close();
                        fsMerge.Close();
                    }

                }
                bw.Flush();
                bw.Close();
                //sw.Close();
                fs.Close();
            }

            return 1;

        }


        /// <summary>
        /// write data to file
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        int WriteFileDummy( int noChannels, double wvstart, double wvstep,double wvstop)
        {
            int n = 0;
            int xLen =Convert.ToInt32( (wvstop- wvstart)*1000/ wvstep);
            int num = xLen / noChannels;
            int m = 0;


            if (!Directory.Exists("./dummy/"))
            {
                Directory.CreateDirectory("./dummy/");
            }

            List<double> listDummy = new List<double>();
            FileStream fsDummy = new FileStream("./dummy/dummy.dat",FileMode.Open);
            BinaryReader bsRder= new BinaryReader(fsDummy);
            try
            {
                while (true)
                {
                    listDummy.Add(bsRder.ReadDouble());
                }

            }
            catch (EndOfStreamException ex)
            {
                bsRder.Close();
                fsDummy.Close();
                //LogHelper.WriteLog(typeof(MainForm), string.Format("end of file {0}", ex.Message));
            }

            double dbmax = Math.Abs(listDummy.Max());
            double dbmin = Math.Abs(listDummy.Min());




            for (n = 1; n <= noChannels; n++)
            {
                string strFile;
                //string []strMF = m_listMF[n-1].Split('.');
                //int mfIndex = Convert.ToInt32(strMF[0].Substring(2));
                //strFile= string.Format("./testdata/test_fs_{0}.dat", n);
                // m_listChannelMapping[n.]
                //strFile = string.Format("./testdata/{0}_{1}.dat", m_listN77XXSN[mfIndex], strMF[1]);
                strFile = string.Format("./testdata/{0}_{1}.dat", m_listChannelMapping[n - 1].strN774XSN, m_listChannelMapping[n - 1].strSlotNo);
                //
                clStructList nj = null;
                bool b2ndScan = false;

                for (int i = 0; i < m_listScanList.Count; i++)
                {
                    nj = m_listScanList[i];
                    if (nj.nSplitPWMChannels == n)
                    {
                        if(nj.strNumberOfScan=="1")
                        {
                            if(nj.nNumberofExecutions==1)
                            {
                                strFile = string.Format("./testdata/{0}_{1}_a.dat", m_listChannelMapping[n - 1].strN774XSN, m_listChannelMapping[n - 1].strSlotNo);
                            }
                            else if(nj.nNumberofExecutions == 2)
                            {
                                b2ndScan = true;
                                strFile = string.Format("./testdata/{0}_{1}_b.dat", m_listChannelMapping[n - 1].strN774XSN, m_listChannelMapping[n - 1].strSlotNo);
                               // m_strAbsolutepath += string.Format("{0}{1}_{2}.dat;", m_strABS_PATH, m_listChannelMapping[n - 1].strN774XSN, m_listChannelMapping[n - 1].strSlotNo );
                            }
                        }
                        else
                        {
                            //m_strAbsolutepath += string.Format("{0}{1}_{2}.dat;", m_strABS_PATH, m_listChannelMapping[n - 1].strN774XSN, m_listChannelMapping[n - 1].strSlotNo);
                        }
                        break;
                    }
                    else
                    {
                        nj = null;
                    }
                }

                if(!Directory.Exists("./testdata/"))
                {
                    Directory.CreateDirectory("./testdata/");
                }
                
                FileStream fs = new FileStream(strFile, FileMode.Create);

                BinaryWriter bw = new BinaryWriter(fs);
                //StreamWriter sw = new StreamWriter(fs);

                double dbl = 0.0f;
                double dblWV = 0.0f;
                int nStartIndex=0;
                int nStopIdex=0;
                int nSplitPoint = 0;
                List<double> listdbl = new List<double>();
                double dbScanStep; //= Convert.ToDouble(nj.strScanStep)*.001;
                m = 0;
                if (nj!=null)
                {
                    dbScanStep= Convert.ToDouble(nj.strScanStep) * .001;
                    nStartIndex = (int)((Convert.ToDouble( nj.strStartWaveLength)-wvstart)/ wvstep+0.5);
                    nStopIdex = (int)((Convert.ToDouble(nj.strStopWaveLength)- wvstart) / wvstep +0.5);
                    nSplitPoint = (int)(dbScanStep / wvstep + 0.5);

                    for ( m = nStartIndex; m <= nStopIdex; m= m+1* nSplitPoint)
                    {
                        dblWV = (wvstart + wvstep * m) ;
                        //dbl = Convert.ToDouble(a.GetValue(m + (n - 1) * num)) * -1;
                        //if (double.IsNaN(dbl))
                        //{
                        //    dbl = -90;
                        //}
                        //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                        bw.Write(BitConverter.GetBytes(dblWV));
                    }
                    for ( m = nStartIndex; m <= nStopIdex; m= m + 1 * nSplitPoint)
                    {
                        dbl = listDummy[m] * -1;
                        if (double.IsNaN(dbl))
                        {
                            dbl = -80.0f;
                        }
                        //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                        bw.Write(BitConverter.GetBytes(dbl));
                    }



                    //如果是第二次扫描，需要从第一个文件读出数据
                    if (b2ndScan)
                    {

                        strFile = string.Format("./testdata/{0}_{1}_a.dat", m_listChannelMapping[n - 1].strN774XSN, m_listChannelMapping[n - 1].strSlotNo);
                        FileStream fsreader = new FileStream(strFile, FileMode.Open);
                        BinaryReader sr = new BinaryReader(fsreader);

                        try
                        {
                            while (true)
                            {
                                listdbl.Add(sr.ReadDouble());
                            }

                        }
                        catch (EndOfStreamException ex)
                        {
                            sr.Close();
                            fsreader.Close();
                            //LogHelper.WriteLog(typeof(MainForm), string.Format("end of file {0}", ex.Message));
                        }

                        int nPowerIndex = listdbl.Count / 2 ;
                        string strFileMerge = string.Format("./testdata/{0}_{1}.dat", m_listChannelMapping[n - 1].strN774XSN, m_listChannelMapping[n - 1].strSlotNo);
                        FileStream fsMerge = new FileStream(strFileMerge, FileMode.Create);
                        BinaryWriter srMerge = new BinaryWriter(fsMerge);
                        for ( m = nStartIndex; m <=nStopIdex; m = m + 1 * nSplitPoint)
                        {
                            dblWV = (wvstart + wvstep * m);

                            srMerge.Write(BitConverter.GetBytes(dblWV));
                        }
                        double dbDecrement = Convert.ToDouble(nj.strSplitDecrementRange);
                        for ( m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                        {
                            dbl = listDummy[m] * -1;
                            //第一次测的值大于 第二档位设置档位值，就用第一次的值
                            if(listdbl[nPowerIndex] >-50)
                            {
                                //if (double.IsNaN(dbl) && (listdbl[nPowerIndex] != -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                //else if ((!double.IsNaN(dbl)) && (listdbl[nPowerIndex] != -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                //else if ((double.IsNaN(dbl)) && (listdbl[nPowerIndex] == -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                dbl = listdbl[nPowerIndex];
                            }
                            //否则用第二次测的值。
                            else
                            {
                                if (double.IsNaN(dbl) )
                                {
                                    dbl = -80f;
                                }
                         
                            }



                        
                            nPowerIndex++;

                            //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                            srMerge.Write(BitConverter.GetBytes(dbl));
                        }
                        srMerge.Flush();
                        srMerge.Close();
                        fsMerge.Close();
                    }

                }
                bw.Flush();
                bw.Close();
                //sw.Close();
                fs.Close();
            }

            return 1;

        }
        /// <summary>
        /// write data to file
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        int  WriteFilePDL(Array a, int noChannels, double wvstart, double wvstep,double wvstop)
        {
            int n = 0;
            int xLen = a.Length;
            int num = xLen / noChannels;
            int m = 0;
            //int nXdata = graphPDL.dataPerCurve;
            int nXdata =Convert.ToInt32((wvstop - wvstart) / 0.001) + 1;
            Array array_PDL = graphPDL_PDL.YData;


            //使用插值将数据整为 STEP 1PM 的数据
            List<double> list_Interpolation = new List<double>();
            list_Interpolation.Clear();
            for (n = 1; n <= noChannels; n++)
            {

                double dbl = 0.0f;

                List<double> listdbl = new List<double>();
                m = 0;

                for (m = 0; m < num; m++)
                {
                    
                    //dbl = Convert.ToDouble(a.GetValue(m));
                    dbl = Convert.ToDouble(a.GetValue(m + (n - 1) * num));
                    if (double.IsNaN(dbl))
                    {
                        dbl = -80.0f;
                    }
                    list_Interpolation.Add(dbl);

                    if (m < (num - 1))
                    {
                        int nInnerValue = 0;
                        int nInnerStep = Convert.ToInt32(wvstep * 1000);
                        double dblInner1 = 0, dblInner2 = 0, dblDiff = 0;

                        dblInner1 = Convert.ToDouble(a.GetValue(m + (n - 1) * num));
                        dblInner2 = Convert.ToDouble(a.GetValue(m + (n - 1) * num + 1));
                        dblDiff = (dblInner2 - dblInner1) / nInnerStep;


                        for (nInnerValue = 1; nInnerValue < nInnerStep; nInnerValue++)
                        {
                            dbl = dblInner1 +  dblDiff * nInnerValue;
                            list_Interpolation.Add(dbl);
                        }

                    }
                }
            }
            //插值 PDL
            List<double> list_Interpolation_PDL = new List<double>();
            list_Interpolation_PDL.Clear();
            for (n = 1; n <= noChannels; n++)
            {

                double dbl = 0.0f;

                List<double> listdbl = new List<double>();
                m = 0;

                for (m = 0; m < num; m++)
                {

                    dbl = Convert.ToDouble(array_PDL.GetValue(m + (n - 1) * num));
                    if (double.IsNaN(dbl))
                    {
                        dbl = -80.0f;
                    }
                    list_Interpolation_PDL.Add(dbl);

                    if (m < (num - 1))
                    {
                        int nInnerValue = 0;
                        int nInnerStep = Convert.ToInt32(wvstep * 1000);
                        double dblInner1 = 0, dblInner2 = 0, dblDiff = 0;

                        dblInner1 = Convert.ToDouble(array_PDL.GetValue(m + (n - 1) * num));
                        dblInner2 = Convert.ToDouble(array_PDL.GetValue(m + (n - 1) * num + 1));
                        dblDiff = (dblInner2 - dblInner1) / nInnerStep;


                        for (nInnerValue = 1; nInnerValue < nInnerStep; nInnerValue++)
                        {
                            dbl = dblInner1 +   dblDiff * nInnerValue;
                            list_Interpolation_PDL.Add(dbl);
                        }

                    }
                }
            }






            if (!Directory.Exists("./monitor/"))
            {
                Directory.CreateDirectory("./monitor/");
            }

            //List<double> listRefDif = new List<double>();
            //FileStream fsMonitor = new FileStream("./monitor/Monitor.dat", FileMode.Create);
            //BinaryWriter bsMonitor = new BinaryWriter(fsMonitor);
            //for (m = 0; m < num; m++)
            //{
            //    listRefDif.Add(m_listReference[m] - Convert.ToDouble(a.GetValue(m)));

            //    //插值 MONITOR
            //    if (m < (num - 1))
            //    {
            //        int nInnerValue = 0;
            //        int nInnerStep = Convert.ToInt32(wvstep * 1000);
            //        double dblInner1 = 0, dblInner2 = 0, dblDiff = 0;

            //        dblInner1 = m_listReference[m] - Convert.ToDouble(a.GetValue(m));
            //        dblInner2 = m_listReference[m+1] - Convert.ToDouble(a.GetValue(m + 1));
            //        dblDiff = (dblInner2 - dblInner1) / nInnerStep;


            //        for (nInnerValue = 1; nInnerValue < nInnerStep; nInnerValue++)
            //        {
            //            double dbl = dblInner1 +  dblDiff * nInnerValue;
            //            listRefDif.Add(dbl);
            //        }

            //    }
            //    bsMonitor.Write(BitConverter.GetBytes(listRefDif[m]));
            //}

            //插值结束, 并强行将 wvstep 置为 1PM
            wvstep = 0.001;

            //bsMonitor.Flush();
            //bsMonitor.Close();
            //fsMonitor.Close();
            //double dbmax = Math.Abs(listRefDif.Max());
            //double dbmin = Math.Abs(listRefDif.Min());

            //if ((dbmax > 0.5) || (dbmin > 1))
            //{
            //    LogHelper.WriteLog(typeof(MainForm), "mornitor 变化超过1, WriteFile 函数退出，并向客户端报告错误");
            //    return Monitor_Error;
            //}




            for (n = 1; n <= noChannels; n++)
            {
                string strFile,strFilePDL;
                string []strMF = m_listMF[n-1].Split('.');
                int mfIndex = Convert.ToInt32(strMF[0].Substring(2));
                //strFile= string.Format("./testdata/test_fs_{0}.dat", n);
                strFile = string.Format("./testdata/{0}_{1}.dat", m_listN77XXSN[n-1], strMF[1]);
                strFilePDL = string.Format("./testdata/{0}_{1}_PDL.dat", m_listN77XXSN[n-1], strMF[1]);

                //
                clStructList nj = null;


                for (int i = 0; i < m_listScanList.Count; i++)
                {
                    nj = m_listScanList[i];
                    if (nj.nSplitPWMChannels == n)
                    {

                        strFile = string.Format("./testdata/{0}_{1}.dat", strMF[3], strMF[1]);
                        strFilePDL = string.Format("./testdata/{0}_{1}_PDL.dat", strMF[3], strMF[1]);
                        //m_strAbsolutepath += string.Format("{0}{1}_{2}.dat;{3}{4}_{5}_PDL.dat;", m_strABS_PATH, m_listN77XXSN[mfIndex], strMF[1], m_strABS_PATH, m_listN77XXSN[mfIndex], strMF[1]);

                        LogHelper.WriteLog(typeof(MainForm), strFile+ ",noChannels:" + noChannels.ToString()+ ",m_listScanList.Count:"+ m_listScanList.Count.ToString());
                        break;
                    }
                    else
                    {
                        nj = null;
                    }
                }

                if(!Directory.Exists("./testdata/"))
                {
                    Directory.CreateDirectory("./testdata/");
                }


                FileStream fs = new FileStream(strFile, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);

                FileStream fs_PDL = new FileStream(strFilePDL, FileMode.Create);

                BinaryWriter bw_PDL = new BinaryWriter(fs_PDL); 

                double dbl = 0.0f;
                double dblWV = 0.0f;
                int nStartIndex=0;
                int nStopIdex=0;
                int nSplitPoint = 0;
                List<double> listdbl = new List<double>();
                double dbScanStep; //= Convert.ToDouble(nj.strScanStep)*.001;
                m = 0;
                if (nj!=null)
                {

                    dbScanStep = Convert.ToDouble(nj.strScanStep) * .001;
                    nStartIndex = (int)((Convert.ToDouble(nj.strStartWaveLength) - wvstart) / wvstep + 0.5);
                    nStopIdex = (int)((Convert.ToDouble(nj.strStopWaveLength) - wvstart) / wvstep + 0.5);
                    nSplitPoint = (int)(dbScanStep / wvstep + 0.5);

                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        dblWV = (wvstart + wvstep * m) ;

                        bw.Write(BitConverter.GetBytes(dblWV));
                        bw_PDL.Write(BitConverter.GetBytes(dblWV));

                    }
                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        //写PDL ENGINE 的 IL 

                        //插值时已经将 NaN 处理为-150, 写文件时只需要判断小于-145 就是无效值即可
                        if (list_Interpolation[m + (n - 1) * nXdata] < -145.0)
                        {
                            dbl = -80.0f;
                        }
                        else
                        {
                            dbl = (list_Interpolation[m + (n - 1) * nXdata]) * -1;

                        }

                        bw.Write(BitConverter.GetBytes(dbl));


                        //写 PDL ENGINE 的 PDL
                        dbl = list_Interpolation_PDL[m + (n - 1) * nXdata] ;
                        if (double.IsNaN(dbl))
                        {
                            dbl = -80.0f;
                        }

                        bw_PDL.Write(BitConverter.GetBytes(dbl));

                    }


                }
                bw.Flush();
                bw.Close();
                fs.Close();

                bw_PDL.Flush();
                bw_PDL.Close();
                fs_PDL.Close();

            }



            return 1;

        }

        private bool CheckFilesExits(string strPath)
        {
            string strPathTemp = strPath;
            return true;
        }


        public void DelectDir(string srcPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }


        /// <summary>
        /// write data to file
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        int WriteFile( DataParam  dataArray, double wvstart, double wvstep, double wvstop, int nPDL_States)
        {
            int nLines = 0,n;
            string strFile ="";

            nLines = m_SCPI_Scan.m_list_Config_string.Count;
            for ( n = 0; n < nLines; n++)
            {
                
                string[] strMF = m_SCPI_Scan.m_list_Config_string[n ].Split('_');
                //
                clStructList nj = null;
                bool b2ndScan = false;
                strFile = "";

                for (int i = 0; i < m_listScanList.Count; i++)
                {
                    nj = m_listScanList[i];
 
                    if ((nj.nSplitPWMChannels-1) == n)
                    {
                        if (nj.strNumberOfScan == "1")
                        {
                            if (nj.strDoPDL == "1")
                            {
                                if (nj.nNumberofExecutions <= 4)
                                {
                                    strFile = string.Format("./testdata/{0}_{1}_a.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                                }
                                else
                                {
                                    b2ndScan = true;
                                    strFile = string.Format("./testdata/{0}_{1}_b.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                                }
                            }
                            else
                            {
                                if (nj.nNumberofExecutions <= 1)
                                {
                                    strFile = string.Format("./testdata/{0}_a.dat", m_SCPI_Scan.m_list_Config_string[n] );
                                }
                                else
                                {
                                    b2ndScan = true;
                                    strFile = string.Format("./testdata/{0}_b.dat", m_SCPI_Scan.m_list_Config_string[n] );
                                }
                            }
                        }
                        else if (nj.strNumberOfScan == "0")
                        {
                            if (nj.strDoPDL == "1")
                            {
                                strFile = string.Format("./testdata/{0}_{1}.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                            }
                            else
                            {
                                strFile = string.Format("./testdata/{0}.dat", m_SCPI_Scan.m_list_Config_string[n]);
                            }
                        }
                        break;
                    }
                    else
                    {
                        nj = null;
                    }
                }// end of  "for (int i = 0; i < m_listScanList.Count; i++)"


                if (strFile =="")
                {
                    continue;
                }

                if (!Directory.Exists("./testdata/"))
                {
                    Directory.CreateDirectory("./testdata/");
                }

                FileStream fs = new FileStream(strFile, FileMode.Create);

                BinaryWriter bw = new BinaryWriter(fs);
                //StreamWriter sw = new StreamWriter(fs);

                double dbl = 0.0f;
                double dblWV = 0.0f;
                int nStartIndex = 0;
                int nStopIdex = 0;
                int nSplitPoint = 0;
                List<double> listdbl = new List<double>();
                double dbScanStep; //= Convert.ToDouble(nj.strScanStep)*.001;
                int m = 0;
                if (nj != null)
                {
                    dbScanStep = Convert.ToDouble(nj.strScanStep) * .001;
                    nStartIndex = (int)((Convert.ToDouble(nj.strStartWaveLength) - wvstart) / wvstep + 0.5);
                    nStopIdex = (int)((Convert.ToDouble(nj.strStopWaveLength) - wvstart) / wvstep + 0.5);
                    nSplitPoint = (int)(dbScanStep / wvstep + 0.5);

                    //写入波长
                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        dblWV = (wvstart + wvstep * m);

                        bw.Write(BitConverter.GetBytes(dblWV));
                    }

                    //再写入测试通道第n通道的IL 值
                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        dbl = dataArray.m_pdblLossArrayP1[n].ilArray[m];
                        if (double.IsNaN(dbl))
                        {
                            dbl = -80.0f;
                        }
                        //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                        bw.Write(BitConverter.GetBytes(dbl));
                    }



                    //如果是第二次扫描，需要从第一个文件读出数据
                    if (b2ndScan)
                    {
                        string strFileMerge = "";

                        if (nj.strDoPDL == "1")
                        {
                            strFile = string.Format("./testdata/{0}_{1}_a.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                            strFileMerge = string.Format("./testdata/{0}_{1}.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                        }
                        else
                        {
                            strFile = string.Format("./testdata/{0}_a.dat", m_SCPI_Scan.m_list_Config_string[n]);
                            strFileMerge = string.Format("./testdata/{0}.dat", m_SCPI_Scan.m_list_Config_string[n]);
                        }
 
                        FileStream fsreader = new FileStream(strFile, FileMode.Open);
                        BinaryReader br = new BinaryReader(fsreader);

                        try
                        {
                            while (true)
                            {
                                listdbl.Add(br.ReadDouble());
                            }

                        }
                        catch (EndOfStreamException ex)
                        {
                            br.Close();
                            fsreader.Close();
                            //LogHelper.WriteLog(typeof(MainForm), string.Format("end of file {0}", ex.Message));
                        }

                        int nPowerIndex = listdbl.Count / 2;

                        FileStream fsMerge = new FileStream(strFileMerge, FileMode.Create);
                        BinaryWriter srMerge = new BinaryWriter(fsMerge);
                        for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                        {
                            dblWV = (wvstart + wvstep * m);

                            srMerge.Write(BitConverter.GetBytes(dblWV));
                        }

                        double dbDecrement = Convert.ToDouble(nj.strSplitDecrementRange);


                        ////写要求扫描通道的功率值
                        nPowerIndex = listdbl.Count / 2;
                        for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                        {
                            dbl = dataArray.m_pdblLossArrayP1[n].ilArray[m];
                            double dblDecreament = Convert.ToDouble(nj.strSplitPWMPower) - 40;
                            //第一次测的值大于 第二档位设置档位值，就用第一次的值
                            if (listdbl[nPowerIndex] > dblDecreament)
                            {
                                //if (double.IsNaN(dbl) && (listdbl[nPowerIndex] != -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                //else if ((!double.IsNaN(dbl)) && (listdbl[nPowerIndex] != -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                //else if ((double.IsNaN(dbl)) && (listdbl[nPowerIndex] == -150.0f))
                                //{
                                //    dbl = listdbl[nPowerIndex];
                                //}
                                dbl = listdbl[nPowerIndex];
                            }
                            //否则用第二次测的值。
                            else
                            {
                                if (double.IsNaN(dbl))
                                {
                                    dbl = -80f;
                                }

                            }
                            nPowerIndex++;
                            //sw.WriteLine(string.Format("{0:0.000},{1:0.000}", dblWV, dbl));
                            srMerge.Write(BitConverter.GetBytes(dbl));
                        }

                        srMerge.Flush();
                        srMerge.Close();
                        fsMerge.Close();

                        if(nPDL_States ==1 || nPDL_States ==2)
                        {
                            if (nj.strDoPDL == "1" && nj.strNumberOfScan == "0")
                            {
                                string strFilecopy = "";
                                if(nPDL_States == 1)
                                {
                                    strFilecopy = string.Format("./testdata/{0}_{1}_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                                }
                                else if(nPDL_States == 2)
                                {
                                    strFilecopy = string.Format("./testdata/{0}_{1}_PDL_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                                }
                                if (File.Exists(strFilecopy))
                                {
                                    File.Delete(strFilecopy);
                                    
                                }
                                File.Copy(strFile, strFilecopy);
                            }
                            else if (nj.strDoPDL == "1" && nj.strNumberOfScan == "1")
                            {
                                string strFilecopy = string.Format("./testdata/{0}_{1}_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                                if (nPDL_States == 1)
                                {
                                    strFilecopy = string.Format("./testdata/{0}_{1}_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                                }
                                else if (nPDL_States == 2)
                                {
                                    strFilecopy = string.Format("./testdata/{0}_{1}_PDL_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                                }
                                if (File.Exists(strFilecopy))
                                {
                                    File.Delete(strFilecopy);
                                    
                                }
                                File.Copy(strFileMerge, strFilecopy);
                            }
                        }

                    }

                }// end  of   "if (nj != null)"
                bw.Flush();
                bw.Close();
                //sw.Close();
                fs.Close();



                if (nPDL_States == 1 || nPDL_States == 2)
                {
                    if (nj.strDoPDL == "1" && nj.strNumberOfScan == "0")
                    {
                        //string strFilecopy = string.Format("./testdata/{0}_{1}_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                        string strFilecopy = "";
                        if (nPDL_States == 1)
                        {
                            strFilecopy = string.Format("./testdata/{0}_{1}_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                        }
                        else if (nPDL_States == 2)
                        {
                            strFilecopy = string.Format("./testdata/{0}_{1}_PDL_copy.dat", m_SCPI_Scan.m_list_Config_string[n], nPDL_States);
                        }
                        if (File.Exists(strFilecopy))
                        {
                            File.Delete(strFilecopy);
                        }
                        File.Copy(strFile, strFilecopy);

                    }

                }
            }// end of  "for ( n = 0; n < nLines; n++)"

            return 1;

        }

        /// <summary>
        /// 将 GOUMAX 扫描数据插值为1pm的数据
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        /// <param name="nChannel">插值哪个通道</param>
        int InterpolationGouMaxIL(GouMaxDataParam dataArray, double wvstart, double wvstep, double wvstop,int nChannel)
        {
            int m=0;

            //使用插值将数据整为 STEP 1PM 的数据
            m_list_GouMax_Interpolation.Clear();
            double dbl = 0.0f;

            for (m = 0; m < dataArray.m_nArrayLength; m++)
            {

                dbl = dataArray.m_pdblLossArrayP1[nChannel].ilArray[m];
                if (double.IsNaN(dbl))
                {
                    dbl = -80.0f;
                }
                m_list_GouMax_Interpolation.Add(dbl);

                if (m < (dataArray.m_nArrayLength - 1))
                {
                    int nInnerValue = 0;
                    int nInnerStep =Convert.ToInt32( m_dbl_SCPI_STEP_WL);
                    double dblInner1 = 0, dblInner2 = 0, dblDiff = 0;

                    dblInner1 = dataArray.m_pdblLossArrayP1[nChannel].ilArray[m];
                    dblInner2 = dataArray.m_pdblLossArrayP1[nChannel].ilArray[m + 1];
                    dblDiff = (dblInner2 - dblInner1) / nInnerStep;


                    for (nInnerValue = 1; nInnerValue < nInnerStep; nInnerValue++)
                    {
                        dbl = dblInner1 + dblDiff * nInnerValue;
                        m_list_GouMax_Interpolation.Add(dbl);
                    }
                }
            }

            return 1;

        }


        /// <summary>
        /// write data to file
        /// </summary>
        /// <param name="a">IL ARRAY</param>
        /// <param name="noChannels">HOW MANY CHANNELS</param>
        /// <param name="wvstart">START WAVELENGTH</param>
        /// <param name="wvstep">STEP SIZE</param>
        int WriteGouMaxFile(GouMaxDataParam dataArray, double wvstart, double wvstep, double wvstop)
        {
            int nLines = 0, n;
            string strFile = "";


            //GOUMAX 最多有64 个通道，使用m_listN77XXSN 这个List的 COUNT ，配置从PortMappingAuto.txt读出来的
            nLines = m_listN77XXSN.Count;
            for (n = 0; n < nLines; n++)
            {
                clStructList nj = null;
                for (int i = 0; i < m_listScanList.Count; i++)
                {
                    nj = m_listScanList[i];
                    if (((nj.nSplitPWMChannels - 1) == n)  )
                    {
                        string[] strMF = m_listMF[n].Split('.');
                        int mfIndex = Convert.ToInt32(strMF[0].Substring(2));
                        strFile = string.Format("./testdata/{0}_{1}.dat", m_listN77XXSN[n], strMF[1]);
                        //使用插值将数据整为 STEP 1PM 的数据
                        InterpolationGouMaxIL(dataArray, wvstart, wvstep, wvstop,n);
                        break;
                    }
                    else
                    {
                        nj = null;
                    }
                }// end of  "for (int i = 0; i < m_listScanList.Count; i++)"


                if (strFile == "")
                {
                    continue;
                }

                if (!Directory.Exists("./testdata/"))
                {
                    Directory.CreateDirectory("./testdata/");
                }

                FileStream fs = new FileStream(strFile, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);

                double dbl = 0.0f;
                double dblWV = 0.0f;
                int nStartIndex = 0;
                int nStopIdex = 0;
                int nSplitPoint = 0;
                List<double> listdbl = new List<double>();
                double dbScanStep; //= Convert.ToDouble(nj.strScanStep)*.001;
                int m = 0;
                if (nj != null)
                {

                    dbScanStep = Convert.ToDouble(nj.strScanStep) * 0.001;
                    nStartIndex = (int)((Convert.ToDouble(nj.strStartWaveLength) - wvstart) / wvstep + 0.5);
                    nStopIdex = (int)((Convert.ToDouble(nj.strStopWaveLength) - wvstart) / wvstep + 0.5);
                    nSplitPoint = (int)(dbScanStep / wvstep + 0.5);

                    //写入波长
                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        //dblWV = dataArray.m_pdblWavelengthArray[m];
                        dblWV = (wvstart + wvstep * m);
                        bw.Write(BitConverter.GetBytes(dblWV));
                    }

                    //再写入测试通道第n通道的IL 值
                    for (m = nStartIndex; m <= nStopIdex; m = m + 1 * nSplitPoint)
                    {
                        dbl = m_list_GouMax_Interpolation[m];
                        if (double.IsNaN(dbl))
                        {
                            dbl = -80.0f;
                        }
                        bw.Write(BitConverter.GetBytes(dbl));
                    }

                }// end  of   "if (nj != null)"
                bw.Flush();
                bw.Close();
                fs.Close();
                strFile = "";
            }// end of  "for ( n = 0; n < nLines; n++)"

            return 1;

        }


    }



    public class clStructList
    {
        public string strGUID;
        public string strSocketID;
        public string strStartWaveLength;
        public string strStopWaveLength;
        public string strScanStep;
        public string strTLSPower;
        public string strPWMPower;
        public string strNumberOfScan;
        public string strIndexOfPWMChannels;
        public string strRemoteEndPoint;
        public string strDoPDL;
        public string strDecrementRange;
        public int nSplitPWMChannels;
        public string strSplitPWMPower;
        public string strSplitDecrementRange;
        /// <summary>
        /// 实际扫描执行次数
        /// </summary>
        public int nNumberofExecutions;
        ///列表状态: 101表示待测试；Queue_Doing_Scan(201)表示的测试中；
        public short shStatus;

        //拼接返回的ABSOLUTE_PATH,用于返回协议 IL 使用
        public string strAbsolutePathForIL;

        //拼接返回的ABSOLUTE_PATH,用于返回协议 PDL 使用
        public string strAbsolutePathForPDL;
    }

    public class clInstrumentChannelsMapping
    {
        public int nChannelSN;
        public string strMFNo;
        public string strSlotNo;
        public string strChannelNo;
        public string strN774XSN;

    }

}
