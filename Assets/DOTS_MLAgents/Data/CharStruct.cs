using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using System;
namespace DOTS_MLAgents.Data
{

    /*
    
    [StructLayout(LayoutKind.Sequential)] 
    struct char<N> {
        private int size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = <N>)]
        private byte[] bytes;

        public char<N>(string s){
            this.bytes=new byte[<N>];
            this.size = s.Length;
            if (this.size > <N>){
                throw new NotSupportedException(
                    "Cannot create a char<N> object with more than <N> characters"
                    );
            }
            System.Text.Encoding.ASCII.GetBytes(s, 0, this.size, this.bytes, 0);
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
            this.bytes=new byte[64];
            this.size = s.Length;
            if (this.size > 64){
                throw new NotSupportedException(
                    "Cannot create a char64 object with more than 64 characters"
                    );
            }
            System.Text.Encoding.ASCII.GetBytes(s, 0, this.size, this.bytes, 0);
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
            this.bytes=new byte[256];
            this.size = s.Length;
            if (this.size > 256){
                throw new NotSupportedException(
                    "Cannot create a char256 object with more than 256 characters"
                    );
            }
            System.Text.Encoding.ASCII.GetBytes(s, 0, this.size, this.bytes, 0);
        }

        public string GetString(){
            return System.Text.Encoding.UTF8.GetString(bytes, 0, size);
        }
    }

}
