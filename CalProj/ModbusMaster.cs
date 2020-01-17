using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.IO.Ports;
using FTD2XX_NET;


namespace CalProj
{
    class ModbusMaster
    {
        private FTDI _modbusMaster;
        private Valve valves;
        private MFC[] mfcs = new MFC[Constants.numberOfMFCsInvolved];

        private FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
        private int delayMsec = 50;
        private float _currentFlow;    // last read flow
        private byte _currentSlave;      // slave id of the last mfc read

        public ModbusMaster()
        {
            _modbusMaster = new FTDI();
            valves = new Valve(Constants.valves_slaveId);
            for (int i = 0; i < 6; i++)
            {
                bool valveState = isValveOpen(i + 1);
                valves.setValveState(i + 1, valveState);
            }
            mfcs[0] = new MFC(Constants.mfc500_slaveId, Constants.mfc500_calibrationPoints, Constants.mfc500Valves, Constants.mfc500_FlowmeterRange, Constants.mfc500_maxDeviation, Constants.mfc500_maxDifference);// add stability check conditions here...
            mfcs[0].delayBetweenCalibrationPts = Constants.mfc500_delayBetweenCalPts;
            mfcs[1] = new MFC(Constants.mfc50_slaveId, Constants.mfc50_calibrationPoints, Constants.mfc50Valves, Constants.mfc50_FlowmeterRange, Constants.mfc50_maxDeviation, Constants.mfc50_maxDifference);
            mfcs[1].delayBetweenCalibrationPts = Constants.mfc50_delayBetweenCalPts;
            mfcs[2] = new MFC(Constants.mfc10_slaveId, Constants.mfc10_calibrationPoints, Constants.mfc10Valves, Constants.mfc10_FlowmeterRange, Constants.mfc10_maxDeviation, Constants.mfc10_maxDifference);
            mfcs[2].delayBetweenCalibrationPts = Constants.mfc500_delayBetweenCalPts;
            _currentSlave = mfcs[0].slaveId;
            _currentFlow = 0f;
        }

        public MFC getMFC(int index)
        {
            return mfcs[index];
        }

        public float currentFlow
        {
            get
            {
                return _currentFlow;
            }
        }

        public byte currentSlave
        {
            get
            {
                return _currentSlave;
            }
        }
          

        public void connect(String serialNo)
        {
            if (!isOpen())
            {
               ftStatus =_modbusMaster.OpenBySerialNumber(serialNo);
               ftStatus |=_modbusMaster.SetBaudRate(Constants.modbus_baudrate);
               ftStatus |= _modbusMaster.SetDataCharacteristics(Constants.modbus_databit, Constants.modbus_stopbit, Constants.modbus_parity);
               ftStatus |= _modbusMaster.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0, 0);
               ftStatus |= _modbusMaster.SetTimeouts(1000, 1000);
               if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    _modbusMaster.Close();
                    throw new System.Exception("Could not open ModBus device with serial number "+Constants.modbus_serial);
                }
            }
        }

        public void disconnect()
        {
            try
            {
                resetSystem();
                if (_modbusMaster.IsOpen)
                {
                    _modbusMaster.Close();
                }
            }
            catch 
            {
                // do nothing
            }
        }


        public bool isOpen()
        {
            return _modbusMaster.IsOpen;
        }

        // sets the flow of all mfcs to 0 and closes all valves
        public void resetSystem()
        {
            foreach (MFC mfc in mfcs)
            {
                setFlow(mfc.slaveId, 0.0f);
                System.Threading.Thread.Sleep(delayMsec);
            }   // close all mfc
            closeAllValves();
        }

        public void openValve(ushort valveNumber)
        {
            if (valveNumber<1 | valveNumber > valves.numberOfValves)
            {
                throw new Exception("Invalid valve number" + valveNumber);
            }
            ushort coilNumber = (ushort)(valveNumber - 1);
            try
            {
                writeCoil(valves.SlaveId, coilNumber, true);
                valves.setValveState(valveNumber, true);
            }catch
            {
                throw new Exception("Could not open valve "+valveNumber);
            }
        }

        public void closeValve(ushort valveNumber)
        {
            if (valveNumber < 1 | valveNumber > valves.numberOfValves)
            {
                throw new Exception("Invalid valve number" + valveNumber);
            }
            ushort coilNumber = (ushort)(valveNumber - 1);
            try
            {
                if (valves.isValveOpen(valveNumber))
                {
                    System.Threading.Thread.Sleep(delayMsec);
                    writeCoil(valves.SlaveId, coilNumber, false);
                    valves.setValveState(valveNumber, false);
                }
            }
            catch
            {
                throw new Exception("Could not close valve " + valveNumber);
            }
        }

        public void closeAllValves()
        {
            for (ushort i = 1; i <= valves.numberOfValves; i++)
            {
                if (valves.isValveOpen(i))
                {
                    closeValve(i);
                    System.Threading.Thread.Sleep(delayMsec);
                }
            }
        }

        public bool isValveOpen(int valveNumber)
        {
            return valves.isValveOpen(valveNumber);
        }


        public void testMFC(MFC mfc, float flowrate)
        {
            ushort[] valves = mfc.valvesToOpen; 
            for (int i = 0; i < valves.Length; i++)
            {
                openValve(valves[i]);
            }
            setFlow(mfc.slaveId, flowrate);
            System.Threading.Thread.Sleep(delayMsec);
            getCurrentFlow(mfc.slaveId);
        }

        public void setFlow(byte slaveId, float flowRate)
        {
            byte[] flow = BitConverter.GetBytes(flowRate);
            Array.Reverse(flow);
            byte targetFlowRegister = 0x0006;
            try
            {
                writeRegisters(slaveId, targetFlowRegister, flow, 2);
                System.Threading.Thread.Sleep(delayMsec);
                getCurrentFlow(slaveId);

            }catch 
            {
                throw new Exception("Could not set flow at "+flowRate.ToString("0.0000"));
            }
        }

        public float getCurrentFlow(byte slaveId)
        {
            float flow = (float)0.0;
            byte[] flowArray = new byte[4];
            byte[] response = new byte[8];

            try
            {
                response = readRegister(slaveId, 0x0000, 2);
                for (int i = 0; i < 4; i++)
                {
                    flowArray[i] = response[6 - i];    // writes response byte 3 to 6 to flowArray in reverse order
                }
                flow = BitConverter.ToSingle(flowArray, 0);
                _currentFlow = flow;
                _currentSlave = slaveId;
            }
            catch 
            {
                throw ;
            }

            return flow;
        }

        private void writeCoil(byte slaveId, ushort coilNumber, bool on)
        {
            // opens a coil if on is true or
            // closes a coil if on is false

            byte[] request = new byte[8];
            byte[] response = new byte[8];
            UInt16 CRC;
            uint bytesWritten = 0;

            request[0] = slaveId;   // slave ID
            request[1] = 0x05;  // write coil command
            request[2] = (byte)(coilNumber >> 8);  // Hi byte of coil address
            request[3] = (byte)(coilNumber);  // lo byte of coil address
            if (on)
            {
                request[4] = 0xFF;
            }
            else
            {
                request[4] = 0;
            }
            request[5] = 0;
            CRC = ModRTU_CRC(request, 6);
            request[6] = (byte)(CRC >> 8);
            request[7] = (byte)(CRC);
            _modbusMaster.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
            System.Threading.Thread.Sleep(delayMsec);
            ftStatus = _modbusMaster.Write(request, request.Length, ref bytesWritten);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                if (on) throw new System.Exception("Could not open coil number " + coilNumber);
                else throw new System.Exception("Could not close coil number" + coilNumber);
            }else
            {
                System.Threading.Thread.Sleep(delayMsec);

            }
        }


        private byte[] readCoil(ModbusSlave slave, ushort coilNumber, ushort numberOfCoils)
        {
            byte[] request = new byte[8];
            byte[] response = new byte[8];
            UInt16 CRC;
            uint bytesWritten = 0;
//            uint numBytes = 0;

            request[0] = slave.SlaveId;   // slave ID
            request[1] = 0x01;  // read coil command
            request[2] = (byte)(coilNumber >> 8);  // Hi byte of coil address
            request[3] = (byte)coilNumber;  // lo byte of coil address
            request[4] = (byte)(numberOfCoils >> 8);  // number of coils to read (2 bytes)
            request[5] = (byte)numberOfCoils;
            CRC = ModRTU_CRC(request, 6);
            request[6] = (byte)(CRC >> 8);
            request[7] = (byte)(CRC);
            _modbusMaster.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
//            System.Threading.Thread.Sleep(delayMsec);
            ftStatus = _modbusMaster.Write(request, request.Length, ref bytesWritten);
            System.Threading.Thread.Sleep(delayMsec);
            ftStatus = _modbusMaster.Read(response, (uint)response.Length, ref bytesWritten);
            return response;

        }


        private void writeRegisters(byte slaveId, ushort register, byte[] values, int numberOfRegisters)
        {
            // function 16
            int noOfRegs = numberOfRegisters;
            byte[] response = new byte[8];
            int requestLength = 9 + 2 * noOfRegs;
            byte[] request = new byte[requestLength];
            UInt16 CRC;
            uint bytesWritten = 0;


            request[0] = slaveId;   // slave ID
            request[1] = 0x10;  // read registers command
            request[2] = (byte)(register >> 8);  // Hi byte of register address
            request[3] = (byte)(register);       // lo byte of register address
            request[4] = (byte)(noOfRegs >> 8);  // hi byte or number of register to read
            request[5] = (byte)noOfRegs;         // lo byte of number of registers to read
            request[6] = (byte)(noOfRegs * 2);   // number of bytes to write (2 bytes per register)
            for (int i = 0; i < values.Length; i++)   // write values
            {
                request[7+i] = values[i];

            }
            CRC = ModRTU_CRC(request, request.Length - 2);
            request[request.Length - 2] = (byte)(CRC);            // lo byt CRC
            request[request.Length - 1] = (byte)(CRC >> 8);      // hi byte CRC

            if (_modbusMaster.IsOpen)
            {
                _modbusMaster.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                System.Threading.Thread.Sleep(delayMsec);
                ftStatus = _modbusMaster.Write(request, request.Length, ref bytesWritten);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    throw new Exception("Could not write to slave " + slaveId);
                }
//                System.Threading.Thread.Sleep(delayMsec);
           }
        }

        private byte[] readRegister(byte slaveId, ushort register, ushort noOfRegs)
        {
            // function 3
            byte[] request = new byte[8];
            byte[] response = new byte[5 + 2 * noOfRegs];
            UInt16 CRC;
            uint numBytesWritten = 0;
            uint numBytesRead = 0;

            if (_modbusMaster.IsOpen)
            {
                request[0] = slaveId;   // slave ID
                request[1] = 0x03;  // read registers command
                request[2] = (byte)(register >> 8);  // Hi byte of register address
                request[3] = (byte)(register);  // lo byte of register address
                request[4] = (byte)(noOfRegs >> 8);  // hi byte or number of register to read
                request[5] = (byte)noOfRegs;         // lo byte of number of registers to read
                CRC = ModRTU_CRC(request, 6);
                request[6] = (byte)(CRC);            // lo byt CRC
                request[7] = (byte)(CRC >> 8);      // hi byte CRC

                _modbusMaster.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                System.Threading.Thread.Sleep(delayMsec);
                ftStatus = _modbusMaster.Write(request, request.Length, ref numBytesWritten);
                if (ftStatus != FTDI.FT_STATUS.FT_OK) throw new Exception("Read request to slave " + slaveId + " failed.");
                System.Threading.Thread.Sleep(delayMsec);
                ftStatus = _modbusMaster.Read(response, (uint)response.Length, ref numBytesRead);
                if (ftStatus != FTDI.FT_STATUS.FT_OK) throw new Exception("Reading slave " + slaveId + " failed.");
            }
            return response;
        }


        private UInt16 ModRTU_CRC(byte[] buf, int len)
        {
            UInt16 crc = 0xFFFF;

            for (int pos = 0; pos < len; pos++)
            {
                crc ^= (UInt16)buf[pos];          // XOR byte into least sig. byte of crc

                for (int i = 8; i != 0; i--)
                {    // Loop over each bit
                    if ((crc & 0x0001) != 0)
                    {      // If the LSB is set
                        crc >>= 1;                    // Shift right and XOR 0xA001
                        crc ^= 0xA001;
                    }
                    else                            // Else LSB is not set
                        crc >>= 1;                    // Just shift right
                }
            }
            // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
            return crc;
        }

        

    }
}
