using Sealevel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Timers;
using static Sealevel.SeaMAX;
using System.IO;

namespace InputToTxt
{
    public partial class Form1 : Form
    {
        private Sealevel.SeaMAX sm;
        private bool isRunning = false;
        private bool startLogging = false;
        private bool writeHeaders = true;
        private bool runRecordLogOnce = true;
        private bool runDisplayRefreshOnce = true;


        public Form1()
        {
            InitializeComponent();


            this.Text = "NODEDA";
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;

            // Set a fixed size and disable resizing
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Size = new Size(1291, 812);

            // Ensure the form's WindowState is set to Normal
            this.WindowState = FormWindowState.Normal;

            // Set the form's AutoScaleMode property to None
            this.AutoScaleMode = AutoScaleMode.None;

            // Explicitly set the StartPosition to Manual
            this.StartPosition = FormStartPosition.Manual;

            // Explicitly set the location to (0,0)
            this.Location = new Point(0, 0);


            //make info inputs inactive
            textBox2.Enabled = false;
            textBox11.Enabled = false;
            textBox3.Enabled = false;
            textBox5.Enabled = false;
            textBox4.Enabled = false;
            textBox7.Enabled = false;
            textBox12.Enabled = false;
            textBox6.Enabled = false;
            textBox9.Enabled = false;
            textBox13.Enabled = false;
            textBox8.Enabled = false;
            textBox16.Enabled = false;
            textBox14.Enabled = false;
            textBox15.Enabled = false;
            textBox1.Enabled = false;
            button4.Enabled = false;
            button2.Enabled = false;
            button1.Enabled = false;

        }

        private void button1_Click(object sender, EventArgs e) //start logging
        {

            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("You must select a value in MANIFOLD LOCATION.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isRunning == false)
            {
                MessageBox.Show("Program is not running", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (startLogging == false)
                {
                    button1.Enabled = false;
                    button4.Enabled = true;
                    startLogging = true;
                }

                else
                {
                    MessageBox.Show("Program is already logging", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void button4_Click(object sender, EventArgs e) //stop logging
        {
            if (isRunning == false)
            {
                MessageBox.Show("Program is not running", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            {
                if (startLogging == true)
                {
                    button4.Enabled = false;
                    button1.Enabled = true;
                    startLogging = false;
                    runRecordLogOnce = true;
                    writeHeaders = true; //reset writeHeaders to true so that headers will be written to the next log when start logging is pressed again
                }

                else
                {
                    MessageBox.Show("Program is not logging anything", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e) //STOP button
        {

            //make inputs inactive
            button3.Enabled = true;
            comboBox1.Enabled = true;
            comboBox2.Enabled = true;
            comboBox3.Enabled = true;
            comboBox4.Enabled = true;

            button1.Enabled = false;
            button4.Enabled = false;
            button2.Enabled = false;

            if (isRunning)
            {
                startLogging = false;
                isRunning = false;
                runDisplayRefreshOnce = true; //restore display refresh bool, so that if start is pressed again the channels can refresh
                writeHeaders = true;
            }
            else
            {
                MessageBox.Show("Program is not running", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e) //START button
        {
            Sealevel.SeaMAX sm = new Sealevel.SeaMAX();
            isRunning = true;

            try
            {
                int returnValue = sm.SM_Open("");

                if (comboBox4.Text != "")
                {
                    returnValue = sm.SM_Open("COM" + comboBox4.Text);

                    if (returnValue < 0)
                    {
                        // Error opening entered COM
                        MessageBox.Show("Error opening COM" + comboBox4.Text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                else
                {
                    MessageBox.Show("Please include COM. ex: (COM6)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                System.Diagnostics.Debug.WriteLine("SM_Open return value: " + returnValue);  // Print to debug output

                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show("You must select a value in MANIFOLD LOCATION.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (sm.IsSeaMAXOpen)
                {
                    //make inputs inactive
                    button3.Enabled = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    //comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    //comboBox3.Enabled = false;
                    //comboBox4.Enabled = false;


                    System.Diagnostics.Debug.WriteLine("Reading File From Device");  // Print to debug output

                    DateTime currentDateTime = DateTime.Now;
                    DateTime currentDate = currentDateTime.Date;
                    TimeSpan currentTime = currentDateTime.TimeOfDay;

                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    long prevRecordLogTimeMills = now.ToUnixTimeMilliseconds();
                    long prevDisplayRefreshTimeMills = now.ToUnixTimeMilliseconds();

                    string fileName = "Error_LOG_something_is_wrong.txt";
                    StreamWriter writer = new StreamWriter(fileName, true);

                    int displayRefresh = int.Parse(comboBox3.Text);
                    while (isRunning)
                    {
                        textBox1.Text = currentDateTime.ToString();

                        double[] data = ReadDataFromDevice(sm, 5); // 5 channels to read

                        String channel1mAmp = (data[0] / 0.249).ToString("0.000");
                        String channel3mAmp = (data[2] / 0.249).ToString("0.000");
                        String channel4mAmp = (data[3] / 0.249).ToString("0.000");
                        String channel5mAmp = (data[4] / 0.249).ToString("0.000");

                        double channel1WaterColumn = (data[0] - 12) * 0.125;
                        double channel2_ppmCO2 = data[1] * 2000;
                        double channel3_CO2 = data[2] * 5;
                        double channel4_O2 = ((data[3] / 249) * 1000 - 4) * 1.5625;
                        double channel5_O2 = ((data[4] / 249) * 1000 - 4) * 1.5625;

                        string strChannel1WaterColumn = channel1WaterColumn.ToString("0.000");
                        string strChannel2_ppmCO2 = channel1WaterColumn.ToString("0.000");
                        string strChannel3_CO2 = channel3_CO2.ToString("0.000");
                        string strChannel4_O2 = channel4_O2.ToString("0.000");
                        string strChannel5_O2 = channel5_O2.ToString("0.000");

                        long newTimeMills = now.ToUnixTimeMilliseconds();
                        if (runDisplayRefreshOnce || (newTimeMills - prevDisplayRefreshTimeMills) >= displayRefresh * 1000)
                        {
                            //First row in UI (5 channels that have Volt values)
                            textBox2.Text = data[0].ToString("0.000"); //channel # 1 
                            textBox5.Text = data[1].ToString("0.000"); //channel # 2
                            textBox7.Text = data[2].ToString("0.000"); //channel # 3
                            textBox9.Text = data[3].ToString("0.000"); //channel # 4
                            textBox16.Text = data[4].ToString("0.000"); // channel # 5

                            //Second row of values (Channles 1,3,4,5 with mA values)
                            textBox11.Text = channel1mAmp;
                            textBox12.Text = channel3mAmp;
                            textBox13.Text = channel4mAmp;
                            textBox14.Text = channel5mAmp;

                            //Third row of values ("WC, pppmCO2, %CO2, %O2, %O2)
                            textBox3.Text = strChannel1WaterColumn;
                            textBox4.Text = strChannel2_ppmCO2;
                            textBox6.Text = strChannel3_CO2;
                            textBox8.Text = strChannel4_O2;
                            textBox15.Text = strChannel5_O2;

                            // Allow the UI to refresh
                            Application.DoEvents();

                            runDisplayRefreshOnce = false;
                            prevDisplayRefreshTimeMills = newTimeMills;
                        }

                        now = DateTimeOffset.UtcNow;
                        if (startLogging == true && (runRecordLogOnce || (newTimeMills - prevRecordLogTimeMills) >= int.Parse(comboBox2.Text) * 1000))
                        {
                            if (writeHeaders == true)
                            {
                                writer.Close(); //close the previous writer

                                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NODEDA_LOGS");
                                if (!Directory.Exists(folderPath))
                                {
                                    Directory.CreateDirectory(folderPath);

                                }

                                fileName = "LOG_" + currentDate.ToString("yyyy-MM-dd") + "_" + currentTime.ToString(@"hh\_mm\_ss") + ".csv";
                                string fullFilePath = Path.Combine(folderPath, fileName);
                                writer = new StreamWriter(fullFilePath, true);
                                writer.WriteLine("DATE, TIME, channel 1 (\"WC), Manifold Location, channel 2 (CO2 ppm), channel 3 (%CO2), channel 4 (%O2), interval");

                                writeHeaders = false;
                            }

                            runRecordLogOnce = false;
                            prevRecordLogTimeMills = newTimeMills;
                            string manifoldLocation = comboBox1.Text;
                            string interval = comboBox2.Text;
                            string[] dataToWrite = { strChannel1WaterColumn, manifoldLocation, strChannel2_ppmCO2, strChannel3_CO2, strChannel4_O2, strChannel5_O2, interval }; //data and time will be prepended so just put the values that go after date and time here
                            WriteDataToFile(writer, currentDateTime, dataToWrite);
                        }

                        currentDateTime = DateTime.Now;
                        currentTime = currentDateTime.TimeOfDay;
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

        // You may want to close the connection when the form is closed
        protected override void OnFormClosing(FormClosingEventArgs e)


        {
            base.OnFormClosing(e);

            // Update your variables here
            startLogging = false;
            isRunning = false;
            runDisplayRefreshOnce = true;
            writeHeaders = true;

            if (sm != null && sm.IsSeaMAXOpen)
            {
                sm.SM_Close();
            }
        }


        private double[] ReadDataFromDevice(Sealevel.SeaMAX sm, int numInputs)
        {
            double[] analogValues = new double[numInputs];
            SeaMAX.ChannelRange[] ranges = new SeaMAX.ChannelRange[numInputs];
            byte[] byteValues = new byte[2 * numInputs];

            //TODO Hopefully someone would be able to figure this out
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



        private void WriteDataToFile(StreamWriter writer, DateTime dataTime, string[] data)
        {
            // Get the date and time components
            DateTime currentDate = dataTime.Date;
            TimeSpan currentTime = dataTime.TimeOfDay;


            string joinedValues = string.Join(",", data);
            string dataToWrite = string.Join(",", new string[] { currentDate.ToString("yyyy-MM-dd"), currentTime.ToString(@"hh\:mm\:ss\.ff"), joinedValues });

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

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {
        }

        private void label7_Click(object sender, EventArgs e)
        {
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
        }

        private void label9_Click(object sender, EventArgs e)
        {
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
        }

        private void label8_Click(object sender, EventArgs e)
        {
        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label22_Click(object sender, EventArgs e)
        {

        }

        private void label36_Click(object sender, EventArgs e)
        {

        }
    }
}