using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using System.Threading;
using System.Data.OleDb;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Rosbridge.Client;
using Newtonsoft.Json.Linq;
using ROSHelper.ROSPublisher;
using Newtonsoft.Json;
using System.IO.Ports;
using System.Windows.Forms;
using InHouseRobot_Body;
using ROBOTIS;
using Robot.Data;
using VoiceAssistantClient;

namespace Robot
{
    namespace Database
    {
        internal class Settings
        {
            private static string _path = Application.StartupPath;
            private static string _file = @"\RobotSettings.accdb";
            private static OleDbCommand _cmd = new OleDbCommand();
            private static OleDbConnection _conn = new OleDbConnection();
            internal enum Type
            {
                DYNAMIXEL_PORTNAME,
                DYNAMIXEL_BAUDRATE,
                ARDUINO_PORTNAME,
                ARDUINO_BAUDRATE
            };

            static Settings()
            {
                _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                _path += @"\Database";
                _conn.ConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;
                                           Persist Security Info=False;
                                           Data Source =" + _path + _file;
                _cmd.Connection = _conn;
            }
            internal static string Read(string type)
            {
                string _data = null;

                try
                {
                    _conn.Open();
                    _cmd.CommandText = "select * from RobotSettings where Type ='" + type + "'";

                    OleDbDataReader _reader = _cmd.ExecuteReader();
                    while (_reader.Read()) _data = _reader["Data"].ToString();
                    _conn.Close();
                }
                catch
                {
                    _data = null;
                }
                return _data;
            }
            internal static void Update(string type, string data)
            {
                try
                {
                    _conn.Open();
                    _cmd.CommandText = "update RobotSettings set Data='" + data + "'where Type= '" + type + "'";
                    _cmd.ExecuteNonQuery();
                    _conn.Close();
                }
                catch
                { }
            }
        }

        internal class Posture
        {
            public string Name { get; set; }
            public int MotorId { get; set; }
            public int GoalPosition { get; set; }
        }
        internal class UpperBody
        {
            static string _path = System.Windows.Forms.Application.StartupPath;
            static string _file = @"\RobotPosture.accdb";
            static OleDbCommand _cmd = new OleDbCommand();
            static OleDbConnection _conn = new OleDbConnection();

            private static List<int> _motorIds = new List<int>();
            //private static int GOAL_POSITION_ADDR = 30;
            private static int PRESENT_POSITION_ADDR = 36;

            static UpperBody()
            {
                _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                _path += @"\Database";
                _conn.ConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;
                                           Persist Security Info=False;
                                           Data Source =" + _path + _file;
                _cmd.Connection = _conn;
            }
            internal static void Initialise(List<int> motorIds)
            {
                _motorIds = motorIds;
            }
            internal static void Save(string PostureName)
            {
                try
                {
                    _conn.Open();
                    foreach (var _motorId in _motorIds)
                    {
                        int _motorValue = dynamixel.dxl_read_word(_motorId, PRESENT_POSITION_ADDR);
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
                                GoalPosition = Convert.ToInt32(dr["MotorValue"])
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
            internal static object GetPostureData(string PostureName)
            {
                object obj;

                try
                {
                    _cmd.CommandText = "select * from RobotPosture where PostureName='"
                                                    + PostureName + "'";

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

            internal static object GetData()
            {
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
        internal class Location
        {
            private static string _path = Application.StartupPath;
            private static string _file = @"\RobotLocation.accdb";
            private static string _connString = null;

            static Location()
            {
                _path = _path.Split(new string[] { @"\bin\" }, StringSplitOptions.None)[0];
                _path += @"\Database";

                _connString = @"Provider=Microsoft.ACE.OLEDB.12.0;
                                Persist Security Info=False;
                                Data Source =" + _path + _file;
            }

            internal static void Add(string locationName, Data.BotLocation botLocation)
            {
                string query = "insert into [Location Table](LocationName,X,Y,Z,W)values(@Locationname,@X,@Y,@Z,@W)";
                OleDbParameter[] para = {
                                        new OleDbParameter("@Locationname",OleDbType.VarChar),
                                        new OleDbParameter("@X",OleDbType.Decimal),
                                        new OleDbParameter("@Y",OleDbType.Decimal),
                                        new OleDbParameter("@Z",OleDbType.Decimal),
                                        new OleDbParameter("@W",OleDbType.Decimal),
                                    };
                para[0].Value = locationName;
                para[1].Value = botLocation.pos_x;
                para[2].Value = botLocation.pos_y;
                para[3].Value = botLocation.ori_z;
                para[4].Value = botLocation.ori_w;

                foreach (var p in para)
                {
                    MessageBox.Show(p.Value.ToString());
                }
                using (OleDbConnection _conn = new OleDbConnection(_connString))
                {
                    using (OleDbCommand _cmd = new OleDbCommand(query, _conn))
                    {
                        if (para != null && para.Length > 0) _cmd.Parameters.AddRange(para);
                        if (_conn.State == ConnectionState.Closed) _conn.Open();
                        _cmd.ExecuteNonQuery();
                        _conn.Close();
                    }
                }
            }

            internal static Dictionary<String, Data.BotLocation> Read()
            {
                Dictionary<String, Data.BotLocation> data = new Dictionary<string, Data.BotLocation>();
                using (OleDbConnection _conn = new OleDbConnection(_connString))
                {
                    _conn.Open();

                    string query = "SELECT LocationName,X,Y,Z,W FROM [Location Table]";

                    OleDbCommand _cmd = new OleDbCommand(query, _conn);

                    using (OleDbDataReader reader = _cmd.ExecuteReader())
                    {
                        decimal pos_x;
                        decimal pos_y;
                        decimal ori_z;
                        decimal ori_w;

                        while (reader.Read())
                        {
                            Data.BotLocation botLocation = new Data.BotLocation();

                            Decimal.TryParse(reader["X"].ToString(), out pos_x);
                            Decimal.TryParse(reader["Y"].ToString(), out pos_y);
                            Decimal.TryParse(reader["Z"].ToString(), out ori_z);
                            Decimal.TryParse(reader["W"].ToString(), out ori_w);

                            botLocation.Set(pos_x, pos_y, ori_z, ori_w);
                            data.Add(reader["LocationName"].ToString(), botLocation);
                        }
                    }
                }
                return data;
            }
            internal static void Delete(string locationName)
            {
                string query = "DELETE FROM [Location Table] WHERE LocationName='" + locationName + "'";

                using (OleDbConnection _conn = new OleDbConnection(_connString))
                {
                    using (OleDbCommand _cmd = new OleDbCommand(query, _conn))
                    {
                        try
                        {
                            if (_conn.State == ConnectionState.Closed) _conn.Open();
                            _cmd.ExecuteNonQuery();
                            _conn.Close();
                        }
                        catch { }
                    }
                }
            }
            internal static void Clear()
            {
                string query = "DELETE * FROM [Location Table]";

                using (OleDbConnection _conn = new OleDbConnection(_connString))
                {
                    using (OleDbCommand _cmd = new OleDbCommand(query, _conn))
                    {
                        try
                        {
                            if (_conn.State == ConnectionState.Closed) _conn.Open();
                            _cmd.ExecuteNonQuery();
                            _conn.Close();
                        }
                        catch { }
                    }
                }
            }
        }
    }
    namespace ROS
    {
        internal static class Bridge
        {
            public static Dictionary<String, Data.BotLocation> SetPoint = new Dictionary<string, Data.BotLocation>();
            private static MessageDispatcher _md;
            private static readonly string _uid = MessageDispatcher.GetUID();

            internal static MessageDispatcher GetMessageDispatcher() { return _md; }
            internal static string GetUID() { return _uid; }

            internal static void Connect(string ip)
            {
                try
                {
                    _md = new MessageDispatcher(new Socket(new Uri("ws://" + ip)), new MessageSerializerV2_0());
                    _md.StartAsync();
                }
                catch (Exception)
                {
                    _md = null;
                    return;
                }
                Data.ROS.Connected = true;
            }
            internal async static void Disconnect()
            {
                try
                {
                    if (Data.ROS.Connected)
                    {
                        await _md.StopAsync();
                        _md = null;
                        Data.ROS.Connected = false;
                    }
                }
                catch { }
            }
        }
        internal class Transmitter
        {
            private Publisher _publisher;
            private List<string> topics = new List<string>();
            private List<string> topic_msg_types = new List<string>();
            private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
            private readonly string _uid = MessageDispatcher.GetUID();
            private readonly double zero = 0;
            private readonly string invertedcommas = "\"";
            private readonly string curlybracketopen = "{";
            private readonly string curlybracketclose = "}";

            internal Transmitter() { }
            internal void Set(Publisher publisher) { _publisher = publisher; }
            internal async void Move(double linear_speed, double angular_speed)
            {
                if (_publisher != null)
                {
                    await semaphoreSlim.WaitAsync();

                    try
                    {
                        string Content = ($"{curlybracketopen}" +
                        $"{invertedcommas}linear{invertedcommas}:{curlybracketopen}" +
                        $"{invertedcommas}y{invertedcommas}:{zero}," +
                        $"{invertedcommas}x{invertedcommas}:{linear_speed}," +
                        $"{invertedcommas}z{invertedcommas}:{zero}{curlybracketclose}," +
                        $" {invertedcommas}angular{invertedcommas}:{curlybracketopen}{invertedcommas}y{invertedcommas}:{zero}," +
                        $"{invertedcommas}x{invertedcommas}:{zero}," +
                        $"{invertedcommas}z{invertedcommas}:{angular_speed}" +
                        $"{curlybracketclose}" +
                        $"{curlybracketclose}");

                        var obj = JObject.Parse(Content);

                        await _publisher.PublishAsync(obj, "cmd_vel");
                    }
                    catch
                    { }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }
            }
            internal async void Go(Data.BotLocation botLocation)
            {
                if (_publisher != null)
                {
                    await semaphoreSlim.WaitAsync();

                    try
                    {
                        string Content = $"{curlybracketopen}{invertedcommas}header{invertedcommas} : {curlybracketopen}" +
                                         $"{invertedcommas}stamp{invertedcommas} : {curlybracketopen}" +
                                         $"{invertedcommas}secs{invertedcommas} : 0, {invertedcommas}nsecs{invertedcommas} : 0 {curlybracketclose}," +
                                         $"{invertedcommas}frame_id{invertedcommas} : {invertedcommas}map{invertedcommas}, {invertedcommas}seq{invertedcommas} : 0 {curlybracketclose}," +
                                         $"{invertedcommas}pose{invertedcommas} : {curlybracketopen} " +
                                         $"{invertedcommas}position{invertedcommas} : {curlybracketopen}" +
                                         $"{invertedcommas}y{invertedcommas} : {botLocation.pos_y} , {invertedcommas}x{invertedcommas} : {botLocation.pos_x}, {invertedcommas}z{invertedcommas} :0.0 {curlybracketclose}," +
                                         $"{invertedcommas}orientation{invertedcommas} : {curlybracketopen} " +
                                         $"{invertedcommas}y{invertedcommas} : 0.0, {invertedcommas}x{invertedcommas} :  0.0, {invertedcommas}z{invertedcommas} : {botLocation.ori_z}, {invertedcommas}w{invertedcommas} : {botLocation.ori_w} {curlybracketclose}" +
                                         $"{curlybracketclose}" +
                                         $"{curlybracketclose}";
                        var obj = JObject.Parse(Content);

                        await _publisher.PublishAsync(obj, "/move_base_simple/goal");

                    }
                    catch
                    { }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }
            }
            internal async void CancelNavigation()
            {
                if (_publisher != null)
                {
                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        string Content = $"{curlybracketopen}{invertedcommas}id{invertedcommas} : {invertedcommas}{invertedcommas} {curlybracketclose} ";
                        var obj = JObject.Parse(Content);
                        await _publisher.PublishAsync(obj, "/move_base/cancel");
                    }
                    catch
                    { }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }
            }

            internal async void ChangeMap(string mapinfo)
            {
                await semaphoreSlim.WaitAsync();
                
                try
                {
                    string Content = ($"{curlybracketopen}" +
                        $" {invertedcommas}data{invertedcommas}:{invertedcommas}{mapinfo}{invertedcommas}" +
                        $"{curlybracketclose}");

                    var obj = JObject.Parse(Content);

                    await _publisher.PublishAsync(obj, "/map_change_data");
                    //MessageBox.Show("changed");
                }
                catch
                { }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
        }
        internal class Receiver
        {
            private Subscriber _subscriber;
            public event EventHandler<NavigationStatusEventArgs> NavigationStatusChanged;
            public event EventHandler<MapStatusEventArgs> MapStatusChanged;
            internal Receiver() { }
            internal async void Initialise()
            {
                foreach (var data in Data.ROS.DataList)
                {
                    try
                    {
                        _subscriber = new Subscriber(data.Value.Topic, "", data.Value.TopicType, "", Bridge.GetMessageDispatcher());
                        _subscriber.MessageReceived += Subscriber_MessageReceived;
                        await _subscriber.SubscribeAsync();
                    }
                    catch { }
                }
            }
            internal async void Subscribe(string topic, string topicType)
            {
                _subscriber = new Subscriber(topic, "", topicType, "", Bridge.GetMessageDispatcher());
                _subscriber.MessageReceived += Subscriber_MessageReceived;
                await _subscriber.SubscribeAsync();
            }
            internal async void Unsubscribe(string subscribed_topic)
            {
                MessageDispatcher _md = Bridge.GetMessageDispatcher();
                await _md.SendAsync(new
                {
                    op = "unsubscribe",
                    id = Bridge.GetUID(),
                    topic = subscribed_topic
                });
            }
            private void Subscriber_MessageReceived(object sender, MessageReceivedEventArgs e)
            {

                try
                {

                    dynamic json = JsonConvert.DeserializeObject<dynamic>(e.Message.ToString());
                    string dataTopic = json.topic;


                    if (dataTopic == "/Lencoder")
                        Data.ROS.DataList["LEFT_ENCODER"].Data = json.msg.data;
                    else if (dataTopic == "/Rencoder")
                        Data.ROS.DataList["RIGHT_ENCODER"].Data = json.msg.data;
                    else if (dataTopic == "/Lvel")
                        Data.ROS.DataList["LEFT_VELOCITY"].Data = json.msg.data;
                    else if (dataTopic == "/Rvel")
                        Data.ROS.DataList["RIGHT_VELOCITY"].Data = json.msg.data;
                    else if (dataTopic == "/sonar0")
                        Data.ROS.DataList["SONAR_0"].Data = json.msg.range;
                    else if (dataTopic == "/sonar1")
                        Data.ROS.DataList["SONAR_1"].Data = json.msg.range;
                    else if (dataTopic == "/sonar2")
                        Data.ROS.DataList["SONAR_2"].Data = json.msg.range;
                    else if (dataTopic == "/sonar3")
                        Data.ROS.DataList["SONAR_3"].Data = json.msg.range;
                    else if (dataTopic == "/move_base/result")
                    {
                        Data.ROS.DataList["NAVIGATION_STATUS"].Data = json.msg.status.text;
                        var args = new NavigationStatusEventArgs((string)json.msg.status.text);
                        OnNavigationStatusChanged(args);
                    }
                    else if (dataTopic == "/amcl_pose")
                    {
                        decimal _posX = Convert.ToDecimal(json.msg.pose.pose.position.x);
                        decimal _posY = Convert.ToDecimal(json.msg.pose.pose.position.y);
                        decimal _oriZ = Convert.ToDecimal(json.msg.pose.pose.orientation.z);
                        decimal _oriW = Convert.ToDecimal(json.msg.pose.pose.orientation.w);

                        Data.ROS.Bot_Location = new Data.BotLocation(_posX, _posY, _oriZ, _oriW);
                    }
                    else if (dataTopic == "/map_change_status")
                    {
                        string map_changed_result = json.msg.data;
                        //MessageBox.Show(map_changed_result);
                        Data.ROS.DataList["Map_Status"].Data = json.msg.data;
                        var args = new MapStatusEventArgs((string)json.msg.data);
                        OnMapStatusChanged(args);

                        //TourHelper.GoNextPoint(GlobalData.LocationCount);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            private void OnNavigationStatusChanged(NavigationStatusEventArgs e)
            {
                EventHandler<NavigationStatusEventArgs> handler = NavigationStatusChanged;
                handler(this, e);
            }
            private void OnMapStatusChanged(MapStatusEventArgs e)
            {
                EventHandler<MapStatusEventArgs> handler = MapStatusChanged;
                handler(this, e);
            }
        }
    }
    namespace Data
    {
        public static class ROS
        {
            public enum BaseDirection { FORWARD, BACKWARD, STOP, CLOCKWISE, ANTICLOCKWISE };
            //public const string ROS_IP = "172.20.10.4:9090"; 
            public const string ROS_IP = " 169.254.3.93:9090"; // 192.168.31.200:9090

            public static List<string> Pub_ListTopics = new List<string> { "cmd_vel", "/move_base_simple/goal", "/move_base/cancel", "/amcl_pose", "/map_change_data" };
            public static List<string> Pub_ListTopicsType = new List<string> { "geometry_msgs/Twist", "geometry_msgs/PoseStamped", "actionlib_msgs/GoalID", "geometry_msgs/PoseWithCovarianceStamped", "std_msgs/String" };
            public static bool Connected { get; internal set; } = false;
            public static Dictionary<string, SubscribedData> DataList = new Dictionary<string, SubscribedData>()
            {
                {ROSTopic.Map_Status, new SubscribedData("/map_change_status", "std_msgs/String", null)},
                {ROSTopic.LEFT_VELOCITY, new SubscribedData("/Lvel", "std_msgs/Int16", null)},
                {ROSTopic.RIGHT_VELOCITY, new SubscribedData("/Rvel", "std_msgs/Int16", null)},
                {ROSTopic.LEFT_ENCODER, new SubscribedData("/Lencoder", "std_msgs/Int16", null)},
                {ROSTopic.RIGHT_ENCODER, new SubscribedData("/Rencoder", "std_msgs/Int16", null)},
                {ROSTopic.SONAR_0, new SubscribedData("/sonar0", "sensor_msgs/Range", null)},
                {ROSTopic.SONAR_1, new SubscribedData("/sonar1", "sensor_msgs/Range", null)},
                {ROSTopic.SONAR_2, new SubscribedData("/sonar2", "sensor_msgs/Range", null)},
                {ROSTopic.SONAR_3, new SubscribedData("/sonar3", "sensor_msgs/Range", null)},
                {ROSTopic.NAVIGATION_STATUS, new SubscribedData("/move_base/result", "move_base_msgs/MoveBaseActionResult", null)},
                {ROSTopic.BOT_LOCATION, new SubscribedData("/amcl_pose", "geometry_msgs/PoseWithCovarianceStamped", null)},
            };
            public static string UnknownData { get; set; }
            public static BotLocation Bot_Location = new BotLocation();
        }
        public static class ROSTopic
        {
            public static string LEFT_ENCODER { get { return "LEFT_ENCODER"; } }
            public static string RIGHT_ENCODER { get { return "RIGHT_ENCODER"; } }
            public static string LEFT_VELOCITY { get { return "LEFT_VELOCITY"; } }
            public static string RIGHT_VELOCITY { get { return "RIGHT_VELOCITY"; } }
            public static string SONAR_0 { get { return "SONAR_0"; } }
            public static string SONAR_1 { get { return "SONAR_1"; } }
            public static string SONAR_2 { get { return "SONAR_2"; } }
            public static string SONAR_3 { get { return "SONAR_3"; } }
            public static string NAVIGATION_STATUS { get { return "NAVIGATION_STATUS"; } }
            public static string BOT_LOCATION { get { return "BOT_LOCATION"; } }
            public static string Map_Status { get { return "Map_Status"; } }
        }
        public class SubscribedData
        {
            public string Topic { get; internal set; }
            public string TopicType { get; internal set; }
            public string Data { get; internal set; }

            public SubscribedData(string topic, string topicType, string data)
            {
                Topic = topic;
                TopicType = topicType;
                Data = data;
            }
        }
        public class BotLocation
        {
            public decimal pos_x { get; private set; }
            public decimal pos_y { get; private set; }
            public decimal ori_z { get; private set; }
            public decimal ori_w { get; private set; }

            public BotLocation() { }
            public BotLocation(decimal x, decimal y, decimal z, decimal w)
            {
                pos_x = x;
                pos_y = y;
                ori_z = z;
                ori_w = w;
            }
            public void Set(decimal x, decimal y, decimal z, decimal w)
            {
                pos_x = x;
                pos_y = y;
                ori_z = z;
                ori_w = w;
            }
            public void Clear()
            {
                pos_x = 0;
                pos_y = 0;
                ori_z = 0;
                ori_w = 0;
            }
        }
    }
    namespace Vision
    {
        public class ObjectDetectedEventArgs : EventArgs
        {
            public List<DetectedObject> Objects { get; private set; }
            public ObjectDetectedEventArgs(List<DetectedObject> _obj)
            {
                this.Objects = _obj;
            }
        }
        public delegate void ObjectDetectedEventHandler(object sender, ObjectDetectedEventArgs e);
        public class DetectedObject
        {
            public string Type { get; set; }
            public int Min_Y { get; set; }
            public int Min_X { get; set; }
            public int Max_Y { get; set; }
            public int Max_X { get; set; }
            public double Confidence { get; set; }
            public string Timestamp { get; set; }
        }
        public class ObjectDetection
        {
            [DllImport("User32.dll")]
            static extern int SetForegroundWindow(IntPtr point);

            private string _textName = "";
            private string _textDirectory = "";
            private FileSystemWatcher _watcher;
            public string TextLocation { get; set; }
            public string ExeLocation { get; set; }
            public event ObjectDetectedEventHandler ObjectDetected;

            public void Start()
            {
                _textDirectory = Path.GetDirectoryName(TextLocation) + @"\";
                _textName = Path.GetFileName(TextLocation);

                _watcher = new FileSystemWatcher(_textDirectory);
                _watcher.Changed += _watcher_Changed;
                _watcher.EnableRaisingEvents = true;
                _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
                Process.Start(ExeLocation);
            }
            public void Stop(char closingKey = 'q')
            {
                try
                {
                    _watcher.Changed -= _watcher_Changed;
                    _watcher.EnableRaisingEvents = false;

                    Process p = Process.GetProcessesByName("python").FirstOrDefault();
                    if (p != null)
                    {
                        IntPtr h = p.MainWindowHandle;
                        SetForegroundWindow(h);
                        SendKeys.SendWait(closingKey.ToString());
                    }
                }
                catch { }
            }
            private void _watcher_Changed(object sender, FileSystemEventArgs e)
            {
                Thread.Sleep(100);
                if (e.Name.ToString() == _textName)
                {
                    try
                    {
                        List<DetectedObject> _objectsData = new List<DetectedObject>();
                        string[] _objectData;
                        string[] _objects = File.ReadAllLines(TextLocation);

                        for (int i = 0; i < _objects.Length; i++)
                        {
                            if (i > 0) // first entry is the data format
                            {
                                _objectData = _objects[i].Split(',');
                                _objectsData.Add(new DetectedObject
                                {
                                    Type = _objectData[0],
                                    Min_Y = Convert.ToInt32(_objectData[1]),
                                    Min_X = Convert.ToInt32(_objectData[2]),
                                    Max_Y = Convert.ToInt32(_objectData[3]),
                                    Max_X = Convert.ToInt32(_objectData[4]),
                                    Confidence = Convert.ToDouble(_objectData[5]),
                                    Timestamp = _objectData[6]
                                });
                            }
                        }
                        OnObjectDetected(_objectsData);
                    }
                    catch { }
                }
            }
            private void OnObjectDetected(List<DetectedObject> ObjectsData)
            {
                if (ObjectDetected != null)
                    this.ObjectDetected(this, new ObjectDetectedEventArgs(ObjectsData));
            }
        }
        public class FaceRecognition
        {

        }
    }
    public class Base
    {
        private static readonly Object _object = new Object();
        private ROS.Transmitter _tx = new ROS.Transmitter();
        private ROS.Receiver _rx = new ROS.Receiver();
        private Dictionary<string, Data.BotLocation> SavedLocations = new Dictionary<string, Data.BotLocation>();
        private Thread _baseLinearThread = null;
        private Thread _baseAngularThread = null;
        private string _prevLinear = null;
        private string _prevAngular = null;
        private double _linearSpeed;
        private double _angularSpeed;
        public event EventHandler<NavigationStatusEventArgs> NavigationStatusChanged;
        public event EventHandler<MapStatusEventArgs> MapStatusChanged;
        public double LinearSpeed
        {
            get { return _linearSpeed; }
            set
            {
                if (value < 0) _linearSpeed = value * -1;
                else _linearSpeed = value;
            }
        }
        public double AngularSpeed
        {
            get { return _angularSpeed; }
            set
            {
                if (value < 0) _angularSpeed = value * -1;
                else _angularSpeed = value;
            }
        }

        public Base()
        {
        }
        public void Connect(string ip = Data.ROS.ROS_IP)
        {
            ROS.Bridge.Connect(ip);
        }
        public void Disconnect()
        {
            ROS.Bridge.Disconnect();
        }
        public void Initialise()
        {
            Thread thread = new Thread(new ThreadStart(() =>
            {
                var md = ROS.Bridge.GetMessageDispatcher();

               

                while (md.CurrentState.ToString() != "Started") ;

            

                Publisher _pub = new Publisher("", "", "", "", "", "", "", "", "", "", ROS.Bridge.GetMessageDispatcher());
                _pub.SetPublisher(Data.ROS.Pub_ListTopics, Data.ROS.Pub_ListTopicsType, ROS.Bridge.GetMessageDispatcher());
                _tx.Set(_pub);

                _rx.Initialise();
                _rx.NavigationStatusChanged += _rx_NavigationStatusChanged;
                _rx.MapStatusChanged += _rx_MapStatusChanged;


                SavedLocations = Database.Location.Read();
                
            }));
            thread.IsBackground = true;
            thread.Start();
        }

        public void SaveLocation(string locationName)
        {

            Data.BotLocation botLocation = new Data.BotLocation();
            botLocation.Set(Convert.ToDecimal(Data.ROS.Bot_Location.pos_x), Convert.ToDecimal(Data.ROS.Bot_Location.pos_y),
                            Convert.ToDecimal(Data.ROS.Bot_Location.ori_z), Convert.ToDecimal(Data.ROS.Bot_Location.ori_w));
            Database.Location.Add(locationName, botLocation);
            SavedLocations = Database.Location.Read();
        }
        public List<string> GetLocations()
        {
            SavedLocations = Database.Location.Read();
            var locationNames = new List<string>();

            foreach (var pair in SavedLocations)
            {
                locationNames.Add(pair.Key);
            }
            return locationNames;
        }

        public void DeleteLocation(string locationName)
        {
            Database.Location.Delete(locationName);
            SavedLocations = Database.Location.Read();
        }
        public void DeleteAllLocations()
        {
            Database.Location.Clear();
            SavedLocations = new Dictionary<string, Data.BotLocation>();
        }
        public void Go(string location)
        {
            _tx.Go(SavedLocations[location]);
        }
        public void Go(Data.BotLocation botLocation)
        {
            _tx.Go(botLocation);
        }
        public void Move()
        {
            _tx.Move(LinearSpeed, AngularSpeed);
        }
        public void Mapchange(string mapinfo)
        {
            _tx.ChangeMap(mapinfo);
        }
        public void Move(double linear_speed, double angular_speed)
        {
            _tx.Move(linear_speed, angular_speed);
        }
        public void Move(Data.ROS.BaseDirection direction)
        {
            if (direction == Data.ROS.BaseDirection.FORWARD || direction == Data.ROS.BaseDirection.BACKWARD)
            {
                StopAngular();
                double _speed = LinearSpeed;
                if (_prevLinear != direction.ToString())
                {
                    if (_baseLinearThread != null)
                    {
                        if (_baseLinearThread.IsAlive)
                            _baseLinearThread.Abort();
                    }

                    if (direction == Data.ROS.BaseDirection.BACKWARD)
                    {
                        _speed = _speed * -1;
                    }
                    _baseLinearThread = new Thread(new ThreadStart(() => MoveContinuously(_speed, 0)));
                    _baseLinearThread.Start();
                    _prevLinear = direction.ToString();
                }
            }
            else if (direction == Data.ROS.BaseDirection.CLOCKWISE || direction == Data.ROS.BaseDirection.ANTICLOCKWISE)
            {
                StopLinear();
                double _speed = AngularSpeed;
                if (_prevAngular != direction.ToString())
                {
                    if (_baseAngularThread != null)
                    {
                        if (_baseAngularThread.IsAlive)
                            _baseAngularThread.Abort();
                    }

                    if (direction == Data.ROS.BaseDirection.CLOCKWISE)
                    {
                        _speed = _speed * -1;
                    }
                    _baseAngularThread = new Thread(new ThreadStart(() => MoveContinuously(0, _speed)));
                    _baseAngularThread.Start();
                    _prevAngular = direction.ToString();
                }
            }
        }
        public void StopLinear()
        {
            try
            {
                _prevLinear = null;
                if (_baseLinearThread.IsAlive)
                    _baseLinearThread.Abort();
            }
            catch
            {

            }
        }
        public void StopAngular()
        {
            try
            {
                _prevAngular = null;
                if (_baseAngularThread.IsAlive)
                    _baseAngularThread.Abort();
            }
            catch
            {

            }
        }
        public void CancelNavigation()
        {
            _tx.CancelNavigation();
        }
        public void Stop()
        {
            try
            {
                StopLinear();
                StopAngular();
            }
            catch { }
        }
        private void MoveContinuously(double linear_speed, double angular_speed)
        {
            lock (_object)
            {
                while (true)
                {
                    _tx.Move(linear_speed, angular_speed);
                    Thread.Sleep(100); // reduce workload
                }
            }
        }
        private void _rx_NavigationStatusChanged(object sender, NavigationStatusEventArgs e)
        {
            EventHandler<NavigationStatusEventArgs> handler = NavigationStatusChanged;
            handler(this, e);
        }

        private void _rx_MapStatusChanged(object sender, MapStatusEventArgs e)
        {
            EventHandler<MapStatusEventArgs> handler = MapStatusChanged;
            handler(this, e);
        }
    }
    public class UpperBody
    {
        public int PortName { get; private set; }
        public int BaudRate { get; private set; }

        private List<int> _motorIds = new List<int>();
        private List<string> _postureNames = new List<string>();
        private List<Database.Posture> _postureList = new List<Database.Posture>();
        private const int GOAL_POSITION_ADDR = 30;
        private const int MOVING_SPEED_ADDR = 32;
        private const int PRESENT_POSITION_ADDR = 36;

        public void SetSpeed(int speed)
        {
            foreach (var item in _motorIds)
            {
                dynamixel.dxl_write_word(item, MOVING_SPEED_ADDR, speed);
            }
        }

        public UpperBody(List<int> MotorIds)
        {
            PortName = Convert.ToInt32(Database.Settings.Read("DYNAMIXEL_PORTNAME"));
            BaudRate = Convert.ToInt32(Database.Settings.Read("DYNAMIXEL_BAUDRATE"));
            _motorIds = MotorIds;

            Database.UpperBody.Initialise(_motorIds);
            _motorIds = MotorIds;
            dynamixel.dxl_initialize(PortName, BaudRate);

            foreach (var item in _motorIds)
            {
                dynamixel.dxl_write_word(item, MOVING_SPEED_ADDR, 20);
            }

            _postureNames = Database.UpperBody.GetPostureNames();

            _postureList = Database.UpperBody.GetPostureList();
        }
        public List<string> GetPostureNames()
        {
            return _postureNames;
        }

        private Database.Posture GetPosture(string postureName)
        {
            return _postureList.SingleOrDefault(x => x.Name == postureName);
        }

        public void Move(string PostureName)
        {
            if (_postureNames.Contains(PostureName))
            {

                var posture = GetPosture(PostureName);

                dynamixel.dxl_write_word(posture.MotorId, GOAL_POSITION_ADDR, posture.GoalPosition);

            }
            else
            {
                MessageBox.Show($"Posture {PostureName} does not exists!");
            }
        }
        public void Save(string PostureName)
        {
            if (_postureNames.Contains(PostureName) == false)
            {
                Database.UpperBody.Save(PostureName);
                _postureNames = Database.UpperBody.GetPostureNames();
            }
            else
            {
                MessageBox.Show($"Posture name ({PostureName}) already exists!");
            }
        }
        public void Delete(string PostureName)
        {
            if (_postureNames.Contains(PostureName))
            {
                Database.UpperBody.Delete(PostureName);
                _postureNames = Database.UpperBody.GetPostureNames();
            }
            else
            {
                MessageBox.Show($"Posture name ({PostureName}) does not exists!");
            }
        }
        public void DeleteAll()
        {
            Database.UpperBody.DeleteAll();
            _postureNames = null;
        }
    }
    public class Face
    {
        public enum Expression { NORMAL, HAPPY, SAD, ANGRY };
        public string Location { get; set; }

        public Face()
        {
            Location = Database.Settings.Read("NORMAL_FACE"); //does not exist
        }

        public string GetFace(Expression ex)
        {
            string[] paths = new string[] { Location, ex.ToString(), @".swf" };
            string path = Path.Combine(paths);
            if (File.Exists(path)) return path;
            else return null;
        }
    }
    public static class LattePandaCommunication
    {
        private static SerialPort _serialPort = null;
        public delegate void LatteDataReceivedHandler(object sender, LatteDataReceivedEventArgs e);
        public static event LatteDataReceivedHandler onLatteDataReceived;

        static LattePandaCommunication() { }
        public static void Initalise()
        {
            _serialPort = new SerialPort(Database.Settings.Read("ARDUINO_PORTNAME"));
            _serialPort.BaudRate = Convert.ToInt32(Database.Settings.Read("ARDUINO_BAUDRATE"));
            _serialPort.RtsEnable = true;
        }
        public static void Start()
        {
            try
            {
                if (_serialPort.IsOpen == false)
                {
                    _serialPort.Open();
                    _serialPort.DataReceived += _serialPort_DataReceived;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static void Stop()
        {
            try
            {
                if (_serialPort.IsOpen == true)
                {
                    _serialPort.Close();
                    _serialPort.DataReceived -= _serialPort_DataReceived;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        public static void Send(string text)
        {
            try
            {
                _serialPort.DiscardOutBuffer();
                _serialPort.DiscardInBuffer();
                _serialPort.WriteLine(text);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        public static void SendObjectAsJson(Object obj)
        {
            try
            {
                var jsonString = JsonConvert.SerializeObject(obj);

                Thread.Sleep(100);
                Send(jsonString);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private static void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            if (onLatteDataReceived != null)
            {
                try
                {
                    LatteDataReceivedEventArgs args = new LatteDataReceivedEventArgs(sp.ReadLine());
                    onLatteDataReceived(null, args);
                }
                catch { }
            }

        }
    }
    public class LatteDataReceivedEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public LatteDataReceivedEventArgs(string message)
        {
            Message = message;
        }
    }
    public class NavigationStatusEventArgs : EventArgs
    {
        public string Status { get; set; }
        public NavigationStatusEventArgs(string status)
        {
            Status = status;
        }

        public NavigationStatusEventArgs()
        {

        }
    }

    public class MapStatusEventArgs : EventArgs
    {
        public string Status { get; set; }
        public MapStatusEventArgs(string status)
        {
            Status = status;
        }

        public MapStatusEventArgs()
        {

        }
    }
}
