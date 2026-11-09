using Godot;

public partial class Portal : StaticBody3D
{
    [Export] public string TargetScenePath { get; set; } = "res://scenes/island2.tscn";

    // Этот метод будет вызывать Main.cs при клике
    public void EnterPortal(Main main)
    {
        GD.Print("Переход в Астрал...");

        // Сохраняем текущее состояние (здоровье, квесты, инвентарь)
        SaveManager.Instance.SaveGame(main);

        // Запоминаем, что мы переходим через портал
        // (Чтобы в новой сцене знать, что нужно загрузиться)
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(TargetScenePath);
    }
}