/*
 * Servo.cs
 * 飞特串行舵机应用层程序
 * 日期: 2021.2.4
 * 作者: 
 */
using ScsServoLib.Def;
using ScsServoLib.Scs;
using ScsServoLib.Comm;

namespace ScsServoLib.Smsbl
{
    class SMSBL : SCS
    {
        public int Err = 0;
        byte[] Mem = new byte[SMSBLMem._PRESENT_CURRENT_H - SMSBLMem._PRESENT_POSITION_L + 1];
        public SMSBL(SCComm Comm):base(Comm)
        {
            End = 0;
        }
        public int WritePosEx(byte ID, int Position, ushort Speed, byte ACC)
        {
	        if(Position<0){
		        Position = -Position;
		        Position |= (1<<15);
	        }
	        byte[] bBuf = new byte[7];
	        bBuf[0] = ACC;
	        Host2SCS(bBuf, 1, (ushort)Position);
	        Host2SCS(bBuf, 3, 0);
	        Host2SCS(bBuf, 5, Speed);

            return genWrite(ID, (byte)SMSBLMem._ACC, bBuf, 7);
        }
        public int RegWritePosEx(byte ID, int Position, ushort Speed, byte ACC)
        {
	        if(Position<0){
		        Position = -Position;
		        Position |= (1<<15);
	        }
	        byte[] bBuf = new byte[7];
	        bBuf[0] = ACC;
            Host2SCS(bBuf, 1, (ushort)Position);
	        Host2SCS(bBuf, 3, 0);
	        Host2SCS(bBuf, 5, Speed);

            return regWrite(ID, (byte)SMSBLMem._ACC, bBuf, 7);
        }
        public void SyncWritePosEx(byte[] ID, byte IDN, int[] Position, ushort[] Speed, byte[] ACC)
        {
            byte[][] offbuf = new byte[32][];
            for(int i = 0; i<IDN; i++){
		        if(Position[i]<0){
			        Position[i] = -Position[i];
			        Position[i] |= (1<<15);
		        }
                offbuf[i] = new byte[7];
		        ushort V;
		        if(Speed!=null){
			        V = Speed[i];
		        }else{
			        V = 0;
		        }
		        if(ACC!=null){
                    offbuf[i][0] = ACC[i];
		        }else{
                    offbuf[i][0] = 0;
		        }
                Host2SCS(offbuf[i], 1, (ushort)Position[i]);
                Host2SCS(offbuf[i], 3, 0);
                Host2SCS(offbuf[i], 5, V);
            }
            snycWrite(ID, IDN, (byte)SMSBLMem._ACC, offbuf, 7);
        }
        public int WheelMode(byte ID)
        {
            return writeByte(ID, (byte)SMSBLMem._MODE, 1);		
        }
        public int WriteSpe(byte ID, int Speed, byte ACC)
        {
	        if(Speed<0){
		        Speed = -Speed;
		        Speed |= (1<<15);
	        }
	        byte[] bBuf = new byte[2];
	        bBuf[0] = ACC;
            genWrite(ID, (byte)SMSBLMem._ACC, bBuf, 1);
	        Host2SCS(bBuf, 0, (ushort)Speed);

            return genWrite(ID, (byte)SMSBLMem._GOAL_SPEED_L, bBuf, 2);
        }
        public int EnableTorque(byte ID, byte Enable)
        {
            return writeByte(ID, (byte)SMSBLMem._TORQUE_ENABLE, Enable);
        }
        public int unLockEprom(byte ID)
        {
            return writeByte(ID, (byte)SMSBLMem._LOCK, 0);
        }

        public int LockEprom(byte ID)
        {
            return writeByte(ID, (byte)SMSBLMem._LOCK, 1);
        }
        public int CalibrationOfs(byte ID)
        {
            return writeByte(ID, (byte)SMSBLMem._TORQUE_ENABLE, 128);
        }
        public int FeedBack(byte ID)
        {
            int nLen = Read(ID, (byte)SMSBLMem._PRESENT_POSITION_L, Mem, (byte)Mem.Length);
            if(nLen != Mem.Length){
		        Err = 1;
		        return -1;
	        }
	        Err = 0;
	        return nLen;
        }
        public int ReadPos(int ID)
        {
	        int Pos = -1;
	        if(ID==-1){
                Pos = Mem[SMSBLMem._PRESENT_POSITION_H - SMSBLMem._PRESENT_POSITION_L];
		        Pos <<= 8;
                Pos |= Mem[SMSBLMem._PRESENT_POSITION_L - SMSBLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Pos = readWord((byte)ID, (byte)SMSBLMem._PRESENT_POSITION_L);
		        if(Pos==-1){
			        Err = 1;
		        }
	        }
	        if((Err==0) && ((Pos&(1<<15))!=0)){
		        Pos = -(Pos&~(1<<15));
	        }
        	
	        return Pos;
        }
        public int ReadSpeed(int ID)
        {
	        int Speed = -1;
	        if(ID==-1){
                Speed = Mem[SMSBLMem._PRESENT_SPEED_H - SMSBLMem._PRESENT_POSITION_L];
		        Speed <<= 8;
                Speed |= Mem[SMSBLMem._PRESENT_SPEED_L - SMSBLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Speed = readWord((byte)ID, (byte)SMSBLMem._PRESENT_SPEED_L);
		        if(Speed==-1){
			        Err = 1;
			        return -1;
		        }
	        }
	        if((Err==0) && ((Speed&(1<<15))!=0)){
		        Speed = -(Speed&~(1<<15));
	        }
	        return Speed;
        }
        public int ReadLoad(int ID)
        {
	        int Load = -1;
	        if(ID==-1){
                Load = Mem[SMSBLMem._PRESENT_LOAD_H - SMSBLMem._PRESENT_POSITION_L];
		        Load <<= 8;
                Load |= Mem[SMSBLMem._PRESENT_LOAD_L - SMSBLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Load = readWord((byte)ID, (byte)SMSBLMem._PRESENT_LOAD_L);
		        if(Load==-1){
			        Err = 1;
		        }
	        }
	        if((Err==0) && ((Load&(1<<10))!=0)){
		        Load = -(Load&~(1<<10));
	        }
	        return Load;
        }
        public int ReadVoltage(int ID)
        {	
	        int Voltage = -1;
	        if(ID==-1){
                Voltage = Mem[SMSBLMem._PRESENT_VOLTAGE - SMSBLMem._PRESENT_POSITION_L];	
	        }else{
		        Err = 0;
                Voltage = readByte((byte)ID, (byte)SMSBLMem._PRESENT_VOLTAGE);
		        if(Voltage==-1){
			        Err = 1;
		        }
	        }
	        return Voltage;
        }
        public int ReadTemper(int ID)
        {	
	        int Temper = -1;
	        if(ID==-1){
                Temper = Mem[SMSBLMem._PRESENT_TEMPERATURE - SMSBLMem._PRESENT_POSITION_L];	
	        }else{
		        Err = 0;
                Temper = readByte((byte)ID, (byte)SMSBLMem._PRESENT_TEMPERATURE);
		        if(Temper==-1){
			        Err = 1;
		        }
	        }
	        return Temper;
        }
        public int ReadMove(int ID)
        {
	        int Move = -1;
	        if(ID==-1){
                Move = Mem[SMSBLMem._MOVING - SMSBLMem._PRESENT_POSITION_L];	
	        }else{
		        Err = 0;
                Move = readByte((byte)ID, (byte)SMSBLMem._MOVING);
		        if(Move==-1){
			        Err = 1;
		        }
	        }
	        return Move;
        }
        public int ReadCurrent(int ID)
        {
	        int Current = -1;
	        if(ID==-1){
                Current = Mem[SMSBLMem._PRESENT_CURRENT_H - SMSBLMem._PRESENT_POSITION_L];
		        Current <<= 8;
                Current |= Mem[SMSBLMem._PRESENT_CURRENT_L - SMSBLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Current = readWord((byte)ID, (byte)SMSBLMem._PRESENT_CURRENT_L);
		        if(Current==-1){
			        Err = 1;
			        return -1;
		        }
	        }
	        if((Err==0) && ((Current&(1<<15))!=0)){
		        Current = -(Current&~(1<<15));
	        }	
	        return Current;
        }
    }
}
