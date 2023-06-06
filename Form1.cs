using Sealevel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Timers;
using static Sealevel.SeaMAX;

namespace InputToTxt
{
    public partial class Form1 : Form
    {
        private static System.Timers.Timer timer;
        private Sealevel.SeaMAX sm;
        private bool isRunning = false;
        

        public Form1()
        {
            InitializeComponent();

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 1;

        }

        private void button1_Click(object sender, EventArgs e)
        {

            Sealevel.SeaMAX sm = new Sealevel.SeaMAX();
            isRunning = true;

            try
            {
                int returnValue = sm.SM_Open("");

                if (textBox10.Text != "")
                {
                    returnValue = sm.SM_Open(textBox10.Text);
                }
                else
                {
                    MessageBox.Show("Please include COM. ex: (COM6)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                System.Diagnostics.Debug.WriteLine("SM_Open return value: " + returnValue);  // Print to debug output

                if (returnValue < 0)
                {
                    // Error opening COM6
                    MessageBox.Show("Error opening COM6", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (sm.IsSeaMAXOpen)
                {
                        
                    System.Diagnostics.Debug.WriteLine("Reading File From Device");  // Print to debug output

                    DateTime currentDateTime = DateTime.Now;
                    DateTime currentDate = currentDateTime.Date;
                    TimeSpan currentTime = currentDateTime.TimeOfDay;

                    string fileName = "LOG_" + currentDate.ToString("yyyy-MM-dd") + "_" + currentTime.ToString(@"hh\_mm\_ss") + ".txt";
                    StreamWriter writer = new StreamWriter(fileName, true);
                    writer.WriteLine("DATE, TIME, channel 1 (mBar), channel 2 (%O2), channel 3(%CO2), channel 4 (%O2), location, interval");

                    // Create a timer with a 5-second interval
                    //timer = new System.Timers.Timer(int.Parse(comboBox1.Text) * 1000);
                    //timer.Elapsed += TimerElapsed;
                    //timer.AutoReset = true; // Set to true to run repeatedly

                    // Start the timer
                    //timer.Start();
                    bool runOnce = true;
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    long prevTimeMills = now.ToUnixTimeMilliseconds();

                    while (isRunning)
                    {
                        textBox1.Text = currentDateTime.ToString();

                        double[] data = ReadDataFromDevice(sm, 4);
                        textBox2.Text = data[0].ToString("0.0");
                        textBox5.Text = data[1].ToString("0.00");
                        textBox7.Text = data[2].ToString("0.00");
                        textBox9.Text = data[3].ToString("0.00");

                        double channel1MBar = ((data[0] / 249) * 1000 - 4) * 125;
                        double channel_O2 = ((data[1]/249)*1000 - 4) * 1.875;
                        double channel3_CO2 = ((data[2] / 249) * 1000 - 4) * 1.875;
                        double channel4_O2 = data[3] * 5;

                        textBox3.Text = channel1MBar.ToString("0.0");
                        textBox4.Text = channel_O2.ToString("0.00");
                        textBox6.Text = channel3_CO2.ToString("0.00");
                        textBox8.Text = channel4_O2.ToString("0.00");

                        // Allow the UI to refresh
                        Application.DoEvents();

                        now = DateTimeOffset.UtcNow;
                        long newTimeMills = now.ToUnixTimeMilliseconds();
                        if (runOnce || (newTimeMills - prevTimeMills) >= int.Parse(comboBox2.Text)*1000)
                        {
                            runOnce = false;
                            prevTimeMills = newTimeMills;
                            WriteDataToFile(writer, fileName, currentDateTime, data, comboBox1.Text, comboBox2.Text);
                        }
                        currentDateTime = DateTime.Now;



                    }

                    writer.Close();
                    sm.SM_Close();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {

            if (isRunning)
            {
                isRunning = false;
            }
            else
            {
                MessageBox.Show("Program is not running", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        // You may want to close the connection when the form is closed
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (sm.IsSeaMAXOpen)
            {
                sm.SM_Close();
            }
        }


        private double[] ReadDataFromDevice(Sealevel.SeaMAX sm, int numInputs)
        {
            double[] analogValues = new double[numInputs];
            SeaMAX.ChannelRange[] ranges = new SeaMAX.ChannelRange[numInputs];
            byte[] byteValues = new byte[2 * numInputs];

            //// Prepare the analog configuration
            //int analogInputNumber = 4; // Replace with the actual number of analog inputs
            //AnalogConfig[] analogConfigs = new AnalogConfig[analogInputNumber];

            //// Configure each analog input
            //for (int i = 0; i < analogInputNumber; i++)
            //{
            //    analogConfigs[i] = new AnalogConfig();
            //    analogConfigs[i].Exponent = 12;
            //    analogConfigs[i].Precision = 0;
            //    analogConfigs[i].MinValue = 0;
            //    if (i == 2)
            //    {
            //        analogConfigs[i].MaxValue = 5;
            //        analogConfigs[i].ChannelMode = (AnalogMode)ChannelMode.DIFFERENTIAL;

            //    }

            //    else
            //    {
            //        analogConfigs[i].MaxValue = 10;
            //        analogConfigs[i].ChannelMode = (AnalogMode)ChannelMode.CURRENT_LOOP;

            //    }
            //    // Set other configuration parameters as needed
            //}

            //// Set the analog configuration
            //int resultAnalogConf = sm.SM_SetAnalogConfig(analogConfigs);
            //if (resultAnalogConf < 0)
            //{
            //    // Error setting analog configuration
            //    MessageBox.Show("Error setting analog configuration" + resultAnalogConf, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}

            
            // Setup the ranges
            int resultRanges = sm.SM_GetAnalogInputRanges(ranges);

            // try seeing ranges //comment this later or remove
            for (int i = 0; i < numInputs; i++)
            {
                System.Diagnostics.Debug.WriteLine(ranges[i]);
            }


            // Read analog inputs starting from the first one (index 0)
            int result = sm.SM_ReadAnalogInputs(0, numInputs, analogValues, ranges, byteValues);
            for (int i = 0; i < numInputs; i++)
            {
                System.Diagnostics.Debug.WriteLine(analogValues[i]);
            }


            if (resultRanges < 0)
            {
                // Handle error (e.g., throw exception, return an error message, etc.)
                System.Diagnostics.Debug.WriteLine("Error reading analog input ranges: " + result);
            }


            // Check for errors
            if (result < 0)
            {
                // Handle error (e.g., throw exception, return an error message, etc.)
                System.Diagnostics.Debug.WriteLine("Error reading analog inputs: " + result);
            }


            return analogValues;
        }



        private void WriteDataToFile(StreamWriter writer, string fileName, DateTime dataTime, double[] data, string location, string interval)
        {
            // Get the date and time components
            DateTime currentDate = dataTime.Date;
            TimeSpan currentTime = dataTime.TimeOfDay;

            string[] formattedData = Array.ConvertAll(data, d => d.ToString("0.00"));

            string joinedValues = string.Join(",", formattedData);
            string dataToWrite = string.Join(",", new string[] { currentDate.ToString("yyyy-MM-dd"), currentTime.ToString(@"hh\:mm\:ss\.ff"), joinedValues, location, interval });

            writer.WriteLine(dataToWrite);

        }


        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void label16_Click(object sender, EventArgs e)
        {

        }
    }
}