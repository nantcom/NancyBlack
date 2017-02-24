using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using NantCom.NancyBlack.Modules.DatabaseSystem.Types;

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
        private static Dictionary<Type, dynamic> _buffers = new Dictionary<Type, dynamic>();

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
                _buffers.Add(t, buffer);
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
        private NancyBlackDatabase _db;
        private List<T> _buffer;
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
            _db = db;
        }

        /// <summary>
        /// Adds item into buffer
        /// </summary>
        /// <param name="item">item to be added</param>
        public void Add( T item )
        {
            lock (_locker)
            {
                if (_buffer == null)
                {
                    _buffer = new List<T>();
                    _flushTimer = new Timer(buffer =>
                    {
                        lock (_flushTimer)
                        {
                            // switch the buffer to a new one
                            // and we work with current one to allow
                            // new insertions to buffer while we flushes
                            List<T> copy;
                            lock (_locker)
                            {
                                if (_buffer.Count == 0)
                                {
                                    return;
                                }
                                copy = _buffer;
                                _buffer = new List<T>();
                            }



                            _db.Transaction(() =>
                            {
                                foreach (var element in copy)
                                {
                                    _db.UpsertRecord(element);
                                }
                            });
                        }

                    }, _buffer, this.FlushDelay, this.FlushDelay);
                }

                _buffer.Add(item);
            }
        }

    }
}