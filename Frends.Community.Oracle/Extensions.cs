using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Frends.Community.Oracle
{
    static class Extensions
    {

        internal static IsolationLevel GetTransactionIsolationLevel(this Oracle_IsolationLevel Oracle_IsolationLevel)
        {
            return GetEnum<IsolationLevel>(Oracle_IsolationLevel);
        }

        private static T GetEnum<T>(Enum enumValue)
        {
            return (T)Enum.Parse(typeof(T), enumValue.ToString());
        }

        public static TEnum ConvertEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
        }
        /// <summary>
        /// Write query results to csv string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToXmlAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            Encoding encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

            using (TextWriter writer = queryOutput.OutputToFile ? new StreamWriter(queryOutput.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Async = true, Indent = true }))
                {
                    await xmlWriter.WriteStartDocumentAsync();
                    await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RootElementName, "");

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // single row element container
                        await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RowElementName, "");

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            switch (reader.GetDataTypeName(i))
                            {
                                case "Decimal":
                                    OracleDecimal v = reader.GetOracleDecimal(i);
                                    OracleDecimal decimalValue = OracleDecimal.SetPrecision(v, 28);
                                    string decimalString = decimalValue.ToString();
                                    // Is decimal separator overwrite value given and query result value is not null?
                                    if (!string.IsNullOrWhiteSpace(queryOutput.XmlOutput.DecimalSeparator))
                                    {
                                        decimalString = decimalString
                                            .Replace(".", queryOutput.XmlOutput.DecimalSeparator)
                                            .Replace(",", queryOutput.XmlOutput.DecimalSeparator);
                                    }

                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", decimalString);
                                    break;

                                default:
                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", reader.GetValue(i).ToString());
                                    break;
                            }
                        }

                        // close single row element container
                        await xmlWriter.WriteEndElementAsync();

                        // write only complete elements, but stop if process was terminated
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await xmlWriter.WriteEndElementAsync();
                    await xmlWriter.WriteEndDocumentAsync();
                }

                if (queryOutput.OutputToFile)
                {
                    return queryOutput.OutputFile.Path;
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
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToJsonAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                var culture = String.IsNullOrWhiteSpace(queryOutput.JsonOutput.CultureInfo) ? CultureInfo.InvariantCulture : new CultureInfo(queryOutput.JsonOutput.CultureInfo);

                // utf-8 as default encoding
                Encoding encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

                // create json result
                using (var fileWriter = queryOutput.OutputToFile ? new StreamWriter(queryOutput.OutputFile.Path, false, encoding) : null)
                using (var writer = queryOutput.OutputToFile ? new JsonTextWriter(fileWriter) : new JTokenWriter() as JsonWriter)
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

                    if (queryOutput.OutputToFile)
                    {
                        return queryOutput.OutputFile.Path;
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
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToCsvAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            Encoding encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            using (TextWriter w = queryOutput.OutputToFile ? new StreamWriter(queryOutput.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            {
                bool headerWritten = false;

                while (await reader.ReadAsync(cancellationToken))
                {
                    // Initiate string builder for the line
                    StringBuilder sb = new StringBuilder();

                    // write csv header if necessary
                    if (!headerWritten && queryOutput.CsvOutput.IncludeHeaders)
                    {
                        var fieldNames = new object[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string fieldName = reader.GetName(i);
                            sb.Append(fieldName);
                            if (i < reader.FieldCount - 1)
                            {
                                sb.Append(queryOutput.CsvOutput.CsvSeparator);
                            }
                        }
                        await w.WriteLineAsync(sb.ToString());
                        headerWritten = true;
                    }

                    sb = new StringBuilder();
                    var fieldValues = new object[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        switch (reader.GetDataTypeName(i))
                        {
                            case "Decimal":
                                OracleDecimal v = reader.GetOracleDecimal(i);
                                OracleDecimal decimalValue= OracleDecimal.SetPrecision(v, 28);
                                // Is decimal separator overwrite value given and query result value is not null?
                                if (!string.IsNullOrWhiteSpace(queryOutput.CsvOutput.DecimalSeparator) && !decimalValue.IsNull)
                                {
                                    fieldValues[i] = decimalValue.ToString()
                                        .Replace(".", queryOutput.CsvOutput.DecimalSeparator)
                                        .Replace(",", queryOutput.CsvOutput.DecimalSeparator);
                                }
                                else
                                    fieldValues[i] = decimalValue;
                                break;

                            default:
                                fieldValues[i] = reader.GetValue(i);
                                break;
                        }

                        string fieldValue = fieldValues[i].ToString();
                        sb.Append(fieldValue);
                        if (i < reader.FieldCount - 1)
                        {
                            sb.Append(queryOutput.CsvOutput.CsvSeparator);
                        }
                    }
                    await w.WriteLineAsync(sb.ToString());

                    // write only complete rows, but stop if process was terminated
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (queryOutput.OutputToFile)
                {
                    return queryOutput.OutputFile.Path;
                }
                else
                {
                    return w.ToString();
                }
            }
        }

        /// <summary>
        /// Write query results to json string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<object> MultiQueryToJsonAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                var culture = String.IsNullOrWhiteSpace(queryOutput.JsonOutput.CultureInfo) ? CultureInfo.InvariantCulture : new CultureInfo(queryOutput.JsonOutput.CultureInfo);

                // utf-8 as default encoding
                Encoding encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

                // create json result
                using (var writer = new JTokenWriter() as JsonWriter)
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

                    return ((JTokenWriter)writer).Token;

                }
            }
        }

        /// <summary>
        /// Write query results to json string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> MultiQueryToCSVAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            Encoding encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                using (StringWriter writer = new StringWriter())
                {
                    bool headerWritten = false;

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // Initiate string builder for the line
                        StringBuilder sb = new StringBuilder();

                        // write csv header if necessary
                        if (!headerWritten && queryOutput.CsvOutput.IncludeHeaders)
                        {
                            var fieldNames = new object[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string fieldName = reader.GetName(i);
                                sb.Append(fieldName);
                                if (i < reader.FieldCount - 1)
                                {
                                    sb.Append(queryOutput.CsvOutput.CsvSeparator);
                                }
                            }
                            await writer.WriteLineAsync(sb.ToString());
                            headerWritten = true;
                        }

                        sb = new StringBuilder();
                        var fieldValues = new object[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetDataTypeName(i).Equals("Decimal"))
                            {
                                OracleDecimal v = reader.GetOracleDecimal(i);
                                fieldValues[i] = OracleDecimal.SetPrecision(v, 28);
                            }
                            else
                            {
                                fieldValues[i] = reader.GetValue(i);
                            }
                            string fieldValue = fieldValues[i].ToString();
                            sb.Append(fieldValue);
                            if (i < reader.FieldCount - 1)
                            {
                                sb.Append(queryOutput.CsvOutput.CsvSeparator);
                            }
                        }
                        await writer.WriteLineAsync(sb.ToString());

                        // write only complete rows, but stop if process was terminated
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    return writer.ToString();

                }
            }
        }

        /// <summary>
        /// Write query results to csv string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> MultiQueryToXmlAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            Encoding encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

            using (StringWriter writer = new StringWriter())
            {
                using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Async = true, Indent = true }))
                    {
                        await xmlWriter.WriteStartDocumentAsync();
                        await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RootElementName, "");

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            // single row element container
                            await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RowElementName, "");

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (reader.GetDataTypeName(i).Equals("Decimal"))
                                {
                                    OracleDecimal v = reader.GetOracleDecimal(i);
                                    OracleDecimal.SetPrecision(v, 28);
                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", v.ToString());
                                }
                                else
                                {
                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", reader.GetValue(i).ToString());
                                }
                            }

                            // close single row element container
                            await xmlWriter.WriteEndElementAsync();

                            // write only complete elements, but stop if process was terminated
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        await xmlWriter.WriteEndElementAsync();
                        await xmlWriter.WriteEndDocumentAsync();
                    }

                    return writer.ToString();

                }

            }
        }


    }

}
