using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace CalProj
{
    class ModbusSlave
    {
        private byte _slaveId;

        public ModbusSlave(byte slaveId)
        {
            _slaveId = slaveId;
        }

        public byte SlaveId
        {
            get { return _slaveId; }
            set { _slaveId = value; }
        }

     }

}
