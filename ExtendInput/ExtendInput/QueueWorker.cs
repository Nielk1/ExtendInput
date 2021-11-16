using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput
{
    public class QueueWorker<T> : IDisposable where T : class
    {
        readonly object _locker = new object();
        readonly List<Thread> _workers;
        readonly Queue<T> _taskQueue = new Queue<T>();
        readonly Action<T> _dequeueAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueWorker{T}"/> class.
        /// </summary>
        /// <param name="workerCount">The worker count.</param>
        /// <param name="dequeueAction">The dequeue action.</param>
        /// <param name="isBackground">Are the threads background threads.</param>
        /// <param name="workerName">Thread names, "QueueWorker" is used if not set.</param>
        public QueueWorker(int workerCount, Action<T> dequeueAction, bool isBackground = true, string workerName = null)
        {
            _dequeueAction = dequeueAction;
            _workers = new List<Thread>(workerCount);

            if (string.IsNullOrWhiteSpace(workerName))
                workerName = "QueueWorker";

            // Create and start a separate thread for each worker
            for (int i = 0; i < workerCount; i++)
            {
                Thread t = new Thread(Consume) { IsBackground = isBackground, Name = $"{workerName} worker {i}" };
                _workers.Add(t);
                t.Start();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueWorker{T}"/> class.
        /// </summary>
        /// <param name="workerCount">The worker count.</param>
        /// <param name="dequeueAction">The dequeue action.</param>
        /// <param name="isBackground">Are the threads background threads.</param>
        public QueueWorker(int workerCount, Action<T> dequeueAction, bool isBackground) : this(workerCount, dequeueAction, isBackground, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueWorker{T}"/> class.
        /// </summary>
        /// <param name="workerCount">The worker count.</param>
        /// <param name="dequeueAction">The dequeue action.</param>
        /// <param name="workerName">Thread names, "QueueWorker" is used if not set.</param>
        public QueueWorker(int workerCount, Action<T> dequeueAction, string workerName) : this(workerCount, dequeueAction, true, workerName) { }

        /// <summary>
        /// Enqueues the task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void EnqueueTask(T task)
        {
            lock (_locker)
            {
                _taskQueue.Enqueue(task);
                Monitor.PulseAll(_locker);
            }
        }

        /// <summary>
        /// Consumes this instance.
        /// </summary>
        void Consume()
        {
            while (true)
            {
                T item;
                lock (_locker)
                {
                    while (_taskQueue.Count == 0) Monitor.Wait(_locker);
                    item = _taskQueue.Dequeue();
                }
                if (item == null) return; // poison to quit

                // run actual method
                _dequeueAction(item);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Enqueue one null task per worker to make each exit.
            _workers.ForEach(thread => EnqueueTask(null)); // inject poison

            _workers.ForEach(thread => thread.Join());

        }
    }
}
