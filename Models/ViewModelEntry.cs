using KinoshitaProductions.Common.Interfaces;
using Newtonsoft.Json;
#pragma warning disable CS8618

namespace KinoshitaProductions.Emvvm.Models
{
    /// <summary>
    /// Helper class for state serialization.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [JsonConverter(typeof(ViewModelJsonConverter))]
    public class ViewModelEntry : IStatefulAsJson
    {
        [JsonIgnore]
        public string? StateJson { get => Value.StateJson; set => Value.StateJson = value; }
        /// <summary>
        /// Kind of ViewModel, e.g. M(ainViewModel), P(ostViewModel)
        /// </summary>
        [JsonProperty("k")]
        public string Kind { get; set; }
        /// <summary>
        /// Data to load into the ViewModel.
        /// </summary>
        [JsonProperty("v")]
        public ObservableViewModel Value { get; set; }
    }
}
