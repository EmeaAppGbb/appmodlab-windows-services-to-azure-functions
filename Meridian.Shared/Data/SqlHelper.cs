using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace Meridian.Shared.Data
{
    public static class SqlHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SqlHelper));
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MeridianDB"]?.ConnectionString 
            ?? "Server=localhost;Database=MeridianCapital;Integrated Security=true;";

        public static DataTable ExecuteQuery(string query)
        {
            var dataTable = new DataTable();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error executing query: {query}", ex);
                throw;
            }
            return dataTable;
        }

        public static int ExecuteNonQuery(string query)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error executing non-query: {query}", ex);
                throw;
            }
        }

        public static object ExecuteScalar(string query)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    return command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error executing scalar: {query}", ex);
                throw;
            }
        }

        public static DataTable ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters)
        {
            var dataTable = new DataTable();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    connection.Open();
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error executing stored procedure: {procedureName}", ex);
                throw;
            }
            return dataTable;
        }

        public static int ExecuteStoredProcedureNonQuery(string procedureName, params SqlParameter[] parameters)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    connection.Open();
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error executing stored procedure non-query: {procedureName}", ex);
                throw;
            }
        }
    }
}
