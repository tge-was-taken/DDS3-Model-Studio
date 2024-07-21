namespace DDS3ModelLibrary.Models
{
    public static class MeshFlagsHelper
    {
        public static MeshFlags Update(MeshFlags flag, object conditionObj, MeshFlags conditionFlag)
        {
            return Update(flag, conditionObj != null, conditionFlag);
        }

        public static MeshFlags Update(MeshFlags flag, bool condition, MeshFlags conditionFlag)
        {
            if (condition)
                flag |= conditionFlag;
            else
                flag &= ~conditionFlag;

            return flag;
        }
    }
}