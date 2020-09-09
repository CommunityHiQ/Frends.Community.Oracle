using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.Community.Oracle.Query.Tests
{
    /// <summary>
    /// THESE TESTS DO NOT WORK UNLESS YOU INSTALL ORACLE LOCALLY ON YOUR OWN COMPUTER!
    /// </summary>
    [TestFixture]
   //[Ignore("Cannot be run unless you have a properly configured Oracle DB running on your local computer")]
    public class OracleQueryTests
    {
        // Problems with local oracle, tests not implemented yet

        ConnectionProperties _conn = new ConnectionProperties
        {
            ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=<host>)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=<service name>)));User Id=<userid>;Password=<pass>;",
            TimeoutSeconds = 900
        };

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            using (var connection = new OracleConnection(_conn.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand("create table DecimalTest(DecimalValue decimal(38,35))", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = new OracleCommand("insert into DecimalTest (DecimalValue) values (1.12345678912345678912345678912345678)", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = new OracleCommand("create table HodorTest(name varchar2(15), value number(10,0))", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = new OracleCommand("insert all into HodorTest values('hodor', 123) into HodorTest values('jon', 321) select 1 from dual", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("create table Inserttest (Name varchar(20), Sendstatus varchar(5))", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("insert into Inserttest(Name, Sendstatus) values ('Han_1', '0')", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }


                using (var command = new OracleCommand("create table Inserttest_2 (Name varchar(20), Sendstatus varchar(5))", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("insert into Inserttest_2(Name, Sendstatus) values ('Han_2', '0')", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("create table duplicate_inserttest_table (PO_NR NUMBER PRIMARY KEY)", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("create table batch_table_test (NR number(20), NAM varchar(20))", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("create table batch_table_rollbacktest (NR NUMBER PRIMARY KEY, NAM varchar(20))", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("insert into batch_table_rollbacktest(NR, NAM) values (123, 'ShouldNotChange')", connection))
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
                using (var command = new OracleCommand("drop table DecimalTest", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = new OracleCommand("drop table InsertTest", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("drop table Inserttest_2", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("drop table duplicate_inserttest_table", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("drop table batch_table_test", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = new OracleCommand("drop table batch_table_rollbacktest", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [Test]
        [Category("Xml tests")]
        public async Task ShouldReturnXmlString()
        {
            var q = new QueryProperties { Query = @"select * from HodorTest" };
            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Xml,
                XmlOutput = new XmlOutputProperties
                {
                    RootElementName = "items",
                    RowElementName = "item"
                }
            };
            var options = new Options { ThrowErrorOnFailure = true };

            Output result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());

            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-16""?>
<items>
  <item>
    <NAME>hodor</NAME>
    <VALUE>123</VALUE>
  </item>
  <item>
    <NAME>jon</NAME>
    <VALUE>321</VALUE>
  </item>
</items>", result.Result);
        }

        [Test]
        [Category("Xml tests")]
        public async Task ShouldWriteXmlFile()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest" };
            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Xml,
                XmlOutput = new XmlOutputProperties
                {
                    RootElementName = "items",
                    RowElementName = "item"
                },
                OutputToFile = true,
                OutputFile = new OutputFileProperties
                {
                    Path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".xml")
                }
            };
            var options = new Options { ThrowErrorOnFailure = true };

            Output result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());

            Assert.IsTrue(File.Exists(result.Result), "should have created xml output file");
            Assert.AreEqual(
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<items>
  <item>
    <name>hodor</name>
    <value>123</value>
  </item>
  <item>
    <name>jon</name>
    <value>321</value>
  </item>
</items>",
                File.ReadAllText(result.Result));
            File.Delete(result.Result);
        }

        /// <summary>
        /// A simple query that fetches a decimal value from the database
        /// </summary>
        [Test]
        [Category("Json tests")]
        public async Task QueryDatabaseJSON()
        {
            var queryProperties = new QueryProperties { Query = "SELECT * FROM DecimalTest" };
            var outputProperties = new OutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties()
            };
            var options = new Options { ThrowErrorOnFailure = true };

            Output result = await QueryTask.Query(queryProperties, outputProperties, _conn, options, new CancellationToken());

            Assert.AreNotEqual("", result.Result);
            Assert.AreEqual(true, result.Success);
        }

        [Test]
        [Category("Json tests")]
        public async Task ShouldReturnJsonString()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest" };
            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = false
            };
            var options = new Options { ThrowErrorOnFailure = true };

            Output result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());

            Assert.IsTrue(string.Equals(result.Result, @"[
  {
    ""name"": ""hodor"",
    ""value"": 123
  },
  {
    ""name"": ""jon"",
    ""value"": 321
  }
]"));
        }

        [Test]
        [Category("Json tests")]
        public async Task ShouldWriteJsonFile()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest" };
            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = true,
                OutputFile = new OutputFileProperties
                {
                    Path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".json")
                }
            };
            var options = new Options { ThrowErrorOnFailure = true };

            Output result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());

            Assert.IsTrue(File.Exists(result.Result), "should have created json outputfile");
            Assert.AreEqual(@"[
  {
    ""name"": ""hodor"",
    ""value"": 123
  },
  {
    ""name"": ""jon"",
    ""value"": 321
  }
]",
                File.ReadAllText(result.Result));
            File.Delete(result.Result);
        }

        [Test]
        [Category("Csv tests")]
        public async Task ShouldReturnCsvString()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest" };
            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                }
            };
            var options = new Options { ThrowErrorOnFailure = true };

            Output result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());

            StringAssert.IsMatch(result.Result, "name;value\r\nhodor;123\r\njon;321\r\n");
        }

        [Test]
        [Category("Csv tests")]
        public async Task ShouldWriteCsvFile()
        {
            var q = new QueryProperties { Query = "select * from HodorTest" };
            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                },
                OutputToFile = true,
                OutputFile = new OutputFileProperties
                {
                    Path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".csv")
                }
            };
            var options = new Options { ThrowErrorOnFailure = true };

            Output result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());

            Assert.IsTrue(File.Exists(result.Result), "should have created csv output file");
            File.Delete(result.Result);
        }

        [Test]
        [Category("Isolation test")]
        public async Task IsolationTest1()
        {
            var q = new QueryProperties { Query = "select * from InsertTest" };



            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                },
                OutputToFile = false,
            };

            var options = new Options();
            options.ThrowErrorOnFailure = true;
            options.IsolationLevel = Oracle_IsolationLevel.ReadCommitted;


            Output result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());
            Assert.AreEqual(result.Result, "NAME;SENDSTATUS\r\nHan_1;0\r\n");

        }


        [Test]

        [Category("RollBackTest")]
        public async Task RollBackTest_1()
        {
            var q = new QueryProperties { Query = @"BEGIN
insert into duplicate_inserttest_table (po_nr)values ('111111');
insert into duplicate_inserttest_table (po_nr)values ('111111');
END;" };



            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                },
                OutputToFile = false,
            };

            var options = new Options();
            options.ThrowErrorOnFailure = true;
            options.IsolationLevel = Oracle_IsolationLevel.Serializable;
            Output result = new Output();
            Output result_debug = new Output();

            try
            {
                 result = await QueryTask.Query(q, o, _conn, options, new CancellationToken());
            }
            catch (Exception)
            {

                var q2 = new QueryProperties { Query = @"select * from duplicate_inserttest_table" };
                result_debug = await QueryTask.Query(q2, o, _conn, options, new CancellationToken());
                
            }

            Assert.AreEqual(result.Success, false);
            Assert.AreEqual(result_debug.Result, "");
        }


        [Test]

        [Category("BatchOperationTests")]
        public async Task BatchOperationInsertTest()
        {

            //t(NR varchar(20), NAM varchar(20))",
            var inputbatch = new InputBatchOperation { Query = @"BEGIN
insert into batch_table_test (NR,NAM)values(:NR,:NAM);
END;",  InputJson = "[{\"NR\": 111, \"NAM\":\"nannaa1\"},{\"NR\":222, \"NAM\":\"nannaa2\"},{\"NR\":333, \"NAM\":\"nannaa3\"},{\"NR\":444, \"NAM\":\"nannaa4\"}]" };

            var options = new BatchOptions();
            options.ThrowErrorOnFailure = true;
            options.IsolationLevel = Oracle_IsolationLevel.Serializable;

            BatchOperationOutput batch_output = new BatchOperationOutput();

            try
            {
                batch_output = await QueryTask.BatchOperation(inputbatch, _conn, options, new CancellationToken());
            }
            catch (Exception ee)
            {

                throw ee;

            }

            //Query rows from db, should be 2.
            var o = new OutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                },
                OutputToFile = false,
            };

            var q2 = new QueryProperties { Query = @"select count(*) as ROWCOUNT from batch_table_test" };
            var options_2 = new Options();
            options.ThrowErrorOnFailure = true;
            options.IsolationLevel = Oracle_IsolationLevel.Serializable;
            var result_debug = await QueryTask.Query(q2, o, _conn, options_2, new CancellationToken());



            Assert.AreEqual(result_debug.Result, "ROWCOUNT\r\n2\r\n");
        }

    }
}
