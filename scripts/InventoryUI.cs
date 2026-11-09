using Godot;

public partial class InventoryUI : PanelContainer
{
    private Label _goldLabel;
    private VBoxContainer _itemsList;

    public override void _Ready()
    {
        _goldLabel = GetNode<Label>("MarginContainer/VBoxContainer/GoldLabel");
        _itemsList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/ItemsList");

        GetNode<Button>("MarginContainer/VBoxContainer/CloseButton").Pressed += ToggleUI;
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.I)
            {
                ToggleUI();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void ToggleUI()
    {
        Visible = !Visible;
        GetTree().Paused = Visible;

        if (Visible) UpdateUI();
    }

    private void UpdateUI()
    {
        var inv = Inventory.Instance;
        if (inv == null) return;

        _goldLabel.Text = $"Золото: {inv.Gold}";

        foreach (Node child in _itemsList.GetChildren())
            child.QueueFree();

        foreach (var item in inv.Items)
        {
            var hbox = new HBoxContainer();
            var label = new Label
            {
                Text = $"{item.Name} (+{item.DamageBonus} урона)",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            var btn = new Button
            {
                Text = "Надеть"
            };
            btn.Pressed += () => {
                inv.EquipWeapon(item);
                UpdateUI();
            };

            hbox.AddChild(label);
            hbox.AddChild(btn);
            _itemsList.AddChild(hbox);
        }
    }
}