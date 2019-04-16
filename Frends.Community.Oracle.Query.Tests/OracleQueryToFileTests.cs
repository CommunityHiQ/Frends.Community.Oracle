using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Frends.Community.Oracle.Query.Tests
{
    [TestFixture]
    [Ignore("Cannot be run unless you have a properly configured Oracle DB running on your local computer")]
    public class OracleQueryToFileTests
    {

        ConnectionProperties _conn = new ConnectionProperties
        {
            ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=xe)));User Id=SYSTEM;Password=<<your password>>;",
            TimeoutSeconds = 300
        };

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            using (var connection = new OracleConnection(_conn.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand("create table HodorTest(Name varchar2(15), Value number(10,0), DecimalValue decimal(38,35), Inserted DATE)", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = new OracleCommand("insert all into HodorTest values('hodor', 123, 1.12345678912345678912345678912345678, TO_DATE('2019-12-09','YYYY-MM-DD')) into HodorTest values('jon', 321, 1.123456, TO_DATE('2019-12-09','YYYY-MM-DD')) select 1 from dual", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            using (var connection = new OracleConnection(_conn.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand("drop table HodorTest", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [Test]
        public async Task ShouldWriteCsvFile()
        {
            var q = new QueryProperties { Query = "select * from HodorTest" };
            var o = new SaveQueryToCsvOptions
            {
                OutputFilePath = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid() + ".csv"),
                FieldDelimiter = CsvFieldDelimiter.Pipe,
                LineBreak = CsvLineBreak.CRLF
            };
            var options = new Options { ThrowErrorOnFailure = true };

            var result = await QueryTask.QueryToFile(q, o, _conn, options, new CancellationToken());

            Assert.AreEqual(result.Success, true, "Should have returned true for success");
            File.Delete(result.Result);
        }
        
        [Test]
        public void FormatDbValue_String()
        {
            var options = new SaveQueryToCsvOptions { FieldDelimiter = CsvFieldDelimiter.Semicolon };
            // Basic case
            Assert.AreEqual(
                "\"hello, world\"",
                Extensions.FormatDbValue("hello, world", null, typeof(string), options));

            // Quotes should be escaped
            Assert.AreEqual(
                "\"hello\\\" world\"",
                Extensions.FormatDbValue("hello\" world", null, typeof(string), options));

            // Newlines should be replaced by spaces
            Assert.AreEqual(
                "\"hello world\"",
                Extensions.FormatDbValue("hello\rworld", null, typeof(string), options));
            Assert.AreEqual(
                "\"hello world\"",
                Extensions.FormatDbValue("hello\r\nworld", null, typeof(string), options));
            Assert.AreEqual(
                "\"hello world\"",
                Extensions.FormatDbValue("hello\nworld", null, typeof(string), options));
        }

        [Test]
        public void FormatDbValue_DateTime()
        {
            var options = new SaveQueryToCsvOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                DateFormat = "dd-MM_yyyy",
                DateTimeFormat = "dd-MM_yyyy HH:mm:ss",
                AddQuotesToDates = false,
            };

            // Date
            Assert.AreEqual(
                "31-12_2018",
                Extensions.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAte", typeof(DateTime), options));

            // Datetime
            Assert.AreEqual(
                "31-12_2018 11:22:33",
                Extensions.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAteTIME", typeof(DateTime), options));

            options.AddQuotesToDates = true;

            // Date
            Assert.AreEqual(
                "\"31-12_2018\"",
                Extensions.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAte", typeof(DateTime), options));

            // Datetime
            Assert.AreEqual(
                "\"31-12_2018 11:22:33\"",
                Extensions.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAteTIME", typeof(DateTime), options));
        }

        [Test]
        public void FormatDbValue_Nulls()
        {
            var options = new SaveQueryToCsvOptions();

            Assert.AreEqual(
                "",
                Extensions.FormatDbValue(null, "DOUBLE", typeof(double), options));

            // All string and date/datetime types should be quoted, including nulls
            Assert.AreEqual(
                "\"\"",
                Extensions.FormatDbValue(DBNull.Value, "DATE", typeof(DateTime), options));

            Assert.AreEqual(
                "\"\"",
                Extensions.FormatDbValue(DBNull.Value, "DATETIME", typeof(DateTime), options));
            Assert.AreEqual(
                "\"\"",
                Extensions.FormatDbValue(DBNull.Value, "NVARCHAR", typeof(string), options));
        }

        [Test]
        public void FormatDbValue_FloatDoubleDecimal()
        {
            var options = new SaveQueryToCsvOptions();
            // Float
            Assert.AreEqual(
                "1234.543",
                Extensions.FormatDbValue((float)1234.543, "FLOAT", typeof(float), options));
            // Double
            Assert.AreEqual(
                "1234.543",
                Extensions.FormatDbValue(1234.543, "DOUBLE", typeof(double), options));
            // Float
            Assert.AreEqual(
                "1234.543",
                Extensions.FormatDbValue((decimal)1234.543, "DECIMAL", typeof(decimal), options));
        }

        [Test]
        public void FormatDbHeader()
        {
            // Basic case
            Assert.AreEqual(
                "123_hello!!! THIS IS MADNESS",
                Extensions.FormatDbHeader("123_hello!!! THIS IS MADNESS", false));
            // Sanitize it!
            Assert.AreEqual(
                "hellothisis5anitiz3d_madness",
                Extensions.FormatDbHeader("123_hello!!! THIS IS 5aNiTiZ3D_MADNESS", true));
        }
    }
}
