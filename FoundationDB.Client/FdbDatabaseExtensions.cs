﻿#region BSD Licence
/* Copyright (c) 2013, Doxense SARL
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Client
{
	using FoundationDB.Layers.Tuples;
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public static class FdbDatabaseExtensions
	{

		/// <summary>Start a new read-only transaction on this database</summary>
		/// <param name="cancellationToken">Optional cancellation token that can abort all pending async operations started by this transaction.</param>
		/// <returns>New transaction instance that can read from the database.</returns>
		/// <remarks>You MUST call Dispose() on the transaction when you are done with it. You SHOULD wrap it in a 'using' statement to ensure that it is disposed in all cases.</remarks>
		/// <example>
		/// using(var tr = db.BeginReadOnlyTransaction(CancellationToken.None))
		/// {
		///		var result = await tr.Get(Slice.FromString("Hello"));
		///		var items = await tr.GetRange(FdbKeyRange.StartsWith(Slice.FromString("ABC"))).ToListAsync();
		/// }</example>
		public static IFdbReadOnlyTransaction BeginReadOnlyTransaction(this IFdbDatabase db, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (db == null) throw new ArgumentNullException("db");
			return db.BeginTransaction(FdbTransactionMode.ReadOnly, cancellationToken);
		}

		/// <summary>Start a new transaction on this database</summary>
		/// <param name="cancellationToken">Optional cancellation token that can abort all pending async operations started by this transaction.</param>
		/// <returns>New transaction instance that can read from or write to the database.</returns>
		/// <remarks>You MUST call Dispose() on the transaction when you are done with it. You SHOULD wrap it in a 'using' statement to ensure that it is disposed in all cases.</remarks>
		/// <example>
		/// using(var tr = db.BeginTransaction(CancellationToken.None))
		/// {
		///		tr.Set(Slice.FromString("Hello"), Slice.FromString("World"));
		///		tr.Clear(Slice.FromString("OldValue"));
		///		await tr.CommitAsync();
		/// }</example>
		public static IFdbTransaction BeginTransaction(this IFdbDatabase db, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (db == null) throw new ArgumentNullException("db");
			return db.BeginTransaction(FdbTransactionMode.Default, cancellationToken);
		}

		#region Options...

		/// <summary>Set the size of the client location cache. Raising this value can boost performance in very large databases where clients access data in a near-random pattern. Defaults to 100000.</summary>
		/// <param name="size">Max location cache entries</param>
		public static void SetLocationCacheSize(this FdbDatabase db, int size)
		{
			if (db == null) throw new ArgumentNullException("db");
			if (size < 0) throw new FdbException(FdbError.InvalidOptionValue, "Location cache size must be a positive integer");

			//REVIEW: we can't really change this to a Property, because we don't have a way to get the current value for the getter, and set only properties are weird...
			//TODO: cache this into a local variable ?
			db.SetOption(FdbDatabaseOption.LocationCacheSize, size);
		}

		/// <summary>Set the maximum number of watches allowed to be outstanding on a database connection. Increasing this number could result in increased resource usage. Reducing this number will not cancel any outstanding watches. Defaults to 10000 and cannot be larger than 1000000.</summary>
		/// <param name="count">Max outstanding watches</param>
		public static void SetMaxWatches(this FdbDatabase db, int count)
		{
			if (db == null) throw new ArgumentNullException("db");
			if (count < 0) throw new FdbException(FdbError.InvalidOptionValue, "Maximum outstanding watches count must be a positive integer");

			//REVIEW: we can't really change this to a Property, because we don't have a way to get the current value for the getter, and set only properties are weird...
			//TODO: cache this into a local variable ?
			db.SetOption(FdbDatabaseOption.MaxWatches, count);
		}

		/// <summary>Specify the machine ID that was passed to fdbserver processes running on the same machine as this client, for better location-aware load balancing.</summary>
		/// <param name="hexId">Hexadecimal ID</param>
		public static void SetMachineId(this FdbDatabase db, string hexId)
		{
			if (db == null) throw new ArgumentNullException("db");
			//REVIEW: we can't really change this to a Property, because we don't have a way to get the current value for the getter, and set only properties are weird...
			//TODO: cache this into a local variable ?
			db.SetOption(FdbDatabaseOption.MachineId, hexId);
		}

		/// <summary>Specify the datacenter ID that was passed to fdbserver processes running in the same datacenter as this client, for better location-aware load balancing.</summary>
		/// <param name="hexId">Hexadecimal ID</param>
		public static void SetDataCenterId(this FdbDatabase db, string hexId)
		{
			if (db == null) throw new ArgumentNullException("db");
			//REVIEW: we can't really change this to a Property, because we don't have a way to get the current value for the getter, and set only properties are weird...
			//TODO: cache this into a local variable ?
			db.SetOption(FdbDatabaseOption.DataCenterId, hexId);
		}

		#endregion

		#region Subspaces...

		/// <summary>Return a new partition of the current database</summary>
		/// <typeparam name="T">Type of the value used for the partition</typeparam>
		/// <param name="value">Prefix of the new partition</param>
		/// <returns>Subspace that is the concatenation of the database global namespace and the specified <paramref name="value"/></returns>
		public static FdbSubspace Partition<T>(this IFdbDatabase db, T value)
		{
			return db.GlobalSpace.Partition<T>(value);
		}

		/// <summary>Return a new partition of the current database</summary>
		/// <returns>Subspace that is the concatenation of the database global namespace and the specified values</returns>
		public static FdbSubspace Partition<T1, T2>(this IFdbDatabase db, T1 value1, T2 value2)
		{
			return db.GlobalSpace.Partition<T1, T2>(value1, value2);
		}

		/// <summary>Return a new partition of the current database</summary>
		/// <returns>Subspace that is the concatenation of the database global namespace and the specified values</returns>
		public static FdbSubspace Partition<T1, T2, T3>(this IFdbDatabase db, T1 value1, T2 value2, T3 value3)
		{
			return db.GlobalSpace.Partition<T1, T2, T3>(value1, value2, value3);
		}

		/// <summary>Return a new partition of the current database</summary>
		/// <returns>Subspace that is the concatenation of the database global namespace and the specified <paramref name="tuple"/></returns>
		public static FdbSubspace Partition(this IFdbDatabase db, IFdbTuple tuple)
		{
			return db.GlobalSpace.Partition(tuple);
		}

		/// <summary>Create a new key by appending a value to the global namespace</summary>
		public static Slice Pack<T>(this IFdbDatabase db, T key)
		{
			return db.GlobalSpace.Pack<T>(key);
		}

		/// <summary>Create a new key by appending two values to the global namespace</summary>
		public static Slice Pack<T1, T2>(this IFdbDatabase db, T1 key1, T2 key2)
		{
			return db.GlobalSpace.Pack<T1, T2>(key1, key2);
		}

		/// <summary>Unpack a key using the current namespace of the database</summary>
		/// <param name="key">Key that should fit inside the current namespace of the database</param>
		public static IFdbTuple Unpack(this IFdbDatabase db, Slice key)
		{
			return db.GlobalSpace.Unpack(key);
		}

		/// <summary>Unpack a key using the current namespace of the database</summary>
		/// <param name="key">Key that should fit inside the current namespace of the database</param>
		public static T UnpackLast<T>(this IFdbDatabase db, Slice key)
		{
			return db.GlobalSpace.UnpackLast<T>(key);
		}

		/// <summary>Unpack a key using the current namespace of the database</summary>
		/// <param name="key">Key that should fit inside the current namespace of the database</param>
		public static T UnpackSingle<T>(this IFdbDatabase db, Slice key)
		{
			return db.GlobalSpace.UnpackSingle<T>(key);
		}

		/// <summary>Add the global namespace prefix to a relative key</summary>
		/// <param name="keyRelative">Key that is relative to the global namespace</param>
		/// <returns>Key that starts with the global namespace prefix</returns>
		/// <example>
		/// // db with namespace prefix equal to"&lt;02&gt;Foo&lt;00&gt;"
		/// db.Concat('&lt;02&gt;Bar&lt;00&gt;') => '&lt;02&gt;Foo&lt;00&gt;&gt;&lt;02&gt;Bar&lt;00&gt;'
		/// db.Concat(Slice.Empty) => '&lt;02&gt;Foo&lt;00&gt;'
		/// db.Concat(Slice.Nil) => Slice.Nil
		/// </example>
		public static Slice Concat(this IFdbDatabase db, Slice keyRelative)
		{
			return db.GlobalSpace.Concat(keyRelative);
		}

		/// <summary>Remove the global namespace prefix of this database form the key, and return the rest of the bytes, or Slice.Nil is the key is outside the namespace</summary>
		/// <param name="keyAbsolute">Binary key that starts with the namespace prefix, followed by some bytes</param>
		/// <returns>Binary key that contain only the bytes after the namespace prefix</returns>
		/// <example>
		/// // db with namespace prefix equal to"&lt;02&gt;Foo&lt;00&gt;"
		/// db.Extract('&lt;02&gt;Foo&lt;00&gt;&lt;02&gt;Bar&lt;00&gt;') => '&gt;&lt;02&gt;Bar&lt;00&gt;'
		/// db.Extract('&lt;02&gt;Foo&lt;00&gt;') => Slice.Empty
		/// db.Extract('&lt;02&gt;TopSecret&lt;00&gt;&lt;02&gt;Password&lt;00&gt;') => Slice.Nil
		/// db.Extract(Slice.Nil) => Slice.Nil
		/// </example>
		public static Slice Extract(this IFdbDatabase db, Slice keyAbsolute)
		{
			return db.GlobalSpace.Extract(keyAbsolute);
		}

		#endregion

		#region System Keys...

		//TODO: move these methods to another subspace ? (ex: 'FoundationDb.Client.System' or 'FoundationDb.Client.Administration' ?)

		/// <summary>Returns a string describing the list of the coordinators for the cluster</summary>
		public static Task<string> GetCoordinatorsAsync(this IFdbDatabase db, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (db == null) throw new ArgumentNullException("db");

			return db.ReadAsync<string>(async (tr) =>
			{
				tr.SetOption(FdbTransactionOption.AccessSystemKeys);
				var result = await tr.GetAsync(Fdb.SystemKeys.Coordinators).ConfigureAwait(false);
				return result.ToAscii();
			}, cancellationToken);
		}

		/// <summary>Return the value of a configuration parameter (located under '\xFF/conf/')</summary>
		/// <param name="name">"storage_engine"</param>
		/// <returns>Value of '\xFF/conf/storage_engine'</returns>
		public static Task<Slice> GetConfigParameter(this IFdbDatabase db, string name, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (db == null) throw new ArgumentNullException("db");
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Configuration parameter name cannot be null or empty", "name");

			return db.ReadAsync<Slice>((tr) =>
			{
				tr.SetOption(FdbTransactionOption.AccessSystemKeys);
				return tr.GetAsync(Fdb.SystemKeys.GetConfigKey(name));
			}, cancellationToken);
		}

		#endregion

	}

}
