/*
 * Def.cs
 * 飞特串行舵机协议相关定义
 * 日期: 2021.2.4
 * 作者: 
 */

namespace ScsServoLib.Def
{

    //内存表定义
    public enum SCSCLMem
    {
        //-------EPROM(只读)--------
        _VERSION_L = 3,
        _VERSION_H =4,

        //-------EPROM(读写)--------
        _ID = 5,
        _BAUD_RATE = 6,
        _MIN_ANGLE_LIMIT_L = 9,
        _MIN_ANGLE_LIMIT_H = 10,
        _MAX_ANGLE_LIMIT_L = 11,
        _MAX_ANGLE_LIMIT_H = 12,
        _CW_DEAD = 26,
        _CCW_DEAD = 27,

        //-------SRAM(读写)--------
        _TORQUE_ENABLE = 40,
        _GOAL_POSITION_L = 42,
        _GOAL_POSITION_H = 43,
        _GOAL_TIME_L = 44,
        _GOAL_TIME_H = 45,
        _GOAL_SPEED_L = 46,
        _GOAL_SPEED_H = 47,
        _LOCK = 48,

        //-------SRAM(只读)--------
        _PRESENT_POSITION_L = 56,
        _PRESENT_POSITION_H = 57,
        _PRESENT_SPEED_L = 58,
        _PRESENT_SPEED_H = 59,
        _PRESENT_LOAD_L = 60,
        _PRESENT_LOAD_H = 61,
        _PRESENT_VOLTAGE = 62,
        _PRESENT_TEMPERATURE = 63,
        _MOVING = 66,
        _PRESENT_CURRENT_L = 69,
        _PRESENT_CURRENT_H = 70
    }
    public enum SMSBLMem
    {
        //-------EPROM(只读)--------
        _MODEL_L = 3,
        _MODEL_H = 4,

        //-------EPROM(读写)--------
        _ID = 5,
        _BAUD_RATE = 6,
        _MIN_ANGLE_LIMIT_L = 9,
        _MIN_ANGLE_LIMIT_H = 10,
        _MAX_ANGLE_LIMIT_L = 11,
        _MAX_ANGLE_LIMIT_H = 12,
        _CW_DEAD = 26,
        _CCW_DEAD = 27,
        _OFS_L = 31,
        _OFS_H = 32,
        _MODE = 33,

        //-------SRAM(读写)--------
        _TORQUE_ENABLE = 40,
        _ACC = 41,
        _GOAL_POSITION_L =42,
        _GOAL_POSITION_H =43,
        _GOAL_TIME_L = 44,
        _GOAL_TIME_H = 45,
        _GOAL_SPEED_L = 46,
        _GOAL_SPEED_H = 47,
        _LOCK = 55,

        //-------SRAM(只读)--------
        _PRESENT_POSITION_L = 56,
        _PRESENT_POSITION_H = 57,
        _PRESENT_SPEED_L = 58,
        _PRESENT_SPEED_H = 59,
        _PRESENT_LOAD_L = 60,
        _PRESENT_LOAD_H = 61,
        _PRESENT_VOLTAGE = 62,
        _PRESENT_TEMPERATURE = 63,
        _MOVING = 66,
        _PRESENT_CURRENT_L = 69,
        _PRESENT_CURRENT_H = 70
    }

    //波特率定义
    public enum BaudList
    {
        _1M = 0,
        _0_5M = 1,
        _250K = 2,
        _128K = 3,
        _115200 = 4,
        _76800 = 5,
        _57600 = 6,
        _38400 = 7
    }
    public enum Inst
    {
        INST_PING = 0x01,
        INST_READ = 0x02,
        INST_WRITE = 0x03,
        INST_REG_WRITE = 0x04,
        INST_REG_ACTION = 0x05,
        INST_SYNC_WRITE = 0x83
    }
}
