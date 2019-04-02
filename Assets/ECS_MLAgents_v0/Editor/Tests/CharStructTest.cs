using NUnit.Framework;
using ECS_MLAgents_v0;

namespace ECS_MLAgents_v0.Editor.Tests{
    public class char32Test{

        [Test]
        public void TestFromString(){
            var tmp = Data.char32.FromString("");
            for (var i=0; i< 32; i++){
                Assert.Equals(tmp.char0, ' ');
                Assert.Equals(tmp.char10, ' ');
                Assert.Equals(tmp.char30, ' ');
            }
            
        }
    }
}