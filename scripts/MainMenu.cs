using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBoxContainer/NewGameBtn").Pressed += () => {
            // Удаляем старое сохранение при новой игре
            var dir = DirAccess.Open("user://");
            if (dir.FileExists("savegame.json")) dir.Remove("savegame.json");
            GetTree().ChangeSceneToFile("res://scenes/main.tscn");
        };

        GetNode<Button>("VBoxContainer/ContinueBtn").Pressed += () => {
            if (FileAccess.FileExists("user://savegame.json"))
                GetTree().ChangeSceneToFile("res://scenes/main.tscn");
            else
                GD.Print("Сохранений нет!");
        };

        GetNode<Button>("VBoxContainer/QuitBtn").Pressed += () => {
            GetTree().Quit();
        };
    }
}