using Godot;
using System.Collections.Generic;

public partial class QuestLogUI : PanelContainer
{
    private VBoxContainer _questsList;
    private Button _closeButton;

    public override void _Ready()
    {
        _questsList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/QuestsList");
        _closeButton = GetNode<Button>("MarginContainer/VBoxContainer/CloseButton");

        _closeButton.Pressed += ToggleLog; // Привязываем кнопку закрытия
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Открытие/закрытие на клавишу J
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.J)
            {
                ToggleLog();
                GetViewport().SetInputAsHandled(); // Съедаем клик, чтобы он не улетел в игру
            }
        }
    }

    private void ToggleLog()
    {
        Visible = !Visible;

        // Ставим игру на паузу (по желанию), чтобы игрок мог спокойно почитать
        GetTree().Paused = Visible;

        if (Visible)
        {
            UpdateLog();
        }
    }

    private void UpdateLog()
    {
        // Очищаем старый список
        foreach (Node child in _questsList.GetChildren())
            child.QueueFree();

        var qm = QuestManager.Instance;
        if (qm == null || qm.Quests.Count == 0)
        {
            AddText("У вас пока нет квестов.", Colors.Gray);
            return;
        }

        foreach (var quest in qm.Quests.Values)
        {
            if (!quest.IsActive && !quest.IsDone) continue; // Пропускаем неактивные

            // Заголовок квеста
            string status = quest.IsDone ? "[ВЫПОЛНЕН] " : "";
            AddText($"{status}{quest.Name}", quest.IsDone ? Colors.Green : Colors.Gold, true);

            // Описание
            AddText(quest.Description, Colors.LightGray);

            // Прогресс
            if (!quest.IsDone)
            {
                AddText($"Прогресс: {quest.Progress} / {quest.Goal}", Colors.White);
            }

            // Разделитель
            _questsList.AddChild(new HSeparator());
        }
    }

    // Вспомогательный метод для быстрого добавления текста
    private void AddText(string text, Color color, bool isTitle = false)
    {
        Label label = new Label();
        label.Text = text;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.AddThemeColorOverride("font_color", color);

        // Принудительно задаем минимальную ширину, чтобы текст не ломался вертикально
        label.CustomMinimumSize = new Vector2(400, 0);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; // Растягиваем на всю ширину

        if (isTitle)
        {
            label.AddThemeFontSizeOverride("font_size", 20);
        }

        _questsList.AddChild(label);
    }
}