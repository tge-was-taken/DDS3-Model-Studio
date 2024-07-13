namespace DDS3ModelLibrary.Models
{
    public struct NodeWeight
    {
        public short NodeIndex;
        public float Weight;

        public NodeWeight(short nodeIndex, float weight)
        {
            NodeIndex = nodeIndex;
            Weight = weight;
        }
    }
}