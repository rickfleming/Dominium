using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Dominium.Store.Dapper
{
	public static class DbConnectionExtensions
	{
		private static readonly IDictionary<Type, string> _allKeyWhere = new ConcurrentDictionary<Type, string>();
		private static readonly IDictionary<Type, string> _allColumns = new ConcurrentDictionary<Type, string>();
		private static readonly IDictionary<Type, string> _allParameters = new ConcurrentDictionary<Type, string>();
		private static readonly IDictionary<Type, string> _allUpsertItems = new ConcurrentDictionary<Type, string>();

		public static bool IsSnakeCase = false;
		
		public static void UpsertAll<T>(this IDbConnection cnn, IEnumerable<T> items)
		{
			if(items == null)
				return;

			foreach(var item in items)
				cnn.Upsert(item);
		}

		public static async Task UpsertAllAsync<T>(this IDbConnection cnn, IEnumerable<T> items)
		{
			if(items == null)
				return;

			foreach(var item in items)
				await cnn.UpsertAsync(item);
		}

		public static int Upsert<T>(this IDbConnection cnn, T item) =>
			cnn.Execute(new CommandDefinition(Upserts.Get<T>(cnn), item));

		public static async Task<int> UpsertAsync<T>(this IDbConnection cnn, T item) =>
			await cnn.ExecuteAsync(new CommandDefinition(Upserts.Get<T>(cnn), item));

		public static T GetOrDefault<T>(this IDbConnection cnn, object key)
			=> cnn.QueryFirstOrDefault<T>(new CommandDefinition($"SELECT {AllColumns<T>()} FROM {TableName<T>()} WHERE {AllKeyWhere(key.GetType())}", key));

		public static async Task<T> GetOrDefaultAsync<T>(this IDbConnection cnn, object key) =>
			await cnn.QueryFirstOrDefaultAsync<T>($"SELECT {AllColumns<T>()} FROM {TableName<T>()} WHERE {AllKeyWhere(key.GetType())}", key);

		public static IEnumerable<T> GetAll<T>(this IDbConnection cnn) =>
			cnn.Query<T>(new CommandDefinition(GetAllSql<T>()));

		public static async Task<IEnumerable<T>> GetAllAsync<T>(this IDbConnection cnn) =>
			await cnn.QueryAsync<T>(new CommandDefinition(GetAllSql<T>()));

		public static IEnumerable<T> GetAll<T>(this IDbConnection cnn, string where) =>
			cnn.Query<T>(new CommandDefinition(GetAllSql<T>(where)));

		public static async Task<IEnumerable<T>> GetAllAsync<T>(this IDbConnection cnn, string where) =>
			await cnn.QueryAsync<T>(new CommandDefinition(GetAllSql<T>(where)));

		public static SqlMapper.GridReader GetAllMultiple(this IDbConnection cnn, params Type[] types) => cnn.QueryMultiple(string.Join(";" + Environment.NewLine, types.Select(GetAllSql)));
		public static async Task<SqlMapper.GridReader> GetAllMultipleAsync(this IDbConnection cnn, params Type[] types) => await cnn.QueryMultipleAsync(string.Join(";" + Environment.NewLine, types.Select(GetAllSql)));
		

		public static int DeleteAll<T>(this IDbConnection cnn) =>
			cnn.Execute(new CommandDefinition($"TRUNCATE TABLE {TableName<T>()}"));

		public static async Task<int> DeleteAllAsync<T>(this IDbConnection cnn) =>
			await cnn.ExecuteAsync(new CommandDefinition($"TRUNCATE TABLE {TableName<T>()} CASCADE"));

		public static string TableName<T>(this IDbConnection cnn) => TableName<T>();
		public static string AllColumns<T>(this IDbConnection cnn) => AllColumns<T>();

		public static string TableName<T>() => TableName(typeof(T));
		public static string TableName(Type type) => ToSnakeCase(type.Name);
		public static string AllColumns<T>() => AllColumns(typeof(T));

		public static string AllColumns(Type type)
		{
			if (!_allColumns.ContainsKey(type))
				_allColumns.Add(type, string.Join(",", type.GetProperties().Select(x => ToSnakeCase(x.Name)).OrderBy(x => x)));

			return _allColumns[type];
		}

		public static string AllParameters<T>() => AllParameters(typeof(T));

		public static string AllParameters(Type type)
		{
			if (!_allParameters.ContainsKey(type))
				_allParameters.Add(type, string.Join(",", type.GetProperties().Select(x => "@" + ToCamelCase(x.Name)).OrderBy(x => x)));

			return _allParameters[type];
		}

		public static string AllKeyWhere<TKey>() => AllKeyWhere(typeof(TKey));

		public static string AllKeyWhere(Type type)
		{
			if (!_allKeyWhere.ContainsKey(type))
				_allKeyWhere.Add(type, string.Join(" AND ", type.GetProperties().Select(x => $"{x.Name} = @{ToCamelCase(x.Name)}")));

			return _allKeyWhere[type];
		}
		
		private static string GetAllSql<T>() => GetAllSql(typeof(T), "");

		private static string GetAllSql(Type type) => GetAllSql(type, "");

		private static string GetAllSql<T>(string where) => GetAllSql(typeof(T), where);

		private static string GetAllSql(Type type, string where) =>
			$"SELECT {AllColumns(type)} FROM {TableName(type)}" + (AllColumns(type).Contains(ToSnakeCase("IsDeleted"))
				? $" WHERE {ToSnakeCase("IsDeleted")} = FALSE " + (string.IsNullOrWhiteSpace(where) ? "" : " AND " + where)
				: (string.IsNullOrWhiteSpace(where) ? "" : " WHERE " + where));

		public static string ToSnakeCase(string input)
		{
			if (string.IsNullOrEmpty(input) || !IsSnakeCase)
				return input;
			
			var length = input.Length;
			var result = new StringBuilder(length * 2);
			var resultLength = 0;
			var wasPrevTranslated = false;

			var characters = input.ToCharArray();
			for (var i = 0; i < length; i++)
			{
				var c = characters[i];
				// skip first starting underscore
				if (i == 0 && c == '_') continue;
				if (char.IsUpper(c))
				{
					if (!wasPrevTranslated && resultLength > 0 && result[resultLength - 1] != '_')
					{
						result.Append('_');
						resultLength++;
					}
					c = char.ToLower(c);
					wasPrevTranslated = true;
				}
				else
				{
					wasPrevTranslated = false;
				}

				result.Append(c);
				resultLength++;
			}

			return resultLength > 0 ? result.ToString() : input;
		}

		public static string ToCamelCase(string pascalCase) => char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.