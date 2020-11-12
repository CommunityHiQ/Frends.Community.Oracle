# Frends.Community.Oracle

FRENDS Task for querying data from Oracle database

[![Actions Status](https://github.com/CommunityHiQ/Frends.Community.Oracle/workflows/PackAndPushAfterMerge/badge.svg)](https://github.com/CommunityHiQ/Frends.Community.Oracle/actions) ![MyGet](https://img.shields.io/myget/frends-community/v/Frends.Community.Oracle) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) 

- [Installing](#installing)
- [Task](#tasks)
	- [ExecuteQueryOracle](#ExecuteQueryOracle)
	- [BatchOperation](#BatchOperationOracle)
	- [TransactionalMultiQuery](#TransactionalMultiQuery)
	- [MultiBatchOperationOracle](#MultiBatchOperationOracle)
- [Known issues](#known-issues)
- [Building](#building)
- [Contributing](#contributing)
- [Change Log](#change-log)

# Installing

You can install the task via frends UI Task View or you can find the NuGet package from the following NuGet feed https://www.myget.org/F/frends-community/api/v3/index.json and in Gallery view in MyGet https://www.myget.org/feed/frends-community/package/nuget/Frends.Community.Oracle

# Task

## ExecuteQueryOracle

Executes query against Oracle database.

### Query input Properties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Query | string | The query to execute | `SELECT * FROM Table WHERE field = :paramName`|
| Parameters | array[Query Parameter] | Possible query parameters. See [Query Parameters Properties](#query-parameters-properties) |  |
| Connection string | string | Oracle database connection string | `Data Source=(DESCRIPTION=(ADDRESS = (PROTOCOL = TCP)(HOST = oracleHost)(PORT = 1521))(CONNECT_DATA = (SERVICE_NAME = MYSERVICE)))` |

#### Query Parameters Properties

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Name | string | Parameter name used in Query property | `username` |
| Value | string | Parameter value | `myUser` |
| Data type | enum<> | Parameter data type | `NVarchar2` |

### Query Output Properties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Return type | enum<Json, Xml, Csv> | Data return type format | `Json` |
| OutputToFile | bool | true to write results to a file, false to return results to executin process | `true` |

#### Xml Output
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Root element name | string | Xml root element name | `items` |
| Row element name |string | Xml row element name | `item` |
| Maximum rows | int | The maximum amount of rows to return; defaults to -1 eg. no limit | `1000` |
| Output to file | boolean | If true, write output to file, instead returning it. | `true` |
| Path | boolean | Path where file is written. | `c:\temp\queryOutput.xml` |
| Encoding | boolean | Set encoding of file. | `utf-8` |


#### Json Output
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Culture info | string | Specify the culture info to be used when parsing result to JSON. If this is left empty InvariantCulture will be used. [List of cultures](https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx) Use the Language Culture Name. | `fi-FI` |
| Output to file | boolean | If true, write output to file, instead returning it. | `true` |
| Path | boolean | Path where file is written. | `c:\temp\queryOutput.xml` |
| Encoding | boolean | Set encoding of file. | `utf-8` |


#### Csv Output
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Include headers | bool | Include field names in the first row | `true` |
| Csv separator | string | Csv separator to use in headers and data items | `;` |
| Output to file | boolean | If true, write output to file, instead returning it. | `true` |
| Path | boolean | Path where file is written. | `c:\temp\queryOutput.xml` |
| Encoding | boolean | Set encoding of file. | `utf-8` |


#### Output File
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Path | string | Output path with file name | `c:\temp\output.json` |
| Encoding | string | Encoding to use for the output file | `utf-8` |

### Query Options

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Throw error on failure | bool | Specify if Exception should be thrown when error occurs. If set to *false*, task outcome can be checked from #result.Success property. | `false` |
| Isolation Level| enum<> | Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Possible values are:  None, Serializable and ReadCommitted. | None |
| Timeout seconds | int | Query timeout in seconds | `60` |
| Enable detaild logging | bool | If true, enables setting additional tracing. If false tracing level is set to default value is 0 indicating tracing is disabled. However, errors will always be traced. | `false` |
| Trace level | int | Valid Values: 1 = public APIs, 2 = private APIs, 4 = network APIs/data More information https://docs.oracle.com/en/database/oracle/oracle-data-access-components/18.3/odpnt/ConfigurationTraceLevel.html#GUID-E4A2B13E-E0AC-4E79-BCD9-51C4DBBBFEA5 | `60` |
| Trace file location | string | Destination directory for trace files. Can not be left empty. | `%TEMP%\ODP.NET\core\trace` |



### Result

Object { bool Success, string Message, string Result }

If output type is file, then _Result_ indicates the written file path. Otherwise it will hold the query output in xml, json or csv.

Example result with return type JSON

*Success:* ``` True ```
*Message:* ``` null ```
*Results:* 
```
[ 
 {
  "Name": "Teela",
  "Age": 42,
  "Address": "Test road 123"
 },
 {
  "Name": "Adam",
  "Age": 42,
  "Address": null
 }
]
```


To access query result, use 
```
#result.Results
```

## BatchOperationOracle

Create a query for a batch operation like insert. The query is executed with Dapper ExecuteAsync.

### Input
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Query | string | The query to execute | `INSERT INTO MyTable(ID,NAME) VALUES (:Id, :FirstName)`|
| InputJson | string |A Json Array of objects that has their properties mapped to the parameters in the Query|[{"Id":10, "FirstName": "Foo"},{"Id":15, "FirstName": "Bar"}]  |
| Connection string | string | Oracle database connection string | `Data Source=(DESCRIPTION=(ADDRESS = (PROTOCOL = TCP)(HOST = oracleHost)(PORT = 1521))(CONNECT_DATA = (SERVICE_NAME = MYSERVICE)))` |

### Options

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Throw error on failure | bool | Specify if Exception should be thrown when error occurs. If set to *false*, task outcome can be checked from #result.Success property. | `false` |
| Transaction Isolation Level| Oracle_IsolationLevel | Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Possible values are:  Serializable, ReadCommitted | Serializable |
| Timeout seconds | int | Query timeout in seconds | `60` |


### Result
Integer - Number of affected rows

## TransactionalMultiQuery

Execute multiple queries and operations in one transaction.

### Query input Properties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| InputQuery | Array[string] | The queries to execute |  |
| Parameters | Array[Query Parameter] | Possible query parameters. |  |
| Connection string | string | Oracle database connection string | `Data Source=(DESCRIPTION=(ADDRESS = (PROTOCOL = TCP)(HOST = oracleHost)(PORT = 1521))(CONNECT_DATA = (SERVICE_NAME = MYSERVICE)))` |

#### Query Parameters Properties

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Name | string | Parameter name used in Query property | `username` |
| Value | string | Parameter value | `myUser` |
| Data type | enum<> | Parameter data type | `NVarchar2` |

### Query Output Properties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Return type | array[enum<[Json](#json-output), [Xml](#xml-output), [Csv](#csv-output)>] | Data return type format | Array[`Json`] |
| OutputToFile | bool | true to write results to a file, false to return results to executin process | `true` |

#### Output File
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Path | string | Output path with file name | `c:\temp\output.json` |
| Encoding | string | Encoding to use for the output file | `utf-8` |

### Query Options

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Throw error on failure | bool | Specify if Exception should be thrown when error occurs. If set to *false*, task outcome can be checked from #result.Success property. | `false` |
| Isolation Level| enum<None, ReadCommitted, Serializable> | Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Possible values are:  None, Serializable and ReadCommitted. | None |
| Timeout seconds | int | Query timeout in seconds | `60` |
| Enable detaild logging | bool | If true, enables setting additional tracing. If false tracing level is set to default value is 0 indicating tracing is disabled. However, errors will always be traced. | `false` |
| Trace level | int | Valid Values: 1 = public APIs, 2 = private APIs, 4 = network APIs/data More information https://docs.oracle.com/en/database/oracle/oracle-data-access-components/18.3/odpnt/ConfigurationTraceLevel.html#GUID-E4A2B13E-E0AC-4E79-BCD9-51C4DBBBFEA5 | `60` |
| Trace file location | string | Destination directory for trace files. Can not be left empty. | `%TEMP%\ODP.NET\core\trace` |



### Result

Object { bool Success, string Message, JArray Result }

If output type is file, then _Result_ indicates the written file path. Otherwise it will hold the query output in xml, json or csv.

Example result with return type JSON when SELECT, SELECT, INSERT and DELETE have been executed.

*Success:* ``` True ```
*Message:* ``` null ```
*Results:* 
```
[
  {
    "QueryIndex": 0,
    "Output": [
      {
        "DECIMALVALUE": 1.123456789123456789123456789
      }
    ]
  },
  {
    "QueryIndex": 1,
    "Output": [
      {
        "NAME": "hodor",
        "VALUE": 123
      },
      {
        "NAME": "jon",
        "VALUE": 321
      }
    ]
  },
  {
    "QueryIndex": 2,
    "Output": []
  },
  {
    "QueryIndex": 3,
    "Output": []
  }
]
```

To access query result, use 
```
#result.Results
```

## MultiBatchOperationOracle

A task to execute multiple operations in one transaction. Task does not support SELECT queries, but you can bulk insert data. The queries are executed with Dapper ExecuteAsync.

### Input
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| BatchOperationQuery | Array[string] | The queries to execute |  |
| InputJson | string |A Json Array of objects that has their properties mapped to the parameters in the Query|[{"Id":10, "FirstName": "Foo"},{"Id":15, "FirstName": "Bar"}]  |
| Connection string | string | Oracle database connection string | `Data Source=(DESCRIPTION=(ADDRESS = (PROTOCOL = TCP)(HOST = oracleHost)(PORT = 1521))(CONNECT_DATA = (SERVICE_NAME = MYSERVICE)))` |

### Options

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Throw error on failure | bool | Specify if Exception should be thrown when error occurs. If set to *false*, task outcome can be checked from #result.Success property. | `false` |
| Transaction Isolation Level| Oracle_IsolationLevel | Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Possible values are:  Serializable, ReadCommitted | Serializable |
| Timeout seconds | int | Query timeout in seconds | `60` |

### Result

Object { bool Success, string Message, JArray Result }

If output type is file, then _Result_ indicates the written file path. Otherwise it will hold the query output in xml, json or csv.

Example result with return type JSON when INSERT and UPDATE have been executed.

*Success:* ``` True ```
*Message:* ``` null ```
*Results:* 
```
[
  {
    "QueryIndex": 0,
    "RowCount": 125
  },
  {
    "QueryIndex": 1,
    "RowCount": 10
  }
]
```

To access query result, use 
```
#result.Result
```

# Known issues

FRENDS Agents try to keep their memory usage as low as possible by removing Processes from memory that are no longer being executed. Sometimes Oracle taskss cause issues when trying to remove old Processes by having handles open. This will cause memory consumption to rise until the Agent is restarted.

This is caused by driver sometimes misshandling connections to Oracle Notification Services. This can be prevented, and thus solving the unloadability issue, by adding to connection string `ENLIST=false; HA EVENTS=false; LOAD BALANCING=false;` See more: https://stackoverflow.com/a/45943074/6734525
 
# Building

Clone a copy of the repo

`git clone https://github.com/CommunityHiQ/Frends.Community.Oracle.git`

Rebuild the project

`dotnet build`

Run Tests

`dotnet test`

Create a NuGet package

`dotnet pack --configuration Release`

# Contributing
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!

# Change Log

| Version | Changes |
| ----- | ----- |
| 1.0.0 | Initial version of Oracle Query Task |
| 2.0.0 | Breaking changes: target .netstandard, more user friendly task settings, csv output, all output types are now possibly to stream directly into a file |
| 2.0.6 | Enabled detailed logging. |
| 3.0.0 | Query ranamed and namespace changed to more generic to enable adding new task. Added BatchOperationOracle task. |
| 3.1.0 | Multiquery tasks added |
| 3.1.1 | Connection string fields changed from text fields to password fields now hidden and won't show on logs |
