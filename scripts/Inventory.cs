using Godot;
using System.Collections.Generic;

public partial class Inventory : Node
{
    public static Inventory Instance { get; private set; }

    public Item EquippedWeapon = null;

    public class Item
    {
        public string Name;
        public int Value;
        public string Type; // "Gold" или "Weapon"
        public int DamageBonus; // Бонус к урону (для оружия)
    }

    public List<Item> Items = new();

    public int Gold = 0;

    public override void _Ready()
    {
        Instance = this;
    }

    public void AddItem(string name, int value, string type)
    {
        if (type == "Gold")
        {
            Gold += value;
            GD.Print($"Получено золото! Всего: {Gold}");
        }
        else
        {
            Items.Add(new Item { Name = name, Value = value, Type = type, DamageBonus = 5 }); // +5 к урону
            GD.Print($"Получен предмет: {name}");
        }
    }

    public void EquipWeapon(Item weapon)
    {
        if (weapon.Type != "Weapon") return;
        EquippedWeapon = weapon;
        GD.Print($"Экипировано оружие: {weapon.Name} (+{weapon.DamageBonus} к урону)");
        // Здесь в будущем мы будем обновлять UI инвентаря
    }
}