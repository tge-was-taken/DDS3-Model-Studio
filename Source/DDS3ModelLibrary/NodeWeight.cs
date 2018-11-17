namespace DDS3ModelLibrary
{
    public struct NodeWeight
    {
        public short NodeIndex;
        public float Weight;

        public NodeWeight( short nodeIndex, float weight )
        {
            NodeIndex = nodeIndex;
            Weight    = weight;
        }
    }
}