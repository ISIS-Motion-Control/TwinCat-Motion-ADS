using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Xml;

namespace TwinCat_Motion_ADS
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class MeasurementDevices
    {//TwinCat_Motion_ADS.MeasurementDevices
        MainWindow windowData;
        public List<MeasurementDevice> MeasurementDeviceList { get; set; }

        public MeasurementDevices()
        {
            windowData = (MainWindow)Application.Current.MainWindow;
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
            if (i > NumberOfDevices)
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
            for (int i = 0; i < MeasurementDeviceList.Count; i++)
            {
                if (MeasurementDeviceList[i].Connected)
                {
                    measurementString += "Device " + i + ":" + MeasurementDeviceList[i].DeviceTypeString + ": " + MeasurementDeviceList[i].GetMeasurement() + " ";
                }
            }
            return measurementString;
        }

        public int ImportDevicesXML(string filepath)
        {
            //string filepath = @"C:\Users\SCoop - Work\Documents\Ads Tests\motionchannelTest\testxml.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filepath);
            XmlNodeList devices = xmlDoc.SelectNodes("MeasurementDevices/MeasurementDevice");

            int deviceCounter = MeasurementDeviceList.Count;
            foreach (XmlNode device in devices)  //for every device in the measurement devices
            {

                string DeviceType = device.SelectSingleNode("DeviceType").InnerText;
                AddDevice(DeviceType);
                MeasurementDeviceList[deviceCounter].Name = device.SelectSingleNode("Name").InnerText;

                switch (DeviceType)
                {
                    case "DigimaticIndicator":
                        Console.WriteLine("Importing DigimaticIndicator");
                        //Comm port
                        MeasurementDeviceList[deviceCounter].PortName = device.SelectSingleNode("Port").InnerText;
                        //Baud Rate
                        MeasurementDeviceList[deviceCounter].UpdateBaudRate(device.SelectSingleNode("BaudRate").InnerText);
                        break;

                    case "KeyenceTM3000":
                        Console.WriteLine("Importing KeyenceTm3000");
                        //Comm port
                        MeasurementDeviceList[deviceCounter].PortName = device.SelectSingleNode("Port").InnerText;
                        //Baud Rate
                        MeasurementDeviceList[deviceCounter].UpdateBaudRate(device.SelectSingleNode("BaudRate").InnerText);

                        //Not to setup channel names and connections
                        XmlNodeList chSettings = device.SelectNodes("Channel");
                        Console.WriteLine("Keyence channels : " + chSettings.Count);

                        int i = 0; //channel counter
                        foreach (XmlNode ch in chSettings)
                        {
                            if (i >= MeasurementDeviceList[deviceCounter].keyence.KEYENCE_MAX_CHANNELS)
                            {
                                break;
                            }
                            MeasurementDeviceList[deviceCounter].keyence.ChName[i] = ch.SelectSingleNode("Name").InnerText;
                            if (ch.SelectSingleNode("Connected").InnerText == "True")
                            {
                                MeasurementDeviceList[deviceCounter].keyence.ChConnected[i] = true;
                            }
                            else
                            {
                                MeasurementDeviceList[deviceCounter].keyence.ChConnected[i] = false;
                            }
                            i++;
                        }
                        break;

                    case "Beckhoff":
                        Console.WriteLine("Importing Beckhoff");
                        //AMS Net ID
                        MeasurementDeviceList[deviceCounter].AmsNetId = device.SelectSingleNode("AmsNetID").InnerText;

                        //Channle Types
                        XmlNodeList digChannels = device.SelectNodes("DigChannel");
                        XmlNodeList pt100Channels = device.SelectNodes("PT100Channel");

                        int chCounter = 0;
                        foreach (XmlNode ch in digChannels)
                        {
                            if (chCounter >= MeasurementDeviceList[deviceCounter].beckhoff.DIGITAL_INPUT_CHANNELS)
                            {
                                break;
                            }
                            if (ch.SelectSingleNode("Connected").InnerText == "True")
                            {
                                MeasurementDeviceList[deviceCounter].beckhoff.DigitalInputConnected[chCounter] = true;
                            }
                            else
                            {
                                MeasurementDeviceList[deviceCounter].beckhoff.DigitalInputConnected[chCounter] = false;
                            }
                            chCounter++;
                        }
                        chCounter = 0;
                        foreach (XmlNode ch in pt100Channels)
                        {
                            if (chCounter >= MeasurementDeviceList[deviceCounter].beckhoff.PT100_CHANNELS)
                            {
                                break;
                            }
                            if (ch.SelectSingleNode("Connected").InnerText == "True")
                            {
                                MeasurementDeviceList[deviceCounter].beckhoff.PT100Connected[chCounter] = true;
                            }
                            else
                            {
                                MeasurementDeviceList[deviceCounter].beckhoff.PT100Connected[chCounter] = false;
                            }
                            chCounter++;
                        }
                        break;

                    case "MotionChannel":
                        Console.WriteLine("Importing Motion Channel");
                        MeasurementDeviceList[deviceCounter].motionChannel.VariableType = device.SelectSingleNode("VariableType").InnerText;
                        MeasurementDeviceList[deviceCounter].motionChannel.VariableString = device.SelectSingleNode("VariablePath").InnerText;
                        break;

                    case "Timestamp":
                        break;

                    default:
                        break;
                }
                MeasurementDeviceList[deviceCounter].ConnectToDevice();
                MeasurementDeviceList[deviceCounter].UpdateChannelList();

                deviceCounter++;
            }
            return devices.Count;

        }

        public void ExportDevicesXml(string selectedFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("MeasurementDevices");
            xmlDoc.AppendChild(rootNode);   //Root of the xml

            //Now create settings for each device

            foreach (MeasurementDevice md in MeasurementDeviceList)
            {
                XmlNode deviceNode = xmlDoc.CreateElement("MeasurementDevice");
                XmlNode nameNode = xmlDoc.CreateElement("Name");
                XmlNode deviceTypeNode = xmlDoc.CreateElement("DeviceType");

                //Common setup to all types
                nameNode.InnerText = md.Name;
                deviceTypeNode.InnerText = md.DeviceTypeString;
                deviceNode.AppendChild(nameNode);
                deviceNode.AppendChild(deviceTypeNode);
                rootNode.AppendChild(deviceNode);

                //Unique settings
                switch (md.DeviceTypeString)
                {
                    case "DigimaticIndicator":
                        AddCommNodes(xmlDoc, md, deviceNode);
                        break;

                    case "KeyenceTM3000":
                        AddCommNodes(xmlDoc, md, deviceNode);

                        for (int i = 0; i < md.keyence.KEYENCE_MAX_CHANNELS; i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("Channel");
                            XmlNode channelName = xmlDoc.CreateElement("Name");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");

                            channelName.InnerText = md.keyence.ChName[i];
                            channelConnection.InnerText = md.keyence.ChConnected[i].ToString();
                            channelNode.AppendChild(channelName);
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        break;

                    case "Beckhoff":
                        XmlNode amsNode = xmlDoc.CreateElement("AmsNetID");
                        amsNode.InnerText = md.AmsNetId;
                        deviceNode.AppendChild(amsNode);

                        //Create digital channles
                        for (int i = 0; i < md.beckhoff.DIGITAL_INPUT_CHANNELS; i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("DigChannel");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");
                            channelConnection.InnerText = md.beckhoff.DigitalInputConnected[i].ToString();
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        //create pt100 channels
                        for (int i = 0; i < md.beckhoff.PT100_CHANNELS; i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("PT100Channel");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");
                            channelConnection.InnerText = md.beckhoff.PT100Connected[i].ToString();
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        break;

                    case "MotionChannel":
                        XmlNode varTypeNode = xmlDoc.CreateElement("VariableType");
                        XmlNode varPathNode = xmlDoc.CreateElement("VariablePath");
                        varTypeNode.InnerText = md.motionChannel.VariableType;
                        varPathNode.InnerText = md.motionChannel.VariableString;
                        deviceNode.AppendChild(varTypeNode);
                        deviceNode.AppendChild(varPathNode);
                        break;

                    case "Timestamp":
                        //No settings to export
                        break;

                }


            }
            xmlDoc.Save(selectedFile);
        }

        private static void AddCommNodes(XmlDocument xmlDoc, MeasurementDevice md, XmlNode deviceNode)
        {
            XmlElement commNode = xmlDoc.CreateElement("Port");
            commNode.InnerText = md.PortName;
            XmlElement baudNode = xmlDoc.CreateElement("BaudRate");
            baudNode.InnerText = md.BaudRate;
            deviceNode.AppendChild(commNode);
            deviceNode.AppendChild(baudNode);
        }
    }
}
