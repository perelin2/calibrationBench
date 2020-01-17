using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalProj
{
    class Valve : ModbusSlave
    {
        byte _slaveId;
        private int numValves = 12;
        private bool[] valveState;
        // controls the valves of the system
        // see documentation for valve number

        public Valve(byte slaveId) : base(slaveId)
        {
            _slaveId = slaveId;
            valveState = new bool[numValves];
            for (int i = 0; i < numValves; i++)
            {
                valveState[i] = false;
            }
        }

        public int numberOfValves
        {
            get
            {
                return numValves;
            }
            set
            {
                numValves = value;
            }
        }

        public bool isValveOpen(int valveNo) 
        {
            if (valveNo > 0 & valveNo <= numValves)
            {
                return valveState[valveNo - 1];
            }else
            {
                throw new Exception("Valve number "+valveNo+" does not exist");
            }
        }

        public void setValveState(int valveNo, bool state)
        {
            if (valveNo > 0 & valveNo <= numValves)
            {
                valveState[valveNo - 1] = state;
            }
            else
            {
                throw new Exception("Valve number " + valveNo + " does not exist");
            }

        }
    }
}
