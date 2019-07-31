using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;

namespace Dominium.Store.DynamoDb
{
	public class DynamoDbStore : IStore
	{
		private readonly IAmazonDynamoDB _db;

		public DynamoDbStore(string awsAccessKeyId, string awsSecretAccessKey, string regionName)
			=> _db = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.GetBySystemName(regionName));
		
		public async Task<TRoot> Load<TRoot>(params object[] keyValues) where TRoot: class
		{
			var rootType = typeof(TRoot);
			var keys = DomainKeyAttribute.FromType<TRoot>();
			var attrKey = keys.Zip(keyValues, (k, v) => new {k, v})
				.ToDictionary(x => x.k, x => x.v.ToAttributeValue());
			var item = await _db.GetItemAsync(rootType.Name, attrKey);
			return item.IsItemSet ? item.Item.AsRoot<TRoot>() : null;
		}

		public async Task Save<TRoot>(TRoot root) where TRoot: class
		{
			var rootType = typeof(TRoot);
			await _db.PutItemAsync(rootType.Name, root.GetAttributeValue());
		}
	}
}

// Dominium
// Copyright (C) 2019 Richard A. Fleming (rfleming@acqusys.com)
// This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
// License version 2.1 as published by the Free Software Foundation.  A full copy of the license can be found in the file LICENSE.