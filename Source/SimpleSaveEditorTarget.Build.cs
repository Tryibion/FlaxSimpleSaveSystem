using Flax.Build;

public class SimpleSaveEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for editor
        Modules.Add("SimpleSave");
        Modules.Add("SimpleSaveEditor");
    }
}
