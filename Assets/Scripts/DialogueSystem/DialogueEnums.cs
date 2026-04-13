public enum DialogueNodeType
{
    // Обычная реплика без выбора ответа
    Line,

    // Реплика с вариантами ответа
    Choice
}

public enum DialogueSpeakerMode
{
    // Имя говорящего берётся из источника диалога
    // Например, из NPC, который запустил этот диалог
    UseSourceSpeakerName,

    // Имя говорящего задаётся вручную в этом узле
    UseCustomName
}