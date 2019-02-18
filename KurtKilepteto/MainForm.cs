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

        public  Image resizeImage(Image imgToResize, Size size)
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
                    ShowStudentData(cardID, dict[cardID]);
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
                ImageRemoveTick(null, null);
                if (dict.ContainsKey(cardID))
                {
                }
            }
        }

        private int entryValidation()
        {
            return -1;
        }

        private void ShowStudentData(string cardID, string studentID)
        {
      
            //set culture to hungarian
            var culture = new CultureInfo("hu-HU");
            var day = culture.DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek);
           
            //read student's txt
            string currPath = ConfigurationManager.AppSettings["configdir"]+"\\";
            string[] lines = File.ReadLines(Path.GetFullPath(Path.Combine(currPath, studentID)) + ".txt", Encoding.UTF8).ToArray();

            //decide if exit is allowed
            bool exitIsValid = ExitValidation(lines, day, studentID);
            if (exitIsValid)
            {
                //student exit is allowed               
                this.panel1.BackColor = Color.Green;

            } else
            {
                //we know this student, but can't validate the exit based on rules
                this.panel1.BackColor = Color.Red;
            }
            imageRemove.Start();

            ShowStudentPicture(dict[cardID]);
            label1.Invoke(new Action(() => label1.Text = lines[0]));

            // label1.Invoke(new Action(() => label1.Text = d.DayOfWeek.ToString()     ));

        }

        private bool ExitValidation(string[] lines, string day, string studentID)
        {
            DateTime validFrom = new DateTime( Convert.ToInt32( lines[2].Split('-')[0] ), Convert.ToInt32(lines[2].Split('-')[1]), Convert.ToInt32(lines[2].Split('-')[2]));
            if ( DateTime.Compare(validFrom, DateTime.Now ) <=0  )
            {
                //validFrom is actual now, 

                //search lines which are valid currently
                for (int i = 3; i < lines.Length; i++)
                {
                    string hunAbrevDayName = lines[i].Split(',')[0];

                    if ( ! string.IsNullOrEmpty(  hunAbrevDayName  ) )
                    if (  (day.ToUpper().StartsWith(hunAbrevDayName.ToUpper()))  || hunAbrevDayName.Equals("*"))
                    {
                        //allowed interval on specific day
                        string fullInterval = lines[i].Split(',')[1]; ;
                        string intervalStart = fullInterval.Split('-')[0];
                        string intervalEnd = fullInterval.Split('-')[1];

                        DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(intervalStart.Split(':')[0]), Convert.ToInt32(intervalStart.Split(':')[1]), 0);
                        DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(intervalEnd.Split(':')[0]), Convert.ToInt32(intervalEnd.Split(':')[1]), 0);

                        //start time of exit is in the past or not
                        if (DateTime.Compare (startDate, DateTime.Now) <=0 )
                        {
                            //end time of exit is in the future
                            if (DateTime.Compare(endDate, DateTime.Now) > 0)
                            {
                                    //student can exit
                                    //log on screen and in file the used rule
                                    AddEvent( DateTime.Now +   " Exit is allowed! Rule: " + lines[i] + " studentID: " + studentID);
                                    Log.Information(" Exit is allowed! Rule: " + lines[i] + " studentID: " + studentID);
                                    return true;
                            }
                        }
                    }
                }


                //all rules are checked but allowance not found
                AddEvent(DateTime.Now + " Exit is not allowed to " + lines[0] + " studentID: " + studentID);
                Log.Information(" Exit is not allowed to " + lines[0] + " studentID: " + studentID);
                return false;
            }
            else
            {
                //currently is not valid student
                AddEvent("Exit is not allowed! Student is not valid today! " + studentID);
                Log.Information("Exit is not allowed! Student is not valid today! " + studentID);
                return false;
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
