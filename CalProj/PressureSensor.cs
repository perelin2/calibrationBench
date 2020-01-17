using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Runtime.InteropServices;

using FTD2XX_NET;



namespace CalProj
{

    class PressureSensor
    {
//        private int _deviceHandler;

        private FTDI _pressureDevice;
        private FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
        //private int _baudRate = 115200;
        //private byte _dataBits = FTDI.FT_DATA_BITS.FT_BITS_8;
        //private byte _stopBits = FTDI.FT_STOP_BITS.FT_STOP_BITS_1;
        //private byte _parity = FTDI.FT_PARITY.FT_PARITY_NONE;
        private uint _rxBytes;
        private uint _rxBytesRead;
        private byte[] _currentPressure = new byte[1];
        private int delayMsec = 500;
        

        public PressureSensor()
        {
            _pressureDevice = new FTDI();
        }

        public string currentPressure_inHG
        {
            get
            {
                string value = System.Text.Encoding.ASCII.GetString(_currentPressure);
                return value;
            }

        }

        public float currentPressure_mbar
        {
            get { 
                float pressureMBAR = 0.0f;
                float pressureHG = 0.0f;
                try
                {
                    string[] substrings = System.Text.Encoding.ASCII.GetString(_currentPressure).Split(' ');
                    pressureHG = Single.Parse(substrings[0]);
                    pressureMBAR = pressureHG * 25.4f / 0.75f;
                }catch
                {
                    
                    pressureMBAR = 0f;
                }
                return pressureMBAR;
            }
        }

        public void loadDevice(string serialNumber)
        {
            if (!_pressureDevice.IsOpen)
            {
                try
                {
                    _pressureDevice.OpenBySerialNumber(serialNumber);
                    _pressureDevice.SetBaudRate(Constants.pressure_baudrate);
                    _pressureDevice.SetDataCharacteristics(Constants.pressure_databits, Constants.pressure_stopbits, Constants.pressure_parity);
                    _pressureDevice.SetTimeouts(5000, 0);
                    _pressureDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0, 0);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public bool isOpen()
        {
            return _pressureDevice.IsOpen;
        }

        public void closeDevice()
        {
            _pressureDevice.Close();
        }

        public uint requestReading()
        {
            uint bytesWritten = 0;
            byte[] writeBuffer = { (byte)'P', (byte)'\r' };
            int bytesToWrite = 2;
            
            _pressureDevice.Write(writeBuffer, bytesToWrite, ref bytesWritten);
            return bytesWritten;
        }

        public bool read()
        {
            uint bytesWritten;
            bool success = false;
            
            if (isOpen())
            {
                _pressureDevice.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                _pressureDevice.ResetDevice();
                bytesWritten = requestReading();
                System.Threading.Thread.Sleep(delayMsec);
                ftStatus = _pressureDevice.GetRxBytesAvailable(ref _rxBytes);
                if (_rxBytes > 0)
                {
                    try
                    {
                        _currentPressure = new byte[_rxBytes];
                        ftStatus=_pressureDevice.Read(_currentPressure, _rxBytes, ref _rxBytesRead);
                        if (ftStatus == FTDI.FT_STATUS.FT_OK)
                        {
                            success= true;
                        }else
                        {
                            success= false;
                        }
                    }
                    catch
                    {
                        return false;
 //                       Console.WriteLine(e.Message);
                    }
                }
            }
            return success;
        }
    }
}
