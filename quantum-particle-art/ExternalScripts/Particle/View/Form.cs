using Godot;


public partial class Form : Control
{
    [Export] private Control _root;
    [Export] private Godot.LineEdit _inputField;
    [Export] private Button _submit;
    public event System.Action<string> OnSubmit;
    public override void _Ready()
    {
        _submit.Pressed += () =>
        {
            OnSubmit?.Invoke(Mail);
        };
        _inputField.TextSubmitted += (string text) =>
        {
            OnSubmit?.Invoke(Mail);
        };
    }
    private string Mail   
    {
        get
        {
            var mail = _inputField.Text.Trim();
            return mail;
        }
    }
    public bool Visible   
    {
        set => _root.Visible = value;
    }
}