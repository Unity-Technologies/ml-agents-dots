using NUnit.Framework;
using ECS_MLAgents_v0;
using ECS_MLAgents_v0.Data;
using System.Runtime.InteropServices;
using UnityEngine;
using System;

namespace ECS_MLAgents_v0.Editor.Tests{
    public class char64Test{

        [Test]
        public void TestFromStringChar64(){
            foreach(string s in new string[]{"", " ", "TestString"}){
                var tmp = new char64(s);
                Assert.AreEqual(s,          tmp.GetString());
                Assert.AreEqual(4 + 64,     Marshal.SizeOf(tmp));
            }
        }

        [Test]
        public void TestLongStringChar64(){
            Assert.That(() =>  new char64("0000000001000000000200000000030000000004000000000500000000060000XXXXX"), 
                Throws.TypeOf<NotSupportedException>());
            }
    }

    public class char256Test{

        [Test]
        public void TestFromStringChar256(){
            foreach(string s in new string[]{"", " ", "TestString"}){
                var tmp = new char256(s);
                Assert.AreEqual(s,          tmp.GetString());
                Assert.AreEqual(4 + 256,    Marshal.SizeOf(tmp));
            }
        }
    }



}