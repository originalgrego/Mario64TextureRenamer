using HashDepot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Mario64TextureRenamer {
    class Program {
    private static uint CRC32_POLYNOMIAL = 0x04C11DB7;

    private static uint[] CRCTable = new uint[256];

    private static uint Reflect(uint reference, byte ch) {
        uint value = 0;

            // Swap bit 0 for bit 7
            // bit 1 for bit 6, etc.
        for (int i = 1; i < (ch + 1); ++i) {
            if ((reference & 1) > 0) { 
                value |= (uint) (1 << (ch - i));
            }
            reference >>= 1;
        }
	    return value;
    }

    private static void CRC_Init() {
        uint crc;

        for (int i = 0; i < 256; ++i) {
            crc = Reflect((uint)i, 8) << 24;
            for (int j = 0; j < 8; ++j)
                    crc = (((crc << 1) ^ (crc & (1 << 31))) > 0 ? CRC32_POLYNOMIAL : 0);

                CRCTable[i] = Reflect(crc, 32);
        }
    }

    public static uint CRC_Calculate(byte[] buffer, uint count) {
        uint crc = 0;
        for (int x = 0; x < count; x ++) { 
            crc = (crc >> 8) ^ CRCTable[(crc & 0xFF) ^ buffer[x]];
        }

        return crc;
    }

        public static String getHex(int number, byte hexDigits) {
            return number.ToString("X" + hexDigits);
        }

        static void Main(string[] args) {
            CRC_Init();

            List<String> images = new List<String>();
            IEnumerable<String> data = Directory.EnumerateDirectories(args[0]);
            foreach (String directory in data) {
                Console.WriteLine(directory);
                IEnumerable<String> files = Directory.EnumerateFiles(directory);
                images.AddRange(files);
            }

            List<String> rawDataFiles = new List<String>();
            IEnumerable<String> data2 = Directory.EnumerateDirectories(args[1]);
            foreach (String directory in data2) {
                Console.WriteLine(directory);
                IEnumerable<String> files = Directory.EnumerateFiles(directory);
                rawDataFiles.AddRange(files);
            }

            foreach (String imageName in images) {
                int index = imageName.LastIndexOf("\\");
                String sub = imageName.Substring(index + 1, imageName.Length - 5 - index);
                foreach (String rawFileName in rawDataFiles) {
                    if (rawFileName.Contains(sub)) {
                        byte[] bytes = File.ReadAllBytes(rawFileName);
                     //   ulong h64_1 = xxHash64.ComputeHash(bytes, bytes.Length);
                        uint crc = CRC_Calculate(bytes, (uint)bytes.Length);
                        uint result = XXHash.Hash32(bytes);
                       Console.WriteLine("Found one - " + sub + " crc " + getHex((int)result, 8) + rawFileName);
                        byte[] imageData = File.ReadAllBytes(imageName);
                        File.WriteAllBytes(args[2] + getHex((int)result, 8) + ".png", imageData);
//                        File.WriteAllBytes(imageName.Replace("textures_pngs", "textures_out_combined").Replace(sub, getHex((int)result, 8)), imageData);
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
