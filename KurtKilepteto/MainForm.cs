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
            Log.Information("ImageRemoveTick called");
            label1.Invoke(new Action(() => label1.Text = "")); // empty label
            if (this.pictureBoxStudentFace.Image != null)
            {
                this.pictureBoxStudentFace.Image.Dispose();
                this.pictureBoxStudentFace.Image = null;
            }
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
        }

        private void OptimizePicturesSize()
        {
            AddEvent("Student image resize started.");
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

            AddEvent("Student image resize finished.");

        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }

        
        public void AddEvent(string eventContent)
        {
            cardEvents.Enqueue(DateTime.Now.ToString("H:mm:s") +" "+eventContent);
            if (cardEvents.Count >= 15)
                cardEvents.Dequeue();

            string newText = "";
            foreach (string cardEvent in cardEvents)
            {
                newText += cardEvent;
                newText += Environment.NewLine;
                newText += Environment.NewLine;
            }
            textBox1.Invoke(new Action(() => textBox1.Text = newText));

        }

        public void CardRead(string readerName, String cardID)
        {
            Log.Information("card read event received;" + readerName + ";" + cardID);
            AddEvent(readerName + ";" + cardID);
            imageRemove.Stop();
            //student is trying go out
            if (ConfigurationManager.AppSettings["exitreadername"].Equals(readerName))
            {
                if (dict.ContainsKey(cardID))
                {
                    string studentID = dict[cardID];
                    ShowStudentPicture(studentID);
                    imageRemove.Start();

                    StudentData sd = new StudentData(dict[cardID]);
                    label1.Invoke(new Action(() => label1.Text = sd.StudentInfo));

                    if (sd.CardValid(DateTime.Now))
                    {
                        if (sd.HasMatchingRule(DateTime.Now))
                        {
                            //student exit is allowed               
                            AddEvent(DateTime.Now + " Exit is allowed! studentID: " + studentID);
                            Log.Information(" Exit is allowed! studentID: " + studentID);
                            this.panel1.BackColor = Color.Green;
                        }
                        else
                        {
                            //we know this student, but can't validate the exit based on rules
                            AddEvent(DateTime.Now + " Exit is not allowed to " + sd.StudentInfo + " studentID: " + studentID);
                            Log.Information(" Exit is not allowed to " + sd.StudentInfo + " studentID: " + studentID);
                            this.panel1.BackColor = Color.Red;
                        }
                    }
                    else
                    {
                        AddEvent("Exit is not allowed! Student is not valid today! " + studentID);
                        Log.Information("Exit is not allowed! Student is not valid today! " + studentID);
                    }
                }
                else
                {
                    //TODO inform user - student or card not found
                    this.pictureBoxStudentFace.BackColor = Color.Red;
                    label1.Invoke(new Action(() => label1.Text = "Student not found with this CardID! Foreign card! (Ismeretlen kartya) " + cardID));
                    AddEvent("Student not found with this CardID! Foreign card! (Ismeretlen kartya) " + cardID);
                    Log.Information("Student not found with this CardID! Foreign card! (Ismeretlen kartya) " + cardID);
                }

            }
            else if (ConfigurationManager.AppSettings["entrancereadername"].Equals(readerName))
            {
                if (dict.ContainsKey(cardID))
                {
                    StudentData sd = new StudentData(dict[cardID]);
                    if (sd.CardValid(DateTime.Now))
                    {
                        if (sd.HasMatchingRule(DateTime.Now))
                        {
                            Log.Information("ENTRY,Ervenyes belepes " + cardID);
                        }
                        else
                        {
                            Log.Information("ENTRY,Idon kivuli belepes " + cardID);
                        }
                    }
                    else
                    {
                        Log.Information("ENTRY,Kartya nem ervenyes " + cardID);

                    }
                    {
                        Log.Information("ENTRY,Student not found with this CardID! Foreign card! (Ismeretlen kartya) " + cardID);
                    }
                }
            }
        }


        private void ShowStudentPicture(string studentID)
        {
            if (this.pictureBoxStudentFace.Image!=null)
            {
                this.pictureBoxStudentFace.Image.Dispose();
                this.pictureBoxStudentFace.Image = null;
            }
            string currPath = ConfigurationManager.AppSettings["configdir"] + "\\";           
            Image studImg = Image.FromFile(Path.GetFullPath(Path.Combine(currPath, studentID))  + ".jpg");
            this.pictureBoxStudentFace.Image = studImg;
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
