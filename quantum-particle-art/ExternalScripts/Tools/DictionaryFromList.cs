using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class DictionaryFromList<K, V>
{
    [Serializable]
    private class KV
    {
        public K Key;
        public V Value;
    }

    [SerializeField] private KV[] values;
    private Dictionary<K, V> _dictionary;

    public DictionaryFromList(Dictionary<K, V> dictionary)
    {
        _dictionary = dictionary;
    }

    public V this[K key]
    {
        get
        {
            BuildDictionnary();
            Assert.IsTrue(_dictionary != null && _dictionary.ContainsKey(key),
                $"Key {key} does not exist in the dictionary.");
            return _dictionary[key];
        }
        set
        {
            BuildDictionnary();
            _dictionary[key] = value;
        }
    }

    public Dictionary<K, V> Dictionary
    {
        get
        {
            BuildDictionnary();
            return _dictionary;
        }
    }

    private void BuildDictionnary()
    {
        if (_dictionary != null)
            return;
        _dictionary = new Dictionary<K, V>();
        foreach (var kv in values)
        {
            Assert.IsFalse(_dictionary.ContainsKey(kv.Key),
                $"Key {kv.Key} already exists in the dictionary.");
            _dictionary.Add(kv.Key, kv.Value);
        }

        values = null; // Editor usage only, we don't need to keep the serialized values after building the dictionary.
    }
}