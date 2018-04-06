using Frends.Tasks.Attributes;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Linq;
using System.Xml;

#pragma warning disable 1591

namespace Frends.Community.Oracle.Query
{
    public class QueryTask
    {
        /// <summary>
        /// Task for performing queries in Oracle databases. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.Query
        /// </summary>
        /// <param name="database"></param>
        /// <param name="queryProperties"></param>
        /// <param name="options"></param>
        /// <returns>Object { bool Success, string Message, string Result }</returns>
        public static Output Query([CustomDisplay(DisplayOption.Tab)]ConnectionProperties database, 
            [CustomDisplay(DisplayOption.Tab)]QueryProperties queryProperties, 
            [CustomDisplay(DisplayOption.Tab)]Options options)
        {
            try
            {
                using (var connection = new OracleConnection(database.ConnectionString))
                {
                    try
                    {
                        connection.Open();

                        using (var command = new OracleCommand(queryProperties.Query, connection))
                        {
                            command.CommandTimeout = database.TimeoutSeconds;
                            command.BindByName = true; // is this xmlCommand specific?

                            // check for command parameters and set them
                            if (queryProperties.Parameters != null)
                                command.Parameters.AddRange(queryProperties.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                            // declare Result object
                            string queryResult;

                            // set commandType according to ReturnType
                            switch (queryProperties.ReturnType)
                            {
                                case QueryReturnType.Xml:
                                    command.XmlCommandType = OracleXmlCommandType.Query;
                                    command.XmlQueryProperties.MaxRows = queryProperties.MaxmimumRows;
                                    command.XmlQueryProperties.RootTag = queryProperties.RootElementName;
                                    command.XmlQueryProperties.RowTag = queryProperties.RowElementName;

                                    var xmlReader = command.ExecuteXmlReader();
                                    var xmlDocument = new XmlDocument { PreserveWhitespace = true };
                                    xmlDocument.Load(xmlReader);
                                    // assign query result XML or empty XML if query returned no results
                                    queryResult = xmlDocument.HasChildNodes ? xmlDocument.OuterXml : $"<{queryProperties.RootElementName}></{queryProperties.RootElementName}>";
                                    break;

                                case QueryReturnType.Json:
                                    command.CommandType = CommandType.Text;
                                    var reader = command.ExecuteReader();
                                    queryResult = reader.ToJson(queryProperties.CultureInfo);
                                    break;
                                default:
                                    throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                            }

                            return new Output { Success = true, Result = queryResult };
                        }
                    }
                    catch (Exception ex) { throw ex; }
                    finally
                    {
                        // Close connection
                        connection.Dispose();
                        connection.Close();
                        OracleConnection.ClearPool(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw ex;
                return new Output
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public static OracleParameter CreateOracleParameter(QueryParameter parameter)
        {
            return new OracleParameter()
            {
                ParameterName = parameter.Name,
                Value = parameter.Value,
                OracleDbType = parameter.DataType.ConvertEnum<OracleDbType>()
            };
        }
    }
}
