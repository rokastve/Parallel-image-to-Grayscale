//IFF-6/15 Rokas Tverijonas
//Inžinerinis projektas
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Atidaro dialogą kuriame galima pasirinkti norimą nuotrauką
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            if (open.ShowDialog() == DialogResult.OK)
            {
                Bitmap bt = new Bitmap(open.FileName);
                pictureBox1.Image = bt; //Nuotrauka atvaizduojama
                bt.Save("..\\..\\Pradinis.jpg"); //Perkopijuojama į programos aplanką tolesniam tyrimui
                richTextBox1.AppendText("Nuotraukos aukštis: " + bt.Height + " plotis:" + bt.Width+"\n");
                richTextBox1.AppendText("Iš viso pikselių: " + bt.Height * bt.Width + "\n");
            }          
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Paimamas pradinis bitmap
            Bitmap bmp = (Bitmap)Image.FromFile("..\\..\\Pradinis.jpg");
            
            LockBitmap lockBitmap = new LockBitmap(bmp); //Baitai perkopijuojami į LockBitmap klasę
            lockBitmap.LockBits(); //Baitai užrakinami
            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i< lockBitmap.GetBytesCount(); i = i +3)
            {
                //Pagal 3 pikelio komponentes naudojantis skaistumo formule paskaičiuojama komponentė
                byte gray = (byte)(lockBitmap.Bytes[i] * 0.2126 + lockBitmap.Bytes[i + 1] * 0.7152 + lockBitmap.Bytes[i + 2] * 0.0722);
                //Komponentė priskiriama 3 pikelio baitams
                lockBitmap.Bytes[i] = lockBitmap.Bytes[i + 1] = lockBitmap.Bytes[i + 2] = gray;
            }
            sw.Stop();
            lockBitmap.UnlockBits(); //Baitai atrakinami
            
            richTextBox1.AppendText("Užtrukęs laikas nelygiagrečiai: " + sw.ElapsedMilliseconds + "\n");
            pictureBox2.Image = bmp; //Atvaizduojami rezultatai
            bmp.Save("..\\..\\GalinisNelyg.jpg");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Paimamas pradinis Bitmap
            Bitmap bmp = (Bitmap)Image.FromFile("..\\..\\Pradinis.jpg");
            
            LockBitmap lockBitmap = new LockBitmap(bmp); //Baitai nukopijuojami į LockBitmap klasę
            lockBitmap.LockBits(); //Baitai užrakinami
            int n = int.Parse(textBox1.Text); //Gaunamas gijų skaičius
            Thread[] threadai = new Thread[n]; //Sukuriama n gijų
            ThreadStart[] starteriai = new ThreadStart[n]; //Sukuriame n threadstart
            
            for(int j = 0; j < n;j++)
            {
                int i = j;
                //prideda į ThreadStart delegatų mastyvą
                starteriai[i] = delegate { pakeisti(lockBitmap, lockBitmap.Height * lockBitmap.Width / n * 3 * i, lockBitmap.Height * lockBitmap.Width / n * 3 * (i + 1)); };
                //priskiria ThreadStart gijoms masyve
                threadai[i] = new Thread(starteriai[i]);
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //Paleidžia visas gijas
            for (int i = 0; i < n; i++)
            {
                threadai[i].Start();
            }
            //Prijungia visas gijas
            for (int i = 0; i <n;i++)
            {
                threadai[i].Join();
            }
            sw.Stop();
            lockBitmap.UnlockBits();
            
            richTextBox1.AppendText("Užtrukęs laikas lygiagrečiai: " + sw.ElapsedMilliseconds + "\n");
            pictureBox2.Image = bmp;
            bmp.Save("..\\..\\GalinisLyg.jpg");
            
        }

        /// <summary>
        /// Pakeičia baitus nuo pradinio nurodyto indekso iki paskutinio nurodyto indekso
        /// </summary>
        /// <param name="lockBitmap">lockBitmap klasė</param>
        /// <param name="pradinis">pradinis keitimo baito indeksas</param>
        /// <param name="paskutinis">paskutinis keitimo baito indeksas</param>
        private void pakeisti(LockBitmap lockBitmap, int pradinis, int paskutinis)
        {
            for (int i = pradinis; i < paskutinis; i = i + 3)
            {
                byte gray = (byte)(lockBitmap.Bytes[i] * 0.2126 + lockBitmap.Bytes[i + 1] * 0.7152 + lockBitmap.Bytes[i + 2] * 0.0722);
                lockBitmap.Bytes[i] = lockBitmap.Bytes[i + 1] = lockBitmap.Bytes[i + 2] = gray;
            }
        }
        /// <summary>
        /// Klasė kuri realizuoja lockbit struktūrą, saugo kiekvieno baito duomenis
        /// LockBit: https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.lockbits?view=netframework-4.7.2
        /// </summary>
        public class LockBitmap
        {
            Bitmap source = null; //Bitmap iš kurio sudaroma ši klasė
            IntPtr Iptr = IntPtr.Zero; //Pradinis pointeris
            BitmapData bitmapData = null; //Bitmap duomenys

            public byte[] Bytes { get; set; } //Visų baitų vertės
            public int Width { get; private set; } //Plotis
            public int Height { get; private set; } //Aukštis

            /// <summary>
            /// Gražina bitmap 
            /// </summary>
            /// <param name="source"></param>
            public LockBitmap(Bitmap source)
            {
                this.source = source;
            }

            /// <summary>
            /// Užrakina bitus
            /// </summary>
            public void LockBits()
            {
                try
                {
                    //Gauna plotį ir aukštį
                    Width = source.Width;
                    Height = source.Height;

                    // Apskaičiuoja visus pikselius
                    int PixelCount = Width * Height;

                    // Sukuria stačiakampį kurį užrakins
                    Rectangle rect = new Rectangle(0, 0, Width, Height);

                    // Užrakina bitmap ir gražina bitmap duomenis
                    bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                                 source.PixelFormat);

                    Bytes = new byte[PixelCount * 3];//Sukuria baitų masyvą (pikselių kiekis * 3 (komponentės))
                    Iptr = bitmapData.Scan0; //Nuskaito pradinį pointerį

                    // Kopijuoja nuo pradinio pointerio į pasyvą
                    Marshal.Copy(Iptr, Bytes, 0, Bytes.Length);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// Atrakina bitmap duomenis
            /// </summary>
            public void UnlockBits()
            {
                try
                {
                    // Kopijuoja visus baitus į bitmap nuo pointerio
                    Marshal.Copy(Bytes, 0, Iptr, Bytes.Length);

                    // Atrakina bitmap duomenis
                    source.UnlockBits(bitmapData);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// Gauti baitų skaičių
            /// </summary>
            /// <returns>Baitų skaičių</returns>
            public int GetBytesCount()
            {
                return Bytes.Length;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        #region Be bitlock
        /// <summary>
        /// Bitmap lygiagretumo paleidima
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click_1(object sender, EventArgs e)
        {
            Bitmap bmp = (Bitmap)Image.FromFile("..\\..\\Pradinis.jpg");
            ThreadStart del = delegate { Pakeisti2(bmp, 0 , 500); };
            Thread t1 = new Thread(del);
            ThreadStart del2 = delegate { Pakeisti2(bmp, 600,1000); };
            Thread t2 = new Thread(del);
            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            pictureBox2.Image = bmp;
            bmp.Save("..\\..\\GalinisLyg.jpg");
        }
        /// <summary>
        /// Testavimui bitmap lygiagretumui
        /// </summary>
        /// <param name="bt"></param>
        /// <param name="pradinis"></param>
        /// <param name="paskutinis"></param>
        private unsafe void Pakeisti2(Bitmap bt, int pradinis, int paskutinis)
        {
            for (int y = pradinis; y < paskutinis; y++)
            {
                for (int x = 0; x < bt.Width; x++)
                {
                    Color test = bt.GetPixel(x, y);
                    int gray = (int)(test.R * 0.2126 + test.G * 0.7152 + test.B * 0.0722);
                    bt.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }
            }
        }
        #endregion
    }
}
