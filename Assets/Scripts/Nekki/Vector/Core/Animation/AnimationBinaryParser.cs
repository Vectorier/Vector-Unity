using System;
using System.Collections.Generic;
using GameDiagnostics;
using System.IO;
using Nekki.Vector.Core.Frame;
using UnityEngine;

namespace Nekki.Vector.Core.Animation
{
    public static class AnimationBinaryParser
    {
        private static readonly Dictionary<string, Provider> CachedBinary = new Dictionary<string, Provider>();

        public static void ClearCachedBinary()
        {
            CachedBinary.Clear();
        }

        public static Provider ParseFile(string fileName, bool useCache = true)
        {
            if (useCache)
            {
                if (CachedBinary.ContainsKey(fileName))
                {
                    return CachedBinary[fileName];
                }
            }
            var bytes = ResourceManager.GetBinary(fileName);
            if (bytes == null)
            {
                GameLog.Warning("Loading the moves", "The move's animation file does not exist.", "The move has no frames.", fileName);
                return new Provider(0);
            }
            try
            {
                return Parse(fileName, bytes, useCache);
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is IOException || ex is OverflowException || ex is OutOfMemoryException || ex is ArgumentException)
            {
                // A .bin cut short or of another format: the move plays without frames, as a missing one does.
                GameLog.Add(GameErrorReport.From(new GameDataException(
                    $"The animation file is cut short or is not a move animation (.bin, {bytes.Length} bytes).", fileName, inner: ex),
                    "Loading the moves", GameErrorSeverity.Warning, "The move has no frames."));
                return new Provider(0);
            }
        }

        private static Provider Parse(string fileName, byte[] bytes, bool useCache)
        {
            var stream = new MemoryStream(bytes);
            var reader = new BinaryReader(stream);

            int count = reader.ReadInt32();
            var provider = new Provider(count);

            for (int i = 0; i < count; i++)
            {
                reader.ReadByte();
                int num = reader.ReadInt32();
                Vector3[] array = new Vector3[num];

                for (int j = 0; j < num; j++)
                {
                    array[j].x = reader.ReadSingle();
                    array[j].y = 0f - reader.ReadSingle();
                    array[j].z = reader.ReadSingle();

                }
                provider.Add(array, i);
            }
            reader.Close();
            stream.Close();
            if (useCache)
            {
                CachedBinary.Add(fileName, provider);
            }
            return provider;
        }
    }
}
