using Flax.Build;

public class SimpleSaveTarget : GameProjectTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for game
        Modules.Add("SimpleSave");
    }
}
