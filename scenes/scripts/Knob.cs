using Godot;
using System;

public partial class Knob : Control
{
    // Exported properties
    [Export] public float MinValue { get; set; } = 0.0f;
    [Export] public float MaxValue { get; set; } = 100.0f;
    [Export] public float StartAngle { get; set; } = 220.0f;
    [Export] public float EndAngle { get; set; } = 500.0f;
    [Export] public float Step { get; set; } = 1.0f;
    [Export] public float AngleOffset { get; set; } = -90.0f;
    [Export] public float Sensitivity { get; set; } = 0.5f;
    [Export] public float LabelUnitScale { get; set; } = 1.0f;
    [Export] public string LabelUnit { get; set; } = "";
    private string title = "knob";
    [Export]
    public string Title
    {
        get => title;
        set
        {
            title = value;
            UpdateTitle();
        }
    }
    [Export] public float NonlinearFactor { get; set; } = 1.0f; // Factor for non-linear adjustment; 1.0 means linear

    // Internal state
    private float currentValue;
    [Export]
    public float CurrentValue
    {
        get => currentValue;
        set
        {
            currentValue = Mathf.Round(value * 1000.0f) / 1000.0f;
            if (currentValue > MaxValue)
                currentValue = MaxValue;
            if (currentValue < MinValue)
                currentValue = MinValue;
            UpdateCurrentValue();
        }
    }

    private float previousValue;
    private bool mouseHovered = false;
    private bool mouseDrag = false;
    private float mouseDragStartValue;
    private float accumulatedValue = 0.0f;

    [Signal]
    public delegate void ValueChangedEventHandler(float value);

    public override void _Ready()
    {
        UpdatePointerRotation();
        GetNode<Label>("TitleLabel").Text = Title;
        UpdateValueLabel();
    }

    private void UpdateValueLabel()
    {
        if (LabelUnitScale < 0.0001f)
            LabelUnitScale = 1.0f;
        if (string.IsNullOrEmpty(LabelUnit))
            LabelUnit = "";
        GetNode<Label>("Control/ColorRect/ValueLabel").Text = $"{CurrentValue * LabelUnitScale}{LabelUnit}";
    }

    private void UpdateTitle()
    {
        if (Engine.IsEditorHint())
        {
            GetNode<Label>("TitleLabel").Text = Title;
        }
    }

    private void UpdateCurrentValue()
    {
        if (Engine.IsEditorHint())
        {
            UpdateValueLabel();
            UpdatePointerRotation();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed && mouseHovered)
            {
                OnStartDrag();
            }
            else if (mouseDrag && !mouseButton.Pressed)
            {
                OnStopDrag();
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && mouseDrag)
        {
            ProcessMotion(mouseMotion.Relative);
        }
    }

    private void ProcessMotion(Vector2 relative)
    {
        // Adjust the knob value based on vertical mouse movement
        accumulatedValue += -relative.Y * Sensitivity * (MaxValue - MinValue) / 100.0f;

        // Apply non-linear mapping
        float newValue = Mathf.Clamp(mouseDragStartValue + accumulatedValue, MinValue, MaxValue);
        float normalizedValue = (newValue - MinValue) / (MaxValue - MinValue);
        normalizedValue = Mathf.Pow(normalizedValue, 1.0f / NonlinearFactor);
        newValue = MinValue + normalizedValue * (MaxValue - MinValue);

        CurrentValue = Mathf.Round(newValue / Step) * Step;
        if (Math.Abs(previousValue - CurrentValue) > Mathf.Epsilon)
        {
            UpdateValueLabel();
            previousValue = CurrentValue;
            EmitSignal(SignalName.ValueChanged, CurrentValue);
        }

        UpdatePointerRotation();
    }

    private void OnStartDrag()
    {
        GD.Print("Start drag");
        mouseDrag = true;
        // Calculate the normalized value according to the current value and nonlinear factor
        float normalizedValue = (CurrentValue - MinValue) / (MaxValue - MinValue);
        if (NonlinearFactor != 1.0f)
        {
            normalizedValue = Mathf.Pow(normalizedValue, NonlinearFactor);
        }
        mouseDragStartValue = MinValue + normalizedValue * (MaxValue - MinValue);
        accumulatedValue = 0.0f;
    }

    private void OnStopDrag()
    {
        GD.Print("Stop drag");
        mouseDrag = false;
    }

    private void OnMouseEntered()
    {
        mouseHovered = true;
    }

    private void OnMouseExited()
    {
        mouseHovered = false;
    }

    private void UpdatePointerRotation()
    {
        // Map the current value to the corresponding angle
        float normalizedValue = (CurrentValue - MinValue) / (MaxValue - MinValue);
        GetNode<ColorRect>("Control/ColorRect").Material.Set("shader_parameter/progress", normalizedValue);
    }

    // Uncomment and implement if needed
    /*
    private float ValueToAngle(float value)
    {
        // Convert a value to an angle between `StartAngle` and `EndAngle`
        float normalizedValue = (value - MinValue) / (MaxValue - MinValue);
        normalizedValue = Mathf.Pow(normalizedValue, NonlinearFactor);
        value = MinValue + normalizedValue * (MaxValue - MinValue);
        return Mathf.Lerp(StartAngle, EndAngle, (value - MinValue) / (MaxValue - MinValue));
    }
    */
}
