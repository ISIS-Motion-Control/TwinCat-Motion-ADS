using System;
using CsvHelper.Configuration.Attributes;

namespace TwinCat_Motion_ADS
{
    public class StandardCSVData
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

        public StandardCSVData(uint cycle, uint step, string status, double targetPosition, double encoderPosition)
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
}
