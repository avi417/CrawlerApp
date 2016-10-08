using CrawlerApp.Helpers;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrawlerApp
{
    public partial class MainForm : Form
    {

        #region Variables

        private static bool stopSignal;
        private static Task task;

        #endregion

        #region Logic Methods

        private void SetStopSignal(bool signal = true)
        {
            stopSignal = signal;
        }

        public static bool GetStopSignal()
        {
            return stopSignal;
        }        

        #endregion

        #region Form Interaction Methods

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SetStopSignal();
        }

        private void startScrapperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!MainForm.stopSignal)
            {                                       
                ShowLog("Start aborted. Scan is already running!", Color.Red);
                return;
            }

            if (task != null && !task.IsCompleted)
            {
                ShowLog("Start aborted. Scan has the signal to stop, but is still running!", Color.Red);
                return;
            }

            task = Task.Factory.StartNew(() =>
            {
                ShowLog("Starting to run SmartScrap");
                SmartScrap scrap = new SmartScrap("(%p)", 3000, 10000);
                SetStopSignal(false);
                scrap.LogCallback += new SmartScrap.CallbackEventHandler(LogCallback);
                scrap.ProcessTask();
                SetStopSignal();
                ShowLog("Ended to run SmartScrap");
            });
        }

        private void stopScrapperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!GetStopSignal())
            {
                SetStopSignal();
                ShowLog("Stop signal has been set!", Color.Red);
            }
           
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox_log_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox_log.Lines.Length >= 1500)
                richTextBox_log.Text = richTextBox_log.Lines[richTextBox_log.Lines.Length - 1].ToString();
            richTextBox_log.SelectionStart = richTextBox_log.Text.Length;
            if (richTextBox_log.Focused == false)
                richTextBox_log.ScrollToCaret();
        }
       
        void LogCallback(string textToLog, Color? color = null)
        {
            ShowLog(textToLog, color);
        }

        public void ShowLog(string text, Color? color = null)
        {
            if (color.HasValue == false)
                color = Color.White;
            Invoke((MethodInvoker)delegate
            {
                richTextBox_log.SelectionStart = richTextBox_log.TextLength;
                richTextBox_log.SelectionLength = text.Length;
                richTextBox_log.SelectionColor = color.Value;
                richTextBox_log.AppendText(Environment.NewLine + DateTime.Now.ToString("HH:mm:ss") + " - " + text);
            });
        }

        #endregion

    }
}
