using HashDepot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Mario64TextureRenamer {
    class Program {
        public static String getHex(int number, byte hexDigits) {
            return number.ToString("X" + hexDigits);
        }

        static void rescursiveDig(String directoryName, List<String> filesList) {
            IEnumerable<String> files = Directory.EnumerateFiles(directoryName);
            filesList.AddRange(files);

            IEnumerable<String> data = Directory.EnumerateDirectories(directoryName);
            foreach (String directory in data) {
                files = Directory.EnumerateFiles(directory);
                filesList.AddRange(files);

                rescursiveDig(directory, filesList);
            }
        }

        private static void doRename(string[] args) {
            List<String> images = new List<String>();
            rescursiveDig(args[1], images);

            List<String> rawDataFiles = new List<String>();
            rescursiveDig(args[2], rawDataFiles);

            foreach (String imageName in images) {
                if (imageName.Contains(".png")) {
                    int count = 0;
                    String sub = imageName.Substring(args[1].Length, imageName.Length - 5 - args[1].Length);
                    foreach (String rawFileName in rawDataFiles) {
                        if (!rawFileName.Contains(".c") && rawFileName.Contains(sub)) {
                            byte[] bytes = File.ReadAllBytes(rawFileName);
                            uint result = XXHash.Hash32(bytes);
                            Console.WriteLine("Found one " + count + " - " + sub + " crc " + getHex((int)result, 8) + rawFileName);
                            byte[] imageData = File.ReadAllBytes(imageName);
                            File.WriteAllBytes(args[3] + getHex((int)result, 8) + ".png", imageData);
                            count++;
                        }
                    }
                }
            }
        }

        private static void generateSplitConfig(string[] args) {
            List<String> images = new List<String>();
            rescursiveDig(args[1], images);

            List<Bitmap> splitFiles = new List<Bitmap>();
            List<String> splitFileNames = new List<String>();
            foreach (String imageFile in images) {
                if (imageFile.Contains(".png")) {
                    splitFiles.Add(new Bitmap(imageFile));
                    splitFileNames.Add(imageFile.Substring(imageFile.LastIndexOf("\\") + 1));
                }
            }

            Bitmap fullImage = new Bitmap(args[2]);

            String splitConfig = "";

            for (int index = 0; index < splitFiles.Count; index ++) {
                Bitmap splitImage = splitFiles[index];

                bool handled = false;
                for (int x = 0; !handled && x < 216; x ++) {
                    for (int y = 0; !handled && y < 216; y++) {
                        bool found = true;
                        for (int dx = 0; found && dx < 32; dx ++) {
                            for (int dy = 0; found && dy < 32; dy++) {
                                found = fullImage.GetPixel(x + dx, y + dy) == splitImage.GetPixel(dx, dy);
                            }
                        }

                        if (found) {
                            handled = true;
                            Console.WriteLine("Found split file at " + x + ", " + y);
                            splitConfig += splitFileNames[index] + "," + x + "," + y + "\r\n";
                        }
                    }
                }
            }

            if (splitConfig != "") {
                File.WriteAllText(args[3], splitConfig);
            }
        }

        static void Main(string[] args) {
            if ("rename".Equals(args[0])) {
                doRename(args);
            } else if ("genSplitCfg".Equals(args[0])) {
                generateSplitConfig(args);
            } else if ("applySplitCfg".Equals(args[0])) {
                applySplitConfig(args);
            }

            Console.ReadKey();
        }

        private static void applySplitConfig(string[] args) {
            Bitmap fullImage = new Bitmap(args[1]);

            String configData = File.ReadAllText(args[2]);
            String[] configLines = configData.Split(new[] { "\r\n" }, StringSplitOptions.None);
            foreach (String configLine in configLines) {
                if (configLine == "" ) {
                    Bitmap result = new Bitmap(256, 256);
                    String[] config = configLine.Split(new[] { "," }, StringSplitOptions.None);
                    if (config.Length == 3) {
                        int xStart = int.Parse(config[1]) * 8;
                        int yStart = int.Parse(config[2]) * 8;
                        for (int x = 0; x < 256; x++) {
                            for (int y = 0; y < 256; y++) {
                                result.SetPixel(x, y, fullImage.GetPixel(xStart + x, yStart + y));
                            }
                        }

                        result.Save(config[0]);
                    }
                }
            }
        }
    }
}
