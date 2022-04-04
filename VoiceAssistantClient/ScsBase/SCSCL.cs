/*
 * Servo.cs
 * 飞特串行舵机应用层程序
 * 日期: 2021.2.4
 * 作者: 
 */
using ScsServoLib.Def;
using ScsServoLib.Scs;
using ScsServoLib.Comm;

namespace ScsServoLib
{
    class SCSCL : SCS
    {
        public int Err = 0;
        byte[] Mem = new byte[SCSCLMem._PRESENT_CURRENT_H-SCSCLMem._PRESENT_POSITION_L + 1];
        public SCSCL(SCComm Comm) : base(Comm)
        {
            End = 1;
        }
        public int WritePos(byte ID, int Position, ushort Time, ushort Speed)
        {
	        byte[] bBuf = new byte[6];
	        Host2SCS(bBuf, 0, (ushort)Position);
            Host2SCS(bBuf, 2, Time);
	        Host2SCS(bBuf, 4, Speed);

            return genWrite(ID, (byte)SCSCLMem._GOAL_POSITION_L, bBuf, 6);
        }
        public int RegWritePos(byte ID, int Position, ushort Time, ushort Speed)
        {
	        byte[] bBuf = new byte[6];
            Host2SCS(bBuf, 0, (ushort)Position);
	        Host2SCS(bBuf, 2, 0);
	        Host2SCS(bBuf, 4, Speed);

            return regWrite(ID, (byte)SCSCLMem._GOAL_POSITION_L, bBuf, 6);
        }
        public void SyncWritePos(byte[] ID, byte IDN, int[] Position, ushort[] Time, ushort[] Speed)
        {
            byte[][] offbuf = new byte[32][];
            for(int i = 0; i<IDN; i++){
		        if(Position[i]<0){
			        Position[i] = -Position[i];
			        Position[i] |= (1<<15);
		        }
                offbuf[i] = new byte[6];
		        ushort T, V;
		        if(Speed!=null){
			        V = Speed[i];
		        }else{
			        V = 0;
		        }
		        if(Time!=null){
			        T = Time[i];
		        }else{
			        T = 0;
		        }
                Host2SCS(offbuf[i], 0, (ushort)Position[i]);
                Host2SCS(offbuf[i], 2, T);
                Host2SCS(offbuf[i], 4, V);
            }
            snycWrite(ID, IDN, (byte)SCSCLMem._GOAL_POSITION_L, offbuf, 6);
        }
        public int PWMMode(byte ID)
        {
	        byte[] bBuf = new byte[4];
	        bBuf[0] = 0;
	        bBuf[1] = 0;
	        bBuf[2] = 0;
	        bBuf[3] = 0;
            return genWrite(ID, (byte)SCSCLMem._MIN_ANGLE_LIMIT_L, bBuf, 4);	
        }
        public int WritePWM(byte ID, int pwmOut)
        {
            if (pwmOut < 0)
            {
                pwmOut = -pwmOut;
                pwmOut |= (1 << 10);
            }
	        byte[] bBuf = new byte[2];
            Host2SCS(bBuf, 0, (ushort)pwmOut);
            return genWrite(ID, (byte)SCSCLMem._GOAL_TIME_L, bBuf, 2);
        }
        public int EnableTorque(byte ID, byte Enable)
        {
            return writeByte(ID, (byte)SCSCLMem._TORQUE_ENABLE, Enable);
        }
        public int unLockEprom(byte ID)
        {
            return writeByte(ID, (byte)SCSCLMem._LOCK, 0);
        }

        public int LockEprom(byte ID)
        {
            return writeByte(ID, (byte)SCSCLMem._LOCK, 1);
        }
        public int FeedBack(byte ID)
        {
            int nLen = Read(ID, (byte)SCSCLMem._PRESENT_POSITION_L, Mem, (byte)Mem.Length);
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
                Pos = Mem[SCSCLMem._PRESENT_POSITION_L - SCSCLMem._PRESENT_POSITION_L];
		        Pos <<= 8;
                Pos |= Mem[SCSCLMem._PRESENT_POSITION_H - SCSCLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Pos = readWord((byte)ID, (byte)SCSCLMem._PRESENT_POSITION_L);
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
                Speed = Mem[SCSCLMem._PRESENT_SPEED_L - SCSCLMem._PRESENT_POSITION_L];
		        Speed <<= 8;
                Speed |= Mem[SCSCLMem._PRESENT_SPEED_H - SCSCLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Speed = readWord((byte)ID, (byte)SCSCLMem._PRESENT_SPEED_L);
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
                Load = Mem[SCSCLMem._PRESENT_LOAD_L - SCSCLMem._PRESENT_POSITION_L];
		        Load <<= 8;
                Load |= Mem[SCSCLMem._PRESENT_LOAD_H - SCSCLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Load = readWord((byte)ID, (byte)SCSCLMem._PRESENT_LOAD_L);
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
                Voltage = Mem[SCSCLMem._PRESENT_VOLTAGE - SCSCLMem._PRESENT_POSITION_L];	
	        }else{
		        Err = 0;
                Voltage = readByte((byte)ID, (byte)SCSCLMem._PRESENT_VOLTAGE);
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
                Temper = Mem[SCSCLMem._PRESENT_TEMPERATURE - SCSCLMem._PRESENT_POSITION_L];	
	        }else{
		        Err = 0;
                Temper = readByte((byte)ID, (byte)SCSCLMem._PRESENT_TEMPERATURE);
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
                Move = Mem[SCSCLMem._MOVING - SCSCLMem._PRESENT_POSITION_L];	
	        }else{
		        Err = 0;
                Move = readByte((byte)ID, (byte)SCSCLMem._MOVING);
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
                Current = Mem[SCSCLMem._PRESENT_CURRENT_L - SCSCLMem._PRESENT_POSITION_L];
		        Current <<= 8;
                Current |= Mem[SCSCLMem._PRESENT_CURRENT_H - SCSCLMem._PRESENT_POSITION_L];
	        }else{
		        Err = 0;
                Current = readWord((byte)ID, (byte)SCSCLMem._PRESENT_CURRENT_L);
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
