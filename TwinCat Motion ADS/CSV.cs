using System;
using CsvHelper.Configuration.Attributes;

namespace TwinCat_Motion_ADS
{
    public class end2endReversalCSV
    {
        [Name("Cycle")]
        public int Cycle { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("ElapsedTime")]
        public long ElapsedTime { get; set; }
        [Name("LimitPosition")]
        public double LimitPosition { get; set; }
        [Name("Dti1Position")]
        public string Dti1Position { get; set; }
        [Name("Dti2Position")]
        public string Dti2Position { get; set; }

        public end2endReversalCSV(int cycle, string status, long elapsedTime, double limitPosition, string dti1Position = "", string dti2Position = "")
        {
            Cycle = cycle;
            Status = status;
            ElapsedTime = elapsedTime;
            LimitPosition = limitPosition;
            Dti1Position = dti1Position;
            Dti2Position = dti2Position;
        }
    }
    public class uniDirectionalAccuracyCSV
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
        [Name("Dti1Position")]
        public string Dti1Position { get; set; }
        [Name("Dti2Position")]
        public string Dti2Position { get; set; }

        public uniDirectionalAccuracyCSV(uint cycle, uint step, string status, double targetPosition, double encoderPosition, string dti1Position = "", string dti2Position = "")
        {
            Cycle = cycle;
            Step = step;
            Status = status;
            TargetPosition = targetPosition;
            EncoderPosition = encoderPosition;
            Dti1Position = dti1Position;
            Dti2Position = dti2Position;
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
        [Name("Dti1Position")]
        public string Dti1Position { get; set; }
        [Name("Dti1Timestamp")]
        public long Dti1Timestamp { get; set; }
        [Name("Dti2Position")]
        public string Dti2Position { get; set; }
        [Name("Dti2Timestamp")]
        public long Dti2Timestamp { get; set; }


        public PneumaticEnd2EndCSV(uint cycle, uint settlingRead, string status, bool extendLimit, bool retractLimit, TimeSpan elapsedTime, long dti1Timestamp, long dti2Timestamp, string dti1Position = "", string dti2Position = "")
        {
            Cycle = cycle;
            SettlingRead = settlingRead;
            Status = status;
            ExtendLimit = extendLimit;
            RetractLimit = retractLimit;
            ElapsedTime = elapsedTime;
            Dti1Timestamp = dti1Timestamp;
            Dti2Timestamp = dti2Timestamp;
            Dti1Position = dti1Position;
            Dti2Position = dti2Position;
        }
    }
}
