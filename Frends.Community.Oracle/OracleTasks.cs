using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Dapper;
using System.Collections.Generic;
using System.IO;

#pragma warning disable 1591

namespace Frends.Community.Oracle
{
    public class OracleTasks
    {

        /// <summary>
        /// Task for performing queries in Oracle databases. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle
        /// </summary>
        /// <param name="queryInput"></param>
        /// <param name="queryOutput"></param>
        /// <param name="queryOptions"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { bool Success, string Message, string Result }</returns>
        public static async Task<Output> ExecuteQueryOracle(
            [PropertyTab] QueryProperties queryInput,
            [PropertyTab] QueryOutputProperties queryOutput,
            [PropertyTab] QueryOptions queryOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                if (queryOptions.EnableDetaildLogging == true)
                {
                    OracleConfiguration.TraceFileLocation = queryOptions.TraceFileLocation;
                    OracleConfiguration.TraceLevel = queryOptions.TraceLevel;
                }

                using (var c = new OracleConnection(queryInput.ConnectionString))
                {
                    try
                    {
                        await c.OpenAsync(cancellationToken);

                        using (var command = new OracleCommand(queryInput.Query, c))
                        {
                            command.CommandTimeout = queryOptions.TimeoutSeconds;
                            command.BindByName = true; // is this xmlCommand specific?

                            // check for command parameters and set them
                            if (queryInput.Parameters != null)
                                command.Parameters.AddRange(queryInput.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                            // declare Result object
                            string queryResult;

                            if (queryOptions.IsolationLevel == Oracle_IsolationLevel.None)
                            {
                                // set commandType according to ReturnType
                                switch (queryOutput.ReturnType)
                                {
                                    case QueryReturnType.Xml:
                                        queryResult = await command.ToXmlAsync(queryOutput, cancellationToken);
                                        break;
                                    case QueryReturnType.Json:
                                        queryResult = await command.ToJsonAsync(queryOutput, cancellationToken);
                                        break;
                                    case QueryReturnType.Csv:
                                        queryResult = await command.ToCsvAsync(queryOutput, cancellationToken);
                                        break;
                                    default:
                                        throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                                }
                                return new Output { Success = true, Result = queryResult };
                            }
                            else
                            {
                                OracleTransaction txn = c.BeginTransaction(queryOptions.IsolationLevel.GetTransactionIsolationLevel());

                                try
                                {

                                    // set commandType according to ReturnType
                                    switch (queryOutput.ReturnType)
                                    {
                                        case QueryReturnType.Xml:
                                            queryResult = await command.ToXmlAsync(queryOutput, cancellationToken);
                                            txn.Commit();
                                            break;
                                        case QueryReturnType.Json:
                                            queryResult = await command.ToJsonAsync(queryOutput, cancellationToken);
                                            txn.Commit();
                                            break;
                                        case QueryReturnType.Csv:
                                            queryResult = await command.ToCsvAsync(queryOutput, cancellationToken);
                                            txn.Commit();
                                            break;
                                        default:
                                            throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                                    }
                                }
                                catch (Exception)
                                {

                                    txn.Rollback();
                                    txn.Dispose();
                                    throw;
                                }

                                txn.Dispose();
                                return new Output { Success = true, Result = queryResult };
                            }
                        }
                    }
                    finally
                    {
                        // Close connection
                        c.Dispose();
                        c.Close();
                        OracleConnection.ClearPool(c);
                    }
                }
            }
            catch (Exception ex)
            {
                if (queryOptions.ThrowErrorOnFailure)
                    throw;
                return new Output
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        /// <summary>
        /// Task to execute multiple queries in Oracle database. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { bool Success, string Message, JArray Results}</returns>
        ///

        public static async Task<MultiQueryOutput> TransactionalMultiQuery(
            [PropertyTab] InputMultiQuery input,
            [PropertyTab] QueryOutputProperties output,
            [PropertyTab] QueryOptions options,
            CancellationToken cancellationToken)
        {

            try
            {
                using (var c = new OracleConnection(input.ConnectionString))
                {
                    try
                    {
                        object queryResult;
                        JArray queryResults = new JArray();

                        await c.OpenAsync(cancellationToken);

                        if (options.IsolationLevel == Oracle_IsolationLevel.None)
                        {
                            //set commandType according to ReturnType
                            switch (output.ReturnType)
                            {
                                case QueryReturnType.Json:
                                    foreach (var query in input.Queries)
                                    {
                                        var command = new OracleCommand(query.InputQueryString, c);

                                        if (input.Parameters != null)
                                            command.Parameters.AddRange(input.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                                        command.CommandTimeout = options.TimeoutSeconds;
                                        command.BindByName = true;
                                        queryResult = await command.MultiQueryToJsonAsync(output, cancellationToken);
                                        var result = new { QueryIndex = Array.IndexOf(input.Queries, query), Output = queryResult };
                                        queryResults.Add(JObject.FromObject(result));
                                    }
                                    break;

                                case QueryReturnType.Xml:

                                    foreach (var query in input.Queries)
                                    {
                                        var command = new OracleCommand(query.InputQueryString, c);

                                        if (input.Parameters != null)
                                            command.Parameters.AddRange(input.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                                        command.CommandTimeout = options.TimeoutSeconds;
                                        command.BindByName = true;
                                        queryResult = await command.MultiQueryToXmlAsync(output, cancellationToken);
                                        var result = new { QueryIndex = Array.IndexOf(input.Queries, query), Output = queryResult };
                                        queryResults.Add(JObject.FromObject(result));
                                    }
                                    break;

                                case QueryReturnType.Csv:

                                    foreach (var query in input.Queries)
                                    {
                                        var command = new OracleCommand(query.InputQueryString, c);

                                        if (input.Parameters != null)
                                            command.Parameters.AddRange(input.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                                        command.CommandTimeout = options.TimeoutSeconds;
                                        command.BindByName = true;
                                        queryResult = await command.MultiQueryToCSVAsync(output, cancellationToken);
                                        var result = new { QueryIndex = Array.IndexOf(input.Queries, query), Output = queryResult };
                                        queryResults.Add(JObject.FromObject(result));
                                    }
                                    break;

                                default:
                                    throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                            }

                            if (output.OutputToFile)
                            {
                                using (StreamWriter file = File.CreateText(output.OutputFile.Path))
                                using (JsonTextWriter writer = new JsonTextWriter(file))
                                {
                                    writer.Formatting = Formatting.Indented;
                                    queryResults.WriteTo(writer);
                                }

                                //Return output file path, not query results 
                                queryResults.Clear();
                                queryResults.Add(JObject.FromObject(new { OutputPath = output.OutputFile.Path.ToString() }));
                            }

                            return new MultiQueryOutput { Success = true, Results = queryResults };
                        }
                        else
                        {
                            OracleTransaction txn = c.BeginTransaction(options.IsolationLevel.GetTransactionIsolationLevel());

                            try
                            {
                                //set commandType according to ReturnType
                                switch (output.ReturnType)
                                {
                                    case QueryReturnType.Xml:
                                        foreach (var query in input.Queries)
                                        {
                                            var command = new OracleCommand(query.InputQueryString, c);

                                            if (input.Parameters != null)
                                                command.Parameters.AddRange(input.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                                            command.CommandTimeout = options.TimeoutSeconds;
                                            command.BindByName = true;
                                            queryResult = await command.MultiQueryToXmlAsync(output, cancellationToken);
                                            var result = new { QueryIndex = Array.IndexOf(input.Queries, query), Output = queryResult };
                                            queryResults.Add(JObject.FromObject(result));
                                        }
                                        txn.Commit();
                                        break;

                                    case QueryReturnType.Csv:
                                        foreach (var query in input.Queries)
                                        {
                                            var command = new OracleCommand(query.InputQueryString, c);

                                            if (input.Parameters != null)
                                                command.Parameters.AddRange(input.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                                            command.CommandTimeout = options.TimeoutSeconds;
                                            command.BindByName = true;
                                            queryResult = await command.MultiQueryToCSVAsync(output, cancellationToken);
                                            var result = new { QueryIndex = Array.IndexOf(input.Queries, query), Output = queryResult };
                                            queryResults.Add(JObject.FromObject(result));
                                        }
                                        txn.Commit();
                                        break;

                                    case QueryReturnType.Json:

                                        foreach (var query in input.Queries)
                                        {
                                            var command = new OracleCommand(query.InputQueryString, c);

                                            if (input.Parameters != null)
                                                command.Parameters.AddRange(input.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                                            command.CommandTimeout = options.TimeoutSeconds;
                                            queryResult = await command.MultiQueryToJsonAsync(output, cancellationToken);
                                            var result = new { QueryIndex = Array.IndexOf(input.Queries, query), Output = queryResult };
                                            queryResults.Add(JObject.FromObject(result));
                                        }

                                        txn.Commit();
                                        break;

                                    default:
                                        throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                                }

                            }
                            catch (Exception)
                            {
                                txn.Rollback();
                                txn.Dispose();
                                throw;
                            }

                            txn.Dispose();

                            if (output.OutputToFile)
                            {
                                using (StreamWriter file = File.CreateText(output.OutputFile.Path))
                                using (JsonTextWriter writer = new JsonTextWriter(file))
                                {
                                    writer.Formatting = Formatting.Indented;
                                    queryResults.WriteTo(writer);
                                }

                                //Return output file path, not query results
                                queryResults.Clear();
                                queryResults.Add(JObject.FromObject(new { OutputPath = output.OutputFile.Path.ToString() }));
                            }

                            return new MultiQueryOutput { Success = true, Results = queryResults };
                        }

                    }
                    finally
                    {
                        //close connection
                        c.Dispose();
                        c.Close();
                        OracleConnection.ClearPool(c);

                    }
                }
            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw ;
                return new MultiQueryOutput
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        /// <summary>
        /// Create a query for a batch operation like insert. The query is executed with Dapper ExecuteAsync. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { bool Success, string Message, string Result }</returns>
        public static async Task<BatchOperationOutput> BatchOperationOracle(
            [PropertyTab] InputBatchOperation input,
            [PropertyTab] BatchOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var c = new OracleConnection(input.ConnectionString))
                {
                    try
                    {
                        await c.OpenAsync(cancellationToken);

                        using (var command = new OracleCommand(input.Query, c))
                        {
                            command.CommandTimeout = options.TimeoutSeconds;
                            command.BindByName = true; // is this xmlCommand specific?

                            // declare Result object
                            int queryResult;

                            if (options.IsolationLevel == Oracle_IsolationLevel.None)
                            {
                                var obj = JsonConvert.DeserializeObject<ExpandoObject[]>(input.InputJson,
                                    new ExpandoObjectConverter());
                                queryResult = await c.ExecuteAsync(
                                        input.Query,
                                        param: obj,
                                        commandTimeout: options.TimeoutSeconds,
                                        commandType: CommandType.Text)
                                    .ConfigureAwait(false);

                                return new BatchOperationOutput { Success = true, Result = queryResult };
                            }

                            else
                            {
                                OracleTransaction txn =
                                    c.BeginTransaction(options.IsolationLevel.GetTransactionIsolationLevel());
                                try
                                {
                                    var obj = JsonConvert.DeserializeObject<ExpandoObject[]>(input.InputJson,
                                        new ExpandoObjectConverter());
                                    queryResult = await c.ExecuteAsync(
                                            input.Query,
                                            param: obj,
                                            commandTimeout: options.TimeoutSeconds,
                                            commandType: CommandType.Text,
                                            transaction: txn)
                                        .ConfigureAwait(false);

                                    txn.Commit();
                                    txn.Dispose();

                                    return new BatchOperationOutput { Success = true, Result = queryResult };
                                }
                                catch (Exception)
                                {
                                    txn.Rollback();
                                    txn.Dispose();
                                    throw;
                                }
                            }
                        }
                    }

                    finally
                    {
                        // Close connection
                        c.Dispose();
                        c.Close();
                        OracleConnection.ClearPool(c);
                    }
                }
            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw;
                return new BatchOperationOutput
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Create multiple queries for batch operations like insert. Queries are executed with Dapper ExecuteAsync. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { bool Success, string Message, JArray Results}</returns>
        public static async Task<MultiBatchOperationOutput> MultiBatchOperationOracle(
            [PropertyTab] InputMultiBatchOperation input,
            [PropertyTab] BatchOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var c = new OracleConnection(input.ConnectionString))
                {
                    try
                    {
                        await c.OpenAsync(cancellationToken);
                        if (options.IsolationLevel == Oracle_IsolationLevel.None)
                        {
                            // declare Result object
                            int queryResult;
                            JArray queryResults = new JArray();

                            foreach (var query in input.BatchQueries)
                            {
                                using (var command = new OracleCommand(query.BatchInputQuery, c))
                                {
                                    command.CommandTimeout = options.TimeoutSeconds;
                                    command.BindByName = true; // is this xmlCommand specific?

                                    var obj = JsonConvert.DeserializeObject<ExpandoObject[]>(query.InputJson,
                                        new ExpandoObjectConverter());
                                    queryResult = await c.ExecuteAsync(
                                            query.BatchInputQuery,
                                            param: obj,
                                            commandTimeout: options.TimeoutSeconds,
                                            commandType: CommandType.Text)
                                        .ConfigureAwait(false);
                                    var result = new { QueryIndex = Array.IndexOf(input.BatchQueries, query), Output = queryResult };
                                    queryResults.Add(JObject.FromObject(result));

                                }
                            }

                            return new MultiBatchOperationOutput { Success = true, Results = queryResults };
                        }

                        else
                        {
                            OracleTransaction txn =
                                c.BeginTransaction(options.IsolationLevel.GetTransactionIsolationLevel());

                            //declare queryResult (rowcount)
                            int queryResult;
                            JArray queryResults = new JArray();

                            try
                            {
                                foreach (var query in input.BatchQueries)
                                {
                                    var obj = JsonConvert.DeserializeObject<ExpandoObject[]>(query.InputJson,
                                        new ExpandoObjectConverter());
                                    queryResult = await c.ExecuteAsync(
                                            query.BatchInputQuery,
                                            param: obj,
                                            commandTimeout: options.TimeoutSeconds,
                                            commandType: CommandType.Text,
                                            transaction: txn)
                                        .ConfigureAwait(false);
                                    var result = new { QueryIndex = Array.IndexOf(input.BatchQueries, query), RowCount = queryResult };
                                    queryResults.Add(JObject.FromObject(result));

                                }
                                txn.Commit();
                                txn.Dispose();
                                return new MultiBatchOperationOutput { Success = true, Results = queryResults };

                            }
                            catch (Exception)
                            {
                                txn.Rollback();
                                txn.Dispose();
                                throw;
                            }
                        }

                    }

                    finally
                    {
                        // Close connection
                        c.Dispose();
                        c.Close();
                        OracleConnection.ClearPool(c);

                    }
                }
            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw;
                return new MultiBatchOperationOutput
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
