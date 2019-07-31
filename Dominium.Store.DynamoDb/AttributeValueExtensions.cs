using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace Dominium.Store.DynamoDb
{
	internal static class AttributeValueExtensions
	{
		private static readonly Type[] SimpleTypes = {
			typeof(string), typeof(bool), typeof(DateTime), typeof(short), typeof(int), typeof(long), typeof(decimal),
			typeof(float), typeof(double), typeof(byte)
		};

		public static TRoot AsRoot<TRoot>(this Dictionary<string, AttributeValue> target) where TRoot: class
			=> (TRoot)AsRoot(target, typeof(TRoot));

		public static AttributeValue ToAttributeValue(this object target)
		{
			var result = FromSimpleType(target) ?? FromEnumerableType(target);
			if (result != null)
				return result;

			var targetType = target.GetType();

			if (!IsSimpleType(targetType) && !IsEnumerableType(targetType))
				return new AttributeValue {M = target.GetAttributeValue(), IsMSet = true};

			if (!IsEnumerableType(targetType))
				return null;

			return new AttributeValue
			{
				L = ((IEnumerable<object>) target).Select(v => new AttributeValue {M = v.GetAttributeValue(), IsMSet = true}).ToList(),
				IsLSet = true
			};
		}
		
		public static Dictionary<string, AttributeValue> GetAttributeValue(this object target)
		{
			var props = target.GetType().GetProperties();
			var simpleValues = props
				.Select(p => new KeyValuePair<string, AttributeValue>(p.Name, FromSimpleType(p.GetValue(target))))
				.Where(k => k.Value != null);
			
			var enumerableValues = props
				.Select(p => new KeyValuePair<string, AttributeValue>(p.Name, FromEnumerableType(p.GetValue(target))))
				.Where(k => k.Value != null);

			var complexValues = props
				.Where(p => !IsSimpleType(p.PropertyType) && !IsEnumerableType(p.PropertyType))
				.Select(p => new KeyValuePair<string, AttributeValue>(p.Name, new AttributeValue {M = p.GetValue(target).GetAttributeValue(), IsMSet = true}))
				.Where(k => k.Value != null);
			
			var enumerableComplexValues = props
				.Where(p => IsEnumerableType(p.PropertyType))
				.Where(p =>
				{
					var genericType = p.PropertyType.GetGenericArguments().FirstOrDefault();
					return !(genericType == null || IsSimpleType(genericType) || typeof(IEnumerable<byte>).IsAssignableFrom(genericType));
				})
				.Select(p => new KeyValuePair<string, AttributeValue>(p.Name, new AttributeValue
				{
					L = ((IEnumerable<object>) p.GetValue(target)).Select(v => new AttributeValue { M = v.GetAttributeValue(), IsMSet = true }).ToList(), 
					IsLSet = true
				}))
				.Where(k => k.Value != null);

			return simpleValues.Concat(enumerableValues).Concat(complexValues).Concat(enumerableComplexValues)
				.ToDictionary(k => k.Key, k => k.Value);
		}
		
		private static object AsRoot(this Dictionary<string, AttributeValue> target, Type rootType)
		{
			var constructor = rootType.GetConstructors()
				.OrderByDescending(c => c.GetParameters().Length)
				.FirstOrDefault();

			if (constructor == null)
				return null;

			var parameters = constructor.GetParameters().OrderBy(p => p.Position).ToArray();
			var dict = new Dictionary<string, AttributeValue>(target, StringComparer.OrdinalIgnoreCase);
			
			var setParams = new List<object>();
			foreach (var par in parameters)
			{
				var value = dict[par.Name];
				
				if (IsSimpleType(par.ParameterType))
					setParams.Add(ToSimpleType(value, par.ParameterType));
				else if (IsEnumerableType(par.ParameterType))
				{
					var genericType = par.ParameterType.GetGenericArguments().FirstOrDefault();
					setParams.Add(IsSimpleType(genericType) ? ToEnumerableType(value, genericType) : value.L.Select(l => l.M.AsRoot(par.ParameterType)));
				}
				else
					setParams.Add(value.M.AsRoot(par.ParameterType));
			}

			var result = constructor.Invoke(setParams.ToArray());
			var propDict = dict.Where(d => !parameters.Any(p => p.Name.Equals(d.Key, StringComparison.OrdinalIgnoreCase)));
			var props = rootType.GetProperties().Where(p => propDict.Any(d => d.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase)));
			foreach (var prop in props)
			{
				var value = dict[prop.Name];
				if (IsSimpleType(prop.PropertyType))
					prop.SetValue(result, ToSimpleType(value, prop.PropertyType));
				else if (IsEnumerableType(prop.PropertyType))
				{
					var genericType = prop.PropertyType.GetGenericArguments().FirstOrDefault();
					prop.SetValue(result, IsSimpleType(genericType)
						? ToEnumerableType(value, genericType)
						: value.L.Select(l => l.M.AsRoot(prop.PropertyType)));
				}
				else
					prop.SetValue(result, value.M.AsRoot(prop.PropertyType));
			}

			return result;
		}

		private static bool IsEnumerableType(Type type)
		{
			if (!type.IsConstructedGenericType)
				return false;
			
			var genericType = type.GenericTypeArguments.FirstOrDefault();
			if (genericType == null)
				return false;

			var comparisonEnumerable = typeof(IEnumerable<>).MakeGenericType(genericType);
			return comparisonEnumerable.IsAssignableFrom(type);
		}

		private static object ToEnumerableType(AttributeValue value, Type genericType)
		{
			switch (Type.GetTypeCode(genericType))
			{
				case TypeCode.String:
					return value.SS;
				case TypeCode.Boolean:
					return value.SS.Select(bool.Parse);
				case TypeCode.DateTime:
					return value.SS.Select(DateTime.Parse);
				case TypeCode.Int16:
					return value.NS.Select(short.Parse);
				case TypeCode.Int32:
					return value.NS.Select(int.Parse);
				case TypeCode.Int64:
					return value.NS.Select(long.Parse);
				case TypeCode.Decimal:
					return value.NS.Select(decimal.Parse);
				case TypeCode.Single:
					return value.NS.Select(float.Parse);
				case TypeCode.Double:
					return value.NS.Select(double.Parse);
				case TypeCode.Byte:
					return value.B.ToArray().AsEnumerable();
				case TypeCode.Char:
					return value.SS.Select(char.Parse);
			}

			return typeof(IEnumerable<byte>).IsAssignableFrom(genericType)
				? value.BS.Select(b => b.ToArray().AsEnumerable())
				: Activator.CreateInstance(typeof(List<>).MakeGenericType(genericType));
		}

		private static AttributeValue FromEnumerableType(object value)
		{
			if (!IsEnumerableType(value.GetType()))
				return null;
			
			var genericType = value.GetType().GenericTypeArguments.FirstOrDefault();
			if (!IsSimpleType(genericType) || !typeof(IEnumerable<char>).IsAssignableFrom(genericType))
				return null;
			
			switch (Type.GetTypeCode(genericType))
			{
				case TypeCode.String:
					return new AttributeValue(((IEnumerable<string>) value).ToList());
				case TypeCode.Boolean:
					return new AttributeValue(((IEnumerable<bool>)value).Select(v => v.ToString()).ToList());
				case TypeCode.DateTime:
					return new AttributeValue(((IEnumerable<DateTime>) value).Select(v => v.ToString("o")).ToList());
				case TypeCode.Int16:
					return new AttributeValue { NS = ((IEnumerable<short>) value).Select(v => v.ToString()).ToList() };
				case TypeCode.Int32:
					return new AttributeValue { NS = ((IEnumerable<int>) value).Select(v => v.ToString()).ToList() };
				case TypeCode.Int64:
					return new AttributeValue { NS = ((IEnumerable<long>) value).Select(v => v.ToString()).ToList() };
				case TypeCode.Decimal:
					return new AttributeValue { NS = ((IEnumerable<decimal>) value).Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList() };
				case TypeCode.Single:
					return new AttributeValue { NS = ((IEnumerable<float>) value).Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList() };
				case TypeCode.Double:
					return new AttributeValue { NS = ((IEnumerable<double>) value).Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList() };
				case TypeCode.Byte:
					return new AttributeValue { B = new MemoryStream(((IEnumerable<byte>) value).ToArray()) };
				case TypeCode.Char:
					return new AttributeValue(((IEnumerable<char>) value).Select(v => v.ToString()).ToList());
			}
			
			return typeof(IEnumerable<byte>).IsAssignableFrom(genericType)
				? new AttributeValue { BS = ((IEnumerable<IEnumerable<byte>>)value).Select(v => new MemoryStream(v.ToArray())).ToList() }
				: null;
		}


		private static object ToSimpleType(AttributeValue value, Type type)
		{
			if (type.IsEnum)
				return Enum.Parse(type, value.S);
			
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.String:
					return value.S;
				case TypeCode.Boolean:
					return value.IsBOOLSet && value.BOOL;
				case TypeCode.DateTime:
					return DateTime.Parse(value.S);
				case TypeCode.Int16:
					return short.Parse(value.N);
				case TypeCode.Int32:
					return int.Parse(value.N);
				case TypeCode.Int64:
					return long.Parse(value.N);
				case TypeCode.Decimal:
					return decimal.Parse(value.N);
				case TypeCode.Single:
					return float.Parse(value.N);
				case TypeCode.Double:
					return double.Parse(value.N);
				case TypeCode.Byte:
					return byte.Parse(value.N);
				case TypeCode.Char:
					return value.S.Length > 0 ? value.S[0] : 0;
			}

			return Activator.CreateInstance(type);
		}
		
		private static AttributeValue FromSimpleType(object value)
		{
			if (value == null || !IsSimpleType(value.GetType()))
				return null;

			switch (value)
			{
				case string s:
					return new AttributeValue(s);
				case bool b:
					return new AttributeValue {IsBOOLSet = true, BOOL = b};
				case DateTime d:
					return new AttributeValue(d.ToUniversalTime().ToString("O"));
				case short sh:
					return new AttributeValue { N = sh.ToString() };
				case int i:
					return new AttributeValue { N = i.ToString() };
				case long l:
					return new AttributeValue { N = l.ToString() };
				case decimal de:
					return new AttributeValue { N = de.ToString(CultureInfo.InvariantCulture) };
				case float f:
					return new AttributeValue { N = f.ToString(CultureInfo.InvariantCulture) };
				case double dou:
					return new AttributeValue { N = dou.ToString(CultureInfo.InvariantCulture) };
				case byte by:
					return new AttributeValue { N = by.ToString() };
				case char _:
					return new AttributeValue(value.ToString());
			}

			return value.GetType().IsEnum ? new AttributeValue(Enum.GetName(value.GetType(), value)) : null;
		}

		
		private static bool IsSimpleType(Type type) => SimpleTypes.Any(t => t == type) || type.IsEnum;
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.