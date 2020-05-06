using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal unsafe struct Counter : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        int* m_Counter;
        Allocator m_Alloc;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle       m_Safety;
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel          m_DisposeSentinel;
#endif

        public Counter(Allocator alloc)
        {
            m_Alloc = alloc;
            m_Counter = (int*)UnsafeUtility.Malloc(4, 4, m_Alloc);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, m_Alloc);
#endif
            Count = 0;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            UnsafeUtility.Free(m_Counter, m_Alloc);
            m_Counter = null;
        }

        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return *m_Counter;
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                *m_Counter = value;
            }
        }

        public int Increment()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return Interlocked.Increment(ref *m_Counter);
        }
    }
}
