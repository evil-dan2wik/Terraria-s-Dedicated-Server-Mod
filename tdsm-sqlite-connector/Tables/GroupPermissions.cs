﻿using System;
using TDSM.API.Data;
using TDSM.API.Logging;

namespace TDSM.Data.SQLite
{
    public class GroupPermissions
    {
        private class TableDefinition
        {
            public const String TableName = "GroupPermissions";

            public static class ColumnNames
            {
                public const String Id = "Id";
                public const String GroupId = "GroupId";
                public const String PermissionId = "PermissionId";
            }

            public static readonly TableColumn[] Columns = new TableColumn[]
                {
                    new TableColumn(ColumnNames.Id, typeof(Int32), true, true),
                    new TableColumn(ColumnNames.GroupId, typeof(Int32)),
                    new TableColumn(ColumnNames.PermissionId, typeof(Int32))
                };

            public static bool Exists(SQLiteConnector conn)
            {
                using (var bl = new SQLiteQueryBuilder(Plugin.SQLSafeName))
                {
                    bl.TableExists(TableName);

                    return ((IDataConnector)conn).Execute(bl);
                }
            }

            public static bool Create(SQLiteConnector conn)
            {
                using (var bl = new SQLiteQueryBuilder(Plugin.SQLSafeName))
                {
                    bl.TableCreate(TableName, Columns);

                    return ((IDataConnector)conn).ExecuteNonQuery(bl) > 0;
                }
            }
        }

        public void Initialise(SQLiteConnector conn)
        {
            if (!TableDefinition.Exists(conn))
            {
                ProgramLog.Admin.Log("Group permissions table does not exist and will now be created");
                TableDefinition.Create(conn);
            }
        }
    }
}

