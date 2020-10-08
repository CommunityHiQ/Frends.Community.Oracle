using System.ComponentModel;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;

#pragma warning disable 1591

namespace Frends.Community.Oracle
{
    public enum QueryReturnType { Json, Xml, Csv };

    /// <summary>
    /// Enumerator representing oracle parameter data types
    /// </summary>
    public enum QueryParameterType
    {
        NVarchar2, Varchar2, NChar, Char, Int16, Int32, Int64, Double, Decimal, Long, LongRaw, Boolean, Date, TimeStamp, TimeStampLTZ, TimeStampTZ, XmlType, Raw, BFile, BinaryDouble, BinaryFloat, Blob, Byte, Clob, NClob, IntervalDS, IntervalYM, RefCursor, Single
    }

    public class QueryProperties
    {
        [DisplayFormat(DataFormatString = "Sql")]
        [DefaultValue("SELECT ColumnName FROM TableName")]
        public string Query { get; set; }

        /// <summary>
        /// Parameters for the database query
        /// </summary>
        public QueryParameter[] Parameters { get; set; }

        /// <summary>
        /// Oracle connection string
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;")]
        public string ConnectionString { get; set; }
    }

    public class QueryParameter
    {
        /// <summary>
        /// The name of the parameter
        /// </summary>
        [DefaultValue("ParameterName")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Name { get; set; }

        /// <summary>
        /// The value of the parameter
        /// </summary>
        [DefaultValue("Parameter value")]
        [DisplayFormat(DataFormatString = "Text")]
        public dynamic Value { get; set; }

        /// <summary>
        /// The type of the parameter
        /// </summary>
        [DefaultValue(QueryParameterType.NVarchar2)]
        public QueryParameterType DataType { get; set; }
    }

    public class QueryOutputProperties
    {
        [DefaultValue(QueryReturnType.Xml)]
        public QueryReturnType ReturnType { get; set; }

        /// <summary>
        /// Xml specific queryOutput properties
        /// </summary>
        [UIHint(nameof(ReturnType), "", QueryReturnType.Xml)]
        public XmlOutputProperties XmlOutput { get; set; }

        /// <summary>
        /// Json specific queryOutput properties
        /// </summary>
        [UIHint(nameof(ReturnType), "", QueryReturnType.Json)]
        public JsonOutputProperties JsonOutput { get; set; }

        /// <summary>
        /// Csv specific queryOutput properties
        /// </summary>
        [UIHint(nameof(ReturnType), "", QueryReturnType.Csv)]
        public CsvOutputProperties CsvOutput { get; set; }

        /// <summary>
        /// In case user wants to write results to a file instead of returning them to process
        /// </summary>
        public bool OutputToFile { get; set; }

        /// <summary>
        /// Output file properties
        /// </summary>
        [UIHint(nameof(OutputToFile), "", true)]
        public OutputFileProperties OutputFile { get; set; }
    }

    public enum Oracle_IsolationLevel { None, ReadCommitted, Serializable }

    public class QueryOptions
    {
        /// <summary>
        /// Choose if error should be thrown if Task failes.
        /// Otherwise returns Object {Success = false }
        /// </summary>
        [DefaultValue(true)]
        public bool ThrowErrorOnFailure { get; set; }

        /// <summary>
        /// Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Default is Serializable.
        /// </summary>
        public Oracle_IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Timeout value in seconds
        /// </summary>
        [DefaultValue(30)]
        public int TimeoutSeconds { get; set; }


        /// <summary>
        /// EnableDetaildLogging
        /// </summary>
        [DefaultValue(false)]
        public bool EnableDetaildLogging { get; set; }

        /// <summary>
        /// TraceLevel
        /// </summary>
        [UIHint(nameof(EnableDetaildLogging), "", true)]
        [DefaultValue(7)]
        public int TraceLevel { get; set; }

        /// <summary>
        /// TraceFileLocation
        /// </summary>
        [UIHint(nameof(EnableDetaildLogging), "", true)]
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue(@"C:\temp")]
        public string TraceFileLocation { get; set; }
    }

    public class BatchOptions
    {
        /// <summary>
        /// Choose if error should be thrown if Task failes.
        /// Otherwise returns Object {Success = false }
        /// </summary>
        [DefaultValue(true)]
        public bool ThrowErrorOnFailure { get; set; }

        /// <summary>
        /// Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Default is Serializable.
        /// </summary>
        public Oracle_IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Timeout value in seconds
        /// </summary>
        [DefaultValue(30)]
        public int TimeoutSeconds { get; set; }

    }

    /// <summary>
    /// Result to be returned from task
    /// </summary>
    public class Output
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Result { get; set; }
    }

    /// <summary>
    /// Result to be returned from task
    /// </summary>
    public class MultiQueryOutput
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public JArray Results { get; set; }
    }

    /// <summary>
    /// Result to be returned from task
    /// </summary>
    public class BatchOperationOutput
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Result { get; set; }
    }

    /// <summary>
    /// Result to be returned from task
    /// </summary>
    public class MultiBatchOperationOutput
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public JArray Results { get; set; }
    }

    /// <summary>
    /// Xml queryOutput specific properties
    /// </summary>
    public class XmlOutputProperties
    {
        /// <summary>
        /// Xml root element name
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("ROWSET")]
        public string RootElementName { get; set; }

        /// <summary>
        /// Xml row element name
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("ROW")]
        public string RowElementName { get; set; }

        /// <summary>
        /// The maximum amount of rows to return; defaults to -1 eg. no limit
        /// </summary>
        [DefaultValue(-1)]
        public int MaxmimumRows { get; set; }
    }

    /// <summary>
    /// Json queryOutput specific properties
    /// </summary>
    public class JsonOutputProperties
    {
        /// <summary>
        /// Specify the culture info to be used when parsing result to JSON. If this is left empty InvariantCulture will be used. List of cultures: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx Use the Language Culture Name.
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        public string CultureInfo { get; set; }
    }

    /// <summary>
    /// Csv queryOutput specific properties
    /// </summary>
    public class CsvOutputProperties
    {
        /// <summary>
        /// Include headers in csv queryOutput file?
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// Csv separator to use
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue(";")]
        public string CsvSeparator { get; set; }
    }

    /// <summary>
    /// Properties for when user wants to write the result directly into a file
    /// </summary>
    public class OutputFileProperties
    {
        /// <summary>
        /// ExecuteQueryOracle queryOutput filepath
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("c:\\temp\\queryOutput.csv")]
        public string Path { get; set; }

        /// <summary>
        /// Output file encoding
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("utf-8")]
        public string Encoding { get; set; }
    }

    public class InputBatchOperation
    {
        /// <summary>
        /// ExecuteQueryOracle string for batch operation.
        /// </summary>
        [DisplayFormat(DataFormatString = "Sql")]
        [DefaultValue("insert into MyTable(ID,NAME) VALUES (:Id, :FirstName)")]
        public string Query { get; set; }

        /// <summary>
        /// Input json for batch operation. Needs to be a Json array.
        /// </summary>
        [DisplayFormat(DataFormatString = "Json")]
        [DefaultValue("[{\"Id\":15,\"FirstName\":\"Foo\"},{\"Id\":20,\"FirstName\":\"Bar\"}]")]
        public string InputJson { get; set; }

        /// <summary>
        /// Oracle connection string
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;")]
        public string ConnectionString { get; set; }

    }

    public class InputMultiQuery
    {
        ///<summary>
        ///Array of queries to execute
        ///</summary>

        //public InputQuery[] Queries { get; set; }
        public InputQuery[] Queries { get; set; }

        /// <summary>
        /// Parameters for the database query
        /// </summary>
        public QueryParameter[] Parameters { get; set; }

        /// <summary>
        /// Oracle connection string
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;")]
        public string ConnectionString { get; set; }

    }

    public class InputQuery
    {
        /// <summary>
        /// Multiquery array item
        /// </summary>
        [DisplayFormat(DataFormatString = "Sql")]
        [DefaultValue("SELECT ColumnName FROM TableName")]
        public string InputQueryString { get; set; }
    }

    public class InputMultiBatchOperation
    {
        public BatchOperationQuery[] BatchQueries { get; set; }

        /// <summary>
        /// Oracle connection string
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;")]
        public string ConnectionString { get; set; }

    }

    public class BatchOperationQuery
    {
        /// <summary>
        /// ExecuteQueryOracle string for batch operation.
        /// </summary>
        [DisplayFormat(DataFormatString = "Sql")]
        [DefaultValue("insert into MyTable(ID,NAME) VALUES (:Id, :FirstName)")]
        public string BatchInputQuery { get; set; }

        /// <summary>
        /// Input json for batch operation. Needs to be a Json array.
        /// </summary>
        [DisplayFormat(DataFormatString = "Json")]
        [DefaultValue("[{\"Id\":15,\"FirstName\":\"Foo\"},{\"Id\":20,\"FirstName\":\"Bar\"}]")]
        public string InputJson { get; set; }
    }


}
