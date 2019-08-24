﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using SanAndreasUnity.Importing.Archive;
using SanAndreasUnity.Utilities;
using UnityEngine;

namespace SanAndreasUnity.Importing.GXT
{
    public class GTX : IDisposable
    {

	    #region LoadPath
		//this whole region needs refactoring
	    enum Language
	    {
		    american,
		    french,
		    german,
		    italian,
		    spanish
	    }


	    private static Language defaultLanguage = Language.american;
	    //this code is pasted from ArchiveMgr; 
	    //todo will refactor in anther commit
	    public static string GameDir => Config.Get<string>("game_dir");
	    public static string GTXDir => Path.Combine(GameDir, "text");

	    //todo put path temp here, will refactor
	    private static string loadPath=> Path.Combine(GTXDir,$"{defaultLanguage}.gxt");
		#endregion

		#region GTXData

		private Int16 version;
		public List<string> SubTableNames { get; } = new List<string>();
		public Dictionary<string, List<int>> TableEntryNameDict { get; } = new Dictionary<string, List<Int32>>();
		public Dictionary<int, string> EntryNameWordDict { get; } = new Dictionary<int, string>();

		private static GTX gtx;
		public static GTX Gtx
		{
			get
			{
				if (gtx == null)
				{
					GTX.Load();
				}

				return gtx;
			}
		}

		private  MemoryStream _rawData;

		#endregion

		//todo will need move to a setting file, will talk about it later.



		public GTX(string fp)
        {
            var bytes = File.ReadAllBytes(loadPath);
            _rawData = new MemoryStream(bytes, false);
        }

        public static void Load()
        {
	        if (gtx != null)
	        {
		        return ;
	        }
			gtx=new GTX(loadPath);
			gtx.InternalLoad();
			gtx._rawData.Dispose();
        }

       
        private void InternalLoad()
        {
            var encoding = LoadHeader();
            var tkeyEntryOffsets = LoadTableBlock(encoding);

            for (int i = 0; i < SubTableNames.Count; i++)
            {
                var subTableName = SubTableNames[i];
                _rawData.Seek(tkeyEntryOffsets[i], SeekOrigin.Begin);
                using (var binReader = new BinaryReader(_rawData, encoding, true))
                {
                    if (i == 0) //parse tkey directly
                    {
                        LoadTKEY(binReader,subTableName);
                    }
                    else //skip tkeyname
                    {
                        _rawData.Seek(8, SeekOrigin.Current);
                        LoadTKEY(binReader,subTableName);
                    }
                }

            }
        }

        private void LoadTKEY(BinaryReader binaryReader,string tableName)
        {
            //Console.WriteLine(nameof(LoadTKEY));
            var tkey = new string(binaryReader.ReadChars(4));
            var blockSize = binaryReader.ReadInt32();

            //Console.WriteLine($"tkey:{tkey} blockSize:{blockSize}");
            if (tkey != "TKEY")
            {
                throw new ConstraintException();
            }

            var entryOffsets = new List<Int32>();
            var tdataEntryCount = blockSize / 8;
            var entryNames=new List<Int32>();
            for (int i = 0; i < tdataEntryCount; i++)
            {

                var entryOffset = binaryReader.ReadInt32();
                var crc32 = binaryReader.ReadInt32();
                entryNames.Add(crc32);
                entryOffsets.Add(entryOffset);
            }

            TableEntryNameDict[tableName] = entryNames;

            //read tdat
            var tdat =new string(binaryReader.ReadChars(4));
            if (tdat != "TDAT")
            {
                throw  new ConstraintException();
            }
            var tdatBlockSize = binaryReader.ReadInt32();
            var bytes=binaryReader.ReadBytes(tdatBlockSize);
            var wordBytes=new List<byte>();
            for (int i = 0; i < entryOffsets.Count; i++)
            {
                var entryName = entryNames[i];
                for (int j = entryOffsets[i]; ; j++)
                {
                    var c = Convert.ToChar(bytes[j]);
                    if (c == '\0')
                    {
                        break;
                    }
                    wordBytes.Add(bytes[j]);
                }
                EntryNameWordDict[entryName] = Win1252ToString(wordBytes.ToArray());
                wordBytes.Clear();
            }
        }

        private string Win1252ToString(byte[] bytes)
        {
            Encoding win1252 = Encoding.GetEncoding(1252);
            var utf8bytes = Encoding.Convert(win1252, Encoding.UTF8, bytes);
            return Encoding.UTF8.GetString(utf8bytes);
        }


        private Encoding LoadHeader()
        {
            using (var binReader = new BinaryReader(_rawData, Encoding.ASCII, true))
            {
                version = binReader.ReadInt16();
                var bitsPerChar = binReader.ReadInt16();
                if (bitsPerChar == 8)
                {
                    return Encoding.ASCII;
                }
                else if (bitsPerChar == 16)
                {
                    return Encoding.Unicode;
                }
            }
            return Encoding.ASCII;
        }

        private List<Int32> LoadTableBlock(Encoding encoding)
        {
            using (var binReader = new BinaryReader(_rawData, encoding, true))
            {
                var offsetList = new List<Int32>();
                var tablConst = binReader.ReadChars(4);
                if (new string(tablConst) != "TABL")
                {
                    throw new ConstraintException();
                }

                var blockSize = binReader.ReadInt32();
                var entryCount = blockSize / 12;
                for (int i = 0; i < entryCount; i++)
                {
                    var subtableName = new string(binReader.ReadChars(8));
                    var offset = binReader.ReadInt32();

                    //Console.WriteLine($"subtablename:{subtableName} offset:{offset}");
                    SubTableNames.Add(subtableName);
                    offsetList.Add(offset);
                }

                return offsetList;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"version:{version}");
            foreach (var kv in TableEntryNameDict)
            {
                stringBuilder.AppendLine($"-----------table {kv.Key} starts------------");
                foreach (var entrykey in kv.Value)
                {
                    stringBuilder.AppendLine($"entry:{entrykey} value:{EntryNameWordDict[entrykey]}");
                }

                stringBuilder.AppendLine($"-----------table {kv.Key} ends------------");
            }

            return stringBuilder.ToString();
        }


        public void Dispose()
        {
            if (_rawData != null)
            {
                _rawData.Dispose();
                _rawData = null;
            }
        }
    }
}
