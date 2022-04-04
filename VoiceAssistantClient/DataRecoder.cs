using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Common;


namespace VoiceAssistantClient
{
    public static class DataRecoder
    {
        public static void WritetoDatabase(string Questions)
        {
            try
            {
                var cb = new SqlConnectionStringBuilder();
                cb.DataSource = "pixadb.database.windows.net";
                cb.UserID = "lyuqingyang";
                cb.Password = "LQY123456!";
                cb.InitialCatalog = "pixadb";

                using (var connection = new SqlConnection(cb.ConnectionString))
                {
                    connection.Open();

                    //Submit_Tsql_NonQuery(connection, "2 - Create-Tables", Build_2_Tsql_CreateTables());

                    Submit_Tsql_NonQuery(connection, "3 - Inserts", Build_3_Tsql_Inserts(Questions));

                    //Submit_Tsql_NonQuery(connection, "4 - Update-Join", Build_4_Tsql_UpdateJoin(),
                    //"@csharpParmDepartmentName", "Accounting");

                    //Submit_Tsql_NonQuery(connection, "5 - Delete-Join", Build_5_Tsql_DeleteJoin(),
                    //"@csharpParmDepartmentName", "Legal");

                    //Submit_6_Tsql_SelectEmployees(connection);
                }
            }
            catch (SqlException e)
            {
                //MessageBox.Show(e.ToString());
            }
        }


        static void Submit_6_Tsql_SelectEmployees(SqlConnection connection)
        {
            Console.WriteLine();
            Console.WriteLine("=================================");
            Console.WriteLine("Now, SelectEmployees (6)...");

            string tsql = Build_6_Tsql_SelectEmployees();

            using (var command = new SqlCommand(tsql, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("{0} , {1} , {2} , {3} , {4}",
                            reader.GetGuid(0),
                            reader.GetString(1),
                            reader.GetInt32(2),
                            (reader.IsDBNull(3)) ? "NULL" : reader.GetString(3),
                            (reader.IsDBNull(4)) ? "NULL" : reader.GetString(4));
                    }
                }
            }
        }

        static void Submit_Tsql_NonQuery(
            SqlConnection connection,
            string tsqlPurpose,
            string tsqlSourceCode,
            string parameterName = null,
            string parameterValue = null
            )
        {
            Console.WriteLine();
            Console.WriteLine("=================================");
            Console.WriteLine("T-SQL to {0}...", tsqlPurpose);

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

        static string Build_2_Tsql_CreateTables()
        {
            return @"
        DROP TABLE IF EXISTS tabQuestions;
        
        CREATE TABLE tabQuestions
        (
            RecoredQuestions  nvarchar(128)     not null    PRIMARY KEY
        );
    ";
        }

        static string Build_3_Tsql_Inserts(string Questions)
        {
            return @"
        
        INSERT INTO tabQuestions (RecoredQuestions)
        VALUES

            ('" + Questions + @"');

    ";
        }

        static string Build_4_Tsql_UpdateJoin()
        {
            return @"
        DECLARE @DName1  nvarchar(128) = @csharpParmDepartmentName;  --'Accounting';

        -- Promote everyone in one department (see @parm...).
        UPDATE empl
        SET
            empl.EmployeeLevel += 1
        FROM
            tabEmployee   as empl
        INNER JOIN
            tabDepartment as dept ON dept.DepartmentCode = empl.DepartmentCode
        WHERE
            dept.DepartmentName = @DName1;
    ";
        }

        static string Build_5_Tsql_DeleteJoin()
        {
            return @"
        DECLARE @DName2  nvarchar(128);
        SET @DName2 = @csharpParmDepartmentName;  --'Legal';

        -- Right size the Legal department.
        DELETE empl
        FROM
            tabEmployee   as empl
        INNER JOIN
            tabDepartment as dept ON dept.DepartmentCode = empl.DepartmentCode
        WHERE
            dept.DepartmentName = @DName2

        -- Disband the Legal department.
        DELETE tabDepartment
            WHERE DepartmentName = @DName2;
    ";
        }

        static string Build_6_Tsql_SelectEmployees()
        {
            return @"
        -- Look at all the final Employees.
        SELECT
            empl.EmployeeGuid,
            empl.EmployeeName,
            empl.EmployeeLevel,
            empl.DepartmentCode,
            dept.DepartmentName
        FROM
            tabEmployee   as empl
        LEFT OUTER JOIN
            tabDepartment as dept ON dept.DepartmentCode = empl.DepartmentCode
        ORDER BY
            EmployeeName;
    ";
        }
    }
}
