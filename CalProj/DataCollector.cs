using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalProj
{
    // This class is used to store sets of data
    // The values sent to the collector are added to an array whose size is defined in the constructor
    // index points to the next position to store incomimg data, currentDatapoint points to the latest entry
    //
    class DataCollector
    {
        private double _targetDatapoint;
        private double _calibrationPoint;
        private double[] dataPoints;        // storing array for data values
        private double[] rollingMean;       // storing array for mean values based on all the data points
        private int index;                  // index of the slot for the next value
        private int _currentDatapoint;      // index of the slot of the latest value stored
        private int arraySize;              // length of datapoint array
        private double _variance;           // variance of the datapoints. Is refreshed after adding a datapoint 
        private double[] _standardDeviation;  // standard deviation of the datapoints. refreshed after adding a datapoint
        private double[] _percentualError;     // difference in percent of datapoint compared to target datapoint;
        private bool _checkStability;

        public DataCollector(int numberOfDatapoints)
        {
            arraySize = numberOfDatapoints;
            dataPoints = new double[arraySize];
            rollingMean = new double[arraySize];
            _standardDeviation = new double[arraySize];
            _percentualError = new double[arraySize];
            for (int i=0; i<arraySize; i++)
            {
                dataPoints[i] = 0.0;
                rollingMean[i] = 0.0;
                _standardDeviation[i] = 100;
                _percentualError[i] = 100;
            }
            index = 0;
            _currentDatapoint = 0;
            _checkStability = false;
            _targetDatapoint = 0;
            _calibrationPoint = 0;
        }

        public bool checkStability
        {
            get
            {
                return _checkStability;
            }
            set
            {
                _checkStability = value;
            }
        }

         public double currentDatapoint
        {
            get
            {
                return dataPoints[_currentDatapoint];
            }
            set
            {
                try
                {
                    dataPoints[index] = value;
                    rollingMean[index] = getMean();
                    if (_checkStability)
                    {
                        getVariance();
                        _standardDeviation[index] = getStandardDeviation();
                        _percentualError[index] = differenceFromTargetValue(true, true);
//                        System.Console.WriteLine("StdDev: "+_standardDeviation[index].ToString("0.00000") + "     %: " + _percentualError[index].ToString("0.00000"));
                    }
                    _currentDatapoint = index;
                    index = (index + 1) % arraySize;
                }
                catch 
                {
                    throw;
                }

            }
        }

        public double standardDeviation
        {
            get
            {
                return _standardDeviation[_currentDatapoint]; // this is only set if the checkStability variable is true
            }
        }

        public double variance
        {
            get
            {
                return _variance;
            }
        }

        public double targetDatapoint
        {
            get
            {
                return _targetDatapoint;
            }
            set
            {
                _targetDatapoint = value;
            }
        }

        public double calibrationPoint
        {
            get
            {
                return _calibrationPoint;
            }
            set
            {
                _calibrationPoint = value;
            }
        }

        public bool isSystemStable(double maxStdDeviation, double maxPercentualDeviation)
        {
            bool ok = true;
            int i = 0;
            while (ok && i < Constants.rollingMeanRange)
            {
                ok = (_standardDeviation[i] < maxStdDeviation) & (_percentualError[i]<maxPercentualDeviation);
                i++;
            }
            return ok;
        }

        public void resetStabilityCheck()
        {
            for (int i=0;i<Constants.rollingMeanRange; i++)
            {
                _standardDeviation[i] = 100;
            }
        }

        public double differenceFromTargetValue(bool inPercent, bool absolute)
        {
            double mean = getMean();
            double diff = mean - currentDatapoint;
            if (absolute) diff = Math.Abs(diff);
            if (inPercent)diff = diff * 100 / mean;
            return diff;
        }


        private double getMean()
        {
            double sum = 0.0;
            for (int i=0; i<arraySize; i++)
            {
                sum = sum + dataPoints[i];
            }
            return (sum / arraySize);
        }

        private void getVariance()
        {
            double mean = getMean();
            double sum = 0.0;
            double difference = 0.0;
            for (int i=0; i<arraySize; i++)
            {
                difference = dataPoints[i] - mean;
                sum = sum + (difference * difference);
            }
            _variance = (sum / arraySize);
        }

        private double getStandardDeviation()
        {
            return Math.Sqrt(_variance);
        }

        
        
    }
}
