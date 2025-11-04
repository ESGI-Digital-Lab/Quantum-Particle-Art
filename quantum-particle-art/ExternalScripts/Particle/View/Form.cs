using Godot;


public partial class Form : Node
{
    [Export] private Control _root;
    [Export] private Godot.TextEdit _inputField;
    public string Mail   
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