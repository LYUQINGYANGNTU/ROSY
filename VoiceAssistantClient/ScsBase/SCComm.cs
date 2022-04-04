/*
 * SCComm.cs
 * 基于C# COM接口程序接口
 * 日期: 2021.2.4
 * 作者: 
 */
using System.IO.Ports;
using System.IO;
using System;

namespace ScsServoLib.Comm
{
    interface SCComm
    {
        int writeSCS(byte[] nDat, int nLen);
        int writeSCS(byte bDat);
        int readSCS(byte[] nDat, int nLen);
        int readSCS();
        void rFlushSCS();
        void wFlushSCS();
    }
    class SerialCom : SCComm
    {
        public int IOTimeOut = 100;
        private SerialPort _serialPort = new SerialPort();
        public bool Open(string PortName, int BaudRate)
        {
            _serialPort.BaudRate = BaudRate;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.PortName = PortName;
            _serialPort.Parity = Parity.None;
            try{
                _serialPort.Open();
                _serialPort.ReadTimeout = IOTimeOut;
            }catch(IOException ex){
                throw ex;
            }
            return _serialPort.IsOpen;
        }
        public void Close()
        {
            _serialPort.Close();
        }
        public int writeSCS(byte[] nDat, int nLen)
        {
            _serialPort.Write(nDat, 0, nLen);
            return nLen;
        }
        public int writeSCS(byte bDat)
        {
            byte[] nDat = new byte[1];
            nDat[0] = bDat;
            _serialPort.Write(nDat, 0, 1);
            return 1;
        }
        public int readSCS(byte[] nDat, int nLen)
        {
            try{
                return _serialPort.Read(nDat, 0, nLen);
            }catch(TimeoutException){
                return 0;
            }
            //return _serialPort.Read(nDat, 0, nLen);
        }
        public int readSCS()
        {
            try{
                return _serialPort.ReadByte();
            }catch(TimeoutException){
                return -1;
            }
        }
        public void rFlushSCS()
        {
            _serialPort.DiscardInBuffer();
        }
        public void wFlushSCS()
        {
        }
    }
}
