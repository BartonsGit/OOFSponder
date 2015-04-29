﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Exchange.WebServices.Data;
using System.Security.Cryptography;
using System.Timers;
using mshtml;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace OOFScheduling
{
    public partial class Form1 : Form
    {
        private ContextMenu trayMenu;

        //Track if force close or just hitting X to minimize
        private bool minimize = true;

        //Track if we have turned on the manual oof message
        private bool manualoof = false;
        public Form1()
        {
            InitializeComponent();

            // The path to the key where Windows looks for startup applications
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            //Path to launch shortcut
            string startPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs)
                               + @"\Microsoft\OOFSponder.appref-ms";

            rkApp.SetValue("OOFSponder", startPath);

            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Run Manually", RunManualMenu);
            trayMenu.MenuItems.Add("Exit", OnExit);

            // Add menu to tray icon and show it.
            notifyIcon1.ContextMenu = trayMenu;

            if (Properties.Settings.Default.EmailAddress != "default")
            {
                emailAddressTB.Text = Properties.Settings.Default.EmailAddress;
            }

            if (Properties.Settings.Default.OOFHtmlExternal != "default")
            {
                htmlEditorControl1.BodyHtml = Properties.Settings.Default.OOFHtmlExternal;
            }

            if (Properties.Settings.Default.OOFHtmlInternal != "default")
            {
                htmlEditorControl2.BodyHtml = Properties.Settings.Default.OOFHtmlInternal;
            }

            if (Properties.Settings.Default.workingHours != "default")
            {
                string[] workingHours = Properties.Settings.Default.workingHours.Split('|');

                //Zero means you are off that day (not working) therefore the box is checked
                string[] dayHours = workingHours[0].Split('~');
                if (dayHours[2] == "0") {sundayOffWorkCB.Checked = true; } else {sundayOffWorkCB.Checked = false;}
                sundayStartTimepicker.Value = DateTime.Parse(dayHours[0]);
                sundayEndTimepicker.Value = DateTime.Parse(dayHours[1]);

                dayHours = workingHours[1].Split('~');
                if (dayHours[2] == "0") {mondayOffWorkCB.Checked = true; } else {mondayOffWorkCB.Checked = false;}
                mondayStartTimepicker.Value = DateTime.Parse(dayHours[0]);
                mondayEndTimepicker.Value = DateTime.Parse(dayHours[1]);

                dayHours = workingHours[2].Split('~');
                if (dayHours[2] == "0") {tuesdayOffWorkCB.Checked = true; } else {tuesdayOffWorkCB.Checked = false;}
                tuesdayStartTimepicker.Value = DateTime.Parse(dayHours[0]);
                tuesdayEndTimepicker.Value = DateTime.Parse(dayHours[1]);

                dayHours = workingHours[3].Split('~');
                if (dayHours[2] == "0") {wednesdayOffWorkCB.Checked = true; } else {wednesdayOffWorkCB.Checked = false;}
                wednesdayStartTimepicker.Value = DateTime.Parse(dayHours[0]);
                wednesdayEndTimepicker.Value = DateTime.Parse(dayHours[1]);

                dayHours = workingHours[4].Split('~');
                if (dayHours[2] == "0") {thursdayOffWorkCB.Checked = true; } else {thursdayOffWorkCB.Checked = false;}
                thursdayStartTimepicker.Value = DateTime.Parse(dayHours[0]);
                thursdayEndTimepicker.Value = DateTime.Parse(dayHours[1]);

                dayHours = workingHours[5].Split('~');
                if (dayHours[2] == "0") {fridayOffWorkCB.Checked = true; } else {fridayOffWorkCB.Checked = false;}
                fridayStartTimepicker.Value = DateTime.Parse(dayHours[0]);
                fridayEndTimepicker.Value = DateTime.Parse(dayHours[1]);

                dayHours = workingHours[6].Split('~');
                if (dayHours[2] == "0") { saturdayOffWorkCB.Checked = true; } else { saturdayOffWorkCB.Checked = false; }
                saturdayStartTimepicker.Value = DateTime.Parse(dayHours[0]);
                saturdayEndTimepicker.Value = DateTime.Parse(dayHours[1]);
            }

            if (Properties.Settings.Default.EmailAddress != "default" &&
               Properties.Settings.Default.OOFHtmlExternal != "default" &&
               Properties.Settings.Default.OOFHtmlInternal != "default" &&
               Properties.Settings.Default.workingHours != "default" &&
               Properties.Settings.Default.EncryptPW != "default")
            {
                toolStripStatusLabel1.Text = "Ready";
            }
            else
            {
                toolStripStatusLabel1.Text = "Please setup OOFsponder";
            }

            toolStripStatusLabel2.Text = "";
            Loopy();
            RunStatusCheck();

        }

        void Loopy()
        {
            //Every 10 minutes for automation
            var timer = new System.Timers.Timer(600000);
            timer.Enabled = true;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
        }

        public void UpdateStatusLabel(ToolStripStatusLabel ourLabel, String status_text)
        {
            MethodInvoker mi = new MethodInvoker(() => ourLabel.Text = status_text);

            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(mi);
            }
            else
            {
                mi.Invoke();
            }
        }

        private async void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RunSetOof();
            RunStatusCheck();
        }

        private async void RunStatusCheck()
        {
            if (Properties.Settings.Default.EmailAddress != "default" &&
                Properties.Settings.Default.EncryptPW != "default")
            {
                string emailAddress = Properties.Settings.Default.EmailAddress;
                await System.Threading.Tasks.Task.Run(() => checkOOFStatus(emailAddress));
            }
        }

        private async void RunManualOOF()
        {
            if (Properties.Settings.Default.EmailAddress != "default" &&
                Properties.Settings.Default.OOFHtmlExternal != "default" &&
                Properties.Settings.Default.OOFHtmlInternal != "default" &&
                Properties.Settings.Default.EncryptPW != "default")
            {
                string emailAddress = Properties.Settings.Default.EmailAddress;
                string oofMessageExternal = Properties.Settings.Default.OOFHtmlExternal;
                string oofMessageInternal = Properties.Settings.Default.OOFHtmlInternal;
                //Toggle Manual OOF
                await System.Threading.Tasks.Task.Run(() => setManualOOF(emailAddress, oofMessageExternal, oofMessageInternal, !manualoof));
            }
        }

        public async System.Threading.Tasks.Task setManualOOF(string emailAddress, string oofMessageExternal, string oofMessageInternal, bool on)
        {
            toolStripStatusLabel1.Text = DateTime.Now.ToString() + " - Sending to Exchange Server";

            try
            {
                OofSettings myOOFSettings = ExchangeServiceConnection.Instance.service.GetUserOofSettings(emailAddress);

                OofSettings myOOF = new OofSettings();

                // Set the OOF status to be a scheduled time period.
                if(on)
                    myOOF.State = OofState.Enabled;
                else
                    myOOF.State = OofState.Disabled;

                // Select the external audience that will receive OOF messages.
                myOOF.ExternalAudience = OofExternalAudience.All;

                // Set the OOF message for your internal audience.
                myOOF.InternalReply = new OofReply(oofMessageInternal);

                // Set the OOF message for your external audience.
                myOOF.ExternalReply = new OofReply(oofMessageExternal);

                string newinternal = Regex.Replace(myOOF.InternalReply, @"\r\n|\n\r|\n|\r", "\r\n");
                string currentinternal = Regex.Replace(myOOFSettings.InternalReply, @"\r\n|\n\r|\n|\r", "\r\n");
                string newexternal = Regex.Replace(myOOF.ExternalReply, @"\r\n|\n\r|\n|\r", "\r\n");
                string currentexternal = Regex.Replace(myOOFSettings.ExternalReply, @"\r\n|\n\r|\n|\r", "\r\n");

                if (myOOF.State != myOOFSettings.State ||
                    newinternal != currentinternal ||
                    newexternal != currentexternal)
                {
                    // Set value to Server
                    ExchangeServiceConnection.Instance.service.SetUserOofSettings(emailAddress, myOOF);
                    UpdateStatusLabel(toolStripStatusLabel1, DateTime.Now.ToString() + " - OOF Message set on Server");
                    RunStatusCheck();
                }
                else
                {
                    UpdateStatusLabel(toolStripStatusLabel1, DateTime.Now.ToString() + " - No changes needed, OOF Message not set on Server");
                }
                if (on)
                    manualoof = true;
                else
                    manualoof = false;
            }
            catch
            {
                notifyIcon1.ShowBalloonTip(100, "Login Error", "Cannot login to Exchange, please check your password!", ToolTipIcon.Error);
                UpdateStatusLabel(toolStripStatusLabel1, DateTime.Now.ToString() + " - Email or Password incorrect");
                return;
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void RunManualMenu(object sender, EventArgs e)
        {
            RunSetOof();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(100);
                this.ShowInTaskbar = false;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(minimize)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
            
        }

        private async void RunSetOof()
        {
            if (Properties.Settings.Default.EmailAddress != "default" &&
                Properties.Settings.Default.OOFHtmlExternal != "default" && 
                Properties.Settings.Default.OOFHtmlInternal != "default" && 
                Properties.Settings.Default.workingHours != "default" &&
                Properties.Settings.Default.EncryptPW != "default")
            {
                string emailAddress = Properties.Settings.Default.EmailAddress;
                string oofMessageExternal = Properties.Settings.Default.OOFHtmlExternal;
                string oofMessageInternal = Properties.Settings.Default.OOFHtmlInternal;
                DateTime[] oofTimes = getOofTime(Properties.Settings.Default.workingHours);
                if (oofTimes[0] != oofTimes[1])
                {
                    await System.Threading.Tasks.Task.Run(() => setOOF(emailAddress, oofMessageExternal, oofMessageInternal, oofTimes[0], oofTimes[1]));
                }
            }
        }

        public async System.Threading.Tasks.Task setOOF(string EmailAddress, string oofMessageExternal, string oofMessageInternal, DateTime StartTime, DateTime EndTime)
        {
            toolStripStatusLabel1.Text = DateTime.Now.ToString() + " - Sending to Exchange Server";

            try
            {
                OofSettings myOOFSettings = ExchangeServiceConnection.Instance.service.GetUserOofSettings(EmailAddress);

                OofSettings myOOF = new OofSettings();

                // Set the OOF status to be a scheduled time period.
                myOOF.State = OofState.Scheduled;

                // Select the time period during which to send OOF messages.
                myOOF.Duration = new TimeWindow(StartTime, EndTime);

                // Select the external audience that will receive OOF messages.
                myOOF.ExternalAudience = OofExternalAudience.All;

                // Set the OOF message for your internal audience.
                myOOF.InternalReply = new OofReply(oofMessageInternal);

                // Set the OOF message for your external audience.
                myOOF.ExternalReply = new OofReply(oofMessageExternal);

                string newinternal = Regex.Replace(myOOF.InternalReply, @"\r\n|\n\r|\n|\r", "\r\n");
                string currentinternal = Regex.Replace(myOOFSettings.InternalReply, @"\r\n|\n\r|\n|\r", "\r\n");
                string newexternal = Regex.Replace(myOOF.ExternalReply, @"\r\n|\n\r|\n|\r", "\r\n");
                string currentexternal = Regex.Replace(myOOFSettings.ExternalReply, @"\r\n|\n\r|\n|\r", "\r\n");

                if (myOOF.State != myOOFSettings.State ||
                    myOOF.Duration != myOOFSettings.Duration ||
                    newinternal != currentinternal ||
                    newexternal != currentexternal)
                {
                    // Set value to Server
                    ExchangeServiceConnection.Instance.service.SetUserOofSettings(EmailAddress, myOOF);
                    UpdateStatusLabel(toolStripStatusLabel1, DateTime.Now.ToString() + " - OOF Message set on Server");
                    RunStatusCheck();
                }
                else
                {
                    UpdateStatusLabel(toolStripStatusLabel1, DateTime.Now.ToString() + " - No changes needed, OOF Message not set on Server");
                }
            }
            catch
            {
                notifyIcon1.ShowBalloonTip(100, "Login Error", "Cannot login to Exchange, please check your password!", ToolTipIcon.Error);
                UpdateStatusLabel(toolStripStatusLabel1, DateTime.Now.ToString() + " - Email or Password incorrect");
                return;
            }
        }

        public async System.Threading.Tasks.Task checkOOFStatus(string EmailAddress)
        {
            try
            {
                OofSettings myOOFSettings = ExchangeServiceConnection.Instance.service.GetUserOofSettings(EmailAddress);

                string currentStatus = "";

                if (myOOFSettings.State == OofState.Scheduled && (myOOFSettings.Duration.StartTime > DateTime.Now && myOOFSettings.Duration.EndTime < DateTime.Now))
                {
                    currentStatus = "OOF until " + myOOFSettings.Duration.EndTime.ToString();
                }
                else if (myOOFSettings.State == OofState.Scheduled && (myOOFSettings.Duration.StartTime < DateTime.Now || myOOFSettings.Duration.EndTime > DateTime.Now)) 
                {
                    currentStatus = "OOF starting at " + myOOFSettings.Duration.StartTime.ToString();
                }
                else if (myOOFSettings.State == OofState.Enabled)
                {
                    currentStatus = "Currently OOF";
                }
                else if (myOOFSettings.State == OofState.Disabled)
                {
                    currentStatus = "OOF Disabled";
                }


                UpdateStatusLabel(toolStripStatusLabel2, "Current Status: " + currentStatus);
                notifyIcon1.Text = "Current Status: " + currentStatus;
            }
            catch
            {
                notifyIcon1.ShowBalloonTip(100, "Login Error", "Cannot login to Exchange, please check your password!", ToolTipIcon.Error);
                UpdateStatusLabel(toolStripStatusLabel1, DateTime.Now.ToString() + " - Email or Password incorrect or we cannot contact the server please check your settings and try again");
                return;
            }

        }
        
        private async void button1_Click(object sender, EventArgs e)
        {
            RunSetOof();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private string ScheduleString()
        {
            string[] scheduleString = new string[7];

            string checkstring = sundayOffWorkCB.Checked ? "0" : "1";
            scheduleString[0] = sundayStartTimepicker.Value.ToString("hh:mm tt") + "~" + sundayEndTimepicker.Value.ToString("hh:mm tt") + "~" + checkstring;

            checkstring = mondayOffWorkCB.Checked ? "0" : "1";
            scheduleString[1] = mondayStartTimepicker.Value.ToString("hh:mm tt") + "~" + mondayEndTimepicker.Value.ToString("hh:mm tt") + "~" + checkstring;

            checkstring = tuesdayOffWorkCB.Checked ? "0" : "1";
            scheduleString[2] = tuesdayStartTimepicker.Value.ToString("hh:mm tt") + "~" + tuesdayEndTimepicker.Value.ToString("hh:mm tt") + "~" + checkstring;

            checkstring = wednesdayOffWorkCB.Checked ? "0" : "1";
            scheduleString[3] = wednesdayStartTimepicker.Value.ToString("hh:mm tt") + "~" + wednesdayEndTimepicker.Value.ToString("hh:mm tt") + "~" + checkstring;

            checkstring = thursdayOffWorkCB.Checked ? "0" : "1";
            scheduleString[4] = thursdayStartTimepicker.Value.ToString("hh:mm tt") + "~" + thursdayEndTimepicker.Value.ToString("hh:mm tt") + "~" + checkstring;

            checkstring = fridayOffWorkCB.Checked ? "0" : "1";
            scheduleString[5] = fridayStartTimepicker.Value.ToString("hh:mm tt") + "~" + fridayEndTimepicker.Value.ToString("hh:mm tt") + "~" + checkstring;

            checkstring = saturdayOffWorkCB.Checked ? "0" : "1";
            scheduleString[6] = saturdayStartTimepicker.Value.ToString("hh:mm tt") + "~" + saturdayEndTimepicker.Value.ToString("hh:mm tt") + "~" + checkstring;

            return string.Join("|", scheduleString);
        }

        private DateTime[] getOofTime(string workingHours)
        {
            DateTime[] OofTimes = new DateTime[2];
            string[] workingTimesArray = workingHours.Split('|');

            //Hold now time (if our working time hasn't come yet but we are on the next day make now still be yesterday
            //Example: Your off at 5PM April 1st at 12:01 AM April 2nd we don't want to change our OOF to the one for April 2nd, we are still off from April 1st
            // To handle this if the time we get for the Beginning of our working time comes after the current time we fall back a day to use that days oof time.
            DateTime currentCheckDate = DateTime.Now;
            string[] currentWorkingTime = workingTimesArray[(int)currentCheckDate.DayOfWeek].Split('~');

            if (DateTime.Parse(currentCheckDate.ToString("D") + " " + currentWorkingTime[0]) > DateTime.Now)
            {
                currentCheckDate = DateTime.Now.AddDays(-1);
                currentWorkingTime = workingTimesArray[(int)currentCheckDate.DayOfWeek].Split('~');
            }

            DateTime StartTime;
            if (currentWorkingTime[2] == "1")
            {
                StartTime = DateTime.Parse(currentCheckDate.ToString("D") + " " + currentWorkingTime[1]);
            }
            else
            {
                int daysback = -1;
                while (true)
                {
                    DateTime backday = currentCheckDate.AddDays(daysback);
                    string[] oldWorkingTime = workingTimesArray[(int)backday.DayOfWeek].Split('~');
                    if (oldWorkingTime[2] == "1")
                    {
                        StartTime = DateTime.Parse(backday.ToString("D") + " " + oldWorkingTime[1]);
                        break;
                    }
                    else
                    {
                        daysback--;
                    }
                }
            }

            string[] futureWorkingTime = workingTimesArray[(int)currentCheckDate.AddDays(1).DayOfWeek].Split('~');
            DateTime EndTime;
            if (futureWorkingTime[2] == "1")
            {
                EndTime = DateTime.Parse(currentCheckDate.AddDays(1).ToString("D") + " " + futureWorkingTime[0]);
            }
            else
            {
                int daysforward = 1;
                while (true)
                {
                    DateTime comingday = currentCheckDate.AddDays(1).AddDays(daysforward);
                    string[] oldWorkingTime = workingTimesArray[(int)comingday.DayOfWeek].Split('~');
                    if (oldWorkingTime[2] == "1")
                    {
                        EndTime = DateTime.Parse(comingday.ToString("D") + " " + oldWorkingTime[0]);
                        break;
                    }
                    else
                    {
                        daysforward++;
                    }
                }
            }

            OofTimes[0] = StartTime;
            OofTimes[1] = EndTime;

            return OofTimes;
        }

        private void sundayOffWorkCB_CheckedChanged(object sender, EventArgs e)
        {
            if (sundayOffWorkCB.Checked)
            {
                sundayStartTimepicker.Enabled = false;
                sundayEndTimepicker.Enabled = false;
            }
            else
            {
                sundayStartTimepicker.Enabled = true;
                sundayEndTimepicker.Enabled = true;
            }
        }

        private void mondayOffWorkCB_CheckedChanged(object sender, EventArgs e)
        {
            if (mondayOffWorkCB.Checked)
            {
                mondayStartTimepicker.Enabled = false;
                mondayEndTimepicker.Enabled = false;
            }
            else
            {
                mondayStartTimepicker.Enabled = true;
                mondayEndTimepicker.Enabled = true;
            }
        }

        private void tuesdayOffWorkCB_CheckedChanged(object sender, EventArgs e)
        {
            if (tuesdayOffWorkCB.Checked)
            {
                tuesdayStartTimepicker.Enabled = false;
                tuesdayEndTimepicker.Enabled = false;
            }
            else
            {
                tuesdayStartTimepicker.Enabled = true;
                tuesdayEndTimepicker.Enabled = true;
            }
        }

        private void wednesdayOffWorkCB_CheckedChanged(object sender, EventArgs e)
        {
            if (wednesdayOffWorkCB.Checked)
            {
                wednesdayStartTimepicker.Enabled = false;
                wednesdayEndTimepicker.Enabled = false;
            }
            else
            {
                wednesdayStartTimepicker.Enabled = true;
                wednesdayEndTimepicker.Enabled = true;
            }
        }

        private void thursdayOffWorkCB_CheckedChanged(object sender, EventArgs e)
        {
            if (thursdayOffWorkCB.Checked)
            {
                thursdayStartTimepicker.Enabled = false;
                thursdayEndTimepicker.Enabled = false;
            }
            else
            {
                thursdayStartTimepicker.Enabled = true;
                thursdayEndTimepicker.Enabled = true;
            }
        }

        private void fridayOffWorkCB_CheckedChanged(object sender, EventArgs e)
        {
            if (fridayOffWorkCB.Checked)
            {
                fridayStartTimepicker.Enabled = false;
                fridayEndTimepicker.Enabled = false;
            }
            else
            {
                fridayStartTimepicker.Enabled = true;
                fridayEndTimepicker.Enabled = true;
            }
        }

        private void saturdayOffWorkCB_CheckedChanged(object sender, EventArgs e)
        {
            if (saturdayOffWorkCB.Checked)
            {
                saturdayStartTimepicker.Enabled = false;
                saturdayEndTimepicker.Enabled = false;
            }
            else
            {
                saturdayStartTimepicker.Enabled = true;
                saturdayEndTimepicker.Enabled = true;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void saveSettings()
        {
            if (string.IsNullOrEmpty(emailAddressTB.Text))
            {
                MessageBox.Show("Please enter your email address");
            }
            else if (string.IsNullOrEmpty(passwordTB.Text))
            {
                MessageBox.Show("Please enter your password");
            }
            else if (string.IsNullOrEmpty(passwordConfirmTB.Text))
            {
                MessageBox.Show("Please confirm your password");
            }
            else if (passwordConfirmTB.Text != passwordTB.Text)
            {
                MessageBox.Show("Your password does not match the confirmed password, please confirm your password");
            }
            else
            {
                Properties.Settings.Default.EmailAddress = emailAddressTB.Text;
                Properties.Settings.Default.EncryptPW = DataProtectionApiWrapper.Encrypt(passwordTB.Text);
                Properties.Settings.Default.OOFHtmlExternal = htmlEditorControl1.BodyHtml;
                Properties.Settings.Default.OOFHtmlInternal = htmlEditorControl2.BodyHtml;
                Properties.Settings.Default.workingHours = ScheduleString();

                Properties.Settings.Default.Save();

                passwordConfirmTB.Text = "";
                passwordTB.Text = "";

                toolStripStatusLabel1.Text = "Settings Saved";
            }
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            minimize = false;
            Application.Exit();
        }

        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            RunManualOOF();
            if (button3.Text == "Go OOF Now")
            {
                button3.Text = "Return from OOF";
            }
            else
            {
                button3.Text = "Go OOF Now";
            }
        }
    }

    public static class DataProtectionApiWrapper
    {
        /// <summary>
        /// Specifies the data protection scope of the DPAPI.
        /// </summary>
        private const DataProtectionScope Scope = DataProtectionScope.CurrentUser;
        private const string ProgramName = "OOFScheduling";

        public static string Encrypt(this string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            //encrypt data
            var data = Encoding.Unicode.GetBytes(text);
            byte[] encrypted = ProtectedData.Protect(data, Encoding.Unicode.GetBytes(ProgramName), Scope);

            //return as base64 string
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(this string cipher)
        {
            if (cipher == null)
            {
                throw new ArgumentNullException("cipher");
            }

            //parse base64 string
            byte[] data = Convert.FromBase64String(cipher);

            //decrypt data
            byte[] decrypted = ProtectedData.Unprotect(data, Encoding.Unicode.GetBytes(ProgramName), Scope);
            return Encoding.Unicode.GetString(decrypted);
        }

    }

    public sealed class ExchangeServiceConnection
    {
        private static volatile ExchangeServiceConnection instance;
        private static object syncRoot = new Object();
        public ExchangeService service;

        private ExchangeServiceConnection() {
            service = new ExchangeService(ExchangeVersion.Exchange2013);
            string emailAddress = Properties.Settings.Default.EmailAddress;
            string pw = Properties.Settings.Default.EncryptPW;
            service.Credentials = new WebCredentials(emailAddress, DataProtectionApiWrapper.Decrypt(pw));
            service.UseDefaultCredentials = false;

            //Let's roll that beautiful bean footage
            service.TraceEnabled = true;
            service.TraceFlags = TraceFlags.All;

            if(service.Url == null)
            {
                service.AutodiscoverUrl(emailAddress, RedirectionUrlValidationCallback);
            }
        }

        public static ExchangeServiceConnection Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (Properties.Settings.Default.EmailAddress != "default" &&
                            Properties.Settings.Default.EncryptPW != "default")
                        {
                            if (instance == null)
                                instance = new ExchangeServiceConnection();
                        }
                    }
                }

                return instance;
            }
        }

        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }
    }
}
