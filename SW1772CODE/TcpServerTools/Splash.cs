using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace SampleTcpServer
{
    public partial class Splash : Form
    {
        public Splash()
        {
            InitializeComponent();
            timer1.Enabled = true;
        }

        public void KillMe(object o, EventArgs e)
        {
            this.Close();
        }

        public static void LoadAndRun(MainForm form)
        {
            form.HandleCreated += delegate
            {
                new Thread(new ThreadStart(delegate
                {
                    Splash splash = new Splash();

                    form.Shown += delegate
                    {
                        splash.Invoke(new EventHandler(splash.KillMe));
                        splash.Dispose();
                    };
                    Application.Run(splash);
                })).Start();
            };

            Application.Run(form);
        }




        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (progressBar1.Value == 1000)
            {
                progressBar1.Value = 1000;
                progressBar1.Value = 0;
            }

            progressBar1.Value += 1;
        }
    }
}

