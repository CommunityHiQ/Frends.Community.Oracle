using NUnit.Framework;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace Frends.Community.Oracle.Query.Tests
{
    [TestFixture]
    public class OracleQueryToFileTests
    {
        [Test]
        public void DataReaderToCsvTest_AllColumns()
        {
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {
                new DataColumn("col_string", typeof(string)),
                new DataColumn("col_datetime", typeof(DateTime)),
                new DataColumn("col_float", typeof(float))
            });

            dt.Rows.Add("Hello\"semicolon1", DateTime.Parse("2018-12-31T11:22:33"), 3000.212);
            dt.Rows.Add("Hello\"semicolon2", DateTime.Parse("2018-12-31T11:22:34"), 3000.212);

            var options = new SaveQueryToCsvOptions
            {
                DateFormat = "MM-dd-yyyy",
                DateTimeFormat = "MM-dd-yyyy HH:mm:ss",
                ColumnsToInclude = new string[0],
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                AddQuotesToDates = false
            };

            using (var writer = new StringWriter())
            using (var csvFile = Extensions.CreateCsvWriter(options.GetFieldDelimiterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                var entries = Extensions.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split("\r\n");

                // 4 lines = 1 header line + 2 data lines + 1 newline at end of file
                Assert.AreEqual(4, resultLines.Length);
                Assert.AreEqual(2, entries);
                Assert.AreEqual("col_string;col_datetime;col_float", resultLines[0]);
                Assert.AreEqual("\"Hello\\\"semicolon1\";12-31-2018 11:22:33;3000.212", resultLines[1]);
                Assert.AreEqual("\"Hello\\\"semicolon2\";12-31-2018 11:22:34;3000.212", resultLines[2]);
            }
        }

        [Test]
        public void DataReaderToCsvTest_ExcludeColumnHeaders()
        {
            var dt = new DataTable();
            dt.Columns.Add(new DataColumn("col_string", typeof(string)));
            dt.Rows.Add("test");

            var options = new SaveQueryToCsvOptions { IncludeHeadersInOutput = false };
            using (var writer = new StringWriter())
            using (var csvFile = Extensions.CreateCsvWriter(options.GetFieldDelimiterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                Extensions.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split("\r\n");

                // 2 lines = 0 header lines + 1 data lines + 1 newline at end of file
                Assert.AreEqual(2, resultLines.Length);
                Assert.AreEqual("\"test\"", resultLines[0]);
            }
        }

        [Test]
        public void DataReaderToCsvTest_SanitizeColumnHeaders()
        {
            var dt = new DataTable();
            dt.Columns.Add(new DataColumn("COL_STRING", typeof(string)));
            dt.Rows.Add("test");

            var options = new SaveQueryToCsvOptions { SanitizeColumnHeaders = true };
            using (var writer = new StringWriter())
            using (var csvFile = Extensions.CreateCsvWriter(options.GetFieldDelimiterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                Extensions.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split("\r\n");

                // 3 lines = 1 header lines + 1 data lines + 1 newline at end of file
                Assert.AreEqual(3, resultLines.Length);
                Assert.AreEqual("col_string", resultLines[0]);
            }
        }

        [Test]
        public void DataReaderToCsvTest_SelectedColumns()
        {
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {
                new DataColumn("COL_StrING", typeof(string)),
                new DataColumn("col_datetime", typeof(DateTime)),
                new DataColumn("123Col_float", typeof(float))
            });

            dt.Rows.Add("Hello\"semicolon1", DateTime.Parse("2018-12-31T11:22:33"), 3000.212);
            dt.Rows.Add("Hello\"semicolon2", DateTime.Parse("2018-12-31T11:22:34"), 3000.212);

            var options = new SaveQueryToCsvOptions
            {
                DateFormat = "MM-dd-yyyy HH:mm:ss",
                ColumnsToInclude = new[] { "COL_StrING", "123Col_float" },
                FieldDelimiter = CsvFieldDelimiter.Pipe,
                SanitizeColumnHeaders = true
            };

            using (var writer = new StringWriter())
            using (var csvFile = Extensions.CreateCsvWriter(options.GetFieldDelimiterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                Extensions.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split("\r\n");

                // 4 lines = 1 header line + 2 data lines + 1 newline at end of file
                Assert.AreEqual(4, resultLines.Length);
                Assert.AreEqual("col_string|col_float", resultLines[0]);
                Assert.AreEqual("\"Hello\\\"semicolon1\"|3000.212", resultLines[1]);
                Assert.AreEqual("\"Hello\\\"semicolon2\"|3000.212", resultLines[2]);
            }
        }

        /// <summary>
        /// This test is basically to make sure that the dump to CSV is not horribly slow.
        /// My results are ok (not great though) - about 1m rows in under 2 seconds. But 
        /// this depends on the agent CPU of course.
        /// </summary>
        [Ignore("This test is for occasional performance testing only and depends on host CPU")]
        [Test]
        public void DataReaderToCsvTest_1mRows()
        {
            var rowAmount = 1000000;
            var processingMaxTimeSeconds = 2d;
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {
                new DataColumn("col_string", typeof(string)),
                new DataColumn("col_datetime", typeof(DateTime)),
                new DataColumn("col_float", typeof(float)),
                new DataColumn("col_double", typeof(double)),
                new DataColumn("col_decimal", typeof(decimal)),
            });

            for (var i = 0; i < rowAmount; i++)
            {
                dt.Rows.Add($"Hello, mister {i}", DateTime.Now, i, i, i);
            }

            var options = new SaveQueryToCsvOptions
            {
                DateFormat = "MM-dd-yyyy HH:mm:ss",
                ColumnsToInclude = new[] { "col_string", "col_float" },
                FieldDelimiter = CsvFieldDelimiter.Pipe
            };

            using (var writer = new StringWriter())
            using (var csvFile = Extensions.CreateCsvWriter(options.GetFieldDelimiterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                var sw = Stopwatch.StartNew();
                Extensions.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                sw.Stop();
                Console.WriteLine("Elapsed={0}", sw.Elapsed);
                var result = writer.ToString();
                var resultLines = result.Split("\r\n");

                // rowAmount + 1 header row + 1 newline at end
                Assert.AreEqual(rowAmount + 2, resultLines.Length);

                // Check execution time
                Assert.IsTrue(
                    sw.Elapsed.TotalSeconds < processingMaxTimeSeconds,
                    $"DataReaderToCsv completed in {sw.Elapsed.TotalSeconds} seconds. Processing max time: {processingMaxTimeSeconds} seconds");
            }
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
