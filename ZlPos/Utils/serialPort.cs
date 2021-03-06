﻿using log4net;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ZlPos.Utils
{
    public class serialPort
    {
        private static ILog logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string brand { get; set; }
        public string port { get; set; }
        public string rate { get; set; }
        public string pageWidth { get; set; }

        private SerialPort m_SerialPort;

        public bool Enable { get => enable; set => enable = value; }

        private bool enable = false;


        public serialPort(String port, String rate)
        {
            this.port = port;
            this.rate = rate;
        }

        public void init()
        {
            if (enable == false)
            {
                m_SerialPort = new SerialPort();
                m_SerialPort.BaudRate = int.Parse(rate);
                m_SerialPort.PortName = port;
                Enable = true;
            }
        }

        public void Close()
        {
            if (m_SerialPort != null)
            {
                m_SerialPort.Close();
            }
        }

        public void PrintString(string txt)
        {
            if (m_SerialPort.IsOpen)
            {
                byte[] strByte = Encoding.Default.GetBytes(txt);
                m_SerialPort.Write(strByte,0,strByte.Length);
            }
        }

        public void PrintQRCode(string txt)
        {
            if (m_SerialPort.IsOpen)
            {
                string hex = StringUtils.StringToHex16String(txt);
                string str = "1D5A021B5A034C06" + String.Format("{0:X}", hex.Length / 2) + "00" + hex;
                byte[] strByte = StringUtils.HexToByte(str);
                m_SerialPort.Write(strByte, 0, strByte.Length);
            }
        }

        public void CustomerWrite(string s)
        {
            m_SerialPort.Open();
            byte[] buffer = StringUtils.HexToByte(s);//strToToHexByte(s as string);
            m_SerialPort.Write(buffer, 0, buffer.Length);
            m_SerialPort.Close();
        }

        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }


        public void OpenCash(string str)
        {
            if (m_SerialPort.IsOpen)
            {
                byte[] strByte = HexUtils.HexStringToByte(str);
                m_SerialPort.Write(strByte, 0, strByte.Length);
            }
        }

        public bool Open(string port, int rate)
        {
            try
            {
                logger.Info("serialPort.cs -> Open -> Enable = " + Enable);
                if (Enable)
                {
                    m_SerialPort.PortName = port;
                    m_SerialPort.BaudRate = rate;
                    m_SerialPort.Open();
                    logger.Info("串口打开成功");
                    return true;
                }
            }
            catch (Exception e)
            {
            }
            return false;
        }

        //private static Dictionary<string, SerialReader> srs = new Dictionary<String, SerialReader>();

        //public void init()
        //{
        //    //		exeShell("chmod 666 /dev");
        //    //		exeShell("chmod 666 /dev/ttyUSB1");
        //}

        //private SerialReader getSerialReader()
        //{
        //    SerialReader sr = srs[this.port];
        //    if (sr == null)
        //    {
        //        sr = new SerialReader(this.port, this.rate);
        //        srs.Add(this.port, sr);
        //    }
        //    return sr;
        //}



        //public static int CMD = 0;
        //public int STRING = 1;


        //public bool Enable()
        //{
        //    return getSerialReader().Enable(this.rate);
        //}

        //public void Close()
        //{
        //    getSerialReader().close();
        //}

        //public int Write(String str, int flag)
        //{
        //    int res = 0;
        //    getSerialReader().openSerialPort(str, String.valueOf(flag));
        //    return res;
        //}

        //public int Write(String str)
        //{
        //    int res = 0;
        //    getSerialReader().openSerialPort(str, rate);
        //    return res;
        //}

        //public String getCardNum()
        //{
        //    byte[] buffer = new byte[16];
        //    RecvByteUart(fd, buffer, 16);
        //    return MyString.bytesToHexString(buffer).trim();
        //}

        ///**
        // * 认证，在读取卡号完成后，完成认证操作
        // * 
        // * @return 对扇区进行认证，如果密钥匹配，则返回值为 00
        // */
        //public String authentication()
        //{
        //    Write("400800080000ffffffffffff00000D", CMD);
        //    byte[] buffer = new byte[16];
        //    RecvByteUart(fd, buffer, 16);
        //    return MyString.bytesToHexString(buffer).substring(10, 14);
        //}

        ///**
        // * 接收串口数据
        // * 
        // * @param hSerial
        // *            OpenUart成功时，返回的设备句柄
        // * @param buffer
        // *            接收缓冲区
        // * @param size
        // *            接收缓冲区长度(请保证缓冲区大小足够大)
        // * @return 大于0，表示成功，否则失败
        // */
        //public int RecvByteUart(int hSerial, byte[] buffer, int size)
        //{

        //    String res = getSerialReader().receiveSerialPortByte(hSerial, size);
        //    if (res.isEmpty() || res.contains("faield"))
        //    {
        //        return -1;
        //    }
        //    byte[] byteTmp = MyString.hexStringToBytes(res);
        //    for (int i = 0; i < res.length() / 2; i++)
        //        buffer[i] = byteTmp[i];

        //    return res.length() / 2;
        //}
    }
}
