using System;

namespace DDS3ModelStudio.GUI.TreeView
{
    public class DataNodeEventArgs : EventArgs
    {
        public DataNode Node { get; }

        public DataNodeEventArgs( DataNode node )
        {
            Node = node;
        }
    }

    public class DataNodeEventArgs<T> : DataNodeEventArgs
    {
        public T Value { get; }

        public DataNodeEventArgs( DataNode node, T value ) : base( node )
        {
            Value = value;
        }
    }

    public class DataNodePropertyChangedEventArgs<T> : DataNodeEventArgs
    {
        public T OldValue { get; }

        public T NewValue { get; }

        public DataNodePropertyChangedEventArgs( DataNode node, T oldValue, T newValue ) : base( node )
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class DataNodeMovedEventArgs : DataNodeEventArgs
    {
        public int OldIndex { get; }

        public int NewIndex { get; }

        public DataNode MovedNode { get; }

        public DataNodeMovedEventArgs( DataNode node, int oldIndex, int newIndex, DataNode movedNode ) : base( node )
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            MovedNode = movedNode;
        }
    }
}