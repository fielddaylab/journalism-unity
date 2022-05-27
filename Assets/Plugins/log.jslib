mergeInto(LibraryManager.library, 
{
  FBGameStart: function() 
  {
    analytics.logEvent("Game Start", {});
  },
  FBLevelBegin: function(int level_id)
  {
        analytics.logEvent("level_begin", {level_id});
  },
  
  FBBeginStory: function(int level_id, int[] snippits_available)
  {
        analytics.logEvent("begin_story", {level_id, snippits_available});
  },
  FBLevelComplete: function(int level_id, int[] snippits_used, float alignment_score, int quality_score)
  {
        analytics.logEvent("level_complete", {level_id, snippits_used, alignment_score, quality_score});
  },
  FBChooseNode: function(int node_id)
  {
        analytics.logEvent("choose_node", {node_id});
  },
  FBAdvanceScript: function(int node_id)
  {
        analytics.logEvent("advance_script", {node_id});
  },
  FBViewStatus: function()
  {
        analytics.logEvent("view_status", {});
  },
  FBCloseStatus: function()
  {
        analytics.logEvent("close_status", {});
  },
  FBViewStory: function()
  {
        analytics.logEvent("view_story", {});
  },
  FBCloseStory: function()
  {
        analytics.logEvent("close_story", {});
  },
  FBViewEditorNotes: function()
  {
        analytics.logEvent("view_editor_notes", {});
  },
  FBCloseEditorNotes: function()
  {
        analytics.logEvent("close_editor_notes", {});
  },
  FBAddSnippitToStory: function(int snippit_id, int story_slot)
  {
        analytics.logEvent("add_snippit_to_story", {snippit_id, story_slot});
  },
  FBDeleteSnippitFromStory: function(int snippit_id, int story_slot)
  {
        analytics.logEvent("delete_snippit_from_story", {snippit_id, story_slot});
  },
  FBDisplayNode: function(int node_id)
  {
        analytics.logEvent("display_node", {node_id});
  },
  FBDisplayChoices: function(int[] node_ids, string[] choice_texts, int[] destination_node_ids)
  {
        analytics.logEvent("display_choices", {node_ids, choice_texts, destination_node_ids});
  },
});