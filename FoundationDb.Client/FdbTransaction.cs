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
	* Neither the name of the <organization> nor the
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

// enable this to help debug Transactions
#undef DEBUG_TRANSACTIONS

namespace FoundationDb.Client
{
	using FoundationDb.Client.Native;
	using FoundationDb.Layers.Tuples;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>Wraps an FDB_TRANSACTION handle</summary>
	public class FdbTransaction : IDisposable
	{

		#region Private Members...

		private readonly FdbDatabase m_database;
		private readonly int m_id;
		private readonly TransactionHandle m_handle;
		private bool m_disposed;
		/// <summary>Estimated size of written data (in bytes)</summary>
		private int m_payloadBytes;

		#endregion

		#region Constructors...

		internal FdbTransaction(FdbDatabase database, int id, TransactionHandle handle)
		{
			m_database = database;
			m_id = id;
			m_handle = handle;
		}

		#endregion

		#region Public Members...

		public int Id { get { return m_id; } }

		public FdbDatabase Database { get { return m_database; } }

		internal TransactionHandle Handle { get { return m_handle; } }

		internal bool StillAlive { get { return !m_disposed; } }

		public int Size { get { return m_payloadBytes; } }

		#endregion

		#region Options..

		/// <summary>Allows this transaction to read and modify system keys (those that start with the byte 0xFF)</summary>
		public void WithAccessToSystemKeys()
		{
			SetOption(FdbTransactionOption.AccessSystemKeys, null);
		}

		/// <summary>Specifies that this transaction should be treated as highest priority and that lower priority transactions should block behind this one. Use is discouraged outside of low-level tools</summary>
		public void WithPrioritySystemImmediate()
		{
			SetOption(FdbTransactionOption.PrioritySystemImmediate, null);
		}

		/// <summary>Specifies that this transaction should be treated as low priority and that default priority transactions should be processed first. Useful for doing batch work simultaneously with latency-sensitive work</summary>
		public void WithPriorityBatch()
		{
			SetOption(FdbTransactionOption.PriorityBatch, null);
		}

		/// <summary>Set a parameter-less option on this transaction</summary>
		/// <param name="option">Option to set</param>
		public void SetOption(FdbTransactionOption option)
		{
			SetOption(option, default(string));
		}

		/// <summary>Set an option on this transaction</summary>
		/// <param name="option">Option to set</param>
		/// <param name="value">Value of the parameter</param>
		public void SetOption(FdbTransactionOption option, string value)
		{
			ThrowIfDisposed();

			Fdb.EnsureNotOnNetworkThread();

			var data = FdbNative.ToNativeString(value, nullTerminated: true);
			unsafe
			{
				fixed (byte* ptr = data.Array)
				{
					FdbNative.TransactionSetOption(m_handle, option, ptr + data.Offset, data.Count);
				}
			}
		}

		#endregion

		#region Versions...

		/// <summary>Returns this transaction snapshot read version.</summary>
		public Task<long> GetReadVersionAsync(CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();

			Fdb.EnsureNotOnNetworkThread();

			var future = FdbNative.TransactionGetReadVersion(m_handle);
			return FdbFuture.CreateTaskFromHandle(future,
				(h) =>
				{
					long version;
					var err = FdbNative.FutureGetVersion(h, out version);
#if DEBUG_TRANSACTIONS
					Debug.WriteLine("FdbTransaction[" + m_id + "].GetReadVersion() => err=" + err + ", version=" + version);
#endif
					Fdb.DieOnError(err);
					return version;
				},
				ct
			);
		}

		/// <summary>Retrieves the database version number at which a given transaction was committed.</summary>
		/// <returns>Version number, or -1 if this transaction was not committed (or did nothing)</returns>
		/// <remarks>The value return by this method is undefined if Commit has not been called</remarks>
		public long GetCommittedVersion()
		{
			ThrowIfDisposed();

			Fdb.EnsureNotOnNetworkThread();

			long version;
			var err = FdbNative.TransactionGetCommittedVersion(m_handle, out version);
#if DEBUG_TRANSACTIONS
			Debug.WriteLine("FdbTransaction[" + m_id + "].GetCommittedVersion() => err=" + err + ", version=" + version);
#endif
			Fdb.DieOnError(err);
			return version;
		}

		public void SetReadVersion(long version)
		{
			ThrowIfDisposed();

			Fdb.EnsureNotOnNetworkThread();

			FdbNative.TransactionSetReadVersion(m_handle, version);
		}

		#endregion

		#region Get...

		private static bool TryGetValueResult(FutureHandle h, out Slice result)
		{
			bool present;
			var err = FdbNative.FutureGetValue(h, out present, out result);
#if DEBUG_TRANSACTIONS
			Debug.WriteLine("FdbTransaction[].TryGetValueResult() => err=" + err + ", present=" + present + ", valueLength=" + result.Count);
#endif
			Fdb.DieOnError(err);
			return present;
		}

		private static Slice GetValueResultBytes(FutureHandle h)
		{
			Slice result;
			if (!TryGetValueResult(h, out result))
			{
				return Slice.Nil;
			}
			return result;
		}

		internal Task<Slice> GetCoreAsync(Slice key, bool snapshot, CancellationToken ct)
		{
			m_database.EnsureKeyIsValid(key);

			var future = FdbNative.TransactionGet(m_handle, key, snapshot);
			return FdbFuture.CreateTaskFromHandle(future, (h) => GetValueResultBytes(h), ct);
		}

		internal Slice GetCore(Slice key, bool snapshot, CancellationToken ct)
		{
			m_database.EnsureKeyIsValid(key);

			var handle = FdbNative.TransactionGet(m_handle, key, snapshot);
			using (var future = FdbFuture.FromHandle(handle, (h) => GetValueResultBytes(h), ct, willBlockForResult: true))
			{
				return future.GetResult();
			}
		}

		/// <summary>Returns the value of a particular key</summary>
		/// <param name="key">Key to retrieve</param>
		/// <param name="snapshot"></param>
		/// <param name="ct">CancellationToken used to cancel this operation</param>
		/// <returns>Task that will return the value of the key if it is found, null if the key does not exist, or an exception</returns>
		/// <exception cref="System.ArgumentException">If the key is null or empty</exception>
		/// <exception cref="System.OperationCanceledException">If the cancellation token is already triggered</exception>
		/// <exception cref="System.ObjectDisposedException">If the transaction has already been completed</exception>
		/// <exception cref="System.InvalidOperationException">If the operation method is called from the Network Thread</exception>
		public Task<Slice> GetAsync(IFdbTuple key, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			return GetAsync(key.ToSlice(), snapshot, ct);
		}

		/// <summary>Returns the value of a particular key</summary>
		/// <param name="keyBytes">Key to retrieve</param>
		/// <param name="snapshot"></param>
		/// <param name="ct">CancellationToken used to cancel this operation</param>
		/// <returns>Task that will return null if the value of the key if it is found, null if the key does not exist, or an exception</returns>
		/// <exception cref="System.ArgumentException">If the key is null or empty</exception>
		/// <exception cref="System.OperationCanceledException">If the cancellation token is already triggered</exception>
		/// <exception cref="System.ObjectDisposedException">If the transaction has already been completed</exception>
		/// <exception cref="System.InvalidOperationException">If the operation method is called from the Network Thread</exception>
		public Task<Slice> GetAsync(Slice keyBytes, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ct.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			return GetCoreAsync(keyBytes, snapshot, ct);
		}

		/// <summary>Returns the value of a particular key</summary>
		/// <param name="key">Key to retrieve</param>
		/// <param name="snapshot"></param>
		/// <param name="ct">CancellationToken used to cancel this operation</param>
		/// <returns>Returns the value of the key if it is found, or null if the key does not exist</returns>
		/// <exception cref="System.ArgumentException">If the key is null or empty</exception>
		/// <exception cref="System.OperationCanceledException">If the cancellation token is already triggered</exception>
		/// <exception cref="System.ObjectDisposedException">If the transaction has already been completed</exception>
		/// <exception cref="System.InvalidOperationException">If the operation method is called from the Network Thread</exception>
		public Slice Get(IFdbTuple key, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			return Get(key.ToSlice(), snapshot, ct);
		}

		/// <summary>Returns the value of a particular key</summary>
		/// <param name="keyBytes">Key to retrieve (UTF-8)</param>
		/// <param name="snapshot"></param>
		/// <param name="ct">CancellationToken used to cancel this operation</param>
		/// <returns>Returns the value of the key if it is found, or null if the key does not exist</returns>
		/// <exception cref="System.ArgumentException">If the key is null or empty</exception>
		/// <exception cref="System.OperationCanceledException">If the cancellation token is already triggered</exception>
		/// <exception cref="System.ObjectDisposedException">If the transaction has already been completed</exception>
		/// <exception cref="System.InvalidOperationException">If the operation method is called from the Network Thread</exception>
		public Slice Get(Slice keyBytes, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();
			Fdb.EnsureNotOnNetworkThread();

			return GetCore(keyBytes, snapshot, ct);
		}

		public Task<List<KeyValuePair<int, Slice>>> GetBatchIndexedAsync(IEnumerable<Slice> keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ct.ThrowIfCancellationRequested();
			return GetBatchIndexedAsync(keys.ToArray(), snapshot, ct);
		}

		public async Task<List<KeyValuePair<int, Slice>>> GetBatchIndexedAsync(Slice[] keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();

			ct.ThrowIfCancellationRequested();

			Fdb.EnsureNotOnNetworkThread();

			var tasks = new List<Task<Slice>>(keys.Length);
			for (int i = 0; i < keys.Length; i++)
			{
				tasks.Add(GetCoreAsync(keys[i], snapshot, ct));
			}

			var results = await Task.WhenAll(tasks).ConfigureAwait(false);

			return results
				.Select((data, i) => new KeyValuePair<int, Slice>(i, data))
				.ToList();
		}

		public Task<List<KeyValuePair<Slice, Slice>>> GetBatchAsync(IEnumerable<Slice> keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ct.ThrowIfCancellationRequested();
			return GetBatchAsync(keys.ToArray(), snapshot, ct);
		}

		public async Task<List<KeyValuePair<Slice, Slice>>> GetBatchAsync(Slice[] keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			var indexedResults = await GetBatchIndexedAsync(keys, snapshot, ct);

			ct.ThrowIfCancellationRequested();

			return indexedResults
				.Select((kvp) => new KeyValuePair<Slice, Slice>(keys[kvp.Key], kvp.Value))
				.ToList();
		}

		public Task<List<KeyValuePair<int, Slice>>> GetBatchIndexedAsync(IEnumerable<IFdbTuple> keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ct.ThrowIfCancellationRequested();
			return GetBatchIndexedAsync(keys.ToArray(), snapshot, ct);
		}

		public async Task<List<KeyValuePair<int, Slice>>> GetBatchIndexedAsync(IFdbTuple[] keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();

			ct.ThrowIfCancellationRequested();

			Fdb.EnsureNotOnNetworkThread();

			var tasks = new List<Task<Slice>>(keys.Length);
			for (int i = 0; i < keys.Length; i++)
			{
				tasks.Add(this.GetCoreAsync(keys[i].ToSlice(), snapshot, ct));
			}

			var results = await Task.WhenAll(tasks).ConfigureAwait(false);

			return results
				.Select((data, i) => new KeyValuePair<int, Slice>(i, data))
				.ToList();
		}

		public Task<List<KeyValuePair<IFdbTuple, Slice>>> GetBatchAsync(IEnumerable<IFdbTuple> keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ct.ThrowIfCancellationRequested();
			return GetBatchAsync(keys.ToArray(), snapshot, ct);
		}

		public async Task<List<KeyValuePair<IFdbTuple, Slice>>> GetBatchAsync(IFdbTuple[] keys, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			var indexedResults = await GetBatchIndexedAsync(keys, snapshot, ct);

			ct.ThrowIfCancellationRequested();

			// maps the index back to the original key
			return indexedResults
				.Select((kvp) => new KeyValuePair<IFdbTuple, Slice>(keys[kvp.Key], kvp.Value))
				.ToList();
		}

		#endregion

		#region GetRange...

		internal static FdbKeySelector ToSelector(Slice slice)
		{
			//TODO: check for null ? check for count == 0 ?
			return FdbKeySelector.FirstGreaterOrEqual(slice);
		}

		internal FdbRangeResults GetRangeCore(FdbKeySelector begin, FdbKeySelector end, int limit, int targetBytes, FdbStreamingMode mode, bool snapshot, bool reverse)
		{
			m_database.EnsureKeyIsValid(begin.Key);
			m_database.EnsureKeyIsValid(end.Key);

			var query = new FdbRangeQuery
			{
				Begin = begin, 
				End = end, 
				Limit = limit, 
				TargetBytes = targetBytes, 
				Mode = mode, 
				Snapshot = snapshot, 
				Reverse = reverse,
			};

			return new FdbRangeResults(this, query);
		}

		public FdbRangeResults GetRange(Slice beginInclusive, Slice endExclusive, int limit = 0, bool snapshot = false, bool reverse = false)
		{
			if (beginInclusive.IsNullOrEmpty) beginInclusive = FdbKey.MinValue;
			if (endExclusive.IsNullOrEmpty) endExclusive = FdbKey.MaxValue;

			return GetRangeCore(
				ToSelector(beginInclusive),
				ToSelector(endExclusive),
				limit,
				0,
				FdbStreamingMode.WantAll,
				snapshot,
				reverse
			);
		}

		public FdbRangeResults GetRange(IFdbTuple beginInclusive, IFdbTuple endExclusive, int limit = 0, bool snapshot = false, bool reverse = false)
		{
			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			var begin = beginInclusive != null ? beginInclusive.ToSlice() : FdbKey.MinValue;
			var end = endExclusive != null ? endExclusive.ToSlice() : FdbKey.MaxValue;

			return GetRangeCore(
				ToSelector(begin),
				ToSelector(end),
				limit,
				0,
				FdbStreamingMode.WantAll,
				snapshot,
				reverse
			);
		}

		public FdbRangeResults GetRangeInclusive(FdbKeySelector beginInclusive, FdbKeySelector endInclusive, int limit = 0, bool snapshot = false, bool reverse = false)
		{
			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			return GetRangeCore(beginInclusive, endInclusive + 1, limit, 0, FdbStreamingMode.WantAll, snapshot, reverse);
		}

		public FdbRangeResults GetRangeStartsWith(Slice prefix, int limit = 0, bool snapshot = false, bool reverse = false)
		{
			if (!prefix.HasValue) throw new ArgumentOutOfRangeException("prefix");

			var range = FdbKeyRange.FromPrefix(prefix);

			return GetRange(range.Begin, range.End, limit, snapshot, reverse);
		}

		public FdbRangeResults GetRangeStartsWith(IFdbTuple suffix, int limit = 0, bool snapshot = false, bool reverse = false)
		{
			if (suffix == null) throw new ArgumentNullException("suffix");

			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			var range = suffix.ToRange();

			return GetRangeCore(
				FdbKeySelector.FirstGreaterOrEqual(range.Begin),
				FdbKeySelector.FirstGreaterThan(range.End),
				limit,
				0,
				FdbStreamingMode.WantAll,
				snapshot,
				reverse
			);
		}

		public FdbRangeResults GetRange(FdbKeySelector beginInclusive, FdbKeySelector endExclusive, int limit = 0, bool snapshot = false, bool reverse = false)
		{
			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			return GetRangeCore(beginInclusive, endExclusive, limit, 0, FdbStreamingMode.WantAll, snapshot, reverse);
		}

		public FdbRangeResults GetRange(FdbKeySelector beginInclusive, FdbKeySelector endExclusive, int limit, int targetBytes, FdbStreamingMode mode, bool snapshot, bool reverse)
		{
			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			return GetRangeCore(beginInclusive, endExclusive, limit, targetBytes, mode, snapshot, reverse);
		}

		#endregion

		#region GetKey...

		private static Slice GetKeyResult(FutureHandle h)
		{
			Slice result;
			var err = FdbNative.FutureGetKey(h, out result);
#if DEBUG_TRANSACTIONS
			Debug.WriteLine("FdbTransaction[].GetKeyResult() => err=" + err + ", result=" + FdbKey.Dump(result));
#endif
			Fdb.DieOnError(err);
			return result;
		}

		internal Task<Slice> GetKeyCoreAsync(FdbKeySelector selector, bool snapshot, CancellationToken ct)
		{
			m_database.EnsureKeyIsValid(selector.Key);

			var future = FdbNative.TransactionGetKey(m_handle, selector, snapshot);
			return FdbFuture.CreateTaskFromHandle(
				future,
				(h) => GetKeyResult(h),
				ct
			);
		}

		public Task<Slice> GetKeyAsync(FdbKeySelector selector, bool snapshot = false, CancellationToken ct = default(CancellationToken))
		{
			ct.ThrowIfCancellationRequested();
			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			return GetKeyCoreAsync(selector, snapshot, ct);
		}

		#endregion

		#region Set...

		internal void SetCore(Slice key, Slice value)
		{
			m_database.EnsureKeyIsValid(key);
			Fdb.EnsureValueIsValid(value);

			FdbNative.TransactionSet(m_handle, key, value);
			Interlocked.Add(ref m_payloadBytes, key.Count + value.Count);
		}

		public void Set(Slice keyBytes, Slice valueBytes)
		{
			ThrowIfDisposed();
			//Fdb.EnsureNotOnNetworkThread();

			SetCore(keyBytes, valueBytes);
		}

		public void Set(IFdbTuple key, Slice valueBytes)
		{
			if (key == null) throw new ArgumentNullException("key");

			ThrowIfDisposed();
			//Fdb.EnsureNotOnNetworkThread();

			SetCore(key.ToSlice(), valueBytes);
		}

		#endregion

		#region Clear...

		internal void ClearCore(Slice key)
		{
			m_database.EnsureKeyIsValid(key);

			FdbNative.TransactionClear(m_handle, key);
			Interlocked.Add(ref m_payloadBytes, key.Count);
		}

		public void Clear(Slice key)
		{
			ThrowIfDisposed();
			//Fdb.EnsureNotOnNetworkThread();

			ClearCore(key);
		}

		public void Clear(IFdbTuple key)
		{
			if (key == null) throw new ArgumentNullException("key");

			ThrowIfDisposed();
			//Fdb.EnsureNotOnNetworkThread();

			ClearCore(key.ToSlice());
		}

		#endregion

		#region Clear Range...

		internal void ClearRangeCore(Slice beginKeyInclusive, Slice endKeyExclusive)
		{
			m_database.EnsureKeyIsValid(beginKeyInclusive);
			m_database.EnsureKeyIsValid(endKeyExclusive);

			FdbNative.TransactionClearRange(m_handle, beginKeyInclusive, endKeyExclusive);
			//TODO: how to account for these ?
			//Interlocked.Add(ref m_payloadBytes, beginKey.Count);
			//Interlocked.Add(ref m_payloadBytes, endKey.Count);
		}

		/// <summary>
		/// Modify the database snapshot represented by transaction to remove all keys (if any) which are lexicographically greater than or equal to the given begin key and lexicographically less than the given end_key.
		/// Sets and clears affect the actual database only if transaction is later committed with fdb_transaction_commit().
		/// </summary>
		/// <param name="beginKeyInclusive"></param>
		/// <param name="endKeyExclusive"></param>
		public void ClearRange(Slice beginKeyInclusive, Slice endKeyExclusive)
		{
			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			ClearRangeCore(beginKeyInclusive, endKeyExclusive);
		}

		public void ClearRange(IFdbTuple beginInclusive, IFdbTuple endExclusive)
		{
			if (beginInclusive == null) throw new ArgumentNullException("beginInclusive");
			if (endExclusive == null) throw new ArgumentNullException("endExclusive");

			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			ClearRangeCore(beginInclusive.ToSlice(), endExclusive.ToSlice());
		}

		public void ClearRange(IFdbTuple prefix)
		{
			if (prefix == null) throw new ArgumentNullException("prefix");

			ThrowIfDisposed();
			Fdb.EnsureNotOnNetworkThread();

			var range = prefix.ToRange();
			ClearRangeCore(range.Begin, range.End);
		}

		#endregion

		#region Commit...

		public Task CommitAsync(CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();

			ct.ThrowIfCancellationRequested();

			Fdb.EnsureNotOnNetworkThread();

			var future = FdbNative.TransactionCommit(m_handle);
			return FdbFuture.CreateTaskFromHandle<object>(future, (h) => null, ct);
		}

		public void Commit(CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();

			ct.ThrowIfCancellationRequested();

			Fdb.EnsureNotOnNetworkThread();

			FutureHandle handle = null;
			try
			{
				// calls fdb_transaction_commit
				handle = FdbNative.TransactionCommit(m_handle);
				using (var future = FdbFuture.FromHandle<object>(handle, (h) => null, ct, willBlockForResult: true))
				{
					future.Wait();
				}
			}
			catch (Exception)
			{
				if (handle != null) handle.Dispose();
				throw;
			}
		}

		#endregion

		#region OnError...

		public Task OnErrorAsync(FdbError code, CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();

			ct.ThrowIfCancellationRequested();

			Fdb.EnsureNotOnNetworkThread();

			var future = FdbNative.TransactionOnError(m_handle, code);
			return FdbFuture.CreateTaskFromHandle<object>(future, (h) => null, ct);
		}

		public void OnError(FdbError code, CancellationToken ct = default(CancellationToken))
		{
			ThrowIfDisposed();

			ct.ThrowIfCancellationRequested();

			Fdb.EnsureNotOnNetworkThread();

			FutureHandle handle = null;
			try
			{
				// calls fdb_transaction_on_error
				handle = FdbNative.TransactionOnError(m_handle, code);
				using (var future = FdbFuture.FromHandle<object>(handle, (h) => null, ct, willBlockForResult: true))
				{
					future.Wait();
				}
			}
			catch (Exception)
			{
				if (handle != null) handle.Dispose();
				throw;
			}
		}

		#endregion

		#region Reset/Rollback...

		/// <summary>Reset the transaction to its initial state.</summary>
		public void Reset()
		{
			ThrowIfDisposed();

			Fdb.EnsureNotOnNetworkThread();

			FdbNative.TransactionReset(m_handle);
		}

		/// <summary>Rollback this transaction, and dispose it. It should not be used after that.</summary>
		public void Rollback()
		{
			//TODO: refactor code between Rollback() and Dispose() ?
			this.Dispose();
		}

		#endregion

		#region IDisposable...

		private void ThrowIfDisposed()
		{
			if (m_disposed) throw new ObjectDisposedException(null);
			// also checks that the DB has not been disposed behind our back
			m_database.EnsureCheckTransactionIsValid(this);
		}

		public void Dispose()
		{
			// note: we can be called by user code, or by the FdbDatabase when it is terminating with pending transactions
			if (!m_disposed)
			{
				m_disposed = true;

				try
				{
					m_database.UnregisterTransaction(this);
				}
				finally
				{
					m_handle.Dispose();
				}
			}
		}

		#endregion
	}

}
