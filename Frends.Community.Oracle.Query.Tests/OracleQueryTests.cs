using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frends.Community.Oracle.Query.Tests
{
    /// <summary>
    /// THESE TESTS DO NOT WORK UNLESS YOU INSTALL ORACLE LOCALLY ON YOUR OWN COMPUTER!
    /// </summary>
    [TestFixture]
    public class OracleQueryTests
    {
        // Problems with local oracle, tests not implemented yet

        ConnectionProperties _conn;

        [SetUp]
        public void Setup()
        {
            _conn = new ConnectionProperties();
            _conn.ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=xe)));User Id=SYSTEM;Password=<YourPasswordHere>;";
            _conn.TimeoutSeconds = 300;
        }

        /// <summary>
        /// A simple query that fetches a decimal value from the database
        /// </summary>
        [Test]
        [Ignore("Cannot be run unless you have a properly configured Oracle DB running on your local computer")]
        public void QueryDatabaseJSON()
        {
            /*
            Run the following statements before running this test:
            CREATE TABLE DecimalTest
            (
                DecimalValue DECIMAL(38,35)
            )

            INSERT INTO DecimalTest (DecimalValue) VALUES (1.12345678912345678912345678912345678)
            */

            QueryProperties Properties = new QueryProperties();
            Properties.Query = "SELECT * FROM DecimalTest";
            Properties.ReturnType = QueryReturnType.Json;

            Options Options = new Options();
            Options.ThrowErrorOnFailure = true;

            var Result = QueryTask.Query(_conn, Properties, Options);

            Assert.AreNotEqual("", Result.Result);
            Assert.AreEqual(true, Result.Success);
        }
    }
}
