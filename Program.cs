using HashDepot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Mario64TextureRenamer {
    class Program {
        public static String getHex(int number, byte hexDigits) {
            return number.ToString("X" + hexDigits);
        }

        static void Main(string[] args) {
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

            bool isWindows = Environment.OSVersion.Platform.ToString().ToLower().Contains("win");
            foreach (String imageName in images) {
                int index;
                if (isWindows) {
                    index = imageName.LastIndexOf("\\");
                } else {
                    index = imageName.LastIndexOf("/");
                }
                String sub = imageName.Substring(index + 1, imageName.Length - 5 - index);
                foreach (String rawFileName in rawDataFiles) {
                    if (rawFileName.Contains(sub)) {
                        byte[] bytes = File.ReadAllBytes(rawFileName);
                        uint result = XXHash.Hash32(bytes);
                        Console.WriteLine("Found one - " + sub + " crc " + getHex((int)result, 8) + rawFileName);
                        byte[] imageData = File.ReadAllBytes(imageName);
                        File.WriteAllBytes(args[2] + getHex((int)result, 8) + ".png", imageData);
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
