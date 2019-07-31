using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Dominium.Store.Dapper
{
	public static class Upserts
	{
		private static readonly IDictionary<string, string> SqlStatements = new ConcurrentDictionary<string, string>();
		private static readonly IDictionary<Type, string> UpsertSqls = new ConcurrentDictionary<Type, string>();

		static Upserts()
		{
			SqlStatements.Add("NpgsqlConnection", "INSERT INTO {tableName} ({allColumns}) VALUES ({allParameters}) ON CONFLICT ON CONSTRAINT pk_{tableName} DO UPDATE SET {pgUpsertItems}");
			SqlStatements.Add("MySqlConnection", "REPLACE INTO {tableName} ({allColumns}) VALUES ({allParameters})");
			SqlStatements.Add("*DEFAULT*", "MERGE INTO {tableName} AS t USING(SELECT {keysAs} AS n({keys}) ON {mergeKeys} WHEN MATCHED THEN UPDATE SET {mergeValues} WHEN NOT MATCHED THEN INSERT ({allColumns}) VALUES ({allParameters})");
		}

		public static string Get<TRoot>(IDbConnection cnn)
		{
			var sql = SqlStatements[cnn.GetType().Name];
			return FlushSql<TRoot>(sql);
		}


		public static void Add<T>(string sql) where T: IDbConnection
			=> SqlStatements.Add(typeof(T).Name, sql);

		private static string FlushSql<TRoot>(string sql)
		{
			if (UpsertSqls.ContainsKey(typeof(TRoot)))
				return UpsertSqls[typeof(TRoot)];
			
			var keys = DomainKeyAttribute.FromType<TRoot>().ToArray();
			
			UpsertSqls.Add(typeof(TRoot), sql
				.Replace("{tableName}", DbConnectionExtensions.TableName<TRoot>())
				.Replace("{allColumns}", DbConnectionExtensions.AllColumns<TRoot>())
				.Replace("{allParameters}", DbConnectionExtensions.AllParameters<TRoot>())
				.Replace("{keysAs}", KeysAs(keys))
				.Replace("{keys}", string.Join(", ", keys))
				.Replace("{mergeKeys}", MergeKeys(keys))
				.Replace("{mergeValues}", MergeValues(keys))
				.Replace("{pgUpsertItems}", PgAllUpserts<TRoot>()));

			return UpsertSqls[typeof(TRoot)];
		}

		private static string KeysAs(IEnumerable<string> keys)
			=> string.Join(", ", keys.Select(k => $"@{DbConnectionExtensions.ToCamelCase(k)} AS {DbConnectionExtensions.ToSnakeCase(k)}"));

		private static string MergeKeys(IEnumerable<string> keys)
			=> string.Join(" AND ", keys.Select(DbConnectionExtensions.ToSnakeCase).Select(k => $"t.{k} = n.{k}"));

		private static string MergeValues(IEnumerable<string> keys)
			=> string.Join(", ", keys.Select(k => $"{DbConnectionExtensions.ToSnakeCase(k)} = @{DbConnectionExtensions.ToCamelCase(k)}"));

		private static string PgAllUpserts<TRoot>()
			=> string.Join(", ", typeof(TRoot).GetProperties().Select(t => DbConnectionExtensions.ToSnakeCase(t.Name)).Select(p => $"{p} = EXCLUDED.{p}"));
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.