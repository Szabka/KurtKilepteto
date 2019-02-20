using PCSC;
using PCSC.Exceptions;
using PCSC.Iso7816;
using PCSC.Monitoring;
using PCSC.Utils;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KurtKilepteto
{
    static class Program
    {
        private static readonly IContextFactory _contextFactory = ContextFactory.Instance;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string logFilename = "kklog_.txt";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFilename, outputTemplate: "{Timestamp:yyyy-MM-dd,HH:mm:ss},{Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: LogEventLevel.Information, rollingInterval: RollingInterval.Day)
                .WriteTo.Trace()
                .CreateLogger();

            MainForm mf = new MainForm();
            var readerNames = GetReaderNames();
            Log.Information("READERLIST," + string.Join(",", readerNames));
            List<string> monitoredReaders = new List<String>();
            string r0 = ConfigurationManager.AppSettings["exitreadername"];
            if (Array.IndexOf(readerNames, r0) < 0)
            {
                Log.Information("ERROR,exit reader not found "+r0);
            }
            else
            {
                monitoredReaders.Add(r0);
            }
            string r1 = ConfigurationManager.AppSettings["entrancereadername"];
            if (Array.IndexOf(readerNames, r1) < 0)
            {
                Log.Information("ERROR,entrance reader not found " + r1);
            }
            else
            {
                monitoredReaders.Add(r1);
            }



            if (monitoredReaders.Count>0)
            {
                var monitorFactory = new MonitorFactory(_contextFactory);
                var monitor = monitorFactory.Create(SCardScope.System);
                monitor.CardInserted += (sender, args) => ProcessEvent(mf, args);
                monitor.MonitorException += MonitorException;

                monitor.Start(monitoredReaders.ToArray());
            }
            else
            {
                Log.Error("ERROR,There are currently useable readers installed. "+ readerNames);
            }
            Log.Debug("KurtKilepteto Started");

            Application.Run(mf);
        }

        public static string[] GetReaderNames()
        {
            using (var context = _contextFactory.Establish(SCardScope.System))
            {
                return context.GetReaders();
            }
        }

        private static void ProcessEvent(MainForm mf, CardStatusEventArgs ea)
        {
            if ("Present" == ea.State.ToString())
            {
                string uid = GetCardUID(ea.ReaderName);
                if (uid != null)
                {
                    mf.CardRead(ea.ReaderName, uid);
                }
            }

        }

        private static string GetCardUID(string readerName)
        {
            using (var context = _contextFactory.Establish(SCardScope.System))
            {
                using (var rfidReader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any))
                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, rfidReader.Protocol)
                    {
                        CLA = 0xFF,
                        Instruction = InstructionCode.GetData,
                        P1 = 0x00,
                        P2 = 0x00,
                        Le = 0 // We don't know the ID tag size
                    };

                    using (rfidReader.Transaction(SCardReaderDisposition.Leave))
                    {
                        //Console.WriteLine("Retrieving the UID .... ");

                        var sendPci = SCardPCI.GetPci(rfidReader.Protocol);
                        var receivePci = new SCardPCI(); // IO returned protocol control information.

                        var receiveBuffer = new byte[256];
                        var command = apdu.ToArray();

                        var bytesReceived = rfidReader.Transmit(
                            sendPci, // Protocol Control Information (T0, T1 or Raw)
                            command, // command APDU
                            command.Length,
                            receivePci, // returning Protocol Control Information
                            receiveBuffer,
                            receiveBuffer.Length); // data buffer

                        var responseApdu =
                            new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, rfidReader.Protocol);
                        Log.Debug("Uid: {2} SW1: {0:X2}, SW2: {1:X2}\n",
                            responseApdu.SW1,
                            responseApdu.SW2,
                            responseApdu.HasData ? BitConverter.ToString(responseApdu.GetData()) : "No uid received");
                        return responseApdu.HasData ? BitConverter.ToString(responseApdu.GetData()).Replace("-", "").ToUpper() : null;
                    }

                }
            }
        }

        private static bool NoReaderFound(ICollection<string> readerNames)
        {
            return readerNames == null || readerNames.Count < 1;
        }

        private static void MonitorException(object sender, PCSCException ex)
        {
            Log.Error("MONITORERROR,Monitor exited due an error" + SCardHelper.StringifyError(ex.SCardError));
        }


    }
}
