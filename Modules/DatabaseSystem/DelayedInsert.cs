using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;
using System.Collections.Concurrent;

namespace NantCom.NancyBlack.Modules.DatabaseSystem
{
    /// <summary>
    /// Extension to enable delayed insertion of records
    /// </summary>
    public static class DelayedInsertExt
    {
        /// <summary>
        /// Dictionary which keeps the default instance of buffer for any given type
        /// </summary>
        private static ConcurrentDictionary<Type, object> _buffers = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Creates buffer for specified type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static dynamic CreateBuffer(NancyBlackDatabase db, Type t)
        {
            if (typeof(IStaticType).IsAssignableFrom(t) == false)
            {
                throw new InvalidOperationException("Specified Type does not implement IStaticType");
            }

            var typedBuffer = typeof(InsertBuffer<>).MakeGenericType(t);
            return Activator.CreateInstance(typedBuffer, db);
        }

        /// <summary>
        /// Changes the delay time for 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="delay"></param>
        public static void SetFlushDelay(this NancyBlackDatabase db, Type t, TimeSpan delay)
        {
            dynamic buffer;
            if (_buffers.TryGetValue(t, out buffer))
            {
                buffer.FlushDelay = delay;
            }
            else
            {
                buffer = DelayedInsertExt.CreateBuffer(db, t);
            }
        }

        /// <summary>
        /// Delay Insert the specified item
        /// </summary>
        /// <param name="item"></param>
        public static void DelayedInsert(this NancyBlackDatabase db, dynamic item)
        {
            var t = item.GetType();
            dynamic buffer;
            if (_buffers.TryGetValue(t, out buffer) == false)
            {
                buffer = DelayedInsertExt.CreateBuffer(db, t);
                _buffers.AddOrUpdate(t, buffer, new Func<Type, object, object>( (tin, o)=> o ));
            }

            buffer.Add(item);
        }
    }

    /// <summary>
    /// A System to handle objects in memory and flushes them into database
    /// after pre-defined period
    /// </summary>
    public sealed class InsertBuffer<T> where T : IStaticType, new()
    {
        private ConcurrentQueue<T> _buffer = new ConcurrentQueue<T>();
        private Timer _flushTimer;
#if DEBUG
        private TimeSpan _flushDelay = TimeSpan.FromSeconds(30);
#else
        private TimeSpan _flushDelay = TimeSpan.FromMinutes(1);
#endif
        private object _locker = new object();
        
        /// <summary>
        /// Delay before flushing of this buffer, default is 1 minute
        /// </summary>
        public TimeSpan FlushDelay
        {
            get
            {
                return _flushDelay;
            }
            set
            {
                _flushDelay = value;
                if (_flushTimer != null)
                {
                    _flushTimer.Change(_flushDelay, _flushDelay);
                }
            }
        }

        /// <summary>
        /// Initializes new instance of Buffer
        /// </summary>
        /// <param name="flushDelay">Time interval for each flush</param>
        public InsertBuffer( NancyBlackDatabase db )
        {
            _flushTimer = new Timer(buffer =>
            {
                int count = _buffer.Count;
                db.Transaction(() =>
                {
                    // only get items up until current number of items we have
                    T result;
                    while ( count >= 0 && _buffer.TryDequeue(out result))
                    {
                        count--;
                        db.UpsertRecord(result);
                    }
                });

            }, _buffer, this.FlushDelay, this.FlushDelay);
        }

        /// <summary>
        /// Adds item into buffer
        /// </summary>
        /// <param name="item">item to be added</param>
        public void Add( T item )
        {
            _buffer.Enqueue(item);
        }

    }
}