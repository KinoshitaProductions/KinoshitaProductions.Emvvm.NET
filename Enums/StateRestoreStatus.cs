namespace KinoshitaProductions.Emvvm.Enums;

public enum StateRestoreStatus
{
    NoStateSaved, // no restore possible
    AutomaticRestore, // if less than 12 minutes have passed, should ask if restore
    PromptForRestore, // after 12 minutes, it should prompt if restore
}
