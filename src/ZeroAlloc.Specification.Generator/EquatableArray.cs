using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ZeroAlloc.Specification.Generator;

/// <summary>
/// An immutable array compared by its elements, so a model holding one stays equal across
/// generator runs and the incremental pipeline can cache it. <see cref="ImmutableArray{T}"/>
/// itself compares by reference.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _items;

    public EquatableArray(ImmutableArray<T> items) => _items = items;

    public int Length => _items.IsDefault ? 0 : _items.Length;

    public bool Equals(EquatableArray<T> other)
    {
        if (Length != other.Length) return false;
        for (var i = 0; i < Length; i++)
        {
            if (!_items[i].Equals(other._items[i])) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < Length; i++) hash = (hash * 397) ^ _items[i].GetHashCode();
            return hash;
        }
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() =>
        (_items.IsDefault ? ImmutableArray<T>.Empty : _items).GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        ((IEnumerable<T>)(_items.IsDefault ? ImmutableArray<T>.Empty : _items)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
}
