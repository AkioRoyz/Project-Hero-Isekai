public enum DialogueConditionType
{
    None,

    // Проверка минимального уровня игрока
    PlayerLevelAtLeast,

    // Проверка наличия предмета
    HasItem,

    // Проверка отсутствия предмета
    DoesNotHaveItem,

    // Условие "этот шаг доступен только один раз"
    PlayOnce,

    // Проверка состояния квеста
    QuestState,

    // Проверка текущего этапа квеста по StepId
    QuestStepId
}