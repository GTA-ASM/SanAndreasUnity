﻿using System;
using System.IO;

namespace SanAndreasUnity.Importing.Sections
{
    [SectionType(TypeId)]
    internal class Clump : SectionData
    {
        public const Int32 TypeId = 16;

        public readonly UInt32 ObjectCount;
        public readonly GeometryList GeometryList;

        public Clump(SectionHeader header, Stream stream)
        {
            var dat = Section<Data>.ReadData(stream);
            if (dat == null) return;

            ObjectCount = BitConverter.ToUInt32(dat.Value, 0);
            Section<SectionData>.Read(stream); // frame list
            GeometryList = Section<GeometryList>.ReadData(stream);
        }
    }
}
