using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Jock.Net.TcpJson
{
    class WeakList<T> : IEnumerable<T>
        where T : class
    {
        private readonly object lockObj = new object();
        private List<WeakReference<T>> weaks = new List<WeakReference<T>>();

        public void Add(T item)
        {
            lock(lockObj)
            {
                weaks.Add(new WeakReference<T>(item));
            }
        }

        public bool Remove(T item)
        {
            lock(lockObj)
            {
                foreach (var weak in weaks)
                {
                    if (weak.TryGetTarget(out T target))
                    {
                        if(target == item)
                        {
                            weaks.Remove(weak);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public void Clear()
        {
            lock (lockObj)
            {
                weaks.Clear();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (lockObj)
            {
                foreach (var weak in weaks)
                {
                    if (weak.TryGetTarget(out T target))
                    {
                        yield return target;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
