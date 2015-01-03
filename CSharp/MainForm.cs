using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace LEDLCDOLED
{
    public partial class MainForm : Form
    {
        System.IO.Ports.SerialPort p = new System.IO.Ports.SerialPort("COM8", 115200);       

        const byte MAXLED_COLOR = 0xFE;
        bool ArduinoReady = false;
        byte lastmode = 0;
        int delay = 0;
        byte fcnt = 0;

        public MainForm()
        {
            InitializeComponent();
            p.DataReceived += p_DataReceived;
            p.Open();
        }

        void UpdateText()
        {
          ldelay.Text = "Delay = " + delay.ToString();
        }
        public delegate void UpdateTextDelegate();

        void UpdateFifo()
        {
            lfifo.Text = "Fifo = " + fcnt.ToString();
        }
        public delegate void UpdateFifoDelegate();

        void p_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            char c = (char)p.ReadByte();
            if (c == 'R')
            {
                ArduinoReady = true;
            }
            else if (c == 'S')
            {
                delay++;
                UpdateTextDelegate d = new UpdateTextDelegate(UpdateText);
                ldelay.BeginInvoke(d);
            }
            else
            {
                fcnt = (byte)c;
                UpdateFifoDelegate d = new UpdateFifoDelegate(UpdateFifo);
                lfifo.BeginInvoke(d);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            byte[] data = new byte[4];
            data[0] = 100;      // Good Bye Gode
            data[1] = 0xFF;      // OxFF tells Leds to show
            p.Write(data, 0, 2);

            p.Close();
        }

        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 254) return 254;
            return i;
        }


        bool blinkstate;
        static int leds = 150;
        byte[] data = new byte[leds * 3 + 1];

        private void Clear()
        {
            for (int j = 0; j < data.Length; j++)
            {
                data[j] = 0;
            }
        }

        private void SetColor(int led, byte r, byte g, byte b)
        {
            if ((led >= 0) && (led < leds))
            {
                data[led * 3] = r;
                data[(led * 3) + 1] = g;
                data[(led * 3) + 2] = b;
            }
        }

        private void SetRed(int led)
        {
            if ((led >= 0) && (led < leds))
                data[led * 3] = 0xFE;
        }

        private void SetGreen(int led)
        {
            if ((led >= 0) && (led < leds))
                data[led * 3 + 1] = 0xFE;
        }

        private void SetBlue(int led)
        {
            if ((led >= 0) && (led < leds))
              data[led * 3 + 2] = 0xFE;
        }

        Random ran = new Random();
        private void RandomColor(int led)
        {
            int r = ran.Next(5);
            switch (r)
            {
                case 0: SetRed(led); break;
                case 1: SetGreen(led); break;
                case 2: SetBlue(led); break;
                case 3: SetRed(led); SetGreen(led);  break;
                case 4: SetRed(led); SetBlue(led);  break;
                case 5: SetGreen(led); SetBlue(led); break;
            }
        }

        private void Clock1()
        {
            Mode = 1;
            System.DateTime t = System.DateTime.Now;
            Clear();
            for(int i = 0; i < t.Hour; i++)
                RandomColor(i);

            for(int i=0; i < t.Minute; i++)
                RandomColor(i + 23);

            for(int i=0;i < t.Second; i++)
                RandomColor(i + 84);
        }

        private void Clock2()
        {
            Mode = 2;
            System.DateTime t = System.DateTime.Now;
            Clear();
            for (int i = 0; i < t.Hour; i++)
                SetColor(i, (byte)ran.Next(254), (byte)ran.Next(254), (byte)ran.Next(254));

            for (int i = 0; i < t.Minute; i++)
                SetColor(i+23, (byte)ran.Next(254), (byte)ran.Next(254), (byte)ran.Next(254));

            for (int i = 0; i < t.Second; i++)
                SetColor(i+84, (byte)ran.Next(254), (byte)ran.Next(254), (byte)ran.Next(254));
        }

        private void Clock3()
        {
            Mode = 3;
            System.DateTime t = System.DateTime.Now;
            Clear();
            for (int i = 0; i < t.Hour; i++)
            {
                int r, g, b;
                double ra = ran.NextDouble();
                double s = ran.NextDouble();
                double v = ran.NextDouble();
                HsvToRgb(ra * 360, 1.0, v, out r, out g, out b);
                SetColor(i, (byte)r, (byte)g, (byte)b);
            }

            for (int i = 0; i < t.Minute; i++)
            {
                int r, g, b;
                double ra = ran.NextDouble();
                HsvToRgb(ra * 360, 1.0, 1.0, out r, out g, out b);
                SetColor(i+23, (byte)r, (byte)g, (byte)b);
            }

            for (int i = 0; i < t.Second; i++)
            {
                int r, g, b;
                double ra = ran.NextDouble();
                HsvToRgb(ra * 360, 1.0, 1.0, out r, out g, out b);
                SetColor(i+84, (byte)r, (byte)g, (byte)b);
            }
        }

        int midblink = 0;
        int slowblink = 0;
        private void Clock4()
        {
            Mode = 4;
            System.DateTime t = System.DateTime.Now;
            Clear();
            blinkstate = !blinkstate;
            if (++slowblink == 10) slowblink = 0;
            if (++midblink == 5) midblink = 0;

            for (int i = 0; i < 60; i++)
            {
                if (i < t.Hour) SetRed(i);
                if (i < t.Minute) SetGreen(i);
                if (i < t.Second) SetBlue(i);
                if ((slowblink == 0) && (i == t.Hour - 1)) SetColor(i, 0, 0, 0);
                if ((midblink == 0) && (i == t.Minute - 1)) SetColor(i, 0, 0, 0);
                if (blinkstate && (i == t.Second - 1)) SetColor(i, 0, 0, 0);
            }
        }

        int h = 0;
        private void Spin()
        {
            Mode = 5;
            timer1.Enabled = false;
            //Clear();

            int r, g, b;
            for (int i = 0; i < 150; i++)
            {
                h += 3; if (h == 360) h = 0;
                HsvToRgb(h, 1.0, 1.0, out r, out g, out b);
                SetColor(i, (byte)r, (byte)g, (byte)b);
                SetColor((leds-1)-i, (byte)r, (byte)g, (byte)b);

//                for (int j = 0; j < 2; j++)
                //{
                    //HsvToRgb(ran.NextDouble() * 360, 1.0, 1.0, out r, out g, out b);
                    //SetColor(ran.Next(leds), (byte)r, (byte)g, (byte)b);
                //}
                Send();
                Thread.Sleep(10);
            }
            timer1.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[4];
            data[0] = 0x00;      // Max color is 254
            data[1] = 0x00;
            data[2] = 0xFE;
            data[3] = 0xFF;      // OxFF tells Leds to show
            p.Write(data, 0, 4);
        }

        private void button3_Click(object sender, EventArgs e)
        {
        }


        byte Mode = 0;
        int whichclock = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            if (cb1.Checked)
            {
                switch (whichclock)
                {
                    case 0: Clock1(); break;
                    case 1: Clock2(); break;
                    case 2: Clock3(); break;
                    case 3: Clock4(); break;
                }

                /*
                if ((whichclock & 0x02) > 0)
                {
                    SetColor(148, 0, 0, 254);
                }
                if ((whichclock & 0x01) > 0)
                {
                    SetColor(149, 0, 0, 254);
                }
                */
            }
            else if (cb3.Checked)
                Spin();
            else
                Clock3();

            Send();
            timer1.Enabled = true;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            whichclock++;
            if (whichclock == 4) whichclock = 0;
        }


        private void NOP(long durationTicks)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedTicks < durationTicks)
            { }
        }

        private void Send()
        {
            if (ArduinoReady == true)
            {
                int blocksize = 16;

                // Clean LED Color Data so we don't send any bad Values
                for (int k = 0; k < data.Length; k++)
                {
                    data[k] = Math.Min((byte)MAXLED_COLOR, data[k]);
                }

                int chunk = 0;
                data[(leds * 3)] = 0xFF;      // OxFF tells Leds to show

                // Send the Mode First
                byte[] ba = new byte[1];
                ba[0] = Mode;
                p.Write(ba, 0, 1);

                // Check if we need to slow down due to screen updating
                if (Mode != lastmode)
                {
                    lastmode = Mode;
                }

                while (chunk < data.Length)
                {
                    int size = Math.Min(blocksize, data.Length - chunk);
                    p.Write(data, chunk, size);
                    Thread.Sleep(4);
                    chunk += size;
                }
            } // if Ready
        } // Send
    }
}
