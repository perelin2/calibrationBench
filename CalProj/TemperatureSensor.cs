using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTD2XX_NET;




namespace CalProj
{

    class TemperatureSensor
    {
        private ushort _portNumber;
        private ushort _channelNumber;
        private ushort _data_type = 1;
        private ushort _no_of_wires = 4;
        private ushort _filtered = 1;
        private long _currentTemperature;
        private bool _isOpen;
        private int delayMsec = 180; // the sensor sends one reading every 180 msec

        public TemperatureSensor()
        {
            _isOpen = false;

        }

        public float currentTemp_kelvin
        {
            get
            {
                float kelvin = (_currentTemperature / 1000.0f) + 273.0f;
                return kelvin;

            }
        }
        public double currentTemp_celsius
        {
            get
            {
                double value = _currentTemperature / 1000.0;
                return value;
            }

        }

        public void loadDevice(ushort comPort, ushort channel)
        {
            if (!_isOpen)
            {
                try
                {
                    _portNumber = comPort;
                    _channelNumber = channel;
                    int status = PT104Wrapper.pt104_open_unit(_portNumber);
                    if (status != 0)
                    {
                        // try again...
                        status = PT104Wrapper.pt104_open_unit(_portNumber);
                    }
                    if (status == 0) { 
                        _isOpen = true;
                        status = PT104Wrapper.pt104_set_channel(_portNumber, _channelNumber, _data_type, _no_of_wires);
                    }
                }
                catch 
                {
                    throw;
                }
            }
        }
        public bool isOpen()
        {
            return _isOpen;
        }

        public void closeDevice()
        {
            PT104Wrapper.pt104_close_unit(_portNumber);
            _isOpen = false;
        }


        public void read()
        {
            if (_isOpen)
            {
                System.Threading.Thread.Sleep(delayMsec);
                short status = PT104Wrapper.pt104_get_value(ref _currentTemperature,_portNumber,_channelNumber,_filtered);
//                if(status != 0)
//                {
//                    System.Console.WriteLine(currentTemp_kelvin);
//                }
             }
        }
    }
}
