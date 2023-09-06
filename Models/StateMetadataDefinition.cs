using KinoshitaProductions.Common.Interfaces;
using Newtonsoft.Json;

namespace KinoshitaProductions.Emvvm.Models;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class StateMetadataDefinition : IStatefulAsJsonWithTimestamp
{
    [JsonProperty("_t")]
    DateTime IStatefulAsJsonWithTimestamp.Timestamp { get => Timestamp; set => Timestamp = value; }
    [JsonIgnore]
    public DateTime Timestamp { get; set; } = DateTime.Now;
    [JsonIgnore]
    public string? StateJson { get; set; }
    [JsonIgnore]
    public int LastViewModelGeneration { get; set; }
    [JsonProperty("dad")]
    public int DeepestActivationDepth { get; set; }
    public virtual bool IsValid => DeepestActivationDepth >= 1;
    [JsonIgnore]
    public int MaxNavigatableDepthPreSave { get; set; }
    public virtual bool HasChanges() => DeepestActivationDepth != MaxNavigatableDepthPreSave;
    public virtual void UpdateMetadataForSaving()
    {
        LastViewModelGeneration = State.LastViewModelGeneration;
        DeepestActivationDepth = MaxNavigatableDepthPreSave;
        Timestamp = DateTime.Now;
    }
}
