using System;
using System.Data;
using System.Threading;
using System.Linq;
using Oracle.ManagedDataAccess.Client;

namespace Frends.Community.Oracle.Tests.Lib
{
    internal static class Helpers
    {
        /// <summary>
        /// This methods waits for the docker container to be ready. 
        /// Method tests connection 20 times and if connection can't be made, it waits for a minute after every attempt
        /// and tries again. This is needed for the CI for it to wait for the container to be ready.
        /// The docker building script will also wait for 15 minutes.  
        /// </summary>
        internal static void TestConnectionBeforeRunningTests(string connectionString)
        {
            using (var con = new OracleConnection(connectionString))
            {
                foreach (var i in Enumerable.Range(1, 20))
                {
                    try { con.Open(); }
                    catch
                    {
                        if (con.State == ConnectionState.Open)
                            break;

                        Console.WriteLine("Wait for a minute before trying again.");
                        Thread.Sleep(60000);
                    }
                }
                if (con.State != ConnectionState.Open)
                    throw new Exception("Check that the docker container is up and running.");
                con.Close();
                OracleConnection.ClearPool(con);
            }   
        }

        internal static void CreateTestTable(OracleConnection con)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = @"create table test_user.workers(" +
                        "id NUMBER, " +
                        "first_name varchar2(50), " +
                        "last_name varchar2(50), " +
                        "start_date date, " +
                        "primary key(id))";
                cmd.ExecuteNonQuery();
            } 
        }

        internal static void DropTestTable(OracleConnection con)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "drop table test_user.workers";

                cmd.CommandType = CommandType.Text;

                cmd.ExecuteNonQuery();
            }
        }

        internal static void CreateTestUser(OracleConnection con)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
create or replace procedure p_create_user (par_username in varchar2) is
begin
  execute immediate ('create user '    || par_username ||
                     ' identified by ' || par_username ||
                     ' temporary tablespace temp '     ||
                     ' profile default ');
  execute immediate ('grant create session to ' || par_username);
  execute immediate ('grant create table to ' || par_username);
  execute immediate ('grant create procedure to ' || par_username);
  execute immediate ('grant unlimited tablespace to ' || par_username);
end;";

                cmd.CommandType = CommandType.Text;

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
begin
  p_create_user('test_user');
end;";
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    DropTestUser(con);
                    cmd.ExecuteNonQuery();
                }
            }
                
        }

        internal static void DropTestUser(OracleConnection con)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "DROP USER test_user CASCADE";
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }
    }
}
