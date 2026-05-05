using System;
using System.Collections.Generic;
using System.Linq;

namespace IndustrialProcessingSystem.Core.Infrastructure
{
    public class ConcurrentPriorityQueue<TPriority, TElement>
        where TPriority : IComparable<TPriority>
    {
        private readonly List<(TPriority Priority, TElement Element)> _elements = new();
        private readonly object _sync = new();

        public void Enqueue(TPriority priority, TElement element)
        {
            lock (_sync)
            {
                _elements.Add((priority, element));
                // Manja vrednost prioriteta ide prva, kako je trazeno u tekstu zadatka.
                _elements.Sort((left, right) => left.Priority.CompareTo(right.Priority));
            }
        }

        public bool TryDequeue(out TElement element)
        {
            lock (_sync)
            {
                if (_elements.Count == 0)
                {
                    element = default!;
                    return false;
                }

                element = _elements[0].Element;
                _elements.RemoveAt(0);
                return true;
            }
        }

        public IEnumerable<(TPriority Priority, TElement Element)> GetItems()
        {
            lock (_sync)
                return _elements.ToList();
        }
    }
}
