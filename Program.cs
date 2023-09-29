using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.IO;

internal class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            String app = System.AppDomain.CurrentDomain.FriendlyName;
            // help usage with optional options
            Console.WriteLine($"usage: {app} [-l <language(s)>] [-t <tag>] [--verbose] [--prod]");
            Console.WriteLine($"               <bundle-file> <tsv-file>");
            Console.WriteLine("");
            Console.WriteLine($"Options:");
            Console.WriteLine("");
            Console.WriteLine($"    -l          Language or languages (comma separated) to replace. Supported: 'en', 'es-419'. Default is 'en'");
            Console.WriteLine($"    -t          Version tag to display in main menu");
            Console.WriteLine($"    --verbose   Log every entry");
            Console.WriteLine($"    --prod      Production mode, no debug info");
            Console.WriteLine("");
            Console.WriteLine($"Examples:");
            Console.WriteLine("");
            Console.WriteLine($"    {app} -l es-419 -t nightly-yymmdd loc_packages_assets_.bundle loc_packages_assets.tsv");
            Console.WriteLine($"    {app} -l es-419 -t nightly-yymmdd dialogue_packages_assets_all.bundle dialogue_packages_assets_all.tsv");
            return 1;
        }


        // read options from other args
        List<string> languages = new List<string>();
        bool isVerbose = false;
        string version = "";
        bool isProduction = false;
        if (args.Length > 2) {
            for (int i = 2; i < args.Length; i++) {
                if (args[i] == "-l") {
                    string[] langs = args[i + 1].Split(',');
                    for (int j = 0; j < langs.Length; j++) {
                        var l = langs[j].ToLower();
                        if (l != "en" && l != "es-419") {
                            Console.WriteLine("Only supported languages are 'en' and 'es-419'");
                            return 1;
                        }
                        languages.Add(l);
                    }
                }
                else if (args[i] == "--prod") {
                    isProduction = true;
                }
                else if (args[i] == "--verbose") {
                    isVerbose = true;
                } else if (args[i] == "-t") {
                    version = args[i + 1];
                }
            }
        }

        var bundleFileName = args[args.Length-2];
        var tsvFileName = args[args.Length-1];

        if (!bundleFileName.EndsWith(".bundle")) {
            Console.WriteLine($"WARNING: {bundleFileName} probably is not a BUNDLE file");
        }
        if (!tsvFileName.EndsWith(".tsv")) {
            Console.WriteLine($"WARNING: {tsvFileName} probably is not a TSV file");
        }

        if (languages.Count == 0) {
            languages.Add("en");
        }

        // read file 'scenes-ignore-debug.txt' with scenes to not add prefixes
        var toIgnore = new List<string>();
        if (File.Exists("scenes-ignore-debug.txt")) {
            var lines = File.ReadAllLines("scenes-ignore-debug.txt");
            foreach (var line in lines) {
                if (line == "")
                    continue;
                toIgnore.Add(line);
            }
        }

        string prefix = "";
        if (tsvFileName.Contains("dialog")) {
            prefix = "D";
        }
        // read TSV >> dictionary<entryName, text>
        var dict = LoadTsv(tsvFileName, prefix, isProduction, toIgnore, version);

        // patch bundle assets
        PatchBundle(bundleFileName, dict, languages, isVerbose);

        return 0;
    }

    private static Dictionary<string, string> LoadTsv(string fileName, string prefix, bool isProduction, List<string> toIgnore, string version) {
        int numCol = 0;
        int sceneCol = 1;
        int entryCol = 2;
        int enCol = 4;
        int textCol = 5;

        Dictionary<String, String> dict = new Dictionary<String, String>();

        StreamReader sr = new StreamReader(fileName);
        string line;
        int i = 0;
        while ((line = sr.ReadLine()) != null)
        {
            // skip first 3 lines (header)
            if (i < 3) {
                i++;
                continue;
            }

            // split line by tab
            string[] parts = line.Split('\t');
            string text = parts[textCol];
            string scene = parts[sceneCol];

            if (scene == "COLL.LETTER") {
                text = text.Replace("<", "");
                text = text.Replace(">", "");
            }
            string entryName = parts[entryCol];
            if (entryName == "SYS.MENU_0000") {
                text = "Играть" + ((version != "") ? $" ({version})" : "");
            }
            
            bool ignore = toIgnore.Contains(scene);
            if (text.Length == 0 && parts[enCol].Length > 0 && !ignore) {
                text = parts[enCol] + "-><пусто>";
            }

            if (!isProduction && !ignore && parts[textCol] != "DO NOT DELETE OR CHANGE") {
                text = "[" + prefix + parts[numCol] + "] " + text;
            }

            // add to dictionary
            dict.Add(parts[entryCol], text);

            i++;
        }

        return dict;
    }

    private static void PatchBundle(
        string filePath,
        Dictionary<String, String> translationMap,
        List<string> languages,
        bool isVerbose = false
    )
    {
        Console.WriteLine($"INFO: patching bundle file {filePath}");
        var manager = new AssetsManager();

        var bundleInstance = manager.LoadBundleFile(filePath, true);
        var assetsInstance = manager.LoadAssetsFileFromBundle(bundleInstance, 0, false);
        var assets = assetsInstance.file;

        var assetsReplacers = new List<AssetsReplacer>();

        foreach (var mono in assets.GetAssetsOfType(AssetClassID.MonoBehaviour))
        {
            var monoBase = manager.GetBaseField(assetsInstance, mono);
            var objName = monoBase["m_Name"].AsString;

            // skip non-text
            if (!objName.Contains("_Text"))
                continue;

            // skip with no lang tag
            string lang;
            try {
                lang = monoBase["_ietfTag"].AsString;
            } catch {
                continue;
            }

            // skip other langs
            if (!languages.Contains(lang)) {
                continue;
            }

            if (isVerbose)
                Console.WriteLine($"DEBUG: rewrite text {objName}");
            var sceneEntries = monoBase["_database"]["_entries"]["Array"];
            foreach (var e in sceneEntries)
            {
                var entryName = e["_entryName"].AsString;
                var translation = translationMap.GetValueOrDefault(entryName);
                if (translation == null)
                {
                    Console.WriteLine($"WARNING: no translation for {entryName}");
                    continue;
                }

                e["_localization"].AsString = translation;
            }

            assetsReplacers.Add(new AssetsReplacerFromMemory(assets, mono, monoBase));
        }

        var bundleReplacers = new List<BundleReplacer>();
        bundleReplacers.Add(new BundleReplacerFromAssets(assetsInstance.name, null, assets, assetsReplacers));
        using (AssetsFileWriter writer = new AssetsFileWriter(filePath + ".mod.uncompressed"))
        {
            bundleInstance.file.Write(writer, bundleReplacers);
        }

        // compress bundle
        Console.WriteLine("INFO: compressing bundle");
        var uncompressedBundle = new AssetBundleFile();
        var reader = new AssetsFileReader(File.OpenRead(filePath + ".mod.uncompressed"));
        uncompressedBundle.Read(reader);
        using (AssetsFileWriter writer = new AssetsFileWriter(filePath + ".mod"))
        {
            uncompressedBundle.Pack(reader, writer, AssetBundleCompressionType.LZ4);
        }
        uncompressedBundle.Close();

        // remove uncompressed file
        File.Delete(filePath + ".mod.uncompressed");

        Console.WriteLine("INFO: patching done");
    }

}