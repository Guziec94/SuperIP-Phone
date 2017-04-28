﻿using Ozeki.Media;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for Ustawienia.xaml
    /// </summary>
    public partial class Ustawienia : Window
    {
        MP3StreamPlayback mp3Player;
        bool CzyUruchomionyTest;
        Thread WatekDoTestow;
        Microphone microphone;
        Speaker speaker;

        public Ustawienia()
        {
            InitializeComponent();
            mp3Player = new MP3StreamPlayback("../../Resources/ding.mp3");
            CzyUruchomionyTest = false;
            WyborMikrofonu();
            WyborGlosnikow();
        }

        private void WyborGlosnikow()
        {
            int i = 0;
            speaker = (Speaker)Application.Current.Properties["WyjscieAudio"];
            foreach (var device in Speaker.GetDevices())
            {
                AudioOUTcomboBox.Items.Add(device);
                if (speaker.DeviceInfo.ToString() == device.ProductName)
                {
                    AudioOUTcomboBox.SelectedIndex = i;
                }
                i++;
            }
        }

        private void WyborMikrofonu()
        {
            int i = 0;
            microphone = (Microphone)Application.Current.Properties["WejscieAudio"];
            foreach (var device in Microphone.GetDevices())
            {
                AudioINcomboBox.Items.Add(device);
                if (microphone.DeviceInfo.ToString() == device.ProductName)
                {
                    AudioINcomboBox.SelectedIndex = i;
                }
                i++;
            }
        }

        private void Glosnikslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            speaker.Volume = (float)Glosnikslider.Value / 100;
        }

        private void Mikrofonslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            microphone.Volume = (float)Mikrofonslider.Value / 100;
        }

        private void TestGlosnikabutton_Click(object sender, RoutedEventArgs e)
        {
            if (mp3Player.IsStreaming != true && speaker != null)
            {
                try
                {
                    MediaConnector mediaConnector = new MediaConnector();

                    mediaConnector.Connect(mp3Player, speaker);
                    mp3Player.Start();
                    speaker.Start();

                    while (mp3Player.IsStreaming) ;

                    mediaConnector.Disconnect(mp3Player, speaker);
                    mp3Player.Stop();
                    speaker.Stop();

                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        private void TestMikrofonubutton_Click(object sender, RoutedEventArgs e)
        {
            if (WatekDoTestow == null || (WatekDoTestow.ThreadState == ThreadState.Aborted && WatekDoTestow.ThreadState != ThreadState.AbortRequested))
            {
                WatekDoTestow = new Thread(delegate ()
                {
                    MediaConnector mediaConnector = new MediaConnector();
                    mediaConnector.Connect(microphone, speaker);
                    microphone.Start();
                    speaker.Start();
                    while (CzyUruchomionyTest) ;
                    mediaConnector.Disconnect(microphone, speaker);
                    microphone.Stop();
                    speaker.Stop();
                });
            }
            if (WatekDoTestow.IsAlive == false && (microphone != null && speaker != null))//if (CzyUruchomionyTestMikrofonu == false)
            {
                WatekDoTestow.Start();
                CzyUruchomionyTest = true;
                TestMikrofonubutton.Content = "Zatrzymaj test";
            }
            else
            {
                CzyUruchomionyTest = false;
                TestMikrofonubutton.Content = "Testuj wejście audio";
                WatekDoTestow.Abort();
            }
        }

        private void AudioINcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDeviceInfo wybrane = AudioINcomboBox.SelectedItem as AudioDeviceInfo;
            microphone = Microphone.GetDevice(wybrane);
            Mikrofonslider.Value = microphone.Volume * 100;
        }

        private void AudioOUTcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDeviceInfo wybrane = AudioOUTcomboBox.SelectedItem as AudioDeviceInfo;
            speaker = Speaker.GetDevice(wybrane);
            Glosnikslider.Value = speaker.Volume * 100;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Properties["WejscieAudio"] = microphone;
            Application.Current.Properties["WyjscieAudio"] = speaker;
        }
    }
}
