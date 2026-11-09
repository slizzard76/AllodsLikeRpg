using Godot;

public partial class GameOverUI : PanelContainer
{
    public override void _Ready()
    {
        Visible = false;

        GetNode<Button>("VBoxContainer/LoadButton").Pressed += () => {
            Visible = false;
            GetTree().Paused = false;
            // Очищаем список выделенных в Main, чтобы не было ошибок
            var main = GetTree().CurrentScene as Main;
            if (main != null) main._selectedUnits.Clear();

            SaveManager.Instance.LoadGame(GetTree().CurrentScene);
        };

        GetNode<Button>("VBoxContainer/RestartButton").Pressed += () => {
            GetTree().Paused = false;
            // Удаляем файл сохранения и перезапускаем сцену
            var dir = DirAccess.Open("user://");
            if (dir.FileExists("savegame.json")) dir.Remove("savegame.json");

            GetTree().ReloadCurrentScene();
        };
    }

    public void ShowGameOver()
    {
        Visible = true;
        GetTree().Paused = true;
    }
}