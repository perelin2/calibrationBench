using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using FTD2XX_NET;

namespace CalProj
{
    public class Constants
    {
        //
        //  Temperature 
        //

        public static ushort temp_port = 7;
        public static ushort temp_channel = 1;
        public static int temp_delay = 100;      // min waiting time between commands

        //
        //  Pressure 
        //

//        public static string pressure_serial = "452131";
        public static string pressure_serial = "430218";
        public static uint pressure_baudrate = 9600;
        public static byte pressure_databits = FTDI.FT_DATA_BITS.FT_BITS_8;
        public static byte pressure_stopbits = FTDI.FT_STOP_BITS.FT_STOP_BITS_1;
        public static byte pressure_parity = FTDI.FT_PARITY.FT_PARITY_NONE;


        //
        // Modbus
        //
        public static string modbus_serial = "FTQAV1N4";
        public static uint modbus_baudrate = 9600;
        public static byte modbus_startbit = 1;
        public static byte modbus_stopbit = FTDI.FT_STOP_BITS.FT_STOP_BITS_1;
        public static byte modbus_databit = FTDI.FT_DATA_BITS.FT_BITS_8;
        public static byte modbus_parity = FTDI.FT_PARITY.FT_PARITY_NONE;
        public static uint modbus_readtimeout = 1000;
        public static uint modbus_writetimeout = 1000;
        public static uint modbus_maxBufferSize = 300;
        public static int mfcDelay = 20;    // waiting time between mfc commands

        //
        //  MFC 
        //
        public static int numberOfMFCsInvolved = 3;
        public static byte mfc10_slaveId = 2;
        public static byte mfc50_slaveId = 5;
        public static byte mfc500_slaveId = 10;

        //public static float[] flowSegment1 = { 510f, 509f, 508f, 507f, 506f, 500f, 475f, 450f, 425f, 400f, 375f, 350f, 325f, 300f, 275f, 250f, 225f, 200f, 175f, 150f, 125f, 100f, 75f, 74f, 73f, 72f, 71f };
        //public static float[] flowSegment2 = { 100.6f, 100.5f, 100.35f, 100.3f, 100.1f, 100f, 95f, 90f, 85f, 80f, 75f, 70f, 65f, 60f, 55f, 50f, 45f, 44.9f, 44.7f, 44.6f, 44.5f, 44.3f };
        //public static float[] flowSegment3 = { 50.5f, 50.4f, 50.3f, 50.2f, 50.1f, 50f, 45f, 40f, 35f, 30f, 25f, 20f, 19.9f, 19.8f, 19.7f, 19.6f, 19.5f };
        //public static float[] flowSegment4 = { 25.5f, 25.4f, 25.3f, 25.2f, 25.1f, 25f, 24f, 23f, 22f, 21f, 20f, 19f, 18f, 17f, 16f, 15f, 14f, 13f, 12f, 11f, 10f, 9f, 8.9f, 8.8f, 8.7f, 8.6f, 8.5f, 10.75f, 10.7f, 10.65f, 10.6f, 10.55f, 10.5f };
        //public static float[] flowSegment5 = { 10f, 9.5f, 9f, 8.5f, 8f, 7.5f, 7f, 6.5f, 6f, 5.5f, 5f, 4.5f, 4f, 3.5f, 3f, 2.5f, 2f, 1.5f, 1.45f, 1.4f, 1.35f, 1.3f, 1.25f };
        //public static float[] flowSegment6 = { 2.35f, 2.3f, 2.25f, 2.2f, 2.15f, 2.1f, 2f, 1.9f, 1.8f, 1.7f, 1.6f, 1.5f, 1.4f, 1.3f, 1.2f, 1.1f, 1f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.35f, 0.3f, 0.25f, 0.2f, 0.15f };

        //public static float[] flowSegment1 = { 510f };
        //public static float[] flowSegment2 = { 100.6f };
        //public static float[] flowSegment3 = { 50.5f };
        //public static float[] flowSegment4 = {  8.5f };
        //public static float[] flowSegment5 = { 10.75f,  1.25f };
        //public static float[] flowSegment6 = { 2.35f,  0.5f, 0.4f, 0.35f, 0.3f, 0.25f, 0.2f, 0.15f };

        public static float[] flowSegment1 = { 510f, 509f, 505f, 500f, 475f, 450f, 425f, 400f, 375f, 350f, 325f, 300f, 275f, 250f, 225f, 200f, 175f, 150f, 125f, 100f, 95f, 90f };
        public static float[] flowSegment2 = { 100.5f, 100.375f, 100.25f, 100.125f, 100f, 95f, 90f, 85f, 80f, 75f, 70f, 65f, 60f, 55f, 50f, 49.5f, 49f, 48.5f, 48f, 47.5f };
        public static float[] flowSegment3 = { 50f, 49.5f, 49f, 48.5f, 48f, 47.5f, 46f, 45f, 40f, 35f, 30f, 25f, 24f, 23f, 22f, 21f, 20f, 19f, 18f, 17f, 16f, 15f, 14f, 13f, 12f, 11f, 10f, 9.95f, 9.9f, 9.85f, 9.8f, 9.75f, 9.7f, 9.5f };
        public static float[] flowSegment4 = { 10f, 9.95f, 9.9f, 9.85f, 9.8f, 9.75f, 9.7f, 9.5f, 9f, 8.5f, 8f, 7.5f, 7f, 6.5f, 6f, 5.5f, 5f, 4.5f, 4f, 3.5f, 3f, 2.5f, 2f, 1.5f, 1.45f, 1.4f, 1.35f, 1.3f, 1.25f };
        public static float[] flowSegment5 = { 2.35f, 2.3f, 2.25f, 2.2f, 2.15f, 2.1f, 2f, 1f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.35f, 0.3f, 0.25f, 0.2f, 0.15f };

        public static float[] certification_segment1 = { 500f, 375f, 250f, 125f };
        public static float[] certification_segment2 = { 50f };
        public static float[] certification_segment3 = { 50f, 40f, 30f, 20f, 10f };
        public static float[] certification_segment4 = { 10f, 8f, 6f, 5f, 4f, 2f, 1f };
        public static float[] certification_segment5 = { 2f, 1f };


        //public static float[] flowSegment1 = { 500f, 375f, 250f,125f };
        //public static float[] flowSegment2 = { 50f };
        //public static float[] flowSegment3 = { 50f, 40f, 30f,20f,10f };
        //public static float[] flowSegment4 = { 10f, 8f,6f,5f,4f,2f, 1f };
        //public static float[] flowSegment5 = { 2f,1f };

        public static float[] mfc10_calibrationPoints = flowSegment4.Concat(flowSegment5).ToArray();
        public static float[] mfc50_calibrationPoints = flowSegment3;
        public static float[] mfc500_calibrationPoints = flowSegment1.Concat(flowSegment2).ToArray();
        public static ushort[] mfc10Valves = { 3, 4 };
        public static ushort[] mfc50Valves = { 2, 4 };
        public static ushort[] mfc500Valves = { 1, 4 };
        public static ushort mfc10_FlowmeterRange = 0;      // 0 = low range
        public static ushort mfc50_FlowmeterRange = 0;      // 1 = high range
        public static ushort mfc500_FlowmeterRange = 1;



        //
        // Valves
        //
        public static byte valves_slaveId = 101;

        //
        // Flowmeter
        //
        public static string flowmeter_serial = "AJV9K5XI";
        public static string flowmeter_description = "FT232R USB UART";
        public static uint flowmeter_baudrate = 115200;
        public static byte flowmeter_databits = 8;
        public static byte flowmeter_stopbits = 1;
        public static byte flowmeter_parity = 0;
        public static int flowmeter_delay = 400;   // mSec to wait before next reading
        public static float flowmeter_maxFlowAllowed = 500f;

        //
        // Measurement parameters
        //
        public static int rollingMeanRange = 5;   // number of measurement to collect to calculate the mean value
        public static double mfc10_maxDeviation = 0.05;  // deviation allowed to consider the measurement as valid
        public static double mfc50_maxDeviation = 0.06;  // deviation allowed to consider the measurement as valid
        public static double mfc500_maxDeviation = 0.06;  // deviation allowed to consider the measurement as valid
        public static double proFlow_maxDeviationHigh = 0.06;  //deviation for proFlow allowed when in high range
        public static double proFlow_maxDeviationLow = 0.1;   //deviation for proFlow allowed when in low range
        public static double proFlow_maxDeviationSuperLow = 0.7;
        public static int readingsPerSecond = 2;
        public static int maxRetries = 60*readingsPerSecond;        // number of readings before a measurement is considered unstable, wait time: 60 sec
        public static int mfc500_delayBetweenCalPts = 10000;  // mSec to wait before evaluating a new calibration point
        public static int mfc50_delayBetweenCalPts = 20000;
        public static int mfc10_delayBetweenCalPts = 120000;
        public static double mfc10_maxDifference = 1.0;    // difference to mean flow in percent for the measured flow to be considered stable
        public static double mfc50_maxDifference = 0.1;    
        public static double mfc500_maxDifference = 0.1;
        public static double proFlow_maxDifferenceHigh = 0.1;
        public static double proFlow_maxDifferenceLow = 0.5;
        public static double proFlow_maxDifferenceSuperLow = 2;

        //
        // Data storage
        //
        public static string logFilePath = @"S:\Restek Flowmeter Kalibrierung\Kalibrierfiles\";    // file containing the 2351 datapoints to measure
        public static string logFileName = @"data";
        public static string logFileExtension = ".csv";
        public static int numberOfValuesToSave = 10;
        public static string logFileHeader = "time; temp;pressure;calibration point;programmed flow at std cond;obtained flow at std cond;obtained MFC flow at actual conditions;ProFlow reading;stdev of obtained mfc flow;difference of obtained mfc flow in percent;stdev of ProFlow reading;difference of ProFlow reading in percent";
        public static bool writeLogFile = true;

        public static string calibrationFilePath = @"S:\Restek Flowmeter Kalibrierung\Kalibrierfiles\";     // calibration file to upload to ProFlow
        public static string calibrationFileName = @"flowmeter";
        public static string calibrationFileExtension = ".cal";
        //        public static string[] calFileHeader = {"0.5,2.0,10.0,25.0,50.0,100",
        //"1.99,9.99,24.9,49.9,99.9,600",
        //"0.01,0.01,0.1,0.1,0.1,1",
        //                                                flowSegment5.Length.ToString()+","+flowSegment4.Length.ToString()+","+flowSegment3.Length.ToString()+","+flowSegment2.Length.ToString()+","+flowSegment1.Length.ToString(),
        //                                                "7",
        //                                                "3" };
        public static string[] calFileHeader = {"0.5,2.0,10.0,50.0,100",
                                                "1.99,9.99,49.9,99.9,500",
                                                "0.01,0.01,0.1,0.1,1",
                                                flowSegment5.Length.ToString()+","+flowSegment4.Length.ToString()+","+flowSegment3.Length.ToString()+","+flowSegment2.Length.ToString()+","+flowSegment1.Length.ToString(),
                                                "7",
                                                "3" };
    }
}
