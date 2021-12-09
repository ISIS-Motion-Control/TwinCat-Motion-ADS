using System;
using CsvHelper.Configuration.Attributes;

namespace TwinCat_Motion_ADS
{
    public class End2endReversalCSV
    {
        [Name("Cycle")]
        public int Cycle { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("ElapsedTime")]
        public long ElapsedTime { get; set; }
        [Name("LimitPosition")]
        public double LimitPosition { get; set; }
        [Name("Device1Measurement")]
        public string Device1Measurement { get; set; }
        [Name("Device2Measurement")]
        public string Device2Measurement { get; set; }
        [Name("Device3Measurement")]
        public string Device3Measurement { get; set; }
        [Name("Device4Measurement")]
        public string Device4Measurement { get; set; }

        public End2endReversalCSV(int cycle, string status, long elapsedTime, double limitPosition, string device1Measurement = "", string device2Measurement = "", string device3Measurement = "", string device4Measurement = "")
        {
            Cycle = cycle;
            Status = status;
            ElapsedTime = elapsedTime;
            LimitPosition = limitPosition;
            Device1Measurement = device1Measurement;
            Device2Measurement = device2Measurement;
            Device3Measurement = device3Measurement;
            Device4Measurement = device4Measurement;
        }
    }
    public class End2endCSV
    {
        [Name("Cycle")]
        public int Cycle { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("ElapsedTime")]
        public long ElapsedTime { get; set; }
        [Name("LimitPosition")]
        public double LimitPosition { get; set; }


        public End2endCSV(int cycle, string status, long elapsedTime, double limitPosition)
        {
            Cycle = cycle;
            Status = status;
            ElapsedTime = elapsedTime;
            LimitPosition = limitPosition;
        }
    }

    public class UniDirectionalAccuracyCSV
    {
        [Name("Cycle")]
        public uint Cycle { get; set; }
        [Name("Step")]
        public uint Step { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("TargetPosition")]
        public double TargetPosition { get; set; }
        [Name("EncoderPosition")]
        public double EncoderPosition { get; set; }
        [Name("Device1Measurement")]
        public string Device1Measurement { get; set; }
        [Name("Device2Measurement")]
        public string Device2Measurement { get; set; }
        [Name("Device3Measurement")]
        public string Device3Measurement { get; set; }
        [Name("Device4Measurement")]
        public string Device4Measurement { get; set; }

        public UniDirectionalAccuracyCSV(uint cycle, uint step, string status, double targetPosition, double encoderPosition, string device1Measurement = "", string device2Measurement = "", string device3Measurement = "", string device4Measurement = "")
        {
            Cycle = cycle;
            Step = step;
            Status = status;
            TargetPosition = targetPosition;
            EncoderPosition = encoderPosition;
            Device1Measurement = device1Measurement;
            Device2Measurement = device2Measurement;
            Device3Measurement = device3Measurement;
            Device4Measurement = device4Measurement;
        }
    }
    public class UniDirectionalAccCSV
    {
        [Name("Cycle")]
        public uint Cycle { get; set; }
        [Name("Step")]
        public uint Step { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("TargetPosition")]
        public double TargetPosition { get; set; }
        [Name("EncoderPosition")]
        public double EncoderPosition { get; set; }

        public UniDirectionalAccCSV(uint cycle, uint step, string status, double targetPosition, double encoderPosition)
        {
            Cycle = cycle;
            Step = step;
            Status = status;
            TargetPosition = targetPosition;
            EncoderPosition = encoderPosition;
        }
    }

    public class PneumaticEnd2EndCSVv2
    {
        [Name("Cycle")]
        public uint Cycle { get; set; }
        [Name("SettlingRead")]
        public uint SettlingRead { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("ExtendLimit")]
        public bool ExtendLimit { get; set; }
        [Name("RetractLimit")]
        public bool RetractLimit { get; set; }
        [Name("ElapsedTime")]
        public TimeSpan ElapsedTime { get; set; }


        public PneumaticEnd2EndCSVv2(uint cycle, uint settlingRead, string status, bool extendLimit, bool retractLimit, TimeSpan elapsedTime)
        {
            Cycle = cycle;
            SettlingRead = settlingRead;
            Status = status;
            ExtendLimit = extendLimit;
            RetractLimit = retractLimit;
            ElapsedTime = elapsedTime;
        }
    }

    public class PneumaticEnd2EndCSV
    {
        [Name("Cycle")]
        public uint Cycle { get; set; }
        [Name("SettlingRead")]
        public uint SettlingRead { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("ExtendLimit")]
        public bool ExtendLimit { get; set; }
        [Name("RetractLimit")]
        public bool RetractLimit { get; set; }
        [Name("ElapsedTime")]
        public TimeSpan ElapsedTime { get; set; }
        [Name("Device1Measurement")]
        public string Device1Measurement { get; set; }
        [Name("Device2Measurement")]
        public string Device2Measurement { get; set; }
        [Name("Device3Measurement")]
        public string Device3Measurement { get; set; }
        [Name("Device4Measurement")]
        public string Device4Measurement { get; set; }
        [Name("Device1Timestamp")]
        public long Device1Timestamp { get; set; }
        [Name("Device2Timestamp")]
        public long Device2Timestamp { get; set; }
        [Name("Device3Timestamp")]
        public long Device3Timestamp { get; set; }
        [Name("Device4Measurement")]
        public long Device4Timestamp { get; set; }


        public PneumaticEnd2EndCSV(uint cycle, uint settlingRead, string status, bool extendLimit, bool retractLimit, TimeSpan elapsedTime, string device1Measurement = "", long device1Timestamp = 0, string device2Measurement = "", long device2Timestamp = 0, string device3Measurement = "", long device3Timestamp = 0, string device4Measurement = "", long device4Timestamp = 0)
        {
            Cycle = cycle;
            SettlingRead = settlingRead;
            Status = status;
            ExtendLimit = extendLimit;
            RetractLimit = retractLimit;
            ElapsedTime = elapsedTime;
            Device1Measurement = device1Measurement;
            Device2Measurement = device2Measurement;
            Device3Measurement = device3Measurement;
            Device4Measurement = device4Measurement;
            Device1Timestamp = device1Timestamp;
            Device2Timestamp = device2Timestamp;
            Device3Timestamp = device3Timestamp;
            Device4Timestamp = device4Timestamp;

        }
    }
}
