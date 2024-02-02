using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NAudio;
using NAudio.CoreAudioApi;
using Wooting;

namespace UwUVolume
{
    internal static class Program
    {

        static (int x, int y)[] lightProgress = [(0, 2), (0, 3), (1, 4), (2, 4), (4, 4), (5, 4), (6, 3), (6, 2)];
        static float lastKnownVol = 1.0f;
        static byte UwU = 255;
        static RGBDeviceInfo uwuDeviceInfo = new RGBDeviceInfo();
        static bool enableLighting = false;
        static int eventTimer = 5;
        private static MMDeviceEnumerator enumer = new MMDeviceEnumerator();
        private static MMDeviceCollection devices = enumer.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);


        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            initLighting();
            initAudio();
            

            // start application in the tray
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WootTrayApp());
            //ApplicationConfiguration.Initialize();
            //Application.Run(new Form1());
        }

        static void initAudio() {
            foreach (var dev in devices)
            {
                dev.AudioEndpointVolume.OnVolumeNotification += volChange;
            }
            
        }

        static void volChange(AudioVolumeNotificationData data)
        {
            lastKnownVol = data.MasterVolume;
            if (eventTimer > 0)
            {
                eventTimer = 8;
            }
            else {
                eventTimer = 8;
                ThreadPool.QueueUserWorkItem(o => runLighting());

            }
        }

        static void initLighting()
        {
            if (!RGBControl.IsConnected()) return;

            var count = RGBControl.GetDeviceCount();
            uwuDeviceInfo = new RGBDeviceInfo();

            for (byte i = 0; i < count; i++)
            {
                RGBControl.SetControlDevice(i);
                var device = RGBControl.GetDeviceInfo();
                //Console.WriteLine($"Found device: Connected: [{device.Connected}], Model: [{device.Model}], Type: [{device.DeviceType}], Max Rows: {device.MaxRows}, Max Cols: {device.MaxColumns}, Max Keycode: {device.KeycodeLimit}");
                if (device.Model.Equals("Wooting UwU RGB"))
                {
                    enableLighting = true;
                    UwU = i;
                    uwuDeviceInfo = device;
                    i = count;
                }
            }

            if (enableLighting)
            {
                eventTimer = 8;
                ThreadPool.QueueUserWorkItem(o => runLighting());
                
            }

        }

        static void runLighting() {
            while (enableLighting && eventTimer > 0)
            {

                RGBControl.SetControlDevice(UwU);
                KeyColour[,] keys = new KeyColour[RGBControl.MaxRGBRows, RGBControl.MaxRGBCols];
                float scaledValue = lastKnownVol * lightProgress.Length;
                int scaledIndex = (int)scaledValue;
                byte roundedFraction = Convert.ToByte((scaledValue - scaledIndex) * 255);
                if (scaledIndex > 0) { 
                    for (int i = 0; i < scaledIndex; i++)
                    {
                        keys[lightProgress[i].y, lightProgress[i].x] = new KeyColour(255, 255, 255);
                    }
                }
                if(roundedFraction > 0)
                {
                    keys[lightProgress[scaledIndex].y, lightProgress[scaledIndex].x] = new KeyColour(roundedFraction, roundedFraction, roundedFraction);
                }

                RGBControl.SetFull(keys); // hmm can we not do set full??
                RGBControl.UpdateKeyboard();

                eventTimer--;
              
                Thread.Sleep(225);
            }
            RGBControl.ResetRGB();
        }


        /// <summary>
        /// Class <c>WootTrayApp</c> is the main tray application
        /// </summary>
        internal class WootTrayApp : ApplicationContext
        {
            private NotifyIcon trayIcon;

            // constructor
            public WootTrayApp()
            {
                // create menu strip with contents
                var strip = new ContextMenuStrip()
                {
                    Items = { new ToolStripMenuItem("Stop UwUVolume", null, new EventHandler(Exit), "EXIT") }
                };
                strip.BackColor = Color.FromArgb(255, 20, 21, 24); ;
                strip.ForeColor = Color.White;
                strip.RenderMode = ToolStripRenderMode.System;

                // create tray application with strip and icon
                trayIcon = new NotifyIcon()
                {
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                    ContextMenuStrip = strip,
                    Visible = true
                };
            }

            // exit button function
            void Exit(object? sender, EventArgs e)
            {
                // Hide tray icon, otherwise it will remain shown until user mouses over it
                trayIcon.Visible = false;
                enableLighting = false;
                RGBControl.SetControlDevice(UwU);
                RGBControl.ResetRGB();
                Application.Exit();
            }
        }
    }
}