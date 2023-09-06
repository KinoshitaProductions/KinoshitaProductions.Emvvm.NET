namespace KinoshitaProductions.Emvvm.Services
{
    public static class ViewModelManager
    {
        private static readonly Dictionary<string, ViewModelMapping> KnownMappings = new ();
        public static void SetupMappings(Dictionary<string, ViewModelMapping> mappings)
        {
            foreach (var entry in mappings)
                KnownMappings.TryAdd(entry.Key, entry.Value);
        }
        public static ViewModelMapping GetMappingFor(string kind) => KnownMappings[kind];
        public static bool ExistsMappingFor(string kind) => KnownMappings.ContainsKey(kind);
    }
}
