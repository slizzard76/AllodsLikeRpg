using Godot;
using System.Collections.Generic;

public partial class QuestManager : Node
{
    // Статическая ссылка на самого себя
    public static QuestManager Instance { get; private set; }

    public class QuestData
    {
        public string Name;
        public string Description;
        public int Goal;
        public int Progress;
        public bool IsActive;
        public bool IsDone;
    }

    public Dictionary<string, QuestData> Quests = new();

    public override void _Ready()
    {
        // Защита от дублирования
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }

        Instance = this;

        // Чтобы квесты не удалялись при перезагрузке сцены
        ProcessMode = ProcessModeEnum.Always;

        //SaveManager.Instance.LoadQuests(); // Загружаем при старте
    }

    public void StartQuest(string name, string description, int goal)
    {
        if (!Quests.ContainsKey(name))
        {
            Quests[name] = new QuestData { Name = name, Description = description, Goal = goal, IsActive = true };
            GD.Print($"[Квест] Начат: {name} (0/{goal})");
            //SaveQuests();
        }
    }

    public void AddKillProgress(string questName)
    {
        if (Quests.TryGetValue(questName, out var quest) && quest.IsActive && !quest.IsDone)
        {
            quest.Progress++;
            GD.Print($"[Квест] {questName}: {quest.Progress}/{quest.Goal}");
            //SaveQuests();
        }
    }

    public bool IsQuestComplete(string questName)
    {
        if (Quests.TryGetValue(questName, out var quest))
            return quest.Progress >= quest.Goal;
        return false;
    }

    public void CompleteQuest(string questName)
    {
        if (Quests.TryGetValue(questName, out var quest))
        {
            quest.IsActive = false;
            quest.IsDone = true;
            GD.Print($"[Квест] Завершён: {questName}");
            //SaveQuests();
        }
    }
}