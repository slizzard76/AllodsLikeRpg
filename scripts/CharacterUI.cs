using Godot;

public partial class CharacterUI : PanelContainer
{
    private Label _levelLabel;
    private Label _xpLabel;
    private Label _pointsLabel;

    private Button _strBtn;
    private Button _agiBtn;
    private Button _intBtn;

    private Unit _currentUnit;

    private HBoxContainer _hBoxContainer;

    public override void _Ready()
    {
        // Ищем лейблы по их точным путям
        _levelLabel = GetNode<Label>("VBoxContainer/LevelLabel");
        _xpLabel = GetNode<Label>("VBoxContainer/XpLabel");
        _pointsLabel = GetNode<Label>("VBoxContainer/PointsLabel");

        // Ищем кнопки по всему дереву VBoxContainer
        // Это защитит нас от того, что вы могли назвать HBoxContainer иначе
        foreach (Node child in GetNode("VBoxContainer").GetChildren())
        {
            if (child is HBoxContainer hbox)
            {

                GD.Print($"Выбран HBoxContainer: {child.Name}"); // <--- ОТЛАДКА
                foreach (Node hboxChild in hbox.GetChildren())
                {
                    if (hboxChild is Button btn)
                    {
                        // Определяем, какая это кнопка, по её текущему тексту
                        if (btn.Text.Contains("Сила")) _strBtn = btn;
                        else if (btn.Text.Contains("Ловкость")) _agiBtn = btn;
                        else if (btn.Text.Contains("Интеллект")) _intBtn = btn;
                    }
                }
            }
        }

        // Подключаем кнопки, если они нашлись
        if (_strBtn != null) _strBtn.Pressed += () => AddStat("Str");
        if (_agiBtn != null) _agiBtn.Pressed += () => AddStat("Agi");
        if (_intBtn != null) _intBtn.Pressed += () => AddStat("Int");

        // Кнопка закрытия
        var closeBtn = GetNodeOrNull<Button>("VBoxContainer/CloseButton");
        if (closeBtn != null) closeBtn.Pressed += ToggleUI;

        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.C)
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

        if (Visible)
        {
            // Ищем Main как текущую сцену
            var main = GetTree().CurrentScene as Main;

            if (main != null && main._selectedUnits.Count > 0)
            {
                _currentUnit = main._selectedUnits[0];
            }
            else
            {
                GD.Print("ОШИБКА: Main не найден или юнит не выделен!"); // <--- ОТЛАДКА
            }

            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (_currentUnit == null || !IsInstanceValid(_currentUnit))
        {
            _levelLabel.Text = "Юнит не выбран";
            _xpLabel.Text = "";
            _pointsLabel.Text = "";
            return;
        }

        _levelLabel.Text = $"Уровень: {_currentUnit.Level}";
        _xpLabel.Text = $"Опыт: {_currentUnit.Experience} / {_currentUnit.ExpToNextLevel}";
        _pointsLabel.Text = $"Свободные очки: {_currentUnit.StatPoints}";

        // Обновляем текст на кнопках, добавляя текущие значения
        if (_strBtn != null) _strBtn.Text = $"Сила: {_currentUnit.Strength} [+]";
        if (_agiBtn != null) _agiBtn.Text = $"Ловкость: {_currentUnit.Agility} [+]";
        if (_intBtn != null) _intBtn.Text = $"Интеллект: {_currentUnit.Intellect} [+]";
    }

    private void AddStat(string stat)
    {
        GD.Print($"Нажата кнопка: {stat}"); // <--- ОТЛАДКА

        if (_currentUnit == null)
        {
            GD.Print("ОШИБКА: _currentUnit равен null! Вы не выделили юнита.");
            return;
        }

        if (_currentUnit.StatPoints <= 0)
        {
            GD.Print("ОШИБКА: Нет свободных очков!");
            return;
        }

        _currentUnit.StatPoints--;
        if (stat == "Str") _currentUnit.Strength++;
        else if (stat == "Agi") _currentUnit.Agility++;
        else if (stat == "Int") _currentUnit.Intellect++;

        UpdateUI();
    }
}