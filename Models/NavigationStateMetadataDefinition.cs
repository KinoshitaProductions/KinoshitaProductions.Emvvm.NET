using Newtonsoft.Json;

namespace KinoshitaProductions.Emvvm.Models;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class NavigationStateMetadataDefinition : StateMetadataDefinition
{
    [JsonProperty("nec")]
    public int NavigationEnginesCount { get; set; }
    [JsonIgnore]
    public int MaxNavigatableEnginePreSave { get; set; }
    public override bool IsValid => NavigationEnginesCount >= 1 || base.IsValid;
    public override bool HasChanges() => base.HasChanges() || NavigationEnginesCount != MaxNavigatableEnginePreSave;
    public override void UpdateMetadataForSaving()
    {
        base.UpdateMetadataForSaving();
        NavigationEnginesCount = MaxNavigatableEnginePreSave;
    }
}
