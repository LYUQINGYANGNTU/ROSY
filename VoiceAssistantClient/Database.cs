using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsServoLib
{
    class DBTables
    {
        public static string ConnSettings = "ConnectionSettings";
        public static string MotorsSettings = "MotorsSettings";
        public static string Postures = "Postures";
        public static string Scenarios = "Scenarios";
    }

    public class ConnSettings
    {
        public string PortName { get; set; }
        public string BaudRate { get; set; }
    }

    public class MotorsSettings
    {
        public int MotorID { get; set; }
        public int MinPos { get; set; }
        public int MaxPos { get; set; }
        public int DefaultSpeed { get; set; }
        public int DefaultAcc { get; set; }
    }

    public class MotorGoalPos
    {
        public int MotorID { get; set; }
        public int GoalPos { get; set; }
    }

    public class MotorPresentPos
    {
        public int MotorID { get; set; }
        public int PresentPos { get; set; }
    }

    public class Posture
    {
        public string Name { get; set; }
        public List<MotorGoalPos> MotorsGoalPos { get; set; }
    }

    public class PostureTransition
    {
        public string PostureName { get; set; }
        public int Delay { get; set; }
    }

    public class Scenario
    {
        public string Name { get; set; }
        public List<PostureTransition> PosturesFlow { get; set; }
    }

    class ServoDatabase
    {
        private string _file = @"\ServoDatabase.accdb";
        private string _connectionString;
        private OleDbCommand _cmd = new OleDbCommand();
        private OleDbConnection _conn = new OleDbConnection();
        
        private List<int> _motorIds = new List<int>();
        public ServoDatabase(string dbPath)
        {
            _connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;
                                        Persist Security Info=False;
                                        Data Source =" + dbPath + _file;
        }

        private DataTable GetAllRows(string tableName)
        {
            object data;

            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "select * from " + tableName;
                
                conn.Open();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(sql, conn))
                {
                    DataSet dataset = new DataSet();
                    adapter.Fill(dataset);
                    data = dataset.Tables[0];
                }
                
            }
            return data as DataTable;
        }

        private bool AddConfig(string configType, string configValue)
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "insert into " + DBTables.ConnSettings + " ([Type], [Data]) " +
                    "values(@type, @data)";

                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("type", configType),
                    new OleDbParameter("data", configValue),
                });


                return cmd.ExecuteNonQuery() > 0;
            }
        }
        public ConnSettings LoadConnSettings()
        {
            var data = GetAllRows(DBTables.ConnSettings);
            
            if (data == null || data.Rows.Count == 0)
            {
                return null;
            }

            var rows = data.Rows;
            string portName = "", baudRate = "";
            foreach (DataRow row in rows)
            {
                string type = row["Type"].ToString();
                string val = row["Data"].ToString();

                switch (type)
                {
                    case "PORTNAME":
                        portName = val;
                        break;
                    case "BAUDRATE":
                        baudRate = val;
                        break;
                }
            }

            return new ConnSettings
            {
                BaudRate = baudRate,
                PortName = portName
            };
        }

        public int AddOrUpdateConConfig(string portName, string baudRate)
        {
            DeleteConnConfig();

            int changedCnt = 0;

            changedCnt += AddConfig("PORTNAME", portName) ? 1 : 0;
            changedCnt += AddConfig("BAUDRATE", baudRate) ? 1 : 0;

            return changedCnt;
        }
        public List<MotorsSettings> LoadMotorSettings()
        {
            var data = GetAllRows(DBTables.MotorsSettings);

            if (data == null || data.Rows.Count == 0)
            {
                return null;
            }

            var list = new List<MotorsSettings>();

            foreach (DataRow row in data.Rows)
            {
                var m = new MotorsSettings
                {
                    MotorID = int.Parse(row["MotorID"].ToString()),
                    DefaultSpeed = int.Parse(row["DefaultSpeed"].ToString()),
                    DefaultAcc = int.Parse(row["DefaultAcc"].ToString()),
                    MinPos = int.Parse(row["MinPos"].ToString()),
                    MaxPos = int.Parse(row["MaxPos"].ToString()),
                };

                list.Add(m);
            }

            return list;           
        }

        public List<Posture> LoadPostures()
        {
            var data = GetAllRows(DBTables.Postures);

            if (data == null || data.Rows.Count == 0)
            {
                return new List<Posture>();
            }

            var postures = new List<Posture>();

            foreach (DataRow row in data.Rows)
            {
                string postureName = row["PostureName"].ToString();

                string motorPosStr = row["MotorPositions"].ToString();

                var motorPosList = StringToMotorGoalPos(motorPosStr);

                var m = new Posture
                {
                    Name = postureName,
                    MotorsGoalPos = motorPosList,
                };

                postures.Add(m);
            }

            return postures;
        }

        public List<Scenario> LoadScenarios()
        {
            var data = GetAllRows(DBTables.Scenarios);

            if (data == null || data.Rows.Count == 0)
            {
                return new List<Scenario>();
            }

            var scenarios = new List<Scenario>();

            foreach (DataRow row in data.Rows)
            {
                string scenarioName = row["ScenarioName"].ToString();

                string posturesFlow = row["PostureList"].ToString();

                var flow = StringToPostureFlow(posturesFlow);

                scenarios.Add(new Scenario { Name = scenarioName, PosturesFlow = flow});
            }

            return scenarios;
        }

        public Scenario FindScenarioByName(string name)
        {
            var list = LoadScenarios();
            return list.Find(x => x.Name == name);
        }

        public Posture FindPostureByName(string name)
        {
            var list = LoadPostures();
            return list.Find(x => x.Name == name);
        }
       
        public bool DeleteConnConfig()
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "Delete from " + DBTables.ConnSettings;

                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteMotor(int motorID)
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "Delete from " + DBTables.MotorsSettings + " WHERE MotorID = '" + motorID + "'";

                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool AddOrUpdateMotor(MotorsSettings settings)
        {
            DeleteMotor(settings.MotorID);

            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "insert into " + DBTables.MotorsSettings +
                    " ([MotorID], [DefaultAcc], [DefaultSpeed], [MinPos], [MaxPos]) " +
                    "values(@id, @acc, @speed, @min, @max)";

                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("id", settings.MotorID),
                    new OleDbParameter("acc", settings.DefaultAcc),
                    new OleDbParameter("speed", settings.DefaultSpeed),
                    new OleDbParameter("min", settings.MinPos),
                    new OleDbParameter("max", settings.MaxPos),
                });


                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeletePostureByName(string postureName)
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "Delete from " + DBTables.Postures + " WHERE PostureName = '" + postureName + "'";

                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteAllPostures()
        {

            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "Delete from " + DBTables.Postures;
                
                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                return cmd.ExecuteNonQuery() > 0;
            }
        }
        
        public bool AddOrUpdatePosture(string postureName, string motorsPos)
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "insert into " + DBTables.Postures + " ([PostureName], [MotorPositions]) " +
                    "values(@name, @pos)";

                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("name", postureName),
                    new OleDbParameter("pos", motorsPos),
                });


                return cmd.ExecuteNonQuery() > 0;
            }
        }
        
        public bool AddScenario(string scenarioName, string postures)
        {
            DeletePostureByName(scenarioName);

            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string sql = "insert into " + DBTables.Scenarios + " ([ScenarioName], [PostureList]) " +
                    "values(@name, @list)";

                conn.Open();

                OleDbCommand cmd = new OleDbCommand(sql, conn);

                cmd.Parameters.AddRange(new OleDbParameter[]
                {
                    new OleDbParameter("name", scenarioName),
                    new OleDbParameter("list", postures),
                });


                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private List<MotorGoalPos> StringToMotorGoalPos(string str)
        {
            var list = new List<MotorGoalPos>();

            var motorsInfo = str.Split(',');

            foreach (var item in motorsInfo)
            {
                try
                {
                    list.Add(new MotorGoalPos
                    {
                        MotorID = int.Parse(item.Split('-')[0]),
                        GoalPos = int.Parse(item.Split('-')[1]),
                    });
                }
                catch (Exception ex)
                {

                    throw ex;
                }
                
            }

            return list;
        }

        private List<PostureTransition> StringToPostureFlow(string str)
        {
            var list = new List<PostureTransition>();

            var items = str.Split('-');

            foreach (var item in items)
            {
                var info = item.Split(',');

                if (info.Length == 1)
                {
                    list.Add(new PostureTransition
                    {
                        Delay = 0,
                        PostureName = info[0]
                    });
                } else if (info.Length == 2)
                {
                    try
                    {
                        list.Add(new PostureTransition
                        {
                            PostureName = info[0],
                            Delay = int.Parse(info[1]),
                        });
                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }
                } else
                {
                    throw new Exception("Invalid format: " + item);
                }
            }

            return list;
        }
    }
}
