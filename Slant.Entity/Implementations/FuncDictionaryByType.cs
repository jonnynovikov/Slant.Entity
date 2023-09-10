using System;
using System.Collections.Generic;

namespace Slant.Entity;

/// <summary>
/// Dictionary to map a type to a function returning that type.
/// Based on: https://codeblog.jonskeet.uk/2008/10/08/mapping-from-a-type-to-an-instance-of-that-type/
/// </summary>
internal class FuncDictionaryByType
{
    private readonly IDictionary<Type, object> _dictionary = new Dictionary<Type, object>();

    /// <summary>
    /// Maps the specified type argument to the given function. If
    /// the type argument already has a value within the dictionary,
    /// ArgumentException is thrown.
    /// </summary>
    public void Add<T>(Func<T> func)
    {
        _dictionary.Add(typeof(T), func);
    }

    /// <summary>
    /// Attempts to fetch a function from the dictionary, returning false and
    /// setting the output parameter to the default value for Func&lt;T&gt; if it
    /// fails, or returning true and setting the output parameter to the
    /// fetched function if it succeeds.
    /// </summary>
    public bool TryGet<T>(out Func<T>? value)
    {
        if (_dictionary.TryGetValue(typeof(T), out object? tmp))
        {
            value = (Func<T>)tmp;
            return true;
        }
        value = default;
        return false;
    }
}