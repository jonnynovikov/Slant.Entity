using System;
using System.Collections.Generic;

namespace Slant.Entity.Tests.Helpers;

/// <summary>
/// A class that acts like a dictionary with weakref'ed keys, meaning,
/// the entry will be removed once its key is garbage-collected.
/// required by ObjectIDGenerator
/// </summary>
internal class WeakKeyDict<TKey, TValue>
{
    private struct Pair
    {
        private readonly WeakReference _wkey;
        public TValue val;

        public Pair(TKey k, TValue v)
        {
            _wkey = new WeakReference(k);
            val = v;
        }

        public bool IsAlive()
        {
            return _wkey.IsAlive;
        }
        public bool IsEqual(TKey key)
        {
            return _wkey.Target != null && _wkey.Target.Equals(key);
        }
    }

    private Dictionary<int, List<Pair>> dict;

    public WeakKeyDict() : this(8000)
    {
    }
    public WeakKeyDict(int capacity)
    {
        dict = new Dictionary<int, List<Pair>>(capacity);
    }

    public void Add(TKey key, TValue val)
    {
        int hash = key!.GetHashCode();
        if (!dict.ContainsKey(hash))
        {
            var buckets = new List<Pair>();
            buckets.Add(new Pair(key, val));
            dict.Add(hash, buckets);
        }
        else
        {
            var buckets = dict[hash];
            bool found = false;
            for (int i = buckets.Count - 1; i >= 0; i--)
            {
                Pair p = buckets[i];
                if (!p.IsAlive())
                {
                    buckets.RemoveAt(i);
                }
                else if (p.IsEqual(key))
                {
                    found = true;
                    p.val = val;
                }
            }
            if (!found)
            {
                buckets.Add(new Pair(key, val));
            }
        }
    }

    public bool TryGetValue(TKey key, out TValue val)
    {
        int hash = key.GetHashCode();
        List<Pair> buckets;
        if (dict.TryGetValue(hash, out buckets))
        {
            for (int i = buckets.Count - 1; i >= 0; i--)
            {
                Pair p = buckets[i];
                if (!p.IsAlive())
                {
                    buckets.RemoveAt(i);
                }
                else if (p.IsEqual(key))
                {
                    val = p.val;
                    return true;
                }
            }
        }
        val = default(TValue);
        return false;
    }

    public void Compact()
    {
        var deadKeys = new List<int>();
        foreach (KeyValuePair<int, List<Pair>> item in dict)
        {
            for (int i = item.Value.Count - 1; i >= 0; i--)
            {
                Pair p = item.Value[i];
                if (!p.IsAlive())
                {
                    item.Value.RemoveAt(i);
                }
            }
            if (item.Value.Count < 1)
            {
                deadKeys.Add(item.Key);
            }
        }
        foreach (int k in deadKeys)
        {
            dict.Remove(k);
        }
    }
}

/// <summary>
/// a class that generates a unique identifier for every object
/// </summary>
internal sealed class ObjectIDGenerator
{
    private readonly WeakKeyDict<object, long> _dict;
    private long _counter;

    public ObjectIDGenerator()
    {
        _dict = new WeakKeyDict<object, long>();
        _counter = 0;
    }

    public long GetId(object obj)
    {
        long id;
        lock (this)
        {
            if (_dict.TryGetValue(obj, out id))
            {
                return id;
            }
            else
            {
                _counter += 1;
                _dict.Add(obj, _counter);
                return _counter;
            }
        }
    }

    public void Compact()
    {
        _dict.Compact();
    }
}