using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Motions;
using DDS3ModelLibrary.Textures;
using System.Diagnostics;
using System.Text;

namespace DDS3ModelLibrary.Tests
{
    [TestClass]
    public class MatchingTests
    {
        private static double CalculateEqualityPercentage(byte[] array1, byte[] array2)
        {
            var equalCount = 0;
            for (var i = 0; i < array1.Length; i++)
            {
                if (array1[i] == array2[i])
                    equalCount++;
            }

            return (double)equalCount / array1.Length;
        }

        private void DoTest<T>(string path, Func<MemoryStream, AbstractResource<T>> factory) 
            where T : class, new()
        {
            var inData = File.ReadAllBytes(path);
            var resource = factory(new MemoryStream(inData));
            var outData = resource.Save().ToArray();
            var pctEqual = CalculateEqualityPercentage(inData, outData);
            if (Debugger.IsAttached)
                File.WriteAllBytes("dump.bin", outData);
            Assert.IsTrue(pctEqual > 0.99);
        }

        [TestMethod]
        public void player_a_mb()
        {
            DoTest(@"TestData/player_a.MB", stream => new Model(stream));
        }

        [TestMethod]
        public void player_a_tb()
        {
            DoTest(@"TestData/player_a.TB", stream => new TexturePack(stream));
        }

        [TestMethod]
        public void player_a_0_AB()
        {
            DoTest(@"TestData/player_a_0.AB", stream => new MotionPack(stream));
        }

        [TestMethod]
        public void player_a_1_AB()
        {
            DoTest(@"TestData/player_a_1.AB", stream => new MotionPack(stream));
        }

        [TestMethod]
        public void player_a_2_AB()
        {
            DoTest(@"TestData/player_a_2.AB", stream => new MotionPack(stream));
        }

        [TestMethod]
        public void player_a_PB()
        {
            DoTest(@"TestData/player_a.PB", stream => new ModelPack(stream));
        }
    }
}