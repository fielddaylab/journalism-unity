#if UNITY_WEBGL && !UNITY_EDITOR
#define FIREBASE
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using UnityEngine;

namespace JournalismAnalytics
{
    public static class AnalyticsService
    {
        //The formatting gets all ugly if I have to type #if Firebase every time, I'll probably change this later cuz it's kinda dumb code lol
        private static bool IsFirebase()
        {
            #if FIREBASE
                return true;
            #endif

            return false;
        }

        [DllImport("__Internal")] private static extern void FBGameStart();
        public static void LogGameStarted()
        {
            Debug.Log("[Firebase] Game Started");
            if (IsFirebase())
                FBGameStart();
        }


        //Progression Log Events
        [DllImport("__Internal")] private static extern void FBLevelBegin(int level_id);
        public static void LogLevelBegin(int level_id)
        {
            Debug.Log("[Firebase] Level Started, id: " + level_id);
            #if FIREBASE
                FBLevelBegin(level_id);
            #endif
        }

        [DllImport("__Internal")] private static extern void FBBeginStory(int level_id, int[] snippits_available);
        public static void LogBeginStory(int level_id, int[] snippits_available) 
        {
            Debug.Log("[Firebase] Started story with id: " + level_id + ", snippits available: " + snippits_available);
            if(IsFirebase())
                FBBeginStory(level_id, snippits_available);
        }

        [DllImport("__Internal")] private static extern void FBLevelComplete(int level_id);
        public static void LogLevelComplete(int level_id, int[] snippits_used, float alignment_score, int quality_score)
        {
            Debug.Log("[Firebase] Level Completed! Level_ID: " + level_id + ", snippits used: " + snippits_used + ", alignment score: " + alignment_score + ", quality score: " + quality_score);
            #if FIREBASE
                FBLevelComplete(level_id, snippits_used, alignment_score, quality_score);
            #endif
        }


        //Player Events
        [DllImport("__Internal")] private static extern void FBChooseNode(int node_id);
        public static void LogChooseNode(int node_id) 
        {
            Debug.Log("[Firebase] Chose node with id: " + node_id);
            if (IsFirebase())
                FBChooseNode(node_id);
        }

        [DllImport("__Internal")] private static extern void FBAdvanceScript(int node_id);
        public static void LogAdvanceScript(int node_id) 
        {
            Debug.Log("[Firebase] Advanced script with id: " + node_id);
            if (IsFirebase())
                FBAdvanceScript(node_id);
        }

        [DllImport("__Internal")] private static extern void FBViewStatus();
        public static void LogViewStatus() 
        {
            Debug.Log("[Firebase] Opened status");
            if (IsFirebase())
                FBViewStatus();
        }

        [DllImport("__Internal")] private static extern void FBCloseStatus();
        public static void LogCloseStatus()
        {
            Debug.Log("[Firebase] Closed status");
            if (IsFirebase())
                FBCloseStatus();
        }

        [DllImport("__Internal")] private static extern void FBViewStory();
        public static void LogViewStory()
        {
            Debug.Log("[Firebase] Opened story");
            if (IsFirebase())
                FBViewStory();
        }

        [DllImport("__Internal")] private static extern void FBCloseStory();
        public static void LogCloseStory()
        {
            Debug.Log("[Firebase] Closed story");
            if (IsFirebase())
                FBCloseStory();
        }

        [DllImport("__Internal")] private static extern void FBViewEditorNotes();
        public static void LogViewEditorNotes()
        {
            Debug.Log("[Firebase] Opened editor notes");
            if (IsFirebase())
                FBViewEditorNotes();
        }

        [DllImport("__Internal")] private static extern void FBCloseEditorNotes();
        public static void LogCloseEditorNotes()
        {
            Debug.Log("[Firebase] Closed editor notes");
            if (IsFirebase())
                FBCloseEditorNotes();
        }

        [DllImport("__Internal")] private static extern void FBAddSnippitToStory(int snippit_id, int story_slot);
        public static void LogAddSnippitToStory(int snippit_id, int story_slot)
        {
            Debug.Log("[Firebase] added snippit with id: "+snippit_id+", to story slot: "+story_slot);
            if (IsFirebase())
                FBAddSnippitToStory(snippit_id, story_slot);
        }

        [DllImport("__Internal")] private static extern void FBDeleteSnippitFromStory(int snippit_id, int story_slot);
        public static void LogDeleteSnippitFromStory(int snippit_id, int story_slot)
        {
            Debug.Log("[Firebase] deleted snippit with id: " + snippit_id + ", from story slot: " + story_slot);
            if (IsFirebase())
                FBDeleteSnippitFromStory(snippit_id, story_slot);
        }


        //Game + Feedback Events
        [DllImport("__Internal")] private static extern void FBDisplayNode(int node_id);
        public static void LogDisplayNode(int node_id)
        {
            Debug.Log("[Firebase] displayed node with id: " + node_id);
            if (IsFirebase())
                FBDisplayNode(node_id);
        }

        [DllImport("__Internal")] private static extern void FBDisplayChoices(int[] node_ids, string[] choice_texts, int[] destination_node_ids);
        public static void LogDisplayChoices(int[] node_ids, string[] choice_texts, int[] destination_node_ids)
        {
            string str = "[Firebase] displaying choices: \n";
            for(int i = 0; i < node_ids.Length; i++)
            {
                str += "[node_id: " + node_ids[i] + ", text: " + choice_texts[i] + ", destination node_id:" + destination_node_ids[i]+"\n";
            }
            Debug.Log(str);
            if (IsFirebase())
                FBDisplayChoices(node_ids, choice_texts, destination_node_ids);
        }
    }
}
