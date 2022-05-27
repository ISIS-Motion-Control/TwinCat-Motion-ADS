using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Xml;

namespace TwinCat_Motion_ADS.MeasurementDevice
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class MeasurementDevices
    {
        public List<I_MeasurementDevice> MeasurementDeviceList { get; set; }

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

        public void AddDevice(DeviceTypes dType = DeviceTypes.NoneSelected)
        {
            I_MeasurementDevice newDevice;
            switch (dType)
            {
                case DeviceTypes.DigimaticIndicator:    //Single channel device              
                    newDevice = new MD_DigimaticIndicator();
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.KeyenceTM3000: //Multi Channel device
                    newDevice = new MD_KeyenceTM3000();
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.Beckhoff:   //Multi-channel device                    
                    newDevice = new MD_Beckhoff();
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.MotionChannel:  //Single channel device
                    newDevice = new MD_MotionControllerChannel(((MainWindow)Application.Current.MainWindow).Plc);
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.Timestamp: //Single channel device
                    newDevice = new MD_TimestampDevice();
                    MeasurementDeviceList.Add(newDevice);
                    break;

                case DeviceTypes.RenishawXL80: //Single channel device
                    newDevice = new MD_RenishawXL80();
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
                    MeasurementDeviceList[i] = new MD_DigimaticIndicator();
                    break;

                case DeviceTypes.KeyenceTM3000: //Multi Channel device
                    MeasurementDeviceList[i] = new MD_KeyenceTM3000();
                    break;

                case DeviceTypes.Beckhoff:   //Multi-channel device                    
                    MeasurementDeviceList[i] = new MD_Beckhoff();
                    break;

                case DeviceTypes.MotionChannel:  //Single channel device
                    MeasurementDeviceList[i] = new MD_MotionControllerChannel(((MainWindow)Application.Current.MainWindow).Plc);
                    break;

                case DeviceTypes.Timestamp: //Single channel device
                    MeasurementDeviceList[i] = new MD_TimestampDevice();
                    break;

                case DeviceTypes.RenishawXL80:
                    MeasurementDeviceList[i] = new MD_RenishawXL80();
                    break;

                case DeviceTypes.NoneSelected:
                    MeasurementDeviceList[i] = new NoneSelectedMeasurementDevice();
                    break;
            }
            MeasurementDeviceList[i].Name = tempName;
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
                        ((BaseRs232MeasurementDevice)(MeasurementDeviceList[deviceCounter])).PortName = device.SelectSingleNode("Port").InnerText;
                        //Baud Rate
                        ((BaseRs232MeasurementDevice)(MeasurementDeviceList[deviceCounter])).UpdateBaudRate(device.SelectSingleNode("BaudRate").InnerText);
                        break;
                    case DeviceTypes.KeyenceTM3000:
                        Console.WriteLine("Importing KeyenceTm3000");
                        //Comm port
                        ((BaseRs232MeasurementDevice)(MeasurementDeviceList[deviceCounter])).PortName = device.SelectSingleNode("Port").InnerText;
                        //Baud Rate
                        ((BaseRs232MeasurementDevice)(MeasurementDeviceList[deviceCounter])).UpdateBaudRate(device.SelectSingleNode("BaudRate").InnerText);

                        //Not to setup channel names and connections
                        XmlNodeList chSettings = device.SelectNodes("Channel");
                        Console.WriteLine("Keyence channels : "+chSettings.Count);

                        int i = 0; //channel counter
                        foreach(XmlNode ch in chSettings)
                        {
                            if (i >= ((MD_KeyenceTM3000)(MeasurementDeviceList[deviceCounter])).KEYENCE_MAX_CHANNELS)
                            {
                                break;
                            }
                            ((MD_KeyenceTM3000)(MeasurementDeviceList[deviceCounter])).ChName[i] = ch.SelectSingleNode("Name").InnerText;
                            if (ch.SelectSingleNode("Connected").InnerText == "True")
                            {
                                ((MD_KeyenceTM3000)(MeasurementDeviceList[deviceCounter])).ChConnected[i] = true;
                            }
                            else
                            {
                                ((MD_KeyenceTM3000)(MeasurementDeviceList[deviceCounter])).ChConnected[i] = false;
                            }
                            i++;
                        }
                        break;
                    case DeviceTypes.Beckhoff:
                        Console.WriteLine("Importing Beckhoff");
                        //AMS Net ID
                        ((MD_Beckhoff)(MeasurementDeviceList[deviceCounter])).Plc.ID = device.SelectSingleNode("AmsNetID").InnerText;

                        //Channle Types
                        XmlNodeList digChannels = device.SelectNodes("DigChannel");
                        XmlNodeList pt100Channels = device.SelectNodes("PT100Channel");                      
                        int chCounter = 0;                       
                        foreach(XmlNode ch in digChannels)
                        {
                            if(chCounter>= ((MD_Beckhoff)(MeasurementDeviceList[deviceCounter])).DIGITAL_INPUT_CHANNELS)
                            {
                                break;
                            }
                            if (ch.SelectSingleNode("Connected").InnerText == "True")
                            {
                                ((MD_Beckhoff)(MeasurementDeviceList[deviceCounter])).DigitalInputConnected[chCounter] = true;
                            }
                            else
                            {
                                ((MD_Beckhoff)(MeasurementDeviceList[deviceCounter])).DigitalInputConnected[chCounter] = false;
                            }
                            chCounter++;
                        }
                        chCounter = 0;
                        foreach (XmlNode ch in pt100Channels)
                        {
                            if (chCounter >= ((MD_Beckhoff)(MeasurementDeviceList[deviceCounter])).PT100_CHANNELS)
                            {
                                break;
                            }
                            if (ch.SelectSingleNode("Connected").InnerText == "True")
                            {
                                ((MD_Beckhoff)(MeasurementDeviceList[deviceCounter])).PT100Connected[chCounter] = true;
                            }
                            else
                            {
                                ((MD_Beckhoff)(MeasurementDeviceList[deviceCounter])).PT100Connected[chCounter] = false;
                            }
                            chCounter++;
                        }
                        break;
                    case DeviceTypes.MotionChannel:
                        Console.WriteLine("Importing Motion Channel");
                        ((MD_MotionControllerChannel)(MeasurementDeviceList[deviceCounter])).VariableType = device.SelectSingleNode("VariableType").InnerText;
                        ((MD_MotionControllerChannel)(MeasurementDeviceList[deviceCounter])).VariableString = device.SelectSingleNode("VariablePath").InnerText;
                        break;
                    case DeviceTypes.Timestamp:
                        Console.WriteLine("Importing Timestamp");
                        break;
                    case DeviceTypes.RenishawXL80:
                        Console.WriteLine("Importing Xl80");
                        break;
                    default:
                        break;
                }
                MeasurementDeviceList[deviceCounter].Connect();
                MeasurementDeviceList[deviceCounter].UpdateChannelList();
                deviceCounter ++;
            }
            return devices.Count;            
        }

        public void ExportDevicesXml(string selectedFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("MeasurementDevices");
            xmlDoc.AppendChild(rootNode);   //Root of the xml

            //Now create settings for each device
            foreach(I_MeasurementDevice md in MeasurementDeviceList)
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
                        commNode.InnerText = ((MD_DigimaticIndicator)(md)).PortName;
                        XmlNode baudNode = xmlDoc.CreateElement("BaudRate");
                        baudNode.InnerText = ((MD_DigimaticIndicator)(md)).BaudRate;
                        deviceNode.AppendChild(commNode);
                        deviceNode.AppendChild(baudNode);
                        break;

                    case DeviceTypes.KeyenceTM3000:
                        commNode = xmlDoc.CreateElement("Port");
                        commNode.InnerText = ((MD_KeyenceTM3000)(md)).PortName;
                        baudNode = xmlDoc.CreateElement("BaudRate");
                        baudNode.InnerText = ((MD_KeyenceTM3000)(md)).BaudRate;
                        deviceNode.AppendChild(commNode);
                        deviceNode.AppendChild(baudNode);

                        for (int i = 0; i< ((MD_KeyenceTM3000)(md)).KEYENCE_MAX_CHANNELS;i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("Channel");
                            XmlNode channelName = xmlDoc.CreateElement("Name");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");

                            channelName.InnerText = ((MD_KeyenceTM3000)(md)).ChName[i];
                            channelConnection.InnerText = ((MD_KeyenceTM3000)(md)).ChConnected[i].ToString();
                            channelNode.AppendChild(channelName);
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        break;

                    case DeviceTypes.Beckhoff:
                        XmlNode amsNode = xmlDoc.CreateElement("AmsNetID");
                        amsNode.InnerText = ((MD_Beckhoff)(md)).Plc.ID;
                        deviceNode.AppendChild(amsNode);

                        //Create digital channles
                        for(int i=0;i< ((MD_Beckhoff)(md)).DIGITAL_INPUT_CHANNELS;i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("DigChannel");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");
                            channelConnection.InnerText = ((MD_Beckhoff)(md)).DigitalInputConnected[i].ToString();
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        //create pt100 channels
                        for (int i = 0; i < ((MD_Beckhoff)(md)).PT100_CHANNELS; i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("PT100Channel");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");
                            channelConnection.InnerText = ((MD_Beckhoff)(md)).PT100Connected[i].ToString();
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        break;
                    case DeviceTypes.MotionChannel:
                        XmlNode varTypeNode = xmlDoc.CreateElement("VariableType");
                        XmlNode varPathNode = xmlDoc.CreateElement("VariablePath");
                        varTypeNode.InnerText = ((MD_MotionControllerChannel)(md)).VariableType;
                        varPathNode.InnerText = ((MD_MotionControllerChannel)(md)).VariableString;
                        deviceNode.AppendChild(varTypeNode);
                        deviceNode.AppendChild(varPathNode);
                        break;
                    case DeviceTypes.Timestamp:
                        //No settings to export
                        break;
                    case DeviceTypes.RenishawXL80:
                        //No settings to export
                        break;
                }
            }
            xmlDoc.Save(selectedFile);
        }
    }
}
