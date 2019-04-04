using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using System;
namespace ECS_MLAgents_v0.Data
{

    /*
    
    [StructLayout(LayoutKind.Sequential)] 
    struct char<N> {
        private int size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = <N>)]
        private byte[] bytes;

        public char<N>(string s){
            var buffer = System.Text.Encoding.ASCII.GetBytes(s);
            this.size = buffer.Length;
            this.bytes=new byte[<N>];
            if (size> <N>){
                size = <N>;
            }
            Array.Copy(buffer, 0, bytes, 0, size);
        }

        public string GetString(){
            return System.Text.Encoding.UTF8.GetString(bytes, 0, size);
        }
    }

     */


    [StructLayout(LayoutKind.Sequential)] 
    public struct char64 {

        private int size; // TODO we could use an end of line stopper rather than keeping track of the size
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        private byte[] bytes;

        public char64(string s){
            var buffer = System.Text.Encoding.ASCII.GetBytes(s);
            this.size = buffer.Length;
            this.bytes=new byte[64];
            if (size> 64){
                size = 64;
            }
            Array.Copy(buffer, 0, bytes, 0, size);
        }

        public string GetString(){
            return System.Text.Encoding.UTF8.GetString(bytes, 0, size);
        }
    }

    [StructLayout(LayoutKind.Sequential)] 
    public struct char256 {

        private int size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        private byte[] bytes;

        public char256(string s){
            var buffer = System.Text.Encoding.ASCII.GetBytes(s);
            this.size = buffer.Length;
            this.bytes=new byte[256];
            if (size> 256){
                size = 256;
            }
            Array.Copy(buffer, 0, bytes, 0, size);
        }

        public string GetString(){
            return System.Text.Encoding.UTF8.GetString(bytes, 0, size);
        }
    }

}
