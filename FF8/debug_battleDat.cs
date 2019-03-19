using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FF8
{
    public class Debug_battleDat
    {
        int id;
        byte[] buffer;
        int debug = 0;

        public struct DatFile
        {
            public uint cSections;
            public uint[] pSections;
            public uint eof;
        }

        public DatFile datFile;

        #region section 1
        [StructLayout(LayoutKind.Sequential,Pack = 1,Size =16)]
        public struct Skeleton
        {
            public ushort cBones;
            public ushort unk;
            public ushort unk2;
            public ushort unk3;
            private short scaleX;
            private short scaleY;
            private short scaleZ;
            public ushort unk4;
            public Bone[] bones;

            public float ScaleX { get => scaleX / 4096.0f; set => scaleX = (short)value; }
            public float ScaleY { get => scaleY / 4096.0f; set => scaleY = (short)value; }
            public float ScaleZ { get => scaleZ / 4096.0f; set => scaleZ = (short)value; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 48)]
        public struct Bone
        {
            public ushort parentId;
            public short boneSize;
            private short unk1; //360
            private short unk2; //360
            private short unk3; //360
            private short unk4;
            private short unk5;
            private short unk6;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 28 )]
            public byte[] Unk;

            public float Size { get => boneSize / 2048.0f; }
            public float Unk1 { get => unk1 / 4096.0f * 360.0f; }
            public float Unk2 { get => unk2 / 4096.0f * 360.0f; }
            public float Unk3 { get => unk3 / 4096.0f * 360.0f; }
            public float Unk4 { get => unk4 / 4096.0f; }
            public float Unk5 { get => unk5 / 4096.0f; }
            public float Unk6 { get => unk6 / 4096.0f; }
        }

        private void ReadSection1(uint v, MemoryStream ms, BinaryReader br)
        {
            ms.Seek(v, SeekOrigin.Begin);
#if _WINDOWS //looks like Linux Mono doesn't like marshalling structure with LPArray to Bone[]
            skeleton = MakiExtended.ByteArrayToStructure<Skeleton>(br.ReadBytes(16));
#else
            skeleton = new Skeleton()
            {
                cBones = br.ReadUInt16(),
                unk = br.ReadUInt16(),
                unk2 = br.ReadUInt16(),
                unk3 = br.ReadUInt16(),
                ScaleX = br.ReadUInt16(),
                ScaleY = br.ReadUInt16(),
                ScaleZ = br.ReadUInt16(),
                unk4 = br.ReadUInt16()
            };
#endif
            skeleton.bones = new Bone[skeleton.cBones];
            for (int i = 0; i < skeleton.cBones; i++)
                skeleton.bones[i] = MakiExtended.ByteArrayToStructure<Bone>(br.ReadBytes(48));
            return;
        }

        public Skeleton skeleton;

#endregion

#region section 2
        public struct Geometry
        {
            public uint cObjects;
            public uint[] pObjects;
            public Object[] objects;
            public uint cTotalVert;
        }

        public struct Object
        {
            public ushort cVertices;
            public VerticeData[] verticeData;
            public ushort cTriangles;
            public ushort cQuads;
            public ulong padding;
            public Triangle[] triangles;
            public Quad[] quads;
        }

        public struct VerticeData
        {
            public ushort boneId;
            public ushort cVertices;
            public Vertex[] vertices;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
        public struct Vertex
        {
            public short x;
            public short y;
            public short z;

            public Vector3 GetVector => new Vector3(x/512.0f, -z/512.0f, y/512.0f);
        }

        [StructLayout(LayoutKind.Sequential, Pack =1, Size =16)]
        public struct Triangle
        {
            private ushort A;
            private ushort B;
            private ushort C;
            public UV vta;
            public UV vtb;
            public ushort texUnk;
            public UV vtc;
            public ushort u;

            public ushort A1 { get => (ushort)(A & 0xFFF); set => A = value; }
            public ushort B1 { get => (ushort)(B & 0xFFF); set => B = value; }
            public ushort C1 { get => (ushort)(C & 0xFFF); set => C = value; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 20)]
        public struct Quad
        {
            private ushort A;
            private ushort B;
            private ushort C;
            private ushort D;
            public UV vta;
            public ushort texUnk;
            public UV vtb;
            public ushort u;
            public UV vtc;
            public UV vtd;

            public ushort A1 { get => (ushort)(A & 0xFFF); set => A = value; }
            public ushort B1 { get => (ushort)(B & 0xFFF); set => B = value; }
            public ushort C1 { get => (ushort)(C & 0xFFF); set => C = value; }
            public ushort D1 { get => (ushort)(D & 0xFFF); set => D = value; }
        }

        [StructLayout(LayoutKind.Sequential, Pack =1,Size =2)]
        public struct UV
        {
            public byte U;
            public byte V;

            public float U1 { get => U/128.0f; set => U = (byte)value; }
            public float V1 { get => V/128.0f; set => V = (byte)value; }
        }

        public Geometry geometry;

        private void ReadSection2(uint v, MemoryStream ms, BinaryReader br)
        {
            ms.Seek(v, SeekOrigin.Begin);
            geometry = new Geometry { cObjects = br.ReadUInt32() };
            geometry.pObjects = new uint[geometry.cObjects];
            for (int i = 0; i < geometry.cObjects; i++)
                geometry.pObjects[i] = br.ReadUInt32();
            geometry.objects = new Object[geometry.cObjects];
            for (int i = 0; i < geometry.cObjects; i++)
                geometry.objects[i] = ReadGeometryObject(ms, br);
            geometry.cTotalVert = br.ReadUInt32();
        }

        private Object ReadGeometryObject(MemoryStream ms, BinaryReader br)
        {
            Object @object = new Object { cVertices = br.ReadUInt16() };
            @object.verticeData = new VerticeData[@object.cVertices];
            for (int n = 0; n < @object.cVertices; n++){
                @object.verticeData[n].boneId = br.ReadUInt16();
                @object.verticeData[n].cVertices = br.ReadUInt16();
                @object.verticeData[n].vertices = new Vertex[@object.verticeData[n].cVertices];
                for (int i = 0; i < @object.verticeData[n].cVertices; i++)
                    @object.verticeData[n].vertices[i] = MakiExtended.ByteArrayToStructure<Vertex>(br.ReadBytes(6));}
            ms.Seek(4-(ms.Position%4), SeekOrigin.Current);
            @object.cTriangles = br.ReadUInt16();
            @object.cQuads = br.ReadUInt16();
            @object.padding = br.ReadUInt64();
            @object.triangles = new Triangle[@object.cTriangles];
            @object.quads = new Quad[@object.cQuads];
            for (int i = 0; i < @object.cTriangles; i++)
                @object.triangles[i] = MakiExtended.ByteArrayToStructure<Triangle>(br.ReadBytes(16));
            for (int i = 0; i < @object.cQuads; i++)
                @object.quads[i] = MakiExtended.ByteArrayToStructure<Quad>(br.ReadBytes(20));
            return @object;
        }

        public VertexPositionTexture[] GetVertexPositions(int objectId, Vector3 position, int animationId, int animationFrame)
        {
            Object obj = geometry.objects[objectId];
            AnimationFrame frame = animHeader.animations[animationId].animationFrames[animationFrame]; //we got the frame
            //let's create a vertices buffer now. We will handle it via triangle basis but looking at bones
            List<VertexPositionTexture> vpt = new List<VertexPositionTexture>();
            List<Tuple<Vector3, int>> verts = new List<Tuple<Vector3, int>>();  

            int i = 0;
            foreach (var a in obj.verticeData)
                foreach (var b in a.vertices)
                    verts.Add(new Tuple<Vector3, int>(b.GetVector, a.boneId-1));

            //we now have a collection of basic Vec3D and a variable indicating a boneId.

            //debug
            
            //debug

            for (;i<obj.cTriangles; i++ ) //let's loop through all triangles
            {
                //we now know the triangle, let's see which vertices it use
                Triangle tr = obj.triangles[i];
                var a = verts[tr.A1].Item1;
                var b = verts[tr.B1].Item1;
                var c = verts[tr.C1].Item1;
                var d = verts[tr.A1].Item2;
                //we grabbed the vertices, now let's parse the distance:
                var e = skeleton.bones[d].Size; //it's the size from origin
                var f = frame.boneRot.Item1[d]; //it's the animation translation for this vertices group (d)
                //so now we have to rotate and translate the vertices by e and f
                //a = Vector3.Transform(a, Matrix.CreateRotationX(MathHelper.ToRadians(f.X)));
                //a = Vector3.Transform(a, Matrix.CreateRotationY(MathHelper.ToRadians(f.Y)));
                //a = Vector3.Transform(a, Matrix.CreateRotationZ(MathHelper.ToRadians(f.Z)));

                //test - are relative bones to parents?
                int parentTester = skeleton.bones[d].parentId;
                    while (parentTester != 0xFFFF)
                    {
                        e += skeleton.bones[skeleton.bones[d].parentId].Size;
                    parentTester = skeleton.bones[parentTester].parentId;
                    }

                a = Vector3.Transform(a, Matrix.CreateTranslation(0, -e, 0));
                a = Vector3.Transform(a, Matrix.CreateTranslation(position));

                d = verts[tr.B1].Item2;
                e = skeleton.bones[d].Size; //it's the size from origin
                f = frame.boneRot.Item1[d]; //it's the animation translation for this vertices group (d)

                //b = Vector3.Transform(b, Matrix.CreateRotationX(MathHelper.ToRadians(f.X)));
                //b = Vector3.Transform(b, Matrix.CreateRotationY(MathHelper.ToRadians(f.Y)));
                //b = Vector3.Transform(b, Matrix.CreateRotationZ(MathHelper.ToRadians(f.Z)));

                parentTester = skeleton.bones[d].parentId;
                while (parentTester != 0xFFFF)
                {
                    e += skeleton.bones[skeleton.bones[d].parentId].Size;
                    parentTester = skeleton.bones[parentTester].parentId;
                }

                b = Vector3.Transform(b, Matrix.CreateTranslation(0, -e, 0));
                b = Vector3.Transform(b, Matrix.CreateTranslation(position));

                d = verts[tr.C1].Item2;
                e = skeleton.bones[d].Size; //it's the size from origin
                f = frame.boneRot.Item1[d]; //it's the animation translation for this vertices group (d)
                //c = Vector3.Transform(c, Matrix.CreateRotationX(MathHelper.ToRadians(f.X)));
                //c = Vector3.Transform(c, Matrix.CreateRotationY(MathHelper.ToRadians(f.Y)));
                //c = Vector3.Transform(c, Matrix.CreateRotationZ(MathHelper.ToRadians(f.Z)));
                parentTester = skeleton.bones[d].parentId;
                while (parentTester != 0xFFFF)
                {
                    e += skeleton.bones[skeleton.bones[d].parentId].Size;
                    parentTester = skeleton.bones[parentTester].parentId;
                }
                c = Vector3.Transform(c, Matrix.CreateTranslation(0, -e, 0));
                c = Vector3.Transform(c, Matrix.CreateTranslation(position));
                vpt.Add(new VertexPositionTexture(c, new Vector2(tr.vta.U1, tr.vta.V1)));
                vpt.Add(new VertexPositionTexture(a, new Vector2(tr.vtb.U1, tr.vtb.V1)));
                vpt.Add(new VertexPositionTexture(b, new Vector2(tr.vtc.U1, tr.vtc.V1)));
                //TODO WIP
            }

            return vpt.ToArray();
        }

        public VertexPositionTexture[] GetVertexPositions(int objectId, Vector3 position)
        {
            //THIS IS DEBUG IN-DEV CLASS
            Object obj = geometry.objects[objectId];
            float fpsVariable = 1000.0f / 15f;
            frameperFPS += Memory.gameTime.ElapsedGameTime.Milliseconds;
            if(frameperFPS > fpsVariable)
            {
                frameperFPS = 0.0f;
                frame++;
            }
            frame = frame == 14 ? 0 : frame;
            return GetVertexPositions(objectId, position, 0, frame); //debug;

            //basically this works like this:
            //every verticeData has a bone attached to it. Bone size declares the 'distance' between bone and rot (bone 0xFFFF)
            //every frame actually rotates the bones (and therefore rotates the whole vertice data)
            //so Maki, you should think of a better algorithm to play animations than just forcing to render everything
            //triangle by triangle (actually I have to, but it's important to store vertices data along bones to another 
            //spectrum, so I'll be able to pre-render it. 

            //so-> a collection of vertex data as-is grabbed by triangle grouped by bone. Then translate by animation rotation
            //and size- voila, that's all

            //MOD: remember to put space for future animation lerp for unlimited FPS smooth motion -> deltaTime calculation?


            //Object obj = geometry.objects[objectId];
            //uint totalVerts = (uint)obj.verticeData.Sum(x => x.cVertices);
            //Vector3[] vecTable = new Vector3[totalVerts]; //DEBUG
            //int totalindex = 0;
            //int i;
            //for (i = 0; i < obj.cVertices; i++)
            //    for (int n = 0; n < obj.verticeData[i].cVertices; n++)
            //        vecTable[totalindex++] = obj.verticeData[i].vertices[n].GetVector;
            //VertexPositionTexture[] vpt = new VertexPositionTexture[obj.cTriangles*3+obj.cQuads*6]; //6 because retriangulation is needed;
            //i = 0;
            //for (; i < obj.cTriangles; i++) {
            //    vpt[i * 3 + 0] = new VertexPositionTexture(position + vecTable[obj.triangles[i].A1], new Vector2(obj.triangles[i].vta.U1, obj.triangles[i].vta.V1));
            //    vpt[i * 3 + 1] = new VertexPositionTexture(position + vecTable[obj.triangles[i].B1], new Vector2(obj.triangles[i].vtb.U1, obj.triangles[i].vtb.V1));
            //    vpt[i * 3 + 2] = new VertexPositionTexture(position + vecTable[obj.triangles[i].C1], new Vector2(obj.triangles[i].vtc.U1, obj.triangles[i].vtc.V1)); }
            //for (int n = 0; n < obj.cQuads; n++) /*quad retriangulation*/
            //{
            //    vpt[i + n * 6 + 0] = new VertexPositionTexture(position + vecTable[obj.quads[n].A1], new Vector2(obj.quads[n].vta.U1, obj.quads[n].vta.V1));
            //    vpt[i + n * 6 + 1] = new VertexPositionTexture(position + vecTable[obj.quads[n].B1], new Vector2(obj.quads[n].vtb.U1, obj.quads[n].vtb.V1));
            //    vpt[i + n * 6 + 2] = new VertexPositionTexture(position + vecTable[obj.quads[n].D1], new Vector2(obj.quads[n].vtd.U1, obj.quads[n].vtd.V1));
            //    vpt[i + n * 6 + 3] = new VertexPositionTexture(position + vecTable[obj.quads[n].A1], new Vector2(obj.quads[n].vta.U1, obj.quads[n].vta.V1));
            //    vpt[i + n * 6 + 4] = new VertexPositionTexture(position + vecTable[obj.quads[n].C1], new Vector2(obj.quads[n].vtc.U1, obj.quads[n].vtc.V1));
            //    vpt[i + n * 6 + 5] = new VertexPositionTexture(position + vecTable[obj.quads[n].D1], new Vector2(obj.quads[n].vtd.U1, obj.quads[n].vtd.V1));
            //    ++i;
            //}

            //return vpt;
        }
#endregion

#region section 3
        public struct AnimationData
        {
            public uint cAnimations;
            public uint[] pAnimations;
            public Animation[] animations;
        }

        public struct Animation
        {
            public byte cFrames;
            public AnimationFrame[] animationFrames;
        }

        public struct AnimationFrame
        {
            public Vector3 position;
            public Tuple<Vector3[], ShortVector[]> boneRot;
            
        }

        public struct ShortVector
        {
            public short x;
            public short y;
            public short z;
        }

        private void ReadSection3(uint v, MemoryStream ms, BinaryReader br)
        {
            ms.Seek(v, SeekOrigin.Begin);
            animHeader = new AnimationData() {cAnimations = br.ReadUInt32() };
            animHeader.pAnimations = new uint[animHeader.cAnimations];
            for (int i = 0; i < animHeader.cAnimations; i++)
                animHeader.pAnimations[i] = br.ReadUInt32();
            animHeader.animations = new Animation[animHeader.cAnimations];
            for (int i = 0; i < animHeader.cAnimations; i++) //animation
            {
                ms.Seek(v + animHeader.pAnimations[i], SeekOrigin.Begin);
                animHeader.animations[i] = new Animation() {cFrames = br.ReadByte() };
                animHeader.animations[i].animationFrames = new AnimationFrame[animHeader.animations[i].cFrames];
                ExtapathyExtended.BitReader bitReader = new ExtapathyExtended.BitReader(ms);
                for(int n = 0; n<animHeader.animations[i].cFrames; n++) //frames
                {
                    animHeader.animations[i].animationFrames[n] = new AnimationFrame()
                    {position = new Vector3(
                    bitReader.ReadPositionType(),
                    bitReader.ReadPositionType(),
                    bitReader.ReadPositionType())};
                    bitReader.ReadBits(1); //padding byte;
                    animHeader.animations[i].animationFrames[n].boneRot = new Tuple<Vector3[], ShortVector[]>(new Vector3[skeleton.cBones], new ShortVector[skeleton.cBones]);
                    for (int k = 0; k < skeleton.cBones; k++) //bones iterator
                        if (n != 0)
                        {
                            animHeader.animations[i].animationFrames[n].boneRot.Item2[k] = new ShortVector() { x = bitReader.ReadRotationType(), y = bitReader.ReadRotationType(), z = bitReader.ReadRotationType() };
                            animHeader.animations[i].animationFrames[n].boneRot.Item1[k] = new Vector3(
                            (animHeader.animations[i].animationFrames[n - 1].boneRot.Item2[k].x + bitReader.ReadRotationType()) * 180f / 2048f,
                            (animHeader.animations[i].animationFrames[n - 1].boneRot.Item2[k].y + bitReader.ReadRotationType()) * 180f / 2048f,
                            (animHeader.animations[i].animationFrames[n - 1].boneRot.Item2[k].z + bitReader.ReadRotationType()) * 180f / 2048f);
                        }
                        else
                        {
                            animHeader.animations[i].animationFrames[n].boneRot.Item2[k] = new ShortVector() { x = bitReader.ReadRotationType(), y = bitReader.ReadRotationType(), z = bitReader.ReadRotationType() };
                            animHeader.animations[i].animationFrames[n].boneRot.Item1[k] = new Vector3(
                            animHeader.animations[i].animationFrames[n].boneRot.Item2[k].x * 180f / 2048f,
                            animHeader.animations[i].animationFrames[n].boneRot.Item2[k].y * 180f / 2048f,
                            animHeader.animations[i].animationFrames[n].boneRot.Item2[k].z * 180f / 2048f);
                        }
                }
            }
        }
        public AnimationData animHeader;
        public int frame;
        public float frameperFPS = 0.0f;
#endregion

#region section 7
        [StructLayout(LayoutKind.Sequential, Pack =1, Size =380)]
        public struct Information
        {
            [MarshalAs(UnmanagedType.ByValArray,SizeConst =24)]
            private char[] monsterName;
            public uint hp;
            public uint str;
            public uint vit;
            public uint mag;
            public uint spr;
            public uint spd;
            public uint eva;
            public Abilities abilitiesLow;
            public Abilities abilitiesMed;
            public Abilities abilitiesHigh;
            public byte medLevelStart;
            public byte highLevelStart;
            public byte unk;
            public byte bitSwitch;
            public byte cardLow;
            public byte cardMed;
            public byte cardHigh;
            public byte devourLow;
            public byte devourMed;
            public byte devourHigh;
            public byte bitSwitch2;
            public byte unk2;
            public ushort extraExp;
            public ushort exp;
            public ulong drawLow;
            public ulong drawMed;
            public ulong drawHigh;
            public ulong mugLow;
            public ulong mugMed;
            public ulong mugHigh;
            public ulong dropLow;
            public ulong dropMed;
            public ulong dropHigh;
            public byte mugRate;
            public byte dropRate;
            public byte padding;
            public byte ap;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst =16)]
            public byte[] unk3;
            public byte fireResistance;
            public byte iceResistance;
            public byte thunderResistance;
            public byte earthResistance;
            public byte poisonResistance;
            public byte windResistance;
            public byte waterResistance;
            public byte holyResistance;

            public byte deathResistanceMental;
            public byte poisonResistanceMental;
            public byte petrifyResistanceMental;
            public byte darknessResistanceMental;
            public byte silenceResistanceMental;
            public byte berserkResistanceMental;
            public byte zombieResistanceMental;
            public byte sleepResistanceMental;
            public byte hasteResistanceMental;
            public byte slowResistanceMental;
            public byte stopResistanceMental;
            public byte regenResistanceMental;
            public byte reflectResistanceMental;
            public byte doomResistanceMental;
            public byte slowPetrifyResistanceMental;
            public byte floatResistanceMental;
            public byte confuseResistanceMental;
            public byte drainResistanceMental;
            public byte explusionResistanceMental;
            public byte unkResistanceMental;

            public string GetNameNormal => new string(monsterName);
        }

        public struct Abilities
        {
            public byte kernelId; //0x2 magic, 0x4 item, 0x8 mosterAbility;
            public byte unk;
            public ushort abilityId;

        }

        private void ReadSection7(uint v, MemoryStream ms, BinaryReader br)
        {
            ms.Seek(v, SeekOrigin.Begin);
        }

        public Information information;
#endregion

#region section 11
        public struct Textures
        {
            public uint cTims;
            public uint[] pTims;
            public uint Eof;
            public Texture2D[] textures;
        }

        public Textures textures;
#endregion

        private void ReadSection11(uint v, MemoryStream ms, BinaryReader br)
        {
            ms.Seek(v, SeekOrigin.Begin);
            textures = new Textures() { cTims = br.ReadUInt32() };
            textures.pTims = new uint[textures.cTims];
            for (int i = 0; i < textures.cTims; i++)
                textures.pTims[i] = br.ReadUInt32();
            textures.Eof = br.ReadUInt32();
            textures.textures = new Texture2D[textures.cTims];
            for(int i = 0; i<textures.cTims; i++)
            {
                ms.Seek(v + textures.pTims[i], SeekOrigin.Begin);
                TIM2 tm = new TIM2(buffer, (uint)ms.Position); //broken
                textures.textures[i] = new Texture2D(Memory.graphics.GraphicsDevice, tm.GetWidth, tm.GetHeight, false,
                SurfaceFormat.Color);
                textures.textures[i].SetData(tm.CreateImageBuffer(tm.GetClutColors(0), true)); //??
                tm.KillStreams();
            }
        }
        public Debug_battleDat(int monsterId)
        {
            id = monsterId;
            ArchiveWorker aw = new ArchiveWorker(Memory.Archives.A_BATTLE);
            string path = aw.GetListOfFiles().First(x => x.ToLower().Contains($"c0m{id.ToString("D03")}")); //c0m000.dat
            buffer = ArchiveWorker.GetBinaryFile(Memory.Archives.A_BATTLE, path);

#if DEBUG && _WINDOWS
            MakiExtended.DumpBuffer(buffer, "D:/out.dat");
#endif

            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryReader br = new BinaryReader(ms))
            {
                datFile = new DatFile { cSections = br.ReadUInt32()};
                datFile.pSections = new uint[datFile.cSections];
                for (int i = 0; i < datFile.cSections; i++)
                    datFile.pSections[i] = br.ReadUInt32();
                datFile.eof = br.ReadUInt32();

                ReadSection1(datFile.pSections[0],ms,br);
                ReadSection2(datFile.pSections[1],ms,br);
                ReadSection3(datFile.pSections[2], ms, br);
                //ReadSection4(datFile.pSections[3]);
                //ReadSection5(datFile.pSections[4]);
                //ReadSection6(datFile.pSections[5]);
                ReadSection7(datFile.pSections[6],ms,br);
                //ReadSection8(datFile.pSections[7]);
                //ReadSection9(datFile.pSections[8]);
                //ReadSection10(datFile.pSections[9]);
                ReadSection11(datFile.pSections[10],ms,br);
            }
        }





        public int GetId => id;
    }
}