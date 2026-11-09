using Godot;

public partial class Npc : StaticBody3D
{
    [Export] public string NpcName { get; set; } = "Странник";
    [Export] public string NpcDialogue { get; set; } = "Приветствую, путник. Помоги мне избавиться от гоблинов!";
    [Export] public int QuestKillGoal { get; set; } = 3;
    [Export] public string QuestName { get; set; } = "Очистка острова";

    private bool _questGiven = false;
    private bool _questCompleted = false;

    public void Interact(DialogueUI ui, QuestManager qm)
    {
        var options = new System.Collections.Generic.List<(string, System.Action)>();

        if (!_questGiven)
        {
            options.Add(("Взять квест", () => {
                _questGiven = true;
                qm.StartQuest(QuestName, "Странник просит очистить остров от гоблинов.", QuestKillGoal);
                ui.ShowDialogue(NpcName, "Отлично! Убей 3 гоблинов и возвращайся.", new System.Collections.Generic.List<(string, System.Action)> {
                    ("Хорошо", null)
                });
            }
            ));
            options.Add(("Отказаться", null));
            ui.ShowDialogue(NpcName, NpcDialogue, options);
        }
        else if (_questGiven && !_questCompleted)
        {
            if (qm.IsQuestComplete(QuestName))
            {
                _questCompleted = true;
                qm.CompleteQuest(QuestName);
                options.Add(("Получить награду", null));
                ui.ShowDialogue(NpcName, "Ты справился! Вот твоя награда — 50 золота.", options);
            }
            else
            {
                options.Add(("Ещё работаю", null));
                ui.ShowDialogue(NpcName, "Гоблины ещё живы. Иди работай!", options);
            }
        }
        else
        {
            options.Add(("Пока", null));
            ui.ShowDialogue(NpcName, "Спасибо за помощь, герой!", options);
        }
    }
}