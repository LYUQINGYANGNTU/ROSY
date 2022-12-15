using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.IO;
using System.Data;
using System.Diagnostics;

namespace VoiceAssistantClient
{
    internal class VisionDatabaseRecorder
    {
        public static void WritetoDatabase(string Questions)
        {
            try
            {
                var cb = new SqlConnectionStringBuilder();
                cb.DataSource = "visiondatabaseserver.database.windows.net";
                cb.UserID = "visiondatabase";
                cb.Password = "QWE666qwe!";
                cb.InitialCatalog = "visiondatabase";

                using (var connection = new SqlConnection(cb.ConnectionString))
                {
                    Submit_Tsql_NonQuery(connection, "3 - Inserts", Build_Tsql_Inserts(Questions));

                    connection.Open();
                }
            }
            catch (SqlException e)
            {
                //MessageBox.Show(e.ToString());
            }
        }

        static void Submit_Tsql_NonQuery(
           SqlConnection connection,
           string tsqlPurpose,
           string tsqlSourceCode,
           string parameterName = null,
           string parameterValue = null)
        {

            using (var command = new SqlCommand(tsqlSourceCode, connection))
            {
                if (parameterName != null)
                {
                    command.Parameters.AddWithValue(  // Or, use SqlParameter class.
                        parameterName,
                        parameterValue);
                }
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine(rowsAffected + " = rows affected.");
            }
        }

        public static byte[] GetPhoto(string filePath)
        {
            FileStream stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            byte[] photo = reader.ReadBytes((int)stream.Length);

            reader.Close();
            stream.Close();

            return photo;
        }

        static string Build_Tsql_Inserts(string Questions)
        {
            return @"
        
        INSERT INTO VisionTable (IncidentType)
        VALUES

            ('" + Questions + @"');

            ";
        }

        public static void AddData(
          string IncidentType,
          string Date,
          string Time,
          string Location,
          string photoFilePath)
        {
            try
            {
                var cb = new SqlConnectionStringBuilder();
                cb.DataSource = "visiondatabaseserver.database.windows.net";
                cb.UserID = "visiondatabase";
                cb.Password = "QWE666qwe!";
                cb.InitialCatalog = "visiondatabase";

                byte[] photo = GetPhoto(photoFilePath);

                using (var connection = new SqlConnection(cb.ConnectionString))
                {

                    SqlCommand command = new SqlCommand(
                      "INSERT INTO VisionTable (IncidentType, Date, " +
                      "Time, Evidence, Location) " +
                      "Values(@IncidentType, @Date, @Time, " +
                      "@Evidence, @Location)", connection);

                    command.Parameters.Add("@IncidentType",
                       SqlDbType.Text).Value = IncidentType;
                    command.Parameters.Add("@Date",
                        SqlDbType.Text).Value = Date;
                    command.Parameters.Add("@Time",
                        SqlDbType.Text, 30).Value = Time;
                    command.Parameters.Add("@Evidence",
                        SqlDbType.Image, photo.Length).Value = photo;
                    command.Parameters.Add("@Location",
                        SqlDbType.Text).Value = Location;

                    connection.Open();
                    command.ExecuteNonQuery();

                    Debug.WriteLine("Update");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Denied" + ex);
            }
        }
    }
}
