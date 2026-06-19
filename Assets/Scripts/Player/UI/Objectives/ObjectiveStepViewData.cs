public readonly struct ObjectiveStepViewData
{
    public ObjectiveStepViewData(string text, string description)
    {
        Text = text;
        Description = description;
    }

    public string Text { get; }
    public string Description { get; }
}
