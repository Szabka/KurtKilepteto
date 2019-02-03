using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;

namespace KurtKilepteto
{
    public partial class MainForm : Form
    {
        Queue<string> cardEvents = new Queue<string>();
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        public void CardRead(string readerName, String cardID)
        {
            Log.Information("card read event received;" + readerName + ";" + cardID);
            cardEvents.Enqueue(readerName + ";" + cardID );
            if (cardEvents.Count >= 10)
                cardEvents.Dequeue();

            string newText = "";
            foreach (string cardEvent in cardEvents)
            {
                newText += cardEvent;
                newText += "\r\n";
            }
            textBox1.Invoke(new Action(() => textBox1.Text = newText));
        }
    }
}
