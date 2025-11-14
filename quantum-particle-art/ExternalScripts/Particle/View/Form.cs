using Godot;
using KGySoft.CoreLibraries;


public partial class Form : Control
{
    [ExportGroup("References")] [Export] private Control _root;
    [Export] private Godot.LineEdit _inputField;
    [Export] private Button _clear;
    [Export] private Button _submit;
    [Export] private Button _close;
    [ExportGroup("Settings")] [Export] private bool _visibleOnStart = false;
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
            if (on)
                _inputField.RemoveThemeColorOverride(_textColorKey);
        };
        _close.Pressed += () => { Exit(); };
        _clear.Pressed += () => { _inputField.Clear(); };
    }


    private void Exit()
    {
        if (_root.Visible)
        {
            _root.Visible = false;
            _inputField.Clear();
            OnExit?.Invoke();
        }
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
        get => _root.Visible;
        set
        {
            if (value == Visible) return;
            if (value) Entered();
            else Exit();
        }
    }

    public void Recolor()
    {
        _inputField.AddThemeColorOverride(_textColorKey, _error);
    }

    private void Entered()
    {
        if (!_root.Visible)
        {
            _root.Visible = true;
            _inputField.GrabFocus();
        }
    }

    public void SetInitState()
    {
        this.Visible = _visibleOnStart;
    }
}