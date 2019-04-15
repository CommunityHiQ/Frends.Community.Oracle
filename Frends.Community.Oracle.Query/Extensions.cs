using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CsvHelper;
using CsvHelper.Configuration;

namespace Frends.Community.Oracle.Query
{
    /// <summary>
    /// Oracle extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert enum
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static TEnum ConvertEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
        }

        /// <summary>
        /// Write query results to csv string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToXmlAsync(this OracleCommand command, OutputProperties output, CancellationToken cancellationToken)
        {
            command.CommandType = CommandType.Text;
            
            // utf-8 as default encoding
            Encoding encoding = string.IsNullOrWhiteSpace(output.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(output.OutputFile.Encoding);

            using (TextWriter writer = output.OutputToFile ? new StreamWriter(output.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Async = true, Indent = true }))
                {
                    await xmlWriter.WriteStartDocumentAsync();
                    await xmlWriter.WriteStartElementAsync("", output.XmlOutput.RootElementName, "");

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // single row element container
                        await xmlWriter.WriteStartElementAsync("", output.XmlOutput.RowElementName, "");

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", reader.GetValue(i).ToString());
                        }

                        // close single row element container
                        await xmlWriter.WriteEndElementAsync();

                        // write only complete elements, but stop if process was terminated
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await xmlWriter.WriteEndElementAsync();
                    await xmlWriter.WriteEndDocumentAsync();
                }

                if (output.OutputToFile)
                {
                    return output.OutputFile.Path;
                }
                else
                {
                    return writer.ToString();
                }
            }
        }

        /// <summary>
        /// Write query results to json string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToJsonAsync(this OracleCommand command, OutputProperties output, CancellationToken cancellationToken)
        {
            command.CommandType = CommandType.Text;
            
            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                var culture = String.IsNullOrWhiteSpace(output.JsonOutput.CultureInfo) ? CultureInfo.InvariantCulture : new CultureInfo(output.JsonOutput.CultureInfo);

                // utf-8 as default encoding
                Encoding encoding = string.IsNullOrWhiteSpace(output.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(output.OutputFile.Encoding);

                // create json result
                using (var fileWriter = output.OutputToFile ? new StreamWriter(output.OutputFile.Path, false, encoding) : null)
                using (var writer = output.OutputToFile ? new JsonTextWriter(fileWriter) : new JTokenWriter() as JsonWriter)
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    writer.Culture = culture;

                    // start array
                    await writer.WriteStartArrayAsync(cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    while (reader.Read())
                    {
                        // start row object
                        await writer.WriteStartObjectAsync(cancellationToken);

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            // add row element name
                            await writer.WritePropertyNameAsync(reader.GetName(i), cancellationToken);
                            
                            // add row element value
                            switch (reader.GetDataTypeName(i))
                            {
                                case "Decimal":
                                    // FCOM-204 fix; proper handling of decimal values and NULL values in decimal type fields
                                    OracleDecimal v = reader.GetOracleDecimal(i);
                                    var FieldValue = OracleDecimal.SetPrecision(v, 28);

                                    if (!FieldValue.IsNull) await writer.WriteValueAsync((decimal)FieldValue, cancellationToken);
                                    else await writer.WriteValueAsync(string.Empty, cancellationToken);
                                    break;
                                default:
                                    await writer.WriteValueAsync(reader.GetValue(i) ?? string.Empty, cancellationToken);
                                    break;
                            }

                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        await writer.WriteEndObjectAsync(cancellationToken); // end row object

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    // end array
                    await writer.WriteEndArrayAsync(cancellationToken);

                    if (output.OutputToFile)
                    {
                        return output.OutputFile.Path;
                    }
                    else
                    {
                        return ((JTokenWriter)writer).Token.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Write query results to csv string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToCsvAsync(this OracleCommand command, OutputProperties output, CancellationToken cancellationToken)
        {
            command.CommandType = CommandType.Text;

            // utf-8 as default encoding
            Encoding encoding = string.IsNullOrWhiteSpace(output.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(output.OutputFile.Encoding);

            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            using (TextWriter w = output.OutputToFile ? new StreamWriter(output.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            {
                bool headerWritten = false;

                while (await reader.ReadAsync(cancellationToken))
                {
                    // write csv header if necessary
                    if (!headerWritten && output.CsvOutput.IncludeHeaders)
                    {
                        var fieldNames = new object[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            fieldNames[i] = reader.GetName(i);
                        }
                        await w.WriteLineAsync(string.Join(output.CsvOutput.CsvSeparator, fieldNames));
                        headerWritten = true;
                    }

                    var fieldValues = new object[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        fieldValues[i] = reader.GetValue(i);
                    }
                    await w.WriteLineAsync(string.Join(output.CsvOutput.CsvSeparator, fieldValues));

                    // write only complete rows, but stop if process was terminated
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (output.OutputToFile)
                {
                    return output.OutputFile.Path;
                }
                else
                {
                    return w.ToString();
                }
            }
        }

        #region QueryToFileTask

        /// <summary>
        /// Write query results to csv file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<int> ToCsvFileAsync(this OracleCommand command, SaveQueryToCsvOptions output, CancellationToken cancellationToken)
        {
            int result;
            command.CommandType = CommandType.Text;

            using (var reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            using (var writer = new StreamWriter(output.OutputFilePath))
            using (var csvFile = CreateCsvWriter(output.GetFieldDelimiterAsString(), writer))
            {
                writer.NewLine = output.GetLineBreakAsString();

                result = DataReaderToCsv(reader, csvFile, output, cancellationToken);
  
                csvFile.Flush();
            }
            return result;
        }
        
        /// <summary>
        /// Return CsvWriter
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static CsvWriter CreateCsvWriter(string delimiter, TextWriter writer)
        {
            var csvOptions = new Configuration
            {
                Delimiter = delimiter,
            };
            return new CsvWriter(writer, csvOptions);
        }

        /// <summary>
        /// Format csv header
        /// </summary>
        /// <param name="header"></param>
        /// <param name="forceSpecialFormatting"></param>
        /// <returns></returns>
        public static string FormatDbHeader(string header, bool forceSpecialFormatting)
        {
            if (!forceSpecialFormatting) return header;

            // First part of regex removes all non-alphanumeric ('_' also allowed) chars from the whole string
            // Second part removed any leading numbers or underscores
            Regex rgx = new Regex("[^a-zA-Z0-9_-]|^[0-9_]+");
            header = rgx.Replace(header, "");
            return header.ToLower();
        }

        /// <summary>
        /// Formats a value according to options
        /// </summary>
        /// <param name="value">Value from the database</param>
        /// <param name="dbTypeName">Type of database column. E.g. for differentiating between DATE and DATETIME types</param>
        /// <param name="dbType"></param>
        /// <param name="options">Formatting options</param>
        /// <returns></returns>
        public static string FormatDbValue(object value, string dbTypeName, Type dbType, SaveQueryToCsvOptions options)
        {
            if (value == null || value == DBNull.Value)
            {
                if (dbType == typeof(string)) return "\"\"";
                if (dbType == typeof(DateTime) && options.AddQuotesToDates) return "\"\"";
                return "";
            }

            if (dbType == typeof(string))
            {
                var str = (string)value;
                str = str.Replace("\"", "\\\"");
                str = str.Replace("\r\n", " ");
                str = str.Replace("\r", " ");
                str = str.Replace("\n", " ");
                return $"\"{str}\"";
            }

            if (dbType == typeof(DateTime))
            {
                var dateTime = (DateTime)value;
                string output;
                switch (dbTypeName?.ToLower())
                {
                    case "date":
                        output = dateTime.ToString(options.DateFormat, CultureInfo.InvariantCulture);
                        break;
                    default:
                        output = dateTime.ToString(options.DateTimeFormat, CultureInfo.InvariantCulture);
                        break;
                }

                if (options.AddQuotesToDates) return $"\"{output}\"";
                return output;
            }

            if (dbType == typeof(float))
            {
                var floatValue = (float)value;
                return floatValue.ToString("0.###########", CultureInfo.InvariantCulture);
            }

            if (dbType == typeof(double))
            {
                var doubleValue = (double)value;
                return doubleValue.ToString("0.###########", CultureInfo.InvariantCulture);
            }

            if (dbType == typeof(decimal))
            {
                var decimalValue = (decimal)value;
                return decimalValue.ToString("0.###########", CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        /// <summary>
        /// Write csv data to file
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="csvWriter"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static int DataReaderToCsv(DbDataReader reader, CsvWriter csvWriter, SaveQueryToCsvOptions options, CancellationToken cancellationToken)
        {
            // Write header and remember column indexes to include
            var columnIndexesToInclude = new List<int>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var includeColumn =
                    options.ColumnsToInclude == null ||
                    options.ColumnsToInclude.Length == 0 ||
                    ((IList) options.ColumnsToInclude).Contains(columnName);

                if (includeColumn)
                {
                    if (options.IncludeHeadersInOutput)
                    {
                        var formattedHeader = FormatDbHeader(columnName, options.SanitizeColumnHeaders);
                        csvWriter.WriteField(formattedHeader);
                    }
                    columnIndexesToInclude.Add(i);
                }
            }

            if (options.IncludeHeadersInOutput) csvWriter.NextRecord();

            var count = 0;
            while (reader.Read())
            {
                foreach (var columnIndex in columnIndexesToInclude)
                {
                    var value = reader.GetValue(columnIndex);
                    var dbTypeName = reader.GetDataTypeName(columnIndex);
                    var dbType = reader.GetFieldType(columnIndex);
                    var formattedValue = FormatDbValue(value, dbTypeName, dbType, options);
                    csvWriter.WriteField(formattedValue, false);
                }
                csvWriter.NextRecord();
                count++;
            }

            return count;
        }

        #endregion
    }
}
