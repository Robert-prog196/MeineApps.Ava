using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

public interface IStoryService
{
    StoryChapter? CheckForNewChapter();
    void MarkChapterViewed(string chapterId);
    StoryChapter? GetPendingChapter();
    IReadOnlyList<StoryChapter> GetAllChapters();
    (int viewed, int total) GetProgress();
}
