using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataStructures {
    public class Node<T>
    {
        public Node<T> next { get; set; }
        public Node<T> prev { get; set; }
        public T value;

        public Node(Node<T> next, Node<T> prev, T value)
        {
            this.next = next;
            this.prev = prev;
            this.value = value;

            if (prev != null)
            {
                prev.next = this;
            }
            if (next != null)
            {
                next.prev = this;
            }
        }
    }

    public class SimpleLinkedList<T>: IEnumerable<T>
    {
        public Node<T> First { get; private set; }
        public Node<T> Last { get; private set; }
        bool disposed;

        public SimpleLinkedList()
        {

        }

        public SimpleLinkedList(IEnumerable<T> items)
        {
            foreach(var item in items)
            {
                AddBack(item);
            }
        }

        public void AddFront(T val)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SimpleLinkedList<T>));
            }
            First = new Node<T>(First, null, val);
        }

        public void AddBack(T val)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SimpleLinkedList<T>));
            }
            Last = new Node<T>(null, Last, val);
        }

        public void AddBack(Node<T> node)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SimpleLinkedList<T>));
            }
            node.prev = Last;
            Last = node;
        }

        public void Remove(Node<T> node)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SimpleLinkedList<T>));
            }
            if (node == First)
            {
                First = node.next;
            } 
            else if(node == Last)
            {
                Last = node.prev;
            }
            else
            {
                var prev = node.prev;
                var next = node.next;

                if(prev != null)
                {
                    prev.next = next;
                }
                if(next != null)
                {
                    next.prev = prev;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SimpleLinkedList<T>));
            }
            var node = First;
            while(node != null)
            {
                yield return node.value;
                node = node.next;
            }
        }

        public void Concat(SimpleLinkedList<T> other)
        {
            AddBack(other.First);
            other.disposed = true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }
}
