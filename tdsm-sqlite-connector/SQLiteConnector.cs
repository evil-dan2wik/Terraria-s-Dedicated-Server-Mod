﻿using System;
using TDSM.API.Data;
using System.Data;
using TDSM.API;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using TDSM.API.Logging;

namespace TDSM.Data.SQLite
{
    public partial class SQLiteConnector : IDataConnector
    {
        private SqliteConnection _connection;

        public QueryBuilder GetBuilder(string pluginName)
        {
            return new SQLiteQueryBuilder(pluginName);
        }

        public SQLiteConnector(string connectionString)
        {
            _connection = new SqliteConnection();
            _connection.ConnectionString = connectionString;
        }

        public void Open()
        {
            _connection.Open();

            InitialisePermissions();
        }

        bool IDataConnector.Execute(QueryBuilder builder)
        {
            if (!(builder is SQLiteQueryBuilder))
                throw new InvalidOperationException("SQLiteQueryBuilder expected");

            var ms = builder as SQLiteQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                ProgramLog.Error.Log(cmd.CommandText);

                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.HasRows;
                }
            }
        }

        int IDataConnector.ExecuteNonQuery(QueryBuilder builder)
        {
            if (!(builder is SQLiteQueryBuilder))
                throw new InvalidOperationException("SQLiteQueryBuilder expected");

            var ms = builder as SQLiteQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                ProgramLog.Error.Log(cmd.CommandText);

                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.RecordsAffected;
                }
            }
        }

        T IDataConnector.ExecuteScalar<T>(QueryBuilder builder)
        {
            if (!(builder is SQLiteQueryBuilder))
                throw new InvalidOperationException("SQLiteQueryBuilder expected");

            var ms = builder as SQLiteQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                ProgramLog.Error.Log(cmd.CommandText);

                return (T)cmd.ExecuteScalar();
            }
        }

        DataSet IDataConnector.ExecuteDataSet(QueryBuilder builder)
        {
            if (!(builder is SQLiteQueryBuilder))
                throw new InvalidOperationException("SQLiteQueryBuilder expected");

            var ms = builder as SQLiteQueryBuilder;

            using (builder)
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = builder.BuildCommand();
                cmd.CommandType = builder.CommandType;
                cmd.Parameters.AddRange(ms.Parameters.ToArray());

                ProgramLog.Error.Log(cmd.CommandText);

                using (var da = new SqliteDataAdapter(cmd))
                {
                    var ds = new DataSet();

                    da.Fill(ds);

                    return ds;
                }
            }
        }

        T[] IDataConnector.ExecuteArray<T>(QueryBuilder builder)
        {
            var ds = (this as IDataConnector).ExecuteDataSet(builder);

            if (ds != null)
            {

            }

            return null;
        }

        public override string ToString()
        {
            return "[MySQLConnector]";
        }
    }

    public class SQLiteQueryBuilder : QueryBuilder
    {
        private List<SqliteParameter> _params;

        public List<SqliteParameter> Parameters
        {
            get
            { return _params; }
        }

        public SQLiteQueryBuilder(string pluginName)
            : base(pluginName)
        {
            _params = new List<SqliteParameter>();
        }

        //        public override QueryBuilder ExecuteProcedure(string name, string prefix = "prm", params DataParameter[] parameters)
        //        {
        //            Append("CALL `{0}`(", name);
        //
        //            if (parameters != null && parameters.Length > 0)
        //            {
        //                for (var x = 0; x < parameters.Length; x++)
        //                {
        //                    var xp = parameters[x];
        //
        //                    var paramKey = prefix + xp.Name;
        //                    _params.Add(new SqliteParameter(paramKey, xp.Value));
        //                    Append("?");
        //
        //                    if (x + 1 < parameters.Length)
        //                        Append(",");
        //                }
        //            }
        //
        //            Append(");");
        //            return this;
        //        }

        public override QueryBuilder AddParam(string name, object value, string prefix = "prm")
        {
            var paramKey = prefix + name;
            _params.Add(new SqliteParameter(paramKey, value));
            return this;
        }

        public override QueryBuilder TableExists(string name)
        {
            Append("SELECT 1 FROM sqlite_master WHERE type = 'table' and name = '{0}'", base.GetTableName(name));
            return this;
        }

        public override QueryBuilder TableCreate(string name, params TableColumn[] columns)
        {
            Append("CREATE TABLE {0} (", base.GetTableName(name));

            if (columns != null && columns.Length > 0)
            {
                for (var x = 0; x < columns.Length; x++)
                {
                    var col = columns[x];

                    Append("`");
                    Append(col.Name);
                    Append("`");

                    if (col.DataType == typeof(Byte))
                    {
                        Append(" INT");
                    }
                    else if (col.DataType == typeof(Int16))
                    {
                        Append(" INT");
                    }
                    else if (col.DataType == typeof(Int32))
                    {
                        Append(" INT");
                    }
                    else if (col.DataType == typeof(Int64))
                    {
                        Append(" BIGINT");
                    }
                    else if (col.DataType == typeof(String))
                    {
                        var isVarChar = col.MinScale.HasValue && !col.MaxScale.HasValue;
                        if (isVarChar)
                        {
                            Append(" VARCHAR(");
                            Append(col.MinScale.Value.ToString());
                            Append(")");
                        }
                        else
                        {
                            Append(" TEXT");
                        }
                    }
                    else if (col.DataType == typeof(DateTime))
                    {
                        Append(" DATETIME");
                    }
                    else if (col.DataType == typeof(Boolean))
                    {
                        Append(" BOOLEAN");
                    }
                    else
                    {
                        throw new NotSupportedException(String.Format("Data type for column '{0}' is not supported", col.Name));
                    }

                    if (col.PrimaryKey) //TODO check for numerics
                    {
                        Append(" PRIMARY KEY");
                    }
                    if (col.Nullable)
                    {
                    }
                    else
                    {
                        Append(" NOT NULL");
                    }

                    if (x + 1 < columns.Length)
                        Append(",");
                }
            }
            Append(")");

            return this;
        }

        public override QueryBuilder TableDrop(string name)
        {
            Append("DROP TABLE '{0}'", base.GetTableName(name));
            return this;
        }

//        public override QueryBuilder ProcedureExists(string name)
//        {
//            return this;
//        }
//
//        public override QueryBuilder ProcedureCreate(string name, string contents, params DataParameter[] parameters)
//        {
//            return this;
//        }
//
//        public override QueryBuilder ProcedureDrop(string name)
//        {
//            return this;
//        }

        public override QueryBuilder Select(params string[] expression)
        {
            Append("SELECT ");

            if (expression != null && expression.Length > 0)
            {
                Append(String.Join(",", expression));

                return this.Append(" ");
            }

            return this;
        }

        public override QueryBuilder All()
        {
            Append("* ");
            return this;
        }

        public override QueryBuilder From(string tableName)
        {
            Append("FROM ");
            Append(base.GetTableName(tableName));
            Append(" ");
            return this;
        }

        public override QueryBuilder Where(params WhereFilter[] clause)
        {
            Append("WHERE ");

            if (clause != null && clause.Length > 0)
            {
                for (var x = 0; x < clause.Length; x++)
                {
                    if (x > 0)
                        Append("AND ");

                    var xp = clause[x];

                    Append(xp.Column);

                    switch (xp.Expression)
                    {
                        case WhereExpression.EqualTo:
                            Append(" = ");
                            break;
                        case WhereExpression.NotEqualTo:
                            Append(" = ");
                            break;
                        case WhereExpression.Like:
                            Append(" LIKE ");
                            break;
                    }

                    var paramKey = "prm" + xp.Column;
                    _params.Add(new SqliteParameter(paramKey, xp.Value));
                    Append("?");
                    Append(" ");
                }
            }

            return this;
        }

        public override QueryBuilder Count(string expression = null)
        {
            Append("COUNT(");
            Append(expression ?? "*");
            return Append(") ");
            //return this.Append(fmt, String.Format("COUNT({0})", expression ?? "*"));
        }

        public override QueryBuilder Delete()
        {
            Append("DELETE ");
            return this;
        }

        public override QueryBuilder InsertInto(string tableName, params DataParameter[] values)
        {
            Append("INSERT INTO ");
            Append(base.GetTableName(tableName));

            if (values != null && values.Length > 0)
            {
                //Columns
                Append(" ( ");
                for (var x = 0; x < values.Length; x++)
                {
                    Append(values[x].Name);

                    if (x + 1 < values.Length)
                        Append(",");
                }
                Append(" ) ");

                //Values
                Append(" VALUES ( ");
                for (var x = 0; x < values.Length; x++)
                {
                    var prm = values[x];
                    var paramKey = "prm" + prm.Name;

                    Append("?");
                    if (x + 1 < values.Length)
                        Append(",");

                    _params.Add(new SqliteParameter(paramKey, prm.Value));
                }
                Append(" ) ");
            }
            return this;
        }

        public override QueryBuilder Update(string tableName, DataParameter[] values)
        {
            Append("UPDATE ");
            Append(base.GetTableName(tableName));

            if (values != null && values.Length > 0)
            {
                Append(" SET ");

                for (var x = 0; x < values.Length; x++)
                {
                    var prm = values[x];
                    var paramKey = "prm" + prm.Name;

                    Append(prm.Name);
                    Append("=");
                    Append("?");
                    Append(" ");

                    if (x + 1 < values.Length)
                        Append(",");

                    _params.Add(new SqliteParameter(paramKey, prm.Value));
                }
            }

            return this;
        }
    }
}

