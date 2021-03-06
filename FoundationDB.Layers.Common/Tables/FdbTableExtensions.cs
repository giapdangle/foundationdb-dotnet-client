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

namespace FoundationDB.Layers.Tables
{
	using FoundationDB.Client;
	using FoundationDB.Layers.Tuples;
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public static class FdbTableTransactionals
	{

		#region FdbTable...

		public static Task<Slice> GetAsync(this FdbTable table, IFdbReadOnlyTransactional dbOrTrans, IFdbTuple id, CancellationToken ct = default(CancellationToken))
		{
			if (table == null) throw new ArgumentNullException("table");
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			if (id == null) throw new ArgumentNullException("id");

			return dbOrTrans.ReadAsync((tr) => table.GetAsync(tr, id), ct);
		}

		public static Task SetAsync(this FdbTable table, IFdbTransactional dbOrTrans, IFdbTuple id, Slice value, CancellationToken ct = default(CancellationToken))
		{
			if (table == null) throw new ArgumentNullException("table");
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			if (id == null) throw new ArgumentNullException("id");

			return dbOrTrans.WriteAsync((tr) => table.Set(tr, id, value), ct);
		}

		public static Task ClearAsync(this FdbTable table, IFdbTransactional dbOrTrans, IFdbTuple id, CancellationToken ct = default(CancellationToken))
		{
			if (table == null) throw new ArgumentNullException("table");
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			if (id == null) throw new ArgumentNullException("id");

			return dbOrTrans.WriteAsync((tr) => table.Clear(tr, id), ct);
		}

		#endregion

		#region FdbTable<K, V>...

		public static Task<TValue> GetAsync<TKey, TValue>(this FdbTable<TKey, TValue> table, IFdbReadOnlyTransactional dbOrTrans, TKey id, CancellationToken ct = default(CancellationToken))
		{
			if (table == null) throw new ArgumentNullException("table");
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");

			return dbOrTrans.ReadAsync((tr) => table.GetAsync(tr, id), ct);
		}

		public static Task SetAsync<TKey, TValue>(this FdbTable<TKey, TValue> table, IFdbTransactional dbOrTrans, TKey id, TValue value, CancellationToken ct = default(CancellationToken))
		{
			if (table == null) throw new ArgumentNullException("table");
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");

			return dbOrTrans.WriteAsync((tr) => table.Set(tr, id, value), ct);
		}

		public static Task ClearAsync<TKey, TValue>(this FdbTable<TKey, TValue> table, IFdbTransactional dbOrTrans, TKey id, CancellationToken ct = default(CancellationToken))
		{
			if (table == null) throw new ArgumentNullException("table");
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");

			return dbOrTrans.WriteAsync((tr) => table.Clear(tr, id), ct);
		}

		#endregion

	}

}
