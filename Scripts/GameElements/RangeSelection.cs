using Godot;

public partial class RangeSelection : ColorRect
{
    
    [Export]
    private Control progressBar;

    [Signal]
    public delegate void RangeSelectedEventHandler(float start, float end);

    [Signal]
    public delegate void SelectionMovedEventHandler(float start, float end);

    private float clickStartTime = 0;
    private bool selectionActive = false;
    private float rectLen;

    public override void _Ready()
    {
        rectLen = GetViewportRect().Size.X;
        SetProcessInput(true);
    }

    public override void _Input(InputEvent @event)
    {
        // TODO: Optimize
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Position.Y < progressBar.Size.Y)
                {
                    if (mouseButton.Pressed)
                    {
                        clickStartTime = mouseButton.Position.X;
                        selectionActive = true;
                    }
                    else if (selectionActive && mouseButton.Position.X > clickStartTime)
                    {
                        selectionActive = false;
                        EmitSignal(SignalName.RangeSelected, clickStartTime / rectLen, mouseButton.Position.X / rectLen);
                        EmitSignal(SignalName.SelectionMoved, 0, 0);
                    }
                }
                else if (!mouseButton.Pressed)
                {
                    selectionActive = false;
                    EmitSignal(SignalName.SelectionMoved, 0, 0);
                }
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                EmitSignal(SignalName.RangeSelected, 0, 1);
            }
        }

        if (@event is InputEventMouseMotion motion && selectionActive)
        {
            EmitSignal(SignalName.SelectionMoved, clickStartTime / rectLen, motion.Position.X / rectLen);
        }
    }
}
