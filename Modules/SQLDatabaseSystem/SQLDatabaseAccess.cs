using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web;

namespace NantCom.NancyBlack.Modules.SQLDatabaseSystem
{
    /// <summary>
    /// Utility class to access database
    /// </summary>
    public static class SQLDatabaseAccess
    {
        public class UpdateEntry
        {
            /// <summary>
            /// Table Name
            /// </summary>
            public string TableName { get; set; }

            /// <summary>
            /// Name of the key property
            /// </summary>
            public string KeyName { get; set; }

            /// <summary>
            /// Object to be updated
            /// </summary>
            public JObject Data { get; set; }
        }

        private static Type GetTypeFromJTokenType(JTokenType jt)
        {
            switch (jt)
            {
                case JTokenType.TimeSpan:
                    return typeof(TimeSpan);
                case JTokenType.Uri:
                    return typeof(Uri);
                case JTokenType.Boolean:
                    return typeof(bool);
                case JTokenType.Guid:
                    return typeof(Guid);
                case JTokenType.String:
                    return typeof(string);
                case JTokenType.Bytes:
                    return Type.GetType( "byte[]" );
                case JTokenType.Date:
                    return typeof(DateTime);
                case JTokenType.Float:
                    return typeof(double);
                case JTokenType.Integer:
                    return typeof(int);
                case JTokenType.Null:
                    return typeof(string);
                default:
                    throw new NotImplementedException("support for " + jt + " is not implemented.");
            }
        }

        /// <summary>
        /// Create new instance of SQLDatabaseAccess 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static string GetConnectionString( string server, string database, string username, string password)
        {
            return string.Format("Server={0};Database={1};User Id={2};Password={3};", server, database, username, password);
        }


        /// <summary>
        /// Reads the rows from database.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<dynamic> Read(string connectionString, string query)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;

                    using (var reader = cmd.ExecuteReader())
                    {
                        List<string> fieldNames = null;

                        while (reader.Read())
                        {
                            var row = new ExpandoObject();
                            var rowAccess = row as IDictionary<string, object>;

                            object[] values = new object[reader.FieldCount];
                            reader.GetValues(values);

                            if (fieldNames == null)
                            {
                                fieldNames = (from index in Enumerable.Range(0, reader.FieldCount)
                                              select reader.GetName(index)).ToList();
                            }

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                rowAccess[fieldNames[i]] = values[i];
                            }

                            yield return row;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate Update Command from give object
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="input"></param>
        /// <param name="keyGetter"></param>
        /// <returns></returns>
        private static SqlCommand GenerateUpdateCommand( UpdateEntry entry)
        {
            SqlCommand cmd = new SqlCommand();

            var command = new StringBuilder();
            command.AppendLine("UPDATE " + entry.TableName + " SET");

            foreach (var k in entry.Data.Properties())
            {
                if (k.Name.StartsWith("$") || k.Name == entry.KeyName)
                {
                    continue;
                }
                command.AppendLine(k.Name + " = @" + k.Name + ",");
            }

            command.Remove(command.Length - 3, 3);
            command.AppendLine("\r\nWHERE " + entry.KeyName + " = @IDParameter");

            cmd.CommandText = command.ToString();

            foreach (var k in entry.Data.Properties())
            {
                if (k.Name.StartsWith("$") || k.Name == entry.KeyName)
                {
                    continue;
                }
                var value = entry.Data[k.Name].ToObject(SQLDatabaseAccess.GetTypeFromJTokenType(entry.Data[k.Name].Type));

                if (value != null)
                {
                    cmd.Parameters.AddWithValue("@" + k.Name, value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@" + k.Name, DBNull.Value);
                }

            }

            cmd.Parameters.AddWithValue("@IDParameter",
                        entry.Data[entry.KeyName].ToObject(
                            SQLDatabaseAccess.GetTypeFromJTokenType(entry.Data[entry.KeyName].Type)));

            return cmd;
        }

        /// <summary>
        /// Update all input
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static void UpdateAll( string connectionString, IEnumerable<UpdateEntry> input )
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var trans = conn.BeginTransaction())
                {
                    foreach (var item in input)
                    {
                        var cmd = SQLDatabaseAccess.GenerateUpdateCommand(item);
                        cmd.Connection = conn;
                        cmd.Transaction = trans;
                        cmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                }
            }

        }

        /// <summary>
        /// Runs non query command
        /// </summary>
        /// <returns></returns>
        public static int NonQuery(string connectionString, string sqlCommand)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlCommand;
                    return cmd.ExecuteNonQuery();
                }
            }
        }

    }
}