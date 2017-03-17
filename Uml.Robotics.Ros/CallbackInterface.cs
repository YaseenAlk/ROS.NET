﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    internal class Callback<T>
        : CallbackInterface where T : RosMessage, new()
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<Callback<T>>();
        private volatile bool callback_state;

        public readonly bool allow_concurrent_callbacks;
        public volatile Queue<Item> queue = new Queue<Item>();
        public uint size;

        public Callback(CallbackDelegate<T> f, string topic, uint queue_size, bool allow_concurrent_callbacks)
            : this(f)
        {
            this.allow_concurrent_callbacks = allow_concurrent_callbacks;
            size = queue_size;
        }

        public Callback(CallbackDelegate<T> f)
        {
            base.Event += b =>
                {
                    T t = b as T;
                    b = null;
                    f.DynamicInvoke(t);
                };
        }

        public override void pushitgood(ISubscriptionCallbackHelper helper, RosMessage message, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time)
        {
            if (was_full)
                was_full = false;
            Item i = new Item
            {
                helper = helper,
                message = message,
                nonconst_need_copy = nonconst_need_copy,
                receipt_time = receipt_time
            };
            lock (queue)
            {
                if (fullNoLock())
                {
                    queue.Dequeue();
                    was_full = true;
                }
                queue.Enqueue(i);
            }
        }

        public override void clear()
        {
            queue.Clear();
        }

        public new virtual bool ready()
        {
            return true;
        }

        private bool fullNoLock()
        {
            return size > 0 && queue.Count >= size;
        }

        public bool full()
        {
            lock (queue)
                return fullNoLock();
        }

        public class Item
        {
            public ISubscriptionCallbackHelper helper;
            public RosMessage message;
            public bool nonconst_need_copy;
            public TimeData receipt_time;
        }

        internal override CallResult Call()
        {
            if (!allow_concurrent_callbacks)
            {
                if (callback_state)
                    return CallResult.TryAgain;
                callback_state = true;
            }
            Item i = null;
            lock (queue)
            {
                if (queue.Count == 0)
                    return CallResult.Invalid;
                i = queue.Dequeue();
            }
            i.helper.call(i.message);
            callback_state = false;
            return CallResult.Success;
        }
    }

    public class CallbackInterface
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<CallbackInterface>();
        private static object uidlock = new object();
        private static UInt64 nextuid;
        private UInt64 uid;

        public CallbackInterface()
        {
            lock (uidlock)
            {
                uid = nextuid;
                nextuid++;
            }
        }

        public UInt64 Get()
        {
            return uid;
        }

        #region Delegates

        public delegate void ICallbackDelegate(RosMessage msg);

        #endregion

        #region CallResult enum

        public enum CallResult
        {
            Success,
            TryAgain,
            Invalid
        }

        #endregion

        public CallbackInterface(ICallbackDelegate f) : this()
        {
            Event += f;
        }

        public void func<T>(T msg) where T : RosMessage, new()
        {
            if (Event != null)
            {
                Event(msg);
            }
            else
            {
                Logger.LogError($"{nameof(Event)} is null");
            }
        }

        public virtual void pushitgood(ISubscriptionCallbackHelper helper, RosMessage msg, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time)
        {
            throw new NotImplementedException();
        }

        public virtual void clear()
        {
            throw new NotImplementedException();
        }

        public event ICallbackDelegate Event;

        internal virtual CallResult Call()
        {
            return CallResult.Invalid;
        }

        internal bool ready()
        {
            return true;
        }
    }
}