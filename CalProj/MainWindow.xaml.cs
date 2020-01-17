using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using FTD2XX_NET;

namespace CalProj
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FTDI.FT_DEVICE_INFO_NODE[] deviceList;
        private PressureSensor ps;
        private TemperatureSensor ts;
        private ModbusMaster modbus;
        private FlowMeter fm;
        private BackgroundWorker pressureWorker; // read from the pressure sensor ps
        private BackgroundWorker tempWorker; // reads from the temperature sensor ts
        private BackgroundWorker calibrationWorker; // runs the calibration process
        private BackgroundWorker modbusWorker; // controls MFC and valves
        private BackgroundWorker flowmeterWorker; // controls MFC and valves
        private ManualResetEvent flowmeterWorkerResetEvent; //

        private DataCollector proFlowData;
        private DataCollector mfcData;

        private int delayMsec = Constants.flowmeter_delay;
        private int readingsPerSec = 2;

        private List<CheckBox> valveList;
        private bool measurementRunning;
        private string calibrationFileName;
        private string logFileName;
        private bool createCalFile;
        private bool createLogFile;
        private byte currentSlaveId;
        private bool pressureCorrection;
        private bool temperatureCorrection;


        public MainWindow()
        {
            ps = new PressureSensor();
            ts = new TemperatureSensor();
            fm = new FlowMeter();
            modbus = new ModbusMaster();
            proFlowData = new DataCollector(Constants.rollingMeanRange);
            mfcData = new DataCollector(Constants.rollingMeanRange);
            valveList = new List<CheckBox>();
            

            InitializeComponent();
            initializeFields();

            pressureWorker = new BackgroundWorker();
            pressureWorker.DoWork += new DoWorkEventHandler(readWorker_DoWork);
            pressureWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(readWorker_Completed);
            pressureWorker.ProgressChanged += new ProgressChangedEventHandler(readWorker_ProgressChanged);
            pressureWorker.WorkerSupportsCancellation = true;
            pressureWorker.WorkerReportsProgress = true;

            tempWorker = new BackgroundWorker();
            tempWorker.DoWork += new DoWorkEventHandler(tempWorker_DoWork);
            tempWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(tempWorker_Completed);
            tempWorker.ProgressChanged += new ProgressChangedEventHandler(tempWorker_ProgressChanged);
            tempWorker.WorkerSupportsCancellation = true;
            tempWorker.WorkerReportsProgress = true;

            calibrationWorker = new BackgroundWorker();
            calibrationWorker.DoWork += new DoWorkEventHandler(calibrationWorker_DoWork);
            calibrationWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(calibrationWorker_Completed);
            calibrationWorker.ProgressChanged += new ProgressChangedEventHandler(calibrationWorker_ProgressChanged);
            calibrationWorker.WorkerSupportsCancellation = true;
            calibrationWorker.WorkerReportsProgress = true;

            modbusWorker = new BackgroundWorker();
            modbusWorker.DoWork += new DoWorkEventHandler(modbusWorker_DoWork);
            modbusWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(modbusWorker_Completed);
            modbusWorker.ProgressChanged += new ProgressChangedEventHandler(modbusWorker_ProgressChanged);
            modbusWorker.WorkerSupportsCancellation = true;
            modbusWorker.WorkerReportsProgress = true;

            flowmeterWorker = new BackgroundWorker();
            flowmeterWorker.DoWork += new DoWorkEventHandler(flowmeterWorker_DoWork);
            flowmeterWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(flowmeterWorker_Completed);
            flowmeterWorker.ProgressChanged += new ProgressChangedEventHandler(flowmeterWorker_ProgressChanged);
            flowmeterWorker.WorkerSupportsCancellation = true;
            flowmeterWorker.WorkerReportsProgress = true;
            flowmeterWorkerResetEvent = new ManualResetEvent(false);

        }


        ~ MainWindow()
        {
            modbus.closeAllValves();
            modbus.disconnect();
            ps.closeDevice();
            ts.closeDevice();
            fm.close();
        }

        private void initializeFields()
        {
            // set the default values for all entry fields in the main window.
            // the values are read from static variables in the Constants class, so far
            // todo: 
            // generate a config file with the values from the Constants class that can be edited by windows users 
            // and read the default values from there.
            tbxTempCOMPort.Text = Constants.temp_port.ToString();
            tbxTempChannel.Text = Constants.temp_channel.ToString();

            getDeviceList();
            updateDeviceListCombos();
            //cb_Valve1.IsChecked = modbus.isValveOpen(1);
            //cb_Valve2.IsChecked = modbus.isValveOpen(2);
            //cb_Valve3.IsChecked = modbus.isValveOpen(3);
            //cb_Valve4.IsChecked = modbus.isValveOpen(4);
            //cb_Valve5.IsChecked = modbus.isValveOpen(5);
            //cb_Valve6.IsChecked = modbus.isValveOpen(6);
            valveList.Add(cb_Valve1);
            valveList.Add(cb_Valve2);
            valveList.Add(cb_Valve3);
            valveList.Add(cb_Valve4);
            valveList.Add(cb_Valve5);
            valveList.Add(cb_Valve6);
            measurementRunning = false;
            txtResult.Text = "Connect pressure sensor, temperature sensor and MFCs first\nThe Start Calibration button will be enabled when these devices are running\n";
            currentSlaveId = 0;

            pressureCorrection = (cb_pressureCorrection.IsChecked==true);
            temperatureCorrection = (cb_tempCorrection.IsChecked==true);

            DateTime date = DateTime.Now;
            string dateString = date.Year.ToString("0000") + date.Month.ToString("00") + date.Day.ToString("00") + date.Hour.ToString("00") + date.Minute.ToString("00") + date.Second.ToString("00");
            calibrationFileName = Constants.calibrationFilePath + Constants.calibrationFileName + "_" + dateString + Constants.calibrationFileExtension;
            tbCalFile.Text = calibrationFileName;
            tbCalFile.ToolTip = calibrationFileName;

            logFileName = Constants.logFilePath + Constants.logFileName + "_" + dateString + Constants.logFileExtension;
            tbxLogFile.Text = logFileName;
            tbxLogFile.ToolTip = logFileName;
            createCalFile = chbCalFile.IsEnabled;
            createLogFile = chbLogFile.IsEnabled;
        }

        private void btnLoadDeviceList_Click(object sender, RoutedEventArgs e)
        {
            getDeviceList();
            updateDeviceListCombos();
        }

        private void getDeviceList()
        {
            FTDI myFtdiDevice = new FTDI();
            UInt32 ftdiDeviceCount = 0;
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            deviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];
            if (ftdiDeviceCount > 0)
            {
                ftStatus = myFtdiDevice.GetDeviceList(deviceList);
            }
        }

        private void enableSettingControls(bool enable)
        {
            btnConnectPressure.IsEnabled = enable;
            btnDisconnectPressure.IsEnabled = enable;
            cb_pressureDevice.IsEnabled = enable;
            btnConnectTemp.IsEnabled = enable;
            btnDisconnectTemp.IsEnabled = enable;
            tbxTempCOMPort.IsEnabled = enable;
            tbxTempChannel.IsEnabled = enable;
            btnModbusConnect.IsEnabled = enable;
            btnModbusDisconnect.IsEnabled = enable;
            cb_modbusDevice.IsEnabled = enable;
            btnFlowmeterConnect.IsEnabled = enable;
            btnFlowmeterDisconnect.IsEnabled = enable;
            cb_flowmeterDevice.IsEnabled = enable;
            chbCalibrationMode.IsEnabled = enable;
            cbReadings.IsEnabled = enable;
            chbCalFile.IsEnabled = enable;
            btnLoad.IsEnabled = enable;
            btnSelectCalFile.IsEnabled = enable;
            btnSelectLogFile.IsEnabled = enable;
            chbLogFile.IsEnabled = enable;
            tbCalFile.IsEnabled = enable;
            tbxLogFile.IsEnabled = enable;
            cb_calibrationPts.IsEnabled = enable;
            btnLoad.IsEnabled = enable;
            btn_valveControl.IsEnabled = enable;
            btnFlow.IsEnabled = enable;
            txbFlow.IsEnabled = enable;
            cb_CurrentConditions.IsEnabled = enable;
            lblFlow.IsEnabled = enable;
            cb_tempCorrection.IsEnabled = enable;
            cb_pressureCorrection.IsEnabled = enable;
        }

        private bool allDevicesReady()
        {
            bool ready = false;
            if (ps.isOpen() & ts.isOpen() & modbus.isOpen())
            {
                ready = true;
            }
            return ready;
        }

        public bool testSystemPressure()
        {
            // to do: 
            // read from the pressure meters DS1 and DS2 
            // wait for 3 min then read again and compare the values to the preaviously read ones.
            // return true if both reading meet the ok creteria
            // return false if the deviation is too big.

            // for now the function return true without testing
            return true;
        }

        public void Resume(ManualResetEvent mre)
        {
            mre.Set();
        }

        public void Pause(ManualResetEvent mre)
        {
            mre.Reset();
        }


        //
        // Manually control valves
        //
        private void cb_Valve_Click(object sender, RoutedEventArgs e)
        {
            if (modbus.isOpen())
            {
                try
                {
                    CheckBox source = (CheckBox)e.Source;
                    ushort valveNo = UInt16.Parse(source.Content.ToString());
                    if (source.IsChecked == true)
                    {
                        modbus.openValve(valveNo);
                    }
                    else
                    {
                        modbus.closeValve(valveNo);
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        private void btnManualMode_Click(object sender, RoutedEventArgs e)
        {
            if (modbus.isOpen()) {
                foreach (CheckBox valve in valveList) {
                    valve.IsEnabled = true;
                }
                lblFlow.IsEnabled = true;
                txbFlow.IsEnabled = true;
                btnFlow.IsEnabled = true;
                btnGetFlow.IsEnabled = true;
            }
        }

        private void btnGetFlow_Click(object sender, RoutedEventArgs e)
        {
            String currentFlow = "";

            if (modbus.isOpen())
            {
                currentFlow=modbus.getCurrentFlow(modbus.currentSlave).ToString();
                txtResult.Text = "Current Flow: "+currentFlow +" mL/min\r" + txtResult.Text;

            }
        }


        private void btnFlow_Click(object sender, RoutedEventArgs e)
        {
            float flow = 0f;
            float pressure = 0f;
            float temp = 0f;
            byte currentSlaveId;

            try
            {
                flow = Convert.ToSingle(txbFlow.Text);
                if (cb_CurrentConditions.IsChecked == true)
                {
                    if (ps.isOpen() & ts.isOpen())
                    {
                        pressure = ps.currentPressure_mbar;
                        temp = ts.currentTemp_kelvin;
                        if (pressure > 0f & temp > 0f)
                        {
                            flow = Convert.ToSingle(getActualFlow(flow, pressure, temp, true, true));
                            //flow = flow / temp / 1013f * pressure * 273.15f;
                            txtResult.Text = "Flow at current conditions: " + flow.ToString("0.000") + "mL/min\r" + txtResult.Text;
                        }
                    }
                    else
                    {
                        MessageBox.Show(this, "Pressure or temperature sensor not connected.\r No correction possible.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                if (modbus.isOpen())
                {
                    if (modbus.isValveOpen(4) | modbus.isValveOpen(5))
                    {
                        if (modbus.isValveOpen(1))
                        {
                            // MFC500
                            if (flow <= 500)
                            {
                                txtResult.Text = "Using MFC500\r" + txtResult.Text;
                                modbus.setFlow(Constants.mfc500_slaveId, flow);
                            }else
                            {
                                MessageBox.Show(this, "Max flow for MFC500 is 500 mL/min. Could not set desired flow.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else if (modbus.isValveOpen(2))
                        {
                            // MFC50
                            if (flow <= 50)
                            {
                                txtResult.Text = "Using MFC50\r" + txtResult.Text;
                                modbus.setFlow(Constants.mfc50_slaveId, flow);
                            }
                            else
                            {
                                MessageBox.Show(this, "Max flow for MFC50 is 50 mL/min. Could not set desired flow.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                        }
                        else if (modbus.isValveOpen(3))
                        {
                            // MFC10
                            if (flow <= 10)
                            {
                                txtResult.Text = "Using MFC10\r" + txtResult.Text;
                                modbus.setFlow(Constants.mfc10_slaveId, flow);
                            }
                            else
                            {
                                MessageBox.Show(this, "Max flow for MFC10 is 10 mL/min. Could not set desired flow.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                        }
                        else
                        {
                            MessageBox.Show(this, "Valves 4 or 5 and 1, 2 or 3 must be open", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }else
                    {
                        MessageBox.Show(this, "Valves 4 or 5 and 1, 2 or 3 must be open", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void updateDeviceListCombos()
        {
            cb_flowmeterDevice.Items.Clear();
            cb_modbusDevice.Items.Clear();
            cb_pressureDevice.Items.Clear();
            for (int i = 0; i < deviceList.Length; i++)
            {
                string description = deviceList[i].Description + " (" + deviceList[i].SerialNumber + ")";

                cb_pressureDevice.Items.Add(description);
                cb_modbusDevice.Items.Add(description);
                cb_flowmeterDevice.Items.Add(description);
                if (deviceList[i].SerialNumber.Equals(Constants.pressure_serial))
                {
                    cb_pressureDevice.SelectedIndex = i;
                }
                if (deviceList[i].SerialNumber.Equals(Constants.modbus_serial))
                {
                    cb_modbusDevice.SelectedIndex = i;
                }
                if (deviceList[i].Description.Equals(Constants.flowmeter_description))
                {
                    cb_flowmeterDevice.SelectedIndex = i;
                }
            }

        }

        // functions for the pressure sensor

        private void btnConnectPressure_Click(object sender, RoutedEventArgs e)
        {
            //connect pressure sensor using serial number
            if (!pressureWorker.IsBusy)
            {
                if (cb_pressureDevice.SelectedIndex > -1)
                {
                    pressureWorker.RunWorkerAsync(deviceList[cb_pressureDevice.SelectedIndex].SerialNumber);
                    btnConnectPressure.Visibility = Visibility.Hidden;
                    btnDisconnectPressure.Visibility = Visibility.Visible;
                    lblCurPressure.Content = "Connecting...";
                }
            }
        }

        private void btnDisconnectPressure_Click(object sender, RoutedEventArgs e)
        {
            pressureWorker.CancelAsync();
        }

        protected void readWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker sendingWorker = (BackgroundWorker)sender;
            ps.loadDevice(e.Argument.ToString());
            if (ps.isOpen())
            {
                while (true)
                {
                    if (ps.read())
                    {
                        try
                        {
                            sendingWorker.ReportProgress(1, ps.currentPressure_mbar.ToString("0.0000") +" mbar");
                        }
                        catch 
                        {
                            sendingWorker.ReportProgress(1, "");
                        }
                    }
                    if (sendingWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
            }
        }

        protected void readWorker_ProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            btnStartCalibration.IsEnabled = (allDevicesReady() & !measurementRunning);
            string value = args.UserState.ToString();
            lblCurPressure.Content = value;
        }

        protected void readWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            ps.closeDevice();
            lblCurPressure.Content = " ";
            btnConnectPressure.Visibility = Visibility.Visible;
            btnDisconnectPressure.Visibility = Visibility.Hidden;
        }


        //
        // functions for the temperature sensor
        //

        private void btnConnectTemp_Click(object sender, RoutedEventArgs e)
        {
            //connect temperature sensor using Com Port
            
            ushort portNo = ushort.Parse(tbxTempCOMPort.Text);
            ushort channelNo = ushort.Parse(tbxTempChannel.Text);
            List<ushort> tempParams = new List<ushort>();
            tempParams.Add(portNo);
            tempParams.Add(channelNo);
            btnConnectTemp.Visibility = Visibility.Hidden;
            btnDisconnectTemp.Visibility = Visibility.Visible;
            lblCurTemp.Content = "Connecting...";
            if (!tempWorker.IsBusy)
            {
                tempWorker.RunWorkerAsync(tempParams);
            }
        }


        private void btnDisconnectTemp_Click(object sender, RoutedEventArgs e)
        {
            tempWorker.CancelAsync();

        }


        protected void tempWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker sendingWorker = (BackgroundWorker)sender;
            List<ushort> arguments = (List<ushort>)e.Argument;
            ts.loadDevice(arguments[0],arguments[1]);
            
            if (ts.isOpen())
            {
                while (true)
                {
                    ts.read();
                    //                e.Result = ts.currentTemp_celsius;
                    sendingWorker.ReportProgress(1, ts.currentTemp_celsius.ToString("0.0000") + " °C");
                    if (sendingWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    Thread.Sleep(Constants.temp_delay);
                }
            }else
            {
                sendingWorker.CancelAsync();
                e.Cancel = true;
            }
        }


        protected void tempWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            btnStartCalibration.IsEnabled = (allDevicesReady() & !measurementRunning);
            string s = e.UserState.ToString();
            lblCurTemp.Content = s ;                        // displays temperature 
            
        }


        protected void tempWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            ts.closeDevice();
            lblCurTemp.Content = " ";
            btnConnectTemp.Visibility = Visibility.Visible;
            btnDisconnectTemp.Visibility = Visibility.Hidden;
        }

        //
        // Calibration process
        //
        public void btnStartCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (!fm.isOpen())
            {
                MessageBoxResult result = MessageBox.Show(this,"Flowmeter not connected. Do you want to start the measurement anyways?", "No flowmeter connected", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.No)
                {
                    txtResult.Text = "Connect flowmeter please.\n" + txtResult.Text;
                    return;  // Measurement canceled

                }
            }

            if (!calibrationWorker.IsBusy)
            {
                createLogFile = (chbLogFile.IsChecked==true);
                createCalFile = (chbCalFile.IsChecked == true);
                calibrationWorker.RunWorkerAsync();
                btnStartCalibration.IsEnabled = false;
                btnStopCalibration.IsEnabled = true;
                measurementRunning = true;
                foreach(CheckBox cbValve in valveList)
                {
                    cbValve.IsEnabled = false;
                }
                btn_valveControl.IsEnabled = false;
                enableSettingControls(false);        // disable all
                txtResult.Text = "";
                ListBoxItem value = (ListBoxItem)cbReadings.SelectedValue;
                try
                {
                    readingsPerSec = Int32.Parse(value.Content.ToString());
                }catch
                {
                    readingsPerSec = Constants.readingsPerSecond;
                }
                pressureCorrection = (cb_pressureCorrection.IsChecked==true);
                temperatureCorrection = (cb_tempCorrection.IsChecked==true);

                delayMsec = 900 / readingsPerSec;
            }

        }

        public void btnStopCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (calibrationWorker.IsBusy)
            {
                calibrationWorker.CancelAsync();
                enableSettingControls(true);
                measurementRunning = false;
            }

        }

        protected void calibrationWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker sendingWorker = (BackgroundWorker)sender;
            float temp = 0f;
            float pressure = 0f;
            DateTime time = DateTime.Now;

            //start pressure test

            //if pressure test ok: start calibration
            // MFC10: 0 - 10 ml, valves 3 and 4 open
            // MFC50: 0 - 50 ml, valves 2 and 4 open
            // MFC500: 0 - 500 ml, valves 1 and 4 open

            // foreach MFC do:
            //   get calibration points
            //   open valves
            //   for each calibration point do:
            //     calculate flow at poi: flow_poi = flow calibration point [mL] / temp [K] / pressure[mbar]
            //     set target flow (flow_poi)
            //     start reading flow from MFC
            //     wait until rolling mean of measured flow is stable 
            //     write data to file: date/time; temp; pressure; calibration point; flow_poi; measured flow
            sendingWorker.ReportProgress(1, "Starting system pressure test. Please wait...\r");
            if (testSystemPressure())
            {
                sendingWorker.ReportProgress(1, "Pressure test successful. Starting measurement.\r");

                // prepare log file
                DateTime date = DateTime.Now;
                string dateString = date.Year.ToString("0000") + date.Month.ToString("00") +date.Day.ToString("00") + date.Hour.ToString("00") + date.Minute.ToString("00") +date.Second.ToString("00");
                //string logFile = Constants.logFilePath + Constants.logFileName +"_"+ dateString + Constants.logFileExtension;

                // todo: 
                // if the file already exists it will be overwritten. add a function to check this and create a new file with a unique name.

                StreamWriter fs = null;
                if (createLogFile == true)
                {
                    fs = new StreamWriter(logFileName);
                    fs.WriteLine(Constants.logFileHeader);
                    fs.Flush();
                }

                StreamWriter calFileStream = null;   // prepare calibration file
                if (createCalFile)
                {
//                    string calFile = Constants.calibrationFilePath + Constants.calibrationFileName + "_"+dateString + Constants.calibrationFileExtension;
                    calFileStream = new StreamWriter(calibrationFileName);
                    for (int i = 0; i < Constants.calFileHeader.Length; i++)
                    {
                        calFileStream.WriteLine(Constants.calFileHeader[i]);
                    }
                    calFileStream.WriteLine(dateString);
                    calFileStream.Flush();
                }


                // start measurement
                for (int i = 0; i < Constants.numberOfMFCsInvolved; i++)
                {
                    modbus.closeAllValves();
                    sendingWorker.ReportProgress(1, "valve");  
                    MFC mfc = modbus.getMFC(i);
                    
                    ushort[] valves = mfc.valvesToOpen;
                    foreach (ushort valve in valves)
                    {
                        try
                        {
                            modbus.openValve(valve);
                            Thread.Sleep(delayMsec);
                            sendingWorker.ReportProgress(valve, "valve");
                        }
                        catch
                        {
                            sendingWorker.CancelAsync();     // could not open valve -> cancelling calibration process
                            e.Cancel = true;
                            modbus.setFlow(mfc.slaveId, 0f);
                            sendingWorker.ReportProgress(1, "Critical error: Unable to open valve " + valve.ToString() + ". Cancelling measurement process.");
                            if (createCalFile) calFileStream.Close();
                            if (createLogFile) fs.Close();
                            return;
                        }
                    }
                    if (fm.isOpen() & (fm.calibrationMode==true))
                    {
                        Pause(flowmeterWorkerResetEvent);
                        if (mfc.flowmeterRange == 0)
                        {
                            fm.setLowRange();
                        }
                        else {
                            fm.setHighRange();
                        }
                        Resume(flowmeterWorkerResetEvent);
                    }

                    for (int j = 0; j < mfc.calibrationPoints.Length; j++)
                    {
                        mfcData.checkStability = true;         // initialize stability check criteria for mfc and proflow
                        mfcData.resetStabilityCheck();
                        proFlowData.checkStability = true;
                        proFlowData.resetStabilityCheck();

                        mfcData.calibrationPoint = mfc.calibrationPoints[j];
                        temp = ts.currentTemp_kelvin;    // get current temperature and pressure
                        pressure = ps.currentPressure_mbar;
                        //mfcData.targetDatapoint = mfc.calibrationPoints[j];
                        mfcData.targetDatapoint = getActualFlow(mfc.calibrationPoints[j],pressure, temp,pressureCorrection, temperatureCorrection);

                        modbus.setFlow(mfc.slaveId, (float)mfcData.targetDatapoint);
                        sendingWorker.ReportProgress(1, "Waiting to reach target flow " + mfcData.targetDatapoint.ToString("0.0000"));
                        Thread.Sleep(mfc.delayBetweenCalibrationPts);   // wait for stabilization (delay is longer for MFC50 and MFC10)
                        int retries = 0;
                        int readings = 0;

                        bool notYetStable = true;
                        time = DateTime.Now;


                        while (notYetStable)
                        {
                            while (DateTime.Now < time)
                            {
                                Thread.Sleep(1); // wait for the delay to elapse
                            }
                            time = DateTime.Now.AddMilliseconds(delayMsec);
                            mfcData.currentDatapoint = modbus.getCurrentFlow(mfc.slaveId);
                            string record = writeRecord(fs);      // creates a record including all relevant readings and writes it to fs if createLogFile is true;
                            sendingWorker.ReportProgress(1, record);

                            readings++;
                            if (readings > Constants.maxRetries)
                            { // flowmeter cannot stabilize
                                                                       
                                modbus.setFlow(mfc.slaveId, 0f);   // retry to set flow and try again to stabilize
                                Thread.Sleep(delayMsec);
                                modbus.setFlow(mfc.slaveId, (float)mfcData.targetDatapoint);     /// retry once
                                Thread.Sleep(delayMsec);
                                mfcData.resetStabilityCheck();
                                proFlowData.resetStabilityCheck();
                                readings = 0;
                                retries++;
                                if (retries > 2)  // if the mfc had to be reset more than twice at the same calibration point, stop the calibration routine
                                {
                                    sendingWorker.ReportProgress(1, "MFC could not stabilize at flow " + mfcData.targetDatapoint);
                                    sendingWorker.CancelAsync();
                                    if (createCalFile) calFileStream.Close();
                                    if (createLogFile) fs.Close();
                                    return;
                                }
                            }   //

                            if (sendingWorker.CancellationPending)
                            {
                                sendingWorker.ReportProgress(1, "Stopped.");
                                e.Cancel = true;
                                if (createCalFile) calFileStream.Close();
                                if (createLogFile) fs.Close();
                                return;
                            }

                            temp = ts.currentTemp_kelvin;    // get current temperature and pressure
                            pressure = ps.currentPressure_mbar;
                            
                            mfcData.targetDatapoint = getActualFlow(mfc.calibrationPoints[j],pressure,temp,pressureCorrection,temperatureCorrection);
                            //mfcData.targetDatapoint = mfc.calibrationPoints[j];

                            modbus.setFlow(mfc.slaveId, (float)mfcData.targetDatapoint);
                            notYetStable = !mfcData.isSystemStable(mfc.maxDeviation,mfc.maxPercentualDeviation);
                            if (fm.isOpen())
                            {
                                double maxDeviation = Constants.proFlow_maxDeviationHigh;
                                double maxPercentDiff = Constants.proFlow_maxDifferenceHigh;
                                if (mfc.flowmeterRange == 0)
                                {
                                    if (mfc.calibrationPoints[j] < 1)
                                    {
                                        maxDeviation = Constants.proFlow_maxDeviationSuperLow;
                                        maxPercentDiff = Constants.proFlow_maxDifferenceSuperLow;
                                    }
                                    else
                                    {
                                        maxDeviation = Constants.proFlow_maxDeviationLow;
                                        maxPercentDiff = Constants.proFlow_maxDifferenceLow;
                                    }
                                }
                                notYetStable |= !proFlowData.isSystemStable(maxDeviation, maxPercentDiff);
                            }
                        }

                        if (createCalFile)
                        {
                            double curAbsoluteFlow = getActualFlow(mfcData.currentDatapoint,pressure, temp, pressureCorrection, temperatureCorrection);
                            //double curAbsoluteFlow = mfcData.currentDatapoint;
                            string proFlowReading = "";                          // format proFlow reading
                            if (proFlowData.currentDatapoint > 100)
                            {
                                proFlowReading = proFlowData.currentDatapoint.ToString("0");
                            }else if (proFlowData.currentDatapoint > 10)
                            {
                                proFlowReading = proFlowData.currentDatapoint.ToString("0.0");

                            }else
                            {
                                proFlowReading = proFlowData.currentDatapoint.ToString("0.00");
                            }
                            try
                            {
                                calFileStream.WriteLine(curAbsoluteFlow.ToString("0.000") + "," + proFlowReading); // add the last reading after stabililzation to the calibration file
                                calFileStream.Flush();
                            }catch
                            {
                                Console.WriteLine("Could not write to calibration File");
                            }
                        }
                    }
                    modbus.setFlow(mfc.slaveId,0f);                    // set flow of current mfc to 0 before going on
                    if (sendingWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        if (createCalFile) calFileStream.Close();
                        if (createLogFile) fs.Close();
                        return;
                    }


                }

                // write measurements to file: 
                // date /time; temp; pressure; calibration point; flow_poi; measured mfc flow; mfc flow at absolute conditions; flow controller reading

                sendingWorker.ReportProgress(100, "Measurement completed.\r");
                modbus.resetSystem();
                mfcData.targetDatapoint = 0;
                mfcData.currentDatapoint = 0;

                fs.Close();
                calFileStream.Close();
                
            }else
            {
                sendingWorker.ReportProgress(1, "System pressure unstable.\rMeasurement cancelled.\r");
                e.Cancel = true;
            }
            if (sendingWorker.CancellationPending)
                {
                    e.Cancel = true;
                }
//                Thread.Sleep(100);
        }

        private double getActualFlow(double flow, double pressure, double temp, bool pressureCorrection, bool temperatureCorrection)
        {
            double testflow = flow / temp / 1013 * pressure * 273.15;
            double actualFlow = flow;
            if (pressureCorrection)
            {
                actualFlow = actualFlow * pressure / 1013;
            }
            if (temperatureCorrection)
            {
                actualFlow = actualFlow / temp * 273.15;
            }
            return actualFlow;
        }

        private string writeRecord(StreamWriter file)
        {
            double[] fields = new double[11];
            //fields[0] = DateTime.Now.ToString();           // time
            fields[0] = ts.currentTemp_kelvin;  // temperature
            fields[1] = ps.currentPressure_mbar;   //pressure
            fields[2] = mfcData.calibrationPoint;  // calibration point
            fields[3] = mfcData.targetDatapoint;  // programmed flow at std cond
            fields[4] = mfcData.currentDatapoint; // obtained flow at std cond
            fields[5] = mfcData.currentDatapoint * fields[0] * 1013 / fields[1] / 273.15; // obtained MFC flow at actual conditions
            fields[6] = proFlowData.currentDatapoint;  //proflow reading
            fields[7] = mfcData.standardDeviation;    // standard deviation of mfc rolling mean data
            fields[8] = mfcData.differenceFromTargetValue(true,false);
            fields[9] = proFlowData.standardDeviation;
            fields[10] = proFlowData.differenceFromTargetValue(true,false);

            string record = DateTime.Now.ToString();
            for (int i = 0; i < fields.Length; i++)
            {
                record = record + ";"+fields[i].ToString("0.0000");
            }
            //file.WriteLine(record + ";" + fm.getCurrentFlow);
            if (createLogFile)
            {
                file.WriteLine(record);
                file.Flush();
            }
            return record;
        }
        //
        //create a record to write to the protocol file
        private string createDataRecord(double temp, double pressure, double measurementPoint, double targetFlow, float currentFlow, double absFlow, String proFlow)
        {

            string[] fields = new string[8];
            fields[0] = DateTime.Now.ToString();           // time
            fields[1] = temp.ToString("0.0000");  // temperature
            fields[2] = pressure.ToString("0.0000");   //pressure
            fields[3] = measurementPoint.ToString("0.0000");
            fields[4] = targetFlow.ToString("0.0000");
            fields[5] = currentFlow.ToString("0.0000");
            fields[6] = absFlow.ToString("0.0000");
            fields[7] = proFlow;

            string record = String.Join(";", fields);
            return record;
        }



        // calibrationWorker_ProgressChanged
        // writes the next data line to the txtResult textbox
        protected void calibrationWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string s = e.UserState.ToString();
            if (s.Equals("valve"))
            {
                for (int i = 0; i < 6; i++)
                {
                    valveList.ElementAt(i).IsChecked = modbus.isValveOpen(i + 1);
                }
            }
            else
            {

            String txt = (s + "\n" + txtResult.Text);
            if (txt.Length > 1000)
            {
                txt.Remove(1000);
            }
            txtResult.Text = txt;    // else add a line in the status window
            
            }
            if (e.ProgressPercentage == 100)
            {
                btnStartCalibration.IsEnabled = true;
                btnStopCalibration.IsEnabled = false;

            }
        }

        protected void calibrationWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker sendingWorker = (BackgroundWorker)sender;
            // do the necessary to end the worker
            modbus.resetSystem();
            measurementRunning = false;
            
            mfcData.checkStability = false;
            mfcData.resetStabilityCheck();
            proFlowData.checkStability = false;
            proFlowData.resetStabilityCheck();
            foreach (CheckBox cbValve in valveList)
            {
                cbValve.IsChecked = false;
            }
            btnStartCalibration.IsEnabled = true;  //reset control elements after stopping the measuring process
            btnStopCalibration.IsEnabled = false;
            btn_valveControl.IsEnabled = true;
            enableSettingControls(true);
            lblFlowmeterOutput.Content = " ";
            lblMFCTargetFlow.Content = " ";
            lblMFCFlowCurCond.Content = "0";
            lblMFCFlowStdCond.Content = "0";
        }


        // functions for the modbus 


        private void btnModbusConnect_Click(object sender, RoutedEventArgs e)
        {
            if (cb_modbusDevice.SelectedIndex > -1)
            {
                btnModbusConnect.Visibility = Visibility.Hidden;
                btnModbusDisconnect.Visibility = Visibility.Visible;
                lblMFCFlowStdCond.Content = "Connecting...";
                if (!modbusWorker.IsBusy)
                {
                    modbusWorker.RunWorkerAsync(deviceList[cb_modbusDevice.SelectedIndex].SerialNumber);
                }
            }
        }

        private void btnModbusDisconnect_Click(object sender, RoutedEventArgs e)
        {
            modbusWorker.CancelAsync();

        }

        private void modbusWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            btnStartCalibration.IsEnabled = (allDevicesReady() & !measurementRunning);
            try
            {
                lblMFCTargetFlow.Content = mfcData.targetDatapoint.ToString("0.000") + " mL/min";
                lblMFCFlowStdCond.Content = mfcData.currentDatapoint.ToString("0.000") + " mL/min"; // displays current flow
                if (ts.isOpen() & ps.isOpen())
                {
                    lblMFCFlowCurCond.Content = getActualFlow(mfcData.currentDatapoint, ps.currentPressure_mbar, ts.currentTemp_kelvin,true,true).ToString("0.000") + " mL/min";
                }else
                {
                    lblMFCFlowCurCond.Content = "invalid data";
                }
                for (int i = 0; i < 6; i++)
                {
                    valveList.ElementAt(i).IsChecked = modbus.isValveOpen(i + 1);
                }
            }
            catch { }
        }

        private void modbusWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {

            modbus.disconnect();
            btnModbusConnect.Visibility = Visibility.Visible;
            btnModbusDisconnect.Visibility = Visibility.Hidden;
            lblMFCFlowStdCond.Content = " ";
            lblMFCFlowStdCond.Content = " ";
            lblMFCTargetFlow.Content = " ";
            foreach (CheckBox cbValve in valveList){
                cbValve.IsChecked = false;
            }
        }

        private void modbusWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker sendingWorker = (BackgroundWorker)sender;
            modbus.connect(e.Argument.ToString());
            if (modbus.isOpen())
            {

                while (true)
                {
                    sendingWorker.ReportProgress(1, "");  // causes the progress_changed routine to update the mfc flow data on the main window
                    if (sendingWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    Thread.Sleep(Constants.mfcDelay);
                }
            }
        
        }


        //
        // Flowmeter
        //
        private void flowmeterWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker sendingWorker = (BackgroundWorker)sender;
            fm.connectFTDI(e.Argument.ToString());
            if (fm.isOpen())
            {
                Resume(flowmeterWorkerResetEvent);
                //fm.enableUSBCommunication();
                //                title_Flowmeter.Content = title_Flowmeter.Content + " " + fm.serialNumber;
                fm.powerOff(0);   // set flowmeter to constant on
                sendingWorker.ReportProgress(1, "Autopoweroff set to " + fm.autoPowerOff + " min");
                fm.readSerialNumber();
                sendingWorker.ReportProgress(1, "Connected to ProFlow " + fm.serialNumber);
                fm.setLinearityCorrection(false);

                while (true)
                {
                    flowmeterWorkerResetEvent.WaitOne(Timeout.Infinite);       // waiting point if worker is paused
//                    fm.readFlow();
                    proFlowData.currentDatapoint = fm.getCurrentFlowAsSingle;
                    //                    sendingWorker.ReportProgress(1, fm.getCurrentFlowValue);
                    sendingWorker.ReportProgress(1, proFlowData.currentDatapoint);

                    if (sendingWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    Thread.Sleep(Constants.flowmeter_delay);
                }
            }
        }

        private void flowmeterWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string s = e.UserState.ToString();
            lblFlowmeterOutput.Content = s;    // displays current flow
        }

        private void flowmeterWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            fm.close();
            btnFlowmeterConnect.Visibility = Visibility.Visible;
            btnFlowmeterDisconnect.Visibility = Visibility.Hidden;
            lblFlowmeterOutput.Content = " ";
        }

        private void btnFlowmeterConnect_Click(object sender, RoutedEventArgs e)
        {
            if (cb_flowmeterDevice.SelectedIndex > -1)
            {
                btnFlowmeterConnect.Visibility = Visibility.Hidden;
                btnFlowmeterDisconnect.Visibility = Visibility.Visible;
                lblFlowmeterOutput.Content = "Connecting...";
                if (!flowmeterWorker.IsBusy)
                {
                    flowmeterWorker.RunWorkerAsync(deviceList[cb_flowmeterDevice.SelectedIndex].SerialNumber);
                }
            }
        }

        private void btnFlowmeterDisconnect_Click(object sender, RoutedEventArgs e)
        {
            flowmeterWorker.CancelAsync();
        }

        private void chbCalibrationMode_Checked(object sender, RoutedEventArgs e)
        {
            Pause(flowmeterWorkerResetEvent);
            if (chbCalibrationMode.IsChecked == true)
            {
                fm.enableCalibrationMode();
                cb_linearityMode.IsEnabled = true;
                cb_linearityMode.IsChecked = fm.linearityCorrection;
//                cb_linearityMode.IsEnabled = fm.linearityCorrection;
            }
            else
            {
                fm.disableCalibrationMode();
                cb_linearityMode.IsEnabled = fm.linearityCorrection;
            }
            Resume(flowmeterWorkerResetEvent);
        }

        private void btnSelectCalFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveCalFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveCalFileDialog.FileName = calibrationFileName;
            saveCalFileDialog.OverwritePrompt = true;
            saveCalFileDialog.Filter = "Calibration File (.cal)|*.cal|Text File (.txt)|*.txt|All Files|*.*";
            saveCalFileDialog.FilterIndex = 1;

            bool? userClickedOK = saveCalFileDialog.ShowDialog();
            if (userClickedOK == true)
            {
                calibrationFileName = saveCalFileDialog.FileName;
                tbCalFile.Text = calibrationFileName;
                tbCalFile.ToolTip = calibrationFileName;
            }

        }

        private void btnSelectLogFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveLogFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveLogFileDialog.FileName = logFileName;
            saveLogFileDialog.OverwritePrompt = true;
            saveLogFileDialog.Filter = "CSV (.csv)|*.csv|Text File (.txt)|*.txt|All Files|*.*";
            saveLogFileDialog.FilterIndex = 1;

            bool? userClickedOK = saveLogFileDialog.ShowDialog();
            if (userClickedOK == true)
            {
                logFileName = saveLogFileDialog.FileName;
                tbxLogFile.Text = logFileName;
                tbxLogFile.ToolTip = logFileName;
            }

        }

        private void btnFlowmeterSettings_Click(object sender, RoutedEventArgs e)
        {
            Pause(flowmeterWorkerResetEvent);
            txtResult.Text = fm.getCalibrationParameters()+"\n" + txtResult.Text;
            Resume(flowmeterWorkerResetEvent);
        }

        private void btnSetSerialNo_Click(object sender, RoutedEventArgs e)
        {
            Pause(flowmeterWorkerResetEvent);
            txtResult.Text = fm.setSerialNumber("RE105999");
            Resume(flowmeterWorkerResetEvent);
        }

        private void cb_calibrationPts_Click(object sender, RoutedEventArgs e)
        {
            if (cb_calibrationPts.IsChecked == true)     // use the measurement points shown in the calibration document
            {
                modbus.getMFC(0).calibrationPoints = Constants.certification_segment1.Concat(Constants.certification_segment2).ToArray();
                modbus.getMFC(1).calibrationPoints = Constants.certification_segment3;
                modbus.getMFC(2).calibrationPoints = Constants.certification_segment4.Concat(Constants.certification_segment5).ToArray();
            }
            else                     // use the measurement points needed for the calibration process
            {
                modbus.getMFC(0).calibrationPoints = Constants.flowSegment1.Concat(Constants.flowSegment2).ToArray();
                modbus.getMFC(1).calibrationPoints = Constants.flowSegment3;
                modbus.getMFC(2).calibrationPoints = Constants.flowSegment4.Concat(Constants.flowSegment5).ToArray();
            }
        }

        private void cb_linearityMode_Click(object sender, RoutedEventArgs e)
        {
            if (cb_linearityMode.IsChecked == true)
            {
                fm.setLinearityCorrection(true);
            }else
            {
                fm.setLinearityCorrection(false);
            }
        }

    }
}
