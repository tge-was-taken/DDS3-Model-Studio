using DDS3ModelStudio.GUI.TreeView.DataNodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DDS3ModelStudio.GUI.TreeView
{
    public static class DataNodeFactory
    {
        private static readonly Lazy<Dictionary<Type, Type>> sDataNodeMap = new Lazy<Dictionary<Type, Type>>(() =>
        {
            var dataNodeMap = new Dictionary<Type, Type>();

            // Try registering types with at least one generic type on the base type
            foreach (var nodeType in Assembly.GetEntryAssembly().GetTypes()
                                              .Where(x => typeof(DataNode).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract))
            {
                var baseType = nodeType.BaseType;
                if (baseType != null && baseType.IsGenericType)
                {
                    var dataType = baseType.GetGenericArguments()[0];

                    // Try to get the real type in case we're dealing with
                    // nested generic parameters
                    while (dataType.IsGenericParameter)
                        dataType = dataType.BaseType;

                    Debug.WriteLine($"{nameof(DataNodeFactory)}: registering {nodeType} for {dataType}");

                    if (dataNodeMap.ContainsKey(dataType))
                    {
                        Debug.WriteLine($"{nameof(DataNodeFactory)}: Library already contains the data type", nameof(dataType));
                        continue;
                    }

                    dataNodeMap.Add(dataType, nodeType);
                }
            }

            return dataNodeMap;
        });

        public static DataNode Create(string name, object data)
            => Create(name, data, null);


        public static T Create<T, T2>(string name, T2 data) where T : DataNode<T2>
            => (T)Create(name, data, null);

        /// <summary>
        /// Create a generic list node for the given list.
        /// </summary>
        public static ListNode<T> Create<T>(string name, IList<T> data, GetItemNameDelegate<T> getItemNameDelegate)
            => (ListNode<T>)Create(name, data, new object[] { getItemNameDelegate });

        /// <summary>
        /// Create a list node for a given list.
        /// </summary>
        public static DataNode Create<T, T2>(string name, object data, GetItemNameDelegate<T2> getItemNameDelegate) where T : ListNode<T2>
            => (T)Create(name, data, new object[] { getItemNameDelegate });

        public static DataNode Create(string name, object data, object[] args, bool dontIgnoreArgs = false)
        {
            var dataType = data.GetType();
            Type nodeType = null;

            if (dataType.IsConstructedGenericType)
            {
                // This is probably a list
                // Try looking up the unbound generic type definition
                var dataTypeGenericTypeDefinition = dataType.GetGenericTypeDefinition();
                foreach (var kvp in sDataNodeMap.Value)
                {
                    var otherDataTypeGenericTypeDefinition = kvp.Key.GetGenericTypeDefinition();
                    if (otherDataTypeGenericTypeDefinition == dataTypeGenericTypeDefinition)
                    {
                        // Construct new generic type that fits the requirements
                        nodeType = kvp.Value.MakeGenericType(dataType.GenericTypeArguments);
                        break;
                    }
                }
            }

            if (nodeType == null)
            {
                // We didn't find the type earlier... so lets try to find one now
                // Try to find a node type that is applicable for this data type
                // going from most specific to least specific
                var curDataType = dataType;

                do
                {
                    if (sDataNodeMap.Value.TryGetValue(curDataType, out nodeType))
                    {
                        sDataNodeMap.Value[dataType] = nodeType;
                        break;
                    }

                    curDataType = curDataType.BaseType;
                }
                while (curDataType != null);
            }

            if (nodeType == null)
            {
                // We tried so hard
                return null;
            }

            if (nodeType.ContainsGenericParameters)
            {
                // If the node type is generic, we must pass in type of the data object as the parameter
                // to instantiate it
                nodeType = nodeType.MakeGenericType(dataType);
            }

            var ctor = nodeType.GetConstructor(new[] { typeof(string), typeof(object) });
            object[] ctorArgs;

            if (args == null || (ctor != null && !dontIgnoreArgs))
            {
                // Ignore arguments if a suitable constructor exists 
                ctorArgs = new[] { name, data };
            }
            else
            {
                ctorArgs = new object[2 + args.Length];
                ctorArgs[0] = name;
                ctorArgs[1] = data;
                Array.Copy(args, 0, ctorArgs, 2, args.Length);
            }

            return (DataNode)Activator.CreateInstance(nodeType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                                         null, ctorArgs, null);
        }
    }
}
