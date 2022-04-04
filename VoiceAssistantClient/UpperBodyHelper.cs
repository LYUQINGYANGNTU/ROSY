using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using ROBOTIS;
using System.ComponentModel;

namespace InHouseRobot_Body
{
    public class UpperBodyHelper
    {
        private static List<string> _postureNames { get; set; }
        private static List<int> _motorIds = new List<int>();
        private static List<Posture> _postureList = new List<Posture>();

        private const int GOAL_POSITION_ADDR = 30;
        private const int MOVING_SPEED_ADDR = 32;
        private const int PRESENT_POSITION_ADDR = 36;
        private const int TORQUE_ENABLE_ADDR = 24;

        public static BackgroundWorker portworker = new BackgroundWorker();

        static UpperBodyHelper()
        {
            portworker.DoWork += Portworker_DoWork;
        }

        private static void Portworker_DoWork(object sender, DoWorkEventArgs e)
        {
            //
        }

        public static void Initialise(int portName, int baudRate, List<int> motorIds)
        {
            return;
            dynamixel.dxl_initialize(portName, baudRate);
            _motorIds = motorIds;

            foreach (var item in motorIds)
            {
                dynamixel.dxl_write_word(item, MOVING_SPEED_ADDR, 20);
            }
            //ParallelLoopResult result = Parallel.ForEach(motorIds, item =>
            // {
            //     dynamixel.dxl_write_word(item, MOVING_SPEED_ADDR, 20);
            // });

            _postureNames = UpperBodyDB.GetPostureNames();
            _postureList = UpperBodyDB.GetPostureList();
        }
        public static List<string> GetPostureNames()
        {
            return UpperBodyDB.GetPostureNames();
        }
        public static void SetSpeed(int speed, List<int> motorIds)
        {
            foreach (var item in motorIds)
            {
                dynamixel.dxl_write_word(item, MOVING_SPEED_ADDR, speed);
            }
        }
        public static void SetSpeed(int speed, int motorId)
        {
            dynamixel.dxl_write_word(motorId, MOVING_SPEED_ADDR, speed);
        }
        public static void UnlockMotors(List<int> motorIds)
        {
            return;
            foreach (var item in motorIds)
            {
                dynamixel.dxl_write_word(item, TORQUE_ENABLE_ADDR, 0);
            }
        }
        public static void LockMotors(List<int> motorIds)
        {
            foreach (var item in motorIds)
            {
                dynamixel.dxl_write_word(item, TORQUE_ENABLE_ADDR, 1);
            }
        }
        public static int GetPresentPosition(int motorId)
        {
            return dynamixel.dxl_read_word(motorId, PRESENT_POSITION_ADDR);
        }
        public async static void Move(string PostureName)
        {
            return;
            if (_postureNames.Contains(PostureName))
            {
                var posture = GetPostures(PostureName);

                foreach (var part in posture)
                {
                    dynamixel.dxl_write_word(part.MotorId, GOAL_POSITION_ADDR, part.GoalPosition);

                }
                //ParallelLoopResult result = Parallel.ForEach(posture,part =>
                //{
                //    Thread.Sleep(50);
                //    dynamixel.dxl_write_word(part.MotorId, GOAL_POSITION_ADDR, part.GoalPosition);
                //});

            }
            else
            {
                MessageBox.Show($"Posture {PostureName} does not exists!");
            }
        }

        public async static void HeadMove(int Position)
        {
            dynamixel.dxl_write_word(8, GOAL_POSITION_ADDR, Position);
        }

        public async static void NeckMove(int Position)
        {
            dynamixel.dxl_write_word(7, GOAL_POSITION_ADDR, Position);
        }

        private static List<Posture> GetPostures(string postureName)
        {
            return null;
            return _postureList.FindAll(x => x.Name == postureName);
        }
        public static void Save(string PostureName, List<int> motorId)
        {
            if (_postureNames.Contains(PostureName) == false)
            {
                UpperBodyDB.Save(PostureName, motorId);
                _postureNames = UpperBodyDB.GetPostureNames();
                _postureList = UpperBodyDB.GetPostureList();
            }
            else
            {
                MessageBox.Show($"Posture name ({PostureName}) already exists!");
            }
        }
        public static void Delete(string PostureName)
        {
            if (_postureNames.Contains(PostureName))
            {
                UpperBodyDB.Delete(PostureName);
                _postureNames = UpperBodyDB.GetPostureNames();
                _postureList = UpperBodyDB.GetPostureList();
            }
            else
            {
                MessageBox.Show($"Posture name ({PostureName}) does not exists!");
            }
        }
        public static void DeleteAll()
        {
            UpperBodyDB.DeleteAll();
            _postureNames = null;
            _postureList = null;
        }
    }

    internal static class UpperBodyDB
    {
        static string _path = Application.StartupPath;
        static string _file = @"\RobotPosture.accdb";
        static OleDbCommand _cmd = new OleDbCommand();
        static OleDbConnection _conn = new OleDbConnection();
        private static int PRESENT_POSITION_ADDR = 36;

        static UpperBodyDB()
        {
            _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
            _path += @"\Database";
            _conn.ConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;
                                           Persist Security Info=False;
                                           Data Source =" + _path + _file;
            _cmd.Connection = _conn;
        }
        internal static void Save(string PostureName, List<int> _motorIds)
        {
            return;
            try
            {
                _conn.Open();
                foreach (var _motorId in _motorIds)
                {
                    int _motorValue = dynamixel.dxl_read_word(_motorId, PRESENT_POSITION_ADDR); // clear buffer
                    _motorValue = dynamixel.dxl_read_word(_motorId, PRESENT_POSITION_ADDR); // clear buffer
                    if (_motorId == 1) MessageBox.Show(_motorValue.ToString());
                    _cmd.CommandText = "insert into RobotPosture (PostureName, MotorId, MotorValue) values('" +
                                   PostureName + "','" + _motorId + "','" + _motorValue + "')";
                    _cmd.ExecuteNonQuery();
                }
                _conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        internal static void Delete(string PostureName)
        { 
            try
            {
                _conn.Open();
                _cmd.CommandText = "delete from RobotPosture where PostureName='" + PostureName + "'";
                _cmd.ExecuteNonQuery();
                _conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        internal static void DeleteAll()
        {
            try
            {
                _conn.Open();
                _cmd.CommandText = "delete * from RobotPosture";
                _cmd.ExecuteNonQuery();
                _conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        internal static List<string> GetPostureNames()
        {
            return null;
            List<string> playlist = new List<string>();

            try
            {
                var obj = GetData();

                if (obj != null)
                {
                    var dt = obj as DataTable;

                    foreach (DataRow dr in dt.Rows)
                    {
                        if (!playlist.Contains(dr["PostureName"].ToString()))
                        {
                            playlist.Add(dr["PostureName"].ToString());
                        }
                    }
                    return playlist;
                }
                else
                {
                    //no data
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }
        internal static List<Posture> GetPostureList()
        {
            return null;
            List<Posture> list = new List<Posture>();

            try
            {
                var obj = GetData();

                if (obj != null)
                {
                    var dt = obj as DataTable;

                    foreach (DataRow dr in dt.Rows)
                    {
                        Posture posture = new Posture
                        {
                            Name = Convert.ToString(dr["PostureName"]),
                            MotorId = Convert.ToInt32(dr["MotorId"]),
                            GoalPosition = Convert.ToInt32(dr["MotorValue"]),
                        };
                        list.Add(posture);
                    }
                    return list;
                }
                else
                {
                    //no data
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }
        internal static object GetData()
        {
            return null;
            object obj;

            try
            {
                _cmd.CommandText = "select * from RobotPosture";

                _conn.Open();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(_cmd))
                {
                    DataSet dataset = new DataSet();

                    adapter.Fill(dataset);

                    obj = dataset.Tables[0];
                }
            }
            catch (Exception ex)
            {
                obj = null;
                MessageBox.Show(ex.Message);
            }
            finally
            {
                _conn.Close();
            }

            return obj;
        }

    }
    internal class Posture
    {
        public string Name { get; set; }
        public int MotorId { get; set; }
        public int GoalPosition { get; set; }
    }
}
