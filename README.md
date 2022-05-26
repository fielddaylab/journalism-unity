# journalism-unity
Unity implementation of Journalism Game

# Logging Event Schema
## Progression
level_begin(level_id)
begin_story(level_id, snippit_ids[])
level_complete(level_id, snippits_used[], alignment_score, quality_score)

## Player Events
choose_node(node_id)
advance_script(node_id)
view_status
view_story
view_editor_notes
add_snippit_to_story(snippit_id, story_slot)
delete_snppit_from_story(snippit_id, story_slot)

## Game and Feedback Events
display_node(node_id)
display_choices(choice_node_ids[])
