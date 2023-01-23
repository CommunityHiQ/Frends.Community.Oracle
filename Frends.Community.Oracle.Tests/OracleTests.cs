using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Frends.Community.Oracle.Tests.Lib;

namespace Frends.Community.Oracle.Query.Tests
{

    /// <summary>
    /// To run the test, you need to build docker container with following build script.
    /// run
    /// ./_build/deploy_oracle_docker_container.sh
    /// </summary>

    [TestFixture]
    public class OracleTests
    {
        /// <summary>
        /// Connection string for Oracle database
        /// </summary>
        private readonly static string _schema = "test_user";
        private readonly static string ConnectionString = $"Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 51521))(CONNECT_DATA = (SERVICE_NAME = XEPDB1))); User Id = {_schema}; Password={_schema};";
        private readonly static string _connectionStringSys = "Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 51521))(CONNECT_DATA = (SERVICE_NAME = XEPDB1))); User Id = sys; Password=mysecurepassword; DBA PRIVILEGE=SYSDBA";

        private readonly string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../TestOut/");
        private readonly string expectedFileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../ExpectedResults/");

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // This is needed to ensure that the DB is up and running.
            Helpers.TestConnectionBeforeRunningTests(_connectionStringSys);

            using (var con = new OracleConnection(_connectionStringSys))
            {
                con.Open();
                Helpers.CreateTestUser(con);
                con.Close();
                con.Dispose();
                OracleConnection.ClearPool(con);
            }
            using (var connection = new OracleConnection(ConnectionString))
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

                using (var command = new OracleCommand("create table duplicate_inserttest_table2 (PO_NR NUMBER PRIMARY KEY)", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                connection.Close();
                connection.Dispose();
                OracleConnection.ClearPool(connection);
            }
            Directory.CreateDirectory(outputDirectory);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            using (var connection = new OracleConnection(ConnectionString))
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

                using (var command = new OracleCommand("drop table duplicate_inserttest_table2", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new OracleCommand("drop table batch_table_test", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                connection.Close();
                connection.Dispose();
                OracleConnection.ClearPool(connection);
            }

            using (var con = new OracleConnection(_connectionStringSys))
            {
                con.Open();
                Helpers.DropTestUser(con);
                con.Close();
                con.Dispose();
                OracleConnection.ClearPool(con);
            }

            foreach (var file in Directory.GetFiles(outputDirectory, "*"))
            {
                File.Delete(outputDirectory + file);
            }
            Directory.Delete(outputDirectory, true);
        }

        // Test needs new table new_table which has column named 'test1' with apostrophe in column name.
        // This needs to be added manually.
        [Ignore("Test needs data with invalid xml characters in column name.")]
        [Test]
        [Category("Xml tests")]
        public async Task ShouldWorkWithApostropheInColumnName()
        {
            var q = new QueryProperties { Query = "select * from new_test", ConnectionString = ConnectionString };
            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Xml,
                XmlOutput = new XmlOutputProperties
                {
                    RootElementName = "items",
                    RowElementName = "item"
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());
            Assert.IsTrue(result.Result.Contains("<_test1_>testi</_test1_>"));
        }

        [Test]
        [Category("Xml tests")]
        public async Task ShouldReturnXmlString()
        {
            var q = new QueryProperties { Query = @"select * from HodorTest", ConnectionString = ConnectionString };
            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Xml,
                XmlOutput = new XmlOutputProperties
                {
                    RootElementName = "items",
                    RowElementName = "item"
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());
            var expected = File.ReadAllText(Path.Combine(expectedFileDirectory, "ExpectedUtf16Xml.xml"));
            Assert.AreEqual(expected.Replace("\r", ""), result.Result);
        }

        [Test]
        [Category("Xml tests")]
        public async Task ShouldWriteXmlFile()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest", ConnectionString = ConnectionString };
            var o = new QueryOutputProperties
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
                    Path = Path.Combine(outputDirectory, Guid.NewGuid().ToString() + ".xml")
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());

            Assert.IsTrue(File.Exists(result.Result), "should have created xml queryOutput file");
            var expected = File.ReadAllText(Path.Combine(expectedFileDirectory, "ExpectedUtf8Xml.xml"));
            Assert.AreEqual(expected.Replace("\r", ""), File.ReadAllText(result.Result));
            File.Delete(result.Result);
        }

        /// <summary>
        /// A simple query that fetches a decimal value from the database.
        /// </summary>
        [Test]
        [Category("Json tests")]
        public async Task QueryDatabaseJSON()
        {
            var queryProperties = new QueryProperties { Query = "SELECT * FROM DecimalTest", ConnectionString = ConnectionString };
            var outputProperties = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties()
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(queryProperties, outputProperties, options, new CancellationToken());

            Assert.AreNotEqual("", result.Result);
            Assert.AreEqual(true, result.Success);
        }

        [Test]
        [Category("Json tests")]
        public async Task ShouldReturnJsonString()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest", ConnectionString = ConnectionString };
            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = false
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());
            var expected = File.ReadAllText(Path.Combine(expectedFileDirectory, "ExpectedJson.json"));
            Assert.AreEqual(expected.Replace("\r", ""), result.Result);
        }

        [Test]
        [Category("Json tests")]
        public async Task ShouldWriteJsonFile()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest", ConnectionString = ConnectionString };
            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = true,
                OutputFile = new OutputFileProperties
                {
                    Path = Path.Combine(outputDirectory, Guid.NewGuid().ToString() + ".json")
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());

            Assert.IsTrue(File.Exists(result.Result), "should have created json outputfile");
            var expected = File.ReadAllText(Path.Combine(expectedFileDirectory, "ExpectedJson.json"));
            Assert.AreEqual(expected.Replace("\r", ""), File.ReadAllText(result.Result));
            File.Delete(result.Result);
        }

        [Test]
        [Category("Csv tests")]
        public async Task ShouldReturnCsvString()
        {
            var q = new QueryProperties { Query = @"select name as ""name"", value as ""value"" from HodorTest", ConnectionString = ConnectionString };
            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());

            StringAssert.IsMatch(result.Result, "name;value\nhodor;123\njon;321\n");
        }

        [Test]
        [Category("Csv tests")]
        public async Task ShouldWriteCsvFile()
        {
            var q = new QueryProperties { Query = "select * from HodorTest", ConnectionString = ConnectionString };
            var o = new QueryOutputProperties
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
                    Path = Path.Combine(outputDirectory, Guid.NewGuid().ToString() + ".csv")
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());

            Assert.IsTrue(File.Exists(result.Result), "should have created csv queryOutput file");
            File.Delete(result.Result);
        }

        [Test]
        [Category("Isolation test")]
        public async Task IsolationTest1()
        {
            var q = new QueryProperties { Query = "select * from InsertTest", ConnectionString = ConnectionString };

            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                },
                OutputToFile = false,
            };

            var options = new QueryOptions
            {
                ThrowErrorOnFailure = true,
                IsolationLevel = Oracle_IsolationLevel.ReadCommitted
            };

            var result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());
            Assert.AreEqual("NAME;SENDSTATUS\nHan_1;0\n", result.Result);

        }


        [Test]

        [Category("RollBackTest")]
        public async Task RollBackTest_1()
        {
            var q = new QueryProperties { Query = @"
                BEGIN
                insert into duplicate_inserttest_table (po_nr)values ('1');
                insert into duplicate_inserttest_table2 (po_nr)values ('2');
                insert into duplicate_inserttest_table2 (po_nr)values ('2');
                END;", ConnectionString = ConnectionString };

            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                },
                OutputToFile = false,
            };

            var options = new QueryOptions
            {
                ThrowErrorOnFailure = true,
                IsolationLevel = Oracle_IsolationLevel.None
            };
            var result = new Output();
            var result_debug = new Output();
            var ex_string = "";

            try
            {
                result = await OracleTasks.ExecuteQueryOracle(q, o, options, new CancellationToken());
            }
            catch (Exception ee)
            {

                ex_string = ee.ToString();

                var q2 = new QueryProperties { Query = @"select * from duplicate_inserttest_table", ConnectionString = ConnectionString };
                result_debug = await OracleTasks.ExecuteQueryOracle(q2, o, options, new CancellationToken());

            }

            Assert.AreEqual(ex_string.Contains("ORA-00001: unique constraint"), true);
            Assert.AreEqual(result.Success, false);
            Assert.AreEqual(result_debug.Result, "");
        }

        [Test]
        [Category("BatchOperationTests")]
        public async Task BatchOperationInsertTest()
        {

            //t(NR varchar(20), NAM varchar(20))",
            var inputbatch = new InputBatchOperation
            {
                Query = @"BEGIN
                insert into batch_table_test (NR,NAM)values(:NR,:NAM);
                END;",
                InputJson = "[{\"NR\": 111, \"NAM\":\"nannaa1\"},{\"NR\":222, \"NAM\":\"nannaa2\"},{\"NR\":333, \"NAM\":\"nannaa3\"}, {\"NR\":444, \"NAM\":\"nannaa4\"}]",
                ConnectionString = ConnectionString
            };

            var options = new BatchOptions
            {
                ThrowErrorOnFailure = true,
                IsolationLevel = Oracle_IsolationLevel.Serializable
            };

            BatchOperationOutput batch_output;

            try
            {
                batch_output = await OracleTasks.BatchOperationOracle(inputbatch, options, new CancellationToken());
            }
            catch (Exception ee)
            {
                throw ee;
            }

            // ExecuteQueryOracle rows from db, should be 2.
            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Csv,
                CsvOutput = new CsvOutputProperties
                {
                    CsvSeparator = ";",
                    IncludeHeaders = true
                },
                OutputToFile = false,
            };

            var q2 = new QueryProperties { Query = @"select count(*) as ROWCOUNT from batch_table_test", ConnectionString = ConnectionString };
            var options_2 = new QueryOptions();
            options.ThrowErrorOnFailure = true;
            options.IsolationLevel = Oracle_IsolationLevel.Serializable;
            var result_debug = await OracleTasks.ExecuteQueryOracle(q2, o, options_2, new CancellationToken());

            Assert.AreEqual("ROWCOUNT\n4\n", result_debug.Result);

        }

        /// <summary>
        /// Two simple select querys to the database.
        /// </summary>
        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiQueryJSON()
        {
            var multiQueryProperties = new InputMultiQuery { Queries = new InputQuery[] { new InputQuery { InputQueryString = "SELECT * FROM DecimalTest" }, new InputQuery { InputQueryString = "SELECT * FROM HodorTest" } }, ConnectionString = ConnectionString };
            var outputProperties = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties()
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.TransactionalMultiQuery(multiQueryProperties, outputProperties, options, new CancellationToken());

            Assert.AreNotEqual("", result.Results);
            Assert.AreEqual(true, result.Success);
        }

        /// <summary>
        /// Two simple select querys to the db with isolationlevel = serializable.
        /// </summary>
        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiQueryJSONIsolation()
        {
            var multiQueryProperties = new InputMultiQuery
            {
                Queries = new InputQuery[] { new InputQuery { InputQueryString = "SELECT * FROM DecimalTest" }, new InputQuery { InputQueryString = "SELECT * FROM HodorTest" },
                new InputQuery { InputQueryString = "INSERT INTO HodorTest values('test', 890)" }, new InputQuery { InputQueryString ="DELETE FROM HodorTest WHERE value = 890" } },
                ConnectionString = ConnectionString
            };

            var outputProperties = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = false,

            };
            var options = new QueryOptions { ThrowErrorOnFailure = true, IsolationLevel = Oracle_IsolationLevel.Serializable };

            var result = await OracleTasks.TransactionalMultiQuery(multiQueryProperties, outputProperties, options, new CancellationToken());

            Assert.AreEqual(result.Results.First?["Output"]?.ToString(), "[\n  {\n    \"DECIMALVALUE\": 1.123456789123456789123456789\n  }\n]");
            Assert.AreEqual(true, result.Success);
        }

        /// <summary>
        /// Check if corrupted query is rolled back.
        /// </summary>
        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiQuerRollback()
        {
            var multiQueryProperties = new InputMultiQuery { Queries = new InputQuery[] { new InputQuery { InputQueryString = "insert into DecimalTest(DecimalValue) values(10.6)" }, new InputQuery { InputQueryString = "SELECT * FROM foo" } }, ConnectionString = ConnectionString };
            var outputProperties = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = true,
                OutputFile = new OutputFileProperties
                {
                    Path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".json")
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true, IsolationLevel = Oracle_IsolationLevel.Serializable };

            var result = new MultiQueryOutput();

            try
            {
                result = await OracleTasks.TransactionalMultiQuery(multiQueryProperties, outputProperties, options, new CancellationToken());

            }

            catch (Exception)
            {

            }
            var multiQueryProperties2 = new InputMultiQuery { Queries = new InputQuery[] { new InputQuery { InputQueryString = "SELECT * FROM DecimalTest" } }, ConnectionString = ConnectionString };
            var outputProperties2 = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties()
            };
            var options2 = new QueryOptions { ThrowErrorOnFailure = true };

            var result2 = await OracleTasks.TransactionalMultiQuery(multiQueryProperties2, outputProperties2, options2, new CancellationToken());

            Assert.That(() => OracleTasks.TransactionalMultiQuery(multiQueryProperties, outputProperties, options, new CancellationToken()), Throws.TypeOf<OracleException>());
            Assert.AreEqual(false, result.Success);
            Assert.AreNotEqual(2, result2.Results.Count);

            File.Delete(outputProperties.OutputFile.Path);
        }

        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiqueryShouldWriteJsonFile()
        {
            var multiQueryProperties = new InputMultiQuery
            {
                Queries = new InputQuery[] { new InputQuery { InputQueryString = "SELECT * FROM DecimalTest" }, new InputQuery { InputQueryString = "SELECT * FROM HodorTest" },
                new InputQuery { InputQueryString = "INSERT INTO HodorTest values('test', 890)" }, new InputQuery { InputQueryString = "DELETE FROM HodorTest WHERE value = 890" } },
                ConnectionString = ConnectionString
            };
            var outputProperties = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = true,
                OutputFile = new OutputFileProperties
                {
                    Path = Path.Combine(outputDirectory, Guid.NewGuid().ToString() + ".json")
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.TransactionalMultiQuery(multiQueryProperties, outputProperties, options, new CancellationToken());

            Assert.IsTrue(File.Exists(outputProperties.OutputFile.Path));
            Assert.IsTrue(File.Exists(outputProperties.OutputFile.Path));
            Assert.IsTrue(File.ReadAllText(result.Results.First?["OutputPath"]?.ToString()).Contains("1.123456789123456789123456789"));

            File.Delete(outputProperties.OutputFile.Path);
        }

        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiqueryShouldWriteCSVFile()
        {
            var multiQueryProperties = new InputMultiQuery
            {
                Queries = new InputQuery[] { new InputQuery { InputQueryString = "SELECT * FROM DecimalTest" }, new InputQuery { InputQueryString = "SELECT * FROM HodorTest" },
                new InputQuery { InputQueryString = "INSERT INTO HodorTest values('test', 890)" }, new InputQuery { InputQueryString = "DELETE FROM HodorTest WHERE value = 890" } },
                ConnectionString = ConnectionString
            };
            var outputProperties = new QueryOutputProperties
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
                    Path = Path.Combine(outputDirectory, Guid.NewGuid().ToString() + ".json")
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.TransactionalMultiQuery(multiQueryProperties, outputProperties, options, new CancellationToken());

            Assert.IsTrue(File.Exists(outputProperties.OutputFile.Path));
            Assert.IsTrue(File.ReadAllText(result.Results.First?["OutputPath"]?.ToString()).Contains("1.123456789123456789123456789"));
            File.Delete(outputProperties.OutputFile.Path);
        }

        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiqueryShouldWriteXMLFile()
        {
            var multiQueryProperties = new InputMultiQuery
            {
                Queries = new InputQuery[] { new InputQuery { InputQueryString = "SELECT * FROM DecimalTest" }, new InputQuery { InputQueryString = "SELECT * FROM HodorTest" },
                new InputQuery { InputQueryString = "INSERT INTO HodorTest values('test', 890)" }, new InputQuery { InputQueryString = "DELETE FROM HodorTest WHERE value = 890" } },
                ConnectionString = ConnectionString
            };
            var outputProperties = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Xml,
                OutputToFile = true,
                XmlOutput = new XmlOutputProperties
                {
                    RootElementName = "items",
                    RowElementName = "item"
                },
                OutputFile = new OutputFileProperties
                {
                    Path = Path.Combine(outputDirectory, Guid.NewGuid().ToString() + ".json")
                }
            };
            var options = new QueryOptions { ThrowErrorOnFailure = true };

            var result = await OracleTasks.TransactionalMultiQuery(multiQueryProperties, outputProperties, options, new CancellationToken());

            Assert.IsTrue(File.Exists(outputProperties.OutputFile.Path));
            Assert.IsTrue(File.ReadAllText(result.Results.First?["OutputPath"]?.ToString()).Contains("<DECIMALVALUE>1.12345678912345678912345678912345678</DECIMALVALUE>"));
            File.Delete(outputProperties.OutputFile.Path);
        }

        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiBatchOperationInsertTest()
        {
            var inputbatch = new InputMultiBatchOperation
            {
                BatchQueries = new BatchOperationQuery[] {
                new BatchOperationQuery {BatchInputQuery = @"delete from batch_table_test", InputJson = ""},
                new BatchOperationQuery { BatchInputQuery = @"insert into batch_table_test (NR,NAM)values(:NR,:NAM)", InputJson = "[{\"NR\": 111, \"NAM\":\"nannaa1\"},{\"NR\":222, \"NAM\":\"nannaa2\"},{\"NR\":333, \"NAM\":\"nannaa3\"}, {\"NR\":444, \"NAM\":\"nannaa4\"}]" },
                new BatchOperationQuery { BatchInputQuery = @"insert into batch_table_test (NR,NAM)values(:NR,:NAM)", InputJson = "[{\"NR\": 555, \"NAM\":\"nannaa1\"},{\"NR\":666, \"NAM\":\"nannaa2\"}]" }
            },

                ConnectionString = ConnectionString
            };

            var options = new BatchOptions
            {
                ThrowErrorOnFailure = true,
                IsolationLevel = Oracle_IsolationLevel.Serializable
            };

            MultiBatchOperationOutput output;

            try
            {
                output = await OracleTasks.MultiBatchOperationOracle(inputbatch, options, new CancellationToken());
            }
            catch (Exception ee)
            {
                throw ee;
            }

            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = false
            };

            var q2 = new QueryProperties { Query = @"select count(*) as ROWCOUNT from batch_table_test", ConnectionString = ConnectionString };
            var options_2 = new QueryOptions();
            options.ThrowErrorOnFailure = true;
            options.IsolationLevel = Oracle_IsolationLevel.Serializable;
            var result_debug = await OracleTasks.ExecuteQueryOracle(q2, o, options_2, new CancellationToken());

            Assert.AreEqual(result_debug.Result, "[\n  {\n    \"ROWCOUNT\": 6.0\n  }\n]");
        }

        /// <summary>
        /// Check if corrupted query is rolled back.
        /// </summary>
        [Test]
        [Category("MultiqueryTests")]
        public async Task MultiBatchOperationRollback()
        {

            var inputbatch = new InputMultiBatchOperation
            {
                BatchQueries = new BatchOperationQuery[] {
                new BatchOperationQuery {BatchInputQuery = @"delete from batch_table_test", InputJson = ""},
                new BatchOperationQuery { BatchInputQuery = @"insert into batch_table_test (NR,NAM)values(:NR,:NAM)", InputJson = "[{\"NR\": 111, \"NAM\":\"nannaa1\"},{\"NR\":222, \"NAM\":\"nannaa2\"},{\"NR\":333, \"NAM\":\"nannaa3\"}, {\"NR\":444, \"NAM\":\"nannaa4\"}]" },
                new BatchOperationQuery { BatchInputQuery = @"insert into batch_table_testfoo (NR,NAM)values(:NR,:NAM)", InputJson = "[{\"NR\": 555, \"NAM\":\"nannaa1\"},{\"NR\":666, \"NAM\":\"nannaa2\"}]" }
            },

                ConnectionString = ConnectionString
            };

            var options = new BatchOptions
            {
                ThrowErrorOnFailure = true,
                IsolationLevel = Oracle_IsolationLevel.Serializable
            };

            MultiBatchOperationOutput output = new MultiBatchOperationOutput();

            try
            {
                output = await OracleTasks.MultiBatchOperationOracle(inputbatch, options, new CancellationToken());
            }
            catch (Exception)
            {

            }
            var multiQueryProperties2 = new InputMultiQuery { Queries = new InputQuery[] { new InputQuery { InputQueryString = "SELECT * FROM " } }, ConnectionString = ConnectionString };
            var outputProperties2 = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties()
            };
            var options2 = new QueryOptions { ThrowErrorOnFailure = true };

            var o = new QueryOutputProperties
            {
                ReturnType = QueryReturnType.Json,
                JsonOutput = new JsonOutputProperties(),
                OutputToFile = false
            };

            var q2 = new QueryProperties { Query = @"select count(*) as ROWCOUNT from batch_table_test", ConnectionString = ConnectionString };
            var options_2 = new QueryOptions();
            options.ThrowErrorOnFailure = true;
            options.IsolationLevel = Oracle_IsolationLevel.Serializable;
            var result_debug = await OracleTasks.ExecuteQueryOracle(q2, o, options_2, new CancellationToken());

            Assert.That(() => OracleTasks.MultiBatchOperationOracle(inputbatch, options, new CancellationToken()), Throws.TypeOf<OracleException>());
            Assert.AreEqual(false, output.Success);
            Assert.AreEqual(result_debug.Result, "[\n  {\n    \"ROWCOUNT\": 4.0\n  }\n]");

        }
    }
}
