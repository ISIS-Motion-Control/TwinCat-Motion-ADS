using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace TwinCat_Motion_ADS
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class MeasurementDevices
    {//TwinCat_Motion_ADS.MeasurementDevices
        public List<MeasurementDevice> MeasurementDeviceList { get; set; }

        public MeasurementDevices()
        {
            MeasurementDeviceList = new();
        }

        public int NumberOfDevices
        {
            get { return MeasurementDeviceList.Count; }
        }


        public void ClearList()
        {
            MeasurementDeviceList.Clear();
        }

        public void AddDevice(string deviceType)
        {
            MeasurementDevice newDevice = new MeasurementDevice(deviceType);
            MeasurementDeviceList.Add(newDevice);
        }

        public void RemoveDevice(int i)
        {
            if(i> NumberOfDevices)
            {
                return;
            }
            if (MeasurementDeviceList[i].Connected)
            {
                MeasurementDeviceList[i].DisconnectFromDevice();
            }
            MeasurementDeviceList.Remove(MeasurementDeviceList[i]);
            return;
        }

        public string GetMeasurements()
        {
            string measurementString = "";
            for(int i=0; i<MeasurementDeviceList.Count; i++)
            {
                if(MeasurementDeviceList[i].Connected)
                {
                    measurementString += "Device " + i + ":" + MeasurementDeviceList[i].DeviceTypeString + ": " + MeasurementDeviceList[i].GetMeasurement() + " ";
                }
            }
            return measurementString;
        }       
    }
}
