using System;
using System.Collections;
using System.IO;
using System.Resources;

class ResourceCompiler {
    static void Main(string[] args) {
        using (var reader = new ResXResourceReader(args[0])) {
            reader.BasePath = Path.GetDirectoryName(Path.GetFullPath(args[0]));
            using (var writer = new ResourceWriter(args[1])) {
                foreach (DictionaryEntry entry in reader) writer.AddResource((string)entry.Key, entry.Value);
            }
        }
    }
}
