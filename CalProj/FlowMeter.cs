using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTD2XX_NET;
using System.Threading;


namespace CalProj
{
    class FlowMeter
    {
        private FTDI _flowmeter;
        private uint _rxBytes;
        private uint _rxBytesRead;
        private byte[] _bytesRead = new byte[1];
        private byte[] _currentFlow = new byte[1];
        private int delayMsec = 400;
        private bool _calibrationMode;
        private bool _linearityCorrection;
        private bool _movingAverage;
        private bool _usbCommunication;
        private int _range;   // 0 = low, 1=high
        private string _autoPowerOff;
        private string _serialNo;

        string cr = "\r";
        string passcode = "\r";
        string chModeCmd = "\r";
        string onCmd = "on\r";
        string offCmd = "off\r";

        string highRange = "high\r";
        string lowRange = "low\r";
        string rangeCmd = "\r";

        string autoPowerCmd = "\r";
        string alwaysOn = "0\r";

        string usbCommunicationCmd = "\r";

        string linearityCmd = "\r";
        string movingAvgCmd = "\r";
        string autoRangeCmd = "\r";
        string serialNoCmd = "\r";

        string calibrationParamsCmd = "\r";
        string setSerialNumberCmd = "\r";

        public FlowMeter()
        {
            _flowmeter = new FTDI();
            if (_flowmeter.IsOpen) _flowmeter.Close();
            _calibrationMode = false;
        }

        ~FlowMeter()
        {
            if (_flowmeter.IsOpen) _flowmeter.Close();
        }

        public void connectFTDI(string serialNumber)
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            if (!_flowmeter.IsOpen)
            {
                ftStatus = _flowmeter.OpenBySerialNumber(serialNumber);
                //ftStatus |= _flowmeter.OpenByDescription("FT232R USB UART");
                ftStatus |= _flowmeter.SetBaudRate(115200);
                _flowmeter.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
                System.Threading.Thread.Sleep(delayMsec);
                ftStatus |= _flowmeter.SetTimeouts(5000, 0);
                System.Threading.Thread.Sleep(delayMsec);
                ftStatus |= _flowmeter.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0, 0);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    if (_flowmeter.IsOpen)
                    {
                        _flowmeter.Close();
                    }
                    throw new Exception("Could not open ProFlow");
                }
            }

        }

        public void close()
        {
            if (_flowmeter.IsOpen)
            {
                _flowmeter.Close();
            }
        }

        public bool isOpen()
        {
            return _flowmeter.IsOpen;
        }

        public string getCurrentFlow
        {    // returns proflow output unchanged
            get
            {
                string flowString = readFTDI();     // todo: review this method
                int x = flowString.IndexOf('\r');
                if (x > -1)
                {
                    flowString = flowString.Remove(x);            // cut the string at the first carrage return
                }
                return flowString;
            }
        }

        //
        // returns the flowrate as string
        public string getCurrentFlowValue     // _currentFlow is a string with the following format : "Flow@000.00 mL/min" 
        {                                     // this function will extract the measured flow without any other characters
            get
            {
                string flowString = getCurrentFlow;
                int x = flowString.IndexOf('@');
                if (x > 0)
                {
                    flowString = flowString.Substring(x + 1);
                }
                x = flowString.IndexOf(' ');
                if (x > 0)
                {
                    flowString = flowString.Remove(x);
                }
                return flowString;
            }
        }

        public float getCurrentFlowAsSingle    // converts the flow value from string to float
        {
            get
            {
                string flowString = getCurrentFlowValue;
                float flow = -1f;
                try
                {
                    flow = Convert.ToSingle(flowString);
                }
                catch
                {
                    flow = -1f;
                }
                return flow;
            }
        }

        public bool calibrationMode
        {
            get
            {
                return _calibrationMode;
            }

        }

        public bool usbCommunication
        {
            get
            {
                return _usbCommunication;
            }
        }

        public bool linearityCorrection
        {
            get
            {
                return _linearityCorrection;
            }
        }

        public int range
        {
            get
            {
                return _range;
            }
        }

        public string autoPowerOff
        {
            get
            {
                return _autoPowerOff;
            }
        }

        public string serialNumber
        {
            get
            {
                return _serialNo;
            }
        }

        public bool movingAverage
        {
            get
            {
                return _movingAverage;
            }
        }

        private FTDI.FT_STATUS writeCmd(string command)
        {
            uint bytesWritten = 0;
            int bytesToWrite;
            FTDI.FT_STATUS ftStatus;
            Encoding encoding = Encoding.ASCII;
            byte[] cmd = encoding.GetBytes(command);
            bytesToWrite = cmd.Length;

            _flowmeter.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
            ftStatus = _flowmeter.ResetDevice();
            ftStatus = _flowmeter.Write(cmd, bytesToWrite, ref bytesWritten);
            System.Threading.Thread.Sleep(delayMsec);
            return ftStatus;
        }

        private String readFTDI()
        {
            String response = "";
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

            if (isOpen())
            {
                ftStatus = _flowmeter.GetRxBytesAvailable(ref _rxBytes);
                if (_rxBytes > 0)
                {
                    try
                    {
                        _bytesRead = new byte[_rxBytes];
                        _flowmeter.Read(_bytesRead, _rxBytes, ref _rxBytesRead);
                        response = System.Text.Encoding.ASCII.GetString(_bytesRead);
                    }
                    catch
                    {
                        return response;
                        //                       Console.WriteLine(e.Message);
                    }
                }
            }
            return response;
        }


        private FTDI.FT_STATUS toggleCalibrationMode(bool on)
        {
            FTDI.FT_STATUS ftStatus;
            String response = "";

            if (on)
            {
                ftStatus = writeCmd(cr);
                ftStatus |= writeCmd(passcode);
                //                ftStatus |= setLinearityCorrection(false);  // for calibration mode
                //                ftStatus |= setLinearityCorrection(true);    // test mode
                //                ftStatus |= setMovingAverage(false);
                ftStatus |= writeCmd(chModeCmd);
                ftStatus |= writeCmd(onCmd);
            }
            else
            {
                ftStatus = writeCmd(chModeCmd);
                ftStatus |= writeCmd(offCmd);
            }
            response = readFTDI();
            if (response.StartsWith("on"))
            {
                _calibrationMode = true;
            }
            else if (response.StartsWith("off"))
            {
                _calibrationMode = false;
            }
            else
            {
                //do nothing
            }
            return ftStatus;
        }

        public void enableCalibrationMode()
        {
            FTDI.FT_STATUS ftStatus;
            ftStatus = toggleCalibrationMode(true);
        }

        public void disableCalibrationMode()
        {
            FTDI.FT_STATUS ftStatus;
            ftStatus = toggleCalibrationMode(false);
        }

        public FTDI.FT_STATUS readSerialNumber()
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            String response = "";
            ftStatus = writeCmd(cr);
            ftStatus |= writeCmd(passcode);
            ftStatus = writeCmd(serialNoCmd);
            response = readFTDI();
            int x = response.IndexOf('R');
            if (x > -1)
            {
                _serialNo = response.Substring(x, 8);
            }

            return ftStatus;
        }

        public FTDI.FT_STATUS powerOff(int minutes)     // 0 = never power off
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            String response = "";
            //            _flowmeter.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
            ftStatus = writeCmd(autoPowerCmd);
            ftStatus |= writeCmd(minutes.ToString() + cr);
            response = readFTDI();
            int x = response.IndexOf('\r');
            if (x > -1)
            {
                _autoPowerOff = response.Remove(x);            // cut the string at the first carrage return
            }
            return ftStatus;
        }


        private FTDI.FT_STATUS setFlowmeterRange(string range)
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            _flowmeter.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
            ftStatus = writeCmd(autoRangeCmd);
            ftStatus |= writeCmd(offCmd);
            ftStatus |= writeCmd(rangeCmd);
            ftStatus |= writeCmd(range);
            string response = readFTDI();
            if (response.StartsWith("high")) _range = 1;
            else if (response.StartsWith("low")) _range = 0;
            else _range = -1;
            return ftStatus;
        }

        public FTDI.FT_STATUS setHighRange()
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            ftStatus = setFlowmeterRange(highRange);
            return ftStatus;
        }
        public FTDI.FT_STATUS setLowRange()
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            ftStatus = setFlowmeterRange(lowRange);
            return ftStatus;
        }

        public String enableUSBCommunication()
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            string response = "";
            _flowmeter.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
            ftStatus = writeCmd(usbCommunicationCmd);
            ftStatus |= writeCmd(onCmd);
            response = readFTDI();
            _usbCommunication = response.StartsWith(onCmd);
            return response;
        }

        public FTDI.FT_STATUS setLinearityCorrection(bool onOff)
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            string response = "";
            ftStatus = writeCmd(linearityCmd);
            if (onOff) ftStatus |= writeCmd(onCmd);
            else ftStatus |= writeCmd(offCmd);
            response = readFTDI();
            _linearityCorrection = response.StartsWith(onCmd);
            return ftStatus;
        }

        public string setMovingAverage(bool onOff)
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            String response = "";
            ftStatus = writeCmd(movingAvgCmd);
            if (onOff) ftStatus |= writeCmd(onCmd);
            else ftStatus |= writeCmd(offCmd);
            response = readFTDI();
            if (response.StartsWith("on"))
            {
                _movingAverage = true;
            }
            else if (response.StartsWith("off"))
            {
                _movingAverage = false;
            }
            else
            {
                // do not change if response does not start with on or off
            }
            return response;
        }

        public string getCalibrationParameters()
        {
            string parameters = "";
            if (calibrationMode)
            {
                _flowmeter.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                writeCmd(calibrationParamsCmd);
                System.Threading.Thread.Sleep(delayMsec);
                parameters = readFTDI();
            }
            else
            {
                parameters = "Flowmeter is not in calibration mode";
            }
            return parameters;
        }

        public String setSerialNumber(string serialNo)
        {
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            string response = "";
            readSerialNumber();
            ftStatus = writeCmd(setSerialNumberCmd);
//            response = readFTDI();
//            ftStatus |= writeCmd("1\r");
            response = readFTDI();
            ftStatus |= writeCmd(serialNo+cr);   //write 8 digit serial number and a carrage return
            //            _linearityCorrection = response.StartsWith(onCmd);
            return response;
        }

    }
}
