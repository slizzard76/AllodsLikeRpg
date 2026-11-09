using Godot;
using System;
using System.Collections.Generic;

public partial class DialogueUI : PanelContainer
{
    private Label _npcNameLabel;
    private Label _dialogueText;
    private HBoxContainer _optionsContainer;

    // Событие, когда диалог закрывается
    public event Action DialogueClosed;

    public override void _Ready()
    {
        _npcNameLabel = GetNode<Label>("MarginContainer/VBoxContainer/NpcName");
        _dialogueText = GetNode<Label>("MarginContainer/VBoxContainer/DialogueText");
        _optionsContainer = GetNode<HBoxContainer>("MarginContainer/VBoxContainer/OptionsContainer");
    }

    public void ShowDialogue(string npcName, string text, List<(string text, Action action)> options)
    {
        _npcNameLabel.Text = npcName;
        _dialogueText.Text = text;

        // Очищаем старые кнопки
        foreach (Node child in _optionsContainer.GetChildren())
            child.QueueFree();

        // Создаём новые кнопки
        foreach (var option in options)
        {
            Button btn = new Button();
            btn.Text = option.text;
            btn.Pressed += () => {
                option.action?.Invoke();
                // Если действие не закрыло диалог, закрываем по умолчанию
                if (Visible) CloseDialogue();
            };
            _optionsContainer.AddChild(btn);
        }

        Visible = true;
        GetTree().Paused = true; // Останавливаем игру на время диалога
    }

    public void CloseDialogue()
    {
        Visible = false;
        GetTree().Paused = false;
        DialogueClosed?.Invoke();
    }
}