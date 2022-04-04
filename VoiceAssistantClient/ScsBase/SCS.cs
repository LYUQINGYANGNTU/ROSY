/*
 * SCS.cs
 * 飞特串行舵机通信层协议程序
 * 日期: 2021.2.4
 * 作者: 
 */
//using System;
using ScsServoLib.Def;
using ScsServoLib.Comm;

namespace ScsServoLib.Scs
{
    class SCS
    {
        public SCS(SCComm Comm)
        {
            this.Comm = Comm;
        }
        private SCComm Comm;
        public byte Level = 1;//舵机返回等级
        public byte End = 0;//处理器大小端结构
        public byte Error = 0;//舵机状态
        public void Host2SCS(byte[] bBuf, int offset, ushort Data)
        {
	        if(End==1){
                bBuf[offset] = (byte)(Data >> 8);
                bBuf[offset+1] = (byte)(Data & 0xff);
	        }else{
                bBuf[offset+1] = (byte)(Data >> 8);
                bBuf[offset] = (byte)(Data & 0xff);
	        }
        }
        public ushort SCS2Host(byte DataL, byte DataH)
        {
            ushort Data;
	        if(End==1){
		        Data = DataL;
		        Data<<=8;
		        Data |= DataH;
	        }else{
		        Data = DataH;
		        Data<<=8;
		        Data |= DataL;
	        }
	        return Data;
        }
        protected void writeBuf(byte ID, byte MemAddr, byte[] nDat, byte nLen, Inst Fun)
        {
	        int msgLen = 2;
	        byte[] bBuf = new byte[6];
	        int CheckSum = 0;
	        bBuf[0] = 0xff;
	        bBuf[1] = 0xff;
	        bBuf[2] = ID;
	        bBuf[4] = (byte)Fun;
            if(nLen!=0){
		        msgLen += nLen + 1;
		        bBuf[3] = (byte)msgLen;
		        bBuf[5] = MemAddr;
                Comm.writeSCS(bBuf, 6);
        		
	        }else{
                bBuf[3] = (byte)msgLen;
                Comm.writeSCS(bBuf, 5);
	        }
	        CheckSum = (int)ID + msgLen + (int)Fun + (int)MemAddr;
            if(nLen != 0){
		        for(int i=0; i<nLen; i++){
			        CheckSum += nDat[i];
		        }
                Comm.writeSCS(nDat, nLen);
	        }
            Comm.writeSCS((byte)~CheckSum);
        }
        protected int Ack(byte ID)
        {
	        Error = 0;
	        if(ID!=0xfe && Level==1){
		        if(checkHead()==0){
			        return 0;
		        }
		        byte[] bBuf = new byte[4];
                if(Comm.readSCS(bBuf, 4)!= 4){
			        return 0;
		        }
		        if(bBuf[0]!=ID){
			        return 0;
		        }
		        if(bBuf[1]!=2){
			        return 0;
		        }
		        byte calSum = (byte)~(bBuf[0]+bBuf[1]+bBuf[2]);
		        if(calSum!=bBuf[3]){
			        return 0;			
		        }
		        Error = bBuf[2];
	        }
	        return 1;
        }
        protected int checkHead()
        {
	        int bDat = 0;
	        byte[] bBuf = new byte[2];
            bBuf[0] = 0;
            bBuf[1] = 0;
            int Cnt = 0;
	        while(true){
                bDat = Comm.readSCS();
                if(bDat == -1){
			        return 0;
		        }
		        bBuf[1] = bBuf[0];
		        bBuf[0] = (byte)bDat;
		        if(bBuf[0]==0xff && bBuf[1]==0xff){
			        break;
		        }
		        Cnt++;
		        if(Cnt>10){
			        return 0;
		        }
	        }
	        return 1;
        }
        //普通写指令
        //舵机ID，MemAddr内存表地址，写入数据，写入长度
        public int genWrite(byte ID, byte MemAddr, byte[] nDat, byte nLen)
        {
            Comm.rFlushSCS();
            writeBuf(ID, MemAddr, nDat, nLen, Inst.INST_WRITE);
            Comm.wFlushSCS();
            return Ack(ID);
        }
        //异步写指令
        //舵机ID，MemAddr内存表地址，写入数据，写入长度
        public int regWrite(byte ID, byte MemAddr, byte[] nDat, byte nLen)
        {
	        Comm.rFlushSCS();
            writeBuf(ID, MemAddr, nDat, nLen, Inst.INST_REG_WRITE);
	        Comm.wFlushSCS();
	        return Ack(ID);
        }
        //异步写执行指令
        //舵机ID
        public int RegWriteAction(byte ID)
        {
	        Comm.rFlushSCS();
            writeBuf(ID, 0, null, 0, Inst.INST_REG_ACTION);
	        Comm.wFlushSCS();
	        return Ack(ID);
        }
        //同步写指令
        //舵机ID[]数组，IDN数组长度，MemAddr内存表地址，写入数据，写入长度
        public void snycWrite(byte[] ID, byte IDN, byte MemAddr, byte[][] nDat, byte nLen)
        {
	        Comm.rFlushSCS();
	        int mesLen = ((nLen+1)*IDN+4);
	        int Sum = 0;
	        byte[] bBuf = new byte[7];
	        bBuf[0] = 0xff;
	        bBuf[1] = 0xff;
	        bBuf[2] = 0xfe;
            bBuf[3] = (byte)mesLen;
            bBuf[4] = (byte)Inst.INST_SYNC_WRITE;
	        bBuf[5] = MemAddr;
	        bBuf[6] = nLen;
            Comm.writeSCS(bBuf, 7);

            Sum = 0xfe + mesLen + (byte)Inst.INST_SYNC_WRITE + MemAddr + nLen;
	        int i, j;
	        for(i=0; i<IDN; i++){
                Comm.writeSCS(ID[i]);
                Comm.writeSCS(nDat[i], nLen);
		        Sum += ID[i];
		        for(j=0; j<nLen; j++){
			        Sum += nDat[i][j];
		        }
	        }
            Comm.writeSCS((byte)~Sum);
	        Comm.wFlushSCS();
        }
        public int writeByte(byte ID, byte MemAddr, byte bDat)
        {
            byte[] bBuf = new byte[1];
            bBuf[0] = bDat;
	        Comm.rFlushSCS();
	        writeBuf(ID, MemAddr, bBuf, 1, Inst.INST_WRITE);
	        Comm.wFlushSCS();
	        return Ack(ID);
        }

        public int writeWord(byte ID, byte MemAddr, ushort wDat)
        {
	        byte[] bBuf = new byte[2];
	        Host2SCS(bBuf, 0, wDat);
	        Comm.rFlushSCS();
	        writeBuf(ID, MemAddr, bBuf, 2, Inst.INST_WRITE);
	        Comm.wFlushSCS();
	        return Ack(ID);
        }
        //读指令
        //舵机ID，MemAddr内存表地址，返回数据nData，数据长度nLen
        public int Read(byte ID, byte MemAddr, byte[] nData, byte nLen)
        {
            byte[] bBuf = new byte[3];
            byte[] bBuf3 = new byte[1];
	        Comm.rFlushSCS();
            bBuf[0] = nLen;
            writeBuf(ID, MemAddr, bBuf, 1, Inst.INST_READ);
	        Comm.wFlushSCS();
	        if(checkHead()==0){
		        return 0;
	        }
	        Error = 0;
            if(Comm.readSCS(bBuf, 3)!=3){
		        return 0;
	        }
            int Size = Comm.readSCS(nData, nLen);
	        if(Size!=nLen){
		        return 0;
	        }
            if(Comm.readSCS(bBuf3, 1)!=1){
		        return 0;
	        }
	        int calSum = bBuf[0]+bBuf[1]+bBuf[2];
	        for(int i=0; i<Size; i++){
		        calSum += nData[i];
	        }
	        calSum = ~calSum;
            if((byte)calSum != bBuf3[0]){
		        return 0;
	        }
	        Error = bBuf[2];
	        return Size;
        }
        //读1字节，超时返回-1
        public int readByte(byte ID, byte MemAddr)
        {
            byte[] bDat = new byte[1];
	        int Size = Read(ID, MemAddr, bDat, 1);
	        if(Size!=1){
		        return -1;
	        }else{
		        return bDat[0];
	        }
        }
        //读2字节，超时返回-1
        public int readWord(byte ID, byte MemAddr)
        {	
	        byte[] nDat = new byte[2];
	        int Size;
	        ushort wDat;
	        Size = Read(ID, MemAddr, nDat, 2);
	        if(Size!=2)
		        return -1;
	        wDat = SCS2Host(nDat[0], nDat[1]);
	        return wDat;
        }
        //Ping指令，返回舵机ID，超时返回-1
        public int Ping(byte ID)
        {
            Comm.rFlushSCS();
            writeBuf(ID, 0, null, 0, Inst.INST_PING);
            Comm.wFlushSCS();
            Error = 0;
            if(checkHead() == 0){
                return -1;
            }
            byte[] bBuf = new byte[4];
            if(Comm.readSCS(bBuf, 4)!=4){
                return -1;
            }
            if(bBuf[0]!=ID && ID!=0xfe){
                return -1;
            }
            if(bBuf[1] != 2){
                return -1;
            }
            int calSum = ~(bBuf[0] + bBuf[1] + bBuf[2]);
            if((byte)calSum != bBuf[3]){
                return -1;
            }
            Error = bBuf[2];
            return bBuf[0];
        }
    }
}
