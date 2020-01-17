using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTD2XX_NET;

namespace CalProj
{
    class MFC
    {
        private float[] _calPts;
        private ushort[] _openValves;
        byte _slaveId;
        ushort _flowMeterRange;
        double _maxStdDeviation;
        double _maxPercentualDeviation;
        int _delayBetweenCalPts;

        public MFC(byte slaveId,float[] calibrationPoints, ushort[] openValves, ushort flowmeterRange, double maxStdDeviation, double maxPercentualDeviation)
        {
            _slaveId = slaveId;
            _calPts = calibrationPoints;
            _openValves = openValves;
            _flowMeterRange = flowmeterRange;
            _maxStdDeviation = maxStdDeviation;
            _maxPercentualDeviation = maxPercentualDeviation;
}

        public byte slaveId
        {
            get
            {
                return _slaveId;
            }
            set
            {
                _slaveId = value;
            }
        }

        public float[] calibrationPoints
        {
            get
            {
                return _calPts;
            }
            set
            {
                _calPts = value;
            }
        }

        public ushort[] valvesToOpen
        {
            get
            {
                return _openValves;
            }
            set
            {
                _openValves = value;
            }
        }

        public  ushort flowmeterRange
        {
            get
            {
                return _flowMeterRange;
            }
            set
            {
                _flowMeterRange = value;    // 0 = low, 1 = high
            }
        }

        public double maxDeviation
        {
            get
            {
                return _maxStdDeviation;
            }
        }

        public double maxPercentualDeviation
        {
            get
            {
                return _maxPercentualDeviation;
            }
        }

        public int delayBetweenCalibrationPts
        {
            get { return _delayBetweenCalPts;
            }
            set
            {
                _delayBetweenCalPts = value;
            }
        }

        //private double generateMockFlow()
        //{
        //    // for testing returns a randomly generated flow.
        //    Random random = new Random(DateTime.Now.Millisecond);

        //    double mockFlow = _targetFlow + (random.NextDouble()/numberOfReadings);
        //    numberOfReadings++;
        //    return mockFlow;
        //}
    }
}
