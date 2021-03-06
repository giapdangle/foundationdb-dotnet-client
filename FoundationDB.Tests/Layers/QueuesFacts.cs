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

namespace FoundationDB.Layers.Collections.Tests
{
	using FoundationDB.Client;
	using FoundationDB.Client.Tests;
	using FoundationDB.Layers.Tuples;
	using NUnit.Framework;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	[TestFixture]
	public class QueuesFacts
	{
		[Test]
		public async Task Test_Queue_Fast()
		{
			// without high contention protecction

			using (var db = await TestHelpers.OpenTestPartitionAsync())
			{
				var location = await TestHelpers.GetCleanDirectory(db, "queue");

				var queue = new FdbQueue<int>(location, highContention: false);

				Console.WriteLine("Clear Queue");
				await queue.ClearAsync(db);

				Console.WriteLine("Empty? " + await queue.EmptyAsync(db));

				Console.WriteLine("Push 10, 8, 6");
				await queue.PushAsync(db, 10);
				await queue.PushAsync(db, 8);
				await queue.PushAsync(db, 6);

#if DEBUG
				await TestHelpers.DumpSubspace(db, location);
#endif

				Console.WriteLine("Empty? " + await queue.EmptyAsync(db));

				Console.WriteLine("Pop item: " + await queue.PopAsync(db));
				Console.WriteLine("Next item: " + await queue.PeekAsync(db));
#if DEBUG
				await TestHelpers.DumpSubspace(db, location);
#endif

				Console.WriteLine("Pop item: " + await queue.PopAsync(db));
#if DEBUG
				await TestHelpers.DumpSubspace(db, location);
#endif

				Console.WriteLine("Pop item: " + await queue.PopAsync(db));
#if DEBUG
				await TestHelpers.DumpSubspace(db, location);
#endif


				Console.WriteLine("Empty? " + await queue.EmptyAsync(db));

				Console.WriteLine("Push 5");
				await queue.PushAsync(db, 5);
#if DEBUG
				await TestHelpers.DumpSubspace(db, location);
#endif

				Console.WriteLine("Clear Queue");
				await queue.ClearAsync(db);
#if DEBUG
				await TestHelpers.DumpSubspace(db, location);
#endif

				Console.WriteLine("Empty? " + await queue.EmptyAsync(db));
			}
		}

		[Test]
		public async Task Test_Single_Client()
		{
			using (var db = await TestHelpers.OpenTestPartitionAsync())
			{
				var location = await TestHelpers.GetCleanDirectory(db, "queue");

				var queue = new FdbQueue<int>(location, highContention: false);

				await queue.ClearAsync(db);

				for (int i = 0; i < 10; i++)
				{
					await queue.PushAsync(db, i);
				}

				for (int i = 0; i < 10; i++)
				{
					var r = await queue.PopAsync(db);
					Assert.That(r, Is.EqualTo(i));
				}

				Assert.That(await queue.EmptyAsync(db), Is.True);
			}

		}

		private static async Task RunMultiClientTest(IFdbDatabase db, FdbSubspace location, bool highContention, string desc, int K, int NUM)
		{
			Console.WriteLine("Starting {0} test with {1} threads and {2} iterations", desc, K, NUM);

			var queue = new FdbQueue<string>(location, highContention);
			await queue.ClearAsync(db);

			var pushLock = new TaskCompletionSource<object>();
			var popLock = new TaskCompletionSource<object>();

			int pushCount = 0;
			int popCount = 0;
			int stalls = 0;

			// use a CTS to ensure that everything will stop in case of problems...
			using (var go = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
			{
				var tok = go.Token;

				var pushTreads = Enumerable.Range(0, K)
					.Select(async id =>
					{
						// wait for the signal
						await pushLock.Task.ConfigureAwait(false);

						var res = new List<string>(NUM);

						for (int i = 0; i < NUM; i++)
						{
							var item = id.ToString() + "." + i.ToString();
							await queue.PushAsync(db, item, tok).ConfigureAwait(false);

							Interlocked.Increment(ref pushCount);
							res.Add(item);
						}

						return res;
					}).ToArray();

				var popThreads = Enumerable.Range(0, K)
					.Select(async id =>
					{
						// make everyone wait a bit, to ensure that they all start roughly at the same time
						await popLock.Task.ConfigureAwait(false);

						var res = new List<string>(NUM);

						int i = 0;
						while (i < NUM)
						{
							var item = await queue.PopAsync(db, tok).ConfigureAwait(false);
							if (item != null)
							{
								Interlocked.Increment(ref popCount);
								res.Add(item);
								++i;
							}
							else
							{
								Interlocked.Increment(ref stalls);
								await Task.Delay(10).ConfigureAwait(false);
							}
						}

						return res;
					}).ToArray();

				var sw = Stopwatch.StartNew();

				ThreadPool.UnsafeQueueUserWorkItem((_) => pushLock.SetResult(null), null);
				await Task.Delay(100);
				ThreadPool.UnsafeQueueUserWorkItem((_) => popLock.SetResult(null), null);

				//using (var timer = new Timer((_) =>
				//{
				//	var __ = TestHelpers.DumpSubspace(db, location);
				//}, null, 1000, 4000))
				{

					await Task.WhenAll(pushTreads);
					await Task.WhenAll(popThreads);
				}

				sw.Stop();
				Console.WriteLine("> Finished {0} test in {1} seconds", desc, sw.Elapsed.TotalSeconds);
				Console.WriteLine("> Pushed {0}, Popped {1} and Stalled {2}", pushCount, popCount, stalls);

				var pushedItems = pushTreads.SelectMany(t => t.Result).ToList();
				var poppedItems = popThreads.SelectMany(t => t.Result).ToList();

				Assert.That(pushCount, Is.EqualTo(K * NUM));
				Assert.That(popCount, Is.EqualTo(K * NUM));

				// all pushed items should have been popped (with no duplicates)
				Assert.That(poppedItems, Is.EquivalentTo(pushedItems));

				// the queue should be empty
				Assert.That(await queue.EmptyAsync(db), Is.True);
			}
		}

		[Test]
		[Ignore("Comment this when running benchmarks")]
		public async Task Test_Multi_Client_Simple()
		{
			int NUM = 100;

			using (var db = await TestHelpers.OpenTestPartitionAsync())
			{
				var location = await TestHelpers.GetCleanDirectory(db, "queue");

				await RunMultiClientTest(db, location, false, "simple queue", 1, NUM);
				await RunMultiClientTest(db, location, false, "simple queue", 2, NUM);
				await RunMultiClientTest(db, location, false, "simple queue", 4, NUM);
				await RunMultiClientTest(db, location, false, "simple queue", 10, NUM);
			}
		}

		[Test]
		[Ignore("Comment this when running benchmarks")]
		public async Task Test_Multi_Client_HighContention()
		{
			int NUM = 100;

			using (var db = await TestHelpers.OpenTestPartitionAsync())
			{
				var location = await TestHelpers.GetCleanDirectory(db, "queue");

				await RunMultiClientTest(db, location, true, "high contention queue", 1, NUM);
				await RunMultiClientTest(db, location, true, "high contention queue", 2, NUM);
				await RunMultiClientTest(db, location, true, "high contention queue", 4, NUM);
				await RunMultiClientTest(db, location, true, "high contention queue", 10, NUM);
			}
		}

	}

}
