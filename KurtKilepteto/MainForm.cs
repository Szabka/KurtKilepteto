using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;
using System.Configuration;
using System.Drawing.Imaging;

namespace KurtKilepteto
{
    public partial class MainForm : Form
    {
        private const int SECOND = 1000;
        Queue<string> cardEvents = new Queue<string>();
        Dictionary<string, string> dict = new Dictionary<string, string>();
        System.Timers.Timer imageRemove; // kep eltunteto timer

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            OptimizePicturesSize();
            dict = File.ReadLines(ConfigurationManager.AppSettings["configdir"]+"\\nyilvantartas.csv").Select(line => line.Split(';')).ToDictionary(line => line[0], line => line[1]);

            imageRemove = new System.Timers.Timer();
            imageRemove.AutoReset = false;
            imageRemove.Interval = int.Parse(ConfigurationManager.AppSettings["imageshowtime"]) * SECOND;
            imageRemove.Elapsed += ImageRemoveTick;
            imageRemove.SynchronizingObject = this;

        }

        private void ImageRemoveTick(Object myObject, System.Timers.ElapsedEventArgs myEventArgs)
        {
            label1.Invoke(new Action(() => label1.Text = "")); // empty label
            this.pictureBoxStudentFace.BackColor = System.Drawing.SystemColors.Control;
            DisposeImage();
        }

        private void DisposeImage()
        {
            if (this.pictureBoxStudentFace.Image != null)
            {
                this.pictureBoxStudentFace.Image.Dispose();
                this.pictureBoxStudentFace.Image = null;
            }
        }

        private void OptimizePicturesSize()
        {
            //AddEvent("Student image resize started.");
            //read all possible image path in an array
            var lines = File.ReadAllLines(ConfigurationManager.AppSettings["configdir"]+"\\nyilvantartas.csv").Select(a => a.Split(';')[1]);
            string currPath = ConfigurationManager.AppSettings["configdir"] + "\\";

            //select and resize every image what we should
            foreach (var line in lines)
            {                
                Image studImg = Image.FromFile(Path.GetFullPath(Path.Combine(currPath, line)) + ".jpg");

                if (studImg.Width != 480 || studImg.Height != 640)
                {
                    Image resizedImage = resizeImage(studImg, new Size(480, 640));
                    studImg.Dispose(); //otherwise we have to save with different name
                    resizedImage.Save(Path.GetFullPath(Path.Combine(currPath, line)) + ".jpg",ImageFormat.Jpeg);
                }
            }

            //AddEvent("Student image resize finished.");

        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }

        
        public void AddEvent(string eventContent)
        {
            cardEvents.Enqueue(DateTime.Now.ToString("HH:mm:ss") +" "+eventContent);
            if (cardEvents.Count >= 10)
                cardEvents.Dequeue();

            string newText = "";
            foreach (string cardEvent in cardEvents.Reverse())
            {
                newText += cardEvent;
                newText += Environment.NewLine;
                newText += Environment.NewLine;
            }
            textBox1.Invoke(new Action(() => textBox1.Text = newText));

        }

        public void CardRead(string readerName, String cardID)
        {
            Log.Debug("DEBUG,"+readerName + ";" + cardID);
            //student is trying go out
            if (ConfigurationManager.AppSettings["exitreadername"].Equals(readerName))
            {
                imageRemove.Stop();
                if (dict.ContainsKey(cardID))
                {
                    string studentID = dict[cardID];
                    ShowStudentPicture(studentID);

                    StudentData sd = new StudentData(dict[cardID]);
                    label1.Invoke(new Action(() => label1.Text = sd.StudentInfo));

                    if (sd.CardValid(DateTime.Now))
                    {
                        if (sd.HasMatchingRule(DateTime.Now))
                        {
                            //student exit is allowed               
                            AddEvent(sd.ShortInfo+Environment.NewLine+ "Érvényes kilépés");
                            Log.Information(cardID+",EXIT,Érvényes kilépés," +sd.ShortInfo + "," + studentID);
                            this.pictureBoxStudentFace.BackColor = Color.Green;
                        }
                        else
                        {
                            //we know this student, but can't validate the exit based on rules
                            AddEvent(sd.ShortInfo + Environment.NewLine + "Érvénytelen kilépés");
                            Log.Information(cardID + ",EXIT,Érvénytelen kilépés," + sd.ShortInfo + "," + studentID);
                            this.pictureBoxStudentFace.BackColor = Color.Red;
                        }
                    }
                    else
                    {
                        AddEvent(sd.ShortInfo + Environment.NewLine + "Érvénytelen kártya " + sd.ValidFromS);
                        Log.Information(cardID + ",EXIT,Érvénytelen kártya," + sd.ShortInfo+","+ sd.ValidFromS);
                        this.pictureBoxStudentFace.BackColor = Color.Red;
                    }
                }
                else
                {
                    DisposeImage();
                    this.pictureBoxStudentFace.BackColor = Color.Red;
                    label1.Invoke(new Action(() => label1.Text = "Ismeretlen kártya" + Environment.NewLine + cardID));
                    AddEvent("Ismeretlen kártya " + Environment.NewLine + cardID);
                    Log.Information(cardID + ",EXIT,Ismeretlen kártya");
                }
                imageRemove.Start();
            }
            else if (ConfigurationManager.AppSettings["entrancereadername"].Equals(readerName))
            {
                if (dict.ContainsKey(cardID))
                {
                    string studentID = dict[cardID];
                    StudentData sd = new StudentData(studentID);
                    if (sd.CardValid(DateTime.Now))
                    {
                        if (sd.HasMatchingRule(DateTime.Now))
                        {
                            Log.Information(cardID + ",ENTRY,Érvényes belépés," + sd.ShortInfo + "," + studentID);
                        }
                        else
                        {
                            Log.Information(cardID + ",ENTRY,Érvénytelen belépés," + sd.ShortInfo + "," + studentID);
                        }
                    }
                    else
                    {
                        Log.Information(cardID + ",ENTRY,Érvénytelen kártya," + sd.ShortInfo + "," + sd.ValidFromS);

                    }
                    {
                        Log.Information(cardID + ",ENTRY,Ismeretlen kártya," + cardID);
                    }
                }
            } else
            {
                Log.Information("UNKNOWN,card read event received," + readerName + "," + cardID);
            }
        }


        private void ShowStudentPicture(string studentID)
        {
            DisposeImage();
            string currPath = ConfigurationManager.AppSettings["configdir"] + "\\";           
            Image studImg = Image.FromFile(Path.GetFullPath(Path.Combine(currPath, studentID))  + ".jpg");
            this.pictureBoxStudentFace.Image = studImg;
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void DetectReadersMenuItem_Click(object sender, EventArgs e)
        {
            AddEvent("READERLIST"+ Environment.NewLine + string.Join(Environment.NewLine, Program.GetReaderNames()));
        }
    }
}
