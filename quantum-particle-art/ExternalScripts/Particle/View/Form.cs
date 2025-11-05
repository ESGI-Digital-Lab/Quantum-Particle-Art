using Godot;
using KGySoft.CoreLibraries;


public partial class Form : Control
{
    [ExportGroup("References")]
    [Export] private Control _root;
    [Export] private Godot.LineEdit _inputField;
    [Export] private Button _submit;
    [Export] private Button _close;
    [ExportGroup("Settings")]
    [Export] private bool _visibleOnStart = false;
    [Export] private Color _error;
    [Export] private string _textColorKey = "font_color";
    public event System.Action<string> OnSubmit;
    public event System.Action OnExit;
    public override void _Ready()
    {
        base._EnterTree();
        _submit.Pressed += () => { OnSubmit?.Invoke(Mail); };
        _inputField.TextSubmitted += (string text) => { OnSubmit?.Invoke(Mail); };
        _inputField.EditingToggled += on =>
        {
            if(on)
                _inputField.RemoveThemeColorOverride(_textColorKey);
        };
        _close.Pressed += () => { OnExit?.Invoke(); };
        OnExit += Exited;
    }
    

    private void Exited()
    {
        _root.Visible = false;
        _inputField.Clear();
    }

    private string Mail
    {
        get
        {
            var mail = _inputField.Text.Trim();
            return mail;
        }
    }

    public new bool Visible
    {
        set
        {
            if (value) Entered();
            else Exited();
        }
    }

    public void Recolor()
    {
        _inputField.AddThemeColorOverride(_textColorKey, _error);
    }

    private bool Entered()
    {
        return _root.Visible = true;
    }

    public void SetInitState()
    {
        this.Visible = _visibleOnStart;
    }
}