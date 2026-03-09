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
    class kt8164
    {
        private MessageBasedSession mbSession ;


        public bool openSession( string strAddress)
        {

            using (var rmSession = new ResourceManager())
            {
                try
                {
                    mbSession = (MessageBasedSession)rmSession.Open(strAddress);

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

        private void closeSession()
        {
            if (mbSession!=null)
            {
                mbSession.Dispose();
            }

        }


        public bool JugeTLSTrigStatus(out int TrigStatus)
        {

            int preFlag;
            int nIndex = 0;
            string strTemp;
            try
            {
                while(true)
                { 
                    strTemp=query("sour0:wav:swe:flag?\n");

                    preFlag = Convert.ToInt32(strTemp);
                    if (preFlag % 2 == 1)
                    {
                        TrigStatus = preFlag;
                        break;
                    }
                    else
                    {
                        Thread.Sleep(50);
                        nIndex++;
                    }
                    if (nIndex > 2000)
                    {
                        throw new Exception("Read Trig Status Error!");
                    }
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                TrigStatus = -1;
                return false;
            }
            return true;
        }

        ///////////////////////////////////////////////////////////////////
        //函数说明：
        //    获得扫描波长函数

        ///////////////////////////////////////////////////////////////////
        private bool GetLamdaScanWLResult()
        {
            string chSendBuf;
            string strTemp;
            //PBYTE pbWLArry = NULL;
            byte [] pbWLArry;
            //double* dbTemp;
            int nTemp;
        
            try
            {

                chSendBuf =query("sour0:read:poin? llog\n");
       
                nTemp = Convert.ToInt32(chSendBuf);

                //从TLS扫描结果中读取点数
                
                write(string.Format("sour0:read:data:block? LLOG,0,{0}",  nTemp));
             
             
                pbWLArry = new byte[(nTemp) * 8 + 100];  // add extra 100 bytes buffer for head :#64008


                pbWLArry =readbyte();
                
      
                nTemp = pbWLArry[1] - '0' + 2;

                //dbTemp = (double*)(pbWLArry + nTemp);

                //for (int nIndex = 0; nIndex < (int)m_pScanParam->m_dwSampleCount; nIndex++)
                //{
                //    pdbWLAarry[nIndex] = dbTemp[nIndex] * 1E9;
                //}

                //if (pbWLArry != NULL)
                //{
                //    delete[] pbWLArry;
                //    pbWLArry = NULL;
                //}
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            //for(int iConvert = 0;iConvert< pdbWLAarry.Length;iConvert++)
            //{
            //    pdbWLAarry[iConvert] = BitConverter.ToDouble(pbWLArry, iConvert*8);

            //}
            
            return true;
        }
   



        //////////////////////////////////////////////
        //函数说明：
        //    通过发送软trig来驱动扫描

        ////////////////////////////////////////////////
        private bool SendSoftTrig(int TrigStatus)
        {
            string  chSendBuf;
            int CurentFlag;
            string strTemp;

            try
            {

                write("sour0:wav:swe:soft\n");
                      
                Thread.Sleep(500);
                //读取Flag标志，如果前面的Flag小于当前Flag则认为扫描完成
                while (true)
                {
                    strTemp = query("sour0:wav:swe:flag?\n");
               
                    CurentFlag = Convert.ToInt32(strTemp);
                    if (CurentFlag > TrigStatus)
                    {
                        break;
                    }
                }
            }
            catch(Exception exp)
            {
                MessageBox.Show(exp.Message);
                return false;
            }
            return true;
        }
    




        /////////////////////////////////////////////////////////////////////
        //函数说明：
        //    设置模块激光输出功率

        ///////////////////////////////////////////////////////////////////////
        private bool SetTLSPower(double dbPower)
        {

            try
            {

                write("sour0:pow 5DBM\n");

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }


        /////////////////////////////////////////////////////////////////
        //函数说明：
        //    读取TLS输出功率

        /////////////////////////////////////////////////////////////////
        private bool ReadTLSPower(out double dbPower)
        {

            string strTemp;
            try
            {

                strTemp = query("sour0:pow?\n");

                dbPower = Convert.ToDouble(strTemp);
            }
            catch (Exception ex)
            {
                dbPower = -1000;
                MessageBox.Show(ex.Message);
            }
            return true;

        }





        private bool GetTLSWLRange(out double dbWLMin, out double dbWLMax)
        {
            string strTemp;

            try
            {
                strTemp = query("sour0:wav? min\n");

                dbWLMin = Convert.ToDouble(strTemp) * 1E9;

                //获得最大波长range
                strTemp = query("sour0:wav? max\n");

                dbWLMax = Convert.ToDouble(strTemp) * 1E9;


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                dbWLMin = 0.0;
                dbWLMax = 0.0;
                return false;
            }
            return true;
        }

        //////////////////////////////////////////////////////
        //函数说明
        //    获得扫描的起始波长范围

        ///////////////////////////////////////////////////////
        private bool GetScanWLRange(out double dbStartWL,out double dbStopWL)
        {

            string strTemp;
            try
            {

                strTemp=query( "sour0:wav:swe:star?\n");

                dbStartWL = Convert.ToDouble(strTemp) * 1E9;

 
                //读取扫描停止波长

                strTemp = query("sour0:wav:swe:stop?\n");

                dbStopWL = Convert.ToDouble(strTemp) * 1E9;


  
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                dbStopWL = 0;
                dbStartWL = 0;
                return false;

            }
            return true;
        }




        //////////////////////////////////////////////////////////////////////////
        //函数说明：
        //    设置扫描起始波长

        /////////////////////////////////////////////////////////////////////////
        bool SetScanWLRange(double dbWLStart, double dbWLStop)
        {

            try
            {
                //设置开始扫描波长
                write(string.Format("sour%d:wav:swe:star {0:0.000}NM\n", dbWLStart));


                //设置停止扫描波长
                write(string.Format("sour%d:wav:swe:stop {0:0.000}NM\n", dbWLStop));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            return true;

        }



        private bool SetScanStop()
        {
            string chSendBuf;

            int nCount;
            int CurentFlag;
            try
            {


                write("sour0:wav:swe 0\n");

                Thread.Sleep(200);

                // *************************************

                write("sour%d:wav:swe:flag?\n");

                nCount = 0;
                while (true)
                {

                    chSendBuf = read();
                    CurentFlag = Convert.ToInt32(chSendBuf);
                    if (CurentFlag == 0)
                    {
                        break;
                    }
                    else
                    {

                        write("sour0:wav:swe 0\n");

                        Thread.Sleep(2000);
                        break;
                    }
                    nCount++;
                    if (nCount > 100)
                    {
                        throw new Exception("Set TLS scan stop error");
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return true;
        }




        ///////////////////////////////////////////////////////////
        //函数说明：
        //    用来进行扫描初始化函数

        //////////////////////////////////////////////////////////
        private bool PrepareScan()
        {
            double dbStartWL, dbStopWL , dbReadStartWL, dbReadStopWL;

            String strTemp;

            try
            {
                //m_pScanParam = (PScanParam)lScanParam;
                if (!GetTLSWLRange(out dbStartWL, out dbStopWL))
                {
                    throw new Exception("Get TLS max and min WL range error");
                }

                dbStartWL -= 0.5;
                dbStopWL += 0.5;


                //设置扫描模式为step模式
                write("sour0:wav:swe:mode step\n");

                //设置Repeat模式为TWOWAY 

                write("sour0:wav:swe:rep twow\n");

                //设置扫描模式为continue模式

                write("sour0:wav:swe:mode cont\n");

                // 		//不管激光器是在扫描状态还是不在扫描状态，
                // 		//先发送停止扫描指令	
                if (!SetScanStop())
                {
                    throw new Exception("Send read laser WL error");
                }
                //设置扫描的起始波长
                if (!SetScanWLRange(dbStartWL, dbStopWL))
                {
                    throw new Exception("Setting scan WL range error");
                }
                //回读扫描的起始波长
                if (!GetScanWLRange(out dbReadStartWL, out dbReadStopWL))
                {
                    throw new Exception("Read Scan WL Range error");
                }
                //判断设置的扫描起始波长与获得的起始波长是否相同
                //if (fabs(dbStartWL - dbReadStartWL) > 0.01 || fabs(dbStopWL - dbReadStopWL) > 0.01)
                //{
                //    throw new Exception("Scan WL range error");
                //}
                //设置扫描速度

                write("sour0:wav:swe:spe 200nm/s\n");

                //设置triger扫描步长

                write("sour%d:wav:swe:step 0.005nm\n");

                //设置激光器的功率 
                if (!SetTLSPower(5))
                {
                    throw new Exception("Setting TLS power error");
                }


                //获得扫描的trig点数,并赋值给结构体
                strTemp = query("sour0:wav:swe:exp?\n");

                //m_pScanParam->m_dwSampleCount  = atoi(chSendBuf)-1;
                //m_pScanParam->m_dwSampleCount = atoi(chSendBuf);//-1;

                //设置trig的模式

                write("trig:conf loop\n");

                //设置tripg输入为sws模式
                write("trig:inp sws\n");

                //设置trig输出为stf模式

                write("trig:outp stf\n");

                //设置扫描最大循环次数
                write("sour0:wav:swe:cycl 0\n");

                //设置am的state模式为0
                write("sour%d:am:stat off\n");

                //设置TLS为Log模式

                write("sour0:wav:swe:llog on\n");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
    
        }




        public string query(string textToWrite)
        {
            try
            {
                mbSession.RawIO.Write(textToWrite);
                return mbSession.RawIO.ReadString();
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

        public void write(string textToWrite)
        {
            try
            {
                mbSession.RawIO.Write(textToWrite);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        public string read()
        {

            try
            {
                return (mbSession.RawIO.ReadString());
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

        public byte[] readbyte()
        {

            try
            {
                return (mbSession.RawIO.Read());
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
    }
}
