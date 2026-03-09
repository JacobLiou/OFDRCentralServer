using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Visa;
using System.Windows.Forms;


namespace SampleTcpServer
{
    class GOUMax_Comm_controller
    {
        private MessageBasedSession mbSession;


        public GOUMax_Comm_controller()
        {


            throw new System.NotImplementedException();
        }

        public void openSession(string strResourceName)
        {
            using (var rmSession = new ResourceManager())
            {
                try
                {
                    mbSession = (MessageBasedSession)rmSession.Open(strResourceName);
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
                    Cursor.Current = Cursors.Default;
                }
            }
          
        }

        public void closeSession_()
        {
            mbSession.Dispose();
        }

        public void queryString(string textToWrite, out string textToRead)
        {
            string str2="";

            textToRead = str2;
            try
            {
                mbSession.RawIO.Write(textToWrite);
                textToRead = InsertCommonEscapeSequences(mbSession.RawIO.ReadString());
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        public void write_buffer(byte []buffer,int nlen)
        {
            try
            {
                mbSession.RawIO.Write(buffer,0,nlen);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        public void read_buffer(out byte [] readbyte, out int nlen)
        {
            byte[] by1 = new byte[64];
            readbyte = by1;
            nlen = 0;
            try
            {
                readbyte = mbSession.RawIO.Read();
                nlen = readbyte.Length;
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }


        private string ReplaceCommonEscapeSequences(string s)
        {
            return s.Replace("\\n", "\n").Replace("\\r", "\r");
        }

        private string InsertCommonEscapeSequences(string s)
        {
            return s.Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private bool Send_TLS_Scan(int nSweepStart, int nSweepStop, short nSpeed)
        {

            byte[] byCommandRead = new byte[128];
            byte[]  byCommandWrite = new byte[] { 0xAA, 0x53, 0x57, 0x53, 0x54, 00, 0x0B, 00, 01, 00, 0x13, 0x12, 0xD0, 00, 0x19, 0xA6, 0x68, 00, 00, 00, 00, 00, 00, 01, 0x90, 00, 00, 00, 00, 04, 0x0A };
            byte[]  byCompare      = new byte[] { 0xAA, 0x53, 0x57, 0x53, 0x54, 00, 0x01, 00, 01, 00, 00, 0x1, 0x52};

            int nlen = byCommandWrite.Length;

            byte[] byExchange = BitConverter.GetBytes(nSweepStart);
            byCommandWrite[9] = byExchange[3];
            byCommandWrite[10] = byExchange[2];
            byCommandWrite[11] = byExchange[1];
            byCommandWrite[12] = byExchange[0];

            byExchange = BitConverter.GetBytes(nSweepStop);
            byCommandWrite[13] = byExchange[3];
            byCommandWrite[14] = byExchange[2];
            byCommandWrite[15] = byExchange[1];
            byCommandWrite[16] = byExchange[0];

            byExchange = BitConverter.GetBytes(nSpeed);
            byCommandWrite[23] = byExchange[1];
            byCommandWrite[24] = byExchange[0];

            short shSum = 0;
            for (int i = 1; i < nlen - 2; i++)
            {
                shSum += byCommandWrite[i];
            }
            byte[] aaaa = BitConverter.GetBytes(shSum);
            byCommandWrite[nlen - 2] = aaaa[1];
            byCommandWrite[nlen - 1] = aaaa[0];


            write_buffer(byCommandWrite, nlen);


            /*Table 5-23: Response to “Start Sweep” Command
            Field Parameter TLS Response
            0 Head byte 0xAA (One byte head)
            1 Command Word_1 0x5357
            2 Command Word_2 0x5354
            3 Length word 0x0001
            4 Error word 16-bit Error Code (0x0000 for No error)
            5 Checksum word 0x0152 for no error
            Unsigned 16-bits integer*/

            read_buffer(out byCommandRead,out nlen);

            for (int i = 1; i < nlen; i++)
            {
                if (byCommandRead[i] != byCommandRead[i])
                {
                    return false;
                }

            }

            return true;
        }


    }
}
