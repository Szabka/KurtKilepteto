using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;

namespace KurtKilepteto
{
    public class StudentData
    {
        private string studentID;
        private string studentInfo;
        private string shortInfo;
        private string studentNote;
        private DateTime ValidFrom;
        private string validFromS;
        private string[] rules;

        public StudentData(string studentID)
        {
            this.StudentID = studentID;

            //read student's txt
            string currPath = ConfigurationManager.AppSettings["configdir"] + "\\";
            string[] lines = File.ReadLines(Path.GetFullPath(Path.Combine(currPath, studentID)) + ".txt", Encoding.UTF8).ToArray();
            ShortInfo = lines[0];
            StudentInfo = lines[0] + Environment.NewLine + lines[1];
            studentNote = lines[2];
            ValidFromS = lines[3];
            string[] dateparts = lines[3].Split('-');
            ValidFrom = new DateTime(Convert.ToInt32(dateparts[0]), Convert.ToInt32(dateparts[1]), Convert.ToInt32(dateparts[2]));
            rules = new string[lines.Length - 4];
            System.Array.Copy(lines,4,rules,0,lines.Length-4);
        }

        public string StudentID { get => studentID; set => studentID = value; }
        public string StudentInfo { get => studentInfo; set => studentInfo = value; }
        public string ShortInfo { get => shortInfo; set => shortInfo = value; }
        public string ValidFromS { get => validFromS; set => validFromS = value; }

        public Boolean CardValid(DateTime d)
        {
            return (DateTime.Compare(ValidFrom, d) <= 0);
        }

        public Boolean HasMatchingRule(DateTime d)
        {
            var culture = new CultureInfo("hu-HU");
            var day = culture.DateTimeFormat.GetDayName(d.DayOfWeek);

            //search lines which are valid currently
            for (int i = 0; i < rules.Length; i++)
            {
                string hunAbrevDayName = rules[i].Split(',')[0];

                if (!string.IsNullOrEmpty(hunAbrevDayName))
                    if ((day.ToUpper().StartsWith(hunAbrevDayName.ToUpper())) || hunAbrevDayName.Equals("*"))
                    {
                        //allowed interval on specific day
                        string fullInterval = rules[i].Split(',')[1]; ;
                        string intervalStart = fullInterval.Split('-')[0];
                        string intervalEnd = fullInterval.Split('-')[1];

                        DateTime startDate = new DateTime(d.Year, d.Month, d.Day, Convert.ToInt32(intervalStart.Split(':')[0]), Convert.ToInt32(intervalStart.Split(':')[1]), 0);
                        DateTime endDate = new DateTime(d.Year, d.Month, d.Day, Convert.ToInt32(intervalEnd.Split(':')[0]), Convert.ToInt32(intervalEnd.Split(':')[1]), 0);

                        //start time of exit is in the past or not
                        if (DateTime.Compare(startDate, d) <= 0)
                        {
                            //end time of exit is in the future
                            if (DateTime.Compare(endDate, d) > 0)
                            {
                                //student can exit
                                return true;
                            }
                        }
                    }
            }
            return false;

        }

    }
}
