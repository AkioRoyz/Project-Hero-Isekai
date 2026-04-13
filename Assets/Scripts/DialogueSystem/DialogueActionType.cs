public enum DialogueActionType
{
    None,

    // Выдать награду: опыт, золото, предметы
    GiveReward,

    // Удалить предмет из инвентаря
    RemoveItem,

    // Пометить ключ как использованный
    MarkPlayed,

    // Действие над всем квестом: взять, завершить, провалить и т.д.
    QuestAction,

    // Засчитать конкретную задачу квеста
    AcceptQuestObjective
}