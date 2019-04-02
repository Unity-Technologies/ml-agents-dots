using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ECS_MLAgents_v0.Data
{


    public struct char32 {
        public char char0;
        public char char1;
        public char char2;
        public char char3;
        public char char4;
        public char char5;
        public char char6;
        public char char7;
        public char char8;
        public char char9;
        public char char10;
        public char char11;
        public char char12;
        public char char13;
        public char char14;
        public char char15;
        public char char16;
        public char char17;
        public char char18;
        public char char19;
        public char char20;
        public char char21;
        public char char22;
        public char char23;
        public char char24;
        public char char25;
        public char char26;
        public char char27;
        public char char28;
        public char char29;
        public char char30;
        public char char31;

        public static char32 FromString(string src){
            // TODO : Documentation and Unit Test
            NativeArray<char> array = new NativeArray<char>(32, Allocator.Temp );
            for (int i = 0; i<array.Length; i++){
                if (i < src.Length){
                    array[i]= src[i];
                }
                else{
                    array[i] = ' ';
                }
            }
            var res = new char32();
            unsafe
            {
                UnsafeUtility.CopyPtrToStructure((byte*) (array.GetUnsafePtr()), out res);
            }
            array.Dispose();
            return res;
        }
    }

    public struct char256{
        public char char0;
        public char char1;
        public char char2;
        public char char3;
        public char char4;
        public char char5;
        public char char6;
        public char char7;
        public char char8;
        public char char9;
        public char char10;
        public char char11;
        public char char12;
        public char char13;
        public char char14;
        public char char15;
        public char char16;
        public char char17;
        public char char18;
        public char char19;
        public char char20;
        public char char21;
        public char char22;
        public char char23;
        public char char24;
        public char char25;
        public char char26;
        public char char27;
        public char char28;
        public char char29;
        public char char30;
        public char char31;
        public char char32;
        public char char33;
        public char char34;
        public char char35;
        public char char36;
        public char char37;
        public char char38;
        public char char39;
        public char char40;
        public char char41;
        public char char42;
        public char char43;
        public char char44;
        public char char45;
        public char char46;
        public char char47;
        public char char48;
        public char char49;
        public char char50;
        public char char51;
        public char char52;
        public char char53;
        public char char54;
        public char char55;
        public char char56;
        public char char57;
        public char char58;
        public char char59;
        public char char60;
        public char char61;
        public char char62;
        public char char63;
        public char char64;
        public char char65;
        public char char66;
        public char char67;
        public char char68;
        public char char69;
        public char char70;
        public char char71;
        public char char72;
        public char char73;
        public char char74;
        public char char75;
        public char char76;
        public char char77;
        public char char78;
        public char char79;
        public char char80;
        public char char81;
        public char char82;
        public char char83;
        public char char84;
        public char char85;
        public char char86;
        public char char87;
        public char char88;
        public char char89;
        public char char90;
        public char char91;
        public char char92;
        public char char93;
        public char char94;
        public char char95;
        public char char96;
        public char char97;
        public char char98;
        public char char99;
        public char char100;
        public char char101;
        public char char102;
        public char char103;
        public char char104;
        public char char105;
        public char char106;
        public char char107;
        public char char108;
        public char char109;
        public char char110;
        public char char111;
        public char char112;
        public char char113;
        public char char114;
        public char char115;
        public char char116;
        public char char117;
        public char char118;
        public char char119;
        public char char120;
        public char char121;
        public char char122;
        public char char123;
        public char char124;
        public char char125;
        public char char126;
        public char char127;
        public char char128;
        public char char129;
        public char char130;
        public char char131;
        public char char132;
        public char char133;
        public char char134;
        public char char135;
        public char char136;
        public char char137;
        public char char138;
        public char char139;
        public char char140;
        public char char141;
        public char char142;
        public char char143;
        public char char144;
        public char char145;
        public char char146;
        public char char147;
        public char char148;
        public char char149;
        public char char150;
        public char char151;
        public char char152;
        public char char153;
        public char char154;
        public char char155;
        public char char156;
        public char char157;
        public char char158;
        public char char159;
        public char char160;
        public char char161;
        public char char162;
        public char char163;
        public char char164;
        public char char165;
        public char char166;
        public char char167;
        public char char168;
        public char char169;
        public char char170;
        public char char171;
        public char char172;
        public char char173;
        public char char174;
        public char char175;
        public char char176;
        public char char177;
        public char char178;
        public char char179;
        public char char180;
        public char char181;
        public char char182;
        public char char183;
        public char char184;
        public char char185;
        public char char186;
        public char char187;
        public char char188;
        public char char189;
        public char char190;
        public char char191;
        public char char192;
        public char char193;
        public char char194;
        public char char195;
        public char char196;
        public char char197;
        public char char198;
        public char char199;
        public char char200;
        public char char201;
        public char char202;
        public char char203;
        public char char204;
        public char char205;
        public char char206;
        public char char207;
        public char char208;
        public char char209;
        public char char210;
        public char char211;
        public char char212;
        public char char213;
        public char char214;
        public char char215;
        public char char216;
        public char char217;
        public char char218;
        public char char219;
        public char char220;
        public char char221;
        public char char222;
        public char char223;
        public char char224;
        public char char225;
        public char char226;
        public char char227;
        public char char228;
        public char char229;
        public char char230;
        public char char231;
        public char char232;
        public char char233;
        public char char234;
        public char char235;
        public char char236;
        public char char237;
        public char char238;
        public char char239;
        public char char240;
        public char char241;
        public char char242;
        public char char243;
        public char char244;
        public char char245;
        public char char246;
        public char char247;
        public char char248;
        public char char249;
        public char char250;
        public char char251;
        public char char252;
        public char char253;
        public char char254;
        public char char255;

        public static char256 FromString(string src){
            // TODO : Documentation and Unit Test
            NativeArray<char> array = new NativeArray<char>(256, Allocator.Temp );
            for (int i = 0; i<array.Length; i++){
                if (i < src.Length){
                    array[i]= src[i];
                }
                else{
                    array[i] = ' ';
                }
            }
            var res = new char256();
            unsafe
            {
                UnsafeUtility.CopyPtrToStructure((byte*) (array.GetUnsafePtr()), out res);
            }
            array.Dispose();
            return res;
        }
    }
}
