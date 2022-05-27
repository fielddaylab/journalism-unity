using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JournalismAnalytics;

public class AnalyticsEventPasser : MonoBehaviour
{
    public void StatsOpened()
    {
        AnalyticsService.LogViewStatus();
    }

    public void StatsClosed()
    {
        AnalyticsService.LogCloseStatus();
    }

    public void StoryOpened()
    {
        AnalyticsService.LogViewStory();
    }

    public void StoryClosed()
    {
        AnalyticsService.LogCloseStory();
    }

    public void NotesOpened()
    {
        AnalyticsService.LogViewEditorNotes();
    }

    public void NotesClosed()
    {
        AnalyticsService.LogCloseEditorNotes();
    }
}
