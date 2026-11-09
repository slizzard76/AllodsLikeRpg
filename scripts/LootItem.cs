using Godot;

public partial class LootItem : StaticBody3D
{
    [Export] public string ItemName { get; set; } = "Золото";
    [Export] public int ItemValue { get; set; } = 10;
    [Export] public string ItemType { get; set; } = "Gold"; // "Gold" или "Weapon"

    public override void _Ready()
    {
        var label = GetNode<Label3D>("Label3D");
        label.Text = $"{ItemName} ({ItemValue})";

        // Поднимаем предмет над землёй
        GlobalPosition += Vector3.Up * 0.5f;
    }

    // Метод подбора (вызывается из Main.cs)
    public void Collect()
    {
        Inventory.Instance.AddItem(ItemName, ItemValue, ItemType);
        QueueFree();
    }
}