using Godot;

public partial class RangeSelection : ColorRect
{
    
    [Export]
    private Control progressBar;

    [Signal]
    public delegate void RangeSelectedEventHandler(float start, float end);

    [Signal]
    public delegate void SelectionMovedEventHandler(float start, float end);

    float clickStart = 0;
    bool clicked = false;
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
                        clickStart = mouseButton.Position.X;
                        clicked = true;
                    }
                    else if (clicked && mouseButton.Position.X > clickStart)
                    {
                        clicked = false;
                        EmitSignal(SignalName.RangeSelected, clickStart / rectLen, mouseButton.Position.X / rectLen);
                        EmitSignal(SignalName.SelectionMoved, 0, 0);
                    }
                }
                else if (!mouseButton.Pressed)
                {
                    clicked = false;
                    EmitSignal(SignalName.SelectionMoved, 0, 0);
                }
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                EmitSignal(SignalName.RangeSelected, 0, 1);
            }
        }

        if (@event is InputEventMouseMotion motion && clicked)
        {
            EmitSignal(SignalName.SelectionMoved, clickStart / rectLen, motion.Position.X / rectLen);
        }
    }
}
