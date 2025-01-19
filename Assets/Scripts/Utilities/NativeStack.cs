using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///////////////////////////////////////////////////////////////////////////////////////
// Native queue collection with fixed length. FILO.
// NativeStack<T> supports writing in IJobParallelFor, just use ParallelWriter and AsParallelWriter().
// Not tested to much, works for me with integers and unity 2019.3
//
// Assembled with help of:
// - NativeCounter example (https://docs.unity3d.com/Packages/com.unity.jobs@0.1/manual/custom_job_types.html)
// - How to Make Custom Native Collections (https://jacksondunstan.com/articles/4734)
// - source code of NativeQueue and NativeList

using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Collections
{
    public unsafe struct NativeStack<T> : IDisposable, IEnumerable<T> where T : unmanaged
    {
        private NativeList<T> m_list;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

        // The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
        // The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
        // This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
        [NativeSetClassTypeToNullOnSchedule] DisposeSentinel m_DisposeSentinel;
#endif

        public NativeStack(int length, Allocator label)
        {
            // This check is redundant since we always use an int that is blittable.
            // It is here as an example of how to check for type correctness for generic types.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<T>())
                throw new ArgumentException(
                    string.Format("{0} used in NativeStack<{0}> must be blittable", typeof(T)));
#endif
            // Allocate native memory for a single integer
            m_list = new NativeList<T>(length, label);

            // Create a dispose sentinel to track memory leaks. This also creates the AtomicSafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, label);
#endif
        }

        public bool IsCreated
        {
            get => m_list.IsCreated;
        }

        public int Count
        {
            get
            {
                // Verify that the caller has read permission on this data. 
                // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_list.Length;
            }
        }

        void Deallocate()
        {
            m_list.Dispose();
        }

        public void Dispose()
        {
            // Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            Deallocate();
        }

        public void Push(T item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_list.Add(item);
        }

        public T Pop()
        {
            T item;

            if (!TryPop(out item))
                throw new InvalidOperationException("Trying to pop from an empty stack.");

            return item;
        }

        public T Peek()
        {
            T item;

            if (m_list.Length <= 0)
                throw new InvalidOperationException("Trying to peek at an empty stack.");
            item = m_list[m_list.Length - 1];
            return item;
        }

        public bool TryPop(out T item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            if (m_list.Length <= 0)
            {
                item = default;
                return false;
            }

            item = m_list[m_list.Length - 1];
            m_list.RemoveAtSwapBack(m_list.Length - 1);
            return true;
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_list.Clear();
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="jobHandle">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif
            var jobHandle = new DisposeJob { Container = this }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif

            return jobHandle;
        }

        public IEnumerator<T> GetEnumerator() => m_list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_list.GetEnumerator();

        [BurstCompile]
        struct DisposeJob : IJob
        {
            public NativeStack<T> Container;

            public void Execute()
            {
                Container.Deallocate();
            }
        }
    }
}
