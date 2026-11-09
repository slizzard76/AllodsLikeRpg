using Godot;
using System.Collections.Generic;
//using System.IO;
using System.Text.Json;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }
    private string _savePath = "user://savegame.json";

    public static bool IsLoading = false;
    public override void _Ready()
    {
        Instance = this;
    }

    // --- КЛАССЫ ДАННЫХ ---
    public class UnitData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Health { get; set; }
    }

    public class EnemyData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Health { get; set; }
    }

    public class LootData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string ItemName { get; set; }
        public int ItemValue { get; set; }
        public string ItemType { get; set; }
    }

    public class InventoryData
    {
        public int Gold { get; set; }
        public List<ItemData> Items { get; set; } = new();
    }

    public class ItemData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int DamageBonus { get; set; }
    }

    public class GameData
    {
        public List<UnitData> Units { get; set; } = new();
        public List<EnemyData> Enemies { get; set; } = new();
        public List<LootData> Loot { get; set; } = new(); // <-- ДОБАВЛЕНО
        public InventoryData Inventory { get; set; } = new();
        public Dictionary<string, QuestManager.QuestData> Quests { get; set; } = new();
    }

    // --- СОХРАНЕНИЕ ---
    public void SaveGame(Node mainNode)
    {
        var data = new GameData();

        foreach (Node child in mainNode.GetChildren())
        {
            if (child is Unit unit)
            {
                var h = unit.GetNode<Health>("Health");
                data.Units.Add(new UnitData { X = unit.GlobalPosition.X, Y = unit.GlobalPosition.Y, Z = unit.GlobalPosition.Z, Health = h.CurrentHealth });
            }
            else if (child is Enemy enemy)
            {
                var h = enemy.GetNode<Health>("Health");
                data.Enemies.Add(new EnemyData { X = enemy.GlobalPosition.X, Y = enemy.GlobalPosition.Y, Z = enemy.GlobalPosition.Z, Health = h.CurrentHealth });
            }
            else if (child is LootItem loot)
            {
                data.Loot.Add(new LootData
                {
                    X = loot.GlobalPosition.X,
                    Y = loot.GlobalPosition.Y,
                    Z = loot.GlobalPosition.Z,
                    ItemName = loot.ItemName,
                    ItemValue = loot.ItemValue,
                    ItemType = loot.ItemType
                });
            }
        }

        if (Inventory.Instance != null)
        {
            data.Inventory.Gold = Inventory.Instance.Gold;
            foreach (var item in Inventory.Instance.Items)
                data.Inventory.Items.Add(new ItemData { Name = item.Name, Type = item.Type, DamageBonus = item.DamageBonus });
        }

        if (QuestManager.Instance != null)
            data.Quests = QuestManager.Instance.Quests;

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        using var file = Godot.FileAccess.Open(_savePath, Godot.FileAccess.ModeFlags.Write);
        file.StoreString(json);
        GD.Print("Игра сохранена!");
    }

    // --- ЗАГРУЗКА ---
    public void LoadGame(Node mainNode)
    {
        // 1. СНИМАЕМ ПАУЗУ СРАЗУ (до чтения файла и очистки)
        GetTree().Paused = false;

        // 2. Закрываем весь UI (на всякий случай)
        var ui = mainNode.GetNodeOrNull<CanvasLayer>("UI");
        if (ui != null)
        {
            foreach (Node child in ui.GetChildren())
            {
                if (child is Control control)
                {
                    control.Visible = false;
                    control.ProcessMode = ProcessModeEnum.Inherit; // Возвращаем в нормальный режим
                }
            }
        }

        if (!FileAccess.FileExists(_savePath)) return;

        using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        var data = JsonSerializer.Deserialize<GameData>(json);
        if (data == null) return;
        var scenePath = GetTree().CurrentScene.SceneFilePath;
        // 3. Удаляем старые объекты
        var toRemove = new List<Node>();
        foreach (Node child in mainNode.GetChildren())
        {
            if (child is Unit || child is Enemy || child is LootItem)
                toRemove.Add(child);
        }
        foreach (Node child in toRemove)
        {
            child.Free();
        }

        // 4. Создаем новые объекты
        SpawnAll(mainNode, data);

        // 5. Еще раз принудительно снимаем паузу (для надежности)
        GetTree().Paused = false;
    }

    private void SpawnAll(Node mainNode, GameData data)
    {
        // Юниты
        var unitScene = GD.Load<PackedScene>("res://scenes/unit.tscn");
        foreach (var ud in data.Units)
        {
            var unit = unitScene.Instantiate<Unit>();
            unit.Position = new Vector3(ud.X, ud.Y, ud.Z);
            unit.ProcessMode = ProcessModeEnum.Inherit; // Явно указываем, что он не на паузе
            mainNode.AddChild(unit);
            unit.GetNode<Health>("Health").CurrentHealth = ud.Health;
        }

        // Враги
        var enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
        foreach (var ed in data.Enemies)
        {
            var enemy = enemyScene.Instantiate<Enemy>();
            enemy.Position = new Vector3(ed.X, ed.Y, ed.Z);
            enemy.ProcessMode = ProcessModeEnum.Inherit;
            mainNode.AddChild(enemy);
            enemy.GetNode<Health>("Health").CurrentHealth = ed.Health;
        }

        // Лут
        var lootScene = GD.Load<PackedScene>("res://scenes/loot_item.tscn");
        foreach (var ld in data.Loot)
        {
            var loot = lootScene.Instantiate<LootItem>();
            loot.ItemName = ld.ItemName;
            loot.ItemValue = ld.ItemValue;
            loot.ItemType = ld.ItemType;
            loot.Position = new Vector3(ld.X, ld.Y, ld.Z);
            mainNode.AddChild(loot);
        }

        // Инвентарь
        if (Inventory.Instance != null && data.Inventory != null)
        {
            Inventory.Instance.Gold = data.Inventory.Gold;
            Inventory.Instance.Items.Clear();
            foreach (var item in data.Inventory.Items)
                Inventory.Instance.Items.Add(new Inventory.Item { Name = item.Name, Type = item.Type, DamageBonus = item.DamageBonus });
        }

        // Квесты
        if (QuestManager.Instance != null && data.Quests != null)
            QuestManager.Instance.Quests = data.Quests;

        GD.Print("Игра успешно загружена, пауза снята!");
    }
}