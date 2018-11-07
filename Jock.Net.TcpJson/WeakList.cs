using System;
using System.Collections.Generic;

namespace Jock.Net.TcpJson
{
    class WeakList<T> : IEnumerable<T>
        where T : class
    {
        private readonly object lockObj = new object();
#if NET35 || NET40
        private List<WeakReference> weaks = new List<WeakReference>();
#else
        private List<WeakReference<T>> weaks = new List<WeakReference<T>>();
#endif

        public void Add(T item)
        {
            lock(lockObj)
            {
#if NET35 || NET40
                weaks.Add(new WeakReference(item));
#else
                weaks.Add(new WeakReference<T>(item));
#endif
            }
        }

        public bool Remove(T item)
        {
            lock(lockObj)
            {
                foreach (var weak in weaks)
                {
#if NET35 || NET40
                    if (weak.IsAlive)
                    {
                        T target = (T)weak.Target;
#else
                    if (weak.TryGetTarget(out T target))
                        {
#endif
                            if (target == item)
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
#if NET35 || NET40
                    if (weak.IsAlive)
                    {
                        T target = (T)weak.Target;
#else
                    if (weak.TryGetTarget(out T target))
                    {
#endif
                        yield return target;
                    }
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
