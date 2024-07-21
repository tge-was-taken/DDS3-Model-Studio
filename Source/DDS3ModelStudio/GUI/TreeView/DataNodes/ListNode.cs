using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace DDS3ModelStudio.GUI.TreeView.DataNodes
{
    public delegate string GetItemNameDelegate<in T>(int index, T item);

    public class ListNode<T> : DataNode<List<T>>
    {
        private readonly GetItemNameDelegate<T> mGetItemNameDelegate;

        public override DataNodeDisplayHint DisplayHint
            => DataNodeDisplayHint.Branch;

        public override DataNodeAction SupportedActions
            => DataNodeAction.Add | DataNodeAction.Delete | DataNodeAction.Move | DataNodeAction.Rename;

        public ListNode([NotNull] string name, [NotNull] List<T> data, [NotNull] GetItemNameDelegate<T> getItemItemNameDelegate) : base(name, data)
        {
            mGetItemNameDelegate = getItemItemNameDelegate;
        }

        protected override void OnInitialize()
        {
            RegisterSyncHandler(() => Nodes.Select(x => (T)x.Data).ToList());
        }

        protected override void OnInitializeView()
        {
            for (var i = 0; i < Data.Count; i++)
            {
                var item = Data[i];
                AddNode(DataNodeFactory.Create(mGetItemNameDelegate(i, item), item));
            }
        }
    }
}
