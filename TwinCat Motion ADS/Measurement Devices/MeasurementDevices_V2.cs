using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace TwinCat_Motion_ADS
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class MeasurementDevices_V2
    {
        public List<I_MeasurementDevice> MeasurementDeviceList { get; set; }

        public MeasurementDevices_V2()
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

        public void AddDevice(DeviceTypes dType = DeviceTypes.NoneSelected)
        {
            I_MeasurementDevice newDevice;
            switch (dType)
            {
                case DeviceTypes.DigimaticIndicator:    //Single channel device              
                    newDevice = new DigimaticIndicator_V2();
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.KeyenceTM3000: //Multi Channel device
                    newDevice = new KeyenceTM3000_V2();
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.Beckhoff:   //Multi-channel device                    
                    
                    break;

                case DeviceTypes.MotionChannel:  //Single channel device
                    newDevice = new MotionControllerChannel_V2();
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.Timestamp: //Single channel device
                    newDevice = new TimestampDevice_V2();
                    MeasurementDeviceList.Add(newDevice);
                    break;
                case DeviceTypes.NoneSelected:
                    newDevice = new NoneSelectedMeasurementDevice();
                    MeasurementDeviceList.Add(newDevice);
                    break;

            }          
        }
        public void ChangeDeviceType(int i, DeviceTypes dType)
        {
            string tempName = MeasurementDeviceList[i].Name;
            switch (dType)
            {
                case DeviceTypes.DigimaticIndicator:    //Single channel device                             
                    MeasurementDeviceList[i] = new DigimaticIndicator_V2();
                    break;

                case DeviceTypes.KeyenceTM3000: //Multi Channel device
                    MeasurementDeviceList[i] = new KeyenceTM3000_V2();
                    break;

                case DeviceTypes.Beckhoff:   //Multi-channel device                    

                    break;

                case DeviceTypes.MotionChannel:  //Single channel device
                    MeasurementDeviceList[i] = new MotionControllerChannel_V2();
                    break;

                case DeviceTypes.Timestamp: //Single channel device
                    MeasurementDeviceList[i] = new TimestampDevice_V2();
                    break;
                case DeviceTypes.NoneSelected:
                    MeasurementDeviceList[i] = new NoneSelectedMeasurementDevice();
                    break;
            }
            MeasurementDeviceList[i].Name = tempName;

            //THis will need to be a case statement
            
            
        }


        public void RemoveDevice(int i)
        {
            if(i> NumberOfDevices)
            {
                return;
            }
            if (MeasurementDeviceList[i].Connected)
            {
                MeasurementDeviceList[i].Disconnect();
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
                    measurementString += "Device " + i + ":" + MeasurementDeviceList[i].DeviceType.GetStringValue() + ": " + MeasurementDeviceList[i].GetMeasurement() + " ";
                }
            }
            return measurementString;
        }

        public int ImportDevicesXML(string filepath)
        {
            /*
            //string filepath = @"C:\Users\SCoop - Work\Documents\Ads Tests\motionchannelTest\testxml.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filepath);
            XmlNodeList devices = xmlDoc.SelectNodes("MeasurementDevices/MeasurementDevice");

            int deviceCounter = MeasurementDeviceList.Count;
            foreach(XmlNode device in devices)  //for every device in the measurement devices
            {
                string DeviceTypeXml = device.SelectSingleNode("DeviceType").InnerText;
                DeviceTypes importedType;
                Enum.TryParse(DeviceTypeXml, out importedType);
                AddDevice(importedType);
                MeasurementDeviceList[deviceCounter].Name = device.SelectSingleNode("Name").InnerText;
                
                switch(importedType)
                {
                    case DeviceTypes.DigimaticIndicator:
                        Console.WriteLine("Importing DigimaticIndicator");
                        //Comm port
                        MeasurementDeviceList[deviceCounter].PortName = device.SelectSingleNode("Port").InnerText;
                        //Baud Rate
                        MeasurementDeviceList[deviceCounter].UpdateBaudRate(device.SelectSingleNode("BaudRate").InnerText);
                        break;
                    case DeviceTypes.KeyenceTM3000:
                        Console.WriteLine("Importing KeyenceTm3000");
                        //Comm port
                        MeasurementDeviceList[deviceCounter].PortName = device.SelectSingleNode("Port").InnerText;
                        //Baud Rate
                        MeasurementDeviceList[deviceCounter].UpdateBaudRate(device.SelectSingleNode("BaudRate").InnerText);

                        //Not to setup channel names and connections
                        XmlNodeList chSettings = device.SelectNodes("Channel");
                        Console.WriteLine("Keyence channels : "+chSettings.Count);

                        int i = 0; //channel counter
                        foreach(XmlNode ch in chSettings)
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
                    case DeviceTypes.Beckhoff:
                        Console.WriteLine("Importing Beckhoff");
                        //AMS Net ID
                        MeasurementDeviceList[deviceCounter].AmsNetId = device.SelectSingleNode("AmsNetID").InnerText;

                        //Channle Types
                        XmlNodeList digChannels = device.SelectNodes("DigChannel");
                        XmlNodeList pt100Channels = device.SelectNodes("PT100Channel");                      
                        int chCounter = 0;                       
                        foreach(XmlNode ch in digChannels)
                        {
                            if(chCounter>=MeasurementDeviceList[deviceCounter].beckhoff.DIGITAL_INPUT_CHANNELS)
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
                    case DeviceTypes.MotionChannel:
                        Console.WriteLine("Importing Motion Channel");
                        MeasurementDeviceList[deviceCounter].motionChannel.VariableType = device.SelectSingleNode("VariableType").InnerText;
                        MeasurementDeviceList[deviceCounter].motionChannel.VariableString = device.SelectSingleNode("VariablePath").InnerText;
                        break;
                    case DeviceTypes.Timestamp:
                        break;
                    default:
                        break;
                }
                MeasurementDeviceList[deviceCounter].ConnectToDevice();
                MeasurementDeviceList[deviceCounter].UpdateChannelList();
                deviceCounter ++;
            }
            return devices.Count;
            */
            return 0;
        }

        public void ExportDeviceesXml(string selectedFile)
        {
            /*
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("MeasurementDevices");
            xmlDoc.AppendChild(rootNode);   //Root of the xml

            //Now create settings for each device
            foreach(MeasurementDevice md in MeasurementDeviceList)
            {
                XmlNode deviceNode = xmlDoc.CreateElement("MeasurementDevice");
                XmlNode nameNode = xmlDoc.CreateElement("Name");
                XmlNode deviceTypeNode = xmlDoc.CreateElement("DeviceType");
                
                //Common setup to all types
                nameNode.InnerText = md.Name;
                deviceTypeNode.InnerText = md.DeviceType.GetStringValue();
                deviceNode.AppendChild(nameNode);
                deviceNode.AppendChild(deviceTypeNode);
                rootNode.AppendChild(deviceNode);

                //Unique settings
                switch(md.DeviceType)
                {
                    case DeviceTypes.DigimaticIndicator:
                        XmlNode commNode = xmlDoc.CreateElement("Port");
                        commNode.InnerText = md.PortName;
                        XmlNode baudNode = xmlDoc.CreateElement("BaudRate");
                        baudNode.InnerText = md.BaudRate;
                        deviceNode.AppendChild(commNode);
                        deviceNode.AppendChild(baudNode);
                        break;
                    case DeviceTypes.KeyenceTM3000:
                        commNode = xmlDoc.CreateElement("Port");
                        commNode.InnerText = md.PortName;
                        baudNode = xmlDoc.CreateElement("BaudRate");
                        baudNode.InnerText = md.BaudRate;
                        deviceNode.AppendChild(commNode);
                        deviceNode.AppendChild(baudNode);

                        for (int i = 0; i<md.keyence.KEYENCE_MAX_CHANNELS;i++)
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
                    case DeviceTypes.Beckhoff:
                        XmlNode amsNode = xmlDoc.CreateElement("AmsNetID");
                        amsNode.InnerText = md.AmsNetId;
                        deviceNode.AppendChild(amsNode);

                        //Create digital channles
                        for(int i=0;i<md.beckhoff.DIGITAL_INPUT_CHANNELS;i++)
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
                    case DeviceTypes.MotionChannel:
                        XmlNode varTypeNode = xmlDoc.CreateElement("VariableType");
                        XmlNode varPathNode = xmlDoc.CreateElement("VariablePath");
                        varTypeNode.InnerText = md.motionChannel.VariableType;
                        varPathNode.InnerText = md.motionChannel.VariableString;
                        deviceNode.AppendChild(varTypeNode);
                        deviceNode.AppendChild(varPathNode);
                        break;
                    case DeviceTypes.Timestamp:
                        //No settings to export
                        break;
                }
            }
            xmlDoc.Save(selectedFile);
            */
        }
    }
}
