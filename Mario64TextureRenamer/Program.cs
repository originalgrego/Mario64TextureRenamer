using HashDepot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Mario64TextureRenamer {
    class Program {
        public static String getHex(int number, byte hexDigits) {
            return number.ToString("X" + hexDigits);
        }

        static void rescursiveDig(String directoryName, List<String> filesList) {
            IEnumerable<String> data = Directory.EnumerateDirectories(directoryName);
            foreach (String directory in data) {
                IEnumerable<String> files = Directory.EnumerateFiles(directory);
                filesList.AddRange(files);

                rescursiveDig(directory, filesList);
            }
        }

        static void Main(string[] args) {
            List<String> images = new List<String>();
            rescursiveDig(args[0], images);

            List<String> rawDataFiles = new List<String>();
            rescursiveDig(args[1], rawDataFiles);

            foreach (String imageName in images) {
                if (imageName.Contains(".png")) {
                    int count = 0;
                    String sub = imageName.Substring(args[0].Length, imageName.Length - 5 - args[0].Length);
                    foreach (String rawFileName in rawDataFiles) {
                        if (!rawFileName.Contains(".c") && rawFileName.Contains(sub)) {
                            byte[] bytes = File.ReadAllBytes(rawFileName);
                            uint result = XXHash.Hash32(bytes);
                            Console.WriteLine("Found one " + count + " - " + sub + " crc " + getHex((int)result, 8) + rawFileName);
                            byte[] imageData = File.ReadAllBytes(imageName);
                            File.WriteAllBytes(args[2] + getHex((int)result, 8) + ".png", imageData);
                            count++;
                        }
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
