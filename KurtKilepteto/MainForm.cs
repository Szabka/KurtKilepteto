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
using System.Drawing.Drawing2D;

namespace KurtKilepteto {
    public partial class MainForm:Form {
        private const int SECOND = 1000;
        Queue<string> cardExitEvents = new Queue<string>();
        Queue<string> cardEntryEvents = new Queue<string>();
        Dictionary<string, string> dict = new Dictionary<string, string>();
        System.Timers.Timer imageRemove; // kep eltunteto timer
        string currPath;
        Boolean showEntryEvents;

        public MainForm() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            currPath = ConfigurationManager.AppSettings["configdir"] + "\\";
            dict = new Dictionary<string, string>();

            imageRemove = new System.Timers.Timer();
            imageRemove.AutoReset = false;
            imageRemove.Interval = int.Parse(ConfigurationManager.AppSettings["imageshowtime"]) * SECOND;
            imageRemove.Elapsed += ImageRemoveTick;
            imageRemove.SynchronizingObject = this;
            imageRemove.Stop();

            showEntryEvents = false;

            var lines = File.ReadAllLines(currPath + "nyilvantartas.csv");
            foreach (var liner in lines) {
                if (liner.Length > 1 && !liner.StartsWith("#") && liner.Contains(",")) {
                    string[] linearr = liner.Split(',');
                    if (linearr.Length == 2&&linearr[1].Length==8) { // Csak akkor foglalkozunk vele, ha a cardid bennevan
                        string cardid = linearr[1].ToUpper();
                        if (!dict.ContainsKey(cardid)) {
                            dict.Add(cardid, linearr[0]);
                        } else {
                            Log.Error("Double entry for the same cardid:"+cardid+" e1:"+dict[cardid]+" e2:"+linearr[0] );
                        }
                    }
                }
            }
        }

        private void ImageRemoveTick(Object myObject, System.Timers.ElapsedEventArgs myEventArgs) {
            label1.Invoke(new Action(() => label1.Text = "")); // empty label
            this.pictureBoxStudentFace.BackColor = System.Drawing.SystemColors.Control;
            DisposeImage();
        }

        private void DisposeImage() {
            if (this.pictureBoxStudentFace.Image != null) {
                this.pictureBoxStudentFace.Image.Dispose();
                this.pictureBoxStudentFace.Image = null;
            }
        }

        public void AddEvent(Boolean entry,string eventContent) {
            Queue<string> localQueue = entry ? cardEntryEvents : cardExitEvents;
            localQueue.Enqueue(DateTime.Now.ToString("HH:mm:ss") + " " + eventContent);
            if (localQueue.Count > 10) {
                localQueue.Dequeue();
            }

            ShowEvents();
        }

        private void ShowEvents() {
            Queue<string> localQueue = showEntryEvents ? cardEntryEvents : cardExitEvents;
            string newText = "";
            foreach (string cardEvent in localQueue.Reverse()) {
                newText += cardEvent;
                newText += Environment.NewLine;
                newText += Environment.NewLine;
            }
            textBox1.Invoke(new Action(() => textBox1.Text = newText));
        }

        public void CardRead(string readerName, String cardID) {
            Log.Debug("DEBUG," + readerName + ";" + cardID);
            //student is trying go out
            if (ConfigurationManager.AppSettings["exitreadername"].Equals(readerName)) {
                imageRemove.Stop();
                if (dict.ContainsKey(cardID)) {
                    string studentID = dict[cardID];
                    ShowStudentPicture(studentID);

                    StudentData sd = StudentData.Load(dict[cardID]);
                    if (sd==null) {
                        AddEvent(false, "Diák adatok nem beolvashatóak " + Environment.NewLine + dict[cardID]);
                        Log.Information(cardID + ",EXIT,Adathiba,"+dict[cardID]);
                    } else {
                        label1.Invoke(new Action(() => label1.Text = sd.StudentInfo));
                        if (sd.CardValid(DateTime.Now)) {
                            if (sd.HasMatchingRule(DateTime.Now)) {
                                //student exit is allowed               
                                AddEvent(false, sd.ShortInfo + Environment.NewLine + "Érvényes kilépés");
                                Log.Information(cardID + ",EXIT,Érvényes kilépés," + sd.ShortInfo + "," + studentID);
                                this.pictureBoxStudentFace.BackColor = Color.Green;
                            } else {
                                //we know this student, but can't validate the exit based on rules
                                AddEvent(false, sd.ShortInfo + Environment.NewLine + "Érvénytelen kilépés");
                                Log.Information(cardID + ",EXIT,Érvénytelen kilépés," + sd.ShortInfo + "," + studentID);
                                this.pictureBoxStudentFace.BackColor = Color.Red;
                            }
                        } else {
                            AddEvent(false, sd.ShortInfo + Environment.NewLine + "Érvénytelen kártya " + sd.ValidFromS);
                            Log.Information(cardID + ",EXIT,Érvénytelen kártya," + sd.ShortInfo + "," + sd.ValidFromS);
                            this.pictureBoxStudentFace.BackColor = Color.Red;
                        }
                    }
                } else {
                    DisposeImage();
                    this.pictureBoxStudentFace.BackColor = Color.Red;
                    label1.Invoke(new Action(() => label1.Text = "Ismeretlen kártya" + Environment.NewLine + cardID));
                    AddEvent(false, "Ismeretlen kártya " + Environment.NewLine + cardID);
                    Log.Information(cardID + ",EXIT,Ismeretlen kártya");
                }
                imageRemove.Start();
            } else if (ConfigurationManager.AppSettings["entrancereadername"].Equals(readerName)) {
                if (dict.ContainsKey(cardID)) {
                    string studentID = dict[cardID];
                    StudentData sd = StudentData.Load(studentID);
                    if (sd==null) {
                        AddEvent(true, "Diák adatok nem beolvashatóak " + Environment.NewLine + dict[cardID]);
                        Log.Information(cardID + ",ENTRY,Adathiba,"+dict[cardID]);
                    } else {
                        if (sd.CardValid(DateTime.Now)) {
                            if (sd.HasMatchingRule(DateTime.Now)) {
                                AddEvent(true, sd.ShortInfo + Environment.NewLine + "Érvényes belépés");
                                Log.Information(cardID + ",ENTRY,Érvényes belépés," + sd.ShortInfo + "," + studentID);
                            } else {
                                AddEvent(true, sd.ShortInfo + Environment.NewLine + "Érvénytelen belépés");
                                Log.Information(cardID + ",ENTRY,Érvénytelen belépés," + sd.ShortInfo + "," + studentID);
                            }
                        } else {
                            AddEvent(true, sd.ShortInfo + Environment.NewLine + "Érvénytelen kártya " + sd.ValidFromS);
                            Log.Information(cardID + ",ENTRY,Érvénytelen kártya," + sd.ShortInfo + "," + sd.ValidFromS);
                        }
                    }
                } else {
                    AddEvent(true, "Ismeretlen kártya " + Environment.NewLine + cardID);
                    Log.Information(cardID + ",ENTRY,Ismeretlen kártya");
                }
            } else {
                Log.Information("UNKNOWN,card read event received," + readerName + "," + cardID);
            }
        }


        private void ShowStudentPicture(string studentID) {
            DisposeImage();
            string imagepath = Path.GetFullPath(Path.Combine(currPath, studentID)) + ".jpg";
            if (File.Exists(imagepath)) {
                Image studImg = Image.FromFile(imagepath);

                if (studImg.Width != 480 || studImg.Height != 640) {
                    Image resizedImage = ResizeImage(studImg, 480, 640);
                    studImg.Dispose(); //otherwise we have to save with different name
                    resizedImage.Save(Path.GetFullPath(Path.Combine(currPath, studentID)) + ".jpg", ImageFormat.Jpeg);
                    studImg = resizedImage;
                }

                this.pictureBoxStudentFace.Image = studImg;
            }

        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height) {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }
        private void DetectReadersMenuItem_Click(object sender, EventArgs e) {
            AddEvent(false,"READERLIST" + Environment.NewLine + string.Join(Environment.NewLine, Program.GetReaderNames()));
        }

        private void SwitchEventsMenuItem_Click(object sender, EventArgs e) {
            showEntryEvents = !showEntryEvents;
            this.switchEventsToolStripMenuItem.Checked = showEntryEvents;
            ShowEvents();
        }

    }
}
